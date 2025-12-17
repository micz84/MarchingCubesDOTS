using System;
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
        [SerializeField] private bool _generateOnStart = false;
        [SerializeField] private int3 _terrainSize = new(8, 8, 8);
        [SerializeField] private int3 _maxChunkSize = new(4, 4, 4);
        [SerializeField] [Range(1,20)] private byte _cubesPerUnit = 1;
        [SerializeField] private MeshFilter _prefab;
        [SerializeField] private float _noiseScale = 1f;
        [SerializeField] private bool _generateInUpdate = true;
        
        private int3 _chunkCounts;
        private NativeArray<TerrainChunk> _terrainChunks;
        private readonly List<Mesh> _meshes = new();
        //Temp arrays
        private NativeList<JobHandle> _jobHandles;
        private NativeArray<MeshData> _meshDatas;
        private HelperArrays _helperArrays;
        private NativeArray<VertexAttributeDescriptor> _vertexAttributes;

        public int3 TerrainSize => _terrainSize;
        public SimpleSmoothTerrain Terrain { get; private set; }
        public List<MeshFilter> MeshFilters { get; } = new();
        public List<MeshCollider> MeshColliders { get; } = new();
        public event System.Action GenerationStarted;
        public event System.Action GenerationFinished;

        
        
        public void RegenerateTerrain(bool withCollider)
        {
            var verticalStride = _chunkCounts.x * _chunkCounts.z;
            CreateMeshFilters(_chunkCounts, verticalStride);
            GenerateTerrain(_chunkCounts, verticalStride, withCollider);
            
        }

        private void Awake()
        {
            Terrain = new SimpleSmoothTerrain(_terrainSize, _noiseScale);
            _chunkCounts = new int3((int)math.ceil((float)_terrainSize.x / _maxChunkSize.x),
                (int)math.ceil((float)_terrainSize.y / _maxChunkSize.y),
                (int)math.ceil((float)_terrainSize.z / _maxChunkSize.z));
            
            _vertexAttributes = new NativeArray<VertexAttributeDescriptor>(new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1)
            }, Allocator.Persistent);
            _terrainChunks = new NativeArray<TerrainChunk>(_chunkCounts.x * _chunkCounts.y * _chunkCounts.z,
                Allocator.Persistent);
            _meshDatas = new NativeArray<MeshData>(_terrainChunks.Length, Allocator.Persistent);
            _jobHandles = new NativeList<JobHandle>(Allocator.Persistent);
            _helperArrays = new HelperArrays(_cubesPerUnit);
           
        }

        private void Start()
        {
            if (_generateOnStart)
            {
                RegenerateTerrain(false);
            }
        }

        private void Update()
        {
            if (_generateInUpdate)
            {
                Terrain.UpdateScale(_noiseScale);
                var chunksDimensions = new int3((int)math.ceil((float)_terrainSize.x / _maxChunkSize.x),
                    (int)math.ceil((float)_terrainSize.y / _maxChunkSize.y),
                    (int)math.ceil((float)_terrainSize.z / _maxChunkSize.z));
                var verticalStride = chunksDimensions.x * chunksDimensions.z;
                GenerateTerrain(chunksDimensions, verticalStride, true);
            }
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

        private void UpdateMeshes(bool withCollider)
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
                MeshFilters[chunkIndex].sharedMesh.RecalculateNormals();
                MeshFilters[chunkIndex].sharedMesh.RecalculateBounds();
                if(withCollider)
                    MeshColliders[chunkIndex].sharedMesh = _meshes[chunkIndex];
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

                        _terrainChunks[index] = new TerrainChunk(Terrain, chunkPosition, chunkSize, _cubesPerUnit, _helperArrays.EdgePoints, _helperArrays.EdgeVertexPairIndices, _helperArrays.TrianglesPerCube);
                        _meshDatas[index] = new(_helperArrays.TriangulationData, _helperArrays.CubeTrianglesIndices);
                        var meshFilter = Instantiate(_prefab);
                        meshFilter.transform.position = new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z);
                        if (meshFilter.mesh == null)
                        {
                            meshFilter.mesh = new Mesh();
                        }

                        MeshFilters.Add(meshFilter);
                        var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                        meshCollider.sharedMesh = meshFilter.mesh;
                        meshCollider.convex = false;
                        MeshColliders.Add(meshCollider);
                    }
                }
            }
        }

        private void GenerateTerrain(int3 chunksDimensions, int verticalStride, bool withCollider)
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
            UpdateMeshes(withCollider);
            GenerationFinished?.Invoke();
        }
    }
}