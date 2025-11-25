using System.Diagnostics;
using System.Globalization;
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
            
            for (var index = 0; index < meshFilters.Count; index++)
            {
                var mesh = meshFilters[index].mesh;
                if (mesh != null && mesh.vertexCount != 0)
                {
                    totalVertices += mesh.vertexCount;
                    filters++;
                }
            }

            if (filters > 0)
            {
                var nfi = new NumberFormatInfo();
                nfi.NumberGroupSeparator = " ";
                Debug.Log(
                    $"Total meshes: {filters} Total vertices: {totalVertices.ToString("N2",nfi)} Average Vertices per chunk: {(totalVertices / filters).ToString("N2",nfi)} Time: {_stopwatch.Elapsed.TotalMilliseconds:F5} ms Average time: {_stopwatch.Elapsed.TotalMilliseconds / filters:F5}");
            }
            
        }

        
        
        
    }
}