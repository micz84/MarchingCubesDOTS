using System.Collections.Generic;
using MarchingCubes.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.Tests
{
    public class CubesGenerator:MonoBehaviour
    {
        [SerializeField]
        CubeTester _cubeTester;
        [SerializeField]
        private int3 _cubesCount =new int3(10, 10, 10);
        [SerializeField]
        private int _cubeSize = 1;
        [SerializeField] private NoiseProvider _noiseProvider;
        [SerializeField] [Range(0,1)] private float  _surface = 0.5f;
        [SerializeField] bool _surfaceMarkersVisible = true;
        [SerializeField] bool _cubeVertexMarkersVisible = true;
        private HelperArrays _helperArrays;
        private readonly List<CubeTester> _cubeTesters = new ();

        public void Start()
        {
            var t = transform;
            _helperArrays = new HelperArrays(_cubeSize);
            for (var x = 0; x < _cubesCount.x; x++)
            {
                for (var y = 0; y < _cubesCount.y; y++)
                {
                    for (int z = 0; z < _cubesCount.z; z++)
                    {
                        var cubeTester = Instantiate(_cubeTester, t);
                        var position = new Vector3(x * _cubeSize, y * _cubeSize, z * _cubeSize);
                        cubeTester.Initialize(_noiseProvider,  _helperArrays, position, _surface);
                        _cubeTesters.Add(cubeTester);
                    }
                }
            }

            UpdateMarkersVisibility();
        }

        [ContextMenu("ToggleSurfaceMarkers")]
        private void ToggleSurfaceMarkers()
        {
            _surfaceMarkersVisible = !_surfaceMarkersVisible;
            UpdateMarkersVisibility();
        }

        

        [ContextMenu("ToggleCubeVertexMarkers")]
        private void ToggleCubeVertexMarkers()
        {
            _cubeVertexMarkersVisible = !_cubeVertexMarkersVisible;
            UpdateMarkersVisibility();
        }
        
        private void UpdateMarkersVisibility()
        {
            foreach (var cubeTester in _cubeTesters)
            {
                cubeTester.UpdateMarkersVisibility(_surfaceMarkersVisible, _cubeVertexMarkersVisible);
            }
        }
        
        private void OnDestroy()
        {
            _helperArrays?.Dispose();
        }
    }
}