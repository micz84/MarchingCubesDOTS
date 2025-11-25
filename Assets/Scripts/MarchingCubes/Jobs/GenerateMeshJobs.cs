using MarchingCubes.DataStructures;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.Jobs
{
    [BurstCompile]
    public struct CountVerticesPerLayerJob : IJobParallelFor{
        
        [ReadOnly] public int3 CubesSize;
        [ReadOnly] public NativeArray<CubeTrianglesIndices> CubeTrianglesIndices;
        [ReadOnly] public NativeArray<CubeData> CubeCodes;
        
        [WriteOnly] public NativeArray<int> VerticesPerLayer;
        public void Execute(int z)
        {
            var layerStride = CubesSize.x * CubesSize.y;
            var stride = layerStride * z;
            var count = 0;
            for (var y = 0; y < CubesSize.y; y++)
            {
                var offset = stride + y * CubesSize.x;
                for (var x = 0; x < CubesSize.x; x++)
                {
                    var i = x + offset;
                    var cubeData = CubeCodes[i];
                    var cubeTrianglesIndices = CubeTrianglesIndices[cubeData.Code];
                    count += cubeTrianglesIndices.EndIndex - cubeTrianglesIndices.StartIndex;
                }
            }
            VerticesPerLayer[z] = count;
        }
    }
    
    [BurstCompile]
    public struct IndexOffsetPerLayerJob : IJob
    {
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<int> TrianglesPerLayer;
        [WriteOnly] public NativeArray<int> TriangleIndexOffsetPerLayer;
        [WriteOnly]
        public NativeList<float3> Vertices;
        [WriteOnly]
        public NativeList<float3> Normals;
        [WriteOnly]
        public NativeList<int> Triangles;
        [WriteOnly]
        public NativeList<TriangleData> TriangleDatas;
        public void Execute()
        {
            var total = 0;
            for(var z = 0; z<TrianglesPerLayer.Length; z++)
            {
                TriangleIndexOffsetPerLayer[z] = total;
                total += TrianglesPerLayer[z];
            }
            
            Vertices.Resize(total * 3, NativeArrayOptions.UninitializedMemory);
            Normals.Resize(total * 3, NativeArrayOptions.UninitializedMemory);
            Triangles.Resize(total * 3, NativeArrayOptions.UninitializedMemory);
            TriangleDatas.Resize(total, NativeArrayOptions.UninitializedMemory);
            
        }
    }
    [BurstCompile]
    public struct CalculateMeshDataJob : IJobParallelFor
    {
        [ReadOnly] public int3 CubesSize;
        [ReadOnly] public int CubesPerUnit;
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<int> IndexOffsetPerLayer;
        [ReadOnly] public NativeArray<CubeData> CubeCodes;
        [ReadOnly] public NativeArray<Triangle> TriangulationData;
        [ReadOnly] public NativeArray<CubeTrianglesIndices> CubeTrianglesIndices;
        [ReadOnly] public NativeArray<float3x2> EdgePoints;
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeList<TriangleData> TriangleDatas;
        public void Execute(int z)
        {
            var cubeWidth = 1f / CubesPerUnit;
            var verticalStride = CubesSize.x * CubesSize.y;
            var stride = verticalStride * z;
            var posZ = z * cubeWidth;
            var triangleIndexInMesh = IndexOffsetPerLayer[z];
            
            for (var y = 0; y < CubesSize.y; y++)
            {
                var offset = stride + y * CubesSize.x;
                var posY = y * cubeWidth;
                for (var x = 0; x < CubesSize.x; x++)
                {
                    var i = x + offset;
                    var cubeCode = CubeCodes[i].Code;
                    if(cubeCode == 0 || cubeCode == 255)
                        continue;
                    var edgeFactors = CubeCodes[i].EdgeFactors;
                    var cubePosition = new float3(x * cubeWidth, posY, posZ);
                    var indices = CubeTrianglesIndices[cubeCode];
                    for (var triangleIndex = indices.StartIndex; triangleIndex < indices.EndIndex; triangleIndex++)
                    {
                        var triangle = TriangulationData[triangleIndex];
                        var p1 = GetVertexPosition(triangle.Vertex0, edgeFactors);
                        var p2 = GetVertexPosition(triangle.Vertex1, edgeFactors);
                        var p3 = GetVertexPosition(triangle.Vertex2, edgeFactors);
                        var triangleData = new TriangleData
                        {
                            Vertex0 = p1 + cubePosition,
                            Vertex1 = p2 + cubePosition,
                            Vertex2 = p3 + cubePosition,
                        };
                        TriangleDatas[triangleIndexInMesh] = triangleData;
                        triangleIndexInMesh += 1;
                    }
                }
            }
        }
        private float3 GetVertexPosition(byte index, EdgeFactors factors)
        {
            var points = EdgePoints[index];
            return math.lerp(points.c0, points.c1, factors[index]);
        }
    }
    
    
    [BurstCompile]
    public struct UpdateVertices : IJobParallelForDefer
    {
        [ReadOnly]
        public NativeList<TriangleData> TriangleDatas;
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeList<float3> Vertices;
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeList<float3> Normals;
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeList<int> Triangles;
        
        public void Execute(int index)
        {
            var triangle = TriangleDatas[index];
            var index0 = index * 3;
            var index1 = index * 3 + 1;
            var index2 = index * 3 + 2;
            var p1 = triangle.Vertex0;
            var p2 = triangle.Vertex1;
            var p3 = triangle.Vertex2;
            Vertices[index0] = p1;
            Vertices[index1] = p2;
            Vertices[index2] = p3;
            var normal = math.normalize(math.cross(p2 - p1, p3 - p1));
            Normals[index0] = normal;
            Normals[index1] = normal;
            Normals[index2] = normal;
            Triangles[index0] = index0;
            Triangles[index1] = index1;
            Triangles[index2] = index2;

        }
    }
    
    
    [BurstCompile]
    public struct CalculateBoundsJob:IJob 
    {
        [ReadOnly] public NativeList<float3> Vertices;
        [WriteOnly] public NativeReference<Bounds> Bounds;
        public void Execute()
        {
            var bounds = new Bounds();
            for (var i = 0; i < Vertices.Length; i++)
            {
                bounds.Encapsulate(Vertices[i]);
            }
            Bounds.Value = bounds;
        }
    }
    
    public struct TriangleData
    {
        public float3 Vertex0;
        public float3 Vertex1;
        public float3 Vertex2;
    }
    
}