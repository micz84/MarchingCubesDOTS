using Unity.Collections;
using Unity.Mathematics;

namespace MarchingCubes.DataStructures
{
    public struct SimpleSmoothTerrain
    {
        private int3 _terrainSize;
        private NativeReference<float> _scale;
        private NativeReference<float3> _offset;

        public SimpleSmoothTerrain(int3 terrainSize, float scale)
        {
            _terrainSize = terrainSize;
            _scale = new NativeReference<float>(scale, Allocator.Persistent);
            _offset = new NativeReference<float3>(float3.zero, Allocator.Persistent);
        }

        public void UpdateScale(float scale)
        {
            _scale.Value = scale;
        }
        
        public void MoveOffset(float3 offset)
        {
            _offset.Value += offset;
        }

        public void Dispose()
        {
            _scale.Dispose();
            _offset.Dispose();
        }

        public float GetFactorForPosition(float3 position)
        {
            if (position.x <= 1 
                || position.x >= _terrainSize.x - 1
                || position.y <= 1 
                || position.y >= _terrainSize.y - 1
                || position.z <= 1 
                || position.z >= _terrainSize.z - 1)
                return -1;
            return noise.snoise(position/_scale.Value + _offset.Value);
        }
    }
}