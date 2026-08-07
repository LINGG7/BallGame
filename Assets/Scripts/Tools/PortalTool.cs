using System.Collections;
using System.Collections.Generic;
using BallGame.Audio;
using BallGame.Core;
using TMPro;
using UnityEngine;

namespace BallGame.Tools
{
    public sealed class PortalTool : MonoBehaviour, ISimulationPreview2D
    {
        private const string PortalInResourcePath = "Audio/SFX/PortalIn";
        private const string PortalOutResourcePath = "Audio/SFX/PortalOut";
        private const string PortalParticleMaterialResourcePath = "Materials/BallDustParticle";
        private const string PortalEffectObjectName = "PortalTransferEffect";
        private static readonly List<PortalTool> Portals = new List<PortalTool>();
        private static readonly HashSet<BallControllerBase> TeleportingBalls = new HashSet<BallControllerBase>();

        [SerializeField] private int pairId;
        [SerializeField] private float exitOffset = 0.7f;
        [SerializeField] private float cooldown = 0.35f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;
        [SerializeField, Min(0f)] private float hiddenDelay = 0.1f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
        [SerializeField] private TextMeshPro pairIdLabel;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer animatedRenderer;
        [Header("Transfer Effect")]
        [SerializeField] private ParticleSystem portalEffect;
        [SerializeField] private Color portalEffectColor = new Color(0.35f, 0.55f, 1f, 0.8f);
        [SerializeField, Min(0)] private int intakeParticleCount = 18;
        [SerializeField, Min(0)] private int outputParticleCount = 24;
        [SerializeField, Min(0f)] private float effectRadius = 0.6f;
        [SerializeField, Min(0f)] private float outputStartRadius = 0.12f;
        [SerializeField] private Vector2 intakeSpeedRange = new Vector2(2f, 2.8f);
        [SerializeField] private Vector2 outputSpeedRange = new Vector2(2.8f, 3.8f);
        [SerializeField] private Vector2 intakeLifetimeRange = new Vector2(0.2f, 0.3f);
        [SerializeField] private Vector2 outputLifetimeRange = new Vector2(0.25f, 0.35f);
        [SerializeField] private Vector2 effectSizeRange = new Vector2(0.04f, 0.09f);
        [SerializeField, Min(0f)] private float spiralStrength = 0.7f;
        [SerializeField, Min(0f)] private float particleLengthScale = 1.5f;
        [SerializeField, Min(0f)] private float particleVelocityScale = 0.1f;

        private sealed class TeleportState
        {
            public BallControllerBase ball;
            public Rigidbody2D body;
            public PortalTool exit;
            public bool wasSimulated;
            public Vector2 velocity;
            public float angularVelocity;
            public SpriteRenderer[] renderers;
            public Color[] rendererColors;
            public Coroutine routine;
        }

        private float lastTeleportTime = -100f;
        private bool simulationPreview;
        private GameManager gameManager;
        private Sprite idleSprite;
        private AudioClip portalInSfx;
        private AudioClip portalOutSfx;
        private readonly Dictionary<BallControllerBase, TeleportState> activeTeleports =
            new Dictionary<BallControllerBase, TeleportState>();

        public int PairId => pairId;

        private void Awake()
        {
            portalInSfx = Resources.Load<AudioClip>(PortalInResourcePath);
            portalOutSfx = Resources.Load<AudioClip>(PortalOutResourcePath);
            EnsurePairIdLabel();
            RefreshPairIdLabel();

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

            EnsurePortalEffect();
        }

        private void OnEnable()
        {
            EnsurePairIdLabel();
            RefreshPairIdLabel();

            if (!Portals.Contains(this))
            {
                Portals.Add(this);
            }

            gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.StateChanged += HandleGameStateChanged;
                HandleGameStateChanged(gameManager.State);
            }
            else
            {
                SetAnimationPlaying(false);
            }
        }

