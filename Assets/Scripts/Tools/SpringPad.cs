using System.Collections;
using BallGame.Audio;
using BallGame.Core;
using BallGame.UI;
using UnityEngine;

namespace BallGame.Tools
{
	public sealed class SpringPad : MonoBehaviour
	{
		private const string AnimationStateName = "Spring Animation";

		private const string SpringSfxResourcePath = "Audio/SFX/Spring";

		[SerializeField]
		private float impulse = 9f;

		[SerializeField]
		private float cooldown = 0.2f;

		[SerializeField]
		private Animator animator;

		[SerializeField]
		private SpriteRenderer animatedRenderer;

		[SerializeField]
		private float animationDuration = 0.1f;

		[SerializeField]
		private float minTopContactDot = 0.5f;

		[SerializeField]
		private AudioClip springSfx;

		private float lastFireTime = -100f;

		private Sprite idleSprite;

		private Coroutine animationRoutine;

		private void Awake()
		{
			EnsureDirectionIndicator();
			if ((UnityEngine.Object)(object)animator == (UnityEngine.Object)null)
			{
				animator = ((Component)this).GetComponentInChildren<Animator>();
			}
			if ((UnityEngine.Object)(object)animatedRenderer == (UnityEngine.Object)null)
			{
				animatedRenderer = (((UnityEngine.Object)(object)animator != (UnityEngine.Object)null) ? ((Component)animator).GetComponent<SpriteRenderer>() : ((Component)this).GetComponentInChildren<SpriteRenderer>());
			}
			if ((UnityEngine.Object)(object)animatedRenderer != (UnityEngine.Object)null)
			{
				idleSprite = animatedRenderer.sprite;
			}
			if ((UnityEngine.Object)(object)springSfx == (UnityEngine.Object)null)
			{
				springSfx = Resources.Load<AudioClip>("Audio/SFX/Spring");
			}
		}

		private void EnsureDirectionIndicator()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			ForceDirectionIndicator forceDirectionIndicator = ((Component)this).GetComponent<ForceDirectionIndicator>();
			if ((UnityEngine.Object)(object)forceDirectionIndicator == (UnityEngine.Object)null)
			{
				forceDirectionIndicator = ((Component)this).gameObject.AddComponent<ForceDirectionIndicator>();
			}
			forceDirectionIndicator.Configure(Vector2.up, new Color(0.1f, 0.9f, 0.35f, 0.8f));
		}

		private void OnEnable()
		{
			SetAnimationPlaying(playing: false);
		}

		private void OnDisable()
		{
			if (animationRoutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(animationRoutine);
				animationRoutine = null;
			}
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			if (!(Time.time - lastFireTime < cooldown))
			{
				BallControllerBase componentInParent = ((Component)collision.collider).GetComponentInParent<BallControllerBase>();
				BallController ballController = componentInParent as BallController;
				if (!((UnityEngine.Object)(object)componentInParent == (UnityEngine.Object)null) && (!((UnityEngine.Object)(object)ballController != (UnityEngine.Object)null) || !ballController.IsStoppedAtGoal) && IsTopContact(collision))
				{
					lastFireTime = Time.time;
					componentInParent.Body.AddForce((Vector2)(((Component)this).transform.up * impulse), (ForceMode2D)1);
					PlayAnimation();
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			Gizmos.color = Color.green;
			Gizmos.DrawLine(((Component)this).transform.position, ((Component)this).transform.position + ((Component)this).transform.up);
		}

		private bool IsTopContact(Collision2D collision)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = (Vector2)(((Component)this).transform.up);
			for (int i = 0; i < collision.contactCount; i++)
			{
				ContactPoint2D contact = collision.GetContact(i);
				if (Vector2.Dot(-contact.normal, val) >= minTopContactDot)
				{
					return true;
				}
			}
			return false;
		}

		private void PlayAnimation()
		{
			if (!((UnityEngine.Object)(object)animator == (UnityEngine.Object)null))
			{
				if (animationRoutine != null)
				{
					((MonoBehaviour)this).StopCoroutine(animationRoutine);
				}
				((Behaviour)animator).enabled = true;
				animator.speed = 1f;
				animator.Play("Spring Animation", 0, 0f);
				animator.Update(0f);
				AudioManager.Instance?.PlaySfx(springSfx);
				animationRoutine = ((MonoBehaviour)this).StartCoroutine(StopAnimationAfterDelay());
			}
		}

		private IEnumerator StopAnimationAfterDelay()
		{
			yield return (object)new WaitForSeconds(animationDuration);
			SetAnimationPlaying(playing: false);
			animationRoutine = null;
		}

		private void SetAnimationPlaying(bool playing)
		{
			if ((UnityEngine.Object)(object)animator == (UnityEngine.Object)null)
			{
				return;
			}
			if (playing)
			{
				((Behaviour)animator).enabled = true;
				animator.speed = 1f;
				return;
			}
			animator.speed = 0f;
			animator.Rebind();
			animator.Update(0f);
			((Behaviour)animator).enabled = false;
			if ((UnityEngine.Object)(object)animatedRenderer != (UnityEngine.Object)null && (UnityEngine.Object)(object)idleSprite != (UnityEngine.Object)null)
			{
				animatedRenderer.sprite = idleSprite;
			}
		}
	}
}
