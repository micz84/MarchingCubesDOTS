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
        private readonly int3 _position;
        private readonly SimpleSmoothTerrain _terrain;
        
        public TerrainChunk(SimpleSmoothTerrain terrain, int3 position, int3 size, int cubesPerUnit)
        {
            _position = position;
            _terrain = terrain;
            CubesPerUnit = cubesPerUnit;
            CubeCodes = default;
            CubesCounts = new int3(size.x * CubesPerUnit, size.y * CubesPerUnit, size.z * CubesPerUnit);
            var totalCubesCount = CubesCounts.x * CubesCounts.y * CubesCounts.z;
            CubeCodes = new NativeArray<CubeData>(totalCubesCount, Allocator.Persistent);
            var handle = new UpdateCubesJob
            {
                ChunkWorldPosition = _position,
                CubesPerUnit = CubesPerUnit,
                CubesCounts = CubesCounts,
                Terrain = _terrain,
                CubeCodes = CubeCodes
            }.Schedule();
            handle.Complete();
        }

        public void Dispose()
        {
            if (CubeCodes.IsCreated)
                CubeCodes.Dispose();
        }
        public void Update(out JobHandle handle)
        {
            handle = new UpdateCubesJob
            {
                ChunkWorldPosition = _position,
                CubesPerUnit = CubesPerUnit,
                CubesCounts = CubesCounts,
                Terrain = _terrain,
                CubeCodes = CubeCodes
            }.Schedule();
        }
    }

    public struct CubeData
    {
        public byte Code;
        public EdgeFactors EdgeFactors;
    }

    public struct EdgeFactors
    {
        private float4x3 _factors;
        public float this[int index] { get => _factors[index/4][index%4]; set => _factors[index/4][index%4] = value;  }
    }
    
    public struct VertexFactors
    {
        private float4x2 _factors;
        public float this[int index] { get => _factors[index/4][index%4]; set => _factors[index/4][index%4] = value;  }
    }
}