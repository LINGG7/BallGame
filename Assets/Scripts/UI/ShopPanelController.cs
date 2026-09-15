using System.Collections;
using System.Collections.Generic;
using BallGame.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class ShopPanelController : MonoBehaviour
    {
        private const string ShopModalResourcePath = "UI/ShopModal";
        private static readonly Color CardNormal = new Color(0.98f, 0.96f, 0.90f, 1f);
        private static readonly Color CardSelected = new Color(1f, 0.86f, 0.50f, 1f);

        private bool effectsCategory;
        private bool unlockPending;
        private Button skinsTab;
        private Button effectsTab;
        private BallSkinCatalog ActiveCatalog => effectsCategory ? EffectSkinService.Catalog : BallSkinService.Catalog;
        private string CurrentSkinId => effectsCategory ? EffectSkinService.CurrentSkinId : BallSkinService.CurrentSkinId;
        private bool IsUnlocked(string id) => effectsCategory ? EffectSkinService.IsUnlocked(id) : BallSkinService.IsUnlocked(id);
        private bool EquipSkin(string id) => effectsCategory ? EffectSkinService.EquipSkin(id) : BallSkinService.EquipSkin(id);

        private Button shopButton;
        private GameObject modalRoot;
        private Transform cardArea;
        private ScrollRect cardScrollRect;
        private RectTransform cardContent;
        private Button cardTemplate;
        private Button previousPageButton;
        private Button nextPageButton;
        private Image selectedPreview;
        private TMP_Text selectedPreviewName;
        private TMP_Text selectedState;
        private Button actionButton;
        private TMP_Text actionLabel;
        private TMP_Text statusText;

        private readonly List<Button> cardButtons = new List<Button>();
        private readonly List<TMP_Text> cardStates = new List<TMP_Text>();
        private readonly List<int> cardSkinIndices = new List<int>();
        private int selectedIndex;
        private int currentPage;
        private int pageCount;
        private bool pageAnimating;
        private bool pageDragging;
        private float dragStartContentX;
        private Coroutine pageAnimationCoroutine;

        private const int CardsPerPage = 4;
        private const float PageAnimationDuration = 0.2f;
        private const float SwipeThreshold = 188f;

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        private void Awake()
        {
            ResolveEntryAndModal();
            Hide();
        }

        public void Show()
        {
            ResolveEntryAndModal();
            if (modalRoot == null)
                return;

            if (pageAnimating)
            {
                StopPageAnimation();
                pageAnimating = false;
            }
            selectedIndex = FindCurrentIndex();
            currentPage = selectedIndex / CardsPerPage;
            Refresh();
            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            SetPageImmediate(currentPage);
        }

        public void Hide()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void ResolveEntryAndModal()
        {
            if (shopButton == null)
            {
                Transform entry = transform.Find("ShopButton");
                shopButton = entry != null ? entry.GetComponent<Button>() : null;
                if (shopButton != null)
                {
                    shopButton.onClick.RemoveListener(Show);
                    shopButton.onClick.AddListener(Show);
                }
            }

            if (modalRoot != null)
                return;

            GameObject modalPrefab = Resources.Load<GameObject>(ShopModalResourcePath);
            if (modalPrefab == null)
            {
                Debug.LogWarning("Missing shop modal prefab: " + ShopModalResourcePath, this);
                return;
            }

            modalRoot = Instantiate(modalPrefab, transform);
            modalRoot.name = "ShopModal";
            Stretch(modalRoot.GetComponent<RectTransform>());

            Button backdrop = modalRoot.GetComponent<Button>();
            if (backdrop != null)
                backdrop.onClick.AddListener(Hide);

            Transform panel = modalRoot.transform.Find("Panel");
            BindCloseButton(panel);
            skinsTab = panel != null ? panel.Find("CategorySidebar/SkinsTab")?.GetComponent<Button>() : null;
            effectsTab = panel != null ? panel.Find("CategorySidebar/EffectsTab")?.GetComponent<Button>() : null;
            if (skinsTab != null) skinsTab.onClick.AddListener(() => SwitchCategory(false));
            if (effectsTab != null) effectsTab.onClick.AddListener(() => SwitchCategory(true));

            cardArea = panel != null ? panel.Find("Content/CardArea") : null;
            if (cardArea == null && panel != null)
                cardArea = panel.Find("CardArea");

            cardScrollRect = cardArea != null ? cardArea.GetComponent<ScrollRect>() : null;
            cardContent = cardArea != null ? cardArea.Find("CardContent")?.GetComponent<RectTransform>() : null;
            BindCardDragEvents();
            cardTemplate = cardContent != null
                ? cardContent.Find("CardTemplate")?.GetComponent<Button>()
                : null;
            if (cardTemplate == null && panel != null)
                cardTemplate = panel.Find("CardTemplate")?.GetComponent<Button>();

            previousPageButton = panel != null ? panel.Find("Content/PrevPageButton")?.GetComponent<Button>() : null;
            nextPageButton = panel != null ? panel.Find("Content/NextPageButton")?.GetComponent<Button>() : null;
            if (previousPageButton != null)
                previousPageButton.onClick.AddListener(() => NavigatePage(-1));
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(() => NavigatePage(1));

            Transform detail = panel != null ? panel.Find("SkinDetail") : null;
            selectedPreview = detail != null ? detail.Find("DetailPreview")?.GetComponent<Image>() : null;
            selectedPreviewName = detail != null ? detail.Find("SelectedName")?.GetComponent<TMP_Text>() : null;
            selectedState = detail != null ? detail.Find("SelectedState")?.GetComponent<TMP_Text>() : null;
            actionButton = detail != null ? detail.Find("ActionButton")?.GetComponent<Button>() : null;
            actionLabel = actionButton != null ? actionButton.GetComponentInChildren<TMP_Text>(true) : null;
            statusText = detail != null ? detail.Find("Status")?.GetComponent<TMP_Text>() : null;

            if (actionButton != null)
                actionButton.onClick.AddListener(ActivateSelected);

            BuildCards();
            modalRoot.SetActive(false);
        }

        private void SwitchCategory(bool effects)
        {
            if (effectsCategory == effects) return;
            StopPageAnimation();
            pageAnimating = false;
            pageDragging = false;
            if (cardScrollRect != null) cardScrollRect.StopMovement();
            effectsCategory = effects;
            selectedIndex = FindCurrentIndex();
            currentPage = selectedIndex / CardsPerPage;
            BuildCards();
            SetStatus(string.Empty);
            Refresh();
            Canvas.ForceUpdateCanvases();
            SetPageImmediate(currentPage);
        }

        private void RefreshCategory()
        {
            SetCategoryTab(skinsTab, !effectsCategory);
            SetCategoryTab(effectsTab, effectsCategory);
            Transform panel = modalRoot != null ? modalRoot.transform.Find("Panel") : null;
            if (panel == null) return;
            var hint = panel.Find("CategorySidebar/CategoryHint") as RectTransform;
            if (hint != null) hint.anchoredPosition = new Vector2(hint.anchoredPosition.x, effectsCategory ? 154f : 244f);
            SetCategoryText(panel, "Content/SkinBanner/Headline", effectsCategory ? "特效皮肤收藏" : "球球皮肤收藏");
            SetCategoryText(panel, "Content/SkinBanner/Subtitle", effectsCategory ? "基础光带免费，主题特效看广告永久解锁" : "换上喜欢的外观，开始新的挑战");
            SetCategoryText(panel, "Content/SectionTitle", effectsCategory ? "特效皮肤" : "皮肤商品");
            SetCategoryText(panel, "SkinDetail/DetailTitle", effectsCategory ? "特效预览" : "皮肤预览");
            var banner = panel.Find("Content/SkinBanner/BannerPreview")?.GetComponent<Image>();
            if (banner != null && ActiveCatalog != null && ActiveCatalog.Skins.Count > 0)
            {
                banner.sprite = ActiveCatalog.Skins[Mathf.Clamp(selectedIndex, 0, ActiveCatalog.Skins.Count - 1)].previewSprite;
                banner.enabled = banner.sprite != null;
            }
        }

        private static void SetCategoryTab(Button tab, bool selected)
        {
            if (tab == null) return;
            tab.interactable = true;
            var colors = tab.colors;
            colors.normalColor = selected ? CardSelected : CardNormal;
            colors.selectedColor = colors.normalColor;
            tab.colors = colors;
        }

        private static void SetCategoryText(Transform panel, string path, string value)
        {
            var label = panel.Find(path)?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = value;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
            }
        }

        private void BindCloseButton(Transform panel)
        {
            Button backButton = panel != null ? panel.Find("BackButton")?.GetComponent<Button>() : null;
            if (backButton != null)
                backButton.onClick.AddListener(Hide);
        }

        private void BuildCards()
        {
            foreach (var oldCard in cardButtons)
            {
                if (oldCard == null) continue;
                oldCard.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(oldCard.gameObject);
                else DestroyImmediate(oldCard.gameObject);
            }
            cardButtons.Clear();
            cardStates.Clear();
            cardSkinIndices.Clear();
            pageCount = 1;
            BallSkinCatalog catalog = ActiveCatalog;
            if (cardArea == null || cardTemplate == null || catalog == null)
                return;

            cardTemplate.gameObject.SetActive(false);
            for (int i = 0; i < catalog.Skins.Count; i++)
            {
                BallSkinEntry skin = catalog.Skins[i];
                if (skin == null)
                    continue;

                Button card = Instantiate(cardTemplate, cardContent != null ? cardContent : cardArea);
                card.name = "SkinCard_" + skin.id;
                card.gameObject.SetActive(true);

                TMP_Text nameLabel = card.transform.Find("Name")?.GetComponent<TMP_Text>();
                TMP_Text stateLabel = card.transform.Find("State")?.GetComponent<TMP_Text>();
                Image preview = card.transform.Find("Preview")?.GetComponent<Image>();
                if (nameLabel != null)
                    nameLabel.text = skin.displayName;
                if (preview != null)
                {
                    preview.sprite = skin.previewSprite;
                    preview.enabled = skin.previewSprite != null;
                }

                int capturedIndex = i;
                card.onClick.RemoveAllListeners();
                card.onClick.AddListener(() => SelectCard(capturedIndex));
                cardButtons.Add(card);
                cardStates.Add(stateLabel);
                cardSkinIndices.Add(i);
            }

            pageCount = CalculatePageCount(cardButtons.Count);
            if (cardContent != null && cardArea != null)
            {
                float viewportWidth = cardArea.GetComponent<RectTransform>().rect.width;
                cardContent.sizeDelta = new Vector2(viewportWidth * pageCount, cardContent.sizeDelta.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardContent);
            }
        }

        private void SelectCard(int index)
        {
            selectedIndex = index;
            if (statusText != null)
                statusText.text = string.Empty;
            Refresh();
        }

        private void ActivateSelected()
        {
            BallSkinCatalog catalog = ActiveCatalog;
            if (catalog == null || selectedIndex < 0 || selectedIndex >= catalog.Skins.Count)
                return;

            BallSkinEntry skin = catalog.Skins[selectedIndex];
            if (!IsUnlocked(skin.id))
            {
                if (unlockPending) return;
                if (effectsCategory && BallSkinService.RewardedAdService == null)
                {
                    SetStatus("广告暂不可用，请稍后重试");
                    return;
                }
                bool requestedEffects = effectsCategory;
                unlockPending = true;
                SetStatus("正在准备广告...");
                Refresh();
                System.Action<bool> completed = success =>
                {
                    if (this == null) return;
                    unlockPending = false;
                    if (effectsCategory == requestedEffects)
                        SetStatus(success ? "皮肤已永久解锁，可以装备" : "未完成广告，请重试");
                    Refresh();
                };
                if (requestedEffects) EffectSkinService.TryUnlock(skin.id, completed);
                else BallSkinService.TryUnlock(skin.id, completed);
                return;
            }

            bool equipped = EquipSkin(skin.id);
            SetStatus(equipped ? "已装备：" + skin.displayName : "该皮肤尚未解锁");
            Refresh();
        }

        private void Refresh()
        {
            RefreshCategory();
            BallSkinCatalog catalog = ActiveCatalog;
            if (catalog == null || catalog.Skins.Count == 0)
            {
                SetStatus("暂无可用皮肤");
                if (actionButton != null)
                    actionButton.interactable = false;
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, catalog.Skins.Count - 1);
            string currentSkinId = CurrentSkinId;
            for (int i = 0; i < cardButtons.Count; i++)
            {
                int skinIndex = cardSkinIndices[i];
                BallSkinEntry skin = catalog.Skins[skinIndex];
                bool selected = selectedIndex == skinIndex;
                bool equipped = currentSkinId == skin.id;
                bool unlocked = IsUnlocked(skin.id);

                ColorBlock colors = cardButtons[i].colors;
                colors.normalColor = selected ? CardSelected : CardNormal;
                colors.selectedColor = CardSelected;
                cardButtons[i].colors = colors;

                TMP_Text state = cardStates[i];
                if (state != null)
                    state.text = equipped ? "已装备" : (unlocked ? "已拥有" : "尚未解锁");
            }

            BallSkinEntry selectedSkin = catalog.Skins[selectedIndex];
            bool selectedUnlocked = IsUnlocked(selectedSkin.id);
            bool selectedEquipped = currentSkinId == selectedSkin.id;

            if (selectedPreview != null)
            {
                selectedPreview.sprite = selectedSkin.previewSprite;
                selectedPreview.enabled = selectedSkin.previewSprite != null;
            }
            if (selectedPreviewName != null)
                selectedPreviewName.text = selectedSkin.displayName;
            if (selectedState != null)
                selectedState.text = selectedEquipped ? "当前装备" : (selectedUnlocked ? "已拥有，可装备" : "尚未解锁");
            if (actionButton != null)
                actionButton.interactable = !selectedEquipped && !unlockPending && (!effectsCategory || !EffectSkinService.IsUnlockPending);
            if (actionLabel != null)
                actionLabel.text = unlockPending ? "等待广告完成" : selectedEquipped
                    ? "已装备"
                    : (selectedUnlocked ? "装备" : (effectsCategory || BallSkinService.RewardedAdService != null ? "观看广告解锁" : "解锁"));
            UpdatePageButtons();
        }

        private void NavigatePage(int direction)
        {
            if (pageAnimating || pageCount <= 1)
                return;

            int targetPage = Mathf.Clamp(currentPage + direction, 0, pageCount - 1);
            if (targetPage == currentPage)
                return;

            StartPageAnimation(targetPage);
        }

        private static int CalculatePageCount(int cardCount)
        {
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0, cardCount) / (float)CardsPerPage));
        }

        private void BindCardDragEvents()
        {
            if (cardArea == null)
                return;

            EventTrigger trigger = cardArea.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = cardArea.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            AddDragTrigger(trigger, EventTriggerType.BeginDrag, HandleBeginDrag);
            AddDragTrigger(trigger, EventTriggerType.EndDrag, HandleEndDrag);
        }

        private static void AddDragTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private void HandleBeginDrag(BaseEventData eventData)
        {
            if (pageCount <= 1 || cardContent == null)
                return;

            StopPageAnimation();
            pageAnimating = false;
            pageDragging = true;
            dragStartContentX = cardContent.anchoredPosition.x;
            if (cardScrollRect != null)
                cardScrollRect.StopMovement();
            UpdatePageButtons();
        }

        private void HandleEndDrag(BaseEventData eventData)
        {
            if (!pageDragging || cardContent == null)
                return;

            pageDragging = false;
            if (cardScrollRect != null)
                cardScrollRect.StopMovement();
            float dragDistance = cardContent.anchoredPosition.x - dragStartContentX;
            int targetPage = ResolveSwipeTargetPage(currentPage, pageCount, dragDistance, SwipeThreshold);
            StartPageAnimation(targetPage);
        }

        private static int ResolveSwipeTargetPage(int page, int totalPages, float dragDistance, float threshold)
        {
            int maxPage = Mathf.Max(0, totalPages - 1);
            int clampedPage = Mathf.Clamp(page, 0, maxPage);
            if (Mathf.Abs(dragDistance) <= Mathf.Max(0f, threshold))
                return clampedPage;

            int direction = dragDistance < 0f ? 1 : -1;
            return Mathf.Clamp(clampedPage + direction, 0, maxPage);
        }

        private void StartPageAnimation(int targetPage)
        {
            StopPageAnimation();
            pageAnimationCoroutine = StartCoroutine(AnimateToPage(targetPage));
        }

        private void StopPageAnimation()
        {
            if (pageAnimationCoroutine == null)
                return;
            StopCoroutine(pageAnimationCoroutine);
            pageAnimationCoroutine = null;
        }

        private IEnumerator AnimateToPage(int targetPage)
        {
            pageAnimating = true;
            UpdatePageButtons();
            float start = cardScrollRect != null ? cardScrollRect.horizontalNormalizedPosition : 0f;
            float end = pageCount <= 1 ? 0f : targetPage / (float)(pageCount - 1);
            float elapsed = 0f;
            while (elapsed < PageAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / PageAnimationDuration);
                t = t * t * (3f - 2f * t);
                if (cardScrollRect != null)
                    cardScrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, end, t);
                yield return null;
            }

            currentPage = targetPage;
            SetPageImmediate(currentPage);
            pageAnimating = false;
            pageAnimationCoroutine = null;
            UpdatePageButtons();
        }

        private void SetPageImmediate(int page)
        {
            currentPage = Mathf.Clamp(page, 0, Mathf.Max(0, pageCount - 1));
            if (cardScrollRect != null)
                cardScrollRect.horizontalNormalizedPosition = pageCount <= 1 ? 0f : currentPage / (float)(pageCount - 1);
            UpdatePageButtons();
        }

        private void UpdatePageButtons()
        {
            bool canNavigate = !pageAnimating && !pageDragging && pageCount > 1;
            if (previousPageButton != null)
                previousPageButton.interactable = canNavigate && currentPage > 0;
            if (nextPageButton != null)
                nextPageButton.interactable = canNavigate && currentPage < pageCount - 1;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private int FindCurrentIndex()
        {
            BallSkinCatalog catalog = ActiveCatalog;
            if (catalog == null)
                return 0;

            for (int i = 0; i < catalog.Skins.Count; i++)
            {
                if (catalog.Skins[i] != null && catalog.Skins[i].id == CurrentSkinId)
                    return i;
            }
            return 0;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
