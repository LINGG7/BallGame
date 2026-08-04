using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class PortalAudioTests
    {
        private const int TestPairId = 987654;
        private static Type PortalType => Type.GetType("BallGame.Tools.PortalTool, Assembly-CSharp");
        private static Type BallBaseType => Type.GetType("BallControllerBase, Assembly-CSharp");

        [SetUp]
        public void SetUp()
        {
            ClearPortalRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            ClearPortalRegistry();
        }

        [Test]
        public void PortalClips_AreLoadedAndUseExpectedImportSettings()
        {
            GameObject portalObject = CreatePortal("Portal", TestPairId, out Component portal);
            try
            {
                AssertClip(portal, "portalInSfx", "Assets/Resources/Audio/SFX/PortalIn.wav");
                AssertClip(portal, "portalOutSfx", "Assets/Resources/Audio/SFX/PortalOut.wav");

                SetField(portal, "portalInSfx", null);
                SetField(portal, "portalOutSfx", null);
                Assert.DoesNotThrow(() => Invoke(portal, "PlayPortalInSound"));
                Assert.DoesNotThrow(() => Invoke(portal, "PlayPortalOutSound"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void Pairing_RequiresMatchingEnabledPortal()
        {
            GameObject entranceObject = CreatePortal("Entrance", TestPairId, out Component entrance);
            GameObject exitObject = CreatePortal("Exit", TestPairId, out Component exit);
            GameObject otherObject = CreatePortal("OtherPair", TestPairId + 1, out _);
            try
            {
                Assert.That(Invoke(entrance, "FindExitPortal"), Is.SameAs(exit));

                exitObject.SetActive(false);
                Assert.That(Invoke(entrance, "FindExitPortal"), Is.Null);

                exitObject.SetActive(true);
                Assert.That(Invoke(entrance, "FindExitPortal"), Is.SameAs(exit));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(entranceObject);
                UnityEngine.Object.DestroyImmediate(exitObject);
                UnityEngine.Object.DestroyImmediate(otherObject);
            }
        }

        [Test]
        public void Teleport_DelayedFlowFreezesBallBlocksRepeatsAndRestoresWhenCancelled()
        {
            GameObject entranceObject = CreatePortal("Entrance", TestPairId, out Component entrance);
            GameObject exitObject = CreatePortal("Exit", TestPairId, out Component exit);
            GameObject ballObject = CreateBall(
                out Component ball,
                out Rigidbody2D body,
                out SpriteRenderer renderer);
            try
            {
                exitObject.transform.position = new Vector3(4f, 3f, 0f);
                exitObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                SetField(exit, "exitOffset", 0.7f);
                Vector2 originalPosition = body.position;
                Vector2 originalVelocity = new Vector2(2.5f, -1.25f);
                const float originalAngularVelocity = 30f;
                Color originalColor = new Color(0.2f, 0.4f, 0.6f, 0.65f);
                body.velocity = originalVelocity;
                body.angularVelocity = originalAngularVelocity;
                renderer.color = originalColor;

                bool teleported = (bool)Invoke(entrance, "TryTeleport", ball, exit, 10f, true);
                Assert.That(teleported, Is.True);
                Assert.That(body.position, Is.EqualTo(originalPosition), "The delayed flow must not move immediately.");
                Assert.That(body.simulated, Is.False);
                Assert.That(GetField<float>(entrance, "lastTeleportTime"), Is.EqualTo(10f));
                Assert.That(GetField<float>(exit, "lastTeleportTime"), Is.EqualTo(10f));

                bool repeated = (bool)Invoke(entrance, "TryTeleport", ball, exit, 20f, true);
                Assert.That(repeated, Is.False, "A ball already teleporting must reject all other attempts.");
                Assert.That(GetField<float>(entrance, "lastTeleportTime"), Is.EqualTo(10f));

                Invoke(entrance, "OnDisable");
                entranceObject.SetActive(false);
                Assert.That(body.simulated, Is.True);
                Assert.That(body.velocity, Is.EqualTo(originalVelocity));
                Assert.That(body.angularVelocity, Is.EqualTo(originalAngularVelocity).Within(0.001f));
                Assert.That(renderer.color, Is.EqualTo(originalColor));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(entranceObject);
                UnityEngine.Object.DestroyImmediate(exitObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void Teleport_ZeroDurationsCompletesImmediatelyAndPreservesMotionAndColor()
        {
            GameObject entranceObject = CreatePortal("Entrance", TestPairId, out Component entrance);
            GameObject exitObject = CreatePortal("Exit", TestPairId, out Component exit);
            GameObject ballObject = CreateBall(
                out Component ball,
                out Rigidbody2D body,
                out SpriteRenderer renderer);
            try
            {
                SetField(entrance, "fadeOutDuration", 0f);
                SetField(entrance, "hiddenDelay", 0f);
                SetField(entrance, "fadeInDuration", 0f);
                exitObject.transform.position = new Vector3(4f, 3f, 0f);
                exitObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                SetField(exit, "exitOffset", 0.7f);

                Vector2 originalVelocity = new Vector2(3f, -2f);
                const float originalAngularVelocity = -45f;
                Color originalColor = new Color(0.3f, 0.5f, 0.7f, 0.55f);
                body.velocity = originalVelocity;
                body.angularVelocity = originalAngularVelocity;
                renderer.color = originalColor;

                bool teleported = (bool)Invoke(entrance, "TryTeleport", ball, exit, 10f, true);

                Vector2 expectedPosition = (Vector2)exitObject.transform.position
                    + (Vector2)exitObject.transform.up * 0.7f;
                Assert.That(teleported, Is.True);
                Assert.That(body.position.x, Is.EqualTo(expectedPosition.x).Within(0.001f));
                Assert.That(body.position.y, Is.EqualTo(expectedPosition.y).Within(0.001f));
                Assert.That(body.simulated, Is.True);
                Assert.That(body.velocity, Is.EqualTo(originalVelocity));
                Assert.That(body.angularVelocity, Is.EqualTo(originalAngularVelocity).Within(0.001f));
                Assert.That(renderer.color, Is.EqualTo(originalColor));
                Assert.That(GetField<float>(exit, "lastTeleportTime"), Is.EqualTo(Time.time));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(entranceObject);
                UnityEngine.Object.DestroyImmediate(exitObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void OnValidate_ClampsTeleportDurationsToZero()
        {
            GameObject portalObject = CreatePortal("Portal", TestPairId, out Component portal);
            try
            {
                SetField(portal, "fadeOutDuration", -1f);
                SetField(portal, "hiddenDelay", -2f);
                SetField(portal, "fadeInDuration", -3f);
                SetField(portal, "spiralStrength", -4f);
                SetField(portal, "outputStartRadius", -5f);

                Invoke(portal, "OnValidate");

                Assert.That(GetField<float>(portal, "fadeOutDuration"), Is.Zero);
                Assert.That(GetField<float>(portal, "hiddenDelay"), Is.Zero);
                Assert.That(GetField<float>(portal, "fadeInDuration"), Is.Zero);
                Assert.That(GetField<float>(portal, "spiralStrength"), Is.Zero);
                Assert.That(GetField<float>(portal, "outputStartRadius"), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void PortalTransferEffect_EmitsInwardAndOutwardParticlesInPortalSpace()
        {
            GameObject portalObject = CreatePortal("Portal", TestPairId, out Component portal);
            try
            {
                ParticleSystem effect = GetField<ParticleSystem>(portal, "portalEffect");
                Assert.That(effect, Is.Not.Null);
                ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
                Assert.That(particleRenderer.renderMode, Is.EqualTo(ParticleSystemRenderMode.Stretch));
                Assert.That(
                    particleRenderer.lengthScale,
                    Is.EqualTo(GetField<float>(portal, "particleLengthScale")).Within(0.001f));
                Assert.That(
                    particleRenderer.velocityScale,
                    Is.EqualTo(GetField<float>(portal, "particleVelocityScale")).Within(0.001f));

                Invoke(portal, "PlayPortalInEffect");
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[32];
                int intakeCount = effect.GetParticles(particles);
                Assert.That(intakeCount, Is.EqualTo(18));
                for (int i = 0; i < intakeCount; i++)
                {
                    Vector2 offset = particles[i].position - portal.transform.position;
                    Assert.That(offset.magnitude, Is.InRange(0.3f, 0.8f));
                    Assert.That(Vector2.Dot(offset, particles[i].velocity), Is.LessThan(0f));
                }

                Invoke(portal, "StopPortalEffect");
                Invoke(portal, "PlayPortalOutEffect");
                int outputCount = effect.GetParticles(particles);
                Assert.That(outputCount, Is.EqualTo(24));
                for (int i = 0; i < outputCount; i++)
                {
                    Vector2 offset = particles[i].position - portal.transform.position;
                    Assert.That(
                        offset.magnitude,
                        Is.GreaterThan(0.01f));
                    Assert.That(
                        offset.magnitude,
                        Is.LessThan(0.5f));
                    Assert.That(Vector2.Dot(offset, particles[i].velocity), Is.GreaterThan(0f));
                    Assert.That(particles[i].velocity.sqrMagnitude, Is.GreaterThan(0.01f));
                }

                portalObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                Vector2 rotatedRadial = (Vector2)Invoke(portal, "CalculateWorldRadialDirection", 0f);
                Assert.That(rotatedRadial.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(rotatedRadial.y, Is.EqualTo(1f).Within(0.001f));
                Vector3 rotatedOutputSpawn = (Vector3)Invoke(
                    portal,
                    "CalculateParticleSpawnPosition",
                    portal.transform.position,
                    rotatedRadial,
                    false);
                Vector2 rotatedOutputOffset = rotatedOutputSpawn - portal.transform.position;
                Assert.That(rotatedOutputOffset.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(
                    rotatedOutputOffset.y,
                    Is.EqualTo(GetField<float>(portal, "outputStartRadius")).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void PortalTransferEffect_UsesOppositeSpiralDirections()
        {
            GameObject portalObject = CreatePortal("Portal", TestPairId, out Component portal);
            try
            {
                const float radialSpeed = 10f;
                Vector2 intakeVelocity = (Vector2)Invoke(
                    portal,
                    "CalculateSpiralVelocity",
                    Vector2.right,
                    radialSpeed,
                    true);
                Vector2 outputVelocity = (Vector2)Invoke(
                    portal,
                    "CalculateSpiralVelocity",
                    Vector2.right,
                    radialSpeed,
                    false);

                Assert.That(intakeVelocity.x, Is.EqualTo(-10f).Within(0.001f));
                Assert.That(intakeVelocity.y, Is.EqualTo(-7f).Within(0.001f));
                Assert.That(outputVelocity.x, Is.EqualTo(10f).Within(0.001f));
                Assert.That(outputVelocity.y, Is.EqualTo(7f).Within(0.001f));

                portalObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                Vector2 rotatedRadial = (Vector2)Invoke(portal, "CalculateWorldRadialDirection", 0f);
                Vector2 rotatedIntakeVelocity = (Vector2)Invoke(
                    portal,
                    "CalculateSpiralVelocity",
                    rotatedRadial,
                    radialSpeed,
                    true);
                Assert.That(rotatedIntakeVelocity.x, Is.EqualTo(7f).Within(0.001f));
                Assert.That(rotatedIntakeVelocity.y, Is.EqualTo(-10f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void TeleportCondition_RejectsInvalidStates()
        {
            Assert.That(CanStart(true, true, true, 1f, 0.35f), Is.True);
            Assert.That(CanStart(false, true, true, 1f, 0.35f), Is.False);
            Assert.That(CanStart(true, false, true, 1f, 0.35f), Is.False);
            Assert.That(CanStart(true, true, false, 1f, 0.35f), Is.False);
            Assert.That(CanStart(true, true, true, 0.2f, 0.35f), Is.False);
        }

        private static void AssertClip(Component portal, string fieldName, string assetPath)
        {
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            Assert.That(expected, Is.Not.Null);
            Assert.That(GetField<AudioClip>(portal, fieldName), Is.SameAs(expected));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.ADPCM));
            StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));
        }

        private static GameObject CreatePortal(string name, int pairId, out Component portal)
        {
            Assert.That(PortalType, Is.Not.Null);
            GameObject portalObject = new GameObject(name);
            portalObject.SetActive(false);
            BoxCollider2D collider = portalObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            portal = portalObject.AddComponent(PortalType);
            SetField(portal, "pairId", pairId);
            portalObject.SetActive(true);
            Invoke(portal, "Awake");
            Invoke(portal, "OnEnable");
            return portalObject;
        }

        private static GameObject CreateBall(
            out Component ball,
            out Rigidbody2D body,
            out SpriteRenderer renderer)
        {
            Assert.That(BallBaseType, Is.Not.Null);
            GameObject ballObject = new GameObject("PortalBall");
            ballObject.SetActive(false);
            renderer = ballObject.AddComponent<SpriteRenderer>();
            body = ballObject.AddComponent<Rigidbody2D>();
            ballObject.AddComponent<CircleCollider2D>();
            ball = ballObject.AddComponent(BallBaseType);
            ballObject.SetActive(true);
            MethodInfo awake = BallBaseType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(ball, null);
            return ballObject;
        }

        private static void ClearPortalRegistry()
        {
            if (PortalType == null)
            {
                return;
            }

            IList portals = (IList)PortalType.GetField("Portals", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            portals.Clear();

            object teleportingBalls = PortalType
                .GetField("TeleportingBalls", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            teleportingBalls.GetType().GetMethod("Clear").Invoke(teleportingBalls, null);
        }

        private static bool CanStart(
            bool simulating,
            bool hasBall,
            bool hasExit,
            float elapsed,
            float cooldown)
        {
            MethodInfo method = PortalType.GetMethod("CanStartTeleport", BindingFlags.Static | BindingFlags.NonPublic);
            return (bool)method.Invoke(null, new object[] { simulating, hasBall, hasExit, elapsed, cooldown });
        }

        private static object Invoke(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = PortalType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(component, arguments);
        }

        private static T GetField<T>(Component component, string fieldName)
        {
            return (T)PortalType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(component);
        }

        private static void SetField(Component component, string fieldName, object value)
        {
            PortalType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(component, value);
        }
    }
}
