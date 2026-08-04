using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class BallHitAudioTests
    {
        private static Type BallType => Type.GetType("BallGame.Core.BallController, Assembly-CSharp");

        [Test]
        public void HitVolume_UsesConfiguredThresholdAndLinearMapping()
        {
            Assert.That(CalculateHitVolume(1.74f), Is.Zero);
            Assert.That(CalculateHitVolume(1.75f), Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(CalculateHitVolume((1.75f + 12f) * 0.5f), Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(CalculateHitVolume(12f), Is.EqualTo(1f));
            Assert.That(CalculateHitVolume(30f), Is.EqualTo(1f));
        }

        [Test]
        public void BallHitClip_IsMonoPreloadedPcm()
        {
            const string assetPath = "Assets/Resources/Audio/SFX/BallHit.wav";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;

            Assert.That(clip, Is.Not.Null);
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));
        }

        [Test]
        public void NormalImpactSpeed_IgnoresTangentialMotion()
        {
            Assert.That(CalculateNormalImpactSpeed(new Vector2(10f, 0f), Vector2.up), Is.Zero.Within(0.001f));
            Assert.That(CalculateNormalImpactSpeed(new Vector2(10f, -3f), Vector2.up), Is.EqualTo(3f).Within(0.001f));
            Assert.That(CalculateNormalImpactSpeed(new Vector2(0f, -5f), Vector2.zero), Is.Zero);
        }

        [Test]
        public void HitCondition_RejectsInvalidStatesAndCooldown()
        {
            Assert.That(ShouldPlay(true, false, false, false, false, 4f, 1f, 0f), Is.True);
            Assert.That(ShouldPlay(false, false, false, false, false, 4f, 1f, 0f), Is.False);
            Assert.That(ShouldPlay(true, true, false, false, false, 4f, 1f, 0f), Is.False);
            Assert.That(ShouldPlay(true, false, true, false, false, 4f, 1f, 0f), Is.False);
            Assert.That(ShouldPlay(true, false, false, true, false, 4f, 1f, 0f), Is.False);
            Assert.That(ShouldPlay(true, false, false, false, true, 4f, 1f, 0f), Is.False);
            Assert.That(ShouldPlay(true, false, false, false, false, 1f, 1f, 0f), Is.False);
            Assert.That(ShouldPlay(true, false, false, false, false, 4f, 1.07f, 1f), Is.False);
            Assert.That(ShouldPlay(true, false, false, false, false, 4f, 1.08f, 1f), Is.True);
        }

        [Test]
        public void BallPair_OnlyLowerInstanceIdPlays()
        {
            Assert.That(InvokeStatic<bool>("ShouldThisBallPlayForPair", 10, 20), Is.True);
            Assert.That(InvokeStatic<bool>("ShouldThisBallPlayForPair", 20, 10), Is.False);
        }

        [Test]
        public void SpringColliders_AreExcluded()
        {
            Type springType = Type.GetType("BallGame.Tools.SpringPad, Assembly-CSharp");
            Type freeSpringType = Type.GetType("SpringPadFree, Assembly-CSharp");
            Assert.That(springType, Is.Not.Null);
            Assert.That(freeSpringType, Is.Not.Null);

            GameObject regular = new GameObject("RegularSurface");
            GameObject spring = new GameObject("SpringSurface");
            GameObject freeSpring = new GameObject("FreeSpringSurface");
            try
            {
                Collider2D regularCollider = regular.AddComponent<BoxCollider2D>();
                Collider2D springCollider = spring.AddComponent<BoxCollider2D>();
                Collider2D freeSpringCollider = freeSpring.AddComponent<BoxCollider2D>();
                spring.AddComponent(springType);
                freeSpring.AddComponent(freeSpringType);

                Assert.That(InvokeStatic<bool>("IsSpringCollider", regularCollider), Is.False);
                Assert.That(InvokeStatic<bool>("IsSpringCollider", springCollider), Is.True);
                Assert.That(InvokeStatic<bool>("IsSpringCollider", freeSpringCollider), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(regular);
                UnityEngine.Object.DestroyImmediate(spring);
                UnityEngine.Object.DestroyImmediate(freeSpring);
            }
        }

        private static float CalculateHitVolume(float speed)
        {
            return InvokeStatic<float>("CalculateHitVolume", speed);
        }

        private static float CalculateNormalImpactSpeed(Vector2 velocity, Vector2 normal)
        {
            return InvokeStatic<float>("CalculateNormalImpactSpeed", velocity, normal);
        }

        private static bool ShouldPlay(
            bool dynamic,
            bool stopped,
            bool trigger,
            bool spring,
            bool duplicate,
            float speed,
            float now,
            float last)
        {
            return InvokeStatic<bool>(
                "ShouldPlayHitSound",
                dynamic,
                stopped,
                trigger,
                spring,
                duplicate,
                speed,
                now,
                last);
        }

        private static T InvokeStatic<T>(string methodName, params object[] arguments)
        {
            Assert.That(BallType, Is.Not.Null);
            MethodInfo method = BallType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(null, arguments);
        }
    }
}
