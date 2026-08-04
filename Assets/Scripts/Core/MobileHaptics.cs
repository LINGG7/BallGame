using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BallGame.Core
{
    public enum HapticIntensityLevel
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    /// <summary>
    /// Aggregates rolling feedback from every active ball and maintains one
    /// continuous haptic effect using the strongest reported rolling speed.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class MobileHaptics : MonoBehaviour
    {
        internal const string IntensityLevelPlayerPrefsKey = "BallGame.Haptics.IntensityLevel";
        private const float ResponseCurveExponent = 1.6f;
        private const int AndroidStrengthSteps = 24;

        [Header("Continuous rolling")]
        [SerializeField, Min(0.02f)] private float rollingReportTimeout = 0.25f;
        [SerializeField, Min(0.01f)] private float intensityRiseTime = 0.15f;
        [SerializeField, Min(0.01f)] private float intensityFallTime = 0.20f;
        [SerializeField, Min(0.02f)] private float platformUpdateInterval = 0.12f;
        [SerializeField, Range(0.001f, 0.25f)] private float minimumUpdateDifference = 0.02f;

        private static MobileHaptics instance;
        private static bool enabledState = true;
        private static bool isQuitting;
        private static bool intensityLevelLoaded;
        private static HapticIntensityLevel intensityLevel = HapticIntensityLevel.Medium;

        private float strongestRollingReport;
        private float targetRollingSpeed;
        private float currentRollingSpeed;
        private float lastRollingReportTime = float.NegativeInfinity;
        private float lastSentOutputStrength = -1f;
        private float nextPlatformUpdateTime;
        private bool isContinuousPlaying;

        /// <summary>Globally enables or disables mobile rolling feedback.</summary>
        public static bool Enabled
        {
            get => enabledState;
            set
            {
                if (enabledState == value)
                {
                    return;
                }

                enabledState = value;
                if (!value)
                {
                    Stop();
                }
            }
        }

        /// <summary>The player's saved rolling vibration strength.</summary>
        public static HapticIntensityLevel IntensityLevel
        {
            get
            {
                EnsureIntensityLevelLoaded();
                return intensityLevel;
            }
        }

        /// <summary>Changes and immediately saves the player's vibration strength.</summary>
        public static void SetIntensityLevel(HapticIntensityLevel level)
        {
            HapticIntensityLevel sanitizedLevel = SanitizeIntensityLevel((int)level);
            intensityLevel = sanitizedLevel;
            intensityLevelLoaded = true;
            PlayerPrefs.SetInt(IntensityLevelPlayerPrefsKey, (int)sanitizedLevel);
            PlayerPrefs.Save();

            if (sanitizedLevel == HapticIntensityLevel.Off)
            {
                Stop();
            }
            else if (instance != null)
            {
                // Force the active continuous effect to adopt the new range next frame.
                instance.lastSentOutputStrength = -1f;
            }
        }

        /// <summary>
        /// Reports rolling speed in the 0..1 range. Multiple reports in a frame
        /// are merged and only the strongest controls the continuous effect.
        /// </summary>
        public static void ReportRolling(float normalizedSpeed)
        {
            if (!enabledState || IntensityLevel == HapticIntensityLevel.Off || !Application.isPlaying)
            {
                return;
            }

            float clampedSpeed = Mathf.Clamp01(normalizedSpeed);
            if (clampedSpeed <= 0f)
            {
                return;
            }

            MobileHaptics service = EnsureInstance();
            if (service != null)
            {
                service.strongestRollingReport = MergeRollingReport(
                    service.strongestRollingReport,
                    clampedSpeed);
                service.lastRollingReportTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>Stops active vibration and clears pending rolling feedback.</summary>
        public static void Stop()
        {
            if (instance != null)
            {
                instance.StopContinuousFeedback(true);
            }
            else
            {
                PlatformHaptics.StopContinuous(true);
            }
        }

        internal static float NormalizeRollingSpeed(float speed, float minimumSpeed, float maximumSpeed)
        {
            if (speed <= minimumSpeed || maximumSpeed <= minimumSpeed)
            {
                return 0f;
            }

            return Mathf.InverseLerp(minimumSpeed, maximumSpeed, speed);
        }

        internal static float CalculateIntensity(float normalizedSpeed, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, ApplyResponseCurve(normalizedSpeed));
        }

        internal static float CalculateProfileIntensity(float normalizedSpeed, HapticIntensityLevel level)
        {
            GetIosIntensityRange(level, out float minimum, out float maximum);
            return CalculateIntensity(normalizedSpeed, minimum, maximum);
        }

        internal static float MergeRollingReport(float currentStrongest, float newReport)
        {
            return Mathf.Max(Mathf.Clamp01(currentStrongest), Mathf.Clamp01(newReport));
        }

        internal static int CalculateAndroidAmplitude(float normalizedSpeed, int minimum, int maximum)
        {
            return Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, ApplyResponseCurve(normalizedSpeed)));
        }

        internal static int CalculateProfileAndroidAmplitude(float normalizedSpeed, HapticIntensityLevel level)
        {
            GetAndroidAmplitudeRange(level, out int minimum, out int maximum);
            float quantizedStrength = QuantizeStrength(
                ApplyAndroidAmplitudeCurve(normalizedSpeed),
                AndroidStrengthSteps);
            return Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, quantizedStrength));
        }

        internal static float ApplyResponseCurve(float normalizedSpeed)
        {
            return Mathf.Pow(Mathf.Clamp01(normalizedSpeed), ResponseCurveExponent);
        }

        /// <summary>
        /// Compensates for the weak and highly compressed low-amplitude range on
        /// typical Android vibration motors while retaining the base speed curve.
        /// </summary>
        internal static float ApplyAndroidAmplitudeCurve(float normalizedSpeed)
        {
            return Mathf.Sqrt(ApplyResponseCurve(normalizedSpeed));
        }

        internal static float QuantizeStrength(float normalizedStrength, int steps)
        {
            float clamped = Mathf.Clamp01(normalizedStrength);
            if (steps <= 1)
            {
                return clamped;
            }

            return Mathf.Round(clamped * (steps - 1)) / (steps - 1);
        }

        internal static float CalculateSharpness(float normalizedSpeed)
        {
            return Mathf.Lerp(0.05f, 0.30f, ApplyResponseCurve(normalizedSpeed));
        }

        internal static int[] BuildAndroidTextureAmplitudes(int amplitude)
        {
            int target = Mathf.Clamp(amplitude, 0, 255);
            if (target == 0)
            {
                return new[] { 0, 0, 0, 0, 0 };
            }

            return new[]
            {
                0,
                Mathf.Max(1, Mathf.RoundToInt(target * 0.78f)),
                0,
                target,
                0
            };
        }

        internal static long[] BuildAndroidTextureTimings(float normalizedStrength)
        {
            float strength = Mathf.Clamp01(normalizedStrength);
            long onDuration = Mathf.RoundToInt(Mathf.Lerp(12f, 18f, strength));
            long offDuration = Mathf.RoundToInt(Mathf.Lerp(8f, 2f, strength));
            return new[] { 0L, onDuration, offDuration, onDuration, offDuration };
        }

        internal static long[] GetAndroidFallbackPattern(HapticIntensityLevel level)
        {
            switch (level)
            {
                case HapticIntensityLevel.Low:
                    return new[] { 0L, 8L, 38L };
                case HapticIntensityLevel.High:
                    return new[] { 0L, 12L, 18L };
                case HapticIntensityLevel.Medium:
                    return new[] { 0L, 10L, 26L };
                default:
                    return new[] { 0L, 0L, 1000L };
            }
        }

        internal static HapticIntensityLevel SanitizeIntensityLevel(int rawLevel)
        {
            return rawLevel >= (int)HapticIntensityLevel.Off && rawLevel <= (int)HapticIntensityLevel.High
                ? (HapticIntensityLevel)rawLevel
                : HapticIntensityLevel.Medium;
        }

        internal static void ReloadIntensityLevelFromPreferences()
        {
            intensityLevel = SanitizeIntensityLevel(PlayerPrefs.GetInt(
                IntensityLevelPlayerPrefsKey,
                (int)HapticIntensityLevel.Medium));
            intensityLevelLoaded = true;
        }

        private static void GetIosIntensityRange(
            HapticIntensityLevel level,
            out float minimum,
            out float maximum)
        {
            switch (level)
            {
                case HapticIntensityLevel.Low:
                    minimum = 0.035f;
                    maximum = 0.16f;
                    break;
                case HapticIntensityLevel.High:
                    minimum = 0.08f;
                    maximum = 0.52f;
                    break;
                case HapticIntensityLevel.Medium:
                    minimum = 0.055f;
                    maximum = 0.32f;
                    break;
                default:
                    minimum = 0f;
                    maximum = 0f;
                    break;
            }
        }

        private static void GetAndroidAmplitudeRange(
            HapticIntensityLevel level,
            out int minimum,
            out int maximum)
        {
            switch (level)
            {
                case HapticIntensityLevel.Low:
                    minimum = 24;
                    maximum = 90;
                    break;
                case HapticIntensityLevel.High:
                    minimum = 64;
                    maximum = 235;
                    break;
                case HapticIntensityLevel.Medium:
                    minimum = 40;
                    maximum = 170;
                    break;
                default:
                    minimum = 0;
                    maximum = 0;
                    break;
            }
        }

        internal static bool IsRollingReportFresh(float now, float lastReportTime, float timeout)
        {
            return timeout > 0f && now - lastReportTime <= timeout;
        }

        internal static float SmoothRollingSpeed(
            float current,
            float target,
            float riseTime,
            float fallTime,
            float deltaTime)
        {
            float clampedCurrent = Mathf.Clamp01(current);
            float clampedTarget = Mathf.Clamp01(target);
            float timeConstant = clampedTarget >= clampedCurrent ? riseTime : fallTime;
            if (timeConstant <= 0f)
            {
                return clampedTarget;
            }

            float blend = 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / timeConstant);
            return Mathf.Lerp(clampedCurrent, clampedTarget, blend);
        }

        internal static bool ShouldSendPlatformUpdate(
            float current,
            float lastSent,
            float now,
            float nextScheduledTime,
            float minimumDifference)
        {
            return lastSent < 0f
                || (now >= nextScheduledTime
                    && Mathf.Abs(current - lastSent) >= Mathf.Max(0f, minimumDifference));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            enabledState = true;
            isQuitting = false;
            intensityLevelLoaded = false;
            intensityLevel = HapticIntensityLevel.Medium;
            PlatformHaptics.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateAfterSceneLoad()
        {
            if (Application.isPlaying && enabledState && IntensityLevel != HapticIntensityLevel.Off)
            {
                EnsureInstance();
            }
        }

        private static MobileHaptics EnsureInstance()
        {
            if (instance != null || isQuitting)
            {
                return instance;
            }

            instance = FindObjectOfType<MobileHaptics>();
            if (instance != null)
            {
                return instance;
            }

            GameObject serviceObject = new GameObject(nameof(MobileHaptics));
            instance = serviceObject.AddComponent<MobileHaptics>();
            DontDestroyOnLoad(serviceObject);
            return instance;
        }

        private static void EnsureIntensityLevelLoaded()
        {
            if (!intensityLevelLoaded)
            {
                ReloadIntensityLevelFromPreferences();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            float report = strongestRollingReport;
            strongestRollingReport = 0f;
            float now = Time.realtimeSinceStartup;

            bool forceStop = !enabledState || IntensityLevel == HapticIntensityLevel.Off;
            bool reportIsFresh = IsRollingReportFresh(now, lastRollingReportTime, rollingReportTimeout);
            if (forceStop || !reportIsFresh)
            {
                if (isContinuousPlaying)
                {
                    StopContinuousFeedback(forceStop);
                }

                return;
            }

            if (report > 0f)
            {
                targetRollingSpeed = report;
            }

            currentRollingSpeed = SmoothRollingSpeed(
                currentRollingSpeed,
                targetRollingSpeed,
                intensityRiseTime,
                intensityFallTime,
                Time.unscaledDeltaTime);

            float outputStrength = ApplyAndroidAmplitudeCurve(currentRollingSpeed);

            if (!isContinuousPlaying)
            {
                isContinuousPlaying = true;
                SendStart(currentRollingSpeed);
                return;
            }

            bool shouldSendUpdate = ShouldSendPlatformUpdate(
                outputStrength,
                lastSentOutputStrength,
                now,
                nextPlatformUpdateTime,
                minimumUpdateDifference);
            if (Time.frameCount % 15 == 0)
            {
                LogDiagnostic(
                    $"[HapticsState] speed={currentRollingSpeed:F3}, "
                    + $"curve={ApplyResponseCurve(currentRollingSpeed):F3}, "
                    + $"androidCurve={outputStrength:F3}, lastOutput={lastSentOutputStrength:F3}, "
                    + $"delta={Mathf.Abs(outputStrength - lastSentOutputStrength):F3}, "
                    + $"intervalRemaining={Mathf.Max(0f, nextPlatformUpdateTime - now):F3}, "
                    + $"shouldUpdate={shouldSendUpdate}");
            }

            if (shouldSendUpdate)
            {
                SendUpdate(currentRollingSpeed);
            }
        }

        private void SendStart(float normalizedSpeed)
        {
            HapticIntensityLevel level = IntensityLevel;
            float responseCurve = ApplyResponseCurve(normalizedSpeed);
            float outputStrength = ApplyAndroidAmplitudeCurve(normalizedSpeed);
            float intensity = CalculateProfileIntensity(normalizedSpeed, level);
            float sharpness = CalculateSharpness(normalizedSpeed);
            int amplitude = CalculateProfileAndroidAmplitude(normalizedSpeed, level);
            LogDiagnostic(
                $"[HapticsStart] speed={normalizedSpeed:F3}, "
                + $"curve={responseCurve:F3}, androidCurve={outputStrength:F3}, level={level}, "
                + $"iosIntensity={intensity:F3}, sharpness={sharpness:F3}, "
                + $"androidAmplitude={amplitude}");
            PlatformHaptics.StartContinuous(intensity, sharpness, amplitude, level);
            RecordPlatformUpdate(outputStrength);
        }

        private void SendUpdate(float normalizedSpeed)
        {
            HapticIntensityLevel level = IntensityLevel;
            float responseCurve = ApplyResponseCurve(normalizedSpeed);
            float outputStrength = ApplyAndroidAmplitudeCurve(normalizedSpeed);
            float intensity = CalculateProfileIntensity(normalizedSpeed, level);
            float sharpness = CalculateSharpness(normalizedSpeed);
            int amplitude = CalculateProfileAndroidAmplitude(normalizedSpeed, level);
            LogDiagnostic(
                $"[HapticsUpdate] speed={normalizedSpeed:F3}, "
                + $"curve={responseCurve:F3}, androidCurve={outputStrength:F3}, level={level}, "
                + $"iosIntensity={intensity:F3}, sharpness={sharpness:F3}, "
                + $"androidAmplitude={amplitude}");
            PlatformHaptics.UpdateContinuous(intensity, sharpness, amplitude, level);
            RecordPlatformUpdate(outputStrength);
        }

        [System.Diagnostics.Conditional("BALLGAME_HAPTICS_DIAGNOSTICS")]
        internal static void LogDiagnostic(string message)
        {
            Debug.Log(message);
        }

        private void RecordPlatformUpdate(float outputStrength)
        {
            lastSentOutputStrength = outputStrength;
            nextPlatformUpdateTime = Time.realtimeSinceStartup + platformUpdateInterval;
        }

        private void StopContinuousFeedback(bool immediate)
        {
            bool wasPlaying = isContinuousPlaying;
            strongestRollingReport = 0f;
            targetRollingSpeed = 0f;
            currentRollingSpeed = 0f;
            lastRollingReportTime = float.NegativeInfinity;
            lastSentOutputStrength = -1f;
            nextPlatformUpdateTime = 0f;
            isContinuousPlaying = false;
            if (wasPlaying)
            {
                LogDiagnostic($"[HapticsStop] immediate={immediate}");
            }
            PlatformHaptics.StopContinuous(immediate);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Stop();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Stop();
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            Stop();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                StopContinuousFeedback(true);
                instance = null;
            }
        }

        private static class PlatformHaptics
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            private static AndroidJavaObject vibrator;
            private static int sdkVersion;
            private static bool hasAmplitudeControl;
            private static bool initializationAttempted;
            private static bool loggedFailure;
            private static bool androidContinuousPlaying;
            private static int lastAndroidAmplitude = -1;
            private static int lastAndroidLevel = -1;
#endif

            public static void StartContinuous(
                float intensity,
                float sharpness,
                int amplitude,
                HapticIntensityLevel level)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                PlayAndroidContinuous(amplitude, level, true);
#elif UNITY_IOS && !UNITY_EDITOR
                BallGameHapticsStartContinuous(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness));
