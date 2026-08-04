using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class InkParticleGraphic : MaskableGraphic
    {
        private struct Particle
        {
            public Vector2 position;
            public Vector2 velocity;
            public Color color;
            public float age;
            public float lifetime;
            public float size;
            public float rotation;
            public float angularVelocity;
        }

        private readonly List<Particle> particles = new List<Particle>(512);

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            color = Color.white;
        }

        public void ClearParticles()
        {
            particles.Clear();
            SetVerticesDirty();
        }

        public void Emit(Vector2 position, Vector2 direction)
        {
            if (particles.Count >= 700)
            {
                return;
            }

            Vector2 jitter = Random.insideUnitCircle;
            Vector2 velocity = direction * Random.Range(24f, 72f) + new Vector2(jitter.x * 42f, Random.Range(30f, 96f));
            float shade = Random.Range(0.38f, 0.72f);
            particles.Add(new Particle
            {
                position = position + jitter * 4f,
                velocity = velocity,
                color = new Color(shade, shade * 0.98f, shade * 0.92f, Random.Range(0.42f, 0.85f)),
                age = 0f,
                lifetime = Random.Range(0.38f, 0.85f),
                size = Random.Range(2.5f, 8.5f),
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-240f, 240f)
            });
            SetVerticesDirty();
        }

        private void Update()
        {
            if (particles.Count == 0)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.age += deltaTime;
                if (particle.age >= particle.lifetime)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                particle.position += particle.velocity * deltaTime;
                particle.velocity *= Mathf.Exp(-1.8f * deltaTime);
                particle.velocity.y += 12f * deltaTime;
                particle.rotation += particle.angularVelocity * deltaTime;
                particles[i] = particle;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            for (int i = 0; i < particles.Count; i++)
            {
                Particle particle = particles[i];
                float normalizedAge = Mathf.Clamp01(particle.age / particle.lifetime);
                float fade = 1f - normalizedAge * normalizedAge;
                float halfSize = particle.size * Mathf.Lerp(0.65f, 1.15f, normalizedAge) * 0.5f;
                float radians = particle.rotation * Mathf.Deg2Rad;
                Vector2 right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * halfSize;
                Vector2 up = new Vector2(-right.y, right.x);
                Color particleColor = particle.color;
                particleColor.a *= fade;

                int start = vertexHelper.currentVertCount;
                vertexHelper.AddVert(particle.position - right - up, particleColor, Vector2.zero);
                vertexHelper.AddVert(particle.position - right + up, particleColor, Vector2.up);
                vertexHelper.AddVert(particle.position + right + up, particleColor, Vector2.one);
                vertexHelper.AddVert(particle.position + right - up, particleColor, Vector2.right);
                vertexHelper.AddTriangle(start, start + 1, start + 2);
                vertexHelper.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
