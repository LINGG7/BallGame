using System;
using BallGame.Core;
using UnityEngine;

namespace BallGame.Placement
{
    [Serializable]
    public sealed class PlacementHintTarget
    {
        public ToolDefinition tool;
        public Vector3 position;
        public float rotationDegrees;
        [Min(0.01f)] public float snapRadius = 0.65f;
        public Vector2 cameraPosition;
        [Min(0f)] public float cameraMoveDuration = 0.6f;
        [Min(0.001f)] public float positionTolerance = 0.05f;
        [Range(0f, 180f)] public float rotationTolerance = 2f;

        public Quaternion Rotation => Quaternion.Euler(0f, 0f, rotationDegrees);
        public bool IsValid => tool != null && tool.Prefab != null && Finite(position.x)
            && Finite(position.y) && Finite(position.z) && Finite(rotationDegrees)
            && Finite(snapRadius) && snapRadius > 0f && Finite(cameraPosition.x)
            && Finite(cameraPosition.y) && Finite(cameraMoveDuration) && cameraMoveDuration >= 0f
            && Finite(positionTolerance) && positionTolerance > 0f
            && Finite(rotationTolerance) && rotationTolerance >= 0f;

        public bool IsInRange(PlaceableTool candidate) => candidate != null && candidate.Definition == tool
            && Vector2.Distance(candidate.transform.position, position) <= snapRadius;

        public bool IsSatisfiedBy(PlaceableTool candidate) => candidate != null && !candidate.IsPreview
            && candidate.Definition == tool
            && Vector2.Distance(candidate.transform.position, position) <= positionTolerance
            && Quaternion.Angle(candidate.transform.rotation, Rotation) <= rotationTolerance;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}