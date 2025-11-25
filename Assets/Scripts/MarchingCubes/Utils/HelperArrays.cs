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
        public NativeArray<float3x2> EdgePoints => _edgesPoints;
        private NativeArray<Triangle> _triangulationData;
        private NativeArray<CubeTrianglesIndices> _cubeTrianglesIndices;
        private NativeArray<float3> _vertexOffsets;
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

            _edgesPoints = new NativeArray<float3x2>(12, Allocator.Persistent);
            _edgesPoints[0] = new(_vertexOffsets[0], _vertexOffsets[1]);
            _edgesPoints[1] = new(_vertexOffsets[1], _vertexOffsets[2]);
            _edgesPoints[2] = new(_vertexOffsets[3], _vertexOffsets[2]);
            _edgesPoints[3] = new(_vertexOffsets[0], _vertexOffsets[3]);
            _edgesPoints[4] = new(_vertexOffsets[4], _vertexOffsets[5]);
            _edgesPoints[5] = new(_vertexOffsets[5], _vertexOffsets[6]);
            _edgesPoints[6] = new(_vertexOffsets[6], _vertexOffsets[7]);
            _edgesPoints[7] = new(_vertexOffsets[4], _vertexOffsets[7]);
            _edgesPoints[8] = new(_vertexOffsets[0], _vertexOffsets[4]);
            _edgesPoints[9] = new(_vertexOffsets[1], _vertexOffsets[5]);
            _edgesPoints[10] = new(_vertexOffsets[2], _vertexOffsets[6]);
            _edgesPoints[11] = new(_vertexOffsets[3], _vertexOffsets[7]);
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