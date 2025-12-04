using System.Collections.Generic;
using MarchingCubes.DataStructures;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.Utils
{
    public class HelperArrays
    {
        public NativeArray<Triangle> TriangulationData => _triangulationData;
        public NativeArray<CubeTrianglesIndices> CubeTrianglesIndices => _cubeTrianglesIndices;
        public NativeArray<float3> VertexOffsets => _vertexOffsets;
        public NativeArray<int> TrianglesPerCube => _trianglesPerCube;
        public NativeArray<int2> EdgeVertexPairIndices => _edgeVertexPairIndices;
        public NativeArray<float3x2> EdgePoints => _edgesPoints;
        
        private NativeArray<Triangle> _triangulationData;
        private NativeArray<CubeTrianglesIndices> _cubeTrianglesIndices;
        private NativeArray<int> _trianglesPerCube;
        private NativeArray<float3> _vertexOffsets;
        private NativeArray<int2> _edgeVertexPairIndices;
        private NativeArray<float3x2> _edgesPoints;

        public HelperArrays(int cubesPerUnit)
        {
            CreateHelperArrays(1f / cubesPerUnit);
        }
        
        public void Dispose()
        {
            _triangulationData.Dispose();
            _cubeTrianglesIndices.Dispose();
            _vertexOffsets.Dispose();
            _edgeVertexPairIndices.Dispose();
            _trianglesPerCube.Dispose();
        }
        
        
//            7             6
//            +-------------+               +-----6-------+   
//          / |           / |             / |            /|   
//        /   |         /   |          11   7         10   5
//    3 +-----+-------+  2  |         +-----+2------+     |   
//      |   4 +-------+-----+ 5       |     +-----4-+-----+   
//      |   /         |   /           3   8         1   9
//      | /           | /             | /           | /       
//    0 +-------------+ 1             +------0------+     
        private void CreateHelperArrays(float cubeWidth)
        {
            var cubeVertexStep = cubeWidth / 2;
            _vertexOffsets = new NativeArray<float3>(8, Allocator.Persistent);
            _vertexOffsets[0] = new float3(-cubeVertexStep, -cubeVertexStep, -cubeVertexStep);
            _vertexOffsets[1] = new float3(cubeVertexStep, -cubeVertexStep, -cubeVertexStep);
            _vertexOffsets[2] = new float3(cubeVertexStep, cubeVertexStep, -cubeVertexStep);
            _vertexOffsets[3] = new float3(-cubeVertexStep, cubeVertexStep, -cubeVertexStep);
            _vertexOffsets[4] = new float3(-cubeVertexStep, -cubeVertexStep, cubeVertexStep);
            _vertexOffsets[5] = new float3(cubeVertexStep, -cubeVertexStep, cubeVertexStep);
            _vertexOffsets[6] = new float3(cubeVertexStep, cubeVertexStep, cubeVertexStep);
            _vertexOffsets[7] = new float3(-cubeVertexStep, cubeVertexStep, cubeVertexStep);
            _edgeVertexPairIndices = new NativeArray<int2>(12, Allocator.Persistent);
            _edgeVertexPairIndices[0] = new int2(0, 1);
            _edgeVertexPairIndices[1] = new int2(1, 2);
            _edgeVertexPairIndices[2] = new int2(3, 2);
            _edgeVertexPairIndices[3] = new int2(0, 3);
            _edgeVertexPairIndices[4] = new int2(4, 5);
            _edgeVertexPairIndices[5] = new int2(5, 6);
            _edgeVertexPairIndices[6] = new int2(6, 7);
            _edgeVertexPairIndices[7] = new int2(4, 7);
            _edgeVertexPairIndices[8] = new int2(0, 4);
            _edgeVertexPairIndices[9] = new int2(1, 5);
            _edgeVertexPairIndices[10] = new int2(2, 6);
            _edgeVertexPairIndices[11] = new int2(3, 7);

            _edgesPoints = new NativeArray<float3x2>(12, Allocator.Persistent);
            for (var edgeIndex = 0; edgeIndex < 12; edgeIndex++)
            {
                var edgeVertexPairIndex = _edgeVertexPairIndices[edgeIndex];
                _edgesPoints[edgeIndex] = new float3x2(_vertexOffsets[edgeVertexPairIndex.x],
                    _vertexOffsets[edgeVertexPairIndex.y]);
            }
     
            _cubeTrianglesIndices = new NativeArray<CubeTrianglesIndices>(256, Allocator.Persistent);
            var triangulationInt = new Dictionary<byte, List<int>>();
            for (var i = 0; i < LookupTables.Triangulation.Length; i++)
            {
                var code = (byte)(i / 16);
                var edge = LookupTables.Triangulation[i];
                if (edge == 255)
                {
                    continue;
                }

                if (!triangulationInt.TryGetValue(code, out var list))
                {
                    triangulationInt[code] = list = new List<int>();
                }

                list.Add(edge);
            }
            _trianglesPerCube = new NativeArray<int>(256, Allocator.Persistent);
            _trianglesPerCube[0] = 0;
            _trianglesPerCube[255] = 0;
            for (byte i = 1; i < _trianglesPerCube.Length - 1; i++)
            {
                _trianglesPerCube[i] = triangulationInt[i].Count/3;
            }

            var nativeList = new NativeList<Triangle>(Allocator.Temp);

            var startIndex = (short)0;
            for (var code = 0; code <= 255; code++)
            {
                if (!triangulationInt.TryGetValue((byte)code, out var list))
                {
                    continue;
                }

                var count = (byte)0;
                for (var i = 0; i < list.Count; i += 3)
                {
                    var trianlge = new Triangle
                    {
                        Vertex0 = (byte)list[i],
                        Vertex1 = (byte)list[i + 2],
                        Vertex2 = (byte)list[i + 1]
                    };
                    nativeList.Add(trianlge);
                    count++;
                }
                var endIndex = (short)(startIndex + count);
                _cubeTrianglesIndices[code] = new CubeTrianglesIndices
                {
                    StartIndex = startIndex,
                    EndIndex = endIndex
                };
                startIndex = endIndex;
            }
            _triangulationData = new NativeArray<Triangle>(nativeList.AsArray(), Allocator.Persistent);
            nativeList.Dispose();
        }
    }
}