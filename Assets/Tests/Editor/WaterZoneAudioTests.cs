using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class WaterZoneAudioTests
    {
        private static Type WaterZoneType => Type.GetType("BallGame.Tools.WaterZone, Assembly-CSharp");
        private static Type BallBaseType => Type.GetType("BallControllerBase, Assembly-CSharp");

        [Test]
        public void SplashClips_AreLoadedAndUseExpectedImportSettings()
        {
            GameObject waterObject = CreateWaterZone(out Component waterZone);
            try
            {
                AssertClip(waterZone, "splashInSfx", "Assets/Resources/Audio/SFX/SplashIn.wav");
                AssertClip(waterZone, "splashOutSfx", "Assets/Resources/Audio/SFX/SplashOut.wav");

                SetField(waterZone, "splashInSfx", null);
                SetField(waterZone, "splashOutSfx", null);
                Assert.DoesNotThrow(() => Invoke(waterZone, "PlaySplashInSound"));
                Assert.DoesNotThrow(() => Invoke(waterZone, "PlaySplashOutSound"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(waterObject);
            }
        }

        [Test]
        public void ContactState_EntersOnceAndExitsAfterFinalCollider()
        {
            GameObject waterObject = CreateWaterZone(out Component waterZone);
            GameObject ballObject = CreateBall(RigidbodyType2D.Dynamic, out Component ball, out Collider2D firstCollider);
            CircleCollider2D secondCollider = ballObject.AddComponent<CircleCollider2D>();
            try
            {
                Assert.That(Register(waterZone, ball, firstCollider), Is.True);
                Assert.That(Register(waterZone, ball, firstCollider), Is.False, "Staying inside must not re-enter.");
                Assert.That(Register(waterZone, ball, secondCollider), Is.False, "Extra colliders must not re-enter.");

                Assert.That(Remove(waterZone, ball, firstCollider), Is.False, "A partial exit is not a ball exit.");
                Assert.That(Remove(waterZone, ball, secondCollider), Is.True, "The final collider produces the exit.");
                Assert.That(Register(waterZone, ball, firstCollider), Is.True, "A complete exit allows re-entry.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(waterObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void InvalidBallState_ClearsContactsAndDisableAllowsFreshEntry()
        {
            GameObject waterObject = CreateWaterZone(out Component waterZone);
            GameObject ballObject = CreateBall(RigidbodyType2D.Dynamic, out Component ball, out Collider2D collider);
            try
            {
                Assert.That(Register(waterZone, ball, collider), Is.True);
                ballObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                Invoke(waterZone, "CleanupInvalidImmersions");
                Assert.That(ContactCount(waterZone), Is.Zero);

                ballObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                Assert.That(Register(waterZone, ball, collider), Is.True);
                Invoke(waterZone, "OnDisable");
                Assert.That(ContactCount(waterZone), Is.Zero);
                Assert.That(Register(waterZone, ball, collider), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(waterObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void DestroyedOrDisabledCollider_IsCleanedWithoutAnExitTransition()
        {
            GameObject waterObject = CreateWaterZone(out Component waterZone);
            GameObject ballObject = CreateBall(RigidbodyType2D.Dynamic, out Component ball, out Collider2D collider);
            try
            {
                Assert.That(Register(waterZone, ball, collider), Is.True);
                collider.enabled = false;
                Invoke(waterZone, "CleanupInvalidImmersions");
                Assert.That(ContactCount(waterZone), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(waterObject);
                UnityEngine.Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void UnderwaterReporting_FollowsWhetherAnyDynamicBallIsImmersed()
        {
            GameObject waterObject = CreateWaterZone(out Component waterZone);
            GameObject ballObjectA = CreateBall(RigidbodyType2D.Dynamic, out Component ballA, out Collider2D colliderA);
            GameObject ballObjectB = CreateBall(RigidbodyType2D.Dynamic, out Component ballB, out Collider2D colliderB);
            try
            {
                Assert.That(ContactCount(waterZone), Is.Zero);
                Assert.That(Register(waterZone, ballA, colliderA), Is.True);
                Assert.That(Register(waterZone, ballB, colliderB), Is.True);
                Assert.That(ContactCount(waterZone), Is.EqualTo(2));

                Assert.That(Remove(waterZone, ballA, colliderA), Is.True);
                Assert.That(ContactCount(waterZone), Is.EqualTo(1));
                Assert.That(Remove(waterZone, ballB, colliderB), Is.True);
                Assert.That(ContactCount(waterZone), Is.Zero);
                Assert.DoesNotThrow(() => Invoke(waterZone, "UpdateUnderwaterReport"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(waterObject);
                UnityEngine.Object.DestroyImmediate(ballObjectA);
                UnityEngine.Object.DestroyImmediate(ballObjectB);
            }
        }

        private static void AssertClip(Component waterZone, string fieldName, string assetPath)
        {
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            Assert.That(expected, Is.Not.Null);
            Assert.That(GetField<AudioClip>(waterZone, fieldName), Is.SameAs(expected));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.ADPCM));
            StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));
        }

        private static GameObject CreateWaterZone(out Component waterZone)
        {
            Assert.That(WaterZoneType, Is.Not.Null);
            GameObject waterObject = new GameObject("WaterZoneTest");
            waterObject.SetActive(false);
            waterObject.AddComponent<BoxCollider2D>();
            waterZone = waterObject.AddComponent(WaterZoneType);
            waterObject.SetActive(true);
            return waterObject;
        }

        private static GameObject CreateBall(
            RigidbodyType2D bodyType,
            out Component ball,
            out Collider2D collider)
        {
            Assert.That(BallBaseType, Is.Not.Null);
            GameObject ballObject = new GameObject("WaterBall");
            ballObject.SetActive(false);
            Rigidbody2D body = ballObject.AddComponent<Rigidbody2D>();
            body.bodyType = bodyType;
            collider = ballObject.AddComponent<CircleCollider2D>();
            ball = ballObject.AddComponent(BallBaseType);
            ballObject.SetActive(true);
            return ballObject;
        }

        private static bool Register(Component waterZone, Component ball, Collider2D collider)
        {
            return (bool)Invoke(waterZone, "RegisterImmersedContact", ball, collider);
        }

        private static bool Remove(Component waterZone, Component ball, Collider2D collider)
        {
            return (bool)Invoke(waterZone, "RemoveImmersedContact", ball, collider);
        }

        private static int ContactCount(Component waterZone)
        {
            IDictionary contacts = GetField<IDictionary>(waterZone, "immersedContacts");
            return contacts.Count;
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
