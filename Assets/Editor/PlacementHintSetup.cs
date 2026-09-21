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
            var samples = CaptureSelection(Selection.gameObjects, out var level);
            PlacementHintGroupWindow.Open(level, samples);
        }

        public static System.Collections.Generic.List<PlacementHintTarget> CaptureSelection(GameObject[] selection, out LevelDefinition level)
        {
            level = null;
            var tools = new System.Collections.Generic.List<PlaceableTool>();
            foreach (var selected in selection)
            {
                if (selected == null || EditorUtility.IsPersistent(selected) || !selected.scene.IsValid()
                    || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(selected) != null)
                    throw new System.InvalidOperationException("请在关卡 Hierarchy 中选中道具实例或其子物体。");
                var tool = selected.GetComponentInParent<PlaceableTool>(true);
                if (tool == null) throw new System.InvalidOperationException("选中对象及父物体上没有 PlaceableTool。");
                var manager = FindInScene<GameManager>(tool.gameObject.scene);
                if (manager == null || manager.CurrentLevel == null)
                    throw new System.InvalidOperationException("道具所在场景需要配置 GameManager 的 Level。");
                if (level != null && level != manager.CurrentLevel)
                    throw new System.InvalidOperationException("一次只能记录同一关卡的道具。");
                if (tools.Count > 0 && tools[0].gameObject.scene != tool.gameObject.scene)
                    throw new System.InvalidOperationException("一次只能记录同一场景的道具。");
                level = manager.CurrentLevel;
                if (!tools.Contains(tool)) tools.Add(tool);
            }
            if (tools.Count == 0) throw new System.InvalidOperationException("请先在 Hierarchy 中选择一个或多个道具。");
            tools.Sort((a, b) => CompareHierarchy(a.transform, b.transform));
            var targets = new System.Collections.Generic.List<PlacementHintTarget>();
            foreach (var tool in tools)
            {
                var definition = ResolveDefinition(tool);
                if (definition == null) throw new System.InvalidOperationException("未找到 " + tool.name + " 的 ToolDefinition，请在 Definition 中指定道具资产。");
                targets.Add(new PlacementHintTarget
                {
                    tool = definition, position = tool.transform.position, rotationDegrees = tool.transform.eulerAngles.z,
                    cameraPosition = tool.transform.position
                });
            }
            return targets;
        }

        private static int CompareHierarchy(Transform a, Transform b)
        {
            var left = new System.Collections.Generic.List<int>();
            var right = new System.Collections.Generic.List<int>();
            for (var t = a; t != null; t = t.parent) left.Insert(0, t.GetSiblingIndex());
            for (var t = b; t != null; t = t.parent) right.Insert(0, t.GetSiblingIndex());
            for (int i = 0; i < Mathf.Min(left.Count, right.Count); i++)
                if (left[i] != right[i]) return left[i].CompareTo(right[i]);
            return left.Count.CompareTo(right.Count);
        }

        public static void ConvertLegacy(LevelDefinition level)
        {
            if (level == null || level.PlacementHintGroups.Count > 0 || level.PlacementHints.Count == 0) return;
            Undo.RecordObject(level, "Convert Placement Hint Groups");
            var serialized = new SerializedObject(level);
            CopyLegacy(level, serialized.FindProperty("placementHintGroups"));
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(level);
        }

        private static void CopyLegacy(LevelDefinition level, SerializedProperty groups)
        {
            if (groups.arraySize > 0) return;
            for (int i = 0; i < level.PlacementHints.Count; i++)
            {
                var group = groups.GetArrayElementAtIndex(groups.arraySize++);
                group.FindPropertyRelative("groupName").stringValue = "旧提示 " + (i + 1);
                var targets = group.FindPropertyRelative("targets");
                targets.arraySize = 1;
                WriteTarget(targets.GetArrayElementAtIndex(0), level.PlacementHints[i] ?? new PlacementHintTarget());
            }
        }

        public static void AppendTargets(LevelDefinition level, int groupIndex, string groupName,
            System.Collections.Generic.List<PlacementHintTarget> targets)
        {
            if (level == null || targets == null || targets.Count == 0) throw new System.InvalidOperationException("没有可记录的目标。");
            foreach (var target in targets)
                if (target == null || !target.IsValid) throw new System.InvalidOperationException("录入目标无效，未写入配置。");
            int count = level.PlacementHintGroups.Count > 0 ? level.PlacementHintGroups.Count : level.PlacementHints.Count;
            if (groupIndex < -1 || groupIndex >= count) throw new System.InvalidOperationException("提示组已变化，请重新选择。");
            Undo.RecordObject(level, "Append Placement Hint Targets");
            var serialized = new SerializedObject(level);
            var groups = serialized.FindProperty("placementHintGroups");
            CopyLegacy(level, groups);
            if (groupIndex == -1)
            {
                groupIndex = groups.arraySize++;
                var created = groups.GetArrayElementAtIndex(groupIndex);
                created.FindPropertyRelative("groupName").stringValue = string.IsNullOrWhiteSpace(groupName) ? "提示组 " + (groupIndex + 1) : groupName;
                created.FindPropertyRelative("targets").arraySize = 0;
            }
            var entries = groups.GetArrayElementAtIndex(groupIndex).FindPropertyRelative("targets");
            int appended = 0;
            foreach (var target in targets)
            {
                bool duplicate = false;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var existing = entries.GetArrayElementAtIndex(i);
                    if (existing.FindPropertyRelative("tool").objectReferenceValue == target.tool
                        && (existing.FindPropertyRelative("position").vector3Value - target.position).sqrMagnitude < 0.00000001f
                        && Mathf.Abs(Mathf.DeltaAngle(existing.FindPropertyRelative("rotationDegrees").floatValue, target.rotationDegrees)) < 0.001f)
                    { duplicate = true; break; }
                }
                if (duplicate) continue;
                WriteTarget(entries.GetArrayElementAtIndex(entries.arraySize++), target);
                appended++;
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(level);
            Debug.Log("已录入 " + appended + " 个提示目标（已跳过重复位置）；请保存关卡资产。", level);
        }

        private static void WriteTarget(SerializedProperty entry, PlacementHintTarget source)
        {
            entry.FindPropertyRelative("tool").objectReferenceValue = source.tool;
            entry.FindPropertyRelative("position").vector3Value = source.position;
            entry.FindPropertyRelative("rotationDegrees").floatValue = source.rotationDegrees;
            entry.FindPropertyRelative("snapRadius").floatValue = source.snapRadius;
            entry.FindPropertyRelative("cameraPosition").vector2Value = source.cameraPosition;
            entry.FindPropertyRelative("cameraMoveDuration").floatValue = source.cameraMoveDuration;
            entry.FindPropertyRelative("positionTolerance").floatValue = source.positionTolerance;
            entry.FindPropertyRelative("rotationTolerance").floatValue = source.rotationTolerance;
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
