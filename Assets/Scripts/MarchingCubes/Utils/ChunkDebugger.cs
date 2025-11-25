using UnityEngine;

namespace MarchingCubes
{
    internal class ChunkDebugger:MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                Debug.LogError("MeshFilter component is missing on the GameObject.");
            }
        }
        private void OnDrawGizmosSelected()
        {
            var bounds = _meshFilter.sharedMesh.bounds;
           Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size); 
        }
    }
}