using BallGame.Core;
using BallGame.Placement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class ToolInventoryButton : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private GameObject costLabel;
        [SerializeField] private TMP_Text costCount;
        [SerializeField] private float dragStartThreshold = 8f;

        private ToolDefinition definition;
        private PlacementManager placementManager;
        private Vector2 pointerDownPosition;
        private bool canStartDrag;
        private bool isPlacing;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            ResolveModeLabels();
        }

        public void Bind(ToolDefinition toolDefinition, PlacementManager manager)
        {
            ResolveModeLabels();
            definition = toolDefinition;
            placementManager = manager;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }

            if (icon != null)
            {
                icon.sprite = definition.Icon;
                icon.color = definition.Icon != null ? Color.white : definition.IconColor;
            }

            if (label != null)
            {
                label.text = definition.DisplayName;
            }

            Refresh();
        }

        public void Refresh()
        {
            ResolveModeLabels();
            int remaining = placementManager != null ? placementManager.GetRemaining(definition) : 0;
            bool showCost = placementManager != null && placementManager.HasUnlimitedSupply;

            if (costLabel != null)
            {
                costLabel.SetActive(showCost);
            }

            if (countLabel != null)
            {
                countLabel.gameObject.SetActive(!showCost);
                if (!showCost)
                {
                    countLabel.text = "X" + remaining.ToString();
                }
            }

            if (costCount != null && showCost)
            {
                costCount.text = definition != null ? definition.Cost.ToString() : "0";
            }

            if (button != null)
            {
                button.interactable = placementManager != null && placementManager.CanPlace(definition);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownPosition = eventData.position;
            canStartDrag = placementManager != null
                && definition != null
                && placementManager.CanPlace(definition)
                && (button == null || button.interactable);
            isPlacing = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!canStartDrag)
            {
                return;
            }

            Vector2 delta = eventData.position - pointerDownPosition;
            if (!isPlacing && delta.magnitude < dragStartThreshold)
            {
                return;
            }

            if (!isPlacing)
            {
                isPlacing = true;
                placementManager.BeginPlaceDrag(definition, eventData.position);
            }

            if (isPlacing)
            {
                placementManager.UpdatePlaceDrag(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isPlacing)
            {
                placementManager?.FinishPlaceDrag(eventData.position, !IsPointerOverUi(eventData));
            }

            ResetDragState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isPlacing)
            {
                placementManager?.FinishPlaceDrag(eventData.position, !IsPointerOverUi(eventData));
            }

            ResetDragState();
        }

        private static bool IsPointerOverUi(PointerEventData eventData)
        {
            return eventData.pointerEnter != null && eventData.pointerEnter.GetComponentInParent<Canvas>() != null;
        }

        private void ResetDragState()
        {
            canStartDrag = false;
            isPlacing = false;
        }

        private void ResolveModeLabels()
        {
            if (countLabel == null)
            {
                countLabel = transform.Find("Count")?.GetComponent<TMP_Text>();
            }

            if (costLabel == null)
            {
                Transform foundCostLabel = transform.Find("CostLabel");
                costLabel = foundCostLabel != null ? foundCostLabel.gameObject : null;
            }

            if (costCount == null && costLabel != null)
            {
                costCount = costLabel.transform.Find("CostCount")?.GetComponent<TMP_Text>();
            }
        }
    }
}
