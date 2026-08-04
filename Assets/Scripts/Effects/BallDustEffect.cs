using System;
using UnityEngine;
using Random = UnityEngine.Random;
using EmitParams = UnityEngine.ParticleSystem.EmitParams;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using MinMaxCurve = UnityEngine.ParticleSystem.MinMaxCurve;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;
using EmissionModule = UnityEngine.ParticleSystem.EmissionModule;
using Burst = UnityEngine.ParticleSystem.Burst;
using ShapeModule = UnityEngine.ParticleSystem.ShapeModule;
using SizeOverLifetimeModule = UnityEngine.ParticleSystem.SizeOverLifetimeModule;
using ColorOverLifetimeModule = UnityEngine.ParticleSystem.ColorOverLifetimeModule;

namespace BallGame.Effects
{
	[DisallowMultipleComponent]
	public sealed class BallDustEffect : MonoBehaviour
	{
		private const string DustMaterialResourcePath = "Materials/BallDustParticle";

		[Header("Particle Systems")]
		[SerializeField]
		private ParticleSystem impactDust;

		[SerializeField]
		private ParticleSystem rollingDust;

		[SerializeField]
		private Material dustMaterial;

		[Header("Trigger Speeds")]
		[SerializeField]
		[Min(0f)]
		private float minimumRollingSpeed = 0.75f;

		[SerializeField]
		[Min(0f)]
		private float minimumImpactSpeed = 1.75f;

		[SerializeField]
		[Min(0.01f)]
		private float fullIntensitySpeed = 12f;

		[Header("Impact Dust")]
		[SerializeField]
		[Min(0f)]
		private int minimumImpactParticles = 5;

		[SerializeField]
		[Min(0f)]
		private int maximumImpactParticles = 16;

		[SerializeField]
		[Min(0f)]
		private float minimumImpactVelocity = 0.7f;

		[SerializeField]
		[Min(0f)]
		private float maximumImpactVelocity = 2.2f;

		[Header("Rolling Dust")]
		[SerializeField]
		[Min(0f)]
		private float minimumEmissionRate = 8f;

		[SerializeField]
		[Min(0f)]
		private float maximumEmissionRate = 34f;

		[SerializeField]
		[Min(0f)]
		private float minimumRollingVelocity = 0.3f;

		[SerializeField]
		[Min(0f)]
		private float maximumRollingVelocity = 0.9f;

		private float rollingEmissionAccumulator;

		private Material runtimeMaterial;

		internal float MinimumRollingSpeed => minimumRollingSpeed;

		private void Awake()
		{
			ClampSettings();
			EnsureParticleSystems();
			ConfigureParticleSystems();
		}

		private void OnValidate()
		{
			ClampSettings();
			if (Application.isPlaying)
			{
				EnsureParticleSystems();
				ConfigureParticleSystems();
			}
		}

		internal void EmitImpact(Vector2 contactPoint, Vector2 outwardNormal, float relativeSpeed)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			EnsureParticleSystems();
			int num = CalculateImpactParticleCount(relativeSpeed, minimumImpactSpeed, fullIntensitySpeed, minimumImpactParticles, maximumImpactParticles);
			if (num > 0 && !((UnityEngine.Object)(object)impactDust == (UnityEngine.Object)null))
			{
				Vector2 val = NormalizeOrUp(outwardNormal);
				Vector2 val2 = default(Vector2);
				val2 = new Vector2(0f - val.y, val.x);
				for (int i = 0; i < num; i++)
				{
					float num2 = Random.Range(minimumImpactVelocity, maximumImpactVelocity);
					float num3 = Random.Range((0f - maximumImpactVelocity) * 0.65f, maximumImpactVelocity * 0.65f);
					EmitParams val3 = default(EmitParams);
					val3.position = (Vector2)(contactPoint + val * 0.025f);
					val3.velocity = (Vector2)(val * num2 + val2 * num3);
					EmitParams val4 = val3;
					impactDust.Emit(val4, 1);
				}
			}
		}

