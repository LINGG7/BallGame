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
        public int formatVersion = 5;
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
            if (string.IsNullOrWhiteSpace(document.mode)) document.mode = "Normal";
            if (document.toolStock == null) document.toolStock = new List<CollaborativeToolStock>();
            if (document.objects == null) document.objects = new List<CollaborativeObjectRecord>();
            foreach (CollaborativeObjectRecord item in document.objects)
            {
                if (item == null) continue;
                if (item.assetKey != "track_intersection")
                    item.trackIntersection = null;
                else if (document.formatVersion < 4 && IsEmptyTrackIntersection(item.trackIntersection))
                    item.trackIntersection = null;
            }
            return document;
        }

        private static bool IsEmptyTrackIntersection(CollaborativeTrackIntersectionData data)
        {
            return data == null
                || ((data.upperCurve == null || data.upperCurve.points == null || data.upperCurve.points.Count == 0)
                    && (data.lowerCurve == null || data.lowerCurve.points == null || data.lowerCurve.points.Count == 0));
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
        public CollaborativeTrackIntersectionData trackIntersection;
        public bool simulationMoverAttached;
        public bool simulationMoveEnabled;
        public float simulationMoveSpeed = 1f;
        public bool simulationPingPong = true;
        public bool simulationClosedPath;
        public bool simulationRotateEnabled;
        public float simulationAngularSpeed;
        public List<Vector2> simulationWaypoints = new List<Vector2>();
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

    [Serializable]
    public sealed class CollaborativeTrackCurveData
    {
        public float width = 0.5f;
        public List<CollaborativeSplinePoint> points = new List<CollaborativeSplinePoint>();
    }

    [Serializable]
    public sealed class CollaborativeEndpointAttachmentData
    {
        public bool attachToStart;
        public Vector2 positionOffset;
        public float rotationOffset;
    }

    [Serializable]
    public sealed class CollaborativeTrackIntersectionData
    {
        public CollaborativeTrackCurveData upperCurve = new CollaborativeTrackCurveData();
        public CollaborativeTrackCurveData lowerCurve = new CollaborativeTrackCurveData();
        public CollaborativeEndpointAttachmentData upperEntryA = new CollaborativeEndpointAttachmentData();
        public CollaborativeEndpointAttachmentData lowerEntryA = new CollaborativeEndpointAttachmentData();

        public CollaborativeTrackIntersectionData Clone()
        {
            return new CollaborativeTrackIntersectionData
            {
                upperCurve = CloneCurve(upperCurve),
                lowerCurve = CloneCurve(lowerCurve),
                upperEntryA = CloneAttachment(upperEntryA),
                lowerEntryA = CloneAttachment(lowerEntryA)
            };
        }

        private static CollaborativeTrackCurveData CloneCurve(CollaborativeTrackCurveData source)
        {
            CollaborativeTrackCurveData clone = new CollaborativeTrackCurveData
            {
                width = source != null ? source.width : 0.5f
            };
            if (source != null && source.points != null)
            {
                foreach (CollaborativeSplinePoint point in source.points)
                {
                    if (point == null) continue;
                    clone.points.Add(new CollaborativeSplinePoint
                    {
                        position = point.position,
                        tangentIn = point.tangentIn,
                        tangentOut = point.tangentOut
                    });
                }
            }
            return clone;
        }

        private static CollaborativeEndpointAttachmentData CloneAttachment(CollaborativeEndpointAttachmentData source)
        {
            return source == null
                ? new CollaborativeEndpointAttachmentData()
                : new CollaborativeEndpointAttachmentData
                {
                    attachToStart = source.attachToStart,
                    positionOffset = source.positionOffset,
                    rotationOffset = source.rotationOffset
                };
        }
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

            if (document.formatVersion < 1 || document.formatVersion > 5)
                errors.Add("Unsupported formatVersion: " + document.formatVersion);
            if (string.IsNullOrWhiteSpace(document.title))
                errors.Add("title is required.");
            if (document.mode != "Normal" && document.mode != "LowestCost")
                errors.Add("mode must be Normal or LowestCost.");
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
                ValidateTrackIntersection(document.formatVersion, item, errors);
                ValidateSimulationMover(item, errors);
            }

            if (ballCount == 0) errors.Add("At least one Ball object is required.");
            if (goalCount == 0) errors.Add("At least one Goal object is required.");
            foreach (KeyValuePair<string, int> pair in portalPairs)
                if (pair.Value != 2) errors.Add("Portal pair " + pair.Key + " must contain exactly two portals.");
            return errors;
        }

        private static void ValidateTrackIntersection(int formatVersion, CollaborativeObjectRecord item, List<string> errors)
        {
            bool isTrackIntersection = item.assetKey == "track_intersection";
            if (!isTrackIntersection) return;

            if (item.trackIntersection == null
                || ((item.trackIntersection.upperCurve == null
                        || item.trackIntersection.upperCurve.points == null
                        || item.trackIntersection.upperCurve.points.Count == 0)
                    && (item.trackIntersection.lowerCurve == null
                        || item.trackIntersection.lowerCurve.points == null
                        || item.trackIntersection.lowerCurve.points.Count == 0)))
            {
                if (formatVersion >= 4)
                    errors.Add("track_intersection is missing compound edit data: " + item.id);
                return;
            }

            ValidateTrackCurve(item.trackIntersection.upperCurve, "upperCurve", item.id, errors);
            ValidateTrackCurve(item.trackIntersection.lowerCurve, "lowerCurve", item.id, errors);
            ValidateAttachment(item.trackIntersection.upperEntryA, "upperEntryA", item.id, errors);
            ValidateAttachment(item.trackIntersection.lowerEntryA, "lowerEntryA", item.id, errors);
        }

        private static void ValidateSimulationMover(CollaborativeObjectRecord item, List<string> errors)
        {
            if (item == null || !item.simulationMoverAttached) return;
            if (!IsFinite(item.simulationMoveSpeed) || item.simulationMoveSpeed < 0f)
                errors.Add("Simulation move speed must be finite and non-negative: " + item.id);
            if (!IsFinite(item.simulationAngularSpeed))
                errors.Add("Simulation angular speed must be finite: " + item.id);
            if (item.simulationWaypoints == null) return;
            foreach (Vector2 waypoint in item.simulationWaypoints)
                if (!IsFinite(waypoint.x) || !IsFinite(waypoint.y))
                    errors.Add("Simulation waypoint contains a non-finite coordinate: " + item.id);
        }

        private static void ValidateTrackCurve(CollaborativeTrackCurveData curve, string name, string id, List<string> errors)
        {
            if (curve == null || curve.points == null || curve.points.Count < 2)
            {
                errors.Add("Track intersection " + name + " needs at least two points: " + id);
                return;
            }
            if (!IsFinitePositive(curve.width))
                errors.Add("Track intersection " + name + " width must be finite and positive: " + id);
            foreach (CollaborativeSplinePoint point in curve.points)
            {
                if (point == null || !IsFinite(point.position.x) || !IsFinite(point.position.y))
                    errors.Add("Track intersection " + name + " contains an invalid point: " + id);
            }
        }

        private static void ValidateAttachment(CollaborativeEndpointAttachmentData attachment, string name, string id, List<string> errors)
        {
            if (attachment == null
                || !IsFinite(attachment.positionOffset.x)
                || !IsFinite(attachment.positionOffset.y)
                || !IsFinite(attachment.rotationOffset))
                errors.Add("Track intersection " + name + " contains an invalid offset: " + id);
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
