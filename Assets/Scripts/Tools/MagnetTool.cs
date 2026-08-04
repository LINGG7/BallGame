using System.Collections.Generic;
using BallGame.Audio;
using UnityEngine;

namespace BallGame.Tools
{
	public sealed class MagnetTool : MonoBehaviour
	{
		private const string AttractResourcePath = "Audio/SFX/Attract";

		private const string RepulsiveResourcePath = "Audio/SFX/Repulsive";

		[SerializeField]
		private float radius = 3f;

		[SerializeField]
		private float strength = 18f;

		[SerializeField]
		private bool repels;

		[SerializeField]
		private LayerMask affectedLayers = (LayerMask)(-1);

		[SerializeField]
		private SpriteRenderer rangeHintRenderer;

		[SerializeField]
		private bool showRangeHint = true;

		private readonly Collider2D[] hits = (Collider2D[])(object)new Collider2D[8];

		private readonly HashSet<BallControllerBase> ballsInside = new HashSet<BallControllerBase>();

		private readonly HashSet<BallControllerBase> ballsDetectedThisStep = new HashSet<BallControllerBase>();

		private AudioClip attractSfx;

		private AudioClip repulsiveSfx;

		private void Awake()
		{
			attractSfx = Resources.Load<AudioClip>("Audio/SFX/Attract");
			repulsiveSfx = Resources.Load<AudioClip>("Audio/SFX/Repulsive");
			RefreshRangeHint();
		}

		private void OnDisable()
		{
			ClearTrackedBalls();
		}

		private void OnValidate()
		{
			RefreshRangeHint();
		}

		private void FixedUpdate()
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Invalid comparison between Unknown and I4
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			BeginDetectionStep();
			int num = Physics2D.OverlapCircleNonAlloc((Vector2)(((Component)this).transform.position), radius, hits, (int)affectedLayers);
			for (int i = 0; i < num; i++)
			{
				BallControllerBase ballControllerBase = (((UnityEngine.Object)(object)hits[i] != (UnityEngine.Object)null) ? ((Component)hits[i]).GetComponentInParent<BallControllerBase>() : null);
				if ((UnityEngine.Object)(object)ballControllerBase == (UnityEngine.Object)null || (UnityEngine.Object)(object)ballControllerBase.Body == (UnityEngine.Object)null || (int)ballControllerBase.Body.bodyType > 0)
				{
					continue;
				}
				if (RegisterBallForCurrentStep(ballControllerBase))
				{
					PlayEntrySound();
				}
				Vector2 val = (Vector2)(((Component)this).transform.position) - ballControllerBase.Body.position;
				if (!(val.sqrMagnitude < 0.01f))
				{
					if (repels)
					{
						val = -val;
					}
					float num2 = 1f - Mathf.Clamp01(val.magnitude / radius);
					ballControllerBase.Body.AddForce(val.normalized * strength * num2, (ForceMode2D)0);
				}
			}
			CompleteDetectionStep();
		}

		private void BeginDetectionStep()
		{
			ballsDetectedThisStep.Clear();
		}

		private bool RegisterBallForCurrentStep(BallControllerBase ball)
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			return (UnityEngine.Object)(object)ball != (UnityEngine.Object)null && (UnityEngine.Object)(object)ball.Body != (UnityEngine.Object)null && (int)ball.Body.bodyType == 0 && ballsDetectedThisStep.Add(ball) && !ballsInside.Contains(ball);
		}

		private void CompleteDetectionStep()
		{
			ballsInside.RemoveWhere((BallControllerBase ball) => (UnityEngine.Object)(object)ball == (UnityEngine.Object)null || !ballsDetectedThisStep.Contains(ball));
			foreach (BallControllerBase item in ballsDetectedThisStep)
			{
				if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null)
				{
					ballsInside.Add(item);
				}
			}
		}

		private void PlayEntrySound()
		{
			AudioManager.Instance?.PlaySfx(GetEntrySfx());
		}

		private AudioClip GetEntrySfx()
		{
			return repels ? repulsiveSfx : attractSfx;
		}

		private void ClearTrackedBalls()
		{
			ballsInside.Clear();
			ballsDetectedThisStep.Clear();
		}

		private void OnDrawGizmosSelected()
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			Gizmos.color = (repels ? Color.red : Color.magenta);
			Gizmos.DrawWireSphere(((Component)this).transform.position, radius);
		}

		private void RefreshRangeHint()
		{
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)rangeHintRenderer == (UnityEngine.Object)null)
			{
				Transform val = ((Component)this).transform.Find("MagnetRangeHint");
				rangeHintRenderer = (((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ((Component)val).GetComponent<SpriteRenderer>() : null);
			}
			if (!((UnityEngine.Object)(object)rangeHintRenderer == (UnityEngine.Object)null))
			{
				((Renderer)rangeHintRenderer).enabled = showRangeHint;
				Transform transform = ((Component)rangeHintRenderer).transform;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				Sprite sprite = rangeHintRenderer.sprite;
				if ((UnityEngine.Object)(object)sprite == (UnityEngine.Object)null)
				{
					transform.localScale = Vector3.one * (radius * 2f);
					return;
				}
				Bounds bounds = sprite.bounds;
				Vector2 val2 = (Vector2)(bounds.size);
				float num = ((val2.x > 0f) ? (radius * 2f / val2.x) : 1f);
				float num2 = ((val2.y > 0f) ? (radius * 2f / val2.y) : 1f);
				transform.localScale = new Vector3(num, num2, 1f);
			}
		}
	}
}
