using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace BallGame.Tests
{
    public sealed class AudioManagerTests
    {
        private const string MasterVolumeKey = "BallGame.Audio.MasterVolume";
        private const string MusicVolumeKey = "BallGame.Audio.MusicVolume";
        private const string SfxVolumeKey = "BallGame.Audio.SfxVolume";
        private const string MutedKey = "BallGame.Audio.Muted";

        private static Type ManagerType => Type.GetType("BallGame.Audio.AudioManager, Assembly-CSharp");

        [SetUp]
        public void SetUp()
        {
            ClearSettings();
            DestroyExistingManagers();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyExistingManagers();
            ClearSettings();
        }

        [Test]
        public void NewManager_UsesDefaultSettingsAndConfiguredMixerGroups()
        {
            Component manager = CreateManager();

            Assert.That(GetProperty<float>(manager, "MasterVolume"), Is.EqualTo(1f));
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(1f));
            Assert.That(GetProperty<float>(manager, "SfxVolume"), Is.EqualTo(1f));
            Assert.That(GetProperty<bool>(manager, "IsMuted"), Is.False);

            AudioSource music = manager.transform.Find("Music").GetComponent<AudioSource>();
            AudioSource sfx = manager.transform.Find("SFX").GetComponent<AudioSource>();
            AudioSource clickSource = manager.transform.Find("Click").GetComponent<AudioSource>();
            AudioSource rolling = manager.transform.Find("Rolling").GetComponent<AudioSource>();
            AudioSource antiGravity = manager.transform.Find("AntiGravity").GetComponent<AudioSource>();
            AudioClip ballHit = Resources.Load<AudioClip>("Audio/SFX/BallHit");
            Assert.That(music.loop, Is.True);
            Assert.That(sfx.loop, Is.False);
            Assert.That(clickSource.loop, Is.False);
            Assert.That(clickSource.playOnAwake, Is.False);
            Assert.That(clickSource.spatialBlend, Is.Zero);
            Assert.That(rolling.loop, Is.True);
            Assert.That(rolling.playOnAwake, Is.False);
            Assert.That(antiGravity.loop, Is.True);
            Assert.That(antiGravity.playOnAwake, Is.False);
            Assert.That(rolling.clip, Is.SameAs(Resources.Load<AudioClip>("Audio/SFX/BallRolling")));
            Assert.That(antiGravity.clip, Is.SameAs(Resources.Load<AudioClip>("Audio/SFX/Antigravity")));
            Assert.That(ballHit, Is.Not.Null);
            Assert.That(GetPrivateField<AudioClip>(manager, "ballHitSfx"), Is.SameAs(ballHit));
            Assert.That(music.outputAudioMixerGroup, Is.Not.Null);
            Assert.That(sfx.outputAudioMixerGroup, Is.Not.Null);
            Assert.That(clickSource.outputAudioMixerGroup, Is.Not.Null);
            Assert.That(rolling.outputAudioMixerGroup, Is.Not.Null);
            Assert.That(antiGravity.outputAudioMixerGroup, Is.Not.Null);
            Assert.That(music.outputAudioMixerGroup.name, Is.EqualTo("Music"));
            Assert.That(sfx.outputAudioMixerGroup.name, Is.EqualTo("SFX"));
            Assert.That(clickSource.outputAudioMixerGroup.name, Is.EqualTo("SFX"));
            Assert.That(rolling.outputAudioMixerGroup.name, Is.EqualTo("SFX"));
            Assert.That(antiGravity.outputAudioMixerGroup.name, Is.EqualTo("SFX"));
        }

        [Test]
        public void VolumeAndMuteChanges_AreClampedAndPersisted()
        {
            Component manager = CreateManager();
            Invoke(manager, "SetMasterVolume", 2f);
            Invoke(manager, "SetMusicVolume", -1f);
            Invoke(manager, "SetSfxVolume", 0.35f);
            Invoke(manager, "SetMuted", true);

            Assert.That(GetProperty<float>(manager, "MasterVolume"), Is.EqualTo(1f));
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(0f));
            Assert.That(GetProperty<float>(manager, "SfxVolume"), Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(GetProperty<bool>(manager, "IsMuted"), Is.True);
            Assert.That(PlayerPrefs.GetFloat(MasterVolumeKey), Is.EqualTo(1f));
            Assert.That(PlayerPrefs.GetFloat(MusicVolumeKey), Is.EqualTo(0f));
            Assert.That(PlayerPrefs.GetFloat(SfxVolumeKey), Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(PlayerPrefs.GetInt(MutedKey), Is.EqualTo(1));

            UnityEngine.Object.DestroyImmediate(manager.gameObject);
            manager = CreateManager();
            Assert.That(GetProperty<float>(manager, "MasterVolume"), Is.EqualTo(1f));
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(0f));
            Assert.That(GetProperty<float>(manager, "SfxVolume"), Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(GetProperty<bool>(manager, "IsMuted"), Is.True);
        }

        [Test]
        public void DuplicateManager_IsRemovedAndOriginalRemainsSingleton()
        {
            Component original = CreateManager();
            GameObject duplicateObject = new GameObject("DuplicateAudioManager");
            Component duplicate = duplicateObject.AddComponent(ManagerType);
            InvokeNonPublic(duplicate, "Awake");

            object currentInstance = ManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
                .GetValue(null);
            Assert.That(currentInstance, Is.SameAs(original));
            Assert.That(duplicateObject == null, Is.True);
            Assert.That(UnityEngine.Object.FindObjectsOfType(ManagerType).Length, Is.EqualTo(1));
        }

        [Test]
        public void PlaybackMethods_HandleNullAndRepeatedMusicSafely()
        {
            Component manager = CreateManager();
            Assert.DoesNotThrow(() => Invoke(manager, "PlayMusic", null, false));
            Assert.DoesNotThrow(() => Invoke(manager, "PlaySfx", null, 1f));
            Assert.DoesNotThrow(() => Invoke(manager, "PlayBallHit", -1f));
            Assert.DoesNotThrow(() => Invoke(manager, "PlayBallHit", 2f));
            ManagerType.GetField("ballHitSfx", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, null);
            Assert.DoesNotThrow(() => Invoke(manager, "PlayBallHit", 1f));
            Assert.DoesNotThrow(() => Invoke(manager, "StopMusic"));

            AudioClip clip = AudioClip.Create("TestMusic", 128, 1, 44100, false);
            try
            {
                Invoke(manager, "PlayMusic", clip, false);
                AudioSource music = manager.transform.Find("Music").GetComponent<AudioSource>();
                Assert.That(music.clip, Is.SameAs(clip));
                Assert.DoesNotThrow(() => Invoke(manager, "PlayMusic", clip, false));
                Assert.DoesNotThrow(() => Invoke(manager, "PlayMusic", clip, true));
                Invoke(manager, "StopMusic");
                Assert.That(music.clip, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MixerResource_ExposesExpectedParameters()
        {
            const string mixerPath = "Assets/Resources/Audio/BallGameAudioMixer.mixer";
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                mixerPath);
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups("Master").Length, Is.GreaterThan(0));
            Assert.That(mixer.FindMatchingGroups("Music").Length, Is.GreaterThan(0));
            Assert.That(mixer.FindMatchingGroups("SFX").Length, Is.GreaterThan(0));

            SerializedProperty exposed = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            Assert.That(HasExposedParameter(exposed, "MasterVolume"), Is.True);
            Assert.That(HasExposedParameter(exposed, "MusicVolume"), Is.True);
            Assert.That(HasExposedParameter(exposed, "SfxVolume"), Is.True);

            Assert.That(mixer.FindSnapshot("Normal"), Is.Not.Null);
            Assert.That(mixer.FindSnapshot("Underwater"), Is.Not.Null);
            string mixerYaml = File.ReadAllText(mixerPath);
            StringAssert.Contains("m_EffectName: Lowpass", mixerYaml);
            StringAssert.Contains(": 22000", mixerYaml);
            StringAssert.Contains(": 1200", mixerYaml);
        }

        [Test]
        public void SceneMusicSelection_UsesMenuAndGameResources()
        {
            AudioClip mainMenuMusic = Resources.Load<AudioClip>("Audio/Music/MainMenuBGM");
            AudioClip gameMusic = Resources.Load<AudioClip>("Audio/Music/GameBGM");
            Assert.That(mainMenuMusic, Is.Not.Null);
            Assert.That(gameMusic, Is.Not.Null);

            Component manager = CreateManager();
            MethodInfo selectMusic = ManagerType.GetMethod(
                "GetMusicForScene",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(selectMusic.Invoke(manager, new object[] { "MainMenu" }), Is.SameAs(mainMenuMusic));
            Assert.That(selectMusic.Invoke(manager, new object[] { "Level_01" }), Is.SameAs(gameMusic));
            Assert.That(selectMusic.Invoke(manager, new object[] { "Challenge_01" }), Is.SameAs(gameMusic));
        }

        [Test]
        public void FallbackListener_IsEnabledOnlyWhenSceneHasNoListener()
        {
            Component manager = CreateManager();
            AudioListener fallback = manager.GetComponent<AudioListener>();
            Assert.That(fallback, Is.Not.Null);

            AudioListener[] existingListeners = UnityEngine.Object.FindObjectsOfType<AudioListener>();
            for (int i = 0; i < existingListeners.Length; i++)
            {
                if (existingListeners[i] != fallback)
                {
                    existingListeners[i].enabled = false;
                }
            }

            GameObject sceneListenerObject = null;
            try
            {
                InvokeNonPublic(manager, "RefreshFallbackListener");
                Assert.That(fallback.enabled, Is.True);

                sceneListenerObject = new GameObject("SceneAudioListener");
                AudioListener sceneListener = sceneListenerObject.AddComponent<AudioListener>();
                InvokeNonPublic(manager, "RefreshFallbackListener");
                Assert.That(sceneListener.enabled, Is.True);
                Assert.That(fallback.enabled, Is.False);
            }
            finally
            {
                if (sceneListenerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(sceneListenerObject);
                }

                for (int i = 0; i < existingListeners.Length; i++)
                {
                    if (existingListeners[i] != null && existingListeners[i] != fallback)
                    {
                        existingListeners[i].enabled = true;
                    }
                }
            }
        }

        [Test]
        public void ButtonClickSound_BindsExistingAndDynamicallyCreatedButtonsOnce()
        {
            AudioClip click = Resources.Load<AudioClip>("Audio/SFX/Click");
            Assert.That(click, Is.Not.Null);

            Component manager = CreateManager();
            GameObject buttonObject = new GameObject("RuntimeButton");
            Button button = buttonObject.AddComponent<Button>();
            try
            {
                InvokeNonPublic(manager, "BindButtonClickSounds");
                Assert.That(IsButtonBound(manager, button), Is.True);
                Assert.DoesNotThrow(button.onClick.Invoke);
                AudioSource clickSource = manager.transform.Find("Click").GetComponent<AudioSource>();
                Assert.That(clickSource.pitch, Is.InRange(0.95f, 1.05f));

                InvokeNonPublic(manager, "BindButtonClickSounds");
                Assert.That(IsButtonBound(manager, button), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void ClickPlayback_RandomizesOnlyClickPitchWithinExpectedRange()
        {
            AudioClip click = Resources.Load<AudioClip>("Audio/SFX/Click");
            AudioClip other = AudioClip.Create("OtherSfx", 128, 1, 44100, false);
            Component manager = CreateManager();
            AudioSource clickSource = manager.transform.Find("Click").GetComponent<AudioSource>();
            AudioSource sfxSource = manager.transform.Find("SFX").GetComponent<AudioSource>();
            try
            {
                float firstPitch = 0f;
                bool observedVariation = false;
                for (int i = 0; i < 32; i++)
                {
                    Invoke(manager, "PlaySfx", click, 1f);
                    Assert.That(clickSource.pitch, Is.InRange(0.95f, 1.05f));
                    if (i == 0)
                    {
                        firstPitch = clickSource.pitch;
                    }
                    else if (!Mathf.Approximately(clickSource.pitch, firstPitch))
                    {
                        observedVariation = true;
                    }
                }

                Assert.That(observedVariation, Is.True);

                sfxSource.pitch = 1f;
                Invoke(manager, "PlaySfx", other, 1f);
                Assert.That(sfxSource.pitch, Is.EqualTo(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void ClickSource_UsesFallbackSfxVolumeAndMuteWithoutMixer()
        {
            Component manager = CreateManager();
            AudioSource clickSource = manager.transform.Find("Click").GetComponent<AudioSource>();
            ManagerType.GetField("mixer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, null);

            Invoke(manager, "SetMasterVolume", 0.5f);
            Invoke(manager, "SetSfxVolume", 0.4f);
            Assert.That(clickSource.volume, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(clickSource.mute, Is.False);

            Invoke(manager, "SetMuted", true);
            Assert.That(clickSource.mute, Is.True);
        }

        [Test]
        public void UnderwaterReports_AggregateSourcesAndOnlyDeactivateAfterLastSourceStops()
        {
            Component manager = CreateManager();
            GameObject sourceA = new GameObject("WaterZoneA");
            GameObject sourceB = new GameObject("WaterZoneB");
            try
            {
                Invoke(manager, "ReportUnderwater", sourceA);
                Invoke(manager, "ReportUnderwater", sourceA);
                Invoke(manager, "ReportUnderwater", sourceB);
                InvokeNonPublic(manager, "UpdateUnderwaterState", Time.unscaledTime);
                Assert.That(GetCollectionCount(manager, "underwaterContributions"), Is.EqualTo(2));
                Assert.That(GetPrivateField<bool>(manager, "underwaterActive"), Is.True);

                Invoke(manager, "StopUnderwater", sourceA);
                InvokeNonPublic(manager, "UpdateUnderwaterState", Time.unscaledTime);
                Assert.That(GetPrivateField<bool>(manager, "underwaterActive"), Is.True);

                Invoke(manager, "StopUnderwater", sourceB);
                InvokeNonPublic(manager, "UpdateUnderwaterState", Time.unscaledTime);
                Assert.That(GetPrivateField<bool>(manager, "underwaterActive"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceA);
                UnityEngine.Object.DestroyImmediate(sourceB);
            }
        }

        [Test]
        public void UnderwaterReports_RemoveTimedOutAndDestroyedSources()
        {
            Component manager = CreateManager();
            GameObject timedOutSource = new GameObject("TimedOutWaterZone");
            GameObject destroyedSource = new GameObject("DestroyedWaterZone");
            Invoke(manager, "ReportUnderwater", timedOutSource);
            Invoke(manager, "ReportUnderwater", destroyedSource);
            UnityEngine.Object.DestroyImmediate(destroyedSource);

            InvokeNonPublic(manager, "UpdateUnderwaterState", Time.unscaledTime + 0.21f);
            Assert.That(GetCollectionCount(manager, "underwaterContributions"), Is.Zero);
            Assert.That(GetPrivateField<bool>(manager, "underwaterActive"), Is.False);
            UnityEngine.Object.DestroyImmediate(timedOutSource);
        }

        [Test]
        public void RollingReports_AggregateVolumeAndWeightedPitchWithoutDuplicatingSources()
        {
            Component manager = CreateManager();
            GameObject sourceA = new GameObject("RollingSourceA");
            GameObject sourceB = new GameObject("RollingSourceB");
            GameObject sourceC = new GameObject("RollingSourceC");
            try
            {
                Invoke(manager, "ReportRolling", sourceA, 0.3f);
                Invoke(manager, "ReportRolling", sourceB, 0.4f);
                InvokeNonPublic(manager, "RefreshRollingTargets", Time.unscaledTime);

                Assert.That(GetPrivateField<float>(manager, "targetRollingVolume"), Is.EqualTo(0.7f).Within(0.001f));
                AssertRollingPitch(manager, (0.3f * 0.3f + 0.4f * 0.4f) / 0.7f);
                Assert.That(GetCollectionCount(manager, "rollingContributions"), Is.EqualTo(2));

                Invoke(manager, "ReportRolling", sourceA, 0.5f);
                InvokeNonPublic(manager, "RefreshRollingTargets", Time.unscaledTime);
                Assert.That(GetPrivateField<float>(manager, "targetRollingVolume"), Is.EqualTo(0.9f).Within(0.001f));
                AssertRollingPitch(manager, (0.5f * 0.5f + 0.4f * 0.4f) / 0.9f);
                Assert.That(GetCollectionCount(manager, "rollingContributions"), Is.EqualTo(2));

                Invoke(manager, "ReportRolling", sourceC, 0.8f);
                InvokeNonPublic(manager, "RefreshRollingTargets", Time.unscaledTime);
                Assert.That(GetPrivateField<float>(manager, "targetRollingVolume"), Is.EqualTo(1f));

                Invoke(manager, "StopRolling", sourceA);
                InvokeNonPublic(manager, "RefreshRollingTargets", Time.unscaledTime);
                Assert.That(GetCollectionCount(manager, "rollingContributions"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceA);
                UnityEngine.Object.DestroyImmediate(sourceB);
                UnityEngine.Object.DestroyImmediate(sourceC);
            }
        }

        [Test]
        public void RollingReports_ClampInputAndRemoveStaleContributions()
        {
            Component manager = CreateManager();
            GameObject source = new GameObject("RollingSource");
            try
            {
                Invoke(manager, "ReportRolling", source, 2f);
                InvokeNonPublic(manager, "RefreshRollingTargets", Time.unscaledTime);
                Assert.That(GetPrivateField<float>(manager, "targetRollingVolume"), Is.EqualTo(1f));

                InvokeNonPublic(manager, "RefreshRollingTargets", Time.unscaledTime + 0.21f);
                Assert.That(GetPrivateField<float>(manager, "targetRollingVolume"), Is.EqualTo(0f));
                Assert.That(GetCollectionCount(manager, "rollingContributions"), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static Component CreateManager()
        {
            Assert.That(ManagerType, Is.Not.Null);
            GameObject managerObject = new GameObject("AudioManagerTest");
            Component manager = managerObject.AddComponent(ManagerType);
            InvokeNonPublic(manager, "Awake");
            return manager;
        }

        private static T GetProperty<T>(Component manager, string propertyName)
        {
            return (T)ManagerType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                .GetValue(manager);
        }

        private static object Invoke(Component manager, string methodName, params object[] arguments)
        {
            return ManagerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                .Invoke(manager, arguments);
        }

        private static object InvokeNonPublic(Component manager, string methodName, params object[] arguments)
        {
            return ManagerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, arguments);
        }

        private static T GetPrivateField<T>(Component manager, string fieldName)
        {
            return (T)ManagerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(manager);
        }

        private static int GetCollectionCount(Component manager, string fieldName)
        {
            return ((ICollection)GetPrivateField<object>(manager, fieldName)).Count;
        }

        private static void AssertRollingPitch(Component manager, float weightedSpeed)
        {
            float expectedPitch = Mathf.Lerp(0.85f, 1.2f, weightedSpeed);
            Assert.That(GetPrivateField<float>(manager, "targetRollingPitch"),
                Is.EqualTo(expectedPitch).Within(0.001f));
        }

        private static bool HasExposedParameter(SerializedProperty exposed, string expectedName)
        {
            for (int i = 0; i < exposed.arraySize; i++)
            {
                if (exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == expectedName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsButtonBound(Component manager, Button expectedButton)
        {
            IEnumerable buttons = (IEnumerable)ManagerType.GetField(
                "clickBoundButtons",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            foreach (object button in buttons)
            {
                if (ReferenceEquals(button, expectedButton))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DestroyExistingManagers()
        {
            if (ManagerType == null)
            {
                return;
            }

            UnityEngine.Object[] managers = UnityEngine.Object.FindObjectsOfType(ManagerType);
            for (int i = 0; i < managers.Length; i++)
            {
                Component manager = (Component)managers[i];
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }

            ManagerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        }

        private static void ClearSettings()
        {
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(MusicVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.DeleteKey(MutedKey);
            PlayerPrefs.Save();
        }
    }
}
