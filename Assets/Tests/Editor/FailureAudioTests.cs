using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class FailureAudioTests
    {
        private static Type ManagerType => Type.GetType("BallGame.Core.GameManager, Assembly-CSharp");
        private static Type StateType => Type.GetType("BallGame.Core.GameState, Assembly-CSharp");

        [Test]
        public void FailureClip_IsLoadedAndMissingClipIsSafe()
        {
            const string assetPath = "Assets/Resources/Audio/SFX/Failure.wav";
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            GameObject host = CreateManager(out Component manager);
            try
            {
                Assert.That(expected, Is.Not.Null);
                Assert.That(GetField<AudioClip>(manager, "failureSfx"), Is.SameAs(expected));
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.forceToMono, Is.True);
                StringAssert.Contains("preloadAudioData: 1", File.ReadAllText(assetPath + ".meta"));

                SetField(manager, "failureSfx", null);
                Assert.DoesNotThrow(() => InvokeNonPublic(manager, "PlayFailureSound"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void FailureCondition_OnlyAcceptsSimulationState()
        {
            Assert.That(CanEnterFailure("Simulate"), Is.True);
            Assert.That(CanEnterFailure("Build"), Is.False);
            Assert.That(CanEnterFailure("Intro"), Is.False);
            Assert.That(CanEnterFailure("Success"), Is.False);
            Assert.That(CanEnterFailure("Failed"), Is.False);
        }

        [Test]
        public void FailLevel_TransitionsOnceAndSchedulesSingleReset()
        {
            GameObject[] sceneBalls = DisableActiveSceneBalls();
            GameObject host = CreateManager(out Component manager);
            try
            {
                SetState(manager, "Simulate");
                InvokePublic(manager, "FailLevel");
                Assert.That(GetState(manager), Is.EqualTo("Failed"));

                float firstResetAt = GetField<float>(manager, "failureResetAtUnscaledTime");
                Assert.That(firstResetAt - Time.unscaledTime, Is.InRange(1.9f, 2.01f));

                InvokePublic(manager, "FailLevel");
                Assert.That(GetField<float>(manager, "failureResetAtUnscaledTime"), Is.EqualTo(firstResetAt));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                RestoreSceneBalls(sceneBalls);
            }
        }

        [Test]
        public void LaterSimulation_CanFailAgainAfterReturningToBuild()
        {
            GameObject[] sceneBalls = DisableActiveSceneBalls();
            GameObject host = CreateManager(out Component manager);
            try
            {
                SetState(manager, "Simulate");
                InvokePublic(manager, "FailLevel");
                Assert.That(GetState(manager), Is.EqualTo("Failed"));

                SetState(manager, "Build");
                SetState(manager, "Simulate");
                InvokePublic(manager, "FailLevel");
                Assert.That(GetState(manager), Is.EqualTo("Failed"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                RestoreSceneBalls(sceneBalls);
            }
        }

        private static GameObject[] DisableActiveSceneBalls()
        {
            Type ballType = Type.GetType("BallGame.Core.BallController, Assembly-CSharp");
            GameObject[] balls = UnityEngine.Object.FindObjectsOfType(ballType)
                .Cast<Component>()
                .Select(component => component.gameObject)
                .ToArray();
            foreach (GameObject ball in balls)
            {
                ball.SetActive(false);
            }

            return balls;
        }

        private static void RestoreSceneBalls(GameObject[] balls)
        {
            foreach (GameObject ball in balls)
            {
                if (ball != null)
                {
                    ball.SetActive(true);
                }
            }
        }

        private static GameObject CreateManager(out Component manager)
        {
            Assert.That(ManagerType, Is.Not.Null);
            GameObject host = new GameObject("FailureAudioTest");
            manager = host.AddComponent(ManagerType);
            InvokeNonPublic(manager, "Awake");
            return host;
        }

        private static bool CanEnterFailure(string stateName)
        {
            object state = Enum.Parse(StateType, stateName);
            MethodInfo method = ManagerType.GetMethod(
                "CanEnterFailure",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new[] { state });
        }

        private static void SetState(Component manager, string stateName)
        {
            object state = Enum.Parse(StateType, stateName);
            ManagerType.GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, state);
        }

        private static string GetState(Component manager)
        {
            return ManagerType.GetProperty("State", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(manager).ToString();
        }

        private static object InvokePublic(Component manager, string methodName)
        {
            return ManagerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                .Invoke(manager, null);
        }

        private static object InvokeNonPublic(Component manager, string methodName)
        {
            return ManagerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);
        }

        private static T GetField<T>(Component manager, string fieldName)
        {
            return (T)ManagerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(manager);
        }

        private static void SetField(Component manager, string fieldName, object value)
        {
            ManagerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, value);
        }
    }
}
