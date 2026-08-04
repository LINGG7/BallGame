using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.Tests
{
    public sealed class SettingsPanelTests
    {
        private const string MainMenuPrefabPath = "Assets/Resources/UI/MainMenuUI.prefab";
        private static Type AudioManagerType => Type.GetType("BallGame.Audio.AudioManager, Assembly-CSharp");
        private static Type SettingsType => Type.GetType("BallGame.UI.HapticsSettingsPanelController, Assembly-CSharp");

        private GameObject menuInstance;
        private Component audioManager;
        private Component settingsController;

        [SetUp]
        public void SetUp()
        {
            DestroyExistingAudioManagers();
            ClearAudioSettings();
            audioManager = CreateComponent(AudioManagerType, "AudioManagerTest");
            Invoke(AudioManagerType, audioManager, "Awake", BindingFlags.Instance | BindingFlags.NonPublic);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            menuInstance = UnityEngine.Object.Instantiate(prefab);
            settingsController = menuInstance.GetComponents<MonoBehaviour>()
                .First(component => component != null && component.GetType() == SettingsType);
            Invoke(SettingsType, settingsController, "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [TearDown]
        public void TearDown()
        {
            if (menuInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(menuInstance);
            }

            DestroyExistingAudioManagers();
            ClearAudioSettings();
        }

        [Test]
        public void SettingsModal_BuildsTabsAndDefaultsToAudioPage()
        {
            InvokePublic(settingsController, "Show");

            Transform modal = menuInstance.transform.Find("SettingsModal");
            Transform panel = modal?.Find("Panel");
            Transform audioPage = panel?.Find("AudioPage");
            Transform hapticsPage = panel?.Find("HapticsPage");
            Assert.That(menuInstance.transform.Find("SettingsButton"), Is.Not.Null);
            Assert.That(panel?.Find("AudioTabButton"), Is.Not.Null);
            Assert.That(panel?.Find("HapticsTabButton"), Is.Not.Null);
            Assert.That(modal.gameObject.activeSelf, Is.True);
            Assert.That(audioPage.gameObject.activeSelf, Is.True);
            Assert.That(hapticsPage.gameObject.activeSelf, Is.False);

            InvokePublic(settingsController, "ShowHapticsPage");
            Assert.That(audioPage.gameObject.activeSelf, Is.False);
            Assert.That(hapticsPage.gameObject.activeSelf, Is.True);

            RectTransform[] options = hapticsPage.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Intensity", StringComparison.Ordinal))
                .Select(button => button.GetComponent<RectTransform>())
                .OrderBy(rect => rect.anchoredPosition.x)
                .ToArray();
            Assert.That(options.Length, Is.EqualTo(4));
            Assert.That(options.Select(rect => rect.anchoredPosition.y).Distinct().Count(), Is.EqualTo(1));
            Assert.That(options[0].anchoredPosition.x, Is.LessThan(options[3].anchoredPosition.x));
        }

        [Test]
        public void AudioSliders_SyncPersistAndDisplayPercentages()
        {
            InvokePublic(audioManager, "SetMusicVolume", 0.42f);
            InvokePublic(audioManager, "SetSfxVolume", 0.73f);
            InvokePublic(settingsController, "Show");

            Transform panel = menuInstance.transform.Find("SettingsModal/Panel");
            Slider musicSlider = panel.Find("AudioPage/MusicSlider").GetComponent<Slider>();
            Slider sfxSlider = panel.Find("AudioPage/SfxSlider").GetComponent<Slider>();
            Component musicPercent = panel.Find("AudioPage/MusicPercent").GetComponent("TMP_Text");
            Component sfxPercent = panel.Find("AudioPage/SfxPercent").GetComponent("TMP_Text");
            Assert.That(musicSlider.value, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(sfxSlider.value, Is.EqualTo(0.73f).Within(0.001f));
            Assert.That(GetProperty<string>(musicPercent, "text"), Is.EqualTo("42%"));
            Assert.That(GetProperty<string>(sfxPercent, "text"), Is.EqualTo("73%"));

            musicSlider.value = 0.256f;
            sfxSlider.value = 0f;
            Assert.That(GetProperty<float>(audioManager, "MusicVolume"), Is.EqualTo(0.256f).Within(0.001f));
            Assert.That(GetProperty<float>(audioManager, "SfxVolume"), Is.Zero);
            Assert.That(GetProperty<string>(musicPercent, "text"), Is.EqualTo("26%"));
            Assert.That(GetProperty<string>(sfxPercent, "text"), Is.EqualTo("0%"));
            Assert.That(PlayerPrefs.GetFloat("BallGame.Audio.MusicVolume"), Is.EqualTo(0.256f).Within(0.001f));
            Assert.That(PlayerPrefs.GetFloat("BallGame.Audio.SfxVolume"), Is.Zero);
        }

        [Test]
        public void SfxSlider_HasSinglePointerUpPreviewCallback()
        {
            InvokePublic(settingsController, "Show");
            EventTrigger trigger = menuInstance.transform
                .Find("SettingsModal/Panel/AudioPage/SfxSlider")
                .GetComponent<EventTrigger>();

            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.triggers.Count, Is.EqualTo(1));
            Assert.That(trigger.triggers[0].eventID, Is.EqualTo(EventTriggerType.PointerUp));
            Assert.DoesNotThrow(() => trigger.triggers[0].callback.Invoke(null));
        }

        private static Component CreateComponent(Type type, string objectName)
        {
            Assert.That(type, Is.Not.Null);
            return new GameObject(objectName).AddComponent(type);
        }

        private static object InvokePublic(Component target, string methodName, params object[] arguments)
        {
            return Invoke(target.GetType(), target, methodName, BindingFlags.Instance | BindingFlags.Public, arguments);
        }

        private static object Invoke(Type type, object target, string methodName, BindingFlags flags, params object[] arguments)
        {
            return type.GetMethod(methodName, flags).Invoke(target, arguments);
        }

        private static T GetProperty<T>(Component target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public).GetValue(target);
        }

        private static void DestroyExistingAudioManagers()
        {
            if (AudioManagerType == null)
            {
                return;
            }

            UnityEngine.Object[] managers = UnityEngine.Object.FindObjectsOfType(AudioManagerType);
            for (int i = 0; i < managers.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(((Component)managers[i]).gameObject);
            }

            AudioManagerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }

        private static void ClearAudioSettings()
        {
            PlayerPrefs.DeleteKey("BallGame.Audio.MasterVolume");
            PlayerPrefs.DeleteKey("BallGame.Audio.MusicVolume");
            PlayerPrefs.DeleteKey("BallGame.Audio.SfxVolume");
            PlayerPrefs.DeleteKey("BallGame.Audio.Muted");
            PlayerPrefs.Save();
        }
    }
}
