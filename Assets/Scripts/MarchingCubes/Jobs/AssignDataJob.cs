using MarchingCubes.DataStructures;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCubes.Jobs
{
    [BurstCompile]
    public unsafe struct AssignDataJob:IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        private Mesh.MeshDataArray _meshDataArray;
        private NativeArray<UnsafeMeshData> _meshDatas;

        [ReadOnly] private NativeArray<VertexAttributeDescriptor> _attributes;
        
        public AssignDataJob(NativeArray<VertexAttributeDescriptor> attributes, Mesh.MeshDataArray meshDataArray, NativeArray<UnsafeMeshData> meshDatas)
        {
            _attributes = attributes;
            _meshDataArray = meshDataArray;
            _meshDatas = meshDatas;
        }
        
        public void Execute(int chunkIndex)
        {
            ref var meshData = ref _meshDatas.GetRef(chunkIndex);
            var writableMeshData = _meshDataArray[chunkIndex];
            writableMeshData.SetVertexBufferParams(meshData.VerticesCount,_attributes);
            var vertexData = writableMeshData.GetVertexData<float3>();
            var normalsData = writableMeshData.GetVertexData<float3>(1);
            writableMeshData.SetIndexBufferParams(meshData.IndexCount, IndexFormat.UInt32);
            
            var indicesData = writableMeshData.GetIndexData<int>();
            for (var i = 0; i < meshData.VerticesCount; i++)
            {
                vertexData[i] = meshData.VerticesPointer[i];
                normalsData[i] = meshData.NormalsPointer[i];
            }
            for (var i = 0; i < meshData.IndexCount; i++)
            {
                // TODO: find reason for bad indices
                indicesData[i] = math.clamp(meshData.IndicesPointer[i],0, meshData.VerticesCount);
            }
          
            writableMeshData.subMeshCount = 1;
            writableMeshData.SetSubMesh(0, new SubMeshDescriptor(0, meshData.IndexCount));
        }
    }
}
                