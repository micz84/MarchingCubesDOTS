using MarchingCubes.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.DataStructures
{
    public struct MeshData
    {

        public NativeList<float3> Vertices { get; private set; }
        public NativeList<float3> Normals { get; private set; }
        public NativeList<int> Indices  { get; private set; }
        public NativeReference<Bounds> Bounds { get; private set; }
        private readonly NativeArray<Triangle> _triangulationData;
        private readonly NativeArray<CubeTrianglesIndices> _cubeTrianglesIndices;
        private readonly NativeArray<float3x2> _edgePoints;
        private NativeArray<int> _verticesPerLayer;
        private NativeArray<int> _indexOffsetPerLayer;
        private NativeList<TriangleData> _triangleDatas;

        public MeshData(NativeArray<Triangle> triangulationData, NativeArray<CubeTrianglesIndices> cubeTrianglesIndices,
            NativeArray<float3x2> edgePoints)
        {
            _triangulationData = triangulationData;
            _cubeTrianglesIndices = cubeTrianglesIndices;
            Vertices = default;
            Normals = default;
            Indices = default;
            _triangleDatas = default;
            _verticesPerLayer = default;
            _indexOffsetPerLayer = default;
            Bounds = default;
            _edgePoints = edgePoints;
        }
        
        public JobHandle ScheduleMeshDataGeneration(ref TerrainChunk chunk, JobHandle handle)
        {
            var size = chunk.CubesCounts.z;
            Vertices = new NativeList<float3>(Allocator.TempJob);
            Normals =  new NativeList<float3>(Allocator.TempJob);
            Indices = new NativeList<int>(Allocator.TempJob);
            _triangleDatas = new NativeList<TriangleData>(Allocator.TempJob);
            _verticesPerLayer = new NativeArray<int>(size, Allocator.TempJob);
            _indexOffsetPerLayer = new NativeArray<int>(size, Allocator.TempJob);
            Bounds = new NativeReference<Bounds>(Allocator.TempJob);
           
            var countVerticesPerLayer = new CountVerticesPerLayerJob()
            {
                CubesSize = chunk.CubesCounts,
                CubeTrianglesIndices = _cubeTrianglesIndices,
                CubeCodes = chunk.CubeCodes,
                VerticesPerLayer = _verticesPerLayer
            };
            handle = countVerticesPerLayer.Schedule(size, 1, handle);
            var indexOffset = new IndexOffsetPerLayerJob()
            {
                TrianglesPerLayer = _verticesPerLayer,
                TriangleIndexOffsetPerLayer = _indexOffsetPerLayer,
                TriangleDatas = _triangleDatas,
                Vertices = Vertices,
                Triangles = Indices,
                Normals = Normals
            };
            handle = indexOffset.Schedule(handle);
            var calculateMeshDataJob = new CalculateMeshDataJob()
            {
                CubesSize = chunk.CubesCounts,
                CubesPerUnit = chunk.CubesPerUnit,
                IndexOffsetPerLayer = _indexOffsetPerLayer,
                CubeCodes = chunk.CubeCodes,
                TriangulationData = _triangulationData,
                CubeTrianglesIndices = _cubeTrianglesIndices,
                EdgePoints = _edgePoints,
                TriangleDatas = _triangleDatas
                
            };
            handle = calculateMeshDataJob.Schedule(size, 1,handle);
            var updateVertices = new UpdateVertices()
            {
                TriangleDatas = _triangleDatas,
                Vertices = Vertices,
                Normals = Normals,
                Triangles = Indices
            };
            handle = updateVertices.Schedule(_triangleDatas, 64, handle);
            _triangleDatas.Dispose(handle);
            var calculateBounds = new CalculateBoundsJob()
            {
                Vertices = Vertices,
                Bounds = Bounds
            };
            handle = calculateBounds.Schedule(handle);
            return handle;
        }

        public void DisposeMeshData()
        {
            if (Vertices.IsCreated)
                Vertices.Dispose();
            if (Normals.IsCreated)
                Normals.Dispose();
            if (Indices.IsCreated)
                Indices.Dispose();
            if(Bounds.IsCreated)
                Bounds.Dispose();
        }
    }
}