using System.Collections.Generic;
using BallGame.Audio;
using BallGame.Core;
using BallGame.UI;
using UnityEngine;

namespace BallGame.Tools
{
	public sealed class FanZone : MonoBehaviour
	{
		private const string AirWooshResourcePath = "Audio/SFX/AirWoosh";

		[SerializeField]
		private float range = 2.5f;

		[SerializeField]
		private float width = 1.6f;

		[SerializeField]
		private float force = 12f;

		[SerializeField]
		private LayerMask affectedLayers = (LayerMask)(-1);

		[SerializeField]
		private Animator animator;

		[SerializeField]
		private SpriteRenderer animatedRenderer;

		[SerializeField]
		private ParticleSystem airFlowEffect;

		[SerializeField]
		private ParticleSystem airFlowEffect1;

		[SerializeField]
		private ParticleSystem airFlowEffect2;

		private readonly Collider2D[] hits = (Collider2D[])(object)new Collider2D[8];

		private readonly HashSet<BallControllerBase> ballsInside = new HashSet<BallControllerBase>();

		private readonly HashSet<BallControllerBase> ballsDetectedThisStep = new HashSet<BallControllerBase>();

		private Collider2D sourceCollider;

		private GameManager gameManager;

		private Sprite idleSprite;

		private AudioClip airWooshSfx;

