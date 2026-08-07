using System;
using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;

namespace BallGame.LevelDesign
{
    public enum CollaborativeObjectKind
    {
        Ball,
        Goal,
        Tool,
        SplineTool
    }

    [Serializable]
    public sealed class CollaborativeLevelDocument
    {
        public int formatVersion = 3;
        public string title = "New Level";
        public string author = string.Empty;
        public string mode = "Normal";
        public Vector2 canvasCenter;
        public float canvasWidth = 24f;
        public float canvasHeight = 14f;
        public List<CollaborativeToolStock> toolStock = new List<CollaborativeToolStock>();
        public List<CollaborativeObjectRecord> objects = new List<CollaborativeObjectRecord>();

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public static CollaborativeLevelDocument FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Level JSON is empty.", nameof(json));
            CollaborativeLevelDocument document = JsonUtility.FromJson<CollaborativeLevelDocument>(json);
            if (document == null)
                throw new InvalidOperationException("Level JSON could not be parsed.");
            return document;
        }
    }

    [Serializable]
    public sealed class CollaborativeToolStock
    {
        public string assetKey;
        public int count;
    }

    [Serializable]
    public sealed class CollaborativeObjectRecord
    {
        public string id;
        public string assetKey;
        public CollaborativeObjectKind kind;
        public Vector2 position;
        public float rotation;
        public Vector2 scale = Vector2.one;
        public bool hasSize;
        public Vector2 size;
        public float splineWidth = 0.5f;
        public bool splineClosed;
        public string splineCollisionMode = "SolidPolygon";
        public List<CollaborativeProperty> properties = new List<CollaborativeProperty>();
        public List<CollaborativeSplinePoint> splinePoints = new List<CollaborativeSplinePoint>();
    }

    [Serializable]
    public sealed class CollaborativeProperty
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class CollaborativeSplinePoint
    {
        public Vector2 position;
        public Vector2 tangentIn;
        public Vector2 tangentOut;
    }

    public static class CollaborativeLevelValidator
    {
        public static List<string> Validate(CollaborativeLevelDocument document, CollaborativeLevelAssetRegistry registry)
        {
            List<string> errors = new List<string>();
            if (document == null)
            {
                errors.Add("Document is null.");
                return errors;
            }

            if (document.formatVersion != 1 && document.formatVersion != 2 && document.formatVersion != 3)
                errors.Add("Unsupported formatVersion: " + document.formatVersion);
            if (string.IsNullOrWhiteSpace(document.title))
                errors.Add("title is required.");
            if (!IsFinitePositive(document.canvasWidth) || !IsFinitePositive(document.canvasHeight))
                errors.Add("canvasWidth and canvasHeight must be finite positive values.");
            if (!IsFinite(document.canvasCenter.x) || !IsFinite(document.canvasCenter.y))
                errors.Add("canvasCenter must contain finite values.");

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int ballCount = 0;
            int goalCount = 0;
            Dictionary<string, int> portalPairs = new Dictionary<string, int>(StringComparer.Ordinal);
            HashSet<string> stockKeys = new HashSet<string>(StringComparer.Ordinal);
            if (document.toolStock != null)
            {
                foreach (CollaborativeToolStock stock in document.toolStock)
                {
                    if (stock == null || string.IsNullOrWhiteSpace(stock.assetKey) || !stockKeys.Add(stock.assetKey))
                    {
                        errors.Add("Tool stock assetKeys must be non-empty and unique.");
                        continue;
                    }
                    CollaborativeLevelAssetEntry stockEntry = registry != null ? registry.Find(stock.assetKey) : null;
                    if (stockEntry == null || stockEntry.toolDefinition == null)
                        errors.Add("Unknown tool stock assetKey: " + stock.assetKey);
                    if (stock.count < 0) errors.Add("Tool stock count cannot be negative: " + stock.assetKey);
                }
            }
            if (document.objects == null)
                document.objects = new List<CollaborativeObjectRecord>();

            foreach (CollaborativeObjectRecord item in document.objects)
            {
                if (item == null)
                {
                    errors.Add("objects contains a null entry.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.id) || !ids.Add(item.id))
                    errors.Add("Object ids must be non-empty and unique: " + item.id);
                CollaborativeLevelAssetEntry entry = registry != null ? registry.Find(item.assetKey) : null;
                if (entry == null)
                    errors.Add("Unknown assetKey: " + item.assetKey);
                else if (entry.kind != item.kind)
                    errors.Add("Object kind does not match its registered assetKey: " + item.id);
                if (!IsFinite(item.position.x) || !IsFinite(item.position.y)
                    || !IsFinite(item.rotation) || !IsFinite(item.scale.x) || !IsFinite(item.scale.y))
                    errors.Add("Object has a non-finite transform: " + item.id);
                if (entry != null && entry.prefab != null)
                {
                    Vector3 prefabScale = entry.prefab.transform.localScale;
                    if (!Approximately(item.scale.x, prefabScale.x) || !Approximately(item.scale.y, prefabScale.y))
                        errors.Add("Transform scale must match the registered prefab scale; use an object's size fields instead: " + item.id);
                }
                bool supportsSize = entry != null && entry.prefab != null
                    && SizeEditable2DUtility.TryGet(entry.prefab, out _);
                if (document.formatVersion >= 2 && item.hasSize)
                {
                    if (!supportsSize)
                        errors.Add("Object does not support size editing: " + item.id);
                    if (!IsFinitePositive(item.size.x) || !IsFinitePositive(item.size.y))
                        errors.Add("Object size must be finite and positive: " + item.id);
                }
                if (item.kind == CollaborativeObjectKind.Ball) ballCount++;
                if (item.kind == CollaborativeObjectKind.Goal) goalCount++;
                if (item.kind == CollaborativeObjectKind.SplineTool)
                {
                    if (item.splinePoints == null || item.splinePoints.Count < 2)
                        errors.Add("Spline object needs at least two points: " + item.id);
                    else
                        foreach (CollaborativeSplinePoint point in item.splinePoints)
                            if (point == null || !IsFinite(point.position.x) || !IsFinite(point.position.y)
                                || !IsFinite(point.tangentIn.x) || !IsFinite(point.tangentIn.y)
                                || !IsFinite(point.tangentOut.x) || !IsFinite(point.tangentOut.y))
                                errors.Add("Spline contains a non-finite control point: " + item.id);
                    if (!IsFinitePositive(item.splineWidth)) errors.Add("Spline width must be finite and positive: " + item.id);
                    if (item.splineCollisionMode != "SolidPolygon" && item.splineCollisionMode != "RoundedEdge")
                        errors.Add("Unknown spline collision mode: " + item.id);
                }
                if (item.assetKey == "portal")
                {
                    string pairId = FindProperty(item, "pairId");
                    if (string.IsNullOrWhiteSpace(pairId)) errors.Add("Portal is missing pairId: " + item.id);
                    else if (!int.TryParse(pairId, out _)) errors.Add("Portal pairId must be an integer: " + item.id);
                    else portalPairs[pairId] = portalPairs.TryGetValue(pairId, out int count) ? count + 1 : 1;
                }
            }

            if (ballCount == 0) errors.Add("At least one Ball object is required.");
            if (goalCount == 0) errors.Add("At least one Goal object is required.");
            foreach (KeyValuePair<string, int> pair in portalPairs)
                if (pair.Value != 2) errors.Add("Portal pair " + pair.Key + " must contain exactly two portals.");
            return errors;
        }

        private static string FindProperty(CollaborativeObjectRecord item, string key)
        {
            if (item.properties == null) return null;
            for (int i = 0; i < item.properties.Count; i++)
                if (item.properties[i] != null && item.properties[i].key == key) return item.properties[i].value;
            return null;
        }

        private static bool IsFinitePositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool Approximately(float value, float expected)
        {
            return Mathf.Abs(value - expected) <= 0.0001f;
        }
    }
}
