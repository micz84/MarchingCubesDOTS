using System.Runtime.CompilerServices;
using MarchingCubes.DataStructures;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MarchingCubes.Jobs
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public struct UpdateCubesJob : IJob
    {
        [ReadOnly] public int3 ChunkWorldPosition;
        [ReadOnly] public int3 CubesCounts;
        [ReadOnly] public int CubesPerUnit;
        [ReadOnly] public SimpleSmoothTerrain Terrain;
        [ReadOnly] public NativeArray<float3x2> EdgePoints;
        [ReadOnly] public NativeArray<int2> EdgeVertexIndices;
        [ReadOnly] public NativeArray<int> TrianglesPerCube;
        [ReadOnly] public NativeArray<int3> Offsets;

        public NativeHashMap<int, float3> EdgeVertexData;
        public NativeArray<CubeData> CubeCodes;
        public NativeReference<int> VertexCount;
        public NativeReference<int> TrianglesCount;
        public NativeHashMap<int3,int> VertexIndexInMesh;

        public void Execute()
        {
            var cubesInLayer = CubesCounts.x * CubesCounts.y;
            var verticesCountX = CubesCounts.x + 1;
            var verticesCountY = CubesCounts.y + 1;
            var verticesCount = verticesCountX * verticesCountY;
            var cubeWidth = (float)1 / CubesPerUnit;
            var firstPlaneFactors = new NativeArray<float>(verticesCount, Allocator.Temp);
            var secondPlaneFactors = new NativeArray<float>(verticesCount, Allocator.Temp);
            var z = 0;
            var currentVertexIndex = 0;
            var totalTriangles = 0;
            // calculate a first layer
            CalculateLayerFactors(cubeWidth, verticesCountX, verticesCountY, z, firstPlaneFactors);
            for (z = 1; z < CubesCounts.z + 1; z++)
            {
                // calculate a second layer
                CalculateLayerFactors(cubeWidth, verticesCountX, verticesCountY, z, secondPlaneFactors);
                // update cubes data
                var cubeZ = z - 1;
                var verticalCubesOffset = cubesInLayer * cubeZ;
                var posZ = cubeZ * cubeWidth;
                for (var y = 0; y < CubesCounts.y; y++)
                {
                    var factorIndexOffset = verticesCountX * y;
                    var cubesOffset = y * CubesCounts.x + verticalCubesOffset;
                    var posY = y * cubeWidth;
                    for (var x = 0; x < CubesCounts.x; x++)
                    {
                        var cubeIndex = x + cubesOffset;
                        var factors = CalculateVertexFactors(x, factorIndexOffset, verticesCountX, firstPlaneFactors,
                            secondPlaneFactors);
                        currentVertexIndex = UpdateEdgeVertexData(new(x, y, cubeZ), EdgeVertexData, factors,
                            currentVertexIndex, new float3(x * cubeWidth, posY, posZ), out var edgeVertexIndex);
                        var code = GenerateCubeData(factors, edgeVertexIndex);
                        CubeCodes[cubeIndex] = code;
                        totalTriangles += TrianglesPerCube[code.Code];
                    }
                }
                // swap layers
                (firstPlaneFactors, secondPlaneFactors) = (secondPlaneFactors, firstPlaneFactors);
            }

            VertexCount.Value = EdgeVertexData.Count;
            TrianglesCount.Value = totalTriangles;
            firstPlaneFactors.Dispose();
            secondPlaneFactors.Dispose();

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static VertexFactors CalculateVertexFactors(int x, int factorIndexOffset, int verticesCountX,
            NativeArray<float> firstPlaneFactors, NativeArray<float> secondPlaneFactors)
        {
            var factors = new VertexFactors();
            var factorIndex0 = x + factorIndexOffset;
            var factorIndex1 = factorIndex0 + 1;
            var factorIndex3 = factorIndex0 + verticesCountX;
            var factorIndex2 = factorIndex3 + 1;
            factors[0] = firstPlaneFactors[factorIndex0];
            factors[1] = firstPlaneFactors[factorIndex1];
            factors[2] = firstPlaneFactors[factorIndex2];
            factors[3] = firstPlaneFactors[factorIndex3];
            factors[4] = secondPlaneFactors[factorIndex0];
            factors[5] = secondPlaneFactors[factorIndex1];
            factors[6] = secondPlaneFactors[factorIndex2];
            factors[7] = secondPlaneFactors[factorIndex3];
            return factors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateLayerFactors(float offset, int verticesCountX, int verticesCountY, int z,
            NativeArray<float> layerFactors)
        {
            var startOffset = -offset / 2;
            for (var y = 0; y < verticesCountY; y++)
            {
                for (var x = 0; x < verticesCountX; x++)
                {
                    var index = x + y * verticesCountX;
                    var calculateVertexPosition = new float3(x * offset + startOffset,
                                                      y * offset + startOffset,
                                                      z * offset + startOffset)
                                                  + ChunkWorldPosition;
                    layerFactors[index] = Terrain.GetFactorForPosition(calculateVertexPosition);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int UpdateEdgeVertexData(int3 cubeIndex, NativeHashMap<int, float3> edgeVertexMap,
            VertexFactors vertexFactors, int currentIndex, float3 cubePosition, out EdgeVertexIndex edgeVertexIndex)
        {
            edgeVertexIndex = new EdgeVertexIndex();
            cubeIndex *= 2;
            for (var edgeIndex = 0; edgeIndex < EdgeVertexIndices.Length; edgeIndex++)
            {
                var edgeSpaceIndex = cubeIndex + Offsets[edgeIndex];
                if (VertexIndexInMesh.ContainsKey(edgeSpaceIndex))
                {
                    edgeVertexIndex[edgeIndex] = VertexIndexInMesh[edgeSpaceIndex];
                }
                else
                {
                    var edgeVertexPairIndex = EdgeVertexIndices[edgeIndex];
                    var factor1 = vertexFactors[edgeVertexPairIndex.x];
                    var factor2 = vertexFactors[edgeVertexPairIndex.y];
                    if ((factor1 > 0 && factor2 > 0) || (factor1 < 0 && factor2 < 0))
                    {
                        edgeVertexIndex[edgeIndex] = -1;
                        continue;
                    }
                    var vertexPositionFactor = math.unlerp(factor1, factor2, 0);
                    var edgePoints = EdgePoints[edgeIndex];
                    var vertexLocalPosition = math.lerp(edgePoints.c0, edgePoints.c1, vertexPositionFactor);
                    var vertexInMeshPosition = vertexLocalPosition + cubePosition;
                    edgeVertexMap[currentIndex] = vertexInMeshPosition;
                    edgeVertexIndex[edgeIndex] = currentIndex;
                    VertexIndexInMesh.Add(edgeSpaceIndex, currentIndex);
                    currentIndex++;
                }

            }

            return currentIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CubeData GenerateCubeData(VertexFactors vertexFactors, EdgeVertexIndex edgeVertexIndex)
        {
            var cubeData = new CubeData();
            byte cubeCode = 0;
            for (var i = 0; i < 8; i++)
            {
                var voxelState = vertexFactors[i] > 0;
                cubeCode = (byte)(voxelState ? cubeCode | (1 << i) : cubeCode);

            }

            cubeData.EdgeVertexIndex = edgeVertexIndex;
            cubeData.Code = cubeCode;
            return cubeData;
        }
    }
}