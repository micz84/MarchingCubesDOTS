using System;
using MarchingCubes.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MarchingCubes.DataStructures
{
    public struct TerrainChunk : IDisposable
    {
        public int CubesPerUnit { get; }
        public int3 CubesCounts { get; }
        public NativeArray<CubeData> CubeCodes { get; }
        public NativeHashMap<int, float3> EdgeVertexData { get; }
        public NativeReference<int> VertexCount { get; }
        public NativeReference<int> TrianglesCount { get; }
        private readonly int3 _position;
        private readonly SimpleSmoothTerrain _terrain;
        private readonly NativeArray<float3x2> _edgePoints;
        private readonly NativeArray<int2> _edgeVertexPairIndices; 
        private readonly NativeArray<int> _trianglesPerCube;
        private NativeHashMap<int3,int> _edgeVertexAdded;
        private NativeArray<int3> _offsets;


        public TerrainChunk(SimpleSmoothTerrain terrain, int3 position, int3 size, int cubesPerUnit, 
            NativeArray<float3x2> edgePoints, NativeArray<int2> edgeVertexPairIndices, NativeArray<int> trianglesPerCube)
        {
            CubesPerUnit = cubesPerUnit;
            CubesCounts = new int3(size.x * CubesPerUnit, size.y * CubesPerUnit, size.z * CubesPerUnit);
            var totalCubesCount = CubesCounts.x * CubesCounts.y * CubesCounts.z;
            CubeCodes = new NativeArray<CubeData>(totalCubesCount, Allocator.Persistent);
            EdgeVertexData = new NativeHashMap<int, float3>(totalCubesCount * 4, Allocator.Persistent);
            VertexCount = new NativeReference<int>(0, Allocator.Persistent);
            TrianglesCount = new NativeReference<int>(0, Allocator.Persistent);
            _position = position;
            _terrain = terrain;
            _edgePoints = edgePoints;
            _edgeVertexPairIndices = edgeVertexPairIndices;
            _trianglesPerCube = trianglesPerCube;
            var totalVerticesCount = (CubesCounts.x + 1) * (CubesCounts.y + 1) * (CubesCounts.z + 1);
            _edgeVertexAdded = new NativeHashMap<int3,int>(totalVerticesCount, Allocator.Persistent);
            // TODO: move to helper array
            _offsets = new NativeArray<int3>(12, Allocator.Persistent);
            _offsets[0] = new int3(1, 0, 0);
            _offsets[1] = new int3(2, 1, 0);
            _offsets[2] = new int3(1, 2, 0);
            _offsets[3] = new int3(0, 1, 0);
            _offsets[4] = new int3(1, 0, 2);
            _offsets[5] = new int3(2, 1, 2);
            _offsets[6] = new int3(1, 2, 2);
            _offsets[7] = new int3(0, 1, 2);
            _offsets[8] = new int3(0, 0, 1);
            _offsets[9] = new int3(2, 0, 1);
            _offsets[10] = new int3(2, 2, 1);
            _offsets[11] = new int3(0, 2, 1);
            ScheduleCubeUpdateJob().Complete();
        }

        public void Dispose()
        {
            if (CubeCodes.IsCreated)
                CubeCodes.Dispose();
            if (EdgeVertexData.IsCreated)
                EdgeVertexData.Dispose();
            if (VertexCount.IsCreated)
                VertexCount.Dispose();
            if (TrianglesCount.IsCreated)
                TrianglesCount.Dispose();
            if(_edgeVertexAdded.IsCreated)
                _edgeVertexAdded.Dispose();
            if(_offsets.IsCreated)
                _offsets.Dispose();
        }
        
        public void Update(out JobHandle handle)
        {
            handle = ScheduleCubeUpdateJob();
        }
        
        private JobHandle ScheduleCubeUpdateJob()
        {
            _edgeVertexAdded.Clear();
            var handle = new UpdateCubesJob
            {
                ChunkWorldPosition = _position,
                CubesPerUnit = CubesPerUnit,
                CubesCounts = CubesCounts,
                TrianglesPerCube = _trianglesPerCube,
                Terrain = _terrain,
                CubeCodes = CubeCodes,
                EdgeVertexData = EdgeVertexData,
                EdgePoints = _edgePoints,
                EdgeVertexIndices = _edgeVertexPairIndices,
                VertexCount = VertexCount,
                TrianglesCount = TrianglesCount,
                VertexIndexInMesh = _edgeVertexAdded,
                Offsets = _offsets
            }.Schedule();
            return handle;
        }
    }

    public struct CubeData
    {
        public byte Code;
        public EdgeVertexIndex EdgeVertexIndex;
    }

    public struct EdgeVertexIndex
    {
        private int4x3 _factors;
        public int this[int index] { get => _factors[index/4][index%4]; set => _factors[index/4][index%4] = value;  }
    }
    
    public struct VertexFactors
    {
        private float4x2 _factors;
        public float this[int index] { get => _factors[index/4][index%4]; set => _factors[index/4][index%4] = value;  }
    }
}