using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SimulationMover2D : MonoBehaviour, ISimulationPreview2D
    {
        [SerializeField] private bool moveEnabled = true;
        [SerializeField, Min(0f)] private float moveSpeed = 1f;
        [SerializeField] private bool pingPong = true;
        [SerializeField] private bool closedPath;
        [SerializeField] private bool rotateEnabled = true;
        [SerializeField] private float angularSpeed;
        [SerializeField] private bool editorOwnedRigidbody;
        private readonly List<Vector2> waypoints = new List<Vector2>();
        private readonly List<Vector2> simulationWorldWaypoints = new List<Vector2>();
        private Rigidbody2D body;
        private GameManager gameManager;
        private Vector3 savedPosition;
        private Vector2 savedLocalPosition;
        private Quaternion savedRotation;
        private RigidbodyType2D savedBodyType;
        private bool savedSimulated;
        private bool playing;
        private bool hasSnapshot;
        private bool runtimePlacedInstance;
        private int targetIndex;
        private int direction = 1;

        public bool MoveEnabled { get => moveEnabled; set => moveEnabled = value; }
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = Mathf.Max(0f, value); }
        public bool PingPong { get => pingPong; set => pingPong = value; }
        public bool ClosedPath { get => closedPath; set => closedPath = value; }
        public bool RotateEnabled { get => rotateEnabled; set => rotateEnabled = value; }
        public float AngularSpeed { get => angularSpeed; set => angularSpeed = value; }
        public bool EditorOwnsRigidbody => editorOwnedRigidbody;
        public bool RuntimePlacedInstance => runtimePlacedInstance;
        public bool HasActiveDriver => moveEnabled || (rotateEnabled && Mathf.Abs(angularSpeed) > 0.0001f);
        public IReadOnlyList<Vector2> WaypointLocalPositions => waypoints;
        public void SetRuntimePlacedInstance(bool value) { runtimePlacedInstance = value; if (value) StopAndRestore(); }

        public void SetEditorRigidbodyOwnership(bool ownsRigidbody)
        {
            editorOwnedRigidbody = ownsRigidbody;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            CaptureWaypointsFromChildren();
        }

        private void OnEnable()
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null) { gameManager.StateChanged += OnStateChanged; OnStateChanged(gameManager.State); }
        }

        private void OnDisable()
        {
            StopAndRestore();
            if (gameManager != null) { gameManager.StateChanged -= OnStateChanged; gameManager = null; }
        }

        private void FixedUpdate()
        {
            if (runtimePlacedInstance || !playing || body == null || !body.simulated) return;
            if (moveEnabled) Move(Time.fixedDeltaTime);
            if (rotateEnabled && angularSpeed != 0f) body.MoveRotation(body.rotation + angularSpeed * Time.fixedDeltaTime);
        }

        public void SetSimulationPreview(bool playingPreview)
        {
            if (playingPreview) StartSimulation(); else StopAndRestore();
        }

        public void CaptureWaypointsFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Waypoint")) waypoints.Add(child.localPosition);
            }
        }

        public void SetWaypointLocalPositions(IList<Vector2> positions)
        {
            waypoints.Clear();
            if (positions != null) for (int i = 0; i < positions.Count; i++) waypoints.Add(positions[i]);
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).name.StartsWith("Waypoint")) children.Add(transform.GetChild(i));
            for (int i = children.Count - 1; i >= waypoints.Count; i--)
            {
                if (Application.isPlaying) Destroy(children[i].gameObject);
                else DestroyImmediate(children[i].gameObject);
            }
            for (int i = 0; i < waypoints.Count; i++)
            {
                Transform child = i < children.Count ? children[i] : new GameObject("Waypoint").transform;
                child.SetParent(transform, false); child.localPosition = waypoints[i]; child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
                child.name = "Waypoint" + (i == 0 ? string.Empty : "_" + i.ToString("00"));
            }
        }

        public int WaypointCount => waypoints.Count;
        public Vector2 GetWaypointLocalPosition(int index) => waypoints[Mathf.Clamp(index, 0, waypoints.Count - 1)];
        public Transform GetWaypointTransform(int index)
        {
            List<Transform> children = GetWaypointChildren();
            return index >= 0 && index < children.Count ? children[index] : null;
        }

        public Vector2 GetWaypointWorldPosition(int index)
        {
            Transform waypoint = GetWaypointTransform(index);
            return waypoint != null ? waypoint.position : (Vector2)transform.position;
        }

        public void SetWaypointWorldPosition(int index, Vector2 worldPosition)
        {
            Transform waypoint = GetWaypointTransform(index);
            if (waypoint == null) return;
            SetWaypointLocalPosition(index, transform.InverseTransformPoint(worldPosition));
        }

        public void AddWaypoint(Vector2 localPosition)
        {
            waypoints.Add(localPosition);
            SetWaypointLocalPositions(new List<Vector2>(waypoints));
        }

        public void RemoveWaypoint(int index)
        {
            if (index < 0 || index >= waypoints.Count) return;
            waypoints.RemoveAt(index);
            SetWaypointLocalPositions(new List<Vector2>(waypoints));
        }

        public void SetWaypointLocalPosition(int index, Vector2 position)
        {
            if (index < 0 || index >= waypoints.Count) return;
            waypoints[index] = position;
            List<Transform> children = GetWaypointChildren();
            if (index < children.Count) children[index].localPosition = position;
        }

        public void MoveWaypoint(int index, int newIndex)
        {
            if (index < 0 || index >= waypoints.Count) return;
            newIndex = Mathf.Clamp(newIndex, 0, waypoints.Count - 1);
            if (index == newIndex) return;
            Vector2 value = waypoints[index]; waypoints.RemoveAt(index); waypoints.Insert(newIndex, value);
            SetWaypointLocalPositions(new List<Vector2>(waypoints));
        }

        private List<Transform> GetWaypointChildren()
        {
            List<Transform> result = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).name.StartsWith("Waypoint")) result.Add(transform.GetChild(i));
            return result;
        }

        private void OnStateChanged(GameState state)
        {
            if (runtimePlacedInstance) return;
            if (state == GameState.Simulate) StartSimulation(); else StopAndRestore();
        }

        private void StartSimulation()
        {
            if (playing || runtimePlacedInstance || body == null || !HasActiveDriver) return;
            savedPosition = transform.position; savedRotation = transform.rotation; savedBodyType = body.bodyType; savedSimulated = body.simulated; hasSnapshot = true;
            savedLocalPosition = transform.localPosition;
            simulationWorldWaypoints.Clear();
            for (int i = 0; i < waypoints.Count; i++) simulationWorldWaypoints.Add(transform.TransformPoint(waypoints[i]));
            body.bodyType = RigidbodyType2D.Kinematic; body.simulated = true; targetIndex = 0; direction = 1; playing = true;
        }

        private void StopAndRestore()
        {
            playing = false;
            if (!hasSnapshot) return;
            if (body == null)
            {
                hasSnapshot = false;
                simulationWorldWaypoints.Clear();
                return;
            }
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = savedBodyType;
            body.simulated = savedSimulated;
            body.position = savedPosition;
            body.rotation = savedRotation.eulerAngles.z;

            // Stopping the level-designer preview happens between physics steps, so
            // restore the Transform as well to make the original pose visible now.
            transform.SetPositionAndRotation(savedPosition, savedRotation);
            hasSnapshot = false;
            simulationWorldWaypoints.Clear();
        }

        private void Move(float deltaTime)
        {
            if (waypoints.Count == 0 || moveSpeed <= 0f) return;
            float remaining = moveSpeed * deltaTime;
            Vector2 position = body.position;
            while (remaining > 0f && playing)
            {
                Vector2 target = GetWorldWaypoint(targetIndex); Vector2 offset = target - position; float length = offset.magnitude;
                if (length <= 0.0001f) { AdvanceTarget(); continue; }
                if (length > remaining) { position += offset / length * remaining; remaining = 0f; break; }
                position = target; remaining -= length; AdvanceTarget();
            }
            body.MovePosition(position);
        }

        private void AdvanceTarget()
        {
            int count = waypoints.Count + 1;
            if (count <= 1) { playing = false; return; }
            if (closedPath) { targetIndex = (targetIndex + 1) % count; return; }
            if (pingPong)
            {
                if (targetIndex == count - 1) direction = -1; else if (targetIndex == 0) direction = 1;
                targetIndex = Mathf.Clamp(targetIndex + direction, 0, count - 1); return;
            }
            if (targetIndex < count - 1) targetIndex++; else playing = false;
        }

        private Vector2 GetWorldWaypoint(int index)
        {
            Vector3 local = index <= 0
                ? savedLocalPosition
                : Vector3.zero;
            if (index <= 0) return transform.parent != null ? transform.parent.TransformPoint(local) : local;
            return simulationWorldWaypoints[Mathf.Clamp(index - 1, 0, simulationWorldWaypoints.Count - 1)];
        }

        private void OnValidate() { moveSpeed = Mathf.Max(0f, moveSpeed); }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
#endif
            CaptureWaypointsFromChildren(); if (waypoints.Count == 0) return; Gizmos.color = Color.cyan; Vector2 previous = transform.position;
            for (int i = 0; i < waypoints.Count; i++) { Vector2 point = transform.TransformPoint(waypoints[i]); Gizmos.DrawLine(previous, point); Gizmos.DrawWireSphere(point, 0.08f); previous = point; }
            if (closedPath && waypoints.Count > 1) Gizmos.DrawLine(previous, transform.position);
#if UNITY_EDITOR
            for (int i = 0; i < waypoints.Count; i++) UnityEditor.Handles.Label(transform.TransformPoint(waypoints[i]), i.ToString());
#endif
        }
    }
}
