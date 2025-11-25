using System;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.Tests
{
    [RequireComponent(typeof(TerrainTester))]
    public class TerrainOffsetMover:MonoBehaviour
    {
        [SerializeField] private float3 _terrainOffset = float3.zero;
        
        private TerrainTester _terrainTester;

        private void Awake()
        {
            _terrainTester = GetComponent<TerrainTester>();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.A))
            {
                _terrainOffset.x += Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.D))
            {
                _terrainOffset.x -= Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.W))
            {
                _terrainOffset.z += Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.S))
            {
                _terrainOffset.z -= Time.deltaTime;
            }
            _terrainTester.Terrain.UpdateOffset(_terrainOffset);
        }
        
    }
}