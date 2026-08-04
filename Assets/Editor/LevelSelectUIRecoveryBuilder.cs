using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    [InitializeOnLoad]
    internal static class LevelSelectUIRecoveryBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/LevelSelectUI.prefab";

        static LevelSelectUIRecoveryBuilder()
        {
            EditorApplication.delayCall += EnsurePrefab;
        }

        [MenuItem("Ball Game/Recovery/Rebuild LevelSelectUI")]
        public static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            var root = new GameObject("LevelSelectUI", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.SetActive(false);

            var panel = CreateRect("Panel", root.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1320f, 820f);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

            CreateButton("CloseButton", panel.transform, new Vector2(-570f, 350f), "关闭");
            CreateButton("PreviousPageButton", panel.transform, new Vector2(-150f, -330f), "上一页");
            CreateButton("NextPageButton", panel.transform, new Vector2(150f, -330f), "下一页");
            CreateText("PageText", panel.transform, new Vector2(0f, -330f), new Vector2(180f, 56f), "1/1", 28f);

            var grid = CreateRect("LevelGrid", panel.transform);
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(1080f, 600f);
            gridRect.anchoredPosition = new Vector2(0f, 20f);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(300f, 160f);
            layout.spacing = new Vector2(60f, 30f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 1; i <= 9; i++)
            {
                var button = CreateButton($"LevelSlot_{i:00}", grid.transform, Vector2.zero, string.Empty);
                CreateText("Text", button.transform, Vector2.zero, Vector2.zero, $"关卡 {i}", 30f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Recovered LevelSelectUI prefab from build structure.");
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 position, string label)
        {
            var go = CreateRect(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = name.StartsWith("LevelSlot") ? new Vector2(300f, 160f) : new Vector2(180f, 56f);
            rect.anchoredPosition = position;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.28f, 0.36f, 1f);
            go.AddComponent<Button>();
            if (!string.IsNullOrEmpty(label))
                CreateText("Label", go.transform, Vector2.zero, Vector2.zero, label, 24f);
            return go;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize)
        {
            var go = CreateRect(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = size == Vector2.zero ? Vector2.zero : position - size * 0.5f;
            rect.offsetMax = size == Vector2.zero ? Vector2.zero : position + size * 0.5f;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }
    }
}
