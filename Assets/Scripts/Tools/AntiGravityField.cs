using System.Collections.Generic;
using BallGame.Audio;
using UnityEngine;

namespace BallGame.Tools
{
    public sealed class AntiGravityField : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(3f, 2f);
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private SpriteRenderer rangeHintRenderer;
        [SerializeField] private bool showRangeHint = true;
        [SerializeField] private bool allowOtherToolsInside = true;

        private readonly Collider2D[] hits = new Collider2D[32];
        private readonly HashSet<Rigidbody2D> affectedBodies = new HashSet<Rigidbody2D>();
        private BoxCollider2D boxCollider;

        public bool AllowOtherToolsInside => allowOtherToolsInside;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            RefreshShape();
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.1f, size.y);
            RefreshShape();
        }

        private void FixedUpdate()
        {
            affectedBodies.Clear();
            bool hasAffectedBall = false;
            int count = Physics2D.OverlapBoxNonAlloc(transform.position, size, transform.eulerAngles.z, hits, affectedLayers);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D body = GetBody(hits[i]);
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic || !affectedBodies.Add(body))
                {
                    continue;
                }

                Vector2 reverseGravityForce = -2f * Physics2D.gravity * body.gravityScale * body.mass;
                body.AddForce(reverseGravityForce, ForceMode2D.Force);
                hasAffectedBall |= IsDynamicBall(body);
            }

            if (hasAffectedBall)
            {
                AudioManager.Instance?.ReportAntiGravity(this);
            }
            else
            {
                AudioManager.Instance?.StopAntiGravity(this);
            }
        }

        private void OnDisable()
        {
            AudioManager.Instance?.StopAntiGravity(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.45f, 0.95f, 1f, 0.55f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
            Gizmos.matrix = previous;
        }

        private static Rigidbody2D GetBody(Collider2D hit)
        {
            if (hit == null)
            {
                return null;
            }

            return hit.attachedRigidbody != null ? hit.attachedRigidbody : hit.GetComponentInParent<Rigidbody2D>();
        }

        private static bool IsDynamicBall(Rigidbody2D body)
        {
            return body != null
                && body.bodyType == RigidbodyType2D.Dynamic
                && body.GetComponentInParent<BallControllerBase>() != null;
        }

        private void RefreshShape()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }

            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
                boxCollider.size = size;
            }

            if (rangeHintRenderer == null)
            {
                rangeHintRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (rangeHintRenderer == null)
            {
                return;
            }

            rangeHintRenderer.enabled = showRangeHint;
            rangeHintRenderer.transform.localPosition = Vector3.zero;
            rangeHintRenderer.transform.localRotation = Quaternion.identity;
            Sprite sprite = rangeHintRenderer.sprite;
            if (sprite == null)
            {
                rangeHintRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
                return;
            }

            Vector2 spriteSize = sprite.bounds.size;
            float scaleX = spriteSize.x > Mathf.Epsilon ? size.x / spriteSize.x : size.x;
            float scaleY = spriteSize.y > Mathf.Epsilon ? size.y / spriteSize.y : size.y;
            rangeHintRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}
