using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class AntiGravityAudioTests
    {
        private static Type AudioManagerType => Type.GetType("BallGame.Audio.AudioManager, Assembly-CSharp");
        private static Type FieldType => Type.GetType("BallGame.Tools.AntiGravityField, Assembly-CSharp");
        private static Type BallBaseType => Type.GetType("BallControllerBase, Assembly-CSharp");

        [SetUp]
        public void SetUp()
        {
            DestroyAudioManagers();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAudioManagers();
        }

        [Test]
        public void AntigravityClip_IsMonoPreloadedPcm()
        {
            const string assetPath = "Assets/Resources/Audio/SFX/Antigravity.wav";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;

            Assert.That(clip, Is.Not.Null);
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));
        }

        [Test]
        public void Reports_UseOneGlobalTargetUntilLastFieldStops()
        {
            Component manager = CreateAudioManager();
            GameObject fieldA = new GameObject("AntiGravityA");
            GameObject fieldB = new GameObject("AntiGravityB");
            try
            {
                InvokePublic(manager, "ReportAntiGravity", fieldA);
                InvokePublic(manager, "ReportAntiGravity", fieldA);
                InvokeNonPublic(manager, "RefreshAntiGravityTargets", Time.unscaledTime);
                Assert.That(GetField<float>(manager, "targetAntiGravityVolume"), Is.EqualTo(1f));
                Assert.That(GetCollectionCount(manager, "antiGravityContributions"), Is.EqualTo(1));

                InvokePublic(manager, "ReportAntiGravity", fieldB);
                InvokeNonPublic(manager, "RefreshAntiGravityTargets", Time.unscaledTime);
                Assert.That(GetCollectionCount(manager, "antiGravityContributions"), Is.EqualTo(2));
                Assert.That(GetField<float>(manager, "targetAntiGravityVolume"), Is.EqualTo(1f));

                InvokePublic(manager, "StopAntiGravity", fieldA);
                InvokeNonPublic(manager, "RefreshAntiGravityTargets", Time.unscaledTime);
                Assert.That(GetField<float>(manager, "targetAntiGravityVolume"), Is.EqualTo(1f));

                InvokePublic(manager, "StopAntiGravity", fieldB);
                InvokeNonPublic(manager, "RefreshAntiGravityTargets", Time.unscaledTime);
                Assert.That(GetField<float>(manager, "targetAntiGravityVolume"), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fieldA);
                UnityEngine.Object.DestroyImmediate(fieldB);
            }
        }

        [Test]
        public void Reports_RemoveStaleAndDestroyedFields()
        {
            Component manager = CreateAudioManager();
            GameObject field = new GameObject("AntiGravityField");
            InvokePublic(manager, "ReportAntiGravity", field);
            InvokeNonPublic(manager, "RefreshAntiGravityTargets", Time.unscaledTime + 0.21f);
            Assert.That(GetCollectionCount(manager, "antiGravityContributions"), Is.Zero);
            Assert.That(GetField<float>(manager, "targetAntiGravityVolume"), Is.Zero);

            field = new GameObject("DestroyedAntiGravityField");
            InvokePublic(manager, "ReportAntiGravity", field);
            UnityEngine.Object.DestroyImmediate(field);
            InvokeNonPublic(manager, "RefreshAntiGravityTargets", Time.unscaledTime);
            Assert.That(GetCollectionCount(manager, "antiGravityContributions"), Is.Zero);
        }

        [Test]
        public void FieldFilter_OnlyAcceptsDynamicBalls()
        {
            Assert.That(FieldType, Is.Not.Null);
            Assert.That(BallBaseType, Is.Not.Null);
            GameObject dynamicBall = CreateBody("DynamicBall", true, RigidbodyType2D.Dynamic, out Rigidbody2D dynamicBody);
            GameObject buildBall = CreateBody("BuildBall", true, RigidbodyType2D.Kinematic, out Rigidbody2D buildBody);
            GameObject otherBody = CreateBody("OtherBody", false, RigidbodyType2D.Dynamic, out Rigidbody2D nonBallBody);
            try
            {
                Assert.That(IsDynamicBall(dynamicBody), Is.True);
                Assert.That(IsDynamicBall(buildBody), Is.False);
                Assert.That(IsDynamicBall(nonBallBody), Is.False);
                Assert.That(IsDynamicBall(null), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dynamicBall);
                UnityEngine.Object.DestroyImmediate(buildBall);
                UnityEngine.Object.DestroyImmediate(otherBody);
            }
        }

        private static GameObject CreateBody(
            string name,
            bool addBall,
            RigidbodyType2D bodyType,
            out Rigidbody2D body)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.SetActive(false);
            body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = bodyType;
            gameObject.AddComponent<CircleCollider2D>();
            if (addBall)
            {
                gameObject.AddComponent(BallBaseType);
            }

            gameObject.SetActive(true);
            return gameObject;
        }

        private static bool IsDynamicBall(Rigidbody2D body)
        {
            MethodInfo method = FieldType.GetMethod("IsDynamicBall", BindingFlags.Static | BindingFlags.NonPublic);
            return (bool)method.Invoke(null, new object[] { body });
        }

        private static Component CreateAudioManager()
        {
            GameObject managerObject = new GameObject("AudioManagerTest");
            Component manager = managerObject.AddComponent(AudioManagerType);
            InvokeNonPublic(manager, "Awake");
            return manager;
        }

        private static object InvokePublic(Component component, string methodName, params object[] arguments)
        {
            return component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                .Invoke(component, arguments);
        }

        private static object InvokeNonPublic(Component component, string methodName, params object[] arguments)
        {
            return component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(component, arguments);
        }

        private static T GetField<T>(Component component, string fieldName)
        {
            return (T)component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(component);
        }

        private static int GetCollectionCount(Component component, string fieldName)
        {
            return ((ICollection)GetField<object>(component, fieldName)).Count;
        }

        private static void DestroyAudioManagers()
        {
            if (AudioManagerType == null)
            {
                return;
            }

            UnityEngine.Object[] managers = UnityEngine.Object.FindObjectsOfType(AudioManagerType);
            foreach (UnityEngine.Object found in managers)
            {
                Component manager = (Component)found;
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }

            AudioManagerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, null);
        }
    }
}
