using BallGame.Core;
using BallGame.Placement;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class PlacementHintSetup
    {
        [MenuItem("Ball Game/Placement Hints/Append Selected Tool As Target")]
        public static void AppendSelectedTool()
        {
            var selected = Selection.activeGameObject;
            if (selected == null || EditorUtility.IsPersistent(selected) || !selected.scene.IsValid()
                || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(selected) != null)
                throw new System.InvalidOperationException("请在关卡 Hierarchy 中选中道具实例，而不是 Project 中的预制体或预制体编辑模式中的对象。");
            var tool = selected.GetComponentInParent<PlaceableTool>(true);
            if (tool == null)
                throw new System.InvalidOperationException("选中的对象及其父物体上没有 PlaceableTool，请选中场景里的道具或其子物体。");
            var definition = ResolveDefinition(tool);
            if (definition == null)
                throw new System.InvalidOperationException("未找到此道具对应的 ToolDefinition。请在 PlaceableTool 的 Definition 中指定对应的道具资产，再记录提示。");
            var manager = FindInScene<GameManager>(tool.gameObject.scene);
            if (manager == null || manager.CurrentLevel == null)
                throw new System.InvalidOperationException("The selected tool's scene needs a configured GameManager level.");
            var level = manager.CurrentLevel;
            Undo.RecordObject(level, "Append Placement Hint");
            var serialized = new SerializedObject(level);
            var hints = serialized.FindProperty("placementHints");
            int index = hints.arraySize++;
            var target = hints.GetArrayElementAtIndex(index);
            target.FindPropertyRelative("tool").objectReferenceValue = definition;
            target.FindPropertyRelative("position").vector3Value = tool.transform.position;
            target.FindPropertyRelative("rotationDegrees").floatValue = tool.transform.eulerAngles.z;
            target.FindPropertyRelative("snapRadius").floatValue = 0.65f;
            target.FindPropertyRelative("cameraPosition").vector2Value = tool.transform.position;
            target.FindPropertyRelative("cameraMoveDuration").floatValue = 0.6f;
            target.FindPropertyRelative("positionTolerance").floatValue = 0.05f;
            target.FindPropertyRelative("rotationTolerance").floatValue = 2f;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(level);
            Debug.Log("Appended hint " + (index + 1) + ". Save the level asset to keep it. Only position and rotation are recorded; use the configured prefab's default scale.", level);
        }
        private static ToolDefinition ResolveDefinition(PlaceableTool tool)
        {
            if (tool.Definition != null) return tool.Definition;
            // Editor-placed prefabs have not passed through PlacementManager.Initialize.
            // Match exact prefab source identity, never the instance's display name.
            string[] definitions = AssetDatabase.FindAssets("t:ToolDefinition");
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(tool.gameObject);
            while (source != null)
            {
                ToolDefinition match = null;
                foreach (string guid in definitions)
                {
                    var candidate = AssetDatabase.LoadAssetAtPath<ToolDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                    if (candidate == null || candidate.Prefab != source) continue;
                    if (match != null && match != candidate)
                        throw new System.InvalidOperationException("多个 ToolDefinition 使用此预制体，请在 PlaceableTool 的 Definition 中明确指定一个。");
                    match = candidate;
                }
                if (match != null) return match;
                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
            }
            return null;
        }

        private static T FindInScene<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }
    }
}
