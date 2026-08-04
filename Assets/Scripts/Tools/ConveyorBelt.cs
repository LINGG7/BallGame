using BallGame.Core;
using UnityEngine;

namespace BallGame.Tools
{
    public sealed class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private float surfaceForce = 12f;
        [SerializeField] private GameObject glowObject;

        private void Awake()
        {
            if (glowObject == null)
            {
                Transform glowTransform = transform.Find("ConveyorGlow");
                glowObject = glowTransform != null ? glowTransform.gameObject : null;
            }

            SetGlowActive(false);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            BallControllerBase ball = collision.collider.GetComponentInParent<BallControllerBase>();
            if (ball != null && ball.Body.bodyType == RigidbodyType2D.Dynamic)
            {
                SetGlowActive(true);
                ball.Body.AddForce((Vector2)transform.right * surfaceForce, ForceMode2D.Force);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            BallControllerBase ball = collision.collider.GetComponentInParent<BallControllerBase>();
            if (ball != null)
            {
                SetGlowActive(false);
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            BallControllerBase ball = collision.GetComponentInParent<BallControllerBase>();
            if (ball != null && ball.Body.bodyType == RigidbodyType2D.Dynamic)
            {
                SetGlowActive(true);
                ball.Body.AddForce((Vector2)transform.right * surfaceForce, ForceMode2D.Force);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            BallControllerBase ball = collision.GetComponentInParent<BallControllerBase>();
            if (ball != null)
            {
                SetGlowActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position - transform.right * 0.6f, transform.position + transform.right * 0.6f);
        }

        private void SetGlowActive(bool active)
        {
            if (glowObject != null && glowObject.activeSelf != active)
            {
                glowObject.SetActive(active);
            }
        }
    }
}
