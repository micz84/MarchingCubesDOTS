using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.Tests
{
    public class NoiseProvider:MonoBehaviour
    {
        [SerializeField] private float _scale = 10;
        [SerializeField] private float3  _offset = float3.zero;

        public void MoveOffset(float3 offset)
        {
            _offset += offset;
        }
        public float NoiseValue(float3 position)
        {
            return math.unlerp(-1,1, noise.cnoise(position/_scale +  _offset));
        }
    }
}