using System.Collections;
using System.Collections.Generic;
using BallGame.Audio;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallGame.Effects
{
    public sealed class FireworkEffect : MonoBehaviour
    {
        private const string FireworkSfxResourcePath = "Audio/SFX/Firework";
        private const string FireworkPreSfxResourcePath = "Audio/SFX/FireworkPre";

        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool destroyAfterFinished = true;
        [Header("Lift")]
        [Tooltip("When enabled, the firework shell rises before the explosion plays.")]
        [SerializeField] private bool playLiftBeforeExplosion = true;
        [Min(0.1f)]
        [Tooltip("Seconds to wait before the explosion starts.")]
        [SerializeField] private float liftDuration = 0.8f;
        [Min(0.1f)]
        [Tooltip("World units below the explosion point where LiftShell starts.")]
        [SerializeField] private float liftHeight = 1.35f;
        [SerializeField] private ParticleSystem[] liftSystems;
        [Header("Explosion")]
        [SerializeField, HideInInspector] private ParticleSystem liftSystem;
        [FormerlySerializedAs("particleSystems")]
        [SerializeField] private ParticleSystem[] explosionSystems;

        private Coroutine explosionRoutine;
        private float destroyAt = -1f;
        private AudioClip fireworkSfx;
        private AudioClip fireworkPreSfx;
        private int lastLiftSoundFrame = -1;

        private void Awake()
        {
            fireworkSfx = Resources.Load<AudioClip>(FireworkSfxResourcePath);
            fireworkPreSfx = Resources.Load<AudioClip>(FireworkPreSfxResourcePath);
            CacheParticleSystems();
        }

        private void OnValidate()
        {
            CacheParticleSystems();
            StopAndClear(liftSystems);
            ApplyLiftHeight();
        }

        private void OnEnable()
        {
            if (playOnAwake)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!destroyAfterFinished || destroyAt < 0f || Time.time < destroyAt)
            {
                return;
            }

            Destroy(gameObject);
        }

        public void Play()
        {
            CacheParticleSystems();
            if (explosionSystems.Length == 0)
            {
                Debug.LogWarning("FireworkEffect has no explosion ParticleSystems. Rebuild Assets/Resources/Prefabs/Firework.prefab from Ball Game/Effects/Rebuild Firework Prefab.", this);
                return;
            }

            if (explosionRoutine != null)
            {
                StopCoroutine(explosionRoutine);
                explosionRoutine = null;
            }

            StopAndClear(liftSystems);
            StopAndClear(explosionSystems);
            ApplyLiftHeight();

            float explosionLifetime = GetLongestLifetime(explosionSystems);
            if (CanPlayLift())
            {
                PlayLift();
                explosionRoutine = StartCoroutine(PlayExplosionAfterDelay(liftDuration));
                destroyAt = destroyAfterFinished ? Time.time + liftDuration + explosionLifetime : -1f;
                return;
            }

            PlayExplosion();
            destroyAt = destroyAfterFinished ? Time.time + explosionLifetime : -1f;
        }

        private bool CanPlayLift()
        {
            return playLiftBeforeExplosion
                && liftSystems != null
                && liftSystems.Length > 0
                && liftDuration > 0f;
        }

        public void Play(float height)
        {
            liftHeight = Mathf.Max(0.1f, height);
            Play();
        }

        private void PlayLift()
        {
            for (int i = 0; i < liftSystems.Length; i++)
            {
                ParticleSystem effect = liftSystems[i];
                if (effect != null)
                {
                    effect.Play(true);
                }
            }

            PlayLiftSound();
        }

        private void PlayLiftSound()
        {
            if (fireworkPreSfx == null || !TryClaimLiftSoundFrame(Time.frameCount))
            {
                return;
            }

            AudioManager.Instance?.PlaySfx(fireworkPreSfx);
        }

        private bool TryClaimLiftSoundFrame(int frameCount)
        {
            if (lastLiftSoundFrame == frameCount)
            {
                return false;
            }

            lastLiftSoundFrame = frameCount;
            return true;
        }

        private IEnumerator PlayExplosionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayExplosion();
            explosionRoutine = null;
        }

        private void PlayExplosion()
        {
            for (int i = 0; i < explosionSystems.Length; i++)
            {
                ParticleSystem effect = explosionSystems[i];
                if (effect != null)
                {
                    effect.Play(true);
                }
            }

            PlayExplosionSound();
        }

        private void PlayExplosionSound()
        {
            AudioManager.Instance?.PlaySfx(fireworkSfx);
        }

        private void CacheParticleSystems()
        {
            if (liftSystems == null || liftSystems.Length == 0)
            {
                List<ParticleSystem> lifts = new List<ParticleSystem>(2);
                AddLiftSystemIfFound(lifts, "LiftShell");
                AddLiftSystemIfFound(lifts, "LiftTrail");
                if (liftSystem != null && !lifts.Contains(liftSystem))
                {
                    lifts.Add(liftSystem);
                }

                liftSystems = lifts.ToArray();
            }

            if (explosionSystems != null && explosionSystems.Length > 0)
            {
                return;
            }

            ParticleSystem[] childSystems = GetComponentsInChildren<ParticleSystem>(true);
            List<ParticleSystem> explosions = new List<ParticleSystem>(childSystems.Length);
            for (int i = 0; i < childSystems.Length; i++)
            {
                ParticleSystem system = childSystems[i];
                if (system != null && !IsLiftSystem(system))
                {
                    explosions.Add(system);
                }
            }

            explosionSystems = explosions.ToArray();
        }

        private void ApplyLiftHeight()
        {
            float height = Mathf.Max(0.1f, liftHeight);
            float duration = Mathf.Max(0.1f, liftDuration);
            for (int i = 0; i < liftSystems.Length; i++)
            {
                ParticleSystem system = liftSystems[i];
                if (system == null)
                {
                    continue;
                }

                if (system.name == "LiftShell")
                {
                    system.transform.localPosition = new Vector3(0f, -height, 0f);
                    SetShellArcVelocity(system, height, duration);
                }
                else if (system.name == "LiftTrail")
                {
                    system.transform.localPosition = new Vector3(0f, -height, 0f);
                    SetUpwardVelocity(system, height / duration * 0.7f);
                }
            }
        }

        private static void SetShellArcVelocity(ParticleSystem system, float height, float duration)
        {
            ParticleSystem.MainModule main = system.main;
            float gravityMagnitude = Mathf.Max(Mathf.Abs(Physics.gravity.y), 0.0001f);
            float effectiveGravity = 2f * height / (duration * duration);
            float upwardVelocity = 2f * height / duration;
            main.duration = 0.12f;
            main.gravityModifier = effectiveGravity / gravityMagnitude;
            main.startLifetime = duration;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.26f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = 0f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            ParticleSystemCurveMode velocityCurveMode = velocity.x.mode;
            velocity.x = CreateFixedCurve(velocityCurveMode, 0f);
            velocity.y = CreateFixedCurve(velocityCurveMode, upwardVelocity);
            velocity.z = CreateFixedCurve(velocityCurveMode, 0f);

            ParticleSystem.TrailModule trails = system.trails;
            trails.enabled = false;
        }

        private static ParticleSystem.MinMaxCurve CreateFixedCurve(ParticleSystemCurveMode mode, float value)
        {
            AnimationCurve constantCurve = AnimationCurve.Constant(0f, 1f, value);
            switch (mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(value, value);
                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(1f, constantCurve);
                case ParticleSystemCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(1f, constantCurve, constantCurve);
                default:
                    return new ParticleSystem.MinMaxCurve(value);
            }
        }

        private static void SetUpwardVelocity(ParticleSystem system, float upwardSpeed)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(upwardSpeed * 0.92f, upwardSpeed * 1.08f);
        }

        private void AddLiftSystemIfFound(List<ParticleSystem> lifts, string childName)
        {
            Transform liftTransform = transform.Find(childName);
            if (liftTransform == null)
            {
                return;
            }

            ParticleSystem found = liftTransform.GetComponent<ParticleSystem>();
            if (found != null && !lifts.Contains(found))
            {
                lifts.Add(found);
            }
        }

        private bool IsLiftSystem(ParticleSystem system)
        {
            for (int i = 0; i < liftSystems.Length; i++)
            {
                if (system == liftSystems[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static void StopAndClear(ParticleSystem effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Clear(true);
        }

        private static void StopAndClear(ParticleSystem[] effects)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                StopAndClear(effects[i]);
            }
        }

        private static float GetLongestLifetime(ParticleSystem[] effects)
        {
            float longestDuration = 0f;
            for (int i = 0; i < effects.Length; i++)
            {
                ParticleSystem effect = effects[i];
                if (effect != null)
                {
                    longestDuration = Mathf.Max(longestDuration, GetTotalLifetime(effect));
                }
            }

            return longestDuration;
        }

        private static float GetTotalLifetime(ParticleSystem effect)
        {
            ParticleSystem.MainModule main = effect.main;
            return main.duration + main.startDelay.constantMax + main.startLifetime.constantMax;
        }
    }
}
