using System.Collections.Generic;
using BallGame.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace BallGame.LevelDesign
{
    public sealed class TrackIntersectionEditable2D : MonoBehaviour
    {
        [SerializeField] private SplineContainer upperSpline;
        [SerializeField] private SplineMesh2DGenerator upperMesh;
        [SerializeField] private SplineCollider2DGenerator upperCollider;
        [SerializeField] private SplineContainer lowerSpline;
        [SerializeField] private SplineMesh2DGenerator lowerMesh;
        [SerializeField] private SplineCollider2DGenerator lowerCollider;
        [SerializeField] private Transform upperEntryA;
        [SerializeField] private Transform lowerEntryA;
        [SerializeField] private TrackLayerSwitcher2D layerSwitcher;
        [SerializeField] private TrackLayerSwitchTrigger2D upperEntryTrigger;
        [SerializeField] private TrackLayerSwitchTrigger2D lowerEntryTrigger;
        [SerializeField] private TrackIntersectionRendererOrder2D rendererOrder;

        public bool IsConfigured => upperSpline != null && upperMesh != null && upperCollider != null
            && lowerSpline != null && lowerMesh != null && lowerCollider != null
            && upperEntryA != null && lowerEntryA != null
            && layerSwitcher != null && upperEntryTrigger != null && lowerEntryTrigger != null
            && rendererOrder != null;

        public SplineContainer GetSpline(bool upper) => upper ? upperSpline : lowerSpline;
        public Transform GetEntry(bool upper) => upper ? upperEntryA : lowerEntryA;

        public CollaborativeTrackIntersectionData CaptureData(CollaborativeTrackIntersectionData existing = null)
        {
            if (!IsConfigured) return existing != null ? existing.Clone() : null;
            CollaborativeTrackIntersectionData result = new CollaborativeTrackIntersectionData
            {
                upperCurve = CaptureCurve(upperSpline, upperMesh),
                lowerCurve = CaptureCurve(lowerSpline, lowerMesh)
            };
            result.upperEntryA = CaptureAttachment(
                upperSpline,
                upperEntryA,
                existing != null && existing.upperEntryA != null ? (bool?)existing.upperEntryA.attachToStart : null);
            result.lowerEntryA = CaptureAttachment(
                lowerSpline,
                lowerEntryA,
                existing != null && existing.lowerEntryA != null ? (bool?)existing.lowerEntryA.attachToStart : null);
            return result;
        }

        public void Apply(CollaborativeTrackIntersectionData data)
        {
            if (!IsConfigured || data == null) return;
            ApplyCurve(true, data.upperCurve);
            ApplyCurve(false, data.lowerCurve);
            ApplyEntries(data);
            rendererOrder.Apply();
        }

        public void ApplyCurve(bool upper, CollaborativeTrackCurveData data)
        {
            if (!IsConfigured || data == null || data.points == null || data.points.Count < 2) return;
            SplineContainer container = GetSpline(upper);
            Spline spline = container.Spline;
            spline.Clear();
            spline.Closed = false;
            foreach (CollaborativeSplinePoint point in data.points)
                spline.Add(new BezierKnot(new float3(point.position.x, point.position.y, 0f)), TangentMode.AutoSmooth);
            SplineMesh2DGenerator mesh = upper ? upperMesh : lowerMesh;
            SplineCollider2DGenerator collider = upper ? upperCollider : lowerCollider;
            mesh.SetWidth(data.width);
            collider.SetWidth(data.width);
            layerSwitcher.SetLayer(layerSwitcher.IsUpperActive);
        }

        public void ApplyEntries(CollaborativeTrackIntersectionData data)
        {
            if (!IsConfigured || data == null) return;
            ApplyAttachment(upperSpline, upperEntryA, data.upperEntryA);
            ApplyAttachment(lowerSpline, lowerEntryA, data.lowerEntryA);
        }

        public Vector2 GetEntryLocalPosition(bool upper)
        {
            Transform entry = GetEntry(upper);
            return entry != null ? (Vector2)transform.InverseTransformPoint(entry.position) : Vector2.zero;
        }

        public float GetEntryLocalRotation(bool upper)
        {
            Transform entry = GetEntry(upper);
            return entry != null ? Mathf.DeltaAngle(transform.eulerAngles.z, entry.eulerAngles.z) : 0f;
        }

        public void SetEntryLocalPose(bool upper, Vector2 localPosition, float localRotation, CollaborativeTrackIntersectionData data)
        {
            if (!IsConfigured || data == null) return;
            CollaborativeEndpointAttachmentData attachment = upper ? data.upperEntryA : data.lowerEntryA;
            SplineContainer curve = GetSpline(upper);
            GetEndpointFrame(curve, attachment.attachToStart, out Vector2 endpoint, out float angle);
            Quaternion frame = Quaternion.Euler(0f, 0f, angle);
            attachment.positionOffset = Quaternion.Inverse(frame) * (localPosition - endpoint);
            attachment.rotationOffset = Mathf.DeltaAngle(angle, localRotation);
            ApplyAttachment(curve, GetEntry(upper), attachment);
        }

        private static CollaborativeTrackCurveData CaptureCurve(SplineContainer container, SplineMesh2DGenerator mesh)
        {
            CollaborativeTrackCurveData result = new CollaborativeTrackCurveData { width = mesh.Width };
            foreach (BezierKnot knot in container.Spline.Knots)
            {
                result.points.Add(new CollaborativeSplinePoint
                {
                    position = new Vector2(knot.Position.x, knot.Position.y)
                });
            }
            return result;
        }

        private CollaborativeEndpointAttachmentData CaptureAttachment(SplineContainer curve, Transform entry, bool? attachToStart)
        {
            Vector2 entryPosition = transform.InverseTransformPoint(entry.position);
            float entryRotation = Mathf.DeltaAngle(transform.eulerAngles.z, entry.eulerAngles.z);
            bool useStart = attachToStart ?? IsStartCloser(curve, entryPosition);
            GetEndpointFrame(curve, useStart, out Vector2 endpoint, out float angle);
            Quaternion frame = Quaternion.Euler(0f, 0f, angle);
            return new CollaborativeEndpointAttachmentData
            {
                attachToStart = useStart,
                positionOffset = Quaternion.Inverse(frame) * (entryPosition - endpoint),
                rotationOffset = Mathf.DeltaAngle(angle, entryRotation)
            };
        }

        private bool IsStartCloser(SplineContainer curve, Vector2 entryPosition)
        {
            GetEndpointFrame(curve, true, out Vector2 start, out _);
            GetEndpointFrame(curve, false, out Vector2 end, out _);
            return Vector2.SqrMagnitude(entryPosition - start) <= Vector2.SqrMagnitude(entryPosition - end);
        }

        private void ApplyAttachment(SplineContainer curve, Transform entry, CollaborativeEndpointAttachmentData attachment)
        {
            if (curve == null || entry == null || attachment == null) return;
            GetEndpointFrame(curve, attachment.attachToStart, out Vector2 endpoint, out float angle);
            Quaternion frame = Quaternion.Euler(0f, 0f, angle);
            Vector2 localPosition = endpoint + (Vector2)(frame * attachment.positionOffset);
            float localRotation = angle + attachment.rotationOffset;
            entry.position = transform.TransformPoint(localPosition);
            entry.rotation = transform.rotation * Quaternion.Euler(0f, 0f, localRotation);
        }

        private void GetEndpointFrame(SplineContainer curve, bool start, out Vector2 endpoint, out float angle)
        {
            float t = start ? 0f : 1f;
            Vector3 endpointWorld = curve.EvaluatePosition(t);
            Vector3 tangentWorld = curve.EvaluateTangent(t);
            if (tangentWorld.sqrMagnitude <= 0.000001f && curve.Spline.Count >= 2)
            {
                int first = start ? 0 : curve.Spline.Count - 2;
                int second = start ? 1 : curve.Spline.Count - 1;
                float3 delta = curve.Spline[second].Position - curve.Spline[first].Position;
                tangentWorld = curve.transform.TransformVector(new Vector3(delta.x, delta.y, delta.z));
            }
            Vector3 endpointLocal = transform.InverseTransformPoint(endpointWorld);
            Vector3 tangentLocal = transform.InverseTransformVector(tangentWorld);
            endpoint = endpointLocal;
            angle = Mathf.Atan2(tangentLocal.y, tangentLocal.x) * Mathf.Rad2Deg;
        }
    }
}
