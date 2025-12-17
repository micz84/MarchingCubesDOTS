using System;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MarchingCubes.Tests
{
    public class CharacterSpawnController : MonoBehaviour
    {
        [SerializeField] private GameObject _character;
        [SerializeField] private GameObject _lookAtTarget;
        [SerializeField] private CinemachineCamera _freeLockCamera;
        [SerializeField] private CinemachineCamera _thirdPersonCamera;
        [SerializeField] private TerrainTester _terrainTester;
        [SerializeField] private GameObject _UI;

        private void Awake()
        {
            var terrainSize = _terrainTester.TerrainSize;
            var cameraPosition = new Vector3((float)terrainSize.x / 2, terrainSize.y + (float)terrainSize.x / 2, (float)terrainSize.z / 2);
            _freeLockCamera.transform.position = cameraPosition;
            _lookAtTarget.transform.position = cameraPosition + Vector3.down * 100;
        }

        public void EnableCharacter()
        {
            _character.SetActive(true);
            _freeLockCamera.gameObject.SetActive(false);
            _freeLockCamera.Priority = 0;
            _thirdPersonCamera.Priority = 1;
            var terrainSize = _terrainTester.TerrainSize;
            _character.transform.position = new Vector3((float)terrainSize.x / 2, terrainSize.y + 2, (float)terrainSize.z / 2);
            _UI.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
               Application.Quit();
            }
        }
    }
}