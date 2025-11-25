using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MarchingCubes.Tests
{
    [RequireComponent(typeof(TerrainTester))]
    public class TerrainStatsGenerator:MonoBehaviour
    {
        [SerializeField]
        private bool _generateStats = true;
        private TerrainTester _terrainTester;
        private Stopwatch _stopwatch;

        private void Awake()
        {
            _stopwatch = new Stopwatch();
            _terrainTester = GetComponent<TerrainTester>();
            _terrainTester.GenerationStarted += OnGenerationStarted;
            _terrainTester.GenerationFinished += OnGenerationFinished;
        }
        
        private void OnDestroy()
        {
            _terrainTester.GenerationStarted -= OnGenerationStarted;
            _terrainTester.GenerationFinished -= OnGenerationFinished;
        }
        
        private void OnGenerationStarted()
        {
            _stopwatch.Restart();
        }

        private void OnGenerationFinished()
        {
            _stopwatch.Stop();
            if(!_generateStats)
                return;
            var totalVertices = 0;
            var filters = 0;
            var meshFilters = _terrainTester.MeshFilters;
            var chunkCounts = _terrainTester.ChunkCounts;
            var verticalStride = chunkCounts.x * chunkCounts.y;
            for (var y = 0; y < chunkCounts.y; y++)
            {
                var verticalOffset = y * verticalStride;
                for (var z = 0; z < chunkCounts.z; z++)
                {
                    var stride = z * chunkCounts.x;
                    for (var x = 0; x < chunkCounts.x; x++)
                    {
                        var index = x + stride + verticalOffset;
                        var mesh = meshFilters[index].mesh;
                        if (mesh != null && mesh.vertexCount != 0)
                        {
                            totalVertices += meshFilters[index].mesh.vertexCount;
                            filters++;
                        }
                    }
                }
            }

            if (filters > 0)
            {
                Debug.Log(
                    $"Total meshes: {filters} Total vertices: {totalVertices} Avg Verts: {totalVertices / filters} Time: {_stopwatch.Elapsed.TotalMilliseconds} ms Avg time: {_stopwatch.Elapsed.TotalMilliseconds / filters}");
            }
            
        }

        
        
        
    }
}