		internal void EmitRolling(Vector2 contactPoint, Vector2 outwardNormal, Vector2 tangentialVelocity, float deltaTime)
		{
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_015b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0168: Unknown result type (might be due to invalid IL or missing references)
			//IL_0170: Unknown result type (might be due to invalid IL or missing references)
			EnsureParticleSystems();
			float magnitude = tangentialVelocity.magnitude;
			float num = CalculateEmissionRate(magnitude, minimumRollingSpeed, fullIntensitySpeed, minimumEmissionRate, maximumEmissionRate);
			if (num <= 0f || (UnityEngine.Object)(object)rollingDust == (UnityEngine.Object)null || deltaTime <= 0f)
			{
				rollingEmissionAccumulator = 0f;
				return;
			}
			rollingEmissionAccumulator += num * deltaTime;
			int num2 = Mathf.Min(Mathf.FloorToInt(rollingEmissionAccumulator), 8);
			rollingEmissionAccumulator -= num2;
			Vector2 val = NormalizeOrUp(outwardNormal);
			Vector2 val2 = ((magnitude > 0.001f) ? (-tangentialVelocity / magnitude) : Vector2.zero);
			Vector2 val3 = default(Vector2);
			val3 = new Vector2(0f - val.y, val.x);
			for (int i = 0; i < num2; i++)
			{
				float num3 = Random.Range(minimumRollingVelocity, maximumRollingVelocity);
				Vector2 val4 = val * num3 + val2 * Random.Range(0.15f, 0.55f) + val3 * Random.Range(-0.15f, 0.15f);
				EmitParams val5 = default(EmitParams);
				val5.position = (Vector2)(contactPoint + val * 0.02f);
				val5.velocity = (Vector2)(val4);
				EmitParams val6 = val5;
				rollingDust.Emit(val6, 1);
			}
		}

		internal void StopEmission(bool clearParticles)
		{
			rollingEmissionAccumulator = 0f;
			if (clearParticles)
			{
				StopAndClear(impactDust);
				StopAndClear(rollingDust);
			}
		}

		private void EnsureParticleSystems()
		{
			impactDust = FindOrCreateParticleSystem("ImpactDust", impactDust);
			rollingDust = FindOrCreateParticleSystem("RollingDust", rollingDust);
		}

