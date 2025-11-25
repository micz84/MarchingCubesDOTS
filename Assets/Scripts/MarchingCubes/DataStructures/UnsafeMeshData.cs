using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MarchingCubes.DataStructures
{
    public unsafe struct UnsafeMeshData
    {
        public float3* VerticesPointer;
        public float3* NormalsPointer;
        public int* IndicesPointer;
        public int VerticesCount;
        public int IndexCount;

        public UnsafeMeshData(MeshData meshData)
        {
            VerticesPointer = meshData.Vertices.GetUnsafeReadOnlyPtr();
            NormalsPointer = meshData.Normals.GetUnsafeReadOnlyPtr();
            IndicesPointer = meshData.Indices.GetUnsafeReadOnlyPtr();
            VerticesCount = meshData.Vertices.Length;
            IndexCount = meshData.Indices.Length;
        }
    }
}