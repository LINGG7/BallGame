using System.Reflection;
using BallGame.Effects;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class BallDustPrefabBuilder
    {
        // Recovery revision: rebuild cached dust children after restoring Ball.prefab.
        private const string BallPrefabPath = "Assets/Resources/Prefabs/Ball.prefab";
        private const string DustMaterialPath = "Assets/Resources/Materials/BallDustParticle.mat";

        [InitializeOnLoadMethod]
        private static void SchedulePrefabValidation()
        {
            EditorApplication.delayCall += EnsurePrefabIsCurrent;
        }

        [MenuItem("Ball Game/Effects/Rebuild Ball Dust Effect")]
        public static void RebuildPrefab()
        {
            BuildPrefab(true);
        }

        private static void EnsurePrefabIsCurrent()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            if (prefab == null
                || (prefab.GetComponent<BallDustEffect>() != null
                    && prefab.transform.Find("ImpactDust") != null
                    && prefab.transform.Find("RollingDust") != null))
            {
                return;
            }

            BuildPrefab(false);
        }

        private static void BuildPrefab(bool logCompletion)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BallPrefabPath);
            if (root == null)
            {
                Debug.LogWarning($"Cannot rebuild ball dust effect because {BallPrefabPath} was not found.");
                return;
            }

            try
            {
                BallDustEffect effect = root.GetComponent<BallDustEffect>();
                if (effect == null)
                {
                    effect = root.AddComponent<BallDustEffect>();
                }

                Material material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
                SerializedObject serializedEffect = new SerializedObject(effect);
                serializedEffect.FindProperty("dustMaterial").objectReferenceValue = material;
                serializedEffect.ApplyModifiedPropertiesWithoutUndo();

                InvokeConfigurationMethod(effect, "ClampSettings");
                InvokeConfigurationMethod(effect, "EnsureParticleSystems");
                InvokeConfigurationMethod(effect, "ConfigureParticleSystems");

                PrefabUtility.SaveAsPrefabAsset(root, BallPrefabPath);
                if (logCompletion)
                {
                    Debug.Log("Rebuilt Ball.prefab dust particle systems.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void InvokeConfigurationMethod(BallDustEffect effect, string methodName)
        {
            MethodInfo method = typeof(BallDustEffect).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(effect, null);
        }
    }
}
