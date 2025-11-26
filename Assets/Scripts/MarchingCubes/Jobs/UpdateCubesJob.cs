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
        
        public NativeArray<CubeData> CubeCodes;

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
            // calculate a first layer
            CalculateLayerFactors(cubeWidth, verticesCountX, verticesCountY, z, firstPlaneFactors);
            for (z = 1; z < CubesCounts.z + 1; z++)
            {
                // calculate a second layer
                CalculateLayerFactors(cubeWidth, verticesCountX, verticesCountY, z, secondPlaneFactors);
                // update cubes data
                var verticalCubesOffset = cubesInLayer * (z - 1);
                for (var y = 0; y < CubesCounts.y; y++)
                {
                    var factorIndexOffset = verticesCountX * y;
                    var cubesOffset = y * CubesCounts.x + verticalCubesOffset;
                    for (var x = 0; x < CubesCounts.x; x++)
                    {
                        var cubeIndex = x + cubesOffset;
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
                        CubeCodes[cubeIndex] = GenerateCubeData(factors);
                    }
                }

                // swap layers
                (firstPlaneFactors, secondPlaneFactors) = (secondPlaneFactors, firstPlaneFactors);
            }

            firstPlaneFactors.Dispose();
            secondPlaneFactors.Dispose();
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
        private static CubeData GenerateCubeData(VertexFactors vertexFactors)
        {
            var cubeData = new CubeData();
            EdgeFactors edgeFactors = new EdgeFactors();
            edgeFactors[0] = math.unlerp(vertexFactors[0], vertexFactors[1], 0);
            edgeFactors[1] = math.unlerp(vertexFactors[1], vertexFactors[2], 0);
            edgeFactors[2] = math.unlerp(vertexFactors[3], vertexFactors[2], 0);
            edgeFactors[3] = math.unlerp(vertexFactors[0], vertexFactors[3], 0);
            edgeFactors[4] = math.unlerp(vertexFactors[4], vertexFactors[5], 0);
            edgeFactors[5] = math.unlerp(vertexFactors[5], vertexFactors[6], 0);
            edgeFactors[6] = math.unlerp(vertexFactors[6], vertexFactors[7], 0);
            edgeFactors[7] = math.unlerp(vertexFactors[4], vertexFactors[7], 0);
            edgeFactors[8] = math.unlerp(vertexFactors[0], vertexFactors[4], 0);
            edgeFactors[9] = math.unlerp(vertexFactors[1], vertexFactors[5], 0);
            edgeFactors[10] = math.unlerp(vertexFactors[2], vertexFactors[6], 0);
            edgeFactors[11] = math.unlerp(vertexFactors[3], vertexFactors[7], 0);
            byte cubeCode = 0;
            for (var i = 0; i < 8; i++)
            {
                var voxelState = vertexFactors[i] > 0;
                cubeCode = (byte)(voxelState ? cubeCode | (1 << i) : cubeCode);
                
            }

            cubeData.EdgeFactors = edgeFactors;
            cubeData.Code = cubeCode;
            return cubeData;
        }
    }
}