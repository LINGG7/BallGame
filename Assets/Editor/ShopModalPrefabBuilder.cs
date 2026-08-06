using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class ShopModalPrefabBuilder
    {
        private const string MainMenuPath = "Assets/Resources/UI/MainMenuUI.prefab";
        private const string ShopModalPath = "Assets/Resources/UI/ShopModal.prefab";
        private const string WoodSkinPath = "Assets/Resources/Art/Skins/Ball_Wood.png";

        private static readonly Color Paper = new Color(0.965f, 0.945f, 0.89f, 1f);
        private static readonly Color PaperLight = new Color(0.995f, 0.985f, 0.95f, 1f);
        private static readonly Color PaperMuted = new Color(0.91f, 0.86f, 0.76f, 1f);
        private static readonly Color Ink = new Color(0.22f, 0.18f, 0.13f, 1f);
        private static readonly Color Line = new Color(0.55f, 0.48f, 0.38f, 0.45f);
        private static readonly Color Gold = new Color(0.98f, 0.76f, 0.30f, 1f);

        [InitializeOnLoadMethod]
        private static void QueueBuild()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShopModalPath);
            if (existing == null
                || existing.transform.Find("Panel/Content/CardArea/CardContent") == null
                || existing.transform.Find("Panel/Content/PrevPageButton") == null
                || existing.transform.Find("Panel/Content/NextPageButton") == null
                || existing.transform.Find("Panel/Content/CardArea")?.GetComponent<EventTrigger>() == null)
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
                if (template == null)
                    throw new InvalidOperationException("NormalButton was not found in MainMenuUI.prefab.");

                Sprite woodSkin = AssetDatabase.LoadAssetAtPath<Sprite>(WoodSkinPath);

                GameObject root = CreateImage("ShopModal", null, new Color(0.11f, 0.09f, 0.06f, 0.66f));
                Stretch(root.GetComponent<RectTransform>());
                Button backdrop = root.AddComponent<Button>();
                backdrop.transition = Selectable.Transition.None;
                backdrop.targetGraphic = root.GetComponent<Image>();

                GameObject panel = CreateImage("Panel", root.transform, Paper);
                SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1760f, 900f));
                AddShadow(panel, new Color(0.18f, 0.13f, 0.08f, 0.32f), new Vector2(10f, -10f));
                Button blocker = panel.AddComponent<Button>();
                blocker.transition = Selectable.Transition.None;
                blocker.targetGraphic = panel.GetComponent<Image>();

                BuildHeader(template, panel.transform);
                BuildSidebar(template, panel.transform);
                BuildContent(template, panel.transform, woodSkin);
                BuildDetail(template, panel.transform);
                CreateText(template, panel.transform, "Footer", "适度游戏，理性消费", new Vector2(0f, -422f), new Vector2(500f, 30f), 16f, TextAlignmentOptions.Center, new Color(0.39f, 0.34f, 0.27f, 0.55f));

                PrefabUtility.SaveAsPrefabAsset(root, ShopModalPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(menu);
            }
        }

        private static void BuildHeader(Button template, Transform panel)
        {
            Button back = CloneButton(template, panel, "BackButton", "<");
            SetRect(back.GetComponent<RectTransform>(), new Vector2(-812f, 386f), new Vector2(82f, 58f));
            SetButtonColors(back, PaperLight, new Color(0.94f, 0.88f, 0.75f, 1f));
            TMP_Text backLabel = back.GetComponentInChildren<TMP_Text>(true);
            if (backLabel != null)
                ConfigureText(backLabel, Vector2.zero, Vector2.zero, 40f, TextAlignmentOptions.Center, Ink, true);

            CreateText(template, panel, "ShopIcon", "店", new Vector2(-735f, 386f), new Vector2(52f, 54f), 28f, TextAlignmentOptions.Center, Ink);
            CreateText(template, panel, "Title", "商城", new Vector2(-585f, 386f), new Vector2(250f, 58f), 38f, TextAlignmentOptions.Left, Ink);

            GameObject headerBand = CreateImage("HeaderBand", panel, new Color(1f, 0.99f, 0.96f, 0.52f));
            SetRect(headerBand.GetComponent<RectTransform>(), new Vector2(130f, 386f), new Vector2(1280f, 62f));
            headerBand.transform.SetAsFirstSibling();

            GameObject divider = CreateImage("HeaderDivider", panel, Line);
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 335f), new Vector2(1640f, 2f));
        }

        private static void BuildSidebar(Button template, Transform panel)
        {
            GameObject sidebar = CreateImage("CategorySidebar", panel, new Color(0.89f, 0.83f, 0.71f, 0.72f));
            SetRect(sidebar.GetComponent<RectTransform>(), new Vector2(-735f, -24f), new Vector2(210f, 660f));
            AddShadow(sidebar, new Color(0.24f, 0.18f, 0.11f, 0.15f), new Vector2(3f, -3f));

            Button skins = CloneButton(template, sidebar.transform, "SkinsTab", "皮肤");
            SetRect(skins.GetComponent<RectTransform>(), new Vector2(0f, 244f), new Vector2(182f, 72f));
            SetButtonColors(skins, new Color(1f, 0.88f, 0.58f, 1f), Gold);
            TMP_Text label = skins.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                ConfigureText(label, Vector2.zero, Vector2.zero, 28f, TextAlignmentOptions.Center, Ink, true);

            CreateText(template, sidebar.transform, "CategoryHint", "-", new Vector2(-66f, 244f), new Vector2(28f, 28f), 18f, TextAlignmentOptions.Center, new Color(0.73f, 0.47f, 0.18f, 1f));
        }

        private static void BuildContent(Button template, Transform panel, Sprite woodSkin)
        {
            GameObject content = CreateImage("Content", panel, new Color(1f, 1f, 1f, 0.12f));
            SetRect(content.GetComponent<RectTransform>(), new Vector2(-165f, -24f), new Vector2(900f, 660f));

            GameObject banner = CreateImage("SkinBanner", content.transform, new Color(0.86f, 0.86f, 0.91f, 0.92f));
            SetRect(banner.GetComponent<RectTransform>(), new Vector2(0f, 210f), new Vector2(840f, 190f));
            AddOutline(banner, new Color(0.43f, 0.38f, 0.32f, 0.55f), new Vector2(2f, -2f));
            AddShadow(banner, new Color(0.18f, 0.14f, 0.10f, 0.17f), new Vector2(4f, -4f));

            GameObject bannerAccent = CreateImage("BannerAccent", banner.transform, new Color(0.72f, 0.82f, 0.90f, 0.42f));
            SetRect(bannerAccent.GetComponent<RectTransform>(), new Vector2(270f, 0f), new Vector2(290f, 184f));
            CreateText(template, banner.transform, "Headline", "球球皮肤收藏", new Vector2(-190f, 35f), new Vector2(430f, 60f), 36f, TextAlignmentOptions.Left, Ink);
            CreateText(template, banner.transform, "Subtitle", "换上喜欢的外观，开始新的挑战", new Vector2(-190f, -28f), new Vector2(430f, 42f), 21f, TextAlignmentOptions.Left, new Color(0.35f, 0.31f, 0.28f, 0.72f));

            GameObject bannerPreview = CreateImage("BannerPreview", banner.transform, Color.white);
            Image bannerImage = bannerPreview.GetComponent<Image>();
            bannerImage.sprite = woodSkin;
            bannerImage.preserveAspect = true;
            bannerImage.raycastTarget = false;
            SetRect(bannerPreview.GetComponent<RectTransform>(), new Vector2(270f, 0f), new Vector2(165f, 165f));

            CreateText(template, content.transform, "SectionTitle", "皮肤商品", new Vector2(-300f, 76f), new Vector2(260f, 46f), 29f, TextAlignmentOptions.Left, Ink);
            GameObject sectionLine = CreateImage("SectionLine", content.transform, new Color(0.53f, 0.45f, 0.35f, 0.3f));
            SetRect(sectionLine.GetComponent<RectTransform>(), new Vector2(170f, 64f), new Vector2(490f, 2f));

            GameObject cardArea = new GameObject("CardArea", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(EventTrigger));
            cardArea.transform.SetParent(content.transform, false);
            RectTransform viewport = cardArea.GetComponent<RectTransform>();
            SetRect(viewport, new Vector2(0f, -155f), new Vector2(760f, 370f));
            cardArea.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

            GameObject cardContent = new GameObject("CardContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardContent.transform.SetParent(cardArea.transform, false);
            RectTransform contentRect = cardContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(760f, 0f);
            HorizontalLayoutGroup horizontal = cardContent.GetComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(10, 10, 12, 12);
            horizontal.spacing = 12f;
            horizontal.childAlignment = TextAnchor.UpperLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            ScrollRect scrollRect = cardArea.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 0f;

            Button previous = CloneButton(template, content.transform, "PrevPageButton", "<");
            SetRect(previous.GetComponent<RectTransform>(), new Vector2(-410f, -155f), new Vector2(54f, 82f));
            SetButtonColors(previous, PaperMuted, new Color(1f, 0.87f, 0.58f, 1f));
            ConfigureText(previous.GetComponentInChildren<TMP_Text>(true), Vector2.zero, Vector2.zero, 38f, TextAlignmentOptions.Center, Ink, true);

            Button next = CloneButton(template, content.transform, "NextPageButton", ">");
            SetRect(next.GetComponent<RectTransform>(), new Vector2(410f, -155f), new Vector2(54f, 82f));
            SetButtonColors(next, PaperMuted, new Color(1f, 0.87f, 0.58f, 1f));
            ConfigureText(next.GetComponentInChildren<TMP_Text>(true), Vector2.zero, Vector2.zero, 38f, TextAlignmentOptions.Center, Ink, true);

            Button card = CloneButton(template, cardContent.transform, "CardTemplate", string.Empty);
            SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(176f, 290f));
            SetButtonColors(card, PaperLight, new Color(1f, 0.89f, 0.64f, 1f));
            AddOutline(card.gameObject, new Color(0.50f, 0.43f, 0.34f, 0.38f), new Vector2(1f, -1f));

            TMP_Text name = card.GetComponentInChildren<TMP_Text>(true);
            name.name = "Name";
            ConfigureText(name, new Vector2(0f, -92f), new Vector2(160f, 40f), 20f, TextAlignmentOptions.Center, Ink);

            GameObject preview = CreateImage("Preview", card.transform, Color.white);
            Image previewImage = preview.GetComponent<Image>();
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;
            SetRect(preview.GetComponent<RectTransform>(), new Vector2(0f, 42f), new Vector2(132f, 132f));

            GameObject stateBand = CreateImage("StateBand", card.transform, new Color(0.89f, 0.85f, 0.76f, 0.72f));
            stateBand.GetComponent<Image>().raycastTarget = false;
            SetRect(stateBand.GetComponent<RectTransform>(), new Vector2(0f, -128f), new Vector2(150f, 34f));
            CreateText(template, card.transform, "State", string.Empty, new Vector2(0f, -128f), new Vector2(150f, 34f), 16f, TextAlignmentOptions.Center, new Color(0.34f, 0.29f, 0.23f, 0.9f));
            card.gameObject.SetActive(false);
        }

        private static void BuildDetail(Button template, Transform panel)
        {
            GameObject detail = CreateImage("SkinDetail", panel, PaperLight);
            SetRect(detail.GetComponent<RectTransform>(), new Vector2(565f, -24f), new Vector2(430f, 660f));
            AddOutline(detail, new Color(0.48f, 0.41f, 0.32f, 0.42f), new Vector2(2f, -2f));
            AddShadow(detail, new Color(0.18f, 0.13f, 0.08f, 0.15f), new Vector2(4f, -4f));

            CreateText(template, detail.transform, "DetailTitle", "皮肤预览", new Vector2(0f, 272f), new Vector2(360f, 50f), 28f, TextAlignmentOptions.Center, Ink);
            GameObject divider = CreateImage("DetailDivider", detail.transform, Line);
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 235f), new Vector2(350f, 2f));

            GameObject previewPlate = CreateImage("PreviewPlate", detail.transform, new Color(0.91f, 0.90f, 0.86f, 0.8f));
            SetRect(previewPlate.GetComponent<RectTransform>(), new Vector2(0f, 75f), new Vector2(320f, 280f));
            AddOutline(previewPlate, new Color(0.50f, 0.44f, 0.35f, 0.28f), new Vector2(1f, -1f));

            GameObject detailPreview = CreateImage("DetailPreview", detail.transform, Color.white);
            Image previewImage = detailPreview.GetComponent<Image>();
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;
            SetRect(detailPreview.GetComponent<RectTransform>(), new Vector2(0f, 75f), new Vector2(230f, 230f));

            CreateText(template, detail.transform, "SelectedName", string.Empty, new Vector2(0f, -105f), new Vector2(360f, 48f), 27f, TextAlignmentOptions.Center, Ink);
            CreateText(template, detail.transform, "SelectedState", string.Empty, new Vector2(0f, -150f), new Vector2(360f, 36f), 20f, TextAlignmentOptions.Center, new Color(0.39f, 0.34f, 0.27f, 0.82f));

            Button action = CloneButton(template, detail.transform, "ActionButton", "装备");
            SetRect(action.GetComponent<RectTransform>(), new Vector2(0f, -220f), new Vector2(250f, 64f));
            SetButtonColors(action, Gold, new Color(1f, 0.84f, 0.46f, 1f));
            TMP_Text actionLabel = action.GetComponentInChildren<TMP_Text>(true);
            if (actionLabel != null)
                ConfigureText(actionLabel, Vector2.zero, Vector2.zero, 24f, TextAlignmentOptions.Center, Ink, true);

            CreateText(template, detail.transform, "Status", string.Empty, new Vector2(0f, -275f), new Vector2(370f, 38f), 17f, TextAlignmentOptions.Center, new Color(0.45f, 0.28f, 0.16f, 0.9f));
        }

        private static Button CloneButton(Button source, Transform parent, string name, string text)
        {
            Button button = UnityEngine.Object.Instantiate(source, parent);
            button.name = name;
            button.onClick.RemoveAllListeners();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
            return button;
        }

        private static TMP_Text CreateText(Button template, Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            TMP_Text text = UnityEngine.Object.Instantiate(template.GetComponentInChildren<TMP_Text>(true), parent);
            text.name = name;
            text.text = value;
            ConfigureText(text, position, size, fontSize, alignment, color);
            return text;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (parent != null)
                result.transform.SetParent(parent, false);
            result.GetComponent<Image>().color = color;
            return result;
        }

        private static void ConfigureText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color, bool stretch = false)
        {
            if (stretch)
                Stretch(text.rectTransform);
            else
                SetRect(text.rectTransform, position, size);
            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
        }

        private static void SetButtonColors(Button button, Color normal, Color highlighted)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = new Color(normal.r * 0.9f, normal.g * 0.9f, normal.b * 0.9f, normal.a);
            button.colors = colors;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
