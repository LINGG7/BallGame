using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class MobileHapticsTests
    {
        private static Type HapticsType => Type.GetType("BallGame.Core.MobileHaptics, Assembly-CSharp");

        [TestCase(0f, 0f)]
        [TestCase(0.75f, 0f)]
        [TestCase(7.375f, 0.5f)]
        [TestCase(14f, 1f)]
        [TestCase(20f, 1f)]
        public void NormalizeRollingSpeed_UsesThresholdAndClamps(float speed, float expected)
        {
            Assert.That(Invoke<float>("NormalizeRollingSpeed", speed, 0.75f, 14f),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void NormalizeRollingSpeed_InvalidRangeReturnsZero()
        {
            Assert.That(Invoke<float>("NormalizeRollingSpeed", 10f, 14f, 14f), Is.Zero);
            Assert.That(Invoke<float>("NormalizeRollingSpeed", 10f, 15f, 14f), Is.Zero);
        }

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.329877f)]
        [TestCase(1f, 1f)]
        [TestCase(2f, 1f)]
        public void ResponseCurve_ClampsAndUsesPowerCurve(float speed, float expected)
        {
            Assert.That(Invoke<float>("ApplyResponseCurve", speed), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void ResponseCurve_IsMonotonicAndChangesMoreAtMidHighSpeed()
        {
            float lowGain = Invoke<float>("ApplyResponseCurve", 0.3f)
                - Invoke<float>("ApplyResponseCurve", 0.1f);
            float highGain = Invoke<float>("ApplyResponseCurve", 0.9f)
                - Invoke<float>("ApplyResponseCurve", 0.7f);

            Assert.That(Invoke<float>("ApplyResponseCurve", 0.25f),
                Is.LessThan(Invoke<float>("ApplyResponseCurve", 0.5f)));
            Assert.That(Invoke<float>("ApplyResponseCurve", 0.5f),
                Is.LessThan(Invoke<float>("ApplyResponseCurve", 0.75f)));
            Assert.That(highGain, Is.GreaterThan(lowGain));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.574349f)]
        [TestCase(1f, 1f)]
        public void AndroidAmplitudeCurve_CompensatesCompressedMotorRange(float speed, float expected)
        {
            Assert.That(Invoke<float>("ApplyAndroidAmplitudeCurve", speed),
                Is.EqualTo(expected).Within(0.001f));
        }

        [TestCase(0.064f, 57)]
        [TestCase(0.336f, 97)]
        public void AndroidAmplitude_CommonGameplaySpeedsHaveClearSeparation(float speed, int expected)
        {
            object medium = Enum.Parse(IntensityLevelType, "Medium");
            Assert.That(Invoke<int>("CalculateProfileAndroidAmplitude", speed, medium),
                Is.EqualTo(expected));
        }

        [Test]
        public void RollingReports_KeepOnlyStrongestBallAndClampInput()
        {
            float strongest = Invoke<float>("MergeRollingReport", 0f, 0.35f);
            strongest = Invoke<float>("MergeRollingReport", strongest, 0.9f);
            strongest = Invoke<float>("MergeRollingReport", strongest, 0.6f);

            Assert.That(strongest, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(Invoke<float>("MergeRollingReport", strongest, 2f), Is.EqualTo(1f));
        }

        [TestCase("Off", 0f, 0f, 0)]
        [TestCase("Off", 1f, 0f, 0)]
        [TestCase("Low", 0f, 0.035f, 24)]
        [TestCase("Low", 1f, 0.16f, 90)]
        [TestCase("Medium", 0f, 0.055f, 40)]
        [TestCase("Medium", 1f, 0.32f, 170)]
        [TestCase("High", 0f, 0.08f, 64)]
        [TestCase("High", 1f, 0.52f, 235)]
        public void IntensityProfiles_MapToExpectedEndpoints(
            string levelName,
            float speed,
            float expectedIos,
            int expectedAndroid)
        {
            object level = Enum.Parse(IntensityLevelType, levelName);
            Assert.That(Invoke<float>("CalculateProfileIntensity", speed, level),
                Is.EqualTo(expectedIos).Within(0.001f));
            Assert.That(Invoke<int>("CalculateProfileAndroidAmplitude", speed, level),
                Is.EqualTo(expectedAndroid));
        }

        [Test]
        public void IntensityProfiles_ClampOutOfRangeSpeed()
        {
            object medium = Enum.Parse(IntensityLevelType, "Medium");
            Assert.That(Invoke<float>("CalculateProfileIntensity", -5f, medium), Is.EqualTo(0.055f));
            Assert.That(Invoke<float>("CalculateProfileIntensity", 5f, medium), Is.EqualTo(0.32f));
            Assert.That(Invoke<int>("CalculateProfileAndroidAmplitude", -5f, medium), Is.EqualTo(40));
            Assert.That(Invoke<int>("CalculateProfileAndroidAmplitude", 5f, medium), Is.EqualTo(170));
        }

        [Test]
        public void IntensityLevel_DefaultsToMediumAndPersistsSelection()
        {
            string key = GetPreferencesKey();
            PlayerPrefs.DeleteKey(key);
            Invoke<object>("ReloadIntensityLevelFromPreferences");
            Assert.That(GetIntensityLevelName(), Is.EqualTo("Medium"));

            SetIntensityLevel("High");
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(3));
            Invoke<object>("ReloadIntensityLevelFromPreferences");
            Assert.That(GetIntensityLevelName(), Is.EqualTo("High"));
        }

        [Test]
        public void IntensityLevel_InvalidSavedValueFallsBackToMedium()
        {
            PlayerPrefs.SetInt(GetPreferencesKey(), 999);
            Invoke<object>("ReloadIntensityLevelFromPreferences");
            Assert.That(GetIntensityLevelName(), Is.EqualTo("Medium"));
        }

        [Test]
        public void IntensityLevel_OffIsSavedAndCanBeRestored()
        {
            Assert.DoesNotThrow(() => SetIntensityLevel("Off"));
            Assert.That(GetIntensityLevelName(), Is.EqualTo("Off"));
            Assert.That(PlayerPrefs.GetInt(GetPreferencesKey()), Is.Zero);

            SetIntensityLevel("Low");
            Assert.That(GetIntensityLevelName(), Is.EqualTo("Low"));
        }

        [TestCase(10f, 9.78f, 0.25f, true)]
        [TestCase(10f, 9.751f, 0.25f, true)]
        [TestCase(10.001f, 9.75f, 0.25f, false)]
        [TestCase(10f, float.NegativeInfinity, 0.25f, false)]
        public void RollingReportFreshness_AllowsShortFixedUpdateGaps(
            float now,
            float lastReportTime,
            float timeout,
            bool expected)
        {
            Assert.That(Invoke<bool>("IsRollingReportFresh", now, lastReportTime, timeout), Is.EqualTo(expected));
        }

        [Test]
        public void RollingSpeed_SmoothlyApproachesTargetWithoutOvershooting()
        {
            Assert.That(Invoke<float>("SmoothRollingSpeed", 0f, 1f, 0.15f, 0.20f, 0.15f),
                Is.EqualTo(0.63212f).Within(0.001f));
            Assert.That(Invoke<float>("SmoothRollingSpeed", 1f, 0f, 0.15f, 0.20f, 0.20f),
                Is.EqualTo(0.36788f).Within(0.001f));
            Assert.That(Invoke<float>("SmoothRollingSpeed", 0.9f, 1f, 0.15f, 0.20f, 10f),
                Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PlatformUpdate_RequiresMeaningfulFinalStrengthChangeAndInterval()
        {
            Assert.That(Invoke<bool>("ShouldSendPlatformUpdate", 0.5f, -1f, 0f, 1f, 0.02f), Is.True);
            Assert.That(Invoke<bool>("ShouldSendPlatformUpdate", 0.5f, 0.47f, 1f, 2f, 0.02f), Is.False);
            Assert.That(Invoke<bool>("ShouldSendPlatformUpdate", 0.5f, 0.49f, 2f, 2f, 0.02f), Is.False);
            Assert.That(Invoke<bool>("ShouldSendPlatformUpdate", 0.5f, 0.47f, 2f, 2f, 0.02f), Is.True);
        }

        [TestCase(0f, 0.05f)]
        [TestCase(1f, 0.30f)]
        public void Sharpness_MapsFromSoftToClear(float speed, float expected)
        {
            Assert.That(Invoke<float>("CalculateSharpness", speed), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void AndroidStrength_UsesTwentyFourClampedSteps()
        {
            Assert.That(Invoke<float>("QuantizeStrength", -1f, 24), Is.Zero);
            Assert.That(Invoke<float>("QuantizeStrength", 1f, 24), Is.EqualTo(1f));
            Assert.That(Invoke<float>("QuantizeStrength", 0.51f, 24),
                Is.EqualTo(12f / 23f).Within(0.001f));
        }

        [Test]
        public void AndroidTexture_UsesMicroDutyCycleAndStaysWithinTarget()
        {
            int[] amplitudes = Invoke<int[]>("BuildAndroidTextureAmplitudes", 100);
            Assert.That(amplitudes, Is.EqualTo(new[] { 0, 78, 0, 100, 0 }));

            int[] clamped = Invoke<int[]>("BuildAndroidTextureAmplitudes", 300);
            Assert.That(clamped[clamped.Length - 1], Is.EqualTo(255));
            foreach (int amplitude in clamped)
            {
                Assert.That(amplitude, Is.InRange(0, 255));
            }

            Assert.That(Invoke<long[]>("BuildAndroidTextureTimings", 0f),
                Is.EqualTo(new[] { 0L, 12L, 8L, 12L, 8L }));
            Assert.That(Invoke<long[]>("BuildAndroidTextureTimings", 1f),
                Is.EqualTo(new[] { 0L, 18L, 2L, 18L, 2L }));
        }

        [TestCase("Low", 8L, 38L)]
        [TestCase("Medium", 10L, 26L)]
        [TestCase("High", 12L, 18L)]
        public void AndroidFallback_UsesLevelSpecificDutyCycles(
            string levelName,
            long expectedOn,
            long expectedOff)
        {
            object level = Enum.Parse(IntensityLevelType, levelName);
            long[] pattern = Invoke<long[]>("GetAndroidFallbackPattern", level);
            Assert.That(pattern, Is.EqualTo(new[] { 0L, expectedOn, expectedOff }));
        }

        [Test]
        public void Enabled_CanDisableAndReenableReportingGlobally()
        {
            Assert.That(HapticsType, Is.Not.Null);
            PropertyInfo enabled = HapticsType.GetProperty("Enabled", BindingFlags.Static | BindingFlags.Public);
            Assert.That(enabled, Is.Not.Null);

            enabled.SetValue(null, false);
            Assert.That(enabled.GetValue(null), Is.False);
            Assert.DoesNotThrow(() => HapticsType.GetMethod("ReportRolling").Invoke(null, new object[] { 1f }));

            enabled.SetValue(null, true);
            Assert.That(enabled.GetValue(null), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (HapticsType != null)
            {
                HapticsType.GetProperty("Enabled", BindingFlags.Static | BindingFlags.Public)?.SetValue(null, true);
                HapticsType.GetMethod("Stop", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
            }

            PlayerPrefs.DeleteKey(GetPreferencesKey());
            Invoke<object>("ReloadIntensityLevelFromPreferences");
        }

        private static Type IntensityLevelType => Type.GetType("BallGame.Core.HapticIntensityLevel, Assembly-CSharp");

        private static string GetPreferencesKey()
        {
            FieldInfo key = HapticsType.GetField(
                "IntensityLevelPlayerPrefsKey",
                BindingFlags.Static | BindingFlags.NonPublic);
            return (string)key.GetRawConstantValue();
        }

        private static string GetIntensityLevelName()
        {
            return HapticsType.GetProperty("IntensityLevel", BindingFlags.Static | BindingFlags.Public)
                .GetValue(null).ToString();
        }

        private static void SetIntensityLevel(string levelName)
        {
            object level = Enum.Parse(IntensityLevelType, levelName);
            HapticsType.GetMethod("SetIntensityLevel", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new[] { level });
        }

        private static T Invoke<T>(string methodName, params object[] arguments)
        {
            Assert.That(HapticsType, Is.Not.Null);
            MethodInfo method = HapticsType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
            return (T)method.Invoke(null, arguments);
        }
    }
}
