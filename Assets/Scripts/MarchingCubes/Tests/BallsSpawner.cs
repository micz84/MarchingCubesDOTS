using UnityEngine;

namespace MarchingCubes.Tests
{
    public class BallsSpawner:MonoBehaviour
    {
        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private GameObject _ballPrefab;
        [SerializeField]
        private int _buttonIndex = 0;

        private void Update()
        {
            if (Input.GetMouseButtonDown(_buttonIndex))
            {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    var position = hitInfo.normal * 0.5f + hitInfo.point;
                    GameObject.Instantiate(_ballPrefab, position, Quaternion.identity);
                }
            }
        }
    }
}