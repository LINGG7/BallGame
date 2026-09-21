using BallGame.Core;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    [CustomEditor(typeof(LevelDefinition))]
    public sealed class LevelDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "placementHints");
            serializedObject.ApplyModifiedProperties();
            var level = (LevelDefinition)target;
            if (level.PlacementHintGroups.Count == 0 && level.PlacementHints.Count > 0)
            {
                EditorGUILayout.HelpBox("正在兼容读取旧提示：每个旧目标独立为一组。转换后可在 Placement Hint Groups 中手动合并、调整目标。", MessageType.Info);
                if (GUILayout.Button("转换旧配置为分组")) PlacementHintSetup.ConvertLegacy(level);
            }
            EditorGUILayout.HelpBox("一次广告开启一组；Targets 按顺序逐个引导。多个目标需同时放置正确才结束。", MessageType.None);
        }
    }
}
