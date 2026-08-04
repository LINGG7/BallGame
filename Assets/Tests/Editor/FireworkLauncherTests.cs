using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class FireworkLauncherTests
    {
        private static Type LauncherType => Type.GetType("BallGame.Effects.FireworkLauncher, Assembly-CSharp");
        private static Type FireworkType => Type.GetType("BallGame.Effects.FireworkEffect, Assembly-CSharp");

        [Test]
        public void LauncherPrefab_DoesNotLoopAutomaticallyOnEnable()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Prefabs/FireworkLauncher.prefab");
            Assert.That(prefab, Is.Not.Null);
            Component launcher = prefab.GetComponent(LauncherType);
            Assert.That(launcher, Is.Not.Null);
            Assert.That(GetField<bool>(launcher, "playOnEnable"), Is.False);
        }

        [Test]
        public void SingleRoundCount_IsClampedToAtLeastOne()
        {
            Assert.That(InvokeStatic("ClampSingleRoundCount", -3), Is.EqualTo(1));
            Assert.That(InvokeStatic("ClampSingleRoundCount", 0), Is.EqualTo(1));
            Assert.That(InvokeStatic("ClampSingleRoundCount", 4), Is.EqualTo(4));
        }

        [Test]
        public void SingleRound_LaunchesRequestedCountAndStopCleansChildren()
        {
            GameObject launcherObject = CreateLauncher(out Component launcher, out GameObject fireworkTemplate);
            try
            {
                SetField(launcher, "launchInterval", 0f);
                IEnumerator round = (IEnumerator)Invoke(launcher, "LaunchSingleRound", 4);
                Assert.That(round.MoveNext(), Is.True);
                Assert.That(launcherObject.GetComponentsInChildren(FireworkType, true).Length, Is.EqualTo(4));
                Assert.That(round.MoveNext(), Is.False);

                InvokePublic(launcher, "Stop");
                Assert.That(launcherObject.GetComponentsInChildren(FireworkType, true).Length, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(launcherObject);
                UnityEngine.Object.DestroyImmediate(fireworkTemplate);
            }
        }

        [Test]
        public void CameraConfiguration_KeepsLaunchAreaInsideOrthographicView()
        {
            GameObject launcherObject = CreateLauncher(out Component launcher, out GameObject fireworkTemplate);
            GameObject cameraObject = new GameObject("FireworkCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 2f;
            camera.transform.position = new Vector3(3f, 2f, -10f);
            try
            {
                Invoke(launcher, "ConfigureForCamera", camera);
                Assert.That(launcherObject.transform.position.x, Is.EqualTo(3f).Within(0.001f));
                Assert.That(launcherObject.transform.position.y, Is.EqualTo(-2.5f).Within(0.001f));
                Assert.That(GetField<float>(launcher, "horizontalRange"), Is.EqualTo(16f).Within(0.001f));
                Assert.That(GetField<float>(launcher, "minimumExplosionHeight"), Is.EqualTo(3f).Within(0.001f));
                Assert.That(GetField<float>(launcher, "maximumExplosionHeight"), Is.EqualTo(7f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(launcherObject);
                UnityEngine.Object.DestroyImmediate(fireworkTemplate);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static GameObject CreateLauncher(out Component launcher, out GameObject fireworkTemplate)
        {
            Assert.That(LauncherType, Is.Not.Null);
            Assert.That(FireworkType, Is.Not.Null);
            GameObject launcherObject = new GameObject("FireworkLauncherTest");
            launcher = launcherObject.AddComponent(LauncherType);

            fireworkTemplate = new GameObject("FireworkTemplate");
            fireworkTemplate.SetActive(false);
            GameObject explosionObject = new GameObject("Explosion");
            explosionObject.transform.SetParent(fireworkTemplate.transform, false);
            ParticleSystem explosion = explosionObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = explosion.main;
            main.playOnAwake = false;
            Component firework = fireworkTemplate.AddComponent(FireworkType);
            SetField(firework, "playOnAwake", false);
            SetField(firework, "destroyAfterFinished", false);
            SetField(firework, "playLiftBeforeExplosion", false);
            SetField(firework, "explosionSystems", new[] { explosion });
            SetField(launcher, "fireworkPrefab", firework);
            fireworkTemplate.SetActive(true);
            return launcherObject;
        }

        private static object Invoke(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = LauncherType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(component, arguments);
        }

        private static object InvokePublic(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = LauncherType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(component, arguments);
        }

        private static object InvokeStatic(string methodName, params object[] arguments)
        {
            MethodInfo method = LauncherType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(null, arguments);
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
