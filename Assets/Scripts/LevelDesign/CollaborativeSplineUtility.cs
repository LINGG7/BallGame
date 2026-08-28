using System.Collections.Generic;
using BallGame.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace BallGame.LevelDesign
{
    public static class CollaborativeSplineUtility
    {
        public static void Apply(GameObject target, IList<CollaborativeSplinePoint> points, float width = 0.5f, bool closed = false, string collisionMode = "SolidPolygon")
        {
            if (target == null || points == null || points.Count < 2) return;
            SplineContainer container = target.GetComponentInChildren<SplineContainer>();
            if (container == null) return;
            Spline spline = container.Spline;
            spline.Clear();
            spline.Closed = closed;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i].position;
                spline.Add(new BezierKnot(new float3(point.x, point.y, 0f)), TangentMode.AutoSmooth);
            }
            SplineMesh2DGenerator mesh = target.GetComponentInChildren<SplineMesh2DGenerator>();
            if (mesh != null) mesh.SetWidth(width);
            SplineCollider2DGenerator collider = target.GetComponentInChildren<SplineCollider2DGenerator>();
            if (collider != null) collider.Configure(width, collisionMode == "RoundedEdge");
        }
    }

    public sealed class CollaborativeSplineHandle : MonoBehaviour
    {
        public CollaborativeLevelObject Owner { get; private set; }
        public int PointIndex { get; private set; }
        public bool IsTrackIntersection { get; private set; }
        public bool IsEntry { get; private set; }
        public bool Upper { get; private set; }
        public bool IsObstacleSizeHandle { get; private set; }
        public bool IsSimulationWaypoint { get; private set; }
        public int SizeHandleSide { get; private set; }

        public void Initialize(CollaborativeLevelObject owner, int pointIndex)
        {
            Owner = owner;
            PointIndex = pointIndex;
            IsTrackIntersection = false;
            IsEntry = false;
            Upper = false;
            IsObstacleSizeHandle = false;
            IsSimulationWaypoint = false;
            SizeHandleSide = 0;
        }

        public void InitializeSimulationWaypoint(CollaborativeLevelObject owner, int pointIndex)
        {
            Owner = owner;
            PointIndex = pointIndex;
            IsTrackIntersection = false;
            IsEntry = false;
            Upper = false;
            IsObstacleSizeHandle = false;
            IsSimulationWaypoint = true;
            SizeHandleSide = 0;
        }

        public void InitializeTrackPoint(CollaborativeLevelObject owner, bool upper, int pointIndex)
        {
            Owner = owner;
            PointIndex = pointIndex;
            IsTrackIntersection = true;
            IsEntry = false;
            Upper = upper;
            IsObstacleSizeHandle = false;
            IsSimulationWaypoint = false;
            SizeHandleSide = 0;
        }

        public void InitializeTrackEntry(CollaborativeLevelObject owner, bool upper)
        {
            Owner = owner;
            PointIndex = -1;
            IsTrackIntersection = true;
            IsEntry = true;
            Upper = upper;
            IsObstacleSizeHandle = false;
            IsSimulationWaypoint = false;
            SizeHandleSide = 0;
        }

        public void InitializeObstacleSize(CollaborativeLevelObject owner, int side)
        {
            Owner = owner;
            PointIndex = -1;
            IsTrackIntersection = false;
            IsEntry = false;
            Upper = false;
            IsObstacleSizeHandle = true;
            IsSimulationWaypoint = false;
            SizeHandleSide = side < 0 ? -1 : 1;
        }
    }
}
