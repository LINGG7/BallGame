using System;
using System.Collections.Generic;
using System.Reflection;
using BallGame.Shop;
using UnityEngine;

// Standalone boundary doubles: Unity rendering/import are validated separately in the Editor.
namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { }
    public class GameObject { }
    public class SerializeField : Attribute { }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; public string fileName; }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public static class Resources { public static object Value; public static T Load<T>(string path) where T : class => Value as T; }
    public static class Debug { public static void LogWarning(object message) { } }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        public static string GetString(string key, string fallback) => Values.TryGetValue(key, out var v) ? (string)v : fallback;
        public static int GetInt(string key, int fallback) => Values.TryGetValue(key, out var v) ? (int)v : fallback;
        public static void SetString(string key, string value) => Values[key] = value;
        public static void SetInt(string key, int value) => Values[key] = value;
        public static void Save() { }
    }
}
namespace BallGame.Shop
{
    public interface IRewardedAdService { void ShowRewardedAd(string placementId, Action<bool> completed); }
    public static class BallSkinService { public static IRewardedAdService RewardedAdService { get; set; } }
}
class FakeAds : IRewardedAdService
{
    public Action<bool> Complete;
    public string Placement;
    public int Calls;
    public bool Throw;
    public void ShowRewardedAd(string placement, Action<bool> callback)
    {
        Calls++;
        if (Throw) throw new Exception("Unavailable");
        Placement = placement;
        Complete = callback;
    }
}
class Program
{
    static int checks;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    static void ResetService() => typeof(EffectSkinService).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    static void Main()
    {
        var catalog = new BallSkinCatalog();
        typeof(BallSkinCatalog).GetField("defaultSkinId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog, "basic");
        typeof(BallSkinCatalog).GetField("skins", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog, new List<BallSkinEntry>
        {
            new BallSkinEntry { id = "basic", unlockType = BallSkinUnlockType.Free },
            new BallSkinEntry { id = "fire", unlockType = BallSkinUnlockType.RewardedAd },
            new BallSkinEntry { id = "ice", unlockType = BallSkinUnlockType.RewardedAd }
        });
        Resources.Value = catalog;
        Check(EffectSkinService.CurrentSkinId == "basic", "Default");
        Check(EffectSkinService.IsUnlocked("basic") && !EffectSkinService.IsUnlocked("fire"), "Initial ownership");
        Check(!EffectSkinService.EquipSkin("fire"), "Locked equip must fail");
        bool success = true;
        EffectSkinService.TryUnlock("fire", result => success = result);
        Check(!success && !EffectSkinService.IsUnlocked("fire"), "Missing ads must not unlock");

        var ads = new FakeAds();
        BallSkinService.RewardedAdService = ads;
        int callbacks = 0;
        EffectSkinService.TryUnlock("fire", result => { success = result; callbacks++; });
        Check(ads.Placement == "effect_fire" && EffectSkinService.IsUnlockPending, "Ad placement and pending");
        EffectSkinService.TryUnlock("ice", result => Check(!result, "Concurrent request rejected"));
        Check(ads.Calls == 1, "No duplicate ad");
        ads.Complete(false);
        ads.Complete(true);
        Check(!success && callbacks == 1 && !EffectSkinService.IsUnlocked("fire"), "Failure and duplicate callback");
        Check(!EffectSkinService.IsUnlockPending, "Failure clears pending");
        EffectSkinService.TryUnlock("fire", result => { success = result; callbacks++; });
        ads.Complete(true);
        ads.Complete(false);
        Check(success && callbacks == 2 && EffectSkinService.IsUnlocked("fire"), "Success permanent, callback once");
        PlayerPrefs.SetString("BallGame.Shop.SelectedSkin", "wood");
        Check(EffectSkinService.EquipSkin("fire"), "Unlocked equip");
        Check(PlayerPrefs.GetString("BallGame.Shop.SelectedSkin", "") == "wood", "Independent appearance selection");
        ResetService();
        Check(EffectSkinService.CurrentSkinId == "fire" && EffectSkinService.IsUnlocked("fire"), "Ownership survives service reset");
        PlayerPrefs.SetString("BallGame.Shop.SelectedEffect", "ice");
        Check(EffectSkinService.CurrentSkinId == "basic", "Locked saved selection falls back");
        PlayerPrefs.SetString("BallGame.Shop.SelectedEffect", "invalid");
        Check(EffectSkinService.CurrentSkinId == "basic", "Missing saved selection falls back");
        ads.Throw = true;
        EffectSkinService.TryUnlock("ice", result => success = result);
        Check(!success && !EffectSkinService.IsUnlockPending && !EffectSkinService.IsUnlocked("ice"), "SDK exception");
        EffectSkinService.TryUnlock("missing", result => success = result);
        Check(!success, "Unknown ID fails");
        Console.WriteLine("PASS: " + checks + " service assertions (in-memory Unity boundary doubles).");
    }
}