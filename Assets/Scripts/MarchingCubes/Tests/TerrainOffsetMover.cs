using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace MarchingCubes.Tests
{
    [RequireComponent(typeof(TerrainTester))]
    public class TerrainOffsetMover:MonoBehaviour
    {
        [SerializeField] private float3 _noiseOffset = float3.zero;
        
        private TerrainTester _terrainTester;

        private void Awake()
        {
            _terrainTester = GetComponent<TerrainTester>();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.A))
            {
                _noiseOffset.x += Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.D))
            {
                _noiseOffset.x -= Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.W))
            {
                _noiseOffset.z += Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.S))
            {
                _noiseOffset.z -= Time.deltaTime;
            }
            _terrainTester.Terrain.UpdateOffset(_noiseOffset);
        }
        
    }
}