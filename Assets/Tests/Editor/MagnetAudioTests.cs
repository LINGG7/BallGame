using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class MagnetAudioTests
    {
        private static Type MagnetType => Type.GetType("BallGame.Tools.MagnetTool, Assembly-CSharp");
        private static Type BallBaseType => Type.GetType("BallControllerBase, Assembly-CSharp");

        [Test]
        public void MagnetClips_AreLoadedAndUseExpectedImportSettings()
        {
            GameObject magnetObject = CreateMagnet(false, out Component magnet);
            try
            {
                AssertClip(magnet, "attractSfx", "Assets/Resources/Audio/SFX/Attract.wav");
                AssertClip(magnet, "repulsiveSfx", "Assets/Resources/Audio/SFX/Repulsive.wav");

                SetField(magnet, "attractSfx", null);
                SetField(magnet, "repulsiveSfx", null);
                Assert.DoesNotThrow(() => Invoke(magnet, "PlayEntrySound"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(magnetObject);
            }
        }

        [Test]
        public void EntrySound_UsesAttractForPositiveAndRepulsiveForNegativeMagnet()
        {
            GameObject positiveObject = CreateMagnet(false, out Component positive);
            GameObject negativeObject = CreateMagnet(true, out Component negative);
            try
            {
                Assert.That(Invoke(positive, "GetEntrySfx"), Is.SameAs(GetField<AudioClip>(positive, "attractSfx")));
                Assert.That(Invoke(negative, "GetEntrySfx"), Is.SameAs(GetField<AudioClip>(negative, "repulsiveSfx")));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(positiveObject);
                UnityEngine.Object.DestroyImmediate(negativeObject);
            }
        }

        [Test]
        public void EntryState_PlaysOnceWhileInsideAndAgainAfterLeaving()
        {
            GameObject magnetObject = CreateMagnet(false, out Component magnet);
            GameObject ballObject = CreateBall("Ball", RigidbodyType2D.Dynamic, out Component ball);
            try
            {
                BeginStep(magnet);
                Assert.That(Register(magnet, ball), Is.True);
                Assert.That(Register(magnet, ball), Is.False, "Multiple colliders in one step must not retrigger.");
                CompleteStep(magnet);

                BeginStep(magnet);
                Assert.That(Register(magnet, ball), Is.False, "A ball staying in range must not retrigger.");
                CompleteStep(magnet);

                BeginStep(magnet);
                CompleteStep(magnet);

                BeginStep(magnet);
                Assert.That(Register(magnet, ball), Is.True, "A complete exit must allow a later re-entry.");
                CompleteStep(magnet);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(magnetObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void EntryState_HandlesMultipleBallsMagnetsAndNonDynamicBalls()
        {
            GameObject magnetObjectA = CreateMagnet(false, out Component magnetA);
            GameObject magnetObjectB = CreateMagnet(true, out Component magnetB);
            GameObject ballObjectA = CreateBall("BallA", RigidbodyType2D.Dynamic, out Component ballA);
            GameObject ballObjectB = CreateBall("BallB", RigidbodyType2D.Dynamic, out Component ballB);
            GameObject buildBallObject = CreateBall("BuildBall", RigidbodyType2D.Kinematic, out Component buildBall);
            try
            {
                BeginStep(magnetA);
                Assert.That(Register(magnetA, ballA), Is.True);
                Assert.That(Register(magnetA, ballB), Is.True);
                Assert.That(Register(magnetA, buildBall), Is.False);
                CompleteStep(magnetA);

                BeginStep(magnetB);
                Assert.That(Register(magnetB, ballA), Is.True, "Each magnet tracks entries independently.");
                CompleteStep(magnetB);

                Invoke(magnetA, "ClearTrackedBalls");
                BeginStep(magnetA);
                Assert.That(Register(magnetA, ballA), Is.True, "Clearing state must allow a fresh entry.");
                CompleteStep(magnetA);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(magnetObjectA);
                UnityEngine.Object.DestroyImmediate(magnetObjectB);
                UnityEngine.Object.DestroyImmediate(ballObjectA);
                UnityEngine.Object.DestroyImmediate(ballObjectB);
                UnityEngine.Object.DestroyImmediate(buildBallObject);
            }
        }

        private static void AssertClip(Component magnet, string fieldName, string assetPath)
        {
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            Assert.That(expected, Is.Not.Null);
            Assert.That(GetField<AudioClip>(magnet, fieldName), Is.SameAs(expected));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.ADPCM));
            StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));
        }

        private static GameObject CreateMagnet(bool repels, out Component magnet)
        {
            Assert.That(MagnetType, Is.Not.Null);
            GameObject magnetObject = new GameObject(repels ? "Magnet-" : "Magnet+");
            magnetObject.SetActive(false);
            magnet = magnetObject.AddComponent(MagnetType);
            SetField(magnet, "repels", repels);
            magnetObject.SetActive(true);
            return magnetObject;
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
            return ballObject;
        }

        private static void BeginStep(Component magnet)
        {
            Invoke(magnet, "BeginDetectionStep");
        }

        private static bool Register(Component magnet, Component ball)
        {
            return (bool)Invoke(magnet, "RegisterBallForCurrentStep", ball);
        }

        private static void CompleteStep(Component magnet)
        {
            Invoke(magnet, "CompleteDetectionStep");
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
