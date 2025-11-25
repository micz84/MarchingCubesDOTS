using System.Collections.Generic;
using MarchingCubes.DataStructures;
using MarchingCubes.Jobs;
using MarchingCubes.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCubes.Tests
{
    internal class TerrainTester : MonoBehaviour
    {
        [SerializeField] private int3 _terrainSize = new(8, 8, 8);
        [SerializeField] private int3 _maxChunkSize = new(4, 4, 4);
        [SerializeField] private byte _cubesPerUnit = 1;
        [SerializeField] private MeshFilter _prefab;
        [SerializeField] private float _terrainScale = 1f;
        
        private NativeArray<TerrainChunk> _terrainChunks;
        private readonly List<Mesh> _meshes = new();
        //Temp arrays
        private NativeList<JobHandle> _jobHandles;
        private NativeArray<MeshData> _meshDatas;
        private HelperArrays _helperArrays;
        private NativeArray<VertexAttributeDescriptor> _vertexAttributes;
        
        public SimpleSmoothTerrain Terrain { get; private set; }
        public List<MeshFilter> MeshFilters { get; } = new();
        public int3 ChunkCounts { get; private set; }
        public event System.Action GenerationStarted;
        public event System.Action GenerationFinished;

        public void Awake()
        {
            Terrain = new SimpleSmoothTerrain(_terrainScale);
            ChunkCounts = new int3((int)math.ceil((float)_terrainSize.x / _maxChunkSize.x),
                (int)math.ceil((float)_terrainSize.y / _maxChunkSize.y),
                (int)math.ceil((float)_terrainSize.z / _maxChunkSize.z));
            
            _vertexAttributes = new NativeArray<VertexAttributeDescriptor>(new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1)
            }, Allocator.Persistent);
            _terrainChunks = new NativeArray<TerrainChunk>(ChunkCounts.x * ChunkCounts.y * ChunkCounts.z,
                Allocator.Persistent);
            _meshDatas = new NativeArray<MeshData>(_terrainChunks.Length, Allocator.Persistent);
            _jobHandles = new NativeList<JobHandle>(Allocator.Persistent);
            _helperArrays = new HelperArrays(_cubesPerUnit);
            
            var verticalStride = ChunkCounts.x * ChunkCounts.z;
            CreateMeshFilters(ChunkCounts, verticalStride);
            GenerateTerrain(ChunkCounts, verticalStride);
            
        }

        private void Update()
        {
            Terrain.UpdateScale(_terrainScale);
            var chunksDimensions = new int3((int)math.ceil((float)_terrainSize.x / _maxChunkSize.x),
                (int)math.ceil((float)_terrainSize.y / _maxChunkSize.y),
                (int)math.ceil((float)_terrainSize.z / _maxChunkSize.z));
            var verticalStride = chunksDimensions.x * chunksDimensions.z;
            GenerateTerrain(chunksDimensions, verticalStride);
        }

        private void OnDestroy()
        {
            Terrain.Dispose();
            if (_jobHandles.IsCreated)
                _jobHandles.Clear();
            _helperArrays.Dispose();
            _vertexAttributes.Dispose();
            if (_terrainChunks.IsCreated)
            {
                for (var i = 0; i < _terrainChunks.Length; i++)
                {
                    ref var chunk = ref _terrainChunks.GetRef(i);
                    chunk.Dispose();
                }

                _terrainChunks.Dispose();
            }

            if (_meshDatas.IsCreated)
            {
               _meshDatas.Dispose();
            }
        }

        private void UpdateMeshes()
        {
            var count = _terrainChunks.Length;
            var unsafeMeshData = new NativeArray<UnsafeMeshData>(count, Allocator.TempJob);
            for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
            {
                unsafeMeshData[chunkIndex] = new(_meshDatas[chunkIndex]);
            }

            var meshDataArray = Mesh.AllocateWritableMeshData(count);
            var job = new AssignDataJob(_vertexAttributes, meshDataArray, unsafeMeshData);
            var jobHandle = job.Schedule(count, 1);
            jobHandle.Complete();
            unsafeMeshData.Dispose();
            for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
            {
                _meshes.Add(MeshFilters[chunkIndex].sharedMesh);
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _meshes);
            for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
            {
                MeshFilters[chunkIndex].sharedMesh.bounds = _meshDatas[chunkIndex].Bounds.Value;
            }

            _meshes.Clear();

            for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
            {
                _meshDatas[chunkIndex].DisposeMeshData();
            }
        }

        private void CreateMeshFilters(int3 chunksDimensions, int verticalStride)
        {
            for (var y = 0; y < chunksDimensions.y; y++)
            {
                var verticalOffset = y * verticalStride;
                for (var z = 0; z < chunksDimensions.z; z++)
                {
                    var stride = z * chunksDimensions.x;
                    for (var x = 0; x < chunksDimensions.x; x++)
                    {
                        var index = x + stride + verticalOffset;
                        var chunkPosition = new int3(x * _maxChunkSize.x, y * _maxChunkSize.y, z * _maxChunkSize.z);
                        var chunkSize = new int3(math.min(_maxChunkSize.x, _terrainSize.x - x * _maxChunkSize.x),
                            math.min(_maxChunkSize.y, _terrainSize.y - y * _maxChunkSize.y),
                            math.min(_maxChunkSize.z, _terrainSize.z - z * _maxChunkSize.z));

                        _terrainChunks[index] = new TerrainChunk(Terrain, chunkPosition, chunkSize, _cubesPerUnit);
                        _meshDatas[index] = new(_helperArrays.TriangulationData, _helperArrays.CubeTrianglesIndices,
                            _helperArrays.EdgePoints);
                        var meshFilter = Instantiate(_prefab);
                        meshFilter.transform.position = new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z);
                        if (meshFilter.mesh == null)
                        {
                            meshFilter.mesh = new Mesh();
                        }

                        MeshFilters.Add(meshFilter);
                    }
                }
            }
        }

        private void GenerateTerrain(int3 chunksDimensions, int verticalStride)
        {
            GenerationStarted?.Invoke();
            for (var y = 0; y < chunksDimensions.y; y++)
            {
                var verticalOffset = y * verticalStride;
                for (var z = 0; z < chunksDimensions.z; z++)
                {
                    var stride = z * chunksDimensions.x;
                    for (var x = 0; x < chunksDimensions.x; x++)
                    {
                        var chunkIndex = x + stride + verticalOffset;
                        ref var chunk = ref _terrainChunks.GetRef(chunkIndex);

                        chunk.Update(out var handle);
                        ref var meshData = ref _meshDatas.GetRef(chunkIndex);
                        handle = meshData.ScheduleMeshDataGeneration(ref chunk, handle);
                        _jobHandles.Add(handle);
                    }
                }
            }
            JobHandle.CompleteAll(_jobHandles.AsArray());
            UpdateMeshes();
            GenerationFinished?.Invoke();
        }
    }
}