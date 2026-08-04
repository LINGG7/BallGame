using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class ShopModalPrefabBuilder
    {
        private const string MainMenuPath = "Assets/Resources/UI/MainMenuUI.prefab";
        private const string ShopModalPath = "Assets/Resources/UI/ShopModal.prefab";
        private static readonly Color Paper = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color Ink = new Color(0.20f, 0.15f, 0.10f, 1f);

        [InitializeOnLoadMethod]
        private static void QueueBuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ShopModalPath) == null)
                EditorApplication.delayCall += Build;
        }

        [MenuItem("Ball Game/UI/Rebuild Shop Modal Prefab")]
        public static void Rebuild()
        {
            Build();
            Debug.Log("Rebuilt " + ShopModalPath);
        }

        private static void Build()
        {
            GameObject menu = PrefabUtility.LoadPrefabContents(MainMenuPath);
            try
            {
                Button template = menu.transform.Find("NormalButton")?.GetComponent<Button>();
                if (template == null) throw new InvalidOperationException("NormalButton was not found in MainMenuUI.prefab.");

                GameObject root = CreateImage("ShopModal", null, new Color(0.10f, 0.08f, 0.05f, 0.62f));
                Stretch(root.GetComponent<RectTransform>());
                Button backdrop = root.AddComponent<Button>();
                backdrop.transition = Selectable.Transition.None;
                backdrop.targetGraphic = root.GetComponent<Image>();

                GameObject panel = CreateImage("Panel", root.transform, Paper);
                SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1760f, 900f));
                Button blocker = panel.AddComponent<Button>();
                blocker.transition = Selectable.Transition.None;
                blocker.targetGraphic = panel.GetComponent<Image>();

                Button back = CloneButton(template, panel.transform, "BackButton", "‹");
                SetRect(back.GetComponent<RectTransform>(), new Vector2(-790f, 385f), new Vector2(92f, 64f));
                CreateText(template, panel.transform, "Title", "商城", new Vector2(-570f, 385f), new Vector2(420f, 70f), 42f, TextAlignmentOptions.Left);
                CreateCounter(template, panel.transform, "Coin", "●  0   ＋", new Vector2(570f, 385f), new Color(0.94f, 0.70f, 0.25f, 1f));
                CreateCounter(template, panel.transform, "Gem", "◆  0   ＋", new Vector2(755f, 385f), new Color(0.48f, 0.30f, 0.82f, 1f));
                GameObject divider = CreateImage("HeaderDivider", panel.transform, new Color(0.72f, 0.66f, 0.55f, 1f));
                SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 330f), new Vector2(1600f, 2f));

                GameObject sidebar = CreateImage("CategorySidebar", panel.transform, new Color(0.90f, 0.85f, 0.74f, 0.70f));
                SetRect(sidebar.GetComponent<RectTransform>(), new Vector2(-690f, -20f), new Vector2(240f, 650f));
                Button recommended = CloneButton(template, sidebar.transform, "RecommendedTab", "推荐");
                SetRect(recommended.GetComponent<RectTransform>(), new Vector2(0f, 250f), new Vector2(210f, 72f));
                Button skins = CloneButton(template, sidebar.transform, "SkinsTab", "皮肤");
                SetRect(skins.GetComponent<RectTransform>(), new Vector2(0f, 160f), new Vector2(210f, 72f));

                CreateText(template, panel.transform, "SectionTitle", "推荐商品", new Vector2(-260f, 265f), new Vector2(760f, 58f), 32f, TextAlignmentOptions.Left);
                GameObject cardArea = new GameObject("CardArea", typeof(RectTransform));
                cardArea.transform.SetParent(panel.transform, false);
                SetRect(cardArea.GetComponent<RectTransform>(), new Vector2(-245f, 35f), new Vector2(780f, 520f));
                Button card = CloneButton(template, panel.transform, "CardTemplate", "");
                SetRect(card.GetComponent<RectTransform>(), new Vector2(-500f, 70f), new Vector2(235f, 300f));
                TMP_Text name = card.GetComponentInChildren<TMP_Text>(true);
                name.name = "Name";
                ConfigureText(name, new Vector2(0f, -102f), new Vector2(210f, 42f), 24f, TextAlignmentOptions.Center);
                GameObject preview = CreateImage("Preview", card.transform, Color.white);
                preview.GetComponent<Image>().preserveAspect = true;
                SetRect(preview.GetComponent<RectTransform>(), new Vector2(0f, 45f), new Vector2(150f, 150f));
                CreateText(template, card.transform, "State", "", new Vector2(0f, -140f), new Vector2(210f, 36f), 18f, TextAlignmentOptions.Center);
                card.gameObject.SetActive(false);

                GameObject detail = CreateImage("SkinDetail", panel.transform, new Color(1f, 0.98f, 0.92f, 0.92f));
                SetRect(detail.GetComponent<RectTransform>(), new Vector2(590f, -10f), new Vector2(480f, 610f));
                CreateText(template, detail.transform, "DetailTitle", "当前皮肤", new Vector2(0f, 245f), new Vector2(400f, 52f), 30f, TextAlignmentOptions.Center);
                GameObject detailPreview = CreateImage("DetailPreview", detail.transform, Color.white);
                detailPreview.GetComponent<Image>().preserveAspect = true;
                SetRect(detailPreview.GetComponent<RectTransform>(), new Vector2(0f, 70f), new Vector2(300f, 300f));
                CreateText(template, detail.transform, "SelectedName", "", new Vector2(0f, -125f), new Vector2(400f, 50f), 28f, TextAlignmentOptions.Center);
                CreateText(template, detail.transform, "SelectedState", "", new Vector2(0f, -175f), new Vector2(400f, 42f), 22f, TextAlignmentOptions.Center);
                Button action = CloneButton(template, detail.transform, "ActionButton", "装备");
                SetRect(action.GetComponent<RectTransform>(), new Vector2(0f, -240f), new Vector2(230f, 64f));
                CreateText(template, detail.transform, "Status", "", new Vector2(0f, -285f), new Vector2(430f, 38f), 18f, TextAlignmentOptions.Center);

                PrefabUtility.SaveAsPrefabAsset(root, ShopModalPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(menu); }
        }

        private static Button CloneButton(Button source, Transform parent, string name, string text)
        {
            Button button = UnityEngine.Object.Instantiate(source, parent);
            button.name = name;
            button.onClick.RemoveAllListeners();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
            return button;
        }

        private static TMP_Text CreateText(Button template, Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            TMP_Text text = UnityEngine.Object.Instantiate(template.GetComponentInChildren<TMP_Text>(true), parent);
            text.name = name; text.text = value; ConfigureText(text, position, size, fontSize, alignment); return text;
        }

        private static void CreateCounter(Button template, Transform parent, string name, string value, Vector2 position, Color accent)
        {
            TMP_Text text = CreateText(template, parent, name + "Counter", value, position, new Vector2(180f, 52f), 24f, TextAlignmentOptions.Center);
            text.color = accent;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (parent != null) result.transform.SetParent(parent, false);
            result.GetComponent<Image>().color = color;
            return result;
        }

        private static void ConfigureText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            SetRect(text.rectTransform, position, size); text.fontSize = text.fontSizeMax = fontSize; text.alignment = alignment; text.color = Ink;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
