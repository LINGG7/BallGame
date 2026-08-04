using BallGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class LevelCompletePopupPrefabBuilder
    {
        private const string PopupPath = "Assets/Resources/UI/LevelCompletePopup.prefab";
        private const string GameHudPath = "Assets/Resources/UI/GameHud.prefab";
        private const string BorderPath = "Assets/Art/UI/Border.png";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai-Light SDF.asset";

        [DidReloadScripts]
        private static void EnsureAfterReload()
        {
            EditorApplication.delayCall += EnsureAfterReloadDelayed;
        }

        private static void EnsureAfterReloadDelayed()
        {
            EnsurePopupPrefabAndHudReference(false);
        }

        [MenuItem("Ball Game/UI/Rebuild Level Complete Popup Prefab")]
        public static void RebuildPopupPrefab()
        {
            EnsurePopupPrefabAndHudReference(true);
        }

        private static void EnsurePopupPrefabAndHudReference(bool forceRebuild)
        {
            if (forceRebuild || AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath) == null)
            {
                CreatePopupPrefab();
            }

            EnsurePopupCostSummary();
            EnsureHudCostText();
            AssignPopupPrefabToHud();
        }

        private static void EnsureHudCostText()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameHudPath) == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(GameHudPath);
            GameHudRefs hud = root.GetComponent<GameHudRefs>();
            Transform topBar = root.transform.Find("Top Bar");
            TMP_Text status = topBar != null ? topBar.Find("Status")?.GetComponent<TMP_Text>() : null;
            TMP_Text costText = topBar != null ? topBar.Find("Cost")?.GetComponent<TMP_Text>() : null;
            if (costText == null && status != null)
            {
                costText = UnityEngine.Object.Instantiate(status, topBar);
                costText.name = "Cost";
                costText.text = string.Empty;
                costText.fontSize = 18f;
                costText.rectTransform.sizeDelta = new Vector2(300f, 42f);
                costText.rectTransform.anchoredPosition = new Vector2(155f, 0f);
                costText.gameObject.SetActive(false);
            }

            if (hud != null && costText != null)
            {
                SerializedObject serialized = new SerializedObject(hud);
                serialized.FindProperty("costText").objectReferenceValue = costText;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GameHudPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void EnsurePopupCostSummary()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath) == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PopupPath);
            Transform panel = root.transform.Find("Panel");
            if (panel != null && panel.Find("CostSummary") == null)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                TMP_Text costSummary = CreateText("CostSummary", panel, "本次成本：0　历史最低：--", 28f, font);
                RectTransform rect = costSummary.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -175f);
                rect.sizeDelta = new Vector2(500f, 92f);
                PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void CreatePopupPrefab()
        {
            Sprite borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BorderPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            GameObject overlay = CreateRect("LevelCompletePopup", null);
            try
            {
            RectTransform overlayTransform = overlay.GetComponent<RectTransform>();
            overlayTransform.anchorMin = Vector2.zero;
            overlayTransform.anchorMax = Vector2.one;
            overlayTransform.offsetMin = Vector2.zero;
            overlayTransform.offsetMax = Vector2.zero;

            Image blocker = overlay.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            blocker.raycastTarget = true;
            overlay.AddComponent<CanvasGroup>();

            GameObject panel = CreateRect("Panel", overlayTransform);
            RectTransform panelTransform = panel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0.5f);
            panelTransform.anchoredPosition = Vector2.zero;
            panelTransform.sizeDelta = new Vector2(640f, 420f);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.sprite = borderSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
            panelImage.raycastTarget = true;

            TMP_Text title = CreateText("Title", panelTransform, "闯关成功", 48f, font);
            RectTransform titleTransform = title.rectTransform;
            titleTransform.anchorMin = new Vector2(0.5f, 1f);
            titleTransform.anchorMax = new Vector2(0.5f, 1f);
            titleTransform.pivot = new Vector2(0.5f, 1f);
            titleTransform.anchoredPosition = new Vector2(0f, -86f);
            titleTransform.sizeDelta = new Vector2(460f, 90f);

            TMP_Text costSummary = CreateText("CostSummary", panelTransform, "本次成本：0　历史最低：--", 28f, font);
            RectTransform costSummaryTransform = costSummary.rectTransform;
            costSummaryTransform.anchorMin = new Vector2(0.5f, 1f);
            costSummaryTransform.anchorMax = new Vector2(0.5f, 1f);
            costSummaryTransform.pivot = new Vector2(0.5f, 1f);
            costSummaryTransform.anchoredPosition = new Vector2(0f, -175f);
            costSummaryTransform.sizeDelta = new Vector2(500f, 92f);

            Button menuButton = CreateButton("MenuButton", panelTransform, "返回菜单", new Vector2(-210f, -120f), borderSprite, font);
            Button retryButton = CreateButton("RetryButton", panelTransform, "再来一次", new Vector2(0f, -120f), borderSprite, font);
            Button nextButton = CreateButton("NextButton", panelTransform, "下一关", new Vector2(210f, -120f), borderSprite, font);
            menuButton.navigation = Navigation.defaultNavigation;
            retryButton.navigation = Navigation.defaultNavigation;
            nextButton.navigation = Navigation.defaultNavigation;

                PrefabUtility.SaveAsPrefabAsset(overlay, PopupPath);
                AssetDatabase.ImportAsset(PopupPath);
            }
            finally
            {
                if (overlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlay);
                }
            }
        }

        private static Button CreateButton(string name, Transform parent, string labelText, Vector2 position, Sprite borderSprite, TMP_FontAsset font)
        {
            GameObject buttonObject = CreateRect(name, parent);
            RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
            buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
            buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
            buttonTransform.pivot = new Vector2(0.5f, 0.5f);
            buttonTransform.anchoredPosition = position;
            buttonTransform.sizeDelta = new Vector2(180f, 76f);

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = borderSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText("Text", buttonTransform, labelText, 28f, font);
            RectTransform labelTransform = label.rectTransform;
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = new Vector2(18f, 8f);
            labelTransform.offsetMax = new Vector2(-18f, -8f);
            return button;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TMP_FontAsset font)
        {
            GameObject textObject = CreateRect(name, parent);
            TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.color = new Color(0.16f, 0.12f, 0.08f, 1f);
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return instance;
        }

        private static void AssignPopupPrefabToHud()
        {
            GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
            if (popupPrefab == null || AssetDatabase.LoadAssetAtPath<GameObject>(GameHudPath) == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(GameHudPath);
            LevelCompletePopup popup = root.GetComponent<LevelCompletePopup>();
            if (popup != null)
            {
                SerializedObject serialized = new SerializedObject(popup);
                serialized.FindProperty("popupPrefab").objectReferenceValue = popupPrefab.GetComponent<RectTransform>();
                serialized.FindProperty("overlayRoot").objectReferenceValue = null;
                serialized.FindProperty("panelRoot").objectReferenceValue = null;
                serialized.FindProperty("canvasGroup").objectReferenceValue = null;
                serialized.FindProperty("titleText").objectReferenceValue = null;
                serialized.FindProperty("costSummaryText").objectReferenceValue = null;
                serialized.FindProperty("menuButton").objectReferenceValue = null;
                serialized.FindProperty("retryButton").objectReferenceValue = null;
                serialized.FindProperty("nextButton").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GameHudPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
