using System.Collections.Generic;
using BallGame.LevelDesign;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class CollaborativeLevelRegistryBuilder
    {
        private const string RegistryPath = "Assets/Resources/LevelDesigner/CollaborativeLevelAssetRegistry.asset";

        [MenuItem("Ball Game/Level Design/Create Asset Registry")]
        public static void CreateRegistry()
        {
            EnsureFolder("Assets/Resources/LevelDesigner");
            CollaborativeLevelAssetRegistry registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<CollaborativeLevelAssetRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 0;

            Add(entries, "ball", CollaborativeObjectKind.Ball, "Assets/Resources/Prefabs/Ball.prefab", "Assets/Resources/Art/Skins/Ball_Wood.png", null);
            Add(entries, "goal", CollaborativeObjectKind.Goal, "Assets/Resources/Prefabs/Goal.prefab", null, null);
            Add(entries, "plank", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Plank.prefab", "Assets/Resources/Art/ItemIcon/Plank_Icon.png", "Assets/Tools/Plank.asset");
            Add(entries, "spring", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Spring.prefab", null, "Assets/Tools/Spring.asset");
            Add(entries, "fan", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Fan.prefab", null, "Assets/Tools/Fan.asset");
            Add(entries, "conveyor", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Conveyor.prefab", "Assets/Resources/Art/ItemIcon/Conveyor_Icon.png", "Assets/Tools/Conveyor.asset");
            Add(entries, "magnet_plus", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Magnet+.prefab", null, "Assets/Tools/Magnet+.asset");
            Add(entries, "magnet_minus", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Magnet-.prefab", null, "Assets/Tools/Magnet-.asset");
            Add(entries, "portal", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Portal.prefab", null, "Assets/Tools/Portal.asset");
            Add(entries, "water", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/WaterZone.prefab", "Assets/Resources/Art/ItemIcon/WaterZone_Icon.png", "Assets/Tools/WaterZone.asset");
            Add(entries, "anti_gravity", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/AntiGravity.prefab", "Assets/Resources/Art/ItemIcon/Antigravity_Icon.png", "Assets/Tools/AntiGravity.asset");
            Add(entries, "obstacle", CollaborativeObjectKind.Tool, "Assets/Resources/Prefabs/Obstacle.prefab", null, null);
            Add(entries, "curve_obstacle", CollaborativeObjectKind.SplineTool, "Assets/Resources/Prefabs/CurveObstacle.prefab", null, null);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + RegistryPath);
        }

        private static void Add(SerializedProperty entries, string key, CollaborativeObjectKind kind, string prefabPath, string iconPath, string toolPath)
        {
            int index = entries.arraySize++;
            SerializedProperty element = entries.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("assetKey").stringValue = key;
            element.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            element.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            element.FindPropertyRelative("icon").objectReferenceValue = string.IsNullOrEmpty(iconPath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            element.FindPropertyRelative("toolDefinition").objectReferenceValue = string.IsNullOrEmpty(toolPath) ? null : AssetDatabase.LoadAssetAtPath<BallGame.Core.ToolDefinition>(toolPath);
            element.FindPropertyRelative("supportsSpline").boolValue = kind == CollaborativeObjectKind.SplineTool;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
