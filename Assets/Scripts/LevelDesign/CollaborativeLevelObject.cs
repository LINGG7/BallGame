using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;

namespace BallGame.LevelDesign
{
    public sealed class CollaborativeLevelObject : MonoBehaviour
    {
        public string Id { get; private set; }
        public string AssetKey { get; private set; }
        public CollaborativeObjectKind Kind { get; private set; }
        public float SplineWidth { get; set; } = 0.5f;
        public bool SplineClosed { get; set; }
        public string SplineCollisionMode { get; set; } = "SolidPolygon";
        public List<CollaborativeProperty> Properties { get; } = new List<CollaborativeProperty>();
        public List<CollaborativeSplinePoint> SplinePoints { get; } = new List<CollaborativeSplinePoint>();

        public void Initialize(string id, CollaborativeLevelAssetEntry entry)
        {
            Id = id;
            AssetKey = entry.assetKey;
            Kind = entry.kind;
        }

        public void Initialize(CollaborativeObjectRecord record)
        {
            Id = record.id;
            AssetKey = record.assetKey;
            Kind = record.kind;
            SplineWidth = record.splineWidth;
            SplineClosed = record.splineClosed;
            SplineCollisionMode = record.splineCollisionMode;
            Properties.Clear();
            if (record.properties != null) Properties.AddRange(record.properties);
            SplinePoints.Clear();
            if (record.splinePoints != null) SplinePoints.AddRange(record.splinePoints);
        }

        public CollaborativeObjectRecord ToRecord()
        {
            bool hasSize = SizeEditable2DUtility.TryGet(gameObject, out ISizeEditable2D editable);
            return new CollaborativeObjectRecord
            {
                id = Id,
                assetKey = AssetKey,
                kind = Kind,
                position = transform.localPosition,
                rotation = transform.localEulerAngles.z,
                scale = transform.localScale,
                hasSize = hasSize,
                size = hasSize ? editable.Size : Vector2.zero,
                splineWidth = SplineWidth,
                splineClosed = SplineClosed,
                splineCollisionMode = SplineCollisionMode,
                properties = new List<CollaborativeProperty>(Properties),
                splinePoints = new List<CollaborativeSplinePoint>(SplinePoints)
            };
        }
    }
}
