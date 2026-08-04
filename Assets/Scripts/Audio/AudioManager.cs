using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallGame.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const string MixerResourcePath = "Audio/BallGameAudioMixer";
        private const string MainMenuMusicResourcePath = "Audio/Music/MainMenuBGM";
        private const string GameMusicResourcePath = "Audio/Music/GameBGM";
        private const string ButtonClickResourcePath = "Audio/SFX/Click";
        private const string BallRollingResourcePath = "Audio/SFX/BallRolling";
        private const string BallHitResourcePath = "Audio/SFX/BallHit";
        private const string AntiGravityResourcePath = "Audio/SFX/Antigravity";
        private const string MainMenuSceneName = "MainMenu";
        private const string MasterVolumeParameter = "MasterVolume";
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const string MasterVolumeKey = "BallGame.Audio.MasterVolume";
        private const string MusicVolumeKey = "BallGame.Audio.MusicVolume";
        private const string SfxVolumeKey = "BallGame.Audio.SfxVolume";
        private const string MutedKey = "BallGame.Audio.Muted";
        private const float MinimumDecibels = -80f;
        private const float MusicFadeDuration = 0.5f;
        private const float RollingReportTimeout = 0.2f;
        private const float RollingFadeInDuration = 0.12f;
        private const float RollingFadeOutDuration = 0.18f;
        private const float MinimumRollingPitch = 0.85f;
        private const float MaximumRollingPitch = 1.2f;
        private const float AntiGravityReportTimeout = 0.2f;
        private const float AntiGravityFadeInDuration = 0.12f;
        private const float AntiGravityFadeOutDuration = 0.18f;
        private const float MinimumClickPitch = 0.95f;
        private const float MaximumClickPitch = 1.05f;
        private const float UnderwaterReportTimeout = 0.2f;
        private const float UnderwaterFadeInDuration = 0.2f;
        private const float UnderwaterFadeOutDuration = 0.25f;

        private struct RollingContribution
        {
            public Object source;
            public float normalizedSpeed;
            public float lastReportTime;
        }

        private struct TimedContribution
        {
            public Object source;
            public float lastReportTime;
        }

        private static AudioManager instance;

        private AudioMixer mixer;
        private AudioMixerSnapshot normalSnapshot;
        private AudioMixerSnapshot underwaterSnapshot;
        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioSource clickSource;
        private AudioSource rollingSource;
        private AudioSource antiGravitySource;
        private AudioListener fallbackListener;
        private AudioClip mainMenuMusic;
        private AudioClip gameMusic;
        private AudioClip buttonClickSfx;
        private AudioClip ballRollingSfx;
        private AudioClip ballHitSfx;
        private AudioClip antiGravitySfx;
        private Coroutine musicTransition;
        private float musicFadeVolume = 1f;
        private int knownSelectableCount = -1;
        private readonly HashSet<Button> clickBoundButtons = new HashSet<Button>();
        private readonly Dictionary<int, RollingContribution> rollingContributions = new Dictionary<int, RollingContribution>();
        private readonly List<int> staleRollingSources = new List<int>();
        private float targetRollingVolume;
        private float currentRollingVolume;
        private float targetRollingPitch = MinimumRollingPitch;
        private float currentRollingPitch = MinimumRollingPitch;
        private readonly Dictionary<int, TimedContribution> antiGravityContributions = new Dictionary<int, TimedContribution>();
        private readonly List<int> staleAntiGravitySources = new List<int>();
        private float targetAntiGravityVolume;
        private float currentAntiGravityVolume;
        private readonly Dictionary<int, TimedContribution> underwaterContributions =
            new Dictionary<int, TimedContribution>();
        private readonly List<int> staleUnderwaterSources = new List<int>();
        private bool underwaterActive;

        public static AudioManager Instance => instance;
        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public bool IsMuted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateGlobalInstance()
        {
            if (instance != null)
            {
                return;
            }

            AudioManager existing = FindObjectOfType<AudioManager>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject managerObject = new GameObject(nameof(AudioManager));
            managerObject.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }

                return;
            }

            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadSettings();
            ConfigureAudio();
            ConfigureFallbackListener();
            ApplyAudioSettings();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                RemoveButtonClickListeners();
                instance = null;
            }
        }

        private void Start()
        {
            ApplyAudioSettings();
            BindButtonClickSounds();
        }

        private void Update()
        {
            UpdateRollingAudio(Time.unscaledDeltaTime, Time.unscaledTime);
            UpdateAntiGravityAudio(Time.unscaledDeltaTime, Time.unscaledTime);
            UpdateUnderwaterState(Time.unscaledTime);
        }

        private void LateUpdate()
        {
            if (Selectable.allSelectableCount != knownSelectableCount)
            {
                BindButtonClickSounds();
            }
        }

        public void PlayMusic(AudioClip clip, bool restartIfSame = false)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (musicSource.clip == clip && !restartIfSame)
            {
                if (!musicSource.isPlaying)
                {
                    musicSource.Play();
                }

                if (musicFadeVolume < 1f)
                {
                    BeginMusicTransition(FadeMusicToFullVolume());
                }

                return;
            }

            BeginMusicTransition(TransitionMusic(clip));
        }

        public void StopMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            CancelMusicTransition();
            musicSource.Stop();
            musicSource.clip = null;
            SetMusicFadeVolume(1f);
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                return;
            }

            if (clip == buttonClickSfx)
            {
                PlayClickSfx(volumeScale);
                return;
            }

            if (sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlayBallHit(float volumeScale = 1f)
        {
            PlaySfx(ballHitSfx, Mathf.Clamp01(volumeScale));
        }

        public void ReportRolling(Object source, float normalizedSpeed)
        {
            if (source == null)
            {
                return;
            }

            float clampedSpeed = Mathf.Clamp01(normalizedSpeed);
            int sourceId = source.GetInstanceID();
            if (clampedSpeed <= 0f)
            {
                rollingContributions.Remove(sourceId);
                return;
            }

            rollingContributions[sourceId] = new RollingContribution
            {
                source = source,
                normalizedSpeed = clampedSpeed,
                lastReportTime = Time.unscaledTime
            };
        }

        public void StopRolling(Object source)
        {
            if (source != null)
            {
                rollingContributions.Remove(source.GetInstanceID());
            }
        }

        public void ReportAntiGravity(Object source)
        {
            if (source == null)
            {
                return;
            }

            antiGravityContributions[source.GetInstanceID()] = new TimedContribution
            {
                source = source,
                lastReportTime = Time.unscaledTime
            };
        }

        public void StopAntiGravity(Object source)
        {
            if (source != null)
            {
                antiGravityContributions.Remove(source.GetInstanceID());
            }
        }

        public void ReportUnderwater(Object source)
        {
            if (source == null)
            {
                return;
            }

            underwaterContributions[source.GetInstanceID()] = new TimedContribution
            {
                source = source,
                lastReportTime = Time.unscaledTime
            };
        }

        public void StopUnderwater(Object source)
        {
            if (source != null)
            {
                underwaterContributions.Remove(source.GetInstanceID());
            }
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            SaveFloat(MasterVolumeKey, MasterVolume);
            ApplyAudioSettings();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            SaveFloat(MusicVolumeKey, MusicVolume);
            ApplyAudioSettings();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            SaveFloat(SfxVolumeKey, SfxVolume);
            ApplyAudioSettings();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        private void LoadSettings()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
        }

        private void ConfigureAudio()
        {
            mixer = Resources.Load<AudioMixer>(MixerResourcePath);
            normalSnapshot = mixer != null ? mixer.FindSnapshot("Normal") : null;
            underwaterSnapshot = mixer != null ? mixer.FindSnapshot("Underwater") : null;
            mainMenuMusic = Resources.Load<AudioClip>(MainMenuMusicResourcePath);
            gameMusic = Resources.Load<AudioClip>(GameMusicResourcePath);
            buttonClickSfx = Resources.Load<AudioClip>(ButtonClickResourcePath);
            ballRollingSfx = Resources.Load<AudioClip>(BallRollingResourcePath);
            ballHitSfx = Resources.Load<AudioClip>(BallHitResourcePath);
            antiGravitySfx = Resources.Load<AudioClip>(AntiGravityResourcePath);
            musicSource = CreateSource("Music", true);
            sfxSource = CreateSource("SFX", false);
            clickSource = CreateSource("Click", false);
            rollingSource = CreateSource("Rolling", true);
            antiGravitySource = CreateSource("AntiGravity", true);
            rollingSource.clip = ballRollingSfx;
            rollingSource.volume = 0f;
            rollingSource.pitch = MinimumRollingPitch;
            antiGravitySource.clip = antiGravitySfx;
            antiGravitySource.volume = 0f;

            if (mixer == null)
            {
                return;
            }

            AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");
            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            musicSource.outputAudioMixerGroup = musicGroups.Length > 0 ? musicGroups[0] : null;
            sfxSource.outputAudioMixerGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
            clickSource.outputAudioMixerGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
            rollingSource.outputAudioMixerGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
            antiGravitySource.outputAudioMixerGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplyAudioSettings()
        {
            if (musicSource == null || sfxSource == null || clickSource == null)
            {
                return;
            }

            if (mixer != null)
            {
                musicSource.volume = musicFadeVolume;
                sfxSource.volume = 1f;
                clickSource.volume = 1f;
                rollingSource.volume = currentRollingVolume;
                antiGravitySource.volume = currentAntiGravityVolume;
                musicSource.mute = false;
                sfxSource.mute = false;
                clickSource.mute = false;
                rollingSource.mute = false;
                antiGravitySource.mute = false;
                mixer.SetFloat(MasterVolumeParameter, ToDecibels(IsMuted ? 0f : MasterVolume));
                mixer.SetFloat(MusicVolumeParameter, ToDecibels(MusicVolume));
                mixer.SetFloat(SfxVolumeParameter, ToDecibels(SfxVolume));
                return;
            }

            musicSource.volume = MasterVolume * MusicVolume * musicFadeVolume;
            sfxSource.volume = MasterVolume * SfxVolume;
            clickSource.volume = MasterVolume * SfxVolume;
            rollingSource.volume = MasterVolume * SfxVolume * currentRollingVolume;
            antiGravitySource.volume = MasterVolume * SfxVolume * currentAntiGravityVolume;
            musicSource.mute = IsMuted;
            sfxSource.mute = IsMuted;
            clickSource.mute = IsMuted;
            rollingSource.mute = IsMuted;
            antiGravitySource.mute = IsMuted;
        }

        private static float ToDecibels(float linearVolume)
        {
            return linearVolume <= 0.0001f
                ? MinimumDecibels
                : Mathf.Log10(Mathf.Clamp01(linearVolume)) * 20f;
        }

        private static void SaveFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            rollingContributions.Clear();
            antiGravityContributions.Clear();
            underwaterContributions.Clear();
            RestoreNormalSnapshotImmediately();
            RefreshFallbackListener();
            PlayMusic(GetMusicForScene(scene.name));
            BindButtonClickSounds();
        }

        private void BindButtonClickSounds()
        {
            clickBoundButtons.RemoveWhere(button => button == null);
            Selectable[] selectables = Selectable.allSelectablesArray;
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] is Button button && clickBoundButtons.Add(button))
                {
                    button.onClick.AddListener(PlayButtonClickSound);
                }
            }

            knownSelectableCount = Selectable.allSelectableCount;
        }

        private void PlayButtonClickSound()
        {
            PlaySfx(buttonClickSfx);
        }

        private void PlayClickSfx(float volumeScale)
        {
            if (buttonClickSfx == null || clickSource == null)
            {
                return;
            }

            clickSource.pitch = Random.Range(MinimumClickPitch, MaximumClickPitch);
            clickSource.PlayOneShot(buttonClickSfx, Mathf.Clamp01(volumeScale));
        }

        private void UpdateUnderwaterState(float now)
        {
            staleUnderwaterSources.Clear();
            foreach (KeyValuePair<int, TimedContribution> pair in underwaterContributions)
            {
                TimedContribution contribution = pair.Value;
                if (contribution.source == null || now - contribution.lastReportTime > UnderwaterReportTimeout)
                {
                    staleUnderwaterSources.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleUnderwaterSources.Count; i++)
            {
                underwaterContributions.Remove(staleUnderwaterSources[i]);
            }

            SetUnderwaterActive(underwaterContributions.Count > 0);
        }

        private void SetUnderwaterActive(bool active)
        {
            if (underwaterActive == active)
            {
                return;
            }

            underwaterActive = active;
            AudioMixerSnapshot target = active ? underwaterSnapshot : normalSnapshot;
            if (target != null)
            {
                target.TransitionTo(active ? UnderwaterFadeInDuration : UnderwaterFadeOutDuration);
            }
        }

        private void RestoreNormalSnapshotImmediately()
        {
            underwaterActive = false;
            if (normalSnapshot != null)
            {
                normalSnapshot.TransitionTo(0f);
            }
        }

        private void RemoveButtonClickListeners()
        {
            foreach (Button button in clickBoundButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(PlayButtonClickSound);
                }
            }

            clickBoundButtons.Clear();
            knownSelectableCount = -1;
        }

        private void ConfigureFallbackListener()
        {
            fallbackListener = gameObject.AddComponent<AudioListener>();
            fallbackListener.enabled = false;
        }

        private void RefreshFallbackListener()
        {
            if (fallbackListener == null)
            {
                return;
            }

            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            bool hasSceneListener = false;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener != null && listener != fallbackListener && listener.isActiveAndEnabled)
                {
                    hasSceneListener = true;
                    break;
                }
            }

            fallbackListener.enabled = !hasSceneListener;
        }

        private AudioClip GetMusicForScene(string sceneName)
        {
            return sceneName == MainMenuSceneName ? mainMenuMusic : gameMusic;
        }

        private void BeginMusicTransition(IEnumerator transition)
        {
            CancelMusicTransition();
            musicTransition = StartCoroutine(transition);
        }

        private void CancelMusicTransition()
        {
            if (musicTransition == null)
            {
                return;
            }

            StopCoroutine(musicTransition);
            musicTransition = null;
        }

        private IEnumerator TransitionMusic(AudioClip nextClip)
        {
            if (musicSource.isPlaying && musicSource.clip != null)
            {
                yield return FadeMusic(musicFadeVolume, 0f, MusicFadeDuration);
            }

            musicSource.Stop();
            musicSource.clip = nextClip;
            SetMusicFadeVolume(0f);
            musicSource.Play();
            yield return FadeMusic(0f, 1f, MusicFadeDuration);
            musicTransition = null;
        }

        private IEnumerator FadeMusicToFullVolume()
        {
            yield return FadeMusic(musicFadeVolume, 1f, MusicFadeDuration);
            musicTransition = null;
        }

        private IEnumerator FadeMusic(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetMusicFadeVolume(to);
                yield break;
            }

            float elapsed = 0f;
            SetMusicFadeVolume(from);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetMusicFadeVolume(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetMusicFadeVolume(to);
        }

        private void SetMusicFadeVolume(float value)
        {
            musicFadeVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
        }

        private void UpdateRollingAudio(float deltaTime, float now)
        {
            RefreshRollingTargets(now);

            float fadeDuration = targetRollingVolume > currentRollingVolume
                ? RollingFadeInDuration
                : RollingFadeOutDuration;
            float volumeStep = fadeDuration > 0f ? deltaTime / fadeDuration : 1f;
            currentRollingVolume = Mathf.MoveTowards(currentRollingVolume, targetRollingVolume, volumeStep);
            currentRollingPitch = Mathf.MoveTowards(currentRollingPitch, targetRollingPitch, deltaTime * 2.5f);

            if (rollingSource == null)
            {
                return;
            }

            rollingSource.pitch = currentRollingPitch;
            ApplyRollingSourceVolume();
            if (targetRollingVolume > 0f && ballRollingSfx != null && !rollingSource.isPlaying)
            {
                rollingSource.Play();
            }
            else if (targetRollingVolume <= 0f && currentRollingVolume <= 0.0001f && rollingSource.isPlaying)
            {
                rollingSource.Stop();
            }
        }

        private void RefreshRollingTargets(float now)
        {
            staleRollingSources.Clear();
            float volumeSum = 0f;
            float weightedSpeedSum = 0f;
            foreach (KeyValuePair<int, RollingContribution> pair in rollingContributions)
            {
                RollingContribution contribution = pair.Value;
                if (contribution.source == null || now - contribution.lastReportTime > RollingReportTimeout)
                {
                    staleRollingSources.Add(pair.Key);
                    continue;
                }

                volumeSum += contribution.normalizedSpeed;
                weightedSpeedSum += contribution.normalizedSpeed * contribution.normalizedSpeed;
            }

            for (int i = 0; i < staleRollingSources.Count; i++)
            {
                rollingContributions.Remove(staleRollingSources[i]);
            }

            targetRollingVolume = Mathf.Clamp01(volumeSum);
            float weightedSpeed = volumeSum > 0f ? weightedSpeedSum / volumeSum : 0f;
            targetRollingPitch = Mathf.Lerp(MinimumRollingPitch, MaximumRollingPitch, Mathf.Clamp01(weightedSpeed));
        }

        private void ApplyRollingSourceVolume()
        {
            if (mixer != null)
            {
                rollingSource.volume = currentRollingVolume;
                rollingSource.mute = false;
                return;
            }

            rollingSource.volume = MasterVolume * SfxVolume * currentRollingVolume;
            rollingSource.mute = IsMuted;
        }

        private void UpdateAntiGravityAudio(float deltaTime, float now)
        {
            RefreshAntiGravityTargets(now);
            float fadeDuration = targetAntiGravityVolume > currentAntiGravityVolume
                ? AntiGravityFadeInDuration
                : AntiGravityFadeOutDuration;
            float volumeStep = fadeDuration > 0f ? deltaTime / fadeDuration : 1f;
            currentAntiGravityVolume = Mathf.MoveTowards(
                currentAntiGravityVolume,
                targetAntiGravityVolume,
                volumeStep);

            if (antiGravitySource == null)
            {
                return;
            }

            ApplyAntiGravitySourceVolume();
            if (targetAntiGravityVolume > 0f && antiGravitySfx != null && !antiGravitySource.isPlaying)
            {
                antiGravitySource.Play();
            }
            else if (targetAntiGravityVolume <= 0f
                && currentAntiGravityVolume <= 0.0001f
                && antiGravitySource.isPlaying)
            {
                antiGravitySource.Stop();
            }
        }

        private void RefreshAntiGravityTargets(float now)
        {
            staleAntiGravitySources.Clear();
            foreach (KeyValuePair<int, TimedContribution> pair in antiGravityContributions)
            {
                TimedContribution contribution = pair.Value;
                if (contribution.source == null || now - contribution.lastReportTime > AntiGravityReportTimeout)
                {
                    staleAntiGravitySources.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleAntiGravitySources.Count; i++)
            {
                antiGravityContributions.Remove(staleAntiGravitySources[i]);
            }

            targetAntiGravityVolume = antiGravityContributions.Count > 0 ? 1f : 0f;
        }

        private void ApplyAntiGravitySourceVolume()
        {
            if (mixer != null)
            {
                antiGravitySource.volume = currentAntiGravityVolume;
                antiGravitySource.mute = false;
                return;
            }

            antiGravitySource.volume = MasterVolume * SfxVolume * currentAntiGravityVolume;
            antiGravitySource.mute = IsMuted;
        }
    }
}
