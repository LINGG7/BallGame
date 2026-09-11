using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BallGame.LevelDesign;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class CollaborativeLevelDesignerSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LevelDesigner.unity";
        private const string UiPrefabPath = "Assets/LevelDesigner/DesignerUI.prefab";
        private const string DesignerFontFolder = "Assets/LevelDesigner/Fonts";
        private const string DesignerFontPath = DesignerFontFolder + "/LXGWWenKai-Light Dynamic SDF.asset";
        private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LXGWWenKai-Light.ttf";
        private const string LevelSettingsPrefabPath = "Assets/LevelDesigner/LevelSettingsPanel.prefab";
        private const string TrackIntersectionPrefabPath = "Assets/Resources/Prefabs/TrackIntersection.prefab";
        private const string BrowserWebGlDefine = "BALLGAME_BROWSER_WEBGL";
        private static readonly string[] TapBrowserPluginPaths =
        {
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jslib",
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jspre",
            "Packages/com.taptap.minigame/Runtime/Plugins/TapSDK.jslib",
            "Packages/com.taptap.minigame/Runtime/Plugins/minihost-plugin.taptap.runtime.dll",
            "Packages/com.taptap.minigame/Runtime/Plugins/TapExtManagerHandler.dll"
        };
        private static readonly HashSet<string> TapBrowserPluginsRequiringReimport = new HashSet<string>
        {
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jslib",
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jspre",
            "Packages/com.taptap.minigame/Runtime/Plugins/TapSDK.jslib"
        };

        [InitializeOnLoadMethod]
        private static void QueueCreateMissingAssets()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                CollaborativeLevelAssetRegistry registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>("Assets/LevelDesigner/CollaborativeLevelAssetRegistry.asset");
                EnsureTrackIntersectionPrefabEditable();
                if (registry == null || RegistryNeedsRefresh(registry))
                {
                    CollaborativeLevelRegistryBuilder.CreateRegistry();
                    registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>("Assets/LevelDesigner/CollaborativeLevelAssetRegistry.asset");
                }
                CreateMissingDesignerDynamicFont();
                EnsureDesignerTitleFont();
                CreateMissingLevelSettingsPanelPrefab();
                EnsureLevelSettingsUi();
                EnsureTrackIntersectionUi();
                EnsurePaletteScrollView();
                EnsurePlacedHierarchyUi();
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                    CreateScene();
                EnsureDesignerUiInstance();
            };
        }

        private static bool RegistryNeedsRefresh(CollaborativeLevelAssetRegistry registry)
        {
            // Non-inventory objects legitimately have no ToolDefinition.
            CollaborativeLevelAssetEntry track = registry.Find("track_intersection");
            return track == null || track.prefab == null;
        }

        [MenuItem("Ball Game/Level Design/Upgrade Track Intersection Editable Prefab")]
        private static void EnsureTrackIntersectionPrefabEditable()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TrackIntersectionPrefabPath);
            if (prefab == null) return;
            TrackIntersectionEditable2D existing = prefab.GetComponent<TrackIntersectionEditable2D>();
            if (existing != null && existing.IsConfigured) return;

            GameObject root = PrefabUtility.LoadPrefabContents(TrackIntersectionPrefabPath);
            try
            {
                Transform upper = FindChild(root.transform, "Upper");
                Transform lower = FindChild(root.transform, "Lower");
                Transform upperEntry = FindChild(root.transform, "Upper Entry A");
                Transform lowerEntry = FindChild(root.transform, "Lower Entry A");
                if (upper == null || lower == null || upperEntry == null || lowerEntry == null)
                    throw new System.InvalidOperationException("TrackIntersection is missing Upper, Lower, Upper Entry A, or Lower Entry A.");

                TrackIntersectionEditable2D editable = root.GetComponent<TrackIntersectionEditable2D>()
                    ?? root.AddComponent<TrackIntersectionEditable2D>();
                SerializedObject serialized = new SerializedObject(editable);
                AssignObject(serialized, "upperSpline", upper.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true));
                AssignObject(serialized, "upperMesh", upper.GetComponentInChildren<BallGame.Core.SplineMesh2DGenerator>(true));
                AssignObject(serialized, "upperCollider", upper.GetComponentInChildren<BallGame.Core.SplineCollider2DGenerator>(true));
                AssignObject(serialized, "lowerSpline", lower.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true));
                AssignObject(serialized, "lowerMesh", lower.GetComponentInChildren<BallGame.Core.SplineMesh2DGenerator>(true));
                AssignObject(serialized, "lowerCollider", lower.GetComponentInChildren<BallGame.Core.SplineCollider2DGenerator>(true));
                AssignObject(serialized, "upperEntryA", upperEntry);
                AssignObject(serialized, "lowerEntryA", lowerEntry);
                AssignObject(serialized, "layerSwitcher", root.GetComponent<BallGame.Core.TrackLayerSwitcher2D>());
                AssignObject(serialized, "upperEntryTrigger", upperEntry.GetComponent<BallGame.Core.TrackLayerSwitchTrigger2D>());
                AssignObject(serialized, "lowerEntryTrigger", lowerEntry.GetComponent<BallGame.Core.TrackLayerSwitchTrigger2D>());
                AssignObject(serialized, "rendererOrder", root.GetComponent<BallGame.Core.TrackIntersectionRendererOrder2D>());
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!editable.IsConfigured)
                    throw new System.InvalidOperationException("TrackIntersection editable references could not be configured.");
                PrefabUtility.SaveAsPrefabAsset(root, TrackIntersectionPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignObject(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            if (value == null) throw new System.InvalidOperationException("Missing TrackIntersection reference: " + propertyName);
            target.FindProperty(propertyName).objectReferenceValue = value;
        }

        [MenuItem("Ball Game/Level Design/Create Missing Designer Dynamic Font")]
        private static void CreateMissingDesignerDynamicFont()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DesignerFontPath) != null) return;

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
                throw new System.InvalidOperationException("Level Designer source font is missing: " + SourceFontPath);

            if (!AssetDatabase.IsValidFolder(DesignerFontFolder))
                AssetDatabase.CreateFolder("Assets/LevelDesigner", "Fonts");

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
                throw new System.InvalidOperationException("Could not create the Level Designer dynamic font asset.");

            fontAsset.name = "LXGWWenKai-Light Dynamic SDF";
            fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.CreateAsset(fontAsset, DesignerFontPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("Created Level Designer-only dynamic font: " + DesignerFontPath);
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer Title Chinese Font")]
        private static void EnsureDesignerTitleFont()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DesignerFontPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (fontAsset == null || prefab == null) return;

            TMP_InputField current = FindChild(prefab.transform, "Level titleInput")?.GetComponent<TMP_InputField>();
            TMP_Text currentPlaceholder = current != null ? current.placeholder as TMP_Text : null;
            if (current != null
                && current.textComponent != null
                && current.textComponent.font == fontAsset
                && (currentPlaceholder == null || currentPlaceholder.font == fontAsset)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                TMP_InputField title = FindChild(root.transform, "Level titleInput")?.GetComponent<TMP_InputField>();
                if (title == null || title.textComponent == null)
                    throw new System.InvalidOperationException("DesignerUI is missing Level titleInput.");

                title.contentType = TMP_InputField.ContentType.Standard;
                title.characterValidation = TMP_InputField.CharacterValidation.None;
                title.lineType = TMP_InputField.LineType.SingleLine;
                title.textComponent.font = fontAsset;
                if (title.placeholder is TMP_Text placeholder) placeholder.font = fontAsset;
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Ball Game/Level Design/Create Missing Level Settings Panel Prefab")]
        private static void CreateMissingLevelSettingsPanelPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LevelSettingsPrefabPath) != null) return;
            CollaborativeLevelAssetRegistry registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>("Assets/LevelDesigner/CollaborativeLevelAssetRegistry.asset");
            if (registry == null) throw new System.InvalidOperationException("Create the collaborative asset registry before the Level Settings prefab.");

            GameObject stagingCanvas = new GameObject("Level Settings Prefab Staging", typeof(Canvas));
            GameObject host = new GameObject("Level Settings Builder Host");
            host.transform.SetParent(stagingCanvas.transform, false);
            host.SetActive(false);
            try
            {
                CollaborativeLevelDesignerController controller = host.AddComponent<CollaborativeLevelDesignerController>();
                System.Type controllerType = typeof(CollaborativeLevelDesignerController);
                controllerType.GetField("registry", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, registry);
                controllerType.GetMethod("BuildLevelSettingsPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller, new object[] { stagingCanvas.transform });
                GameObject panel = (GameObject)controllerType.GetField("levelSettingsPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
                if (panel == null) throw new System.InvalidOperationException("The Level Settings panel hierarchy could not be built.");
                panel.name = "LevelSettingsPanel";
                PrefabUtility.SaveAsPrefabAsset(panel, LevelSettingsPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created " + LevelSettingsPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stagingCanvas);
            }
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer UI Level Settings")]
        private static void EnsureLevelSettingsUi()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null || FindChild(prefab.transform, "Level SettingsButton") != null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                Transform topBar = FindChild(root.transform, "TopBar");
                Button source = FindChild(root.transform, "TestButton")?.GetComponent<Button>();
                if (topBar == null || source == null)
                    throw new System.InvalidOperationException("DesignerUI is missing TopBar or TestButton.");

                GameObject buttonObject = UnityEngine.Object.Instantiate(source.gameObject, topBar);
                buttonObject.name = "Level SettingsButton";
                SetPosition(buttonObject.transform, new Vector2(1010f, 0f));
                TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "Level Settings";
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(150f, rect.sizeDelta.y);
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer UI Track Intersection")]
        private static void EnsureTrackIntersectionUi()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null) return;
            bool needsButton = FindChild(prefab.transform, "track_intersectionButton") == null;
            bool needsControls = FindChild(prefab.transform, "Track PartDropdown") == null
                || FindChild(prefab.transform, "Entry XInput") == null
                || FindChild(prefab.transform, "Entry YInput") == null
                || FindChild(prefab.transform, "Entry RotationInput") == null;
            if (!needsButton && !needsControls) return;

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                Transform palette = FindChild(root.transform, "Palette");
                Transform inspector = FindChild(root.transform, "Inspector");
                if (palette == null || inspector == null)
                    throw new System.InvalidOperationException("DesignerUI is missing Palette or Inspector.");

                if (needsButton)
                {
                    Button source = FindChild(root.transform, "curve_obstacleButton")?.GetComponent<Button>();
                    if (source == null) throw new System.InvalidOperationException("DesignerUI is missing curve_obstacleButton.");
                    GameObject buttonObject = UnityEngine.Object.Instantiate(source.gameObject, palette);
                    buttonObject.name = "track_intersectionButton";
                    SetPosition(buttonObject.transform, new Vector2(135f, -846f));
                    TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.text = "track_intersection";
                }

                System.Type controllerType = typeof(CollaborativeLevelDesignerController);
                MethodInfo addDropdown = controllerType.GetMethod("AddDropdown", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo addInput = controllerType.GetMethod("AddInput", BindingFlags.NonPublic | BindingFlags.Static);
                if (addDropdown == null || addInput == null)
                    throw new System.InvalidOperationException("Designer UI creation helpers were not found.");

                if (FindChild(root.transform, "Track PartDropdown") == null)
                    addDropdown.Invoke(null, new object[]
                    {
                        inspector,
                        "Track Part",
                        new Vector2(150f, -720f),
                        new Vector2(260f, 44f),
                        new[] { "Upper Curve", "Lower Curve", "Upper Entry A", "Lower Entry A" }
                    });

                UnityEngine.Events.UnityAction<string> noOp = _ => { };
                if (FindChild(root.transform, "Entry XInput") == null)
                    addInput.Invoke(null, new object[] { inspector, "Entry X", new Vector2(80f, -775f), noOp });
                if (FindChild(root.transform, "Entry YInput") == null)
                    addInput.Invoke(null, new object[] { inspector, "Entry Y", new Vector2(220f, -775f), noOp });
                if (FindChild(root.transform, "Entry RotationInput") == null)
                    addInput.Invoke(null, new object[] { inspector, "Entry Rotation", new Vector2(150f, -830f), noOp });

                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer UI Bottom Palette Grid")]
        private static void EnsurePaletteScrollView()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null) return;
            Transform currentPalette = FindChild(prefab.transform, "Palette");
            Transform currentScroll = currentPalette != null ? currentPalette.Find("ToolScrollView") : null;
            Transform currentContent = currentScroll != null ? currentScroll.Find("Viewport/Content") : null;
            RectTransform currentPaletteRect = currentPalette as RectTransform;
            if (currentContent != null && currentScroll.GetComponent<ScrollRect>() != null
                && FindChild(currentPalette, "Scrollbar")?.GetComponent<Scrollbar>() != null
                && currentContent.GetComponent<GridLayoutGroup>() != null
                && currentContent.GetComponent<VerticalLayoutGroup>() == null
                && currentPaletteRect != null
                && currentPaletteRect.anchorMin == new Vector2(0f, 0f)
                && currentPaletteRect.anchorMax == new Vector2(1f, 0f)
                && Mathf.Approximately(currentPaletteRect.sizeDelta.x, -350f)
                && !HasDirectPaletteButtons(currentPalette)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                Transform palette = FindChild(root.transform, "Palette");
                if (palette == null) throw new System.InvalidOperationException("DesignerUI is missing Palette.");
                ConfigureBottomPalette(palette as RectTransform);
                RectTransform title = FindChild(palette, "Title") as RectTransform;
                if (title != null)
                {
                    title.anchorMin = title.anchorMax = new Vector2(0f, 0f);
                    title.pivot = new Vector2(0.5f, 0.5f);
                    title.anchoredPosition = new Vector2(65f, 60f);
                    title.sizeDelta = new Vector2(110f, 44f);
                }
                RectTransform content = GetOrCreatePaletteScrollHierarchy(palette);

                CollaborativeLevelAssetRegistry registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>(
                    "Assets/LevelDesigner/CollaborativeLevelAssetRegistry.asset");
                HashSet<Transform> moved = new HashSet<Transform>();
                if (registry != null)
                {
                    foreach (CollaborativeLevelAssetEntry entry in registry.Entries)
                    {
                        if (entry == null) continue;
                        Transform button = FindChild(palette, entry.assetKey + "Button");
                        if (button == null) continue;
                        ConfigurePaletteButton(button, content);
                        moved.Add(button);
                    }
                }

                List<Transform> remainingButtons = new List<Transform>();
                for (int i = 0; i < palette.childCount; i++)
                {
                    Transform child = palette.GetChild(i);
                    if (child.GetComponent<Button>() != null && !moved.Contains(child)) remainingButtons.Add(child);
                }
                for (int i = 0; i < remainingButtons.Count; i++) ConfigurePaletteButton(remainingButtons[i], content);

                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool HasDirectPaletteButtons(Transform palette)
        {
            if (palette == null) return false;
            for (int i = 0; i < palette.childCount; i++)
                if (palette.GetChild(i).GetComponent<Button>() != null) return true;
            return false;
        }

        private static RectTransform GetOrCreatePaletteScrollHierarchy(Transform palette)
        {
            Transform scrollTransform = palette.Find("ToolScrollView");
            GameObject scrollObject = scrollTransform != null ? scrollTransform.gameObject : new GameObject(
                "ToolScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            if (scrollTransform == null) scrollObject.transform.SetParent(palette, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>() ?? scrollObject.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(130f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -12f);
            Image scrollImage = scrollObject.GetComponent<Image>() ?? scrollObject.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = true;

            Transform viewportTransform = scrollObject.transform.Find("Viewport");
            GameObject viewportObject = viewportTransform != null ? viewportTransform.gameObject : new GameObject(
                "Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            if (viewportTransform == null) viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>() ?? viewportObject.AddComponent<RectTransform>();
            StretchRect(viewport);
            Image viewportImage = viewportObject.GetComponent<Image>() ?? viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            if (viewportObject.GetComponent<RectMask2D>() == null) viewportObject.AddComponent<RectMask2D>();

            Transform contentTransform = viewportObject.transform.Find("Content");
            GameObject contentObject = contentTransform != null ? contentTransform.gameObject : new GameObject(
                "Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            if (contentTransform == null) contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>() ?? contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup verticalLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null) UnityEngine.Object.DestroyImmediate(verticalLayout);
            GridLayoutGroup layout = contentObject.GetComponent<GridLayoutGroup>() ?? contentObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(230f, 48f);
            layout.spacing = new Vector2(10f, 10f);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>() ?? contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Transform scrollbarTransform = palette.Find("Scrollbar");
            GameObject scrollbarObject = scrollbarTransform != null ? scrollbarTransform.gameObject : new GameObject(
                "Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            if (scrollbarTransform == null) scrollbarObject.transform.SetParent(palette, false);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>() ?? scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-20f, 12f);
            scrollbarRect.offsetMax = new Vector2(-8f, -12f);
            Image scrollbarImage = scrollbarObject.GetComponent<Image>() ?? scrollbarObject.AddComponent<Image>();
            scrollbarImage.color = new Color(0.15f, 0.16f, 0.18f, 0.32f);

            Transform slidingTransform = scrollbarObject.transform.Find("Sliding Area");
            GameObject slidingObject = slidingTransform != null ? slidingTransform.gameObject : new GameObject("Sliding Area", typeof(RectTransform));
            if (slidingTransform == null) slidingObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingArea = slidingObject.GetComponent<RectTransform>() ?? slidingObject.AddComponent<RectTransform>();
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);

            Transform handleTransform = slidingObject.transform.Find("Handle");
            GameObject handleObject = handleTransform != null ? handleTransform.gameObject : new GameObject(
                "Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (handleTransform == null) handleObject.transform.SetParent(slidingArea, false);
            RectTransform handle = handleObject.GetComponent<RectTransform>() ?? handleObject.AddComponent<RectTransform>();
            StretchRect(handle);
            Image handleImage = handleObject.GetComponent<Image>() ?? handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.36f, 0.38f, 0.42f, 0.9f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>() ?? scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>() ?? scrollObject.AddComponent<ScrollRect>();
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

        private static void ConfigurePaletteButton(Transform button, RectTransform content)
        {
            button.SetParent(content, false);
            button.localScale = Vector3.one;
            RectTransform rect = button as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(230f, 48f);
            }
        }

        private static void ConfigureBottomPalette(RectTransform palette)
        {
            if (palette == null) throw new System.InvalidOperationException("DesignerUI Palette has no RectTransform.");
            float preservedHeight = palette.sizeDelta.y;
            palette.anchorMin = new Vector2(0f, 0f);
            palette.anchorMax = new Vector2(1f, 0f);
            palette.pivot = new Vector2(0.5f, 0f);
            palette.offsetMin = Vector2.zero;
            palette.offsetMax = new Vector2(-350f, preservedHeight);
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer UI Placed Hierarchy")]
        private static void EnsurePlacedHierarchyUi()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null) return;
            Transform currentPanel = FindChild(prefab.transform, "PlacedHierarchy");
            if (currentPanel != null
                && FindChild(currentPanel, "PlacedHierarchyScrollView")?.GetComponent<ScrollRect>() != null
                && FindChild(currentPanel, "PlacedHierarchyContent")?.GetComponent<VerticalLayoutGroup>() != null
                && FindChild(currentPanel, "HierarchyItemTemplate")?.GetComponent<Button>() != null)
                return;
            if (currentPanel != null)
            {
                Debug.LogError("DesignerUI PlacedHierarchy is incomplete. Repair its missing children instead of creating a duplicate panel.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                RectTransform palette = FindChild(root.transform, "Palette") as RectTransform;
                MethodInfo createHierarchy = typeof(CollaborativeLevelDesignerController).GetMethod(
                    "CreatePlacedHierarchyUi", BindingFlags.NonPublic | BindingFlags.Static);
                if (createHierarchy == null)
                    throw new System.InvalidOperationException("Placed Hierarchy UI creation helper was not found.");
                object[] arguments = { root.transform, palette, null, null, null };
                createHierarchy.Invoke(null, arguments);
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        [MenuItem("Ball Game/Level Design/Create Level Designer Scene")]
        public static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("Level designer scene already exists: " + ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.88f, 0.92f, 0.94f, 1f);
            GameObject designer = new GameObject("Level Designer", typeof(CollaborativeLevelDesignerController));
            SerializedObject controller = new SerializedObject(designer.GetComponent<CollaborativeLevelDesignerController>());
            controller.FindProperty("registry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>(
                "Assets/LevelDesigner/CollaborativeLevelAssetRegistry.asset");
            controller.FindProperty("levelSettingsPanelPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(LevelSettingsPrefabPath);
            controller.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath, false);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + ScenePath);
        }

        private static void EnsureDesignerUiInstance()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            bool exists = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "DesignerUI") continue;
                exists = true;
                break;
            }
            if (!exists)
            {
                PrefabUtility.InstantiatePrefab(prefab, scene);
                EditorSceneManager.SaveScene(scene, ScenePath, false);
            }
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer UI Size Fields")]
        private static void EnsureDesignerUiSizeFields()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null) return;
            if (FindChild(prefab.transform, "Size XInput") != null
                && FindChild(prefab.transform, "Size YInput") != null
                && FindChild(prefab.transform, "Size X Label") != null
                && FindChild(prefab.transform, "Size Y Label") != null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                Transform inspector = FindChild(root.transform, "Inspector");
                TMP_InputField sizeX = FindChild(root.transform, "Scale XInput")?.GetComponent<TMP_InputField>();
                TMP_InputField sizeY = FindChild(root.transform, "Scale YInput")?.GetComponent<TMP_InputField>();
                TMP_InputField rotation = FindChild(root.transform, "RotationInput")?.GetComponent<TMP_InputField>();
                TMP_InputField pathWidth = FindChild(root.transform, "Path widthInput")?.GetComponent<TMP_InputField>();
                Button closed = FindChild(root.transform, "ClosedButton")?.GetComponent<Button>();
                Button collision = FindChild(root.transform, "CollisionButton")?.GetComponent<Button>();
                if (inspector == null || sizeX == null || sizeY == null)
                    throw new System.InvalidOperationException("DesignerUI is missing the expected size input controls.");

                sizeX.name = "Size XInput";
                sizeY.name = "Size YInput";
                SetPosition(sizeX.transform, new Vector2(190f, -495f));
                SetPosition(sizeY.transform, new Vector2(190f, -550f));
                if (rotation != null) SetPosition(rotation.transform, new Vector2(150f, -440f));
                if (pathWidth != null) SetPosition(pathWidth.transform, new Vector2(150f, -610f));
                if (closed != null) SetPosition(closed.transform, new Vector2(80f, -665f));
                if (collision != null) SetPosition(collision.transform, new Vector2(220f, -665f));

                CreateSizeLabel(inspector, "Size X Label", "Size X", new Vector2(55f, -495f), sizeX.textComponent);
                CreateSizeLabel(inspector, "Size Y Label", "Size Y", new Vector2(55f, -550f), sizeY.textComponent);
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateSizeLabel(Transform parent, string name, string value, Vector2 position, TMP_Text source)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(80f, 30f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = source != null ? source.font : null;
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            label.raycastTarget = false;
        }

        private static void SetPosition(Transform target, Vector2 position)
        {
            if (target is RectTransform rect) rect.anchoredPosition = position;
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name == name) return children[i];
            return null;
        }

        [MenuItem("Ball Game/Level Design/Build WebGL Designer")]
        public static void BuildWebGLDesigner()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Build Stopped", "Create the Level Designer scene first.", "OK");
                return;
            }

            string output = EditorUtility.SaveFolderPanel("WebGL Designer Output", "Build", "LevelDesignerWebGL");
            if (string.IsNullOrEmpty(output)) return;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                subtarget = 0,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { BrowserWebGlDefine }
            };
            Dictionary<string, bool> tapPluginCompatibility = new Dictionary<string, bool>();
            BuildReport report;
            try
            {
                DisableTapBrowserPlugins(tapPluginCompatibility);
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                RestoreTapBrowserPlugins(tapPluginCompatibility);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build Failed", "The browser WebGL designer build did not complete successfully. Check the Console for details.", "OK");
                return;
            }

            string contaminatedFile = FindTapBridgeReference(output);
            if (!string.IsNullOrEmpty(contaminatedFile))
            {
                Debug.LogError("Browser WebGL build still contains a TapTap mini-game bridge: " + contaminatedFile);
                EditorUtility.DisplayDialog(
                    "Build Validation Failed",
                    "The output still contains TapTap mini-game bridge code and must not be uploaded to a browser host. Check the Console for details.",
                    "OK");
                return;
            }

            Debug.Log("Created browser-safe Level Designer WebGL build: " + output);
            EditorUtility.DisplayDialog("Build Complete", "Browser WebGL designer created successfully.\n\n" + output, "OK");
        }

        private static void DisableTapBrowserPlugins(Dictionary<string, bool> compatibility)
        {
            for (int i = 0; i < TapBrowserPluginPaths.Length; i++)
            {
                string path = TapBrowserPluginPaths[i];
                PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null)
                    throw new FileNotFoundException("TapTap browser plugin importer was not found.", path);

                bool wasCompatible = importer.GetCompatibleWithPlatform(BuildTarget.WebGL);
                compatibility[path] = wasCompatible;
                if (!wasCompatible) continue;
                importer.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
                if (TapBrowserPluginsRequiringReimport.Contains(path)) importer.SaveAndReimport();
            }
        }

        private static void RestoreTapBrowserPlugins(Dictionary<string, bool> compatibility)
        {
            foreach (KeyValuePair<string, bool> entry in compatibility)
            {
                PluginImporter importer = AssetImporter.GetAtPath(entry.Key) as PluginImporter;
                if (importer == null || importer.GetCompatibleWithPlatform(BuildTarget.WebGL) == entry.Value) continue;
                importer.SetCompatibleWithPlatform(BuildTarget.WebGL, entry.Value);
                if (TapBrowserPluginsRequiringReimport.Contains(entry.Key)) importer.SaveAndReimport();
            }
        }

        private static string FindTapBridgeReference(string output)
        {
            string[] frameworkFiles = Directory.GetFiles(output, "*.framework.js", SearchOption.AllDirectories);
            for (int i = 0; i < frameworkFiles.Length; i++)
            {
                string contents = File.ReadAllText(frameworkFiles[i]);
                if (contents.Contains("TJWASMSDK"))
                    return frameworkFiles[i];
            }

            string[] symbolFiles = Directory.GetFiles(output, "*.symbols.json", SearchOption.AllDirectories);
            for (int i = 0; i < symbolFiles.Length; i++)
            {
                string contents = File.ReadAllText(symbolFiles[i]);
                if (contents.Contains("TapSDKManagerHandler_")) return symbolFiles[i];
            }

            return null;
        }
    }
}
