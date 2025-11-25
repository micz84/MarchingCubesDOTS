using System.Collections.Generic;
using MarchingCubes.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes.Tests
{
    public class CubeTester: MonoBehaviour
    {
        private static readonly int MainColor = Shader.PropertyToID("_BaseColor");
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private Transform[] _vertices;
        [SerializeField] private int2[] _edgePointIndices;
        [SerializeField] private MeshRenderer[] _surfaceMarkers;

        private float _surface;
        private float[] _edgeFactor = new float[12];
        private float[] _vertexValue;
        private float3[] _vertexPosition;
        private MeshRenderer[] _verticesMeshRenderers;
        private HelperArrays _helperArrays;
        private List<Vector3> _meshVertices = new();
        private List<Vector3> _meshNormals = new();
        private List<int> _meshIndices = new();
        private Mesh _mesh;
        private NoiseProvider _noiseProvider;
        private bool _initialized;
        private bool _surfaceMarkersVisible;
        private bool _cubeVertexMarkersVisible;

        public void Initialize(NoiseProvider noiseProvider, HelperArrays helperArrays, Vector3 position, float surface)
        {
            transform.localPosition = position;
            _noiseProvider = noiseProvider;
            _helperArrays = helperArrays;
            _surface = surface;
            _vertexValue = new float[_vertices.Length];
            _vertexPosition = new float3[_vertices.Length];
            _verticesMeshRenderers = new MeshRenderer[_vertices.Length];
            for (var i = 0; i < _vertices.Length; i++)
            {
                var vertexMarkerRenderer = _vertices[i].GetComponent<MeshRenderer>();
                _verticesMeshRenderers[i] =  vertexMarkerRenderer;
                vertexMarkerRenderer.transform.localPosition = _helperArrays.VertexOffsets[i];

            }

            _mesh = new Mesh();
            _meshFilter.sharedMesh = _mesh;
            _initialized = true;
        }

        private void Update()
        {
            if(!_initialized)
                return;
            byte cubeCode = 0;
            for (var i = 0; i < _vertices.Length; i++)
            { 
                _vertexPosition[i] = _vertices[i].position;
                _vertexValue[i] = _noiseProvider.NoiseValue(_vertices[i].position);
                cubeCode |= (byte) (_vertexValue[i] > _surface ? 1 << i : 0);
            }
            for (var i = 0; i < _edgePointIndices.Length; i++)
            {
                var point1Index = _edgePointIndices[i].x;
                var point2Index = _edgePointIndices[i].y;
                var point1NoiseValue = _vertexValue[point1Index];
                var point2NoiseValue = _vertexValue[point2Index];

                var factor = math.unlerp(point1NoiseValue, point2NoiseValue, _surface);
                _edgeFactor[i] = factor;
                if (_surfaceMarkersVisible 
                    && (point1NoiseValue >= _surface && point2NoiseValue < _surface 
                        || point1NoiseValue < _surface && point2NoiseValue >= _surface))
                {
                    _surfaceMarkers[i].enabled = true;
                    _surfaceMarkers[i].transform.position = math.lerp(_vertexPosition[point1Index],  _vertexPosition[point2Index], factor);
                }
                else
                {
                    _surfaceMarkers[i].enabled = false;
                }

                if (_cubeVertexMarkersVisible)
                {
                    var point1Color = new Color(point1NoiseValue, point1NoiseValue, point1NoiseValue, 1);
                    _verticesMeshRenderers[point1Index].sharedMaterial.SetColor(MainColor, point1Color);
                    var point2Color = new Color(point2NoiseValue, point2NoiseValue, point2NoiseValue, 1);
                    _verticesMeshRenderers[point2Index].sharedMaterial.SetColor(MainColor, point2Color);
                }
            }

            var trianglesIndex = _helperArrays.CubeTrianglesIndices[cubeCode];
            var trianglesCount = trianglesIndex.EndIndex -  trianglesIndex.StartIndex;
            if (trianglesCount > 0)
            {
                var index = 0;
                for (var triangleIndex = trianglesIndex.StartIndex;
                     triangleIndex < trianglesIndex.EndIndex;
                     triangleIndex++)
                {
                    var triangle = _helperArrays.TriangulationData[triangleIndex];
                    var p1 = GetVertexPosition(triangle.Vertex0, _edgeFactor[triangle.Vertex0]);
                    var p2 = GetVertexPosition(triangle.Vertex1, _edgeFactor[triangle.Vertex1]);
                    var p3 = GetVertexPosition(triangle.Vertex2, _edgeFactor[triangle.Vertex2]);
                    _meshVertices.Add(p1);
                    _meshVertices.Add(p2);
                    _meshVertices.Add(p3);
                    var normal = CalculateNormal(p1, p2, p3);
                    _meshNormals.Add(normal);
                    _meshNormals.Add(normal);
                    _meshNormals.Add(normal);
                    _meshIndices.Add(index);
                    _meshIndices.Add(index + 1);
                    _meshIndices.Add(index + 2);
                    index += 3;
                }
                _mesh.Clear();
                _mesh.SetVertices(_meshVertices);
                _mesh.SetNormals(_meshNormals);
                _mesh.SetIndices(_meshIndices,  MeshTopology.Triangles, 0);
                _meshRenderer.enabled = true;
                _meshVertices.Clear();
                _meshNormals.Clear();
                _meshIndices.Clear();
            }
            else
            {
                _meshRenderer.enabled = false;
            }
        }

        private Vector3 CalculateNormal(float3 p1, float3 p2, float3 p3)
        {
            var a = p2 - p1;
            var b = p3 - p1;
            var nX = a.y * b.z - a.z * b.y;
            var nY = a.z * b.x - a.x * b.z;
            var nZ = a.x * b.y - a.y * b.x;
            return math.normalize(new float3(nX, nY, nZ));
        }

        

        private float3 GetVertexPosition(byte index, float factor)
        {
            var points = _helperArrays.EdgePoints[index];
            return math.lerp(points.c0, points.c1, factor);
        }

        public void UpdateMarkersVisibility(bool surfaceMarkersVisible, bool cubeVertexMarkersVisible)
        {
            _surfaceMarkersVisible = surfaceMarkersVisible;
            _cubeVertexMarkersVisible = cubeVertexMarkersVisible;
            foreach (var verticesMeshRenderer in _verticesMeshRenderers)
            {
                verticesMeshRenderer.enabled = _cubeVertexMarkersVisible;
            }
        }
    }
}