using BallGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class GameHudTmpConverter
    {
        private static readonly string[] PrefabPaths =
        {
            "Assets/Resources/UI/GameHud.prefab",
            "Assets/Resources/UI/ToolInventoryButton.prefab"
        };

        [MenuItem("Ball Game/Convert GameHud Text To TMP")]
        public static void ConvertGameHudTextToTmp()
        {
            int convertedCount = 0;

            foreach (string path in PrefabPaths)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                {
                    continue;
                }

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                convertedCount += ConvertTextsInPrefab(prefabRoot);
                ReassignKnownReferences(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Converted {convertedCount} GameHud text component(s) to TextMeshProUGUI.");
        }

        private static int ConvertTextsInPrefab(GameObject root)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text oldText in texts)
            {
                ConvertText(oldText);
            }

            return texts.Length;
        }

        private static void ConvertText(Text oldText)
        {
            GameObject owner = oldText.gameObject;
            string text = oldText.text;
            Color color = oldText.color;
            int fontSize = oldText.fontSize;
            bool raycastTarget = oldText.raycastTarget;
            bool supportRichText = oldText.supportRichText;
            bool resizeForBestFit = oldText.resizeTextForBestFit;
            int resizeMin = oldText.resizeTextMinSize;
            int resizeMax = oldText.resizeTextMaxSize;
            TextAnchor alignment = oldText.alignment;

            UnityEngine.Object.DestroyImmediate(oldText, true);

            TextMeshProUGUI tmp = owner.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = raycastTarget;
            tmp.richText = supportRichText;
            tmp.enableAutoSizing = resizeForBestFit;
            tmp.fontSizeMin = resizeForBestFit ? Mathf.Max(1, resizeMin) : Mathf.Max(1, fontSize - 8);
            tmp.fontSizeMax = resizeForBestFit ? Mathf.Max(fontSize, resizeMax) : fontSize;
            tmp.alignment = ConvertAlignment(alignment);
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
        }

        private static void ReassignKnownReferences(GameObject root)
        {
            GameHudRefs hud = root.GetComponent<GameHudRefs>();
            if (hud != null)
            {
                SerializedObject serializedHud = new SerializedObject(hud);
                serializedHud.FindProperty("titleText").objectReferenceValue = FindTmp(root.transform, "Top Bar/Title");
                serializedHud.FindProperty("statusText").objectReferenceValue = FindTmp(root.transform, "Top Bar/Status");
                serializedHud.ApplyModifiedPropertiesWithoutUndo();
            }

            ToolInventoryButton inventoryButton = root.GetComponent<ToolInventoryButton>();
            if (inventoryButton != null)
            {
                SerializedObject serializedButton = new SerializedObject(inventoryButton);
                serializedButton.FindProperty("label").objectReferenceValue = FindTmp(root.transform, "Label");
                serializedButton.FindProperty("countLabel").objectReferenceValue = FindTmp(root.transform, "Count");
                serializedButton.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static TMP_Text FindTmp(Transform root, string path)
        {
            Transform child = root.Find(path);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