        private void OnDisable()
        {
            simulationPreview = false;
            CancelActiveTeleports();
            StopPortalEffect();
            Portals.Remove(this);
            SetAnimationPlaying(false);

            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleGameStateChanged;
                gameManager = null;
            }
        }

        public void SetPairId(int value)
        {
            pairId = Mathf.Max(0, value);
            RefreshPairIdLabel();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            BallControllerBase ball = other.GetComponentInParent<BallControllerBase>();
            PortalTool exit = FindExitPortal();
            TryTeleport(ball, exit, Time.time, simulationPreview || (gameManager != null && gameManager.IsSimulating));
        }

        private bool TryTeleport(BallControllerBase ball, PortalTool exit, float now, bool isSimulating)
        {
            bool hasBall = ball != null && ball.Body != null;
            if (!CanStartTeleport(
                isSimulating,
                hasBall,
                exit != null,
                now - lastTeleportTime,
                cooldown)
                || TeleportingBalls.Contains(ball))
            {
                return false;
            }

            TeleportState state = CaptureTeleportState(ball);
            state.exit = exit;
            TeleportingBalls.Add(ball);
            activeTeleports.Add(ball, state);

            PlayPortalInSound();
            PlayPortalInEffect();
            lastTeleportTime = now;
            exit.lastTeleportTime = now;
            state.body.velocity = Vector2.zero;
            state.body.angularVelocity = 0f;
            state.body.simulated = false;
            if (fadeOutDuration <= 0f && hiddenDelay <= 0f && fadeInDuration <= 0f)
            {
                CompleteTeleportImmediately(state, exit);
            }
            else
            {
                state.routine = StartCoroutine(TeleportRoutine(state, exit));
            }

            return true;
        }

        private TeleportState CaptureTeleportState(BallControllerBase ball)
        {
            SpriteRenderer[] renderers = ball.GetComponentsInChildren<SpriteRenderer>(true);
            Color[] colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                colors[i] = renderers[i].color;
            }

            return new TeleportState
            {
                ball = ball,
                body = ball.Body,
                wasSimulated = ball.Body.simulated,
                velocity = ball.Body.velocity,
                angularVelocity = ball.Body.angularVelocity,
                renderers = renderers,
                rendererColors = colors
            };
        }

        private IEnumerator TeleportRoutine(TeleportState state, PortalTool exit)
        {
            yield return FadeRenderers(state, 1f, 0f, fadeOutDuration);

            if (hiddenDelay > 0f)
            {
                yield return new WaitForSeconds(hiddenDelay);
            }

            if (!CanFinishTeleport(state, exit))
            {
                CancelTeleport(state, false);
                yield break;
            }

            MoveBodyToExit(state.body, exit);
            exit.lastTeleportTime = Time.time;
            exit.PlayPortalOutSound();
            exit.PlayPortalOutEffect();

            yield return FadeRenderers(state, 0f, 1f, fadeInDuration);
            CompleteTeleport(state);
        }

        private void CompleteTeleportImmediately(TeleportState state, PortalTool exit)
        {
            SetRendererAlpha(state, 0f);
            if (!CanFinishTeleport(state, exit))
            {
                CancelTeleport(state, false);
                return;
            }

            MoveBodyToExit(state.body, exit);
            exit.lastTeleportTime = Time.time;
            exit.PlayPortalOutSound();
            exit.PlayPortalOutEffect();
            CompleteTeleport(state);
        }

        private static void MoveBodyToExit(Rigidbody2D body, PortalTool exit)
        {
            Vector2 target = (Vector2)exit.transform.position
                + (Vector2)exit.transform.up * exit.exitOffset;
            Vector3 currentPosition = body.transform.position;
            body.transform.position = new Vector3(target.x, target.y, currentPosition.z);
        }

        private static bool CanFinishTeleport(TeleportState state, PortalTool exit)
        {
            return state != null
                && state.ball != null
                && state.body != null
                && exit != null
                && exit.isActiveAndEnabled;
        }

        private static IEnumerator FadeRenderers(
            TeleportState state,
            float startAlphaFactor,
            float targetAlphaFactor,
            float duration)
        {
            if (duration <= 0f)
            {
                SetRendererAlpha(state, targetAlphaFactor);
                yield break;
            }

            float elapsed = 0f;
            SetRendererAlpha(state, startAlphaFactor);
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                SetRendererAlpha(
                    state,
                    Mathf.Lerp(startAlphaFactor, targetAlphaFactor, Mathf.Clamp01(elapsed / duration)));
            }

            SetRendererAlpha(state, targetAlphaFactor);
        }

        private static void SetRendererAlpha(TeleportState state, float alphaFactor)
        {
            if (state == null || state.renderers == null || state.rendererColors == null)
            {
                return;
            }

            float factor = Mathf.Clamp01(alphaFactor);
            for (int i = 0; i < state.renderers.Length; i++)
            {
                SpriteRenderer renderer = state.renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = state.rendererColors[i];
                color.a *= factor;
                renderer.color = color;
            }
        }

        private void CompleteTeleport(TeleportState state)
        {
            RestoreTeleportState(state);
            RemoveTeleportState(state);
        }

        private void CancelTeleport(TeleportState state, bool stopRoutine = true)
        {
            if (state == null)
            {
                return;
            }

            if (stopRoutine && state.routine != null)
            {
                StopCoroutine(state.routine);
            }

            state.routine = null;
            StopPortalEffect();
            if (state.exit != null)
            {
                state.exit.StopPortalEffect();
            }

            RestoreTeleportState(state);
            RemoveTeleportState(state);
        }

        private static void RestoreTeleportState(TeleportState state)
        {
            SetRendererAlpha(state, 1f);
            if (state == null || state.body == null)
            {
                return;
            }

            state.body.simulated = state.wasSimulated;
            state.body.velocity = state.velocity;
            state.body.angularVelocity = state.angularVelocity;
        }

        private void RemoveTeleportState(TeleportState state)
        {
            if (state == null)
            {
                return;
            }

            TeleportingBalls.Remove(state.ball);
            if (state.ball != null)
            {
                activeTeleports.Remove(state.ball);
            }

            state.routine = null;
        }

        private void CancelActiveTeleports()
        {
            if (activeTeleports.Count == 0)
            {
                return;
            }

            var states = new List<TeleportState>(activeTeleports.Values);
            for (int i = 0; i < states.Count; i++)
            {
                CancelTeleport(states[i]);
            }

            activeTeleports.Clear();
        }

        private static bool CanStartTeleport(
            bool isSimulating,
            bool hasBall,
            bool hasExit,
            float elapsedSinceLastTeleport,
            float cooldownDuration)
        {
            return isSimulating
                && hasBall
                && hasExit
                && elapsedSinceLastTeleport >= cooldownDuration;
        }

        private void PlayPortalInSound()
        {
            AudioManager.Instance?.PlaySfx(portalInSfx);
        }

        private void PlayPortalOutSound()
        {
            AudioManager.Instance?.PlaySfx(portalOutSfx);
        }

        private void PlayPortalInEffect()
        {
            EmitPortalParticles(true);
        }

        private void PlayPortalOutEffect()
        {
            EmitPortalParticles(false);
        }

        private void EmitPortalParticles(bool inward)
        {
            EnsurePortalEffect();
            if (portalEffect == null)
            {
                return;
            }

            int count = inward ? intakeParticleCount : outputParticleCount;
            if (count <= 0)
            {
                return;
            }

            Vector2 speedRange = inward ? intakeSpeedRange : outputSpeedRange;
            Vector2 lifetimeRange = inward ? intakeLifetimeRange : outputLifetimeRange;
            Vector3 center = transform.position;
            portalEffect.Play(false);
            for (int i = 0; i < count; i++)
            {
                float angleStep = Mathf.PI * 2f / count;
                float angle = i * angleStep + Random.Range(-angleStep * 0.35f, angleStep * 0.35f);
                Vector2 radialDirection = CalculateWorldRadialDirection(angle);
                float speed = Random.Range(speedRange.x, speedRange.y);
                float lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
                Vector2 velocity = CalculateSpiralVelocity(radialDirection, speed, inward);
                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
                {
                    position = CalculateParticleSpawnPosition(center, radialDirection, inward),
                    velocity = velocity,
                    startColor = portalEffectColor,
                    startLifetime = lifetime,
                    startSize = Random.Range(effectSizeRange.x, effectSizeRange.y)
                };
                portalEffect.Emit(emit, 1);
            }
        }

        private Vector3 CalculateParticleSpawnPosition(
            Vector3 center,
            Vector2 radialDirection,
            bool inward)
        {
            float radius = inward ? effectRadius : outputStartRadius;
            return center + (Vector3)(radialDirection.normalized * radius);
        }

        private Vector2 CalculateWorldRadialDirection(float angle)
        {
            Vector3 localDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Vector2 worldDirection = transform.TransformDirection(localDirection);
            return worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : Vector2.right;
        }

        private Vector2 CalculateSpiralVelocity(Vector2 radialDirection, float radialSpeed, bool inward)
        {
            Vector2 radial = radialDirection.sqrMagnitude > 0.0001f
                ? radialDirection.normalized
                : Vector2.right;
            Vector2 counterClockwiseTangent = new Vector2(-radial.y, radial.x);
            float radialSign = inward ? -1f : 1f;
            float tangentSign = inward ? -1f : 1f;
            return radial * (radialSpeed * radialSign)
                + counterClockwiseTangent * (radialSpeed * spiralStrength * tangentSign);
        }

        private void EnsurePortalEffect()
        {
            if (portalEffect == null)
            {
                Transform existing = transform.Find(PortalEffectObjectName);
                if (existing != null)
                {
                    portalEffect = existing.GetComponent<ParticleSystem>();
                }
            }

            if (portalEffect == null)
            {
                GameObject effectObject = new GameObject(PortalEffectObjectName);
                effectObject.transform.SetParent(transform, false);
                portalEffect = effectObject.AddComponent<ParticleSystem>();
            }

            ConfigurePortalEffect(portalEffect);
        }

        private void ConfigurePortalEffect(ParticleSystem effect)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem.MainModule main = effect.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(32, intakeParticleCount + outputParticleCount);

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = effect.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                },
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                }
            });

            ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.lengthScale = particleLengthScale;
            particleRenderer.velocityScale = particleVelocityScale;
            particleRenderer.sortingLayerID = animatedRenderer != null
                ? animatedRenderer.sortingLayerID
                : 0;
            particleRenderer.sortingOrder = animatedRenderer != null
                ? animatedRenderer.sortingOrder + 1
                : 6;

            Material material = Resources.Load<Material>(PortalParticleMaterialResourcePath);
            if (material != null)
            {
                particleRenderer.sharedMaterial = material;
            }
        }

        private void StopPortalEffect()
        {
            if (portalEffect != null)
            {
                portalEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private PortalTool FindExitPortal()
        {
            foreach (PortalTool portal in Portals)
            {
                if (portal != this && portal.isActiveAndEnabled && portal.pairId == pairId)
                {
                    return portal;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.45f);
            Gizmos.DrawLine(transform.position, transform.position + transform.up * exitOffset);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state != GameState.Simulate)
            {
                CancelActiveTeleports();
                StopPortalEffect();
            }

            SetAnimationPlaying(state == GameState.Simulate);
        }

        public void SetSimulationPreview(bool playing)
        {
            simulationPreview = playing;
            SetAnimationPlaying(playing);
        }

        private void SetAnimationPlaying(bool playing)
        {
            if (playing)
            {
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.speed = 1f;
                }

                return;
            }

            if (animator != null)
            {
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                animator.enabled = false;
            }

            if (animatedRenderer != null && idleSprite != null)
            {
                animatedRenderer.sprite = idleSprite;
            }
        }

        private void EnsurePairIdLabel()
        {
            if (pairIdLabel != null)
            {
                return;
            }

            Transform existingLabel = transform.Find("PairIdLabel");
            if (existingLabel != null)
            {
                pairIdLabel = existingLabel.GetComponent<TextMeshPro>();
            }

            if (pairIdLabel != null)
            {
                return;
            }
        }

        private void RefreshPairIdLabel()
        {
            EnsurePairIdLabel();
            if (pairIdLabel != null)
            {
                pairIdLabel.text = $"#{pairId + 1}";
            }
        }

        private void OnValidate()
        {
            pairId = Mathf.Max(0, pairId);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
            hiddenDelay = Mathf.Max(0f, hiddenDelay);
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
            intakeParticleCount = Mathf.Max(0, intakeParticleCount);
            outputParticleCount = Mathf.Max(0, outputParticleCount);
            effectRadius = Mathf.Max(0f, effectRadius);
            outputStartRadius = Mathf.Max(0f, outputStartRadius);
            ClampRange(ref intakeSpeedRange);
            ClampRange(ref outputSpeedRange);
            ClampRange(ref intakeLifetimeRange);
            ClampRange(ref outputLifetimeRange);
            ClampRange(ref effectSizeRange);
            spiralStrength = Mathf.Max(0f, spiralStrength);
            particleLengthScale = Mathf.Max(0f, particleLengthScale);
            particleVelocityScale = Mathf.Max(0f, particleVelocityScale);
            if (pairIdLabel != null)
            {
                pairIdLabel.text = $"#{pairId + 1}";
            }
        }

        private static void ClampRange(ref Vector2 range)
        {
            float minimum = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            range = new Vector2(minimum, maximum);
        }
    }
}
