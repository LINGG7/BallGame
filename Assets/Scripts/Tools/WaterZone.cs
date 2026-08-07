using System.Collections;
using System.Collections.Generic;
using BallGame.Audio;
using BallGame.Core;
using UnityEngine;

namespace BallGame.Tools
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WaterZone : MonoBehaviour, ISizeEditable2D
    {
        private const string SplashInResourcePath = "Audio/SFX/SplashIn";
        private const string SplashOutResourcePath = "Audio/SFX/SplashOut";

        [SerializeField] private Vector2 size = new Vector2(3f, 2f);
        [SerializeField] private float buoyancy = 16f;
        [SerializeField, Min(0f)] private float linearDrag = 2.5f;
        [SerializeField, Min(0f)] private float angularDrag = 3f;
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private SpriteRenderer waterRenderer;
        [SerializeField] private Transform surfaceLine;
        [SerializeField] private ParticleSystem splashEffect;
        [Header("Bubbles")]
        [SerializeField, Min(0.05f)] private float bubbleInterval = 0.5f;
        [SerializeField, Min(1)] private int minimumBubblesPerBurst = 2;
        [SerializeField, Min(1)] private int maximumBubblesPerBurst = 4;
        [SerializeField] private Vector2 bubbleSizeRange = new Vector2(0.07f, 0.14f);
        [SerializeField] private Vector2 bubbleSpeedRange = new Vector2(0.7f, 1.05f);
        [SerializeField] private ParticleSystem bubbleRiseEffect;
        [SerializeField] private ParticleSystem bubblePopEffect;

        private readonly Dictionary<BallControllerBase, HashSet<Collider2D>> immersedContacts =
            new Dictionary<BallControllerBase, HashSet<Collider2D>>();
        private readonly Dictionary<BallControllerBase, float> nextBubbleTimes = new Dictionary<BallControllerBase, float>();
        private readonly List<BallControllerBase> staleImmersedBalls = new List<BallControllerBase>();
        private BoxCollider2D waterCollider;
        private Material runtimeBubbleMaterial;
        private AudioClip splashInSfx;
        private AudioClip splashOutSfx;

        public Vector2 Size => size;

        public void SetSize(Vector2 value)
        {
            size = new Vector2(Mathf.Max(0.1f, value.x), Mathf.Max(0.1f, value.y));
            RefreshShape();
        }

        private void Awake()
        {
            splashInSfx = Resources.Load<AudioClip>(SplashInResourcePath);
            splashOutSfx = Resources.Load<AudioClip>(SplashOutResourcePath);
            EnsureSplashEffect();
            EnsureBubbleEffects();
            RefreshShape();
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.1f, size.y);
            buoyancy = Mathf.Max(0f, buoyancy);
            linearDrag = Mathf.Max(0f, linearDrag);
            angularDrag = Mathf.Max(0f, angularDrag);
            bubbleInterval = Mathf.Max(0.05f, bubbleInterval);
            minimumBubblesPerBurst = Mathf.Max(1, minimumBubblesPerBurst);
            maximumBubblesPerBurst = Mathf.Max(minimumBubblesPerBurst, maximumBubblesPerBurst);
            bubbleSizeRange.x = Mathf.Max(0.01f, bubbleSizeRange.x);
            bubbleSizeRange.y = Mathf.Max(bubbleSizeRange.x, bubbleSizeRange.y);
            bubbleSpeedRange.x = Mathf.Max(0.05f, bubbleSpeedRange.x);
            bubbleSpeedRange.y = Mathf.Max(bubbleSpeedRange.x, bubbleSpeedRange.y);
            RefreshShape();
        }

        private void FixedUpdate()
        {
            CleanupInvalidImmersions();
            UpdateUnderwaterReport();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            AudioManager.Instance?.StopUnderwater(this);
            ClearImmersionState();
            if (bubbleRiseEffect != null)
            {
                bubbleRiseEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (bubblePopEffect != null)
            {
                bubblePopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            BallControllerBase ball = other.GetComponentInParent<BallControllerBase>();
            if (ball == null)
            {
                return;
            }

            if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                RemoveImmersedContact(ball, other);
                return;
            }

            if (ball.Body == null || ball.Body.bodyType != RigidbodyType2D.Dynamic)
            {
                RemoveImmersedContact(ball, other);
                return;
            }

            if (RegisterImmersedContact(ball, other))
            {
                PlaySplashEffect(other, ball);
                PlaySplashInSound();
                nextBubbleTimes[ball] = Time.time + Random.Range(0.12f, 0.35f);
            }

            EmitBubblesIfDue(ball);

            Rigidbody2D body = ball.Body;
            body.AddForce(Vector2.up * buoyancy * body.mass, ForceMode2D.Force);
            body.velocity *= Mathf.Exp(-linearDrag * Time.fixedDeltaTime);
            body.angularVelocity *= Mathf.Exp(-angularDrag * Time.fixedDeltaTime);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            BallControllerBase ball = other.GetComponentInParent<BallControllerBase>();
            if (ball != null
                && RemoveImmersedContact(ball, other)
                && ball.Body != null
                && ball.Body.bodyType == RigidbodyType2D.Dynamic)
            {
                PlaySplashOutSound();
            }
        }

        private bool RegisterImmersedContact(BallControllerBase ball, Collider2D other)
        {
            if (!immersedContacts.TryGetValue(ball, out HashSet<Collider2D> contacts))
            {
                contacts = new HashSet<Collider2D>();
                immersedContacts.Add(ball, contacts);
            }

            return contacts.Add(other) && contacts.Count == 1;
        }

        private bool RemoveImmersedContact(BallControllerBase ball, Collider2D other)
        {
            if (!immersedContacts.TryGetValue(ball, out HashSet<Collider2D> contacts)
                || !contacts.Remove(other)
                || contacts.Count > 0)
            {
                return false;
            }

            immersedContacts.Remove(ball);
            nextBubbleTimes.Remove(ball);
            return true;
        }

        private void ClearImmersionState()
        {
            immersedContacts.Clear();
            nextBubbleTimes.Clear();
            staleImmersedBalls.Clear();
        }

        private void CleanupInvalidImmersions()
        {
            staleImmersedBalls.Clear();
            foreach (KeyValuePair<BallControllerBase, HashSet<Collider2D>> pair in immersedContacts)
            {
                BallControllerBase ball = pair.Key;
                HashSet<Collider2D> contacts = pair.Value;
                contacts.RemoveWhere(contact => contact == null || !contact.enabled);
                if (ball == null
                    || ball.Body == null
                    || ball.Body.bodyType != RigidbodyType2D.Dynamic
                    || contacts.Count == 0)
                {
                    staleImmersedBalls.Add(ball);
                }
            }

            for (int i = 0; i < staleImmersedBalls.Count; i++)
            {
                BallControllerBase ball = staleImmersedBalls[i];
                immersedContacts.Remove(ball);
                nextBubbleTimes.Remove(ball);
            }

            staleImmersedBalls.Clear();
        }

        private void UpdateUnderwaterReport()
        {
            if (immersedContacts.Count > 0)
            {
                AudioManager.Instance?.ReportUnderwater(this);
            }
            else
            {
                AudioManager.Instance?.StopUnderwater(this);
            }
        }

        private void EmitBubblesIfDue(BallControllerBase ball)
        {
            if (bubbleRiseEffect == null || bubblePopEffect == null)
            {
                return;
            }

            if (!nextBubbleTimes.TryGetValue(ball, out float nextTime))
            {
                nextBubbleTimes[ball] = Time.time + Random.Range(0.12f, 0.35f);
                return;
            }

            if (Time.time < nextTime)
            {
                return;
            }

            int count = Random.Range(minimumBubblesPerBurst, maximumBubblesPerBurst + 1);
            for (int i = 0; i < count; i++)
            {
                EmitBubble(ball.Body.position);
            }

            nextBubbleTimes[ball] = Time.time + bubbleInterval * Random.Range(0.85f, 1.15f);
        }

        private void EmitBubble(Vector2 ballPosition)
        {
            Vector2 localStart = transform.InverseTransformPoint(ballPosition);
            Vector2 halfSize = size * 0.5f;
            localStart.x += Random.Range(-0.18f, 0.18f);
            localStart.y += Random.Range(-0.1f, 0.1f);
            localStart.x = Mathf.Clamp(localStart.x, -halfSize.x + 0.02f, halfSize.x - 0.02f);
            localStart.y = Mathf.Clamp(localStart.y, -halfSize.y + 0.02f, halfSize.y - 0.02f);

            Vector2 start = transform.TransformPoint(localStart);
            float speed = Random.Range(bubbleSpeedRange.x, bubbleSpeedRange.y);
            Vector2 velocity = new Vector2(Random.Range(-0.06f, 0.06f), speed);
            if (!TryGetBoundaryTravel(start, velocity, out float lifetime, out Vector2 exitPoint))
            {
                lifetime = 0.6f;
                exitPoint = start + velocity * lifetime;
            }

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                position = start,
                velocity = velocity,
                startColor = new Color(0.75f, 0.94f, 1f, Random.Range(0.65f, 0.9f)),
                startLifetime = Mathf.Max(0.05f, lifetime),
                startSize = Random.Range(bubbleSizeRange.x, bubbleSizeRange.y)
            };
            bubbleRiseEffect.Emit(emit, 1);
            StartCoroutine(PlayBubblePop(exitPoint, Mathf.Max(0.05f, lifetime)));
        }

        private bool TryGetBoundaryTravel(Vector2 worldStart, Vector2 worldVelocity, out float travelTime, out Vector2 exitPoint)
        {
            Vector2 localStart = transform.InverseTransformPoint(worldStart);
            Vector2 localVelocity = transform.InverseTransformVector(worldVelocity);
            Vector2 halfSize = size * 0.5f;
            travelTime = float.PositiveInfinity;

            if (Mathf.Abs(localVelocity.x) > 0.0001f)
            {
                float boundaryX = localVelocity.x > 0f ? halfSize.x : -halfSize.x;
                float timeX = (boundaryX - localStart.x) / localVelocity.x;
                if (timeX > 0f)
                {
                    travelTime = Mathf.Min(travelTime, timeX);
                }
            }

            if (Mathf.Abs(localVelocity.y) > 0.0001f)
            {
                float boundaryY = localVelocity.y > 0f ? halfSize.y : -halfSize.y;
                float timeY = (boundaryY - localStart.y) / localVelocity.y;
                if (timeY > 0f)
                {
                    travelTime = Mathf.Min(travelTime, timeY);
                }
            }

            if (float.IsInfinity(travelTime) || float.IsNaN(travelTime))
            {
                exitPoint = worldStart;
                return false;
            }

            exitPoint = worldStart + worldVelocity * travelTime;
            return true;
        }

        private IEnumerator PlayBubblePop(Vector2 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (bubblePopEffect == null)
            {
                yield break;
            }

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = Vector3.zero,
                startColor = new Color(0.82f, 0.96f, 1f, 0.75f),
                startLifetime = 0.22f,
                startSize = Random.Range(0.12f, 0.2f)
            };
            bubblePopEffect.Emit(emit, 1);
        }

        private void PlaySplashEffect(Collider2D other, BallControllerBase ball)
        {
            if (splashEffect == null)
            {
                return;
            }

            Vector2 entryPoint = waterCollider != null
                ? waterCollider.ClosestPoint(ball.transform.position)
                : other.ClosestPoint(transform.position);
            splashEffect.transform.position = entryPoint;
            splashEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            splashEffect.Play();
        }

        private void PlaySplashInSound()
        {
            AudioManager.Instance?.PlaySfx(splashInSfx);
        }

        private void PlaySplashOutSound()
        {
            AudioManager.Instance?.PlaySfx(splashOutSfx);
        }

        private void EnsureSplashEffect()
        {
            if (splashEffect != null)
            {
                return;
            }

            Transform existing = transform.Find("SplashEffect");
            splashEffect = existing != null ? existing.GetComponent<ParticleSystem>() : null;
            if (splashEffect != null)
            {
                return;
            }

            GameObject splashObject = new GameObject("SplashEffect");
            splashObject.transform.SetParent(transform, false);
            splashEffect = splashObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = splashEffect.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.startColor = new Color(0.55f, 0.88f, 1f, 0.9f);
            main.gravityModifier = 0.55f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = splashEffect.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            ParticleSystem.ShapeModule shape = splashEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.08f;
            splashObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystemRenderer particleRenderer = splashObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sortingOrder = 6;
            Shader particleShader = Shader.Find("Sprites/Default");
            if (particleShader != null)
            {
                particleRenderer.material = new Material(particleShader)
                {
                    name = "Runtime Water Splash Material"
                };
            }
        }

        private void EnsureBubbleEffects()
        {
            bubbleRiseEffect = FindOrCreateParticleSystem("BubbleRiseEffect", bubbleRiseEffect);
            bubblePopEffect = FindOrCreateParticleSystem("BubblePopEffect", bubblePopEffect);
            ConfigureBubbleSystem(bubbleRiseEffect, false, 5);
            ConfigureBubbleSystem(bubblePopEffect, true, 7);

            Shader bubbleShader = Shader.Find("BallGame/Bubble Particle");
            if (bubbleShader == null)
            {
                return;
            }

            ParticleSystemRenderer riseRenderer = bubbleRiseEffect.GetComponent<ParticleSystemRenderer>();
            ParticleSystemRenderer popRenderer = bubblePopEffect.GetComponent<ParticleSystemRenderer>();
            bool riseNeedsMaterial = riseRenderer.sharedMaterial == null || riseRenderer.sharedMaterial.shader != bubbleShader;
            bool popNeedsMaterial = popRenderer.sharedMaterial == null || popRenderer.sharedMaterial.shader != bubbleShader;
            if (riseNeedsMaterial || popNeedsMaterial)
            {
                runtimeBubbleMaterial = new Material(bubbleShader)
                {
                    name = "Runtime Bubble Particle Material"
                };
                riseRenderer.sharedMaterial = runtimeBubbleMaterial;
                popRenderer.sharedMaterial = runtimeBubbleMaterial;
            }
        }

        private ParticleSystem FindOrCreateParticleSystem(string childName, ParticleSystem current)
        {
            if (current != null)
            {
                return current;
            }

            Transform existing = transform.Find(childName);
            ParticleSystem effect = existing != null ? existing.GetComponent<ParticleSystem>() : null;
            if (effect != null)
            {
                return effect;
            }

            GameObject effectObject = new GameObject(childName);
            effectObject.transform.SetParent(transform, false);
            return effectObject.AddComponent<ParticleSystem>();
        }

        private static void ConfigureBubbleSystem(ParticleSystem effect, bool expands, int sortingOrder)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem.MainModule main = effect.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.maxParticles = 128;
            ParticleSystem.EmissionModule emission = effect.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = false;
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = effect.sizeOverLifetime;
            sizeOverLifetime.enabled = expands;
            if (expands)
            {
                AnimationCurve expansion = AnimationCurve.Linear(0f, 0.55f, 1f, 1.45f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, expansion);
            }

            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = sortingOrder;
        }

        private void OnDestroy()
        {
            AudioManager.Instance?.StopUnderwater(this);
            if (runtimeBubbleMaterial != null)
            {
                Destroy(runtimeBubbleMaterial);
            }
        }

        private void RefreshShape()
        {
            if (waterCollider == null)
            {
                waterCollider = GetComponent<BoxCollider2D>();
            }

            if (waterCollider != null)
            {
                waterCollider.isTrigger = true;
                waterCollider.size = size;
            }

            if (waterRenderer != null)
            {
                waterRenderer.transform.localPosition = Vector3.zero;
                waterRenderer.transform.localRotation = Quaternion.identity;
                waterRenderer.transform.localScale = CalculateSpriteScale(waterRenderer, size.x, size.y);
            }

            if (surfaceLine != null)
            {
                surfaceLine.localPosition = new Vector3(0f, size.y * 0.5f, -0.01f);
                surfaceLine.localRotation = Quaternion.identity;
                surfaceLine.localScale = CalculateSpriteScale(
                    surfaceLine.GetComponent<SpriteRenderer>(),
                    size.x,
                    0.18f);
            }
        }

        private static Vector3 CalculateSpriteScale(SpriteRenderer renderer, float targetWidth, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return new Vector3(targetWidth, targetHeight, 1f);
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            float scaleX = spriteSize.x > Mathf.Epsilon ? targetWidth / spriteSize.x : targetWidth;
            float scaleY = spriteSize.y > Mathf.Epsilon ? targetHeight / spriteSize.y : targetHeight;
            return new Vector3(scaleX, scaleY, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.15f, 0.65f, 1f, 0.65f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
            Gizmos.matrix = previous;
        }
    }
}
