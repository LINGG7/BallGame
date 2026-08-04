using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace BallGame.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SplineMesh2DGenerator : MonoBehaviour
    {
        [SerializeField] private int splineIndex;
        [SerializeField] private float width = 0.5f;
        [SerializeField] private float samplesPerUnit = 8f;
        [SerializeField] private int minSamples = 8;
        [SerializeField] private int maxSamples = 256;

        private readonly List<Vector2> centerPoints = new List<Vector2>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<float> distances = new List<float>();

        private SplineContainer splineContainer;
        private MeshFilter meshFilter;
        private Mesh generatedMesh;

        private void OnEnable()
        {
            CacheComponents();
            Spline.Changed += OnSplineChanged;
            SplineContainer.SplineAdded += OnSplineContainerChanged;
            SplineContainer.SplineRemoved += OnSplineContainerChanged;
            SplineContainer.SplineReordered += OnSplineContainerReordered;
            Rebuild();
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
            SplineContainer.SplineAdded -= OnSplineContainerChanged;
            SplineContainer.SplineRemoved -= OnSplineContainerChanged;
            SplineContainer.SplineReordered -= OnSplineContainerReordered;
        }

        private void OnValidate()
        {
            splineIndex = Mathf.Max(0, splineIndex);
            width = Mathf.Max(0.01f, width);
            samplesPerUnit = Mathf.Max(0.1f, samplesPerUnit);
            minSamples = Mathf.Max(2, minSamples);
            maxSamples = Mathf.Max(minSamples, maxSamples);

            Rebuild();
        }

        private void Update()
        {
            if (!Application.isPlaying && transform.hasChanged)
            {
                transform.hasChanged = false;
                Rebuild();
            }
        }

        private void Reset()
        {
            width = 0.5f;
            samplesPerUnit = 8f;
            minSamples = 8;
            maxSamples = 256;
            Rebuild();
        }

        public void Rebuild()
        {
            CacheComponents();

            if (splineContainer == null || meshFilter == null)
            {
                return;
            }

            if (splineContainer.Splines == null || splineIndex >= splineContainer.Splines.Count)
            {
                ClearMesh();
                return;
            }

            BuildCenterPoints();

            if (centerPoints.Count < 2)
            {
                ClearMesh();
                return;
            }

            BuildMeshData();
            ApplyMesh();
        }

        private void CacheComponents()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }

            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }
        }

        private void BuildCenterPoints()
        {
            centerPoints.Clear();
            distances.Clear();

            float length = Mathf.Max(0.01f, splineContainer.CalculateLength(splineIndex));
            int sampleCount = Mathf.Clamp(Mathf.CeilToInt(length * samplesPerUnit), minSamples, maxSamples);

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                var evaluatedPoint = splineContainer.EvaluatePosition(splineIndex, t);
                Vector3 worldPoint = new Vector3(evaluatedPoint.x, evaluatedPoint.y, evaluatedPoint.z);
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                Vector2 point = new Vector2(localPoint.x, localPoint.y);

                if (centerPoints.Count == 0 || Vector2.SqrMagnitude(centerPoints[centerPoints.Count - 1] - point) > 0.000001f)
                {
                    if (centerPoints.Count == 0)
                    {
                        distances.Add(0f);
                    }
                    else
                    {
                        float previousDistance = distances[distances.Count - 1];
                        distances.Add(previousDistance + Vector2.Distance(centerPoints[centerPoints.Count - 1], point));
                    }

                    centerPoints.Add(point);
                }
            }
        }

        private void BuildMeshData()
        {
            vertices.Clear();
            uvs.Clear();
            triangles.Clear();

            float halfWidth = width * 0.5f;

            for (int i = 0; i < centerPoints.Count; i++)
            {
                Vector2 normal = GetNormal(i);
                Vector2 left = centerPoints[i] + normal * halfWidth;
                Vector2 right = centerPoints[i] - normal * halfWidth;
                float u = distances[i];

                vertices.Add(new Vector3(left.x, left.y, 0f));
                vertices.Add(new Vector3(right.x, right.y, 0f));
                uvs.Add(new Vector2(u, 1f));
                uvs.Add(new Vector2(u, 0f));
            }

            for (int i = 0; i < centerPoints.Count - 1; i++)
            {
                int start = i * 2;
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }
        }

        private Vector2 GetNormal(int index)
        {
            int previousIndex = Mathf.Max(0, index - 1);
            int nextIndex = Mathf.Min(centerPoints.Count - 1, index + 1);
            Vector2 direction = centerPoints[nextIndex] - centerPoints[previousIndex];

            if (direction.sqrMagnitude <= 0.000001f)
            {
                return Vector2.up;
            }

            direction.Normalize();
            return new Vector2(-direction.y, direction.x);
        }

        private void ApplyMesh()
        {
            Mesh mesh = GetOrCreateMesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        private Mesh GetOrCreateMesh()
        {
            if (generatedMesh == null)
            {
                generatedMesh = new Mesh
                {
                    name = "Spline 2D Mesh",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (meshFilter.sharedMesh != generatedMesh)
            {
                meshFilter.sharedMesh = generatedMesh;
            }

            return generatedMesh;
        }

        private void ClearMesh()
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.Clear();
            }
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            CacheComponents();

            if (splineContainer == null || splineContainer.Splines == null || splineIndex >= splineContainer.Splines.Count)
            {
                return;
            }

            if (ReferenceEquals(splineContainer.Splines[splineIndex], spline))
            {
                Rebuild();
            }
        }

        private void OnSplineContainerChanged(SplineContainer container, int changedSplineIndex)
        {
            if (ReferenceEquals(container, splineContainer))
            {
                Rebuild();
            }
        }

        private void OnSplineContainerReordered(SplineContainer container, int previousIndex, int newIndex)
        {
            if (ReferenceEquals(container, splineContainer))
            {
                Rebuild();
            }
        }
    }
}
