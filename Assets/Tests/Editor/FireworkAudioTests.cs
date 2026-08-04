using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class FireworkAudioTests
    {
        private static Type FireworkType => Type.GetType("BallGame.Effects.FireworkEffect, Assembly-CSharp");

        [Test]
        public void FireworkClip_IsLoadedAndUsesExpectedImportSettings()
        {
            GameObject fireworkObject = CreateFirework(out Component firework, out _);
            try
            {
                AssertClip(firework, "fireworkSfx", "Assets/Resources/Audio/SFX/Firework.wav");
                AssertClip(firework, "fireworkPreSfx", "Assets/Resources/Audio/SFX/FireworkPre.wav");

                SetField(firework, "fireworkSfx", null);
                SetField(firework, "fireworkPreSfx", null);
                Assert.DoesNotThrow(() => Invoke(firework, "PlayExplosionSound"));
                Assert.DoesNotThrow(() => Invoke(firework, "PlayLiftSound"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fireworkObject);
            }
        }

        [Test]
        public void LiftEntryPoint_StartsParticlesAndHandlesMissingSoundSafely()
        {
            GameObject fireworkObject = CreateFireworkWithLift(out Component firework, out ParticleSystem lift);
            try
            {
                SetField(firework, "fireworkPreSfx", null);
                Assert.That(lift.isPlaying, Is.False);
                Assert.DoesNotThrow(() => Invoke(firework, "PlayLift"));
                Assert.That(lift.isPlaying, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fireworkObject);
            }
        }

        [Test]
        public void LiftConditions_RequireEnabledParticlesAndPositiveDuration()
        {
            GameObject fireworkObject = CreateFireworkWithLift(out Component firework, out ParticleSystem lift);
            try
            {
                Assert.That(Invoke(firework, "CanPlayLift"), Is.True);

                SetField(firework, "playLiftBeforeExplosion", false);
                Assert.That(Invoke(firework, "CanPlayLift"), Is.False);
                SetField(firework, "playLiftBeforeExplosion", true);

                SetField(firework, "liftSystems", Array.Empty<ParticleSystem>());
                Assert.That(Invoke(firework, "CanPlayLift"), Is.False);

                SetField(firework, "liftSystems", new[] { lift });
                SetField(firework, "liftDuration", 0f);
                Assert.That(Invoke(firework, "CanPlayLift"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fireworkObject);
            }
        }

        [Test]
        public void LiftSound_DeduplicatesSameFrameButAllowsLaterFrames()
        {
            GameObject fireworkObject = CreateFirework(out Component firework, out _);
            try
            {
                Assert.That(Invoke(firework, "TryClaimLiftSoundFrame", 100), Is.True);
                Assert.That(Invoke(firework, "TryClaimLiftSoundFrame", 100), Is.False);
                Assert.That(Invoke(firework, "TryClaimLiftSoundFrame", 101), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fireworkObject);
            }
        }

        [Test]
        public void ExplosionEntryPoint_StartsParticlesAndHandlesMissingSoundSafely()
        {
            GameObject fireworkObject = CreateFirework(out Component firework, out ParticleSystem explosion);
            try
            {
                SetField(firework, "fireworkSfx", null);
                Assert.That(explosion.isPlaying, Is.False);
                Assert.DoesNotThrow(() => Invoke(firework, "PlayExplosion"));
                Assert.That(explosion.isPlaying, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fireworkObject);
            }
        }

        [Test]
        public void SeparateFireworks_LoadIndependentExplosionPlaybackState()
        {
            GameObject firstObject = CreateFirework(out Component first, out ParticleSystem firstExplosion);
            GameObject secondObject = CreateFirework(out Component second, out ParticleSystem secondExplosion);
            try
            {
                Invoke(first, "PlayExplosion");
                Assert.That(firstExplosion.isPlaying, Is.True);
                Assert.That(secondExplosion.isPlaying, Is.False);

                Invoke(second, "PlayExplosion");
                Assert.That(secondExplosion.isPlaying, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        private static GameObject CreateFirework(out Component firework, out ParticleSystem explosion)
        {
            Assert.That(FireworkType, Is.Not.Null);
            GameObject fireworkObject = new GameObject("FireworkTest");
            fireworkObject.SetActive(false);
            GameObject explosionObject = new GameObject("Explosion");
            explosionObject.transform.SetParent(fireworkObject.transform, false);
            explosion = explosionObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = explosion.main;
            main.playOnAwake = false;

            firework = fireworkObject.AddComponent(FireworkType);
            SetField(firework, "playOnAwake", false);
            SetField(firework, "explosionSystems", new[] { explosion });
            fireworkObject.SetActive(true);
            return fireworkObject;
        }

        private static GameObject CreateFireworkWithLift(out Component firework, out ParticleSystem lift)
        {
            GameObject fireworkObject = CreateFirework(out firework, out _);
            fireworkObject.SetActive(false);
            GameObject liftObject = new GameObject("LiftShell");
            liftObject.transform.SetParent(fireworkObject.transform, false);
            lift = liftObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = lift.main;
            main.playOnAwake = false;
            SetField(firework, "liftSystems", new[] { lift });
            SetField(firework, "playLiftBeforeExplosion", true);
            SetField(firework, "liftDuration", 0.8f);
            fireworkObject.SetActive(true);
            return fireworkObject;
        }

        private static void AssertClip(Component firework, string fieldName, string assetPath)
        {
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            Assert.That(expected, Is.Not.Null);
            Assert.That(GetField<AudioClip>(firework, fieldName), Is.SameAs(expected));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.ADPCM));
            StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));
        }

        private static object Invoke(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(component, arguments);
        }

        private static T GetField<T>(Component component, string fieldName)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(component);
        }

        private static void SetField(Component component, string fieldName, object value)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(component, value);
        }
    }
}
