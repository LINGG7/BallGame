using BallGame.Core;
using BallGame.Shop;
using UnityEngine;

namespace BallGame.Effects
{
    [DisallowMultipleComponent]
    public sealed class BallTrailEffect : MonoBehaviour
    {
        private Rigidbody2D body;
        private SpriteRenderer ballRenderer;
        private BallVisual3DTransition visual3D;
        private GameManager gameManager;
        private GameObject instance;
        private TrailRenderer[] trails = new TrailRenderer[0];
        private ParticleSystem[] particles = new ParticleSystem[0];
        private Renderer[] effectRenderers = new Renderer[0];
        private Vector3 lastPosition;
        private float[] particleRates = new float[0];
        private float[] particleRemainders = new float[0];
        private bool[] emitLeft = new bool[0];
        private const float ParticleSideGap = 0.015f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ballRenderer = GetComponent<SpriteRenderer>();
            gameManager = FindObjectOfType<GameManager>();
        }

        private void OnEnable()
        {
            EffectSkinService.SkinChanged += Apply;
            Apply(EffectSkinService.CurrentSkinId);
        }

        private void OnDisable()
        {
            EffectSkinService.SkinChanged -= Apply;
            Clear();
        }

        private void Apply(string id)
        {
            Clear();
            if (instance != null)
            {
                instance.SetActive(false);
                Destroy(instance);
            }
            trails = new TrailRenderer[0];
            particles = new ParticleSystem[0];
            effectRenderers = new Renderer[0];
            particleRates = new float[0];
            particleRemainders = new float[0];
            emitLeft = new bool[0];
            var entry = EffectSkinService.Catalog != null ? EffectSkinService.Catalog.Find(id) : null;
            if (entry == null || entry.effectPrefab == null) return;
            instance = Instantiate(entry.effectPrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            trails = instance.GetComponentsInChildren<TrailRenderer>(true);
            particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            particleRates = new float[particles.Length];
            particleRemainders = new float[particles.Length];
            emitLeft = new bool[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                var emission = particles[i].emission;
                particleRates[i] = Mathf.Max(0f, emission.rateOverDistance.constantMax);
                // Keep the prefab's density, but place each particle explicitly on a side.
                // Disable automatic emission so no particles are also spawned in the ribbon.
                emission.enabled = false;
                var main = particles[i].main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            effectRenderers = instance.GetComponentsInChildren<Renderer>(true);
            Clear();
        }

        public void Clear()
        {
            foreach (var trail in trails)
            {
                trail.emitting = false;
                trail.Clear();
            }
            foreach (var particle in particles)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < particleRemainders.Length; i++)
            {
                particleRemainders[i] = 0f;
                emitLeft[i] = true;
            }
            lastPosition = transform.position;
        }

        private void EmitSideParticles(int index, Vector3 from, Vector3 to)
        {
            Vector2 movement = (Vector2)(to - from);
            float distance = movement.magnitude;
            float rate = particleRates[index];
            if (distance < 0.00001f || rate <= 0f) return;

            Vector2 forward = movement / distance;
            Vector2 left = new Vector2(-forward.y, forward.x);
            float ribbonHalfWidth = 0f;
            foreach (var trail in trails)
                ribbonHalfWidth = Mathf.Max(ribbonHalfWidth, trail.widthMultiplier * trail.widthCurve.Evaluate(0f) * 0.5f);

            var system = particles[index];

            // Keep particle centres at the ribbon edge; their soft sprites may overlap it.
            float sideOffset = ribbonHalfWidth * 0.85f + ParticleSideGap;
            float previousRemainder = particleRemainders[index];
            float emissionCount = previousRemainder + distance * rate;
            int count = Mathf.FloorToInt(emissionCount);
            particleRemainders[index] = emissionCount - count;
            for (int i = 0; i < count; i++)
            {
                float alongSegment = (1f - previousRemainder + i) / (rate * distance);
                Vector2 outward = emitLeft[index] ? left : -left;
                var emit = new ParticleSystem.EmitParams
                {
                    position = Vector3.Lerp(from, to, alongSegment) + (Vector3)(outward * sideOffset),
                    velocity = Vector3.zero,
                    applyShapeToPosition = false
                };
                system.Emit(emit, 1);
                emitLeft[index] = !emitLeft[index];
            }
        }
        private void LateUpdate()
        {
            Vector3 position = transform.position;
            if (visual3D == null) visual3D = GetComponent<BallVisual3DTransition>();
            float expectedDistance = body != null ? body.velocity.magnitude * Time.deltaTime : 0f;
            bool discontinuity = Vector3.Distance(position, lastPosition) > Mathf.Max(1f, expectedDistance * 2f + 0.25f);
            bool running = body != null && body.simulated && body.bodyType == RigidbodyType2D.Dynamic
                && (gameManager == null || gameManager.IsSimulating)
                && (ballRenderer == null || ((ballRenderer.enabled || (visual3D != null && visual3D.Is3D)) && ballRenderer.color.a > 0.05f));
            if (discontinuity || !running)
            {
                Clear();
                return;
            }

            bool moving = body.velocity.sqrMagnitude > 0.04f;
            foreach (var renderer in effectRenderers)
            {
                if (ballRenderer == null) continue;
                renderer.sortingLayerID = ballRenderer.sortingLayerID;
                renderer.sortingOrder = ballRenderer.sortingOrder - (renderer is TrailRenderer ? 2 : 1);
            }
            foreach (var trail in trails) trail.emitting = moving;
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (moving)
                {
                    if (!particle.isPlaying) particle.Play(true);
                    EmitSideParticles(i, lastPosition, position);
                }
                else if (particle.isPlaying)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            lastPosition = position;
        }
    }
}