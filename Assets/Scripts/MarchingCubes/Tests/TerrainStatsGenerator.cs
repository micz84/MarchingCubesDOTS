using System.Diagnostics;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MarchingCubes.Tests
{
    [RequireComponent(typeof(TerrainTester))]
    public class TerrainStatsGenerator : MonoBehaviour
    {
        [SerializeField] private bool _generateStats = true;
        [SerializeField]
        private TextMeshProUGUI _stats;
        private TerrainTester _terrainTester;
        private Stopwatch _stopwatch;
        private StringBuilder _stringBuilder = new StringBuilder();

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
            if (!_generateStats)
                return;
            var totalVertices = 0;
            var totalTriangles = 0;
            var filters = 0;
            var meshFilters = _terrainTester.MeshFilters;

            for (var index = 0; index < meshFilters.Count; index++)
            {
                var mesh = meshFilters[index].mesh;
                if (mesh != null && mesh.vertexCount != 0)
                {
                    totalVertices += mesh.vertexCount;
                    totalTriangles += mesh.triangles.Length / 3;
                    filters++;
                }
            }

            if (filters > 0)
            {
                var nfi = new NumberFormatInfo();
                
                nfi.NumberGroupSeparator = " ";
                _stringBuilder.AppendLine(
                    $"TerrainSize\n W:{_terrainTester.TerrainSize.x} H:{_terrainTester.TerrainSize.y} D:{_terrainTester.TerrainSize.z}");
                _stringBuilder.AppendLine($"Total meshes: {filters}");
                _stringBuilder.AppendLine($"Total vertices: {totalVertices.ToString("N0", nfi)}");
                _stringBuilder.AppendLine($"Total Triangles: {totalTriangles.ToString("N0", nfi)}");
                _stringBuilder.AppendLine($"Average Vertices per mesh: {(totalVertices / filters).ToString("N0", nfi)}");
                _stringBuilder.AppendLine($"Average Triangles per mesh: {(totalTriangles / filters).ToString("N0", nfi)}");
                _stringBuilder.AppendLine($"Total Time: {_stopwatch.Elapsed.TotalMilliseconds:F5} ms");
                _stringBuilder.AppendLine($"Average time: {_stopwatch.Elapsed.TotalMilliseconds / filters:F5}");
                var stats = _stringBuilder.ToString();
                _stringBuilder.Clear();
                Debug.Log(stats);
                _stats.text = stats;
            }
        }
    }
}