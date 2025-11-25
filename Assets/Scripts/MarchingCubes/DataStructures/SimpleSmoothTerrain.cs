using Unity.Collections;
using Unity.Mathematics;

namespace MarchingCubes.DataStructures
{
    public struct SimpleSmoothTerrain
    {
        private NativeReference<float> _scale;
        private NativeReference<float3> _offset;

        public SimpleSmoothTerrain(float scale)
        {
            _scale = new NativeReference<float>(scale, Allocator.Persistent);
            _offset = new NativeReference<float3>(float3.zero, Allocator.Persistent);
        }

        public void UpdateScale(float scale)
        {
            _scale.Value = scale;
        }
        
        public void UpdateOffset(float3 offset)
        {
            _offset.Value = offset;
        }

        public void Dispose()
        {
            _scale.Dispose();
        }

        public float GetFactorForPosition(float3 position)
        {
            return noise.snoise(position/_scale.Value + _offset.Value);
        }
    }
}