		private void Awake()
		{
			EnsureDirectionIndicator();
			airWooshSfx = Resources.Load<AudioClip>("Audio/SFX/AirWoosh");
			sourceCollider = ((Component)this).GetComponent<Collider2D>();
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
			if ((UnityEngine.Object)(object)airFlowEffect == (UnityEngine.Object)null)
			{
				Transform val = ((Component)this).transform.Find("AirFlowEffect");
				airFlowEffect = (((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ((Component)val).GetComponent<ParticleSystem>() : ((Component)this).GetComponentInChildren<ParticleSystem>());
			}
			if ((UnityEngine.Object)(object)airFlowEffect1 == (UnityEngine.Object)null)
			{
				Transform val2 = ((Component)this).transform.Find("AirFlowEffect1");
				airFlowEffect1 = (((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? ((Component)val2).GetComponent<ParticleSystem>() : ((Component)this).GetComponentInChildren<ParticleSystem>());
			}
			if ((UnityEngine.Object)(object)airFlowEffect2 == (UnityEngine.Object)null)
			{
				Transform val3 = ((Component)this).transform.Find("AirFlowEffect2");
				airFlowEffect2 = (((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null) ? ((Component)val3).GetComponent<ParticleSystem>() : ((Component)this).GetComponentInChildren<ParticleSystem>());
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
			forceDirectionIndicator.Configure(Vector2.right, new Color(0.25f, 0.7f, 1f, 0.8f));
		}

		private void OnEnable()
		{
			gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
			if ((UnityEngine.Object)(object)gameManager != (UnityEngine.Object)null)
			{
				gameManager.StateChanged += HandleGameStateChanged;
				HandleGameStateChanged(gameManager.State);
			}
			else
			{
				SetAnimationPlaying(playing: false);
			}
		}

		private void OnDisable()
		{
			ClearTrackedBalls();
			if ((UnityEngine.Object)(object)gameManager != (UnityEngine.Object)null)
			{
				gameManager.StateChanged -= HandleGameStateChanged;
				gameManager = null;
			}
		}

		private void FixedUpdate()
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Invalid comparison between Unknown and I4
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			BeginDetectionStep();
			Vector2 val = (Vector2)(((Component)this).transform.right);
			Vector2 windBoxCenter = GetWindBoxCenter(val);
			int num = Physics2D.OverlapBoxNonAlloc(windBoxCenter, new Vector2(range, width), ((Component)this).transform.eulerAngles.z, hits, (int)affectedLayers);
			for (int i = 0; i < num; i++)
			{
				Collider2D val2 = hits[i];
				if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null || (UnityEngine.Object)(object)val2 == (UnityEngine.Object)(object)sourceCollider)
				{
					continue;
				}
				BallControllerBase componentInParent = ((Component)val2).GetComponentInParent<BallControllerBase>();
				if ((UnityEngine.Object)(object)componentInParent != (UnityEngine.Object)null && (int)componentInParent.Body.bodyType == 0)
				{
					if (RegisterBallForCurrentStep(componentInParent))
					{
						PlayAirWoosh();
					}
					componentInParent.Body.AddForce(val * force, (ForceMode2D)0);
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

		private void PlayAirWoosh()
		{
			AudioManager.Instance?.PlaySfx(airWooshSfx);
		}

		private void ClearTrackedBalls()
		{
			ballsInside.Clear();
			ballsDetectedThisStep.Clear();
		}

		private void OnDrawGizmosSelected()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = (Vector2)(((Component)this).transform.right);
			Vector2 windBoxCenter = GetWindBoxCenter(val);
			Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.45f);
			Matrix4x4 matrix = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS((Vector2)(windBoxCenter), Quaternion.Euler(0f, 0f, ((Component)this).transform.eulerAngles.z), Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(range, width, 0f));
			Gizmos.matrix = matrix;
			Gizmos.DrawLine(((Component)this).transform.position, ((Component)this).transform.position + (Vector3)(val * (GetEmitterHalfDepth() + range)));
		}

		private Vector2 GetWindBoxCenter(Vector2 forward)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			return (Vector2)(((Component)this).transform.position) + forward * (GetEmitterHalfDepth() + range * 0.5f);
		}

		private float GetEmitterHalfDepth()
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			Collider2D obj = sourceCollider;
			BoxCollider2D val = (BoxCollider2D)(object)((obj is BoxCollider2D) ? obj : null);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = ((Component)this).GetComponent<BoxCollider2D>();
			}
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return 0f;
			}
			return Mathf.Abs(val.size.x * ((Component)this).transform.lossyScale.x) * 0.5f;
		}

		private void HandleGameStateChanged(GameState state)
		{
			if (state != GameState.Simulate)
			{
				ClearTrackedBalls();
			}
			SetAnimationPlaying(state == GameState.Simulate);
		}

		private void SetAnimationPlaying(bool playing)
		{
			if (playing)
			{
				if ((UnityEngine.Object)(object)animator != (UnityEngine.Object)null)
				{
					((Behaviour)animator).enabled = true;
					animator.speed = 1f;
				}
				if ((UnityEngine.Object)(object)airFlowEffect != (UnityEngine.Object)null && !airFlowEffect.isPlaying)
				{
					airFlowEffect.Play();
				}
				if ((UnityEngine.Object)(object)airFlowEffect1 != (UnityEngine.Object)null && !airFlowEffect1.isPlaying)
				{
					airFlowEffect1.Play();
				}
				if ((UnityEngine.Object)(object)airFlowEffect2 != (UnityEngine.Object)null && !airFlowEffect2.isPlaying)
				{
					airFlowEffect2.Play();
				}
				return;
			}
			if ((UnityEngine.Object)(object)animator != (UnityEngine.Object)null)
			{
				animator.speed = 0f;
				animator.Rebind();
				animator.Update(0f);
				((Behaviour)animator).enabled = false;
			}
			if ((UnityEngine.Object)(object)airFlowEffect != (UnityEngine.Object)null)
			{
				airFlowEffect.Stop(true, (ParticleSystemStopBehavior)0);
			}
			if ((UnityEngine.Object)(object)airFlowEffect1 != (UnityEngine.Object)null)
			{
				airFlowEffect1.Stop(true, (ParticleSystemStopBehavior)0);
			}
			if ((UnityEngine.Object)(object)airFlowEffect2 != (UnityEngine.Object)null)
			{
				airFlowEffect2.Stop(true, (ParticleSystemStopBehavior)0);
			}
			if ((UnityEngine.Object)(object)animatedRenderer != (UnityEngine.Object)null && (UnityEngine.Object)(object)idleSprite != (UnityEngine.Object)null)
			{
				animatedRenderer.sprite = idleSprite;
			}
		}
	}
}
