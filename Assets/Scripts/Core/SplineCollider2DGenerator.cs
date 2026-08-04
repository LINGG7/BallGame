using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace BallGame.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class SplineCollider2DGenerator : MonoBehaviour
    {
        private enum ColliderMode
        {
            SolidPolygon,
            RoundedEdge
        }

        [SerializeField] private ColliderMode colliderMode;
        [SerializeField] private int splineIndex;
        [SerializeField] private float width = 0.5f;
        [SerializeField] private float samplesPerUnit = 8f;
        [SerializeField] private int minSamples = 8;
        [SerializeField] private int maxSamples = 256;

        private readonly List<Vector2> centerPoints = new List<Vector2>();
        private readonly List<Vector2> colliderPoints = new List<Vector2>();

        private SplineContainer splineContainer;
        private PolygonCollider2D polygonCollider;
        private EdgeCollider2D edgeCollider;

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

            if (splineContainer == null)
            {
                return;
            }

            if (splineContainer.Splines == null || splineIndex >= splineContainer.Splines.Count)
            {
                ClearColliders();
                return;
            }

            BuildCenterPoints();

            if (centerPoints.Count < 2)
            {
                ClearColliders();
                return;
            }

            if (colliderMode == ColliderMode.RoundedEdge)
            {
                RebuildRoundedEdgeCollider();
            }
            else
            {
                RebuildSolidPolygonCollider();
            }
        }

        private void CacheComponents()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }

            if (polygonCollider == null)
            {
                polygonCollider = GetComponent<PolygonCollider2D>();
            }

            if (edgeCollider == null)
            {
                edgeCollider = GetComponent<EdgeCollider2D>();
            }
        }

        private void BuildCenterPoints()
        {
            centerPoints.Clear();

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
                    centerPoints.Add(point);
                }
            }
        }

        private void BuildColliderPoints()
        {
            colliderPoints.Clear();

            float halfWidth = width * 0.5f;
            int count = centerPoints.Count;

            for (int i = 0; i < count; i++)
            {
                colliderPoints.Add(centerPoints[i] + GetNormal(i) * halfWidth);
            }

            for (int i = count - 1; i >= 0; i--)
            {
                colliderPoints.Add(centerPoints[i] - GetNormal(i) * halfWidth);
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

        private void RebuildSolidPolygonCollider()
        {
            if (polygonCollider == null)
            {
                return;
            }

            if (edgeCollider != null)
            {
                edgeCollider.enabled = false;
                edgeCollider.points = System.Array.Empty<Vector2>();
            }

            polygonCollider.enabled = true;
            BuildColliderPoints();
            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, colliderPoints.ToArray());
        }

        private void RebuildRoundedEdgeCollider()
        {
            if (edgeCollider == null)
            {
                return;
            }

            if (polygonCollider != null)
            {
                polygonCollider.pathCount = 0;
                polygonCollider.enabled = false;
            }

            edgeCollider.enabled = true;
            edgeCollider.edgeRadius = width * 0.5f;
            edgeCollider.points = centerPoints.ToArray();
        }

        private void ClearColliders()
        {
            if (polygonCollider != null)
            {
                polygonCollider.pathCount = 0;
            }

            if (edgeCollider != null)
            {
                edgeCollider.points = System.Array.Empty<Vector2>();
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
