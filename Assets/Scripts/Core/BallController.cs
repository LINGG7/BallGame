using System.Collections.Generic;
using BallGame.Audio;
using BallGame.Effects;
using BallGame.Tools;
using UnityEngine;

namespace BallGame.Core
{
	[RequireComponent(typeof(Rigidbody2D))]
	[RequireComponent(typeof(Collider2D))]
	[RequireComponent(typeof(BallDustEffect))]
	public sealed class BallController : BallControllerBase
	{
		private struct RollingContact
		{
			public Vector2 point;

			public Vector2 normal;

			public Vector2 tangentialVelocity;

			public float TangentialSpeedSquared => tangentialVelocity.sqrMagnitude;
		}

		private const float MinimumHitSpeed = 1.75f;

		private const float FullHitSpeed = 12f;

		private const float MinimumHitVolume = 0.2f;

		private const float HitSoundCooldown = 0.08f;

		[SerializeField]
		private float maxSpeed = 14f;

		[SerializeField]
		[Min(0f)]
		private float minimumHapticSpeed = 0.75f;

		private Vector3 startPosition;

		private readonly Dictionary<Collider2D, RollingContact> rollingContacts = new Dictionary<Collider2D, RollingContact>();

		private BallDustEffect dustEffect;

		private float lastHitSoundTime = float.NegativeInfinity;

		public bool IsStoppedAtGoal { get; private set; }

		protected override void Awake()
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			base.Awake();
            if (GetComponent<BallTrailEffect>() == null) gameObject.AddComponent<BallTrailEffect>();
			dustEffect = ((Component)this).GetComponent<BallDustEffect>();
			startPosition = ((Component)this).transform.position;
			SetBuildMode();
		}