		private ParticleSystem FindOrCreateParticleSystem(string childName, ParticleSystem current)
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)current != (UnityEngine.Object)null)
			{
				return current;
			}
			Transform val = ((Component)this).transform.Find(childName);
			ParticleSystem val2 = (((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ((Component)val).GetComponent<ParticleSystem>() : null);
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				return val2;
			}
			GameObject val3 = new GameObject(childName);
			val3.transform.SetParent(((Component)this).transform, false);
			return val3.AddComponent<ParticleSystem>();
		}

		private void ConfigureParticleSystems()
		{
			ConfigureParticleSystem(impactDust, impact: true, 96);
			ConfigureParticleSystem(rollingDust, impact: false, 160);
			Material val = ResolveMaterial();
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				((Renderer)((Component)impactDust).GetComponent<ParticleSystemRenderer>()).sharedMaterial = val;
				((Renderer)((Component)rollingDust).GetComponent<ParticleSystemRenderer>()).sharedMaterial = val;
			}
		}

		private static void ConfigureParticleSystem(ParticleSystem effect, bool impact, int maxParticles)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_014f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0170: Unknown result type (might be due to invalid IL or missing references)
			//IL_0175: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_018b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b7: Expected O, but got Unknown
			//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e3: Expected O, but got Unknown
			//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0212: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0221: Unknown result type (might be due to invalid IL or missing references)
			//IL_0238: Unknown result type (might be due to invalid IL or missing references)
			//IL_023d: Unknown result type (might be due to invalid IL or missing references)
			//IL_024e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0253: Unknown result type (might be due to invalid IL or missing references)
			//IL_0264: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_0278: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)effect == (UnityEngine.Object)null))
			{
				MainModule main = effect.main;
				main.playOnAwake = false;
				main.loop = false;
				main.simulationSpace = (ParticleSystemSimulationSpace)1;
				main.maxParticles = maxParticles;
				main.startLifetime = (impact ? new MinMaxCurve(0.35f, 0.75f) : new MinMaxCurve(0.45f, 0.9f));
				main.startSize = (impact ? new MinMaxCurve(0.08f, 0.22f) : new MinMaxCurve(0.06f, 0.16f));
				main.startSpeed = 0f;
				main.startRotation = new MinMaxCurve(0f, MathF.PI * 2f);
				main.startColor = new MinMaxGradient(new Color(0.72f, 0.72f, 0.72f, 0.78f), new Color(0.34f, 0.34f, 0.34f, 0.62f));
				main.gravityModifier = -0.035f;
				EmissionModule emission = effect.emission;
				emission.enabled = false;
				emission.rateOverTime = 0f;
				emission.SetBursts(Array.Empty<Burst>());
				ShapeModule shape = effect.shape;
				shape.enabled = false;
				SizeOverLifetimeModule sizeOverLifetime = effect.sizeOverLifetime;
				sizeOverLifetime.enabled = true;
				AnimationCurve val = new AnimationCurve((Keyframe[])(object)new Keyframe[3]
				{
					new Keyframe(0f, 0.62f),
					new Keyframe(0.28f, 1f),
					new Keyframe(1f, impact ? 1.55f : 1.35f)
				});
				sizeOverLifetime.size = new MinMaxCurve(1f, val);
				ColorOverLifetimeModule colorOverLifetime = effect.colorOverLifetime;
				colorOverLifetime.enabled = true;
				Gradient val2 = new Gradient();
				val2.SetKeys((GradientColorKey[])(object)new GradientColorKey[2]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(new Color(0.82f, 0.82f, 0.82f), 1f)
				}, (GradientAlphaKey[])(object)new GradientAlphaKey[3]
				{
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(1f, 0.12f),
					new GradientAlphaKey(0f, 1f)
				});
				colorOverLifetime.color = val2;
				ParticleSystemRenderer component = ((Component)effect).GetComponent<ParticleSystemRenderer>();
				component.renderMode = (ParticleSystemRenderMode)0;
				component.alignment = (ParticleSystemRenderSpace)0;
				((Renderer)component).sortingOrder = 15;
			}
		}

		private Material ResolveMaterial()
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)dustMaterial != (UnityEngine.Object)null)
			{
				return dustMaterial;
			}
			dustMaterial = Resources.Load<Material>("Materials/BallDustParticle");
			if ((UnityEngine.Object)(object)dustMaterial != (UnityEngine.Object)null)
			{
				return dustMaterial;
			}
			Shader val = Shader.Find("BallGame/Soft Dust Particle");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return null;
			}
			runtimeMaterial = new Material(val)
			{
				name = "Runtime Ball Dust Material"
			};
			return runtimeMaterial;
		}

		private void ClampSettings()
		{
			minimumRollingSpeed = Mathf.Max(0f, minimumRollingSpeed);
			minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
			fullIntensitySpeed = Mathf.Max(Mathf.Max(minimumRollingSpeed, minimumImpactSpeed) + 0.01f, fullIntensitySpeed);
			minimumImpactParticles = Mathf.Max(0, minimumImpactParticles);
			maximumImpactParticles = Mathf.Max(minimumImpactParticles, maximumImpactParticles);
			minimumImpactVelocity = Mathf.Max(0f, minimumImpactVelocity);
			maximumImpactVelocity = Mathf.Max(minimumImpactVelocity, maximumImpactVelocity);
			minimumEmissionRate = Mathf.Max(0f, minimumEmissionRate);
			maximumEmissionRate = Mathf.Max(minimumEmissionRate, maximumEmissionRate);
			minimumRollingVelocity = Mathf.Max(0f, minimumRollingVelocity);
			maximumRollingVelocity = Mathf.Max(minimumRollingVelocity, maximumRollingVelocity);
		}

		private static int CalculateImpactParticleCount(float speed, float threshold, float fullIntensitySpeed, int minimumCount, int maximumCount)
		{
			minimumCount = Mathf.Max(0, minimumCount);
			maximumCount = Mathf.Max(minimumCount, maximumCount);
			if (speed < Mathf.Max(0f, threshold))
			{
				return 0;
			}
			float num = Mathf.Max(0.01f, fullIntensitySpeed - threshold);
			float num2 = Mathf.Clamp01((speed - threshold) / num);
			return Mathf.RoundToInt(Mathf.Lerp((float)minimumCount, (float)maximumCount, num2));
		}

		private static float CalculateEmissionRate(float speed, float threshold, float fullIntensitySpeed, float minimumRate, float maximumRate)
		{
			minimumRate = Mathf.Max(0f, minimumRate);
			maximumRate = Mathf.Max(minimumRate, maximumRate);
			if (speed < Mathf.Max(0f, threshold))
			{
				return 0f;
			}
			float num = Mathf.Max(0.01f, fullIntensitySpeed - threshold);
			float num2 = Mathf.Clamp01((speed - threshold) / num);
			return Mathf.Lerp(minimumRate, maximumRate, num2);
		}

		private static Vector2 NormalizeOrUp(Vector2 direction)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			return (direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector2.up;
		}

		private static void StopAndClear(ParticleSystem effect)
		{
			if ((UnityEngine.Object)(object)effect != (UnityEngine.Object)null)
			{
				effect.Stop(true, (ParticleSystemStopBehavior)0);
			}
		}

		private void OnDestroy()
		{
			if ((UnityEngine.Object)(object)runtimeMaterial != (UnityEngine.Object)null)
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)runtimeMaterial);
			}
		}
	}
}
