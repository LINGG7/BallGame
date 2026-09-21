using BallGame.Core;
using BallGame.Placement;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public sealed class PlacementHintGroupWindow : EditorWindow
    {
        [SerializeField] private LevelDefinition level;
        [SerializeField] private List<PlacementHintTarget> targets;
        [SerializeField] private int selectedGroup;
        [SerializeField] private string groupName = "提示组";

        public static void Open(LevelDefinition level, List<PlacementHintTarget> targets)
        {
            var window = GetWindow<PlacementHintGroupWindow>(true, "记录提示组", true);
            window.level = level;
            window.targets = targets;
            window.selectedGroup = 0;
            window.minSize = new Vector2(380f, 170f);
            window.Show();
        }
        private void OnGUI()
        {
            if (level == null || targets == null) { EditorGUILayout.LabelField("请重新选择场景道具并打开菜单。"); return; }
            EditorGUILayout.LabelField("关卡", level.name);
            EditorGUILayout.LabelField("已按 Hierarchy 顺序记录 " + targets.Count + " 个目标");
            var options = new List<string> { "新建组" };
            foreach (var group in level.GetEffectivePlacementHintGroups()) options.Add(group != null ? group.groupName : "无效组");
            selectedGroup = Mathf.Clamp(selectedGroup, 0, options.Count - 1);
            selectedGroup = EditorGUILayout.Popup("追加到", selectedGroup, options.ToArray());
            if (selectedGroup == 0) groupName = EditorGUILayout.TextField("组名", groupName);
            EditorGUILayout.HelpBox("使用打开窗口时的位置与旋转。旧配置会按每项一组保留；操作支持 Undo。", MessageType.Info);
            if (GUILayout.Button("记录目标"))
            {
                PlacementHintSetup.AppendTargets(level, selectedGroup - 1, groupName, targets);
                Close();
            }
        }
    }
}
