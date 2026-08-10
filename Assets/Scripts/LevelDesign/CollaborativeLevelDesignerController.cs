using System;
using System.Collections.Generic;
using System.Globalization;
using BallGame.Core;
using BallGame.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.LevelDesign
{
    public sealed class CollaborativeLevelDesignerController : MonoBehaviour
    {
        private const string RegistryResourcePath = "LevelDesigner/CollaborativeLevelAssetRegistry";
        private const string LevelSettingsPrefabResourcePath = "LevelDesigner/LevelSettingsPanel";
        private const string BackgroundResourcePath = "Prefabs/WorldBackground";
        private const float DefaultCameraSize = 7f;
        private const float CanvasPadding = 2f;
        private const float MinimumCanvasWidth = 24f;
        private const float MinimumCanvasHeight = 14f;
        private const int DefaultClickSortingOrder = 5;
        private const float PaletteHeight = 120f;
        private const float InspectorWidth = 350f;
        private const float PaletteCellWidth = 230f;
        private const float PaletteCellHeight = 48f;
        private const float PaletteCellSpacing = 10f;
        private const float PlacedHierarchyWidth = 270f;
        private const float TopBarHeight = 68f;
        private readonly Stack<string> undo = new Stack<string>();
        private readonly Stack<string> redo = new Stack<string>();
        private readonly List<SpriteRenderer> selectedRenderers = new List<SpriteRenderer>();
        private readonly List<Color> selectedRendererColors = new List<Color>();
        private readonly List<LineRenderer> originMarkerLines = new List<LineRenderer>();
        private readonly Dictionary<string, TMP_InputField> levelStockInputs = new Dictionary<string, TMP_InputField>(StringComparer.Ordinal);
        private readonly Dictionary<CollaborativeLevelObject, Button> hierarchyRows = new Dictionary<CollaborativeLevelObject, Button>();
        private readonly Dictionary<Button, ColorBlock> hierarchyRowColors = new Dictionary<Button, ColorBlock>();
        private CollaborativeLevelAssetRegistry registry;
        private CollaborativeLevelDocument document = new CollaborativeLevelDocument();
        private Camera worldCamera;
        private Transform worldRoot;
        private Transform splineHandleRoot;
        private Transform originMarkerRoot;
        private Material originMarkerMaterial;
        private CollaborativeLevelObject selected;
        private CollaborativeSplineHandle draggedSplineHandle;
        private TMP_Text statusText;
        private TMP_Text selectionText;
        private TMP_InputField titleInput;
        private TMP_InputField authorInput;
        private TMP_InputField xInput;
        private TMP_InputField yInput;
        private TMP_InputField rotationInput;
        private TMP_InputField sizeXInput;
        private TMP_InputField sizeYInput;
        private TMP_InputField splineWidthInput;
        private TMP_Dropdown trackPartDropdown;
        private GameObject trackIntersectionInfo;
        private TMP_InputField trackEntryXInput;
        private TMP_InputField trackEntryYInput;
        private TMP_InputField trackEntryRotationInput;
        private RectTransform hierarchyContent;
        private Button hierarchyItemTemplate;
        private ScrollRect hierarchyScrollRect;
        private GameObject levelSettingsPanel;
        private TMP_Dropdown levelModeDropdown;
        private TMP_Text levelSettingsHint;
        private TMP_Text levelSettingsValidation;
        private bool dragging;
        private bool canvasPanning;
        private Vector2 dragOffset;
        private Vector2 lastPanScreenPosition;
        private bool simulating;
        private string editSnapshot;
        private int nextObjectId = 1;

        private void Awake()
        {
            registry = Resources.Load<CollaborativeLevelAssetRegistry>(RegistryResourcePath);
            BuildWorld();
            BuildUi();
            if (registry == null)
                SetStatus("Missing asset registry. Create it from the Unity Level Design menu.");
            else
                SetStatus("Add a Ball and Goal, then design the level.");
        }

        private void Update()
        {
            if (levelSettingsPanel != null && levelSettingsPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseLevelSettings();
                return;
            }
            HandleCamera();
            if (simulating)
            {
                HandleSimulationCanvasPan();
                return;
            }
            HandleWorldPointer();
            if (Input.GetKeyDown(KeyCode.Delete)) DeleteSelected();
            if (Input.GetKeyDown(KeyCode.Q)) RotateSelected(15f);
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R)) RotateSelected(-15f);
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) Undo();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y)) Redo();
        }

        private void LateUpdate()
        {
            if (originMarkerRoot == null || worldCamera == null) return;
            float scale = Mathf.Max(0.01f, worldCamera.orthographicSize / DefaultCameraSize);
            originMarkerRoot.localScale = new Vector3(scale, scale, 1f);
            for (int i = 0; i < originMarkerLines.Count; i++)
            {
                if (originMarkerLines[i] == null) continue;
                originMarkerLines[i].startWidth = 0.035f * scale;
                originMarkerLines[i].endWidth = 0.035f * scale;
            }
        }

        private void OnDestroy()
        {
            if (originMarkerMaterial != null) Destroy(originMarkerMaterial);
        }

        public void ImportJson(string json)
        {
            try
            {
                CollaborativeLevelDocument imported = CollaborativeLevelDocument.FromJson(json);
                List<string> errors = CollaborativeLevelValidator.Validate(imported, registry);
                if (errors.Count > 0)
                {
                    SetStatus("Import rejected: " + string.Join(" | ", errors));
                    return;
                }
                PushUndo();
                LoadDocument(imported);
                SetStatus("Imported " + imported.title);
            }
            catch (Exception exception)
            {
                SetStatus("Import failed: " + exception.Message);
            }
        }

        public void ExportJson()
        {
            CollaborativeLevelDocument exported = CaptureDocument();
            List<string> errors = CollaborativeLevelValidator.Validate(exported, registry);
            if (errors.Count > 0)
            {
                SetStatus("Export blocked: " + string.Join(" | ", errors));
                return;
            }
            string filename = SanitizeFilename(exported.title) + ".json";
            CollaborativeWebGLFileBridge.Download(filename, exported.ToJson());
            SetStatus("Exported " + filename);
        }

        public void RequestImport()
        {
            CollaborativeWebGLFileBridge.PickJson(gameObject.name, nameof(ImportJson));
        }

        private void BuildWorld()
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                worldCamera = cameraObject.GetComponent<Camera>();
            }
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = DefaultCameraSize;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            worldCamera.backgroundColor = new Color(0.88f, 0.92f, 0.94f, 1f);
            GameObject root = new GameObject("Designed Objects");
            worldRoot = root.transform;
            splineHandleRoot = new GameObject("Spline Handles").transform;
            GameObject backgroundPrefab = Resources.Load<GameObject>(BackgroundResourcePath);
            if (backgroundPrefab != null)
            {
                GameObject background = Instantiate(backgroundPrefab);
                background.name = "Designer Infinite Background";
                InfiniteTiledBackground2D tiledBackground = background.GetComponent<InfiniteTiledBackground2D>();
                if (tiledBackground != null) tiledBackground.SetTargetCamera(worldCamera);
            }
            BuildOriginMarker();
        }

        private void BuildOriginMarker()
        {
            originMarkerRoot = new GameObject("Designer Origin Marker").transform;
            originMarkerRoot.position = Vector3.zero;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) originMarkerMaterial = new Material(shader) { name = "Designer Origin Marker Material" };
            CreateOriginAxis("X Axis", new Vector3(-0.45f, 0f), new Vector3(0.45f, 0f), new Color(0.86f, 0.25f, 0.22f, 0.9f));
            CreateOriginAxis("Y Axis", new Vector3(0f, -0.45f), new Vector3(0f, 0.45f), new Color(0.22f, 0.65f, 0.35f, 0.9f));

            GameObject labelObject = new GameObject("Origin Label", typeof(TextMeshPro));
            labelObject.transform.SetParent(originMarkerRoot, false);
            labelObject.transform.localPosition = new Vector3(0.4f, 0.2f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.25f;
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = "(0,0)";
            label.fontSize = 10f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            label.renderer.sortingOrder = 1001;
        }

        private void CreateOriginAxis(string name, Vector3 start, Vector3 end, Color color)
        {
            GameObject axisObject = new GameObject(name, typeof(LineRenderer));
            axisObject.transform.SetParent(originMarkerRoot, false);
            LineRenderer line = axisObject.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.startColor = color;
            line.endColor = color;
            line.sharedMaterial = originMarkerMaterial;
            line.sortingOrder = 1000;
            originMarkerLines.Add(line);
        }

        private void BuildUi()
        {
            EnsureEventSystem();
            if (TryBindPrefabUi()) return;

            GameObject canvasObject = new GameObject("Designer UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform top = CreatePanel(canvas.transform, "TopBar", new Color(0.12f, 0.14f, 0.16f, 0.96f));
            Anchor(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 68f), Vector2.zero);
            AddButton(top, "New", new Vector2(70f, 0f), NewDocument);
            AddButton(top, "Import", new Vector2(200f, 0f), RequestImport);
            AddButton(top, "Export", new Vector2(330f, 0f), ExportJson);
            AddButton(top, "Undo", new Vector2(460f, 0f), Undo);
            AddButton(top, "Redo", new Vector2(590f, 0f), Redo);
            AddButton(top, "Reset", new Vector2(720f, 0f), ResetView);
            AddButton(top, "Test", new Vector2(850f, 0f), ToggleSimulation);
            Button settingsButton = AddButton(top, "Level Settings", new Vector2(1010f, 0f), OpenLevelSettings);
            settingsButton.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 46f);
            statusText = AddText(top, "Status", new Vector2(1220f, 0f), new Vector2(880f, 50f), 20f, TextAlignmentOptions.MidlineRight);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.offsetMin = new Vector2(1100f, 8f);
            statusRect.offsetMax = new Vector2(-20f, -8f);

            RectTransform palette = CreatePanel(canvas.transform, "Palette", new Color(0.96f, 0.95f, 0.90f, 0.97f));
            ConfigureBottomPaletteRect(palette);
            AddText(palette, "Title", new Vector2(65f, 60f), new Vector2(110f, 44f), 26f, TextAlignmentOptions.Center);
            palette.Find("Title").GetComponent<TMP_Text>().text = "OBJECTS";
            RectTransform paletteContent = CreatePaletteScrollView(palette);
            if (registry != null)
            {
                for (int i = 0; i < registry.Entries.Count; i++)
                {
                    CollaborativeLevelAssetEntry captured = registry.Entries[i];
                    Button button = AddButton(paletteContent, captured.assetKey, Vector2.zero, () => AddObject(captured));
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(230f, 48f);
                }
            }

            CreatePlacedHierarchyUi(canvas.transform, palette, out hierarchyContent, out hierarchyItemTemplate, out hierarchyScrollRect);
            RebuildHierarchy();

            RectTransform inspector = CreatePanel(canvas.transform, "Inspector", new Color(0.96f, 0.95f, 0.90f, 0.97f));
            Anchor(inspector, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(300f, -68f), new Vector2(0f, -34f));
            selectionText = AddText(inspector, "Selection", new Vector2(150f, -55f), new Vector2(260f, 90f), 22f, TextAlignmentOptions.TopLeft);
            AddButton(inspector, "Rotate -", new Vector2(80f, -110f), () => RotateSelected(-15f));
            AddButton(inspector, "Rotate +", new Vector2(220f, -110f), () => RotateSelected(15f));
            AddButton(inspector, "Duplicate", new Vector2(80f, -165f), DuplicateSelected);
            AddButton(inspector, "Delete", new Vector2(220f, -165f), DeleteSelected);
            AddButton(inspector, "Add Point", new Vector2(80f, -220f), AddSplinePoint);
            AddButton(inspector, "Remove Point", new Vector2(220f, -220f), RemoveSplinePoint);
            titleInput = AddInput(inspector, "Level title", new Vector2(150f, -285f), value => document.title = value);
            ConfigureLevelTitleInput();
            authorInput = AddInput(inspector, "Author", new Vector2(150f, -335f), value => document.author = value);
            xInput = AddInput(inspector, "X", new Vector2(80f, -390f), _ => ApplyInspectorTransform());
            yInput = AddInput(inspector, "Y", new Vector2(220f, -390f), _ => ApplyInspectorTransform());
            rotationInput = AddInput(inspector, "Rotation", new Vector2(80f, -440f), _ => ApplyInspectorTransform());
            AddText(inspector, "Size X Label", new Vector2(55f, -495f), new Vector2(80f, 30f), 15f, TextAlignmentOptions.MidlineLeft).text = "Size X";
            sizeXInput = AddInput(inspector, "Size X", new Vector2(190f, -495f), _ => ApplyInspectorTransform());
            AddText(inspector, "Size Y Label", new Vector2(55f, -550f), new Vector2(80f, 30f), 15f, TextAlignmentOptions.MidlineLeft).text = "Size Y";
            sizeYInput = AddInput(inspector, "Size Y", new Vector2(190f, -550f), _ => ApplyInspectorTransform());
            splineWidthInput = AddInput(inspector, "Path width", new Vector2(150f, -610f), _ => ApplyInspectorTransform());
            AddButton(inspector, "Closed", new Vector2(80f, -665f), ToggleSplineClosed);
            AddButton(inspector, "Collision", new Vector2(220f, -665f), ToggleSplineCollision);
            trackPartDropdown = AddDropdown(inspector, "Track Part", new Vector2(150f, -720f), new Vector2(260f, 44f),
                new[] { "Upper Curve", "Lower Curve", "Upper Entry A", "Lower Entry A" });
            trackPartDropdown.onValueChanged.AddListener(_ => RefreshTrackPartSelection());
            trackEntryXInput = AddInput(inspector, "Entry X", new Vector2(80f, -775f), _ => ApplyInspectorTransform());
            trackEntryYInput = AddInput(inspector, "Entry Y", new Vector2(220f, -775f), _ => ApplyInspectorTransform());
            trackEntryRotationInput = AddInput(inspector, "Entry Rotation", new Vector2(150f, -830f), _ => ApplyInspectorTransform());
            titleInput.SetTextWithoutNotify(document.title);
            authorInput.SetTextWithoutNotify(document.author);
            RefreshSelection();
            BuildLevelSettingsPanel(canvas.transform);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private bool TryBindPrefabUi()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            Transform root = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].name == "DesignerUI")
                {
                    root = canvases[i].transform;
                    break;
                }
            }
            if (root == null) return false;

            RectTransform paletteRect = FindComponent<RectTransform>(root, "Palette");
            RectTransform hierarchyPanel = FindComponent<RectTransform>(root, "PlacedHierarchy");
            if (hierarchyPanel == null)
                CreatePlacedHierarchyUi(root, paletteRect, out hierarchyContent, out hierarchyItemTemplate, out hierarchyScrollRect);
            else
            {
                ConfigurePlacedHierarchyRect(hierarchyPanel, paletteRect);
                hierarchyContent = FindComponent<RectTransform>(hierarchyPanel, "PlacedHierarchyContent");
                hierarchyItemTemplate = FindComponent<Button>(hierarchyPanel, "HierarchyItemTemplate");
                hierarchyScrollRect = FindComponent<ScrollRect>(hierarchyPanel, "PlacedHierarchyScrollView");
            }

            statusText = FindComponent<TMP_Text>(root, "Status");
            ConfigureStatusRect(statusText);
            selectionText = FindComponent<TMP_Text>(root, "Selection");
            titleInput = BindInput(root, "Level titleInput", value => document.title = value);
            ConfigureLevelTitleInput();
            authorInput = BindInput(root, "AuthorInput", value => document.author = value);
            xInput = BindInput(root, "XInput", _ => ApplyInspectorTransform());
            yInput = BindInput(root, "YInput", _ => ApplyInspectorTransform());
            rotationInput = BindInput(root, "RotationInput", _ => ApplyInspectorTransform());
            sizeXInput = BindInput(root, "Size XInput", _ => ApplyInspectorTransform());
            sizeYInput = BindInput(root, "Size YInput", _ => ApplyInspectorTransform());
            splineWidthInput = BindInput(root, "Path widthInput", _ => ApplyInspectorTransform());
            trackPartDropdown = FindComponent<TMP_Dropdown>(root, "Track PartDropdown");
            RectTransform trackInfoRect = FindComponent<RectTransform>(root, "TrackIntersectionInfo");
            trackIntersectionInfo = trackInfoRect != null ? trackInfoRect.gameObject : null;
            if (trackPartDropdown != null) trackPartDropdown.onValueChanged.AddListener(_ => RefreshTrackPartSelection());
            trackEntryXInput = BindInput(root, "Entry XInput", _ => ApplyInspectorTransform());
            trackEntryYInput = BindInput(root, "Entry YInput", _ => ApplyInspectorTransform());
            trackEntryRotationInput = BindInput(root, "Entry RotationInput", _ => ApplyInspectorTransform());

            BindButton(root, "NewButton", NewDocument);
            BindButton(root, "ImportButton", RequestImport);
            BindButton(root, "ExportButton", ExportJson);
            BindButton(root, "UndoButton", Undo);
            BindButton(root, "RedoButton", Redo);
            BindButton(root, "ResetButton", ResetView);
            BindButton(root, "TestButton", ToggleSimulation);
            BindButton(root, "Level SettingsButton", OpenLevelSettings);
            BindButton(root, "Rotate -Button", () => RotateSelected(-15f));
            BindButton(root, "Rotate +Button", () => RotateSelected(15f));
            BindButton(root, "DuplicateButton", DuplicateSelected);
            BindButton(root, "DeleteButton", DeleteSelected);
            BindButton(root, "Add PointButton", AddSplinePoint);
            BindButton(root, "Remove PointButton", RemoveSplinePoint);
            BindButton(root, "ClosedButton", ToggleSplineClosed);
            BindButton(root, "CollisionButton", ToggleSplineCollision);

            if (registry != null)
            {
                for (int i = 0; i < registry.Entries.Count; i++)
                {
                    CollaborativeLevelAssetEntry captured = registry.Entries[i];
                    BindButton(root, captured.assetKey + "Button", () => AddObject(captured));
                }
            }

            titleInput.SetTextWithoutNotify(document.title);
            authorInput.SetTextWithoutNotify(document.author);
            RefreshSelection();
            RebuildHierarchy();
            BuildLevelSettingsPanel(root);
            return true;
        }

        private static void ConfigureStatusRect(TMP_Text status)
        {
            if (status == null) return;
            RectTransform rect = status.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(1100f, 8f);
            rect.offsetMax = new Vector2(-20f, -8f);
        }

        private void BuildLevelSettingsPanel(Transform canvasRoot)
        {
            if (levelSettingsPanel != null || canvasRoot == null) return;

            GameObject prefab = Resources.Load<GameObject>(LevelSettingsPrefabResourcePath);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, canvasRoot, false);
                instance.name = "LevelSettingsPanel";
                BindLevelSettingsPanel(instance.transform);
                return;
            }

            RectTransform overlay = CreatePanel(canvasRoot, "LevelSettingsPanel", new Color(0f, 0f, 0f, 0.58f));
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.SetAsLastSibling();

            RectTransform dialog = CreatePanel(overlay, "LevelSettingsDialog", new Color(0.12f, 0.14f, 0.16f, 0.99f));
            Anchor(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640f, 760f), Vector2.zero);
            AddText(dialog, "LevelSettingsTitle", new Vector2(320f, -38f), new Vector2(580f, 48f), 28f, TextAlignmentOptions.Center).text = "LEVEL SETTINGS";
            AddText(dialog, "ModeLabel", new Vector2(105f, -98f), new Vector2(150f, 38f), 19f, TextAlignmentOptions.MidlineLeft).text = "Mode";
            AddDropdown(dialog, "LevelMode", new Vector2(390f, -98f), new Vector2(320f, 44f), new[] { "Normal", "LowestCost" });
            AddText(dialog, "ModeHint", new Vector2(320f, -145f), new Vector2(570f, 46f), 16f, TextAlignmentOptions.Center);

            int row = 0;
            if (registry != null)
            {
                for (int i = 0; i < registry.Entries.Count; i++)
                {
                    CollaborativeLevelAssetEntry entry = registry.Entries[i];
                    if (entry == null || !entry.IsInventoryTool) continue;
                    float y = -200f - row * 48f;
                    AddText(dialog, entry.assetKey + " StockLabel", new Vector2(210f, y), new Vector2(300f, 38f), 18f, TextAlignmentOptions.MidlineLeft).text = entry.assetKey;
                    TMP_InputField input = AddInput(dialog, entry.assetKey + " Stock", new Vector2(475f, y), _ => { });
                    input.contentType = TMP_InputField.ContentType.IntegerNumber;
                    input.characterLimit = 9;
                    input.SetTextWithoutNotify("0");
                    row++;
                }
            }

            TMP_Text validation = AddText(dialog, "LevelSettingsValidation", new Vector2(320f, -640f), new Vector2(570f, 42f), 16f, TextAlignmentOptions.Center);
            validation.color = new Color(1f, 0.45f, 0.38f, 1f);
            AddButton(dialog, "Save Settings", new Vector2(220f, -700f), () => { });
            AddButton(dialog, "Cancel Settings", new Vector2(420f, -700f), () => { });
            BindLevelSettingsPanel(overlay);
        }

        private void BindLevelSettingsPanel(Transform panelRoot)
        {
            levelSettingsPanel = panelRoot.gameObject;
            levelModeDropdown = FindComponent<TMP_Dropdown>(panelRoot, "LevelModeDropdown");
            levelSettingsHint = FindComponent<TMP_Text>(panelRoot, "ModeHint");
            levelSettingsValidation = FindComponent<TMP_Text>(panelRoot, "LevelSettingsValidation");
            levelStockInputs.Clear();
            if (registry != null)
            {
                for (int i = 0; i < registry.Entries.Count; i++)
                {
                    CollaborativeLevelAssetEntry entry = registry.Entries[i];
                    if (entry == null || !entry.IsInventoryTool) continue;
                    TMP_InputField input = FindComponent<TMP_InputField>(panelRoot, entry.assetKey + " StockInput");
                    if (input != null) levelStockInputs.Add(entry.assetKey, input);
                }
            }
            if (levelModeDropdown != null) levelModeDropdown.onValueChanged.AddListener(_ => RefreshLevelSettingsMode());
            BindButton(panelRoot, "Save SettingsButton", SaveLevelSettings);
            BindButton(panelRoot, "Cancel SettingsButton", CloseLevelSettings);
            levelSettingsPanel.SetActive(false);
        }

        private void OpenLevelSettings()
        {
            if (levelSettingsPanel == null || simulating) return;
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (document.toolStock != null)
            {
                for (int i = 0; i < document.toolStock.Count; i++)
                {
                    CollaborativeToolStock stock = document.toolStock[i];
                    if (stock != null && !string.IsNullOrWhiteSpace(stock.assetKey)) counts[stock.assetKey] = Mathf.Max(0, stock.count);
                }
            }
            foreach (KeyValuePair<string, TMP_InputField> item in levelStockInputs)
                item.Value.SetTextWithoutNotify(counts.TryGetValue(item.Key, out int count) ? count.ToString(CultureInfo.InvariantCulture) : "0");
            levelModeDropdown.SetValueWithoutNotify(document.mode == "LowestCost" ? 1 : 0);
            levelSettingsValidation.text = string.Empty;
            RefreshLevelSettingsMode();
            levelSettingsPanel.SetActive(true);
            levelSettingsPanel.transform.SetAsLastSibling();
        }

        private void CloseLevelSettings()
        {
            if (levelSettingsPanel != null) levelSettingsPanel.SetActive(false);
        }

        private void RefreshLevelSettingsMode()
        {
            bool lowestCost = levelModeDropdown != null && levelModeDropdown.value == 1;
            foreach (TMP_InputField input in levelStockInputs.Values) input.interactable = !lowestCost;
            if (levelSettingsHint != null)
                levelSettingsHint.text = lowestCost
                    ? "Shared ToolCatalog · Unlimited supply · Cost scoring"
                    : "Set player inventory independently from objects placed in the scene";
        }

        private void SaveLevelSettings()
        {
            List<CollaborativeToolStock> stock = new List<CollaborativeToolStock>();
            if (registry == null)
            {
                levelSettingsValidation.text = "Asset registry is missing.";
                return;
            }
            for (int i = 0; i < registry.Entries.Count; i++)
            {
                CollaborativeLevelAssetEntry entry = registry.Entries[i];
                if (entry == null || !entry.IsInventoryTool || !levelStockInputs.TryGetValue(entry.assetKey, out TMP_InputField input)) continue;
                if (!int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count < 0)
                {
                    levelSettingsValidation.text = entry.assetKey + " must be a non-negative integer.";
                    return;
                }
                if (count > 0) stock.Add(new CollaborativeToolStock { assetKey = entry.assetKey, count = count });
            }

            PushUndo();
            document.mode = levelModeDropdown != null && levelModeDropdown.value == 1 ? "LowestCost" : "Normal";
            document.toolStock = stock;
            CloseLevelSettings();
            SetStatus("Level settings saved: " + document.mode);
        }

        private static TMP_Dropdown AddDropdown(Transform parent, string name, Vector2 position, Vector2 size, string[] options)
        {
            GameObject root = new GameObject(name + "Dropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.27f, 1f);

            TMP_Text caption = AddText(root.transform, "Label", new Vector2(size.x * 0.5f - 18f, -size.y * 0.5f), new Vector2(size.x - 52f, size.y), 18f, TextAlignmentOptions.MidlineLeft);
            AddText(root.transform, "Arrow", new Vector2(size.x - 25f, -size.y * 0.5f), new Vector2(30f, size.y), 20f, TextAlignmentOptions.Center).text = "▼";

            RectTransform template = CreatePanel(root.transform, "Template", new Color(0.18f, 0.20f, 0.23f, 1f));
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 88f);

            GameObject itemObject = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            itemObject.transform.SetParent(template, false);
            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0f, 40f);
            Image itemImage = itemObject.GetComponent<Image>();
            itemImage.color = new Color(0.28f, 0.30f, 0.34f, 1f);
            Toggle itemToggle = itemObject.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            TMP_Text itemText = AddText(itemObject.transform, "Item Label", new Vector2(size.x * 0.5f, -20f), new Vector2(size.x - 24f, 40f), 18f, TextAlignmentOptions.MidlineLeft);

            TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            dropdown.options.Clear();
            for (int i = 0; i < options.Length; i++) dropdown.options.Add(new TMP_Dropdown.OptionData(options[i]));
            dropdown.RefreshShownValue();
            template.gameObject.SetActive(false);
            return dropdown;
        }

        private static T FindComponent<T>(Transform root, string objectName) where T : Component
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name == objectName) return children[i].GetComponent<T>();
            return null;
        }

        private static void BindButton(Transform root, string objectName, UnityEngine.Events.UnityAction action)
        {
            Button button = FindComponent<Button>(root, objectName);
            if (button != null) button.onClick.AddListener(action);
        }

        private static TMP_InputField BindInput(Transform root, string objectName, UnityEngine.Events.UnityAction<string> action)
        {
            TMP_InputField input = FindComponent<TMP_InputField>(root, objectName);
            if (input != null) input.onEndEdit.AddListener(action);
            return input;
        }

        private void ConfigureLevelTitleInput()
        {
            if (titleInput == null) return;
            titleInput.contentType = TMP_InputField.ContentType.Standard;
            titleInput.characterValidation = TMP_InputField.CharacterValidation.None;
            titleInput.lineType = TMP_InputField.LineType.SingleLine;

#if UNITY_WEBGL && !UNITY_EDITOR
            CollaborativeWebGLTextInputBridge bridge = titleInput.GetComponent<CollaborativeWebGLTextInputBridge>();
            if (bridge == null) bridge = titleInput.gameObject.AddComponent<CollaborativeWebGLTextInputBridge>();
            bridge.Configure(titleInput, gameObject, nameof(OnWebGLTitleInputChanged), nameof(OnWebGLTitleInputEnded));
#endif
        }

        public void OnWebGLTitleInputChanged(string value)
        {
            string title = value ?? string.Empty;
            if (titleInput != null) titleInput.SetTextWithoutNotify(title);
            document.title = title;
        }

        public void OnWebGLTitleInputEnded(string value)
        {
            OnWebGLTitleInputChanged(value);
            CollaborativeWebGLTextInputBridge.RestoreUnityKeyboardCapture();
            if (titleInput != null) titleInput.DeactivateInputField();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void AddObject(CollaborativeLevelAssetEntry entry)
        {
            if (entry == null || entry.prefab == null || simulating) return;
            PushUndo();
            GameObject instance = Instantiate(entry.prefab, worldRoot);
            instance.name = entry.assetKey + "_" + nextObjectId++;
            instance.transform.position = new Vector3(worldCamera.transform.position.x, worldCamera.transform.position.y, 0f);
            CollaborativeLevelObject marker = instance.AddComponent<CollaborativeLevelObject>();
            marker.Initialize(instance.name, entry);
            if (entry.assetKey == "portal")
            {
                marker.Properties.Add(new CollaborativeProperty { key = "pairId", value = GetNextPortalPairId() });
                ApplyPortalPair(instance, marker.Properties);
            }
            if (entry.kind == CollaborativeObjectKind.SplineTool)
            {
                marker.SplinePoints.Add(NewSplinePoint(-2f, 0f));
                marker.SplinePoints.Add(NewSplinePoint(0f, 1f));
                marker.SplinePoints.Add(NewSplinePoint(2f, 0f));
                ApplySpline(marker);
            }
            SetPhysics(instance, false);
            Select(marker);
            RebuildHierarchy();
        }

        private string GetNextPortalPairId()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            CollaborativeLevelObject[] objects = worldRoot.GetComponentsInChildren<CollaborativeLevelObject>(true);
            foreach (CollaborativeLevelObject item in objects)
            {
                if (item.AssetKey != "portal") continue;
                foreach (CollaborativeProperty property in item.Properties)
                    if (property != null && property.key == "pairId") counts[property.value] = counts.TryGetValue(property.value, out int count) ? count + 1 : 1;
            }
            for (int id = 0; ; id++)
            {
                string key = id.ToString(CultureInfo.InvariantCulture);
                if (!counts.TryGetValue(key, out int count) || count < 2) return key;
            }
        }

        private void HandleWorldPointer()
        {
            if (Input.GetMouseButtonUp(0)) CompletePointerDrag();
            if (IsPointerOverUi())
            {
                if (canvasPanning) lastPanScreenPosition = Input.mousePosition;
                return;
            }
            Vector2 world = ScreenToWorld(Input.mousePosition);
            if (Input.GetMouseButtonDown(0))
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(world);
                draggedSplineHandle = FindSplineHandle(hits);
                if (draggedSplineHandle != null)
                {
                    PushUndo();
                    dragging = true;
                    return;
                }
                CollaborativeLevelObject marker = selected != null && IsObjectAt(selected, world, hits)
                    ? selected
                    : FindObjectAt(world, hits);
                Select(marker);
                if (marker != null)
                {
                    PushUndo();
                    dragging = true;
                    dragOffset = (Vector2)marker.transform.position - world;
                }
                else
                {
                    canvasPanning = true;
                    lastPanScreenPosition = Input.mousePosition;
                }
            }
            if (dragging && draggedSplineHandle != null && Input.GetMouseButton(0))
            {
                CollaborativeLevelObject owner = draggedSplineHandle.Owner;
                if (owner != null && draggedSplineHandle.IsTrackIntersection)
                {
                    TrackIntersectionEditable2D editable = owner.GetComponent<TrackIntersectionEditable2D>();
                    CollaborativeTrackIntersectionData data = owner.TrackIntersection;
                    if (editable != null && data != null)
                    {
                        if (draggedSplineHandle.IsEntry)
                        {
                            Vector2 local = owner.transform.InverseTransformPoint(world);
                            float rotation = editable.GetEntryLocalRotation(draggedSplineHandle.Upper);
                            editable.SetEntryLocalPose(draggedSplineHandle.Upper, local, rotation, data);
                        }
                        else
                        {
                            CollaborativeTrackCurveData curve = draggedSplineHandle.Upper ? data.upperCurve : data.lowerCurve;
                            SplineContainerTransformPoint(editable, draggedSplineHandle.Upper, world, out Vector2 local);
                            if (curve != null && curve.points != null && draggedSplineHandle.PointIndex >= 0
                                && draggedSplineHandle.PointIndex < curve.points.Count)
                            {
                                curve.points[draggedSplineHandle.PointIndex].position = local;
                                editable.ApplyCurve(draggedSplineHandle.Upper, curve);
                                editable.ApplyEntries(data);
                            }
                        }
                        draggedSplineHandle.transform.position = world;
                    }
                }
                else if (owner != null && draggedSplineHandle.PointIndex < owner.SplinePoints.Count)
                {
                    owner.SplinePoints[draggedSplineHandle.PointIndex].position = owner.transform.InverseTransformPoint(world);
                    ApplySpline(owner);
                    draggedSplineHandle.transform.position = world;
                }
            }
            else if (dragging && selected != null && Input.GetMouseButton(0))
            {
                selected.transform.position = world + dragOffset;
                UpdateSplineHandlePositions();
            }
            else if (canvasPanning && Input.GetMouseButton(0))
                PanCanvasTo(Input.mousePosition);
        }

        private void HandleSimulationCanvasPan()
        {
            if (Input.GetMouseButtonUp(0)) canvasPanning = false;
            if (IsPointerOverUi())
            {
                if (canvasPanning) lastPanScreenPosition = Input.mousePosition;
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                canvasPanning = true;
                lastPanScreenPosition = Input.mousePosition;
            }
            else if (canvasPanning && Input.GetMouseButton(0))
            {
                PanCanvasTo(Input.mousePosition);
            }
        }

        private void PanCanvasTo(Vector2 currentScreenPosition)
        {
            Vector2 previousWorld = ScreenToWorld(lastPanScreenPosition);
            Vector2 currentWorld = ScreenToWorld(currentScreenPosition);
            worldCamera.transform.position += (Vector3)(previousWorld - currentWorld);
            lastPanScreenPosition = currentScreenPosition;
        }

        private void CompletePointerDrag()
        {
            bool movedObject = dragging && selected != null;
            dragging = false;
            canvasPanning = false;
            draggedSplineHandle = null;
            if (movedObject) RefreshSelection();
        }

        private static void SplineContainerTransformPoint(TrackIntersectionEditable2D editable, bool upper, Vector2 world, out Vector2 local)
        {
            Transform splineTransform = editable.GetSpline(upper).transform;
            local = splineTransform.InverseTransformPoint(world);
        }

        private CollaborativeLevelObject FindObjectAt(Vector2 world, Collider2D[] hits)
        {
            HashSet<CollaborativeLevelObject> candidates = new HashSet<CollaborativeLevelObject>();
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                CollaborativeLevelObject candidate = hits[i].GetComponentInParent<CollaborativeLevelObject>();
                if (candidate != null) candidates.Add(candidate);
            }

            // Rigidbody2D.simulated=false also removes its colliders from physics queries.
            // Use visible sprite bounds so balls and other paused physics objects remain editable.
            CollaborativeLevelObject[] objects = worldRoot.GetComponentsInChildren<CollaborativeLevelObject>(true);
            for (int i = 0; i < objects.Length; i++)
            {
                SpriteRenderer[] renderers = objects[i].GetComponentsInChildren<SpriteRenderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (!renderers[j].bounds.Contains(world)) continue;
                    candidates.Add(objects[i]);
                    break;
                }
            }

            CollaborativeLevelObject best = null;
            int bestClickSortingOrder = int.MinValue;
            int bestSiblingIndex = int.MinValue;
            foreach (CollaborativeLevelObject candidate in candidates)
                ConsiderCandidate(candidate, ref best, ref bestClickSortingOrder, ref bestSiblingIndex);
            return best;
        }

        private static bool IsObjectAt(CollaborativeLevelObject candidate, Vector2 world, Collider2D[] hits)
        {
            if (candidate == null) return false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                if (hits[i].GetComponentInParent<CollaborativeLevelObject>() == candidate) return true;
            }

            SpriteRenderer[] renderers = candidate.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i].bounds.Contains(world)) return true;
            return false;
        }

        private void ConsiderCandidate(
            CollaborativeLevelObject candidate,
            ref CollaborativeLevelObject best,
            ref int bestClickSortingOrder,
            ref int bestSiblingIndex)
        {
            if (candidate == null) return;
            CollaborativeLevelAssetEntry entry = registry != null ? registry.Find(candidate.AssetKey) : null;
            int clickSortingOrder = entry != null && entry.toolDefinition != null
                ? entry.toolDefinition.ClickSortingOrder
                : DefaultClickSortingOrder;
            int siblingIndex = candidate.transform.GetSiblingIndex();
            if (best != null && (clickSortingOrder < bestClickSortingOrder
                || (clickSortingOrder == bestClickSortingOrder && siblingIndex <= bestSiblingIndex))) return;
            best = candidate;
            bestClickSortingOrder = clickSortingOrder;
            bestSiblingIndex = siblingIndex;
        }

        private static CollaborativeSplineHandle FindSplineHandle(Collider2D[] hits)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                CollaborativeSplineHandle handle = hits[i].GetComponent<CollaborativeSplineHandle>();
                if (handle != null) return handle;
            }
            return null;
        }

        private void HandleCamera()
        {
            if (IsPointerOverUi()) return;
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) worldCamera.orthographicSize = Mathf.Clamp(worldCamera.orthographicSize - scroll, 3f, 10f);
            if (Input.GetMouseButton(2))
            {
                Vector2 delta = new Vector2(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
                worldCamera.transform.position += (Vector3)(delta * worldCamera.orthographicSize * 0.05f);
            }
        }

        private void ResetView()
        {
            dragging = false;
            canvasPanning = false;
            draggedSplineHandle = null;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            worldCamera.orthographicSize = DefaultCameraSize;
            SetStatus("View reset to (0,0).");
        }

        private void NewDocument()
        {
            CloseLevelSettings();
            PushUndo();
            LoadDocument(new CollaborativeLevelDocument());
            SetStatus("New level");
        }

        private void DeleteSelected()
        {
            if (selected == null || simulating) return;
            PushUndo();
            GameObject target = selected.gameObject;
            RemoveHierarchyItem(selected);
            Select(null);
            Destroy(target);
        }

        private void DuplicateSelected()
        {
            if (selected == null || simulating) return;
            CollaborativeLevelAssetEntry entry = registry.Find(selected.AssetKey);
            if (entry == null) return;
            PushUndo();
            CollaborativeObjectRecord record = selected.ToRecord();
            record.id = entry.assetKey + "_" + nextObjectId++;
            record.position += new Vector2(0.5f, -0.5f);
            if (record.assetKey == "portal")
                record.properties = new List<CollaborativeProperty> { new CollaborativeProperty { key = "pairId", value = GetNextPortalPairId() } };
            GameObject clone = Instantiate(selected.gameObject, selected.transform.parent);
            clone.name = record.id;
            clone.transform.localPosition = record.position;
            CollaborativeLevelObject marker = clone.GetComponent<CollaborativeLevelObject>();
            marker.Initialize(record);
            ApplyRecordSize(clone, record);
            if (record.assetKey == "portal") ApplyPortalPair(clone, marker.Properties);
            if (marker.Kind == CollaborativeObjectKind.SplineTool) ApplySpline(marker);
            ApplyTrackIntersection(marker);
            Select(marker);
            RebuildHierarchy();
        }

        private void RotateSelected(float degrees)
        {
            if (selected == null || simulating) return;
            PushUndo();
            selected.transform.Rotate(0f, 0f, degrees);
            UpdateSplineHandlePositions();
            RefreshSelection();
        }

        private void ToggleSimulation()
        {
            if (!simulating)
            {
                editSnapshot = CaptureDocument().ToJson();
                simulating = true;
                foreach (Rigidbody2D body in worldRoot.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = true;
                foreach (BallController ball in worldRoot.GetComponentsInChildren<BallController>(true)) ball.Launch();
                ApplyAllPortalPairs();
                SetSimulationPreview(true);
                SetHierarchyInteractable(false);
                SetStatus("Simulation running. Click Test again to restore edit state.");
            }
            else
            {
                SetSimulationPreview(false);
                simulating = false;
                LoadDocument(CollaborativeLevelDocument.FromJson(editSnapshot));
                undo.Clear();
                redo.Clear();
                SetStatus("Edit state restored.");
            }
        }

        private void SetSimulationPreview(bool playing)
        {
            MonoBehaviour[] behaviours = worldRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is ISimulationPreview2D participant) participant.SetSimulationPreview(playing);
        }

        private void ApplyAllPortalPairs()
        {
            CollaborativeLevelObject[] objects = worldRoot.GetComponentsInChildren<CollaborativeLevelObject>(true);
            for (int i = 0; i < objects.Length; i++)
                if (objects[i].AssetKey == "portal") ApplyPortalPair(objects[i].gameObject, objects[i].Properties);
        }

        private static void ApplyPortalPair(GameObject instance, IList<CollaborativeProperty> properties)
        {
            PortalTool portal = instance.GetComponentInChildren<PortalTool>(true);
            if (portal == null || properties == null) return;
            for (int i = 0; i < properties.Count; i++)
            {
                CollaborativeProperty property = properties[i];
                if (property == null || property.key != "pairId" || !int.TryParse(property.value, out int pairId)) continue;
                portal.SetPairId(pairId);
                return;
            }
        }

        private CollaborativeLevelDocument CaptureDocument()
        {
            document.formatVersion = 4;
            document.objects.Clear();
            CollaborativeLevelObject[] objects = worldRoot.GetComponentsInChildren<CollaborativeLevelObject>(true);
            UpdateCanvasBounds(objects);
            for (int i = 0; i < objects.Length; i++)
                document.objects.Add(objects[i].ToRecord());
            if (document.toolStock == null) document.toolStock = new List<CollaborativeToolStock>();
            for (int i = document.toolStock.Count - 1; i >= 0; i--)
                if (document.toolStock[i] != null && document.toolStock[i].count == 0)
                    document.toolStock.RemoveAt(i);
            return document;
        }

        private void UpdateCanvasBounds(CollaborativeLevelObject[] objects)
        {
            bool hasBounds = false;
            Bounds combined = default(Bounds);
            for (int i = 0; i < objects.Length; i++)
            {
                CollaborativeLevelObject item = objects[i];
                Encapsulate(ref combined, ref hasBounds, new Bounds(item.transform.position, Vector3.zero));

                SpriteRenderer[] sprites = item.GetComponentsInChildren<SpriteRenderer>(true);
                for (int j = 0; j < sprites.Length; j++) Encapsulate(ref combined, ref hasBounds, sprites[j].bounds);

                MeshRenderer[] meshes = item.GetComponentsInChildren<MeshRenderer>(true);
                for (int j = 0; j < meshes.Length; j++) Encapsulate(ref combined, ref hasBounds, meshes[j].bounds);

                Collider2D[] colliders = item.GetComponentsInChildren<Collider2D>(true);
                for (int j = 0; j < colliders.Length; j++)
                    if (colliders[j].bounds.size.sqrMagnitude > 0f) Encapsulate(ref combined, ref hasBounds, colliders[j].bounds);
            }

            if (!hasBounds)
            {
                document.canvasCenter = Vector2.zero;
                document.canvasWidth = MinimumCanvasWidth;
                document.canvasHeight = MinimumCanvasHeight;
                return;
            }

            document.canvasCenter = combined.center;
            document.canvasWidth = Mathf.Max(MinimumCanvasWidth, combined.size.x + CanvasPadding * 2f);
            document.canvasHeight = Mathf.Max(MinimumCanvasHeight, combined.size.y + CanvasPadding * 2f);
        }

        private static void Encapsulate(ref Bounds combined, ref bool hasBounds, Bounds value)
        {
            if (!hasBounds)
            {
                combined = value;
                hasBounds = true;
                return;
            }
            combined.Encapsulate(value);
        }

        private void LoadDocument(CollaborativeLevelDocument source)
        {
            CloseLevelSettings();
            ClearHierarchyItems();
            for (int i = worldRoot.childCount - 1; i >= 0; i--) Destroy(worldRoot.GetChild(i).gameObject);
            document = source;
            if (titleInput != null) titleInput.SetTextWithoutNotify(document.title);
            if (authorInput != null) authorInput.SetTextWithoutNotify(document.author);
            nextObjectId = 1;
            List<CollaborativeLevelObject> loadedObjects = new List<CollaborativeLevelObject>();
            foreach (CollaborativeObjectRecord record in document.objects)
            {
                CollaborativeLevelAssetEntry entry = registry != null ? registry.Find(record.assetKey) : null;
                if (entry == null || entry.prefab == null) continue;
                GameObject instance = Instantiate(entry.prefab, worldRoot);
                instance.name = record.id;
                instance.transform.localPosition = record.position;
                instance.transform.localRotation = Quaternion.Euler(0f, 0f, record.rotation);
                ApplyRecordSize(instance, record);
                CollaborativeLevelObject marker = instance.AddComponent<CollaborativeLevelObject>();
                marker.Initialize(record);
                if (record.assetKey == "portal") ApplyPortalPair(instance, marker.Properties);
                if (record.kind == CollaborativeObjectKind.SplineTool)
                    ApplySpline(marker);
                ApplyTrackIntersection(marker);
                SetPhysics(instance, false);
                loadedObjects.Add(marker);
                nextObjectId++;
            }
            Select(null);
            RebuildHierarchy(loadedObjects);
        }

        private void PushUndo()
        {
            if (worldRoot == null) return;
            undo.Push(CaptureDocument().ToJson());
            redo.Clear();
            while (undo.Count > 50) TrimOldest(undo);
        }

        private void Undo()
        {
            if (undo.Count == 0 || simulating) return;
            redo.Push(CaptureDocument().ToJson());
            LoadDocument(CollaborativeLevelDocument.FromJson(undo.Pop()));
        }

        private void Redo()
        {
            if (redo.Count == 0 || simulating) return;
            undo.Push(CaptureDocument().ToJson());
            LoadDocument(CollaborativeLevelDocument.FromJson(redo.Pop()));
        }

        private static void TrimOldest(Stack<string> stack)
        {
            string[] items = stack.ToArray();
            stack.Clear();
            for (int i = items.Length - 2; i >= 0; i--) stack.Push(items[i]);
        }

        private void Select(CollaborativeLevelObject marker)
        {
            RestoreSelectionOpacity();
            if (marker != selected && marker != null && marker.GetComponent<TrackIntersectionEditable2D>() != null && trackPartDropdown != null)
                trackPartDropdown.SetValueWithoutNotify(0);
            selected = marker;
            ApplySelectionOpacity();
            RefreshSelection();
            RebuildSplineHandles();
            RefreshHierarchySelection();
        }

        private void ApplySelectionOpacity()
        {
            if (selected == null) return;
            SpriteRenderer[] renderers = selected.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                Color original = renderer.color;
                selectedRenderers.Add(renderer);
                selectedRendererColors.Add(original);
                original.a *= 0.55f;
                renderer.color = original;
            }
        }

        private void RestoreSelectionOpacity()
        {
            int count = Mathf.Min(selectedRenderers.Count, selectedRendererColors.Count);
            for (int i = 0; i < count; i++)
                if (selectedRenderers[i] != null) selectedRenderers[i].color = selectedRendererColors[i];
            selectedRenderers.Clear();
            selectedRendererColors.Clear();
        }

        private void AddSplinePoint()
        {
            if (selected == null || simulating) return;
            if (TryGetActiveTrackPart(out TrackIntersectionEditable2D track, out CollaborativeTrackIntersectionData trackData, out bool upper, out bool entry))
            {
                if (entry) return;
                CollaborativeTrackCurveData curve = upper ? trackData.upperCurve : trackData.lowerCurve;
                PushUndo();
                Vector2 trackPosition = curve.points.Count == 0 ? Vector2.zero : curve.points[curve.points.Count - 1].position + Vector2.right;
                curve.points.Add(NewSplinePoint(trackPosition.x, trackPosition.y));
                track.ApplyCurve(upper, curve);
                track.ApplyEntries(trackData);
                RebuildSplineHandles();
                return;
            }
            if (selected.Kind != CollaborativeObjectKind.SplineTool) return;
            PushUndo();
            Vector2 position = selected.SplinePoints.Count == 0 ? Vector2.zero : selected.SplinePoints[selected.SplinePoints.Count - 1].position + Vector2.right;
            selected.SplinePoints.Add(NewSplinePoint(position.x, position.y));
            ApplySpline(selected);
            RebuildSplineHandles();
        }

        private void RemoveSplinePoint()
        {
            if (selected == null || simulating) return;
            if (TryGetActiveTrackPart(out TrackIntersectionEditable2D track, out CollaborativeTrackIntersectionData trackData, out bool upper, out bool entry))
            {
                if (entry) return;
                CollaborativeTrackCurveData curve = upper ? trackData.upperCurve : trackData.lowerCurve;
                if (curve.points.Count <= 2) return;
                PushUndo();
                curve.points.RemoveAt(curve.points.Count - 1);
                track.ApplyCurve(upper, curve);
                track.ApplyEntries(trackData);
                RebuildSplineHandles();
                return;
            }
            if (selected.Kind != CollaborativeObjectKind.SplineTool || selected.SplinePoints.Count <= 2) return;
            PushUndo();
            selected.SplinePoints.RemoveAt(selected.SplinePoints.Count - 1);
            ApplySpline(selected);
            RebuildSplineHandles();
        }

        private void RebuildSplineHandles()
        {
            if (splineHandleRoot == null) return;
            for (int i = splineHandleRoot.childCount - 1; i >= 0; i--) Destroy(splineHandleRoot.GetChild(i).gameObject);
            if (selected == null) return;
            if (TryGetActiveTrackPart(out TrackIntersectionEditable2D track, out CollaborativeTrackIntersectionData trackData, out bool upper, out bool entry))
            {
                if (entry)
                {
                    CreateTrackHandle(track.GetEntry(upper).position, new Color(0.20f, 0.78f, 1f, 1f), -1, upper, true);
                    return;
                }
                CollaborativeTrackCurveData curve = upper ? trackData.upperCurve : trackData.lowerCurve;
                Transform splineTransform = track.GetSpline(upper).transform;
                for (int i = 0; i < curve.points.Count; i++)
                    CreateTrackHandle(splineTransform.TransformPoint(curve.points[i].position), new Color(1f, 0.72f, 0.18f, 1f), i, upper, false);
                return;
            }
            if (selected.Kind != CollaborativeObjectKind.SplineTool) return;
            for (int i = 0; i < selected.SplinePoints.Count; i++)
            {
                GameObject handle = new GameObject("Point " + i, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(CollaborativeSplineHandle));
                handle.transform.SetParent(splineHandleRoot, false);
                handle.transform.position = selected.transform.TransformPoint(selected.SplinePoints[i].position);
                handle.transform.localScale = Vector3.one * 0.28f;
                SpriteRenderer renderer = handle.GetComponent<SpriteRenderer>();
                renderer.sprite = GetHandleSprite();
                renderer.color = new Color(1f, 0.72f, 0.18f, 1f);
                renderer.sortingOrder = 1000;
                handle.GetComponent<CircleCollider2D>().radius = 0.6f;
                handle.GetComponent<CollaborativeSplineHandle>().Initialize(selected, i);
            }
        }

        private void CreateTrackHandle(Vector3 position, Color color, int pointIndex, bool upper, bool entry)
        {
            GameObject handle = new GameObject(entry ? "Entry Handle" : "Point " + pointIndex,
                typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(CollaborativeSplineHandle));
            handle.transform.SetParent(splineHandleRoot, false);
            handle.transform.position = position;
            handle.transform.localScale = Vector3.one * (entry ? 0.36f : 0.28f);
            SpriteRenderer renderer = handle.GetComponent<SpriteRenderer>();
            renderer.sprite = GetHandleSprite();
            renderer.color = color;
            renderer.sortingOrder = 1000;
            handle.GetComponent<CircleCollider2D>().radius = 0.6f;
            CollaborativeSplineHandle marker = handle.GetComponent<CollaborativeSplineHandle>();
            if (entry) marker.InitializeTrackEntry(selected, upper);
            else marker.InitializeTrackPoint(selected, upper, pointIndex);
        }

        private void UpdateSplineHandlePositions()
        {
            if (selected == null || splineHandleRoot == null) return;
            CollaborativeSplineHandle[] handles = splineHandleRoot.GetComponentsInChildren<CollaborativeSplineHandle>(true);
            for (int i = 0; i < handles.Length; i++)
            {
                CollaborativeSplineHandle handle = handles[i];
                if (handle.Owner != selected) continue;
                if (handle.IsTrackIntersection)
                {
                    TrackIntersectionEditable2D track = selected.GetComponent<TrackIntersectionEditable2D>();
                    if (track == null || selected.TrackIntersection == null) continue;
                    if (handle.IsEntry)
                        handle.transform.position = track.GetEntry(handle.Upper).position;
                    else
                    {
                        CollaborativeTrackCurveData curve = handle.Upper
                            ? selected.TrackIntersection.upperCurve
                            : selected.TrackIntersection.lowerCurve;
                        if (handle.PointIndex >= 0 && handle.PointIndex < curve.points.Count)
                            handle.transform.position = track.GetSpline(handle.Upper).transform.TransformPoint(curve.points[handle.PointIndex].position);
                    }
                }
                else if (handle.PointIndex >= 0 && handle.PointIndex < selected.SplinePoints.Count)
                    handle.transform.position = selected.transform.TransformPoint(selected.SplinePoints[handle.PointIndex].position);
            }
        }

        private void RefreshTrackPartSelection()
        {
            RefreshSelection();
            RebuildSplineHandles();
        }

        private bool TryGetActiveTrackPart(
            out TrackIntersectionEditable2D editable,
            out CollaborativeTrackIntersectionData data,
            out bool upper,
            out bool entry)
        {
            editable = selected != null ? selected.GetComponent<TrackIntersectionEditable2D>() : null;
            data = selected != null ? selected.TrackIntersection : null;
            int part = trackPartDropdown != null ? trackPartDropdown.value : 0;
            upper = part == 0 || part == 2;
            entry = part >= 2;
            return editable != null && editable.IsConfigured && data != null;
        }

        private static Sprite GetHandleSprite()
        {
            if (handleSprite != null) return handleSprite;
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[256];
            Vector2 center = new Vector2(7.5f, 7.5f);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    pixels[y * 16 + x] = Vector2.Distance(new Vector2(x, y), center) <= 7f ? Color.white : Color.clear;
            texture.SetPixels(pixels);
            texture.Apply();
            handleSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            return handleSprite;
        }

        private static CollaborativeSplinePoint NewSplinePoint(float x, float y)
        {
            return new CollaborativeSplinePoint { position = new Vector2(x, y) };
        }

        private static Sprite handleSprite;

        private void RefreshSelection()
        {
            bool hasSelection = selected != null;
            bool trackSelected = hasSelection && selected.GetComponent<TrackIntersectionEditable2D>() != null && selected.TrackIntersection != null;
            if (trackIntersectionInfo != null) trackIntersectionInfo.SetActive(trackSelected);
            if (selectionText != null)
            {
                selectionText.text = selected == null
                    ? "No selection"
                    : selected.Id + "\n" + selected.AssetKey + "\nPosition: " + (Vector2)selected.transform.position + "\nRotation: " + selected.transform.localEulerAngles.z.ToString("0");
            }
            SetInput(xInput, hasSelection ? selected.transform.position.x : 0f, hasSelection);
            SetInput(yInput, hasSelection ? selected.transform.position.y : 0f, hasSelection);
            SetInput(rotationInput, hasSelection ? selected.transform.localEulerAngles.z : 0f, hasSelection);
            ISizeEditable2D editable = null;
            bool sizeEditable = hasSelection && SizeEditable2DUtility.TryGet(selected.gameObject, out editable);
            SetInput(sizeXInput, sizeEditable ? editable.Size.x : 1f, sizeEditable);
            SetInput(sizeYInput, sizeEditable ? editable.Size.y : 1f, sizeEditable);
            bool splineSelected = hasSelection && selected.Kind == CollaborativeObjectKind.SplineTool;
            if (trackPartDropdown != null) trackPartDropdown.interactable = trackSelected;
            bool trackCurveSelected = TryGetActiveTrackPart(out TrackIntersectionEditable2D track, out CollaborativeTrackIntersectionData trackData, out bool upper, out bool entry) && !entry;
            CollaborativeTrackCurveData trackCurve = trackCurveSelected ? (upper ? trackData.upperCurve : trackData.lowerCurve) : null;
            SetInput(splineWidthInput,
                splineSelected ? selected.SplineWidth : trackCurveSelected ? trackCurve.width : 0.5f,
                splineSelected || trackCurveSelected);
            bool trackEntrySelected = trackSelected && !trackCurveSelected && track != null;
            SetInput(trackEntryXInput, trackEntrySelected ? track.GetEntryLocalPosition(upper).x : 0f, trackEntrySelected);
            SetInput(trackEntryYInput, trackEntrySelected ? track.GetEntryLocalPosition(upper).y : 0f, trackEntrySelected);
            SetInput(trackEntryRotationInput, trackEntrySelected ? track.GetEntryLocalRotation(upper) : 0f, trackEntrySelected);
        }

        private void ApplyInspectorTransform()
        {
            if (selected == null || simulating) return;
            if (!TryRead(xInput, out float x) || !TryRead(yInput, out float y)
                || !TryRead(rotationInput, out float rotation)) return;
            bool sizeEditable = SizeEditable2DUtility.TryGet(selected.gameObject, out ISizeEditable2D editable);
            float sizeX = 1f;
            float sizeY = 1f;
            if (sizeEditable && (!TryRead(sizeXInput, out sizeX) || !TryRead(sizeYInput, out sizeY)
                || sizeX <= 0f || sizeY <= 0f)) return;
            float splineWidth = 0.5f;
            bool trackPart = TryGetActiveTrackPart(out TrackIntersectionEditable2D track, out CollaborativeTrackIntersectionData trackData, out bool upper, out bool entry);
            bool widthEditable = selected.Kind == CollaborativeObjectKind.SplineTool || (trackPart && !entry);
            if (widthEditable && (!TryRead(splineWidthInput, out splineWidth) || splineWidth <= 0f)) return;
            Vector2 entryPosition = Vector2.zero;
            float entryRotation = 0f;
            if (trackPart && entry)
            {
                if (!TryRead(trackEntryXInput, out float entryX)
                    || !TryRead(trackEntryYInput, out float entryY)
                    || !TryRead(trackEntryRotationInput, out entryRotation)) return;
                entryPosition = new Vector2(entryX, entryY);
            }
            PushUndo();
            selected.transform.position = new Vector3(x, y, 0f);
            selected.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            if (sizeEditable) editable.SetSize(new Vector2(sizeX, sizeY));
            if (selected.Kind == CollaborativeObjectKind.SplineTool)
            {
                selected.SplineWidth = splineWidth;
                ApplySpline(selected);
            }
            else if (trackPart && !entry)
            {
                CollaborativeTrackCurveData curve = upper ? trackData.upperCurve : trackData.lowerCurve;
                curve.width = splineWidth;
                track.ApplyCurve(upper, curve);
                track.ApplyEntries(trackData);
            }
            else if (trackPart)
                track.SetEntryLocalPose(upper, entryPosition, entryRotation, trackData);
            RebuildSplineHandles();
            RefreshSelection();
        }

        private void ToggleSplineClosed()
        {
            if (selected == null || selected.Kind != CollaborativeObjectKind.SplineTool || simulating) return;
            PushUndo();
            selected.SplineClosed = !selected.SplineClosed;
            ApplySpline(selected);
            SetStatus(selected.SplineClosed ? "Path is closed." : "Path is open.");
        }

        private void ToggleSplineCollision()
        {
            if (selected == null || selected.Kind != CollaborativeObjectKind.SplineTool || simulating) return;
            PushUndo();
            selected.SplineCollisionMode = selected.SplineCollisionMode == "RoundedEdge" ? "SolidPolygon" : "RoundedEdge";
            ApplySpline(selected);
            SetStatus("Collision: " + selected.SplineCollisionMode);
        }

        private static void ApplySpline(CollaborativeLevelObject marker)
        {
            CollaborativeSplineUtility.Apply(marker.gameObject, marker.SplinePoints, marker.SplineWidth, marker.SplineClosed, marker.SplineCollisionMode);
        }

        private static void ApplyTrackIntersection(CollaborativeLevelObject marker)
        {
            if (marker == null || marker.TrackIntersection == null) return;
            TrackIntersectionEditable2D editable = marker.GetComponent<TrackIntersectionEditable2D>();
            if (editable != null) editable.Apply(marker.TrackIntersection);
        }

        private static void ApplyRecordSize(GameObject instance, CollaborativeObjectRecord record)
        {
            if (record.hasSize && SizeEditable2DUtility.TryGet(instance, out ISizeEditable2D editable))
                editable.SetSize(record.size);
        }

        private static void SetInput(TMP_InputField input, float value, bool interactable)
        {
            if (input == null) return;
            input.interactable = interactable;
            input.SetTextWithoutNotify(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static bool TryRead(TMP_InputField input, out float value)
        {
            return float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void SetPhysics(GameObject target, bool simulated)
        {
            Rigidbody2D[] bodies = target.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < bodies.Length; i++) bodies[i].simulated = simulated;
        }

        private Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector3 result = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            return result;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }

        private void RebuildHierarchy()
        {
            CollaborativeLevelObject[] objects = worldRoot != null
                ? worldRoot.GetComponentsInChildren<CollaborativeLevelObject>(true)
                : Array.Empty<CollaborativeLevelObject>();
            Array.Sort(objects, (left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            RebuildHierarchy(objects);
        }

        private void RebuildHierarchy(IList<CollaborativeLevelObject> objects)
        {
            if (hierarchyContent == null || hierarchyItemTemplate == null) return;
            float scrollPosition = hierarchyScrollRect != null ? hierarchyScrollRect.verticalNormalizedPosition : 1f;
            ClearHierarchyItems();
            for (int i = 0; i < objects.Count; i++)
            {
                CollaborativeLevelObject marker = objects[i];
                if (marker == null) continue;
                Button row = Instantiate(hierarchyItemTemplate, hierarchyContent, false);
                row.name = "HierarchyItem_" + marker.Id;
                row.gameObject.SetActive(true);
                row.onClick.RemoveAllListeners();
                CollaborativeLevelObject captured = marker;
                row.onClick.AddListener(() =>
                {
                    if (!simulating && captured != null) Select(captured);
                });
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = marker.Id;
                row.interactable = !simulating;
                hierarchyRows[marker] = row;
                hierarchyRowColors[row] = row.colors;
            }
            if (hierarchyScrollRect != null) hierarchyScrollRect.verticalNormalizedPosition = scrollPosition;
            RefreshHierarchySelection();
        }

        private void ClearHierarchyItems()
        {
            foreach (KeyValuePair<CollaborativeLevelObject, Button> pair in hierarchyRows)
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    Destroy(pair.Value.gameObject);
                }
            hierarchyRows.Clear();
            hierarchyRowColors.Clear();
        }

        private void RemoveHierarchyItem(CollaborativeLevelObject marker)
        {
            if (marker == null || !hierarchyRows.TryGetValue(marker, out Button row)) return;
            hierarchyRows.Remove(marker);
            if (row != null)
            {
                hierarchyRowColors.Remove(row);
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
        }

        private void RefreshHierarchySelection()
        {
            foreach (KeyValuePair<CollaborativeLevelObject, Button> pair in hierarchyRows)
            {
                Button row = pair.Value;
                if (row == null || !hierarchyRowColors.TryGetValue(row, out ColorBlock original)) continue;
                ColorBlock colors = original;
                if (pair.Key == selected)
                {
                    colors.normalColor = original.selectedColor;
                    colors.highlightedColor = original.selectedColor;
                }
                row.colors = colors;
            }
        }

        private void SetHierarchyInteractable(bool interactable)
        {
            foreach (Button row in hierarchyRows.Values)
                if (row != null) row.interactable = interactable;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel.GetComponent<RectTransform>();
        }

        private static RectTransform CreatePlacedHierarchyUi(
            Transform canvasRoot,
            RectTransform palette,
            out RectTransform content,
            out Button itemTemplate,
            out ScrollRect scrollRect)
        {
            RectTransform panel = CreatePanel(canvasRoot, "PlacedHierarchy", new Color(0.96f, 0.95f, 0.90f, 0.97f));
            ConfigurePlacedHierarchyRect(panel, palette);
            TMP_Text title = AddText(panel, "PlacedHierarchyTitle", Vector2.zero, Vector2.zero, 24f, TextAlignmentOptions.Center);
            title.text = "HIERARCHY";
            title.color = new Color(0.12f, 0.14f, 0.16f, 1f);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(10f, -52f);
            titleRect.offsetMax = new Vector2(-10f, -8f);

            GameObject scrollObject = new GameObject("PlacedHierarchyScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel, false);
            RectTransform scrollTransform = scrollObject.GetComponent<RectTransform>();
            scrollTransform.anchorMin = Vector2.zero;
            scrollTransform.anchorMax = Vector2.one;
            scrollTransform.offsetMin = new Vector2(10f, 10f);
            scrollTransform.offsetMax = new Vector2(-22f, -58f);
            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(1f, 1f, 1f, 0.001f);
            scrollImage.raycastTarget = true;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            StretchToParent(viewport);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = new GameObject("PlacedHierarchyContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            itemTemplate = AddButton(content, "HierarchyItemTemplate", Vector2.zero, () => { });
            itemTemplate.gameObject.name = "HierarchyItemTemplate";
            RectTransform templateRect = itemTemplate.GetComponent<RectTransform>();
            templateRect.sizeDelta = new Vector2(230f, 44f);
            TMP_Text templateLabel = itemTemplate.GetComponentInChildren<TMP_Text>(true);
            if (templateLabel != null) templateLabel.text = "object_id";
            itemTemplate.gameObject.SetActive(false);

            GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(panel, false);
            RectTransform scrollbarTransform = scrollbarObject.GetComponent<RectTransform>();
            scrollbarTransform.anchorMin = new Vector2(1f, 0f);
            scrollbarTransform.anchorMax = new Vector2(1f, 1f);
            scrollbarTransform.pivot = new Vector2(1f, 0.5f);
            scrollbarTransform.offsetMin = new Vector2(-18f, 10f);
            scrollbarTransform.offsetMax = new Vector2(-6f, -58f);
            scrollbarObject.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.18f, 0.32f);

            GameObject slidingObject = new GameObject("Sliding Area", typeof(RectTransform));
            slidingObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform sliding = slidingObject.GetComponent<RectTransform>();
            sliding.anchorMin = Vector2.zero;
            sliding.anchorMax = Vector2.one;
            sliding.offsetMin = new Vector2(2f, 2f);
            sliding.offsetMax = new Vector2(-2f, -2f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObject.transform.SetParent(sliding, false);
            RectTransform handle = handleObject.GetComponent<RectTransform>();
            StretchToParent(handle);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.36f, 0.38f, 0.42f, 0.9f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;

            scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = 1f;
            return panel;
        }

        private static void ConfigurePlacedHierarchyRect(RectTransform panel, RectTransform palette)
        {
            if (panel == null) return;
            float paletteHeight = palette != null && palette.rect.height > 0f ? palette.rect.height : PaletteHeight;
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.offsetMin = new Vector2(0f, paletteHeight);
            panel.offsetMax = new Vector2(PlacedHierarchyWidth, -TopBarHeight);
        }

        private static void ConfigureBottomPaletteRect(RectTransform palette)
        {
            palette.anchorMin = new Vector2(0f, 0f);
            palette.anchorMax = new Vector2(1f, 0f);
            palette.pivot = new Vector2(0.5f, 0f);
            palette.offsetMin = Vector2.zero;
            palette.offsetMax = new Vector2(-InspectorWidth, PaletteHeight);
        }

        private static RectTransform CreatePaletteScrollView(Transform palette)
        {
            GameObject scrollObject = new GameObject("ToolScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(palette, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(130f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -12f);
            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = true;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            StretchToParent(viewport);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            GridLayoutGroup layout = contentObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(PaletteCellWidth, PaletteCellHeight);
            layout.spacing = new Vector2(PaletteCellSpacing, PaletteCellSpacing);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(palette, false);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-20f, 12f);
            scrollbarRect.offsetMax = new Vector2(-8f, -12f);
            scrollbarObject.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.18f, 0.32f);

            GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObject.transform.SetParent(slidingArea, false);
            RectTransform handle = handleObject.GetComponent<RectTransform>();
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.36f, 0.38f, 0.42f, 0.9f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = 1f;
            return content;
        }

        private static Button AddButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = GetChildAnchor(parent);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(116f, 46f);
            buttonObject.GetComponent<Image>().color = new Color(0.90f, 0.72f, 0.28f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            TMP_Text text = AddText(buttonObject.transform, "Text", Vector2.zero, rect.sizeDelta, 18f, TextAlignmentOptions.Center);
            text.text = label;
            return button;
        }

        private static TMP_Text AddText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = GetChildAnchor(parent);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_InputField AddInput(Transform parent, string placeholder, Vector2 position, UnityEngine.Events.UnityAction<string> submitted)
        {
            GameObject root = new GameObject(placeholder + "Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = GetChildAnchor(parent);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(125f, 44f);
            root.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.2f, 1f);

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(7f, 2f);
            viewport.offsetMax = new Vector2(-7f, -2f);

            TMP_Text text = AddText(viewport, "Text", Vector2.zero, Vector2.zero, 17f, TextAlignmentOptions.MidlineLeft);
            StretchToParent(text.rectTransform);
            TMP_Text hint = AddText(viewport, "Placeholder", Vector2.zero, Vector2.zero, 15f, TextAlignmentOptions.MidlineLeft);
            StretchToParent(hint.rectTransform);
            hint.text = placeholder;
            hint.color = new Color(1f, 1f, 1f, 0.45f);
            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = hint;
            input.onEndEdit.AddListener(submitted);
            return input;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Vector2 GetChildAnchor(Transform parent)
        {
            if (parent.GetComponent<Button>() != null || parent.GetComponent<TMP_InputField>() != null)
                return new Vector2(0.5f, 0.5f);
            return parent.name == "TopBar" ? new Vector2(0f, 0.5f) : new Vector2(0f, 1f);
        }

        private static string SanitizeFilename(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "level";
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars()) value = value.Replace(invalid.ToString(), string.Empty);
            return string.IsNullOrWhiteSpace(value) ? "level" : value.Trim().Replace(' ', '_');
        }
    }
}
