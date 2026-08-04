using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class FanZoneAudioTests
    {
        private static Type FanType => Type.GetType("BallGame.Tools.FanZone, Assembly-CSharp");
        private static Type BallBaseType => Type.GetType("BallControllerBase, Assembly-CSharp");

        [Test]
        public void AirWooshClip_IsLoadedAndUsesExpectedImportSettings()
        {
            const string assetPath = "Assets/Resources/Audio/SFX/AirWoosh.wav";
            AudioClip expectedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            GameObject fanObject = CreateFan(out Component fan);
            try
            {
                Assert.That(expectedClip, Is.Not.Null);
                Assert.That(GetField<AudioClip>(fan, "airWooshSfx"), Is.SameAs(expectedClip));
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.forceToMono, Is.True);
                Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
                StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));

                SetField(fan, "airWooshSfx", null);
                Assert.DoesNotThrow(() => Invoke(fan, "PlayAirWoosh"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fanObject);
            }
        }

        [Test]
        public void EntryState_PlaysOnceWhileInsideAndAgainAfterLeaving()
        {
            GameObject fanObject = CreateFan(out Component fan);
            GameObject ballObject = CreateBall("Ball", RigidbodyType2D.Dynamic, out Component ball);
            try
            {
                BeginStep(fan);
                Assert.That(Register(fan, ball), Is.True);
                Assert.That(Register(fan, ball), Is.False, "Multiple colliders in one step must not retrigger.");
                CompleteStep(fan);

                BeginStep(fan);
                Assert.That(Register(fan, ball), Is.False, "A ball staying inside must not retrigger.");
                CompleteStep(fan);

                BeginStep(fan);
                CompleteStep(fan);

                BeginStep(fan);
                Assert.That(Register(fan, ball), Is.True, "A complete exit must allow a later re-entry.");
                CompleteStep(fan);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fanObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void EntryState_HandlesMultipleBallsFansAndNonDynamicBalls()
        {
            GameObject fanObjectA = CreateFan(out Component fanA);
            GameObject fanObjectB = CreateFan(out Component fanB);
            GameObject ballObjectA = CreateBall("BallA", RigidbodyType2D.Dynamic, out Component ballA);
            GameObject ballObjectB = CreateBall("BallB", RigidbodyType2D.Dynamic, out Component ballB);
            GameObject buildBallObject = CreateBall("BuildBall", RigidbodyType2D.Kinematic, out Component buildBall);
            try
            {
                BeginStep(fanA);
                Assert.That(Register(fanA, ballA), Is.True);
                Assert.That(Register(fanA, ballB), Is.True);
                Assert.That(Register(fanA, buildBall), Is.False);
                CompleteStep(fanA);

                BeginStep(fanB);
                Assert.That(Register(fanB, ballA), Is.True, "Each fan must track entries independently.");
                CompleteStep(fanB);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fanObjectA);
                UnityEngine.Object.DestroyImmediate(fanObjectB);
                UnityEngine.Object.DestroyImmediate(ballObjectA);
                UnityEngine.Object.DestroyImmediate(ballObjectB);
                UnityEngine.Object.DestroyImmediate(buildBallObject);
            }
        }

        [Test]
        public void ClearingState_AllowsCurrentBallToEnterAgain()
        {
            GameObject fanObject = CreateFan(out Component fan);
            GameObject ballObject = CreateBall("Ball", RigidbodyType2D.Dynamic, out Component ball);
            try
            {
                BeginStep(fan);
                Assert.That(Register(fan, ball), Is.True);
                CompleteStep(fan);

                Invoke(fan, "ClearTrackedBalls");
                BeginStep(fan);
                Assert.That(Register(fan, ball), Is.True);
                CompleteStep(fan);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fanObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        private static GameObject CreateFan(out Component fan)
        {
            Assert.That(FanType, Is.Not.Null);
            GameObject fanObject = new GameObject("FanTest");
            fanObject.SetActive(false);
            fanObject.AddComponent<BoxCollider2D>();
            fan = fanObject.AddComponent(FanType);
            fanObject.SetActive(true);
            Invoke(fan, "Awake");
            return fanObject;
        }

        private static GameObject CreateBall(string name, RigidbodyType2D bodyType, out Component ball)
        {
            Assert.That(BallBaseType, Is.Not.Null);
            GameObject ballObject = new GameObject(name);
            ballObject.SetActive(false);
            Rigidbody2D body = ballObject.AddComponent<Rigidbody2D>();
            body.bodyType = bodyType;
            ballObject.AddComponent<CircleCollider2D>();
            ball = ballObject.AddComponent(BallBaseType);
            ballObject.SetActive(true);
            Invoke(ball, "Awake");
            return ballObject;
        }

        private static void BeginStep(Component fan)
        {
            Invoke(fan, "BeginDetectionStep");
        }

        private static bool Register(Component fan, Component ball)
        {
            return (bool)Invoke(fan, "RegisterBallForCurrentStep", ball);
        }

        private static void CompleteStep(Component fan)
        {
            Invoke(fan, "CompleteDetectionStep");
        }

        private static object Invoke(Component component, string methodName, params object[] arguments)
        {
            Type type = component.GetType();
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                method = type.BaseType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            }

            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(component, arguments);
        }

        private static T GetField<T>(Component component, string fieldName)
        {
            return (T)component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(component);
        }

        private static void SetField(Component component, string fieldName, object value)
        {
            component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(component, value);
        }
    }
}