#endif
            }

            public static void UpdateContinuous(
                float intensity,
                float sharpness,
                int amplitude,
                HapticIntensityLevel level)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                PlayAndroidContinuous(amplitude, level, false);
#elif UNITY_IOS && !UNITY_EDITOR
                BallGameHapticsUpdateContinuous(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness));
#endif
            }

            public static void StopContinuous(bool immediate)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    LogDiagnostic(
                        $"[HapticsAndroidStop] immediate={immediate}, "
                        + $"playing={androidContinuousPlaying}, previous={lastAndroidAmplitude}");
                    vibrator?.Call("cancel");
                    androidContinuousPlaying = false;
                    lastAndroidAmplitude = -1;
                    lastAndroidLevel = -1;
                }
                catch (Exception exception)
                {
                    LogPlatformFailure(exception);
                }
#elif UNITY_IOS && !UNITY_EDITOR
                BallGameHapticsStopContinuous(immediate ? 1 : 0);
#endif
            }

            public static void Reset()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                vibrator?.Dispose();
                vibrator = null;
                sdkVersion = 0;
                hasAmplitudeControl = false;
                initializationAttempted = false;
                loggedFailure = false;
                androidContinuousPlaying = false;
                lastAndroidAmplitude = -1;
                lastAndroidLevel = -1;
