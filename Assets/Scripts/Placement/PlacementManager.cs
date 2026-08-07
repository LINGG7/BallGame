using System.Collections.Generic;
using BallGame.Core;
using BallGame.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.Placement
{
    public sealed class PlacementManager : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform placedRoot;
        [SerializeField] private LayerMask blockedPlacementMask;
        [SerializeField] private float nudgePixels = 10f;

        private readonly Dictionary<ToolDefinition, int> remaining = new Dictionary<ToolDefinition, int>();
        private readonly HashSet<ToolDefinition> availableTools = new HashSet<ToolDefinition>();
        private readonly List<PlaceableTool> placedTools = new List<PlaceableTool>();
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private readonly Collider2D[] placementHits = new Collider2D[32];
        private ContactFilter2D placementFilter;

        private ToolDefinition activeDefinition;
        private PlaceableTool preview;
        private PlaceableTool selected;
        private PlaceableTool draggedPlacedTool;
        private Vector3 draggedOriginalPosition;
        private Vector2 draggedPointerOffset;
        private Vector2 pointerDownScreenPosition;
        private bool isSimulating;
        private bool interactionEnabled = true;
        private bool pointerWasDown;
        private bool pointerStartedOverUi;
        private bool unlimitedSupply;

        public event System.Action InventoryChanged;
        public event System.Action CostChanged;

        public bool HasUnlimitedSupply => unlimitedSupply;
        public int CurrentCost { get; private set; }

        public int GetRemaining(ToolDefinition definition)
        {
            if (unlimitedSupply && definition != null && availableTools.Contains(definition))
            {
                return int.MaxValue;
            }

            return definition != null && remaining.TryGetValue(definition, out int count) ? count : 0;
        }

        public bool CanPlace(ToolDefinition definition)
        {
            return definition != null && availableTools.Contains(definition) && (unlimitedSupply || GetRemaining(definition) > 0);
        }

        public bool TryGetPlaceableAtScreenPosition(Vector2 screenPosition, out PlaceableTool tool)
        {
            tool = FindPlaceableAt(ScreenToWorld(screenPosition));
            return tool != null;
        }

        private void Update()
        {
            if (isSimulating || !interactionEnabled)
            {
                return;
            }

            HandlePlacementPreview();
            HandleSelectionShortcuts();
        }

        private void Awake()
        {
            placementFilter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true
            };
            placementFilter.SetLayerMask(blockedPlacementMask);
        }

        public void ResetForLevel(LevelDefinition level)
        {
            ClearPlacedTools();
            remaining.Clear();
            availableTools.Clear();
            unlimitedSupply = level != null && level.Mode == LevelMode.LowestCost;
            if (level != null)
            {
                if (unlimitedSupply && level.ToolCatalog != null)
                {
                    foreach (ToolDefinition tool in level.ToolCatalog.Tools)
                    {
                        if (tool != null)
                        {
                            availableTools.Add(tool);
                        }
                    }
                }
                else
                {
                    foreach (LevelDefinition.ToolStock stock in level.Tools)
                    {
                        if (stock.tool != null)
                        {
                            availableTools.Add(stock.tool);
                            remaining[stock.tool] = Mathf.Max(0, stock.count);
                        }
                    }
                }
            }

            ClearPreview();
            Select(null);
            isSimulating = false;
            SetCurrentCost(0);
            InventoryChanged?.Invoke();
        }

        public void BeginPlaceDrag(ToolDefinition definition, Vector2 screenPosition)
        {
            if (!interactionEnabled || definition == null || definition.Prefab == null || !CanPlace(definition) || isSimulating)
            {
                return;
            }

            ClearPreview();
            Select(null);
            activeDefinition = definition;
            GameObject previewObject = Instantiate(definition.Prefab, placedRoot);
            preview = previewObject.GetComponent<PlaceableTool>();
            if (preview == null)
            {
                preview = previewObject.AddComponent<PlaceableTool>();
            }

            preview.Initialize(definition);
            AssignPortalPairId(preview);
            preview.transform.position = ScreenToWorld(screenPosition);
            preview.SetPreview(true, true);
        }

        public void UpdatePlaceDrag(Vector2 screenPosition)
        {
            if (interactionEnabled && preview != null)
            {
                preview.transform.position = ScreenToWorld(screenPosition);
            }
        }

        public void FinishPlaceDrag(Vector2 screenPosition, bool allowPlace)
        {
            if (preview == null)
            {
                return;
            }

            Vector2 worldPosition = ScreenToWorld(screenPosition);
            preview.transform.position = worldPosition;
            if (allowPlace && IsPlacementValidAfterSync(preview))
            {
                PlacePreview();
            }
            else
            {
                ClearPreview();
            }
        }

        public void RotateSelected(float direction)
        {
            if (!interactionEnabled)
            {
                return;
            }

            PlaceableTool target = preview != null ? preview : selected;
            if (target == null || target.Definition == null)
            {
                return;
            }

            if (target == preview)
            {
                target.Rotate(target.Definition.RotationStep * Mathf.Sign(direction));
                return;
            }

            if (target != selected || !selected.CanEdit || isSimulating)
            {
                return;
            }

            Vector3 originalPosition = selected.transform.position;
            Quaternion originalRotation = selected.transform.rotation;
            selected.Rotate(selected.Definition.RotationStep * Mathf.Sign(direction));
            TryCommitSelectedTransform(originalPosition, originalRotation);
        }

        public void DeleteSelected()
        {
            if (!interactionEnabled || selected == null || !selected.CanEdit || selected.Definition == null || isSimulating)
            {
                return;
            }

            ToolDefinition definition = selected.Definition;
            placedTools.Remove(selected);
            Destroy(selected.gameObject);
            selected = null;
            if (!unlimitedSupply)
            {
                remaining[definition] = GetRemaining(definition) + 1;
            }

            RecalculateCurrentCost();
            InventoryChanged?.Invoke();
        }

        public void ClearAllPlacedTools()
        {
            if (isSimulating || !interactionEnabled)
            {
                return;
            }

            ClearPreview();
            EndPlacedToolDrag(false);
            Select(null);

            foreach (PlaceableTool tool in placedTools)
            {
                if (tool == null)
                {
                    continue;
                }

                if (!unlimitedSupply && tool.Definition != null)
                {
                    remaining[tool.Definition] = GetRemaining(tool.Definition) + 1;
                }

                Destroy(tool.gameObject);
            }

            placedTools.Clear();
            SetCurrentCost(0);
            InventoryChanged?.Invoke();
        }

        public void MoveSelected(Vector2 screenDirection)
        {
            if (!interactionEnabled || selected == null || !selected.CanEdit || selected.Definition == null || isSimulating || screenDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector3 originalPosition = selected.transform.position;
            Quaternion originalRotation = selected.transform.rotation;
            Vector2 worldDelta = ScreenPixelsToWorldDelta(screenDirection.normalized * nudgePixels);
            selected.transform.position = originalPosition + (Vector3)worldDelta;

            TryCommitSelectedTransform(originalPosition, originalRotation);
        }

        public void SetSimulationMode(bool simulating)
        {
            isSimulating = simulating;
            ClearPreview();
            EndPlacedToolDrag(false);
            Select(null);
            foreach (PlaceableTool tool in placedTools)
            {
                if (tool != null)
                {
                    tool.SetSimulationMode(simulating);
                }
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            pointerWasDown = false;
            pointerStartedOverUi = false;
            if (!enabled)
            {
                ClearPreview();
                EndPlacedToolDrag(false);
                Select(null);
            }
        }

        private void HandlePlacementPreview()
        {
            bool pointerDown = TryGetPointer(out Vector2 screenPosition, out bool justPressed, out bool justReleased, out int pointerId);
            bool pointerOverUi = IsPointerOverUi(pointerId);
            if (justPressed && !pointerOverUi)
            {
                pointerOverUi = IsScreenPositionOverUi(screenPosition);
            }

            if (justPressed)
            {
                pointerDownScreenPosition = screenPosition;
                pointerStartedOverUi = pointerOverUi;
            }

            // On mobile, EventSystem may discard the touch pointer data before this
            // Update observes TouchPhase.Ended. Remember where the gesture began so
            // releasing a UI button cannot also select (or deselect) a world tool.
            bool pointerBelongsToUi = pointerOverUi || pointerStartedOverUi;

            if (preview != null)
            {
                Vector2 worldPosition = ScreenToWorld(screenPosition);
                preview.transform.position = worldPosition;
                bool valid = IsPlacementValidAfterSync(preview);
                preview.SetPreview(true, valid);

                if (Input.GetKeyDown(KeyCode.R))
                {
                    RotateSelected(1f);
                }

                if (justReleased && !pointerBelongsToUi)
                {
                    FinishPlaceDrag(screenPosition, true);
                }
                else if (justReleased)
                {
                    ClearPreview();
                }
            }
            else if (draggedPlacedTool != null)
            {
                DragPlacedTool(screenPosition);
                if (justReleased)
                {
                    EndPlacedToolDrag(true);
                }
            }
            else if (justPressed && !pointerBelongsToUi)
            {
                TryBeginPlacedToolDrag(screenPosition);
            }
            else if (justReleased && !pointerBelongsToUi && !WasPointerDragged(screenPosition))
            {
                SelectAt(ScreenToWorld(screenPosition));
            }

            pointerWasDown = pointerDown;
            if (justReleased)
            {
                pointerStartedOverUi = false;
            }
        }

        private void HandleSelectionShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                DeleteSelected();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                RotateSelected(-1f);
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R))
            {
                RotateSelected(1f);
            }
        }

        private bool TryGetPointer(out Vector2 screenPosition, out bool justPressed, out bool justReleased, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                justPressed = touch.phase == TouchPhase.Began;
                justReleased = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }

            screenPosition = Input.mousePosition;
            pointerId = -1;
            bool down = Input.GetMouseButton(0);
            justPressed = !pointerWasDown && down;
            justReleased = pointerWasDown && !down;
            return down;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private bool IsScreenPositionOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, uiRaycastResults);
            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                if (uiRaycastResults[i].module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        private bool WasPointerDragged(Vector2 screenPosition)
        {
            return (screenPosition - pointerDownScreenPosition).sqrMagnitude > 64f;
        }

        private void PlacePreview()
        {
            if (preview == null || activeDefinition == null)
            {
                return;
            }

            AssignPortalPairId(preview);
            preview.SetPreview(false, true);
            placedTools.Add(preview);
            if (!unlimitedSupply)
            {
                remaining[activeDefinition] = Mathf.Max(0, GetRemaining(activeDefinition) - 1);
            }

            Select(preview);
            preview = null;
            activeDefinition = null;
            RecalculateCurrentCost();
            InventoryChanged?.Invoke();
        }

        private bool IsPlacementValid(PlaceableTool tool)
        {
            if (tool == null || tool.Definition == null)
            {
                return false;
            }

            if (tool.GetComponent<AntiGravityField>() != null || tool.GetComponent<WaterZone>() != null)
            {
                return !OverlapsBlockingAntiGravity(tool);
            }

            IReadOnlyList<Collider2D> toolColliders = tool.Colliders;
            if (toolColliders == null || toolColliders.Count == 0)
            {
                return IsFallbackRadiusPlacementValid(tool);
            }

            foreach (Collider2D toolCollider in toolColliders)
            {
                if (toolCollider == null || !toolCollider.enabled)
                {
                    continue;
                }

                int hitCount = toolCollider.OverlapCollider(placementFilter, placementHits);
                for (int i = 0; i < hitCount; i++)
                {
                    Collider2D hit = placementHits[i];
                    if (hit != null && !hit.transform.IsChildOf(tool.transform) && !IsIgnoredPlacementHit(hit))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsPlacementValidAfterSync(PlaceableTool tool)
        {
            Physics2D.SyncTransforms();
            return IsPlacementValid(tool);
        }

        private bool TryCommitSelectedTransform(Vector3 originalPosition, Quaternion originalRotation)
        {
            if (IsPlacementValidAfterSync(selected))
            {
                Select(selected);
                return true;
            }

            selected.transform.position = originalPosition;
            selected.transform.rotation = originalRotation;
            Physics2D.SyncTransforms();
            return false;
        }

        private void AssignPortalPairId(PlaceableTool tool)
        {
            PortalTool portal = tool != null ? tool.GetComponent<PortalTool>() : null;
            if (portal == null)
            {
                return;
            }

            portal.SetPairId(GetNextPortalPairId());
        }

        private int GetNextPortalPairId()
        {
            Dictionary<int, int> countsByPairId = new Dictionary<int, int>();
            foreach (PlaceableTool tool in placedTools)
            {
                PortalTool portal = tool != null ? tool.GetComponent<PortalTool>() : null;
                if (portal == null)
                {
                    continue;
                }

                int pairId = portal.PairId;
                countsByPairId[pairId] = countsByPairId.TryGetValue(pairId, out int count) ? count + 1 : 1;
            }

            for (int pairId = 0; ; pairId++)
            {
                if (!countsByPairId.TryGetValue(pairId, out int count) || count < 2)
                {
                    return pairId;
                }
            }
        }

        private bool IsFallbackRadiusPlacementValid(PlaceableTool tool)
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(tool.transform.position, tool.Definition.PlacementRadius, placementHits, blockedPlacementMask);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = placementHits[i];
                if (hit != null && !hit.transform.IsChildOf(tool.transform) && !IsIgnoredPlacementHit(hit))
                {
                    return false;
                }
            }

            return true;
        }

        private bool OverlapsBlockingAntiGravity(PlaceableTool tool)
        {
            IReadOnlyList<Collider2D> toolColliders = tool.Colliders;
            if (toolColliders == null || toolColliders.Count == 0)
            {
                int fallbackHitCount = Physics2D.OverlapCircleNonAlloc(
                    tool.transform.position,
                    tool.Definition.PlacementRadius,
                    placementHits,
                    blockedPlacementMask);
                return ContainsBlockingAntiGravity(tool, fallbackHitCount);
            }

            foreach (Collider2D toolCollider in toolColliders)
            {
                if (toolCollider == null || !toolCollider.enabled)
                {
                    continue;
                }

                int hitCount = toolCollider.OverlapCollider(placementFilter, placementHits);
                if (ContainsBlockingAntiGravity(tool, hitCount))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsBlockingAntiGravity(PlaceableTool tool, int hitCount)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = placementHits[i];
                if (hit == null || hit.transform.IsChildOf(tool.transform))
                {
                    continue;
                }

                AntiGravityField field = hit.GetComponentInParent<AntiGravityField>();
                if (field != null && !field.AllowOtherToolsInside)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIgnoredPlacementHit(Collider2D hit)
        {
            if (hit == null)
            {
                return false;
            }

            AntiGravityField antiGravity = hit.GetComponentInParent<AntiGravityField>();
            if (antiGravity != null)
            {
                return antiGravity.AllowOtherToolsInside;
            }

            return hit.GetComponentInParent<WaterZone>() != null;
        }

        private void TryBeginPlacedToolDrag(Vector2 screenPosition)
        {
            if (!TryGetPlaceableAtScreenPosition(screenPosition, out PlaceableTool tool))
            {
                Select(null);
                return;
            }

            if (tool != selected)
            {
                Select(tool);
                return;
            }

            draggedPlacedTool = tool;
            draggedOriginalPosition = tool.transform.position;
            draggedPointerOffset = (Vector2)tool.transform.position - ScreenToWorld(screenPosition);
            Select(tool);
            DragPlacedTool(screenPosition);
        }

        private void DragPlacedTool(Vector2 screenPosition)
        {
            if (draggedPlacedTool == null)
            {
                return;
            }

            Vector2 worldPosition = ScreenToWorld(screenPosition);
            Vector2 targetPosition = worldPosition + draggedPointerOffset;
            draggedPlacedTool.transform.position = targetPosition;
            bool valid = IsPlacementValidAfterSync(draggedPlacedTool);
            draggedPlacedTool.SetPreview(true, valid);
        }

        private void EndPlacedToolDrag(bool keepIfValid)
        {
            if (draggedPlacedTool == null)
            {
                return;
            }

            bool valid = keepIfValid && IsPlacementValidAfterSync(draggedPlacedTool);
            if (!valid)
            {
                draggedPlacedTool.transform.position = draggedOriginalPosition;
                Physics2D.SyncTransforms();
            }

            draggedPlacedTool.SetPreview(false, true);
            Select(draggedPlacedTool);
            draggedPlacedTool = null;
        }

        private void SelectAt(Vector2 worldPosition)
        {
            Select(FindPlaceableAt(worldPosition));
        }

        private PlaceableTool FindPlaceableAt(Vector2 worldPosition)
        {
            int hitCount = Physics2D.OverlapPointNonAlloc(worldPosition, placementHits);
            PlaceableTool bestTool = null;
            int bestDefinitionSortingOrder = int.MinValue;
            int bestSortingOrder = int.MinValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = placementHits[i];
                if (hit == null)
                {
                    continue;
                }

                PlaceableTool tool = hit.GetComponentInParent<PlaceableTool>();
                if (tool == null || !tool.CanEdit || !placedTools.Contains(tool))
                {
                    continue;
                }

                int definitionSortingOrder = tool.Definition != null ? tool.Definition.ClickSortingOrder : int.MinValue;
                int sortingOrder = GetHighestSortingOrder(tool);
                if (bestTool == null
                    || definitionSortingOrder > bestDefinitionSortingOrder
                    || (definitionSortingOrder == bestDefinitionSortingOrder && sortingOrder > bestSortingOrder))
                {
                    bestTool = tool;
                    bestDefinitionSortingOrder = definitionSortingOrder;
                    bestSortingOrder = sortingOrder;
                }
            }

            return bestTool;
        }

        private static int GetHighestSortingOrder(PlaceableTool tool)
        {
            int highestOrder = int.MinValue;
            Renderer[] renderers = tool.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer toolRenderer = renderers[i];
                if (toolRenderer != null && toolRenderer.enabled)
                {
                    highestOrder = Mathf.Max(highestOrder, toolRenderer.sortingOrder);
                }
            }

            return highestOrder;
        }

        private void Select(PlaceableTool tool)
        {
            if (tool != null && (!tool.CanEdit || !placedTools.Contains(tool)))
            {
                tool = null;
            }

            if (selected != null)
            {
                selected.SetSelected(false);
            }

            selected = tool;
            if (selected != null)
            {
                selected.SetSelected(true);
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            Vector3 world = cameraToUse.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cameraToUse.transform.position.z));
            return world;
        }

        private Vector2 ScreenPixelsToWorldDelta(Vector2 screenDelta)
        {
            Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (cameraToUse == null)
            {
                return screenDelta * 0.01f;
            }

            float depth = -cameraToUse.transform.position.z;
            Vector3 origin = cameraToUse.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 target = cameraToUse.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, depth));
            return target - origin;
        }

        private void ClearPreview()
        {
            if (preview != null)
            {
                Destroy(preview.gameObject);
                preview = null;
            }

            activeDefinition = null;
        }

        private void ClearPlacedTools()
        {
            foreach (PlaceableTool tool in placedTools)
            {
                if (tool != null)
                {
                    Destroy(tool.gameObject);
                }
            }

            placedTools.Clear();
            SetCurrentCost(0);
        }

        private void RecalculateCurrentCost()
        {
            long total = 0;
            foreach (PlaceableTool tool in placedTools)
            {
                if (tool != null && tool.Definition != null)
                {
                    total += tool.Definition.Cost;
                }
            }

            SetCurrentCost(total > int.MaxValue ? int.MaxValue : (int)total);
        }

        private void SetCurrentCost(int value)
        {
            value = Mathf.Max(0, value);
            if (CurrentCost == value)
            {
                return;
            }

            CurrentCost = value;
            CostChanged?.Invoke();
        }
    }
}
