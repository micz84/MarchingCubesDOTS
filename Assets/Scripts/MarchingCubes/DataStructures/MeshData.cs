using MarchingCubes.Jobs;
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
        private NativeList<TriangleData> _triangleDatas;

        public MeshData(NativeArray<Triangle> triangulationData, 
            NativeArray<CubeTrianglesIndices> cubeTrianglesIndices)
        {
            _triangulationData = triangulationData;
            _cubeTrianglesIndices = cubeTrianglesIndices;
            Vertices = default;
            Normals = default;
            Indices = default;
            _triangleDatas = default;
            Bounds = default;
        }
        public JobHandle ScheduleMeshDataGeneration(ref TerrainChunk chunk, JobHandle handle)
        {
            Vertices = new NativeList<float3>(Allocator.TempJob);
            Normals =  new NativeList<float3>(Allocator.TempJob);
            Indices = new NativeList<int>(Allocator.TempJob);
            _triangleDatas = new NativeList<TriangleData>(Allocator.TempJob);
            Bounds = new NativeReference<Bounds>(Allocator.TempJob);
           
            var calculateMeshDataJob = new GenerateMeshDataJob()
            {
                VerticesMap = chunk.EdgeVertexData,
                CubeDatas = chunk.CubeCodes,
                VertexCount = chunk.VertexCount,
                TrianglesCount = chunk.TrianglesCount,
                TriangulationData = _triangulationData,
                CubeTrianglesIndices = _cubeTrianglesIndices,
                Vertices = Vertices,
                Normals = Normals,
                Triangles = Indices
                
            };
            handle = calculateMeshDataJob.Schedule(handle);
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
            
            if(_triangleDatas.IsCreated)
                _triangleDatas.Dispose();
        }
    }
}