		private void FixedUpdate()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Invalid comparison between Unknown and I4
			//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			if ((int)base.Body.bodyType > 0)
			{
				StopRollingAudio();
				return;
			}
			Vector2 velocity = base.Body.velocity;
			if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
			{
				Rigidbody2D obj = base.Body;
				velocity = base.Body.velocity;
				obj.velocity = velocity.normalized * maxSpeed;
			}
			if (Time.frameCount % 15 == 0)
			{
				velocity = base.Body.velocity;
				float magnitude = velocity.magnitude;
				float num = MobileHaptics.NormalizeRollingSpeed(magnitude, minimumHapticSpeed, maxSpeed);
			}
			RemoveDestroyedContacts();
			RollingContact bestContact = default(RollingContact);
			bool flag = !IsStoppedAtGoal && TryGetBestRollingContact(out bestContact);
			if (flag)
			{
				dustEffect?.EmitRolling(bestContact.point, bestContact.normal, bestContact.tangentialVelocity, Time.fixedDeltaTime);
			}
			else
			{
				dustEffect?.StopEmission(clearParticles: false);
			}
			float speed = (flag ? Mathf.Sqrt(bestContact.TangentialSpeedSquared) : 0f);
			float normalizedSpeed = MobileHaptics.NormalizeRollingSpeed(speed, minimumHapticSpeed, maxSpeed);
			if (ShouldReportRollingAudio((int)base.Body.bodyType == 0, IsStoppedAtGoal, flag, normalizedSpeed))
			{
				AudioManager.Instance?.ReportRolling((UnityEngine.Object)(object)this, normalizedSpeed);
			}
			else
			{
				StopRollingAudio();
			}
			if (!IsStoppedAtGoal && rollingContacts.Count > 0)
			{
				velocity = base.Body.velocity;
				float normalizedSpeed2 = MobileHaptics.NormalizeRollingSpeed(velocity.magnitude, minimumHapticSpeed, maxSpeed);
				MobileHaptics.ReportRolling(normalizedSpeed2);
			}
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			if (AddRollingContact(collision, out var bestContact))
			{
				BallDustEffect ballDustEffect = dustEffect;
				if (ballDustEffect != null)
				{
					Vector2 point = bestContact.point;
					Vector2 normal = bestContact.normal;
					Vector2 relativeVelocity = collision.relativeVelocity;
					ballDustEffect.EmitImpact(point, normal, relativeVelocity.magnitude);
				}
			}
			TryPlayHitSound(collision);
		}

		private void OnCollisionStay2D(Collision2D collision)
		{
			AddRollingContact(collision, out var _);
		}

		private void OnCollisionExit2D(Collision2D collision)
		{
			Collider2D externalCollider = GetExternalCollider(collision);
			if ((UnityEngine.Object)(object)externalCollider != (UnityEngine.Object)null)
			{
				rollingContacts.Remove(externalCollider);
			}
			if (rollingContacts.Count == 0)
			{
				dustEffect?.StopEmission(clearParticles: false);
				StopRollingAudio();
			}
		}

		private void OnDisable()
		{
			rollingContacts.Clear();
			dustEffect?.StopEmission(clearParticles: true);
            GetComponent<BallTrailEffect>()?.Clear();
			StopRollingAudio();
		}

		public void SetStart(Vector2 position)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			startPosition = (Vector2)(position);
			ResetBall();
		}

		public void SetBuildMode()
		{
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			IsStoppedAtGoal = false;
			rollingContacts.Clear();
			dustEffect?.StopEmission(clearParticles: true);
            GetComponent<BallTrailEffect>()?.Clear();
			StopRollingAudio();
			base.Body.bodyType = (RigidbodyType2D)1;
			base.Body.simulated = true;
			base.Body.velocity = Vector2.zero;
			base.Body.angularVelocity = 0f;
		}

		public void Launch()
		{
			IsStoppedAtGoal = false;
			base.Body.bodyType = (RigidbodyType2D)0;
			base.Body.WakeUp();
		}

		public void StopAtGoal()
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			IsStoppedAtGoal = true;
			rollingContacts.Clear();
			dustEffect?.StopEmission(clearParticles: true);
            GetComponent<BallTrailEffect>()?.Clear();
			StopRollingAudio();
			base.Body.velocity = Vector2.zero;
			base.Body.angularVelocity = 0f;
			base.Body.bodyType = (RigidbodyType2D)1;
			base.Body.simulated = true;
		}

		public void ResetBall()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			rollingContacts.Clear();
			((Component)this).transform.position = startPosition;
			((Component)this).transform.rotation = Quaternion.identity;
			base.Body.velocity = Vector2.zero;
			base.Body.angularVelocity = 0f;
			SetBuildMode();
		}

		private bool AddRollingContact(Collision2D collision, out RollingContact bestContact)
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			bestContact = default(RollingContact);
			Collider2D externalCollider = GetExternalCollider(collision);
			if ((UnityEngine.Object)(object)externalCollider == (UnityEngine.Object)null || externalCollider.isTrigger || collision == null || collision.contactCount == 0)
			{
				return false;
			}
			Rigidbody2D attachedRigidbody = externalCollider.attachedRigidbody;
			Vector2 val;
			if (!((UnityEngine.Object)(object)attachedRigidbody != (UnityEngine.Object)null) || !((UnityEngine.Object)(object)attachedRigidbody != (UnityEngine.Object)(object)base.Body))
			{
				val = Vector2.zero;
			}
			else
			{
				ContactPoint2D contact = collision.GetContact(0);
				val = attachedRigidbody.GetPointVelocity(contact.point);
			}
			Vector2 val2 = val;
			Vector2 val3 = base.Body.velocity - val2;
			bool flag = false;
			for (int i = 0; i < collision.contactCount; i++)
			{
				ContactPoint2D contact2 = collision.GetContact(i);
				Vector2 outwardNormal = GetOutwardNormal(contact2.point, contact2.normal);
				Vector2 tangentialVelocity = val3 - Vector2.Dot(val3, outwardNormal) * outwardNormal;
				RollingContact rollingContact = new RollingContact
				{
					point = contact2.point,
					normal = outwardNormal,
					tangentialVelocity = tangentialVelocity
				};
				if (ShouldReplaceBestContact(rollingContact.TangentialSpeedSquared, bestContact.TangentialSpeedSquared, flag))
				{
					bestContact = rollingContact;
					flag = true;
				}
			}
			if (flag)
			{
				rollingContacts[externalCollider] = bestContact;
			}
			return flag;
		}

		private Collider2D GetExternalCollider(Collision2D collision)
		{
			if (collision == null)
			{
				return null;
			}
			Collider2D val = collision.collider;
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && ((Component)val).transform.IsChildOf(((Component)this).transform))
			{
				val = collision.otherCollider;
			}
			return val;
		}

		private void RemoveDestroyedContacts()
		{
			if (rollingContacts.Count == 0)
			{
				return;
			}
			List<Collider2D> list = null;
			foreach (KeyValuePair<Collider2D, RollingContact> rollingContact in rollingContacts)
			{
				Collider2D key = rollingContact.Key;
				if (!((UnityEngine.Object)(object)key != (UnityEngine.Object)null) || !((Behaviour)key).enabled || key.isTrigger)
				{
					if (list == null)
					{
						list = new List<Collider2D>();
					}
					list.Add(key);
				}
			}
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					rollingContacts.Remove(list[i]);
				}
			}
		}

		private bool TryGetBestRollingContact(out RollingContact bestContact)
		{
			bestContact = default(RollingContact);
			float num = (((UnityEngine.Object)(object)dustEffect != (UnityEngine.Object)null) ? dustEffect.MinimumRollingSpeed : minimumHapticSpeed);
			float num2 = num * num;
			bool flag = false;
			foreach (KeyValuePair<Collider2D, RollingContact> rollingContact in rollingContacts)
			{
				RollingContact value = rollingContact.Value;
				if (!(value.TangentialSpeedSquared < num2) && ShouldReplaceBestContact(value.TangentialSpeedSquared, bestContact.TangentialSpeedSquared, flag))
				{
					bestContact = value;
					flag = true;
				}
			}
			return flag;
		}

		private static bool ShouldReplaceBestContact(float candidateSpeedSquared, float bestSpeedSquared, bool hasBest)
		{
			return !hasBest || candidateSpeedSquared > bestSpeedSquared;
		}

		private static bool ShouldReportRollingAudio(bool isDynamic, bool isStoppedAtGoal, bool hasRollingContact, float normalizedSpeed)
		{
			return isDynamic && !isStoppedAtGoal && hasRollingContact && normalizedSpeed > 0f;
		}

		private void TryPlayHitSound(Collision2D collision)
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Invalid comparison between Unknown and I4
			Collider2D externalCollider = GetExternalCollider(collision);
			if (collision == null || (UnityEngine.Object)(object)externalCollider == (UnityEngine.Object)null || collision.contactCount == 0)
			{
				return;
			}
			BallController componentInParent = ((Component)externalCollider).GetComponentInParent<BallController>();
			bool isDuplicateBallHit = (UnityEngine.Object)(object)componentInParent != (UnityEngine.Object)null && !ShouldThisBallPlayForPair(((UnityEngine.Object)this).GetInstanceID(), ((UnityEngine.Object)componentInParent).GetInstanceID());
			float impactSpeed = CalculateMaximumNormalImpactSpeed(collision);
			if (ShouldPlayHitSound((int)base.Body.bodyType == 0, IsStoppedAtGoal, externalCollider.isTrigger, IsSpringCollider(externalCollider), isDuplicateBallHit, impactSpeed, Time.time, lastHitSoundTime))
			{
				AudioManager instance = AudioManager.Instance;
				if (!((UnityEngine.Object)(object)instance == (UnityEngine.Object)null))
				{
					instance.PlayBallHit(CalculateHitVolume(impactSpeed));
					lastHitSoundTime = Time.time;
				}
			}
		}

		private static float CalculateMaximumNormalImpactSpeed(Collision2D collision)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			float num = 0f;
			Vector2 relativeVelocity = collision.relativeVelocity;
			for (int i = 0; i < collision.contactCount; i++)
			{
				float num2 = num;
				ContactPoint2D contact = collision.GetContact(i);
				num = Mathf.Max(num2, CalculateNormalImpactSpeed(relativeVelocity, contact.normal));
			}
			return num;
		}

		private static float CalculateNormalImpactSpeed(Vector2 relativeVelocity, Vector2 contactNormal)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			if (contactNormal.sqrMagnitude <= 0.0001f)
			{
				return 0f;
			}
			return Mathf.Abs(Vector2.Dot(relativeVelocity, contactNormal.normalized));
		}

		private static float CalculateHitVolume(float impactSpeed)
		{
			if (impactSpeed < 1.75f)
			{
				return 0f;
			}
			float num = Mathf.InverseLerp(1.75f, 12f, impactSpeed);
			return Mathf.Lerp(0.2f, 1f, num);
		}

		private static bool ShouldPlayHitSound(bool isDynamic, bool isStoppedAtGoal, bool isTrigger, bool isSpring, bool isDuplicateBallHit, float impactSpeed, float currentTime, float lastPlayTime)
		{
			return isDynamic && !isStoppedAtGoal && !isTrigger && !isSpring && !isDuplicateBallHit && impactSpeed >= 1.75f && currentTime - lastPlayTime >= 0.08f;
		}

		private static bool ShouldThisBallPlayForPair(int thisInstanceId, int otherInstanceId)
		{
			return thisInstanceId < otherInstanceId;
		}

		private static bool IsSpringCollider(Collider2D collider)
		{
			return (UnityEngine.Object)(object)collider != (UnityEngine.Object)null && ((UnityEngine.Object)(object)((Component)collider).GetComponentInParent<SpringPad>() != (UnityEngine.Object)null || (UnityEngine.Object)(object)((Component)collider).GetComponentInParent<SpringPadFree>() != (UnityEngine.Object)null);
		}

		private void StopRollingAudio()
		{
			AudioManager.Instance?.StopRolling((UnityEngine.Object)(object)this);
		}

		private Vector2 GetOutwardNormal(Vector2 contactPoint, Vector2 contactNormal)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = ((contactNormal.sqrMagnitude > 0.0001f) ? contactNormal.normalized : Vector2.up);
			Vector2 val2 = (Vector2)(((Component)this).transform.position) - contactPoint;
			if (Vector2.Dot(val, val2) < 0f)
			{
				val = -val;
			}
			return val;
		}
	}
}
