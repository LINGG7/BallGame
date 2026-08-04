using System.Collections;
using System.Collections.Generic;
using BallGame.Audio;
using UnityEngine;

public class SpringPadFree : MonoBehaviour
{
    private const string AnimationStateName = "Spring Animation";
    //private const string SpringSfxResourcePath = "Audio/SFX/Spring";

    [SerializeField] private float impulse = 9f;
    [SerializeField] private float cooldown = 0.2f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer animatedRenderer;
    [SerializeField] private float animationDuration = 0.1f;
    [SerializeField] private float minTopContactDot = 0.5f;
    [SerializeField] private AudioClip springSfx;

    private float lastFireTime = -100f;
    private Sprite idleSprite;
    private Coroutine animationRoutine;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animatedRenderer == null)
        {
            animatedRenderer = animator != null
                ? animator.GetComponent<SpriteRenderer>()
                : GetComponentInChildren<SpriteRenderer>();
        }

        if (animatedRenderer != null)
        {
            idleSprite = animatedRenderer.sprite;
        }

        //if (springSfx == null)
        //{
        //    springSfx = Resources.Load<AudioClip>(SpringSfxResourcePath);
        //}
    }

    private void OnEnable()
    {
        SetAnimationPlaying(false);
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastFireTime < cooldown)
        {
            return;
        }

        BallControllerBase ball = collision.collider.GetComponentInParent<BallControllerBase>();
        BallGame.Core.BallController controlledBall = ball as BallGame.Core.BallController;
        if (ball == null || (controlledBall != null && controlledBall.IsStoppedAtGoal))
        {
            return;
        }

        if (!IsTopContact(collision))
        {
            return;
        }

        lastFireTime = Time.time;
        ball.Body.AddForce(transform.up * impulse, ForceMode2D.Impulse);
        PlayAnimation();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.up);
    }

    private bool IsTopContact(Collision2D collision)
    {
        Vector2 springUp = transform.up;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (Vector2.Dot(-contact.normal, springUp) >= minTopContactDot)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(AnimationStateName, 0, 0f);
        animator.Update(0f);
        //AudioManager.Instance?.PlaySfx(springSfx);
        animationRoutine = StartCoroutine(StopAnimationAfterDelay());
    }

    private IEnumerator StopAnimationAfterDelay()
    {
        yield return new WaitForSeconds(animationDuration);
        SetAnimationPlaying(false);
        animationRoutine = null;
    }

    private void SetAnimationPlaying(bool playing)
    {
        if (animator == null)
        {
            return;
        }

        if (playing)
        {
            animator.enabled = true;
            animator.speed = 1f;
            return;
        }

        animator.speed = 0f;
        animator.Rebind();
        animator.Update(0f);
        animator.enabled = false;

        if (animatedRenderer != null && idleSprite != null)
        {
            animatedRenderer.sprite = idleSprite;
        }
    }
}
