using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace MarchingCubes.Tests
{
    [RequireComponent(typeof(TerrainTester))]
    public class TerrainOffsetMover:MonoBehaviour
    {
        [SerializeField] private CharacterSpawnController _characterSpawnController;
        [SerializeField] private NoiseProvider _noiseProvider;
        private bool _characterSpawned = false;
        private TerrainTester _terrainTester;

        private void Awake()
        {
            _terrainTester = GetComponent<TerrainTester>();
        }

        private void Update()
        {
            if (!_characterSpawned)
            {
                bool offsetMoved = false;
                var offset = float3.zero;
                if (Input.GetKey(KeyCode.A))
                {
                    offset.x += Time.deltaTime;
                    offsetMoved = true;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    offset.x -= Time.deltaTime;
                    offsetMoved = true;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    offset.z += Time.deltaTime;
                    offsetMoved = true;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    offset.z -= Time.deltaTime;
                    offsetMoved = true;
                }

                if (offsetMoved)
                {
                    _terrainTester.Terrain.MoveOffset(offset);
                    _terrainTester.RegenerateTerrain(false);
                }
            }

            if (Input.GetKey(KeyCode.Return))
            {
                _characterSpawned = true;
                _terrainTester.RegenerateTerrain(true);
                _characterSpawnController.EnableCharacter();
            }
        }
        
    }
}