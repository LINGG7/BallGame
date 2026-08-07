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
        private const string BackgroundResourcePath = "Prefabs/WorldBackground";
        private const float DefaultCameraSize = 7f;
        private const float CanvasPadding = 2f;
        private const float MinimumCanvasWidth = 24f;
        private const float MinimumCanvasHeight = 14f;
        private readonly Stack<string> undo = new Stack<string>();
        private readonly Stack<string> redo = new Stack<string>();
        private readonly List<SpriteRenderer> selectedRenderers = new List<SpriteRenderer>();
        private readonly List<Color> selectedRendererColors = new List<Color>();
        private readonly List<LineRenderer> originMarkerLines = new List<LineRenderer>();
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
            statusText = AddText(top, "Status", new Vector2(1220f, 0f), new Vector2(880f, 50f), 20f, TextAlignmentOptions.MidlineRight);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.offsetMin = new Vector2(920f, 8f);
            statusRect.offsetMax = new Vector2(-20f, -8f);

            RectTransform palette = CreatePanel(canvas.transform, "Palette", new Color(0.96f, 0.95f, 0.90f, 0.97f));
            Anchor(palette, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(270f, -68f), new Vector2(0f, -34f));
            AddText(palette, "Title", new Vector2(135f, -34f), new Vector2(240f, 44f), 26f, TextAlignmentOptions.Center);
            palette.Find("Title").GetComponent<TMP_Text>().text = "OBJECTS";
            if (registry != null)
            {
                for (int i = 0; i < registry.Entries.Count; i++)
                {
                    CollaborativeLevelAssetEntry captured = registry.Entries[i];
                    Button button = AddButton(palette, captured.assetKey, new Vector2(135f, -92f - i * 58f), () => AddObject(captured));
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(230f, 48f);
                }
            }

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
            titleInput.SetTextWithoutNotify(document.title);
            authorInput.SetTextWithoutNotify(document.author);
            RefreshSelection();
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

            statusText = FindComponent<TMP_Text>(root, "Status");
            ConfigureStatusRect(statusText);
            selectionText = FindComponent<TMP_Text>(root, "Selection");
            titleInput = BindInput(root, "Level titleInput", value => document.title = value);
            authorInput = BindInput(root, "AuthorInput", value => document.author = value);
            xInput = BindInput(root, "XInput", _ => ApplyInspectorTransform());
            yInput = BindInput(root, "YInput", _ => ApplyInspectorTransform());
            rotationInput = BindInput(root, "RotationInput", _ => ApplyInspectorTransform());
            sizeXInput = BindInput(root, "Size XInput", _ => ApplyInspectorTransform());
            sizeYInput = BindInput(root, "Size YInput", _ => ApplyInspectorTransform());
            splineWidthInput = BindInput(root, "Path widthInput", _ => ApplyInspectorTransform());

            BindButton(root, "NewButton", NewDocument);
            BindButton(root, "ImportButton", RequestImport);
            BindButton(root, "ExportButton", ExportJson);
            BindButton(root, "UndoButton", Undo);
            BindButton(root, "RedoButton", Redo);
            BindButton(root, "ResetButton", ResetView);
            BindButton(root, "TestButton", ToggleSimulation);
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
            return true;
        }

        private static void ConfigureStatusRect(TMP_Text status)
        {
            if (status == null) return;
            RectTransform rect = status.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(920f, 8f);
            rect.offsetMax = new Vector2(-20f, -8f);
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
                CollaborativeLevelObject marker = FindObjectAt(world, hits);
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
                if (owner != null && draggedSplineHandle.PointIndex < owner.SplinePoints.Count)
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

        private CollaborativeLevelObject FindObjectAt(Vector2 world, Collider2D[] hits)
        {
            CollaborativeLevelObject best = null;
            int bestSortingOrder = int.MinValue;
            for (int i = 0; i < hits.Length; i++)
            {
                CollaborativeLevelObject candidate = hits[i].GetComponentInParent<CollaborativeLevelObject>();
                ConsiderCandidate(candidate, ref best, ref bestSortingOrder);
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
                    ConsiderCandidate(objects[i], ref best, ref bestSortingOrder);
                    break;
                }
            }
            return best;
        }

        private static void ConsiderCandidate(CollaborativeLevelObject candidate, ref CollaborativeLevelObject best, ref int bestSortingOrder)
        {
            if (candidate == null) return;
            SpriteRenderer[] renderers = candidate.GetComponentsInChildren<SpriteRenderer>(true);
            int sortingOrder = int.MinValue + 1;
            for (int i = 0; i < renderers.Length; i++) sortingOrder = Mathf.Max(sortingOrder, renderers[i].sortingOrder);
            if (best != null && sortingOrder < bestSortingOrder) return;
            best = candidate;
            bestSortingOrder = sortingOrder;
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
            PushUndo();
            LoadDocument(new CollaborativeLevelDocument());
            SetStatus("New level");
        }

        private void DeleteSelected()
        {
            if (selected == null || simulating) return;
            PushUndo();
            GameObject target = selected.gameObject;
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
            Select(marker);
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
            document.formatVersion = 3;
            document.objects.Clear();
            document.toolStock.Clear();
            Dictionary<string, int> stock = new Dictionary<string, int>(StringComparer.Ordinal);
            CollaborativeLevelObject[] objects = worldRoot.GetComponentsInChildren<CollaborativeLevelObject>(true);
            UpdateCanvasBounds(objects);
            for (int i = 0; i < objects.Length; i++)
            {
                document.objects.Add(objects[i].ToRecord());
                if (objects[i].Kind == CollaborativeObjectKind.Tool)
                    stock[objects[i].AssetKey] = stock.TryGetValue(objects[i].AssetKey, out int count) ? count + 1 : 1;
            }
            foreach (KeyValuePair<string, int> item in stock)
                document.toolStock.Add(new CollaborativeToolStock { assetKey = item.Key, count = item.Value });
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
            for (int i = worldRoot.childCount - 1; i >= 0; i--) Destroy(worldRoot.GetChild(i).gameObject);
            document = source;
            if (titleInput != null) titleInput.SetTextWithoutNotify(document.title);
            if (authorInput != null) authorInput.SetTextWithoutNotify(document.author);
            nextObjectId = 1;
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
                SetPhysics(instance, false);
                nextObjectId++;
            }
            Select(null);
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
            selected = marker;
            ApplySelectionOpacity();
            RefreshSelection();
            RebuildSplineHandles();
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
            if (selected == null || selected.Kind != CollaborativeObjectKind.SplineTool || simulating) return;
            PushUndo();
            Vector2 position = selected.SplinePoints.Count == 0 ? Vector2.zero : selected.SplinePoints[selected.SplinePoints.Count - 1].position + Vector2.right;
            selected.SplinePoints.Add(NewSplinePoint(position.x, position.y));
            ApplySpline(selected);
            RebuildSplineHandles();
        }

        private void RemoveSplinePoint()
        {
            if (selected == null || selected.Kind != CollaborativeObjectKind.SplineTool || selected.SplinePoints.Count <= 2 || simulating) return;
            PushUndo();
            selected.SplinePoints.RemoveAt(selected.SplinePoints.Count - 1);
            ApplySpline(selected);
            RebuildSplineHandles();
        }

        private void RebuildSplineHandles()
        {
            if (splineHandleRoot == null) return;
            for (int i = splineHandleRoot.childCount - 1; i >= 0; i--) Destroy(splineHandleRoot.GetChild(i).gameObject);
            if (selected == null || selected.Kind != CollaborativeObjectKind.SplineTool) return;
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

        private void UpdateSplineHandlePositions()
        {
            if (selected == null || selected.Kind != CollaborativeObjectKind.SplineTool || splineHandleRoot == null) return;
            CollaborativeSplineHandle[] handles = splineHandleRoot.GetComponentsInChildren<CollaborativeSplineHandle>(true);
            for (int i = 0; i < handles.Length; i++)
            {
                CollaborativeSplineHandle handle = handles[i];
                if (handle.Owner != selected || handle.PointIndex < 0 || handle.PointIndex >= selected.SplinePoints.Count) continue;
                handle.transform.position = selected.transform.TransformPoint(selected.SplinePoints[handle.PointIndex].position);
            }
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
            if (selectionText == null) return;
            selectionText.text = selected == null
                ? "No selection"
                : selected.Id + "\n" + selected.AssetKey + "\nPosition: " + (Vector2)selected.transform.position + "\nRotation: " + selected.transform.localEulerAngles.z.ToString("0");
            bool hasSelection = selected != null;
            SetInput(xInput, hasSelection ? selected.transform.position.x : 0f, hasSelection);
            SetInput(yInput, hasSelection ? selected.transform.position.y : 0f, hasSelection);
            SetInput(rotationInput, hasSelection ? selected.transform.localEulerAngles.z : 0f, hasSelection);
            ISizeEditable2D editable = null;
            bool sizeEditable = hasSelection && SizeEditable2DUtility.TryGet(selected.gameObject, out editable);
            SetInput(sizeXInput, sizeEditable ? editable.Size.x : 1f, sizeEditable);
            SetInput(sizeYInput, sizeEditable ? editable.Size.y : 1f, sizeEditable);
            bool splineSelected = hasSelection && selected.Kind == CollaborativeObjectKind.SplineTool;
            SetInput(splineWidthInput, splineSelected ? selected.SplineWidth : 0.5f, splineSelected);
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
            if (selected.Kind == CollaborativeObjectKind.SplineTool
                && (!TryRead(splineWidthInput, out splineWidth) || splineWidth <= 0f)) return;
            PushUndo();
            selected.transform.position = new Vector3(x, y, 0f);
            selected.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            if (sizeEditable) editable.SetSize(new Vector2(sizeX, sizeY));
            if (selected.Kind == CollaborativeObjectKind.SplineTool)
            {
                selected.SplineWidth = splineWidth;
                ApplySpline(selected);
            }
            RebuildSplineHandles();
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

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel.GetComponent<RectTransform>();
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