#endif
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            private static void PlayAndroidContinuous(
                int amplitude,
                HapticIntensityLevel level,
                bool forceRestart)
            {
                if (!TryInitializeAndroid())
                {
                    return;
                }

                try
                {
                    int selectedAmplitude = hasAmplitudeControl ? Mathf.Clamp(amplitude, 1, 255) : -1;
                    int selectedLevel = (int)level;
                    bool outputUnchanged = hasAmplitudeControl
                        ? selectedAmplitude == lastAndroidAmplitude
                        : selectedLevel == lastAndroidLevel;
                    LogDiagnostic(
                        $"[HapticsAndroid] level={level}, amplitude={selectedAmplitude}, "
                        + $"previous={lastAndroidAmplitude}, unchanged={outputUnchanged}, "
                        + $"forceRestart={forceRestart}, sdk={sdkVersion}, "
                        + $"amplitudeControl={hasAmplitudeControl}");
                    if (!forceRestart && androidContinuousPlaying && outputUnchanged)
                    {
                        return;
                    }

                    if (sdkVersion >= 26 && hasAmplitudeControl)
                    {
                        GetAndroidAmplitudeRange(level, out int minimumAmplitude, out int maximumAmplitude);
                        float textureStrength = maximumAmplitude > minimumAmplitude
                            ? Mathf.InverseLerp(minimumAmplitude, maximumAmplitude, selectedAmplitude)
                            : 0f;
                        long[] timings = BuildAndroidTextureTimings(textureStrength);
                        int[] amplitudes = BuildAndroidTextureAmplitudes(selectedAmplitude);
                        using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                                   "createWaveform", timings, amplitudes, 1))
                        {
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        long[] pattern = GetAndroidFallbackPattern(level);
                        if (sdkVersion >= 26)
                        {
                            using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                            using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                                       "createWaveform", pattern, 1))
                            {
                                vibrator.Call("vibrate", effect);
                            }
                        }
                        else
                        {
                            vibrator.Call("vibrate", pattern, 1);
                        }
                    }

                    androidContinuousPlaying = true;
                    lastAndroidAmplitude = selectedAmplitude;
                    lastAndroidLevel = selectedLevel;
                }
                catch (Exception exception)
                {
                    LogPlatformFailure(exception);
                }
            }

            private static bool TryInitializeAndroid()
            {
                if (initializationAttempted)
                {
                    return vibrator != null;
                }

                initializationAttempted = true;
                try
                {
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        sdkVersion = versionClass.GetStatic<int>("SDK_INT");
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }

                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    {
                        vibrator?.Dispose();
                        vibrator = null;
                        return false;
                    }

                    hasAmplitudeControl = sdkVersion >= 26 && vibrator.Call<bool>("hasAmplitudeControl");
                    LogDiagnostic(
                        $"[HapticsCapability] SDK={sdkVersion}, HasVibrator=True, "
                        + $"AmplitudeControl={hasAmplitudeControl}");
                    return true;
                }
                catch (Exception exception)
                {
                    LogPlatformFailure(exception);
                    vibrator?.Dispose();
                    vibrator = null;
                    return false;
                }
            }

            private static void LogPlatformFailure(Exception exception)
            {
                if (loggedFailure)
                {
                    return;
                }

                loggedFailure = true;
                Debug.LogWarning($"Mobile haptics are unavailable: {exception.Message}");
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
            private static extern void BallGameHapticsStartContinuous(float intensity, float sharpness);

            [DllImport("__Internal")]
            private static extern void BallGameHapticsUpdateContinuous(float intensity, float sharpness);

            [DllImport("__Internal")]
            private static extern void BallGameHapticsStopContinuous(int immediate);
#endif
        }
    }
}
