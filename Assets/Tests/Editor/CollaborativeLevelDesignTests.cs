using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests.Editor
{
    public sealed class CollaborativeLevelDesignTests
    {
        private const string RegistryPath = "Assets/Resources/LevelDesigner/CollaborativeLevelAssetRegistry.asset";
        private const string ScenePath = "Assets/Scenes/LevelDesigner.unity";

        [Test]
        public void DesignerAssets_ExistAndRegistryUsesStableKeys()
        {
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty entries = serialized.FindProperty("entries");
            Assert.That(entries.arraySize, Is.GreaterThanOrEqualTo(10));
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                Assert.That(entry.FindPropertyRelative("assetKey").stringValue, Is.Not.Empty);
                Assert.That(entry.FindPropertyRelative("prefab").objectReferenceValue, Is.Not.Null);
            }
        }

        [Test]
        public void EmptyDocument_IsRejectedForMissingBallAndGoal()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type registryType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelAssetRegistry, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            Assert.That(documentType, Is.Not.Null);
            Assert.That(registryType, Is.Not.Null);
            Assert.That(validatorType, Is.Not.Null);
            object document = Activator.CreateInstance(documentType);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            Assert.That(errors.Count, Is.GreaterThanOrEqualTo(2));
            string combined = string.Join("\n", errors.CastToStrings());
            StringAssert.Contains("Ball", combined);
            StringAssert.Contains("Goal", combined);
        }

        [Test]
        public void CurrentDocument_UsesVersionThreeAndRejectsLegacyTransformScale()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            Assert.That(documentType, Is.Not.Null);
            object current = Activator.CreateInstance(documentType);
            Assert.That((int)documentType.GetField("formatVersion").GetValue(current), Is.EqualTo(3));

            string json = "{\"formatVersion\":1,\"title\":\"Legacy\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"position\":{\"x\":0,\"y\":0},\"scale\":{\"x\":2,\"y\":1}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"position\":{\"x\":2,\"y\":0},\"scale\":{\"x\":1,\"y\":1}}]}";
            object document = JsonUtility.FromJson(json, documentType);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            StringAssert.Contains("must match the registered prefab scale", string.Join("\n", errors.CastToStrings()));
        }

        [Test]
        public void VersionTwo_RejectsSizeForNonResizableObject()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            string json = "{\"formatVersion\":2,\"title\":\"Invalid Size\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"position\":{\"x\":0,\"y\":0},\"scale\":{\"x\":1,\"y\":1},\"hasSize\":true,\"size\":{\"x\":2,\"y\":2}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"position\":{\"x\":2,\"y\":0},\"scale\":{\"x\":1,\"y\":1}}]}";
            object document = JsonUtility.FromJson(json, documentType);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            StringAssert.Contains("does not support size editing", string.Join("\n", errors.CastToStrings()));
        }

        [Test]
        public void VersionTwo_DefaultsCanvasCenterToOrigin()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            object document = JsonUtility.FromJson("{\"formatVersion\":2,\"title\":\"Legacy\",\"canvasWidth\":24,\"canvasHeight\":14}", documentType);
            Vector2 center = (Vector2)documentType.GetField("canvasCenter").GetValue(document);
            Assert.That(center, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void InfiniteBackgroundPrefab_UsesTiledBackgroundComponent()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/WorldBackground.prefab");
            Assert.That(prefab, Is.Not.Null);
            Type backgroundType = Type.GetType("BallGame.Core.InfiniteTiledBackground2D, Assembly-CSharp");
            Assert.That(prefab.GetComponent(backgroundType), Is.Not.Null);
        }

        [Test]
        public void DesignerUi_ContainsSizeInputsAndPersistentLabels()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LevelDesigner/DesignerUI.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size XInput"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size YInput"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size X Label"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size Y Label"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Scale XInput"), Is.Null);
            Assert.That(FindChild(prefab.transform, "Scale YInput"), Is.Null);
        }

        [Test]
        public void Registry_OnlyExposesSizeCapabilityForApprovedObjects()
        {
            Type interfaceType = Type.GetType("BallGame.Core.ISizeEditable2D, Assembly-CSharp");
            Assert.That(interfaceType, Is.Not.Null);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            SerializedProperty entries = new SerializedObject(registry).FindProperty("entries");
            string[] approved = { "anti_gravity", "water", "obstacle" };
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string key = entry.FindPropertyRelative("assetKey").stringValue;
                GameObject prefab = (GameObject)entry.FindPropertyRelative("prefab").objectReferenceValue;
                bool supportsSize = false;
                foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    supportsSize |= behaviour != null && interfaceType.IsInstanceOfType(behaviour);
                Assert.That(supportsSize, Is.EqualTo(Array.IndexOf(approved, key) >= 0), key);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name == name) return children[i];
            return null;
        }
    }

    internal static class CollaborativeTestListExtensions
    {
        public static string[] CastToStrings(this IList values)
        {
            string[] result = new string[values.Count];
            for (int i = 0; i < values.Count; i++) result[i] = values[i]?.ToString();
            return result;
        }
    }
}
