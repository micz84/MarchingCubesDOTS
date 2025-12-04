using MarchingCubes.DataStructures;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MarchingCubes.Jobs
{
    public struct GenerateMeshDataJob:IJob
    {
        [ReadOnly] public NativeHashMap<int, float3> VerticesMap;
        [ReadOnly] public NativeArray<CubeData> CubeDatas;
        [ReadOnly] public NativeReference<int> VertexCount;
        [ReadOnly] public NativeReference<int> TrianglesCount;
        [ReadOnly] public NativeArray<Triangle> TriangulationData;
        [ReadOnly] public NativeArray<CubeTrianglesIndices> CubeTrianglesIndices;
        
         public NativeList<float3> Vertices;
        [WriteOnly] public NativeList<float3> Normals;
        [WriteOnly] public NativeList<int> Triangles;
        
        public void Execute()
        {
            Vertices.Resize(VertexCount.Value, NativeArrayOptions.UninitializedMemory);
            Normals.Resize(VertexCount.Value, NativeArrayOptions.UninitializedMemory);
            Triangles.Resize(TrianglesCount.Value * 3, NativeArrayOptions.UninitializedMemory);
            var index = 0;
            for (var vertexIndex = 0; vertexIndex < VertexCount.Value; vertexIndex++)
            {
                Vertices[vertexIndex] = VerticesMap[vertexIndex];
            }
            for (var cubeIndex = 0; cubeIndex < CubeDatas.Length; cubeIndex++)
            {
                var cubeData = CubeDatas[cubeIndex];
                var cubeCode = cubeData.Code;
                if(cubeCode == 0 || cubeCode == 255)
                    continue;
                var indices = CubeTrianglesIndices[cubeCode];
                for (var triangleIndex = indices.StartIndex; triangleIndex < indices.EndIndex; triangleIndex++)
                {
                    var triangle = TriangulationData[triangleIndex];
                    Triangles[index++] = cubeData.EdgeVertexIndex[triangle.Vertex0];
                    Triangles[index++] = cubeData.EdgeVertexIndex[triangle.Vertex1];
                    Triangles[index++] = cubeData.EdgeVertexIndex[triangle.Vertex2];
                }
            }
            
        }
    }
}