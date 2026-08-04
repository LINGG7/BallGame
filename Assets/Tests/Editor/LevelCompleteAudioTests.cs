using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallGame.Tests
{
    public sealed class LevelCompleteAudioTests
    {
        private static Type PopupType => Type.GetType("BallGame.UI.LevelCompletePopup, Assembly-CSharp");

        [Test]
        public void Popup_LoadsLevelCompletedClipAndHandlesMissingClipSafely()
        {
            GameObject host = CreatePopup(out Component popup, out _);
            try
            {
                AudioClip expected = Resources.Load<AudioClip>("Audio/SFX/LevelCompleted");
                Assert.That(expected, Is.Not.Null);
                Assert.That(GetField<AudioClip>(popup, "levelCompletedSfx"), Is.SameAs(expected));
                Assert.That(GetField<Component>(popup, "fireworkLauncherPrefab"), Is.Not.Null);

                SetField(popup, "levelCompletedSfx", null);
                Assert.DoesNotThrow(() => Invoke(popup, "PlayLevelCompletedSound"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DisplayState_TriggersOnceUntilPopupIsHidden()
        {
            GameObject host = CreatePopup(out Component popup, out RectTransform overlay);
            try
            {
                Assert.That(overlay.gameObject.activeSelf, Is.False);

                Invoke(popup, "Show");
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                Assert.That(GetField<bool>(popup, "isShown"), Is.True);
                Component firstLauncher = GetField<Component>(popup, "activeSuccessFireworkLauncher");
                Assert.That(firstLauncher, Is.Not.Null);
                Assert.That((bool)Invoke(popup, "TryMarkShown"), Is.False,
                    "Repeated Success or Show calls must not count as another appearance.");

                Invoke(popup, "Show");
                Assert.That(GetField<bool>(popup, "isShown"), Is.True);
                Assert.That(GetField<Component>(popup, "activeSuccessFireworkLauncher"), Is.SameAs(firstLauncher));

                Invoke(popup, "HideImmediate");
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(GetField<bool>(popup, "isShown"), Is.False);
                Assert.That(GetField<Component>(popup, "activeSuccessFireworkLauncher"), Is.Null);
                Assert.That((bool)Invoke(popup, "TryMarkShown"), Is.True,
                    "A later completion after hiding must be able to play again.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SuccessFireworkCount_IsAlwaysBetweenThreeAndFive()
        {
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(20260721);
            bool observedMinimum = false;
            bool observedMaximum = false;
            try
            {
                for (int i = 0; i < 256; i++)
                {
                    int count = (int)InvokeStatic("GetSuccessFireworkCount");
                    Assert.That(count, Is.InRange(3, 5));
                    observedMinimum |= count == 3;
                    observedMaximum |= count == 5;
                }
            }
            finally
            {
                UnityEngine.Random.state = previousState;
            }

            Assert.That(observedMinimum, Is.True);
            Assert.That(observedMaximum, Is.True);
        }

        [Test]
        public void MissingOverlay_DoesNotMarkPopupAsShown()
        {
            Assert.That(PopupType, Is.Not.Null);
            GameObject host = new GameObject("MissingPopupTest");
            host.SetActive(false);
            Component popup = host.AddComponent(PopupType);
            try
            {
                LogAssert.Expect(LogType.Warning, "LevelCompletePopup is missing popup prefab or overlay reference.");
                Invoke(popup, "Show");
                Assert.That(GetField<bool>(popup, "isShown"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static GameObject CreatePopup(out Component popup, out RectTransform overlay)
        {
            Assert.That(PopupType, Is.Not.Null);
            GameObject host = new GameObject("LevelCompletePopupTest");
            host.SetActive(false);
            popup = host.AddComponent(PopupType);

            GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform));
            overlayObject.transform.SetParent(host.transform, false);
            overlay = overlayObject.GetComponent<RectTransform>();
            SetField(popup, "overlayRoot", overlay);

            host.SetActive(true);
            Invoke(popup, "Awake");
            return host;
        }

        private static object Invoke(Component component, string methodName, params object[] arguments)
        {
            MethodInfo method = PopupType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(component, arguments);
        }

        private static object InvokeStatic(string methodName, params object[] arguments)
        {
            MethodInfo method = PopupType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(null, arguments);
        }

        private static T GetField<T>(Component component, string fieldName)
        {
            return (T)PopupType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(component);
        }

        private static void SetField(Component component, string fieldName, object value)
        {
            PopupType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(component, value);
        }
    }
}
