using BallGame.Placement;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class PlacementControls : MonoBehaviour
    {
        [SerializeField] private PlacementManager placementManager;
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button moveUpButton;
        [SerializeField] private Button moveDownButton;
        [SerializeField] private Button moveLeftButton;
        [SerializeField] private Button moveRightButton;
        [SerializeField] private Button clearAllButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;

        public void BindPlacementManager(PlacementManager manager)
        {
            if (placementManager != null)
            {
                placementManager.HistoryChanged -= RefreshHistoryButtons;
            }

            placementManager = manager;
            if (placementManager != null)
            {
                placementManager.HistoryChanged += RefreshHistoryButtons;
            }

            RefreshHistoryButtons();
        }

        private void Start()
        {
            if (rotateLeftButton != null)
            {
                rotateLeftButton.onClick.AddListener(RotateLeft);
            }

            if (rotateRightButton != null)
            {
                rotateRightButton.onClick.AddListener(RotateRight);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(DeleteSelected);
            }

            if (moveUpButton != null)
            {
                moveUpButton.onClick.AddListener(MoveUp);
            }

            if (moveDownButton != null)
            {
                moveDownButton.onClick.AddListener(MoveDown);
            }

            if (moveLeftButton != null)
            {
                moveLeftButton.onClick.AddListener(MoveLeft);
            }

            if (moveRightButton != null)
            {
                moveRightButton.onClick.AddListener(MoveRight);
            }

            if (clearAllButton != null)
            {
                clearAllButton.onClick.AddListener(ClearAll);
            }

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(Undo);
            }

            if (redoButton != null)
            {
                redoButton.onClick.AddListener(Redo);
            }

            RefreshHistoryButtons();
        }

        private void OnDestroy()
        {
            if (placementManager != null)
            {
                placementManager.HistoryChanged -= RefreshHistoryButtons;
            }

            if (rotateLeftButton != null) rotateLeftButton.onClick.RemoveListener(RotateLeft);
            if (rotateRightButton != null) rotateRightButton.onClick.RemoveListener(RotateRight);
            if (deleteButton != null) deleteButton.onClick.RemoveListener(DeleteSelected);
            if (moveUpButton != null) moveUpButton.onClick.RemoveListener(MoveUp);
            if (moveDownButton != null) moveDownButton.onClick.RemoveListener(MoveDown);
            if (moveLeftButton != null) moveLeftButton.onClick.RemoveListener(MoveLeft);
            if (moveRightButton != null) moveRightButton.onClick.RemoveListener(MoveRight);
            if (clearAllButton != null) clearAllButton.onClick.RemoveListener(ClearAll);
            if (undoButton != null) undoButton.onClick.RemoveListener(Undo);
            if (redoButton != null) redoButton.onClick.RemoveListener(Redo);
        }

        private void RotateLeft() => placementManager?.RotateSelected(-1f);
        private void RotateRight() => placementManager?.RotateSelected(1f);
        private void DeleteSelected() => placementManager?.DeleteSelected();
        private void MoveUp() => placementManager?.MoveSelected(Vector2.up);
        private void MoveDown() => placementManager?.MoveSelected(Vector2.down);
        private void MoveLeft() => placementManager?.MoveSelected(Vector2.left);
        private void MoveRight() => placementManager?.MoveSelected(Vector2.right);
        private void ClearAll() => placementManager?.ClearAllPlacedTools();
        private void Undo() => placementManager?.Undo();
        private void Redo() => placementManager?.Redo();

        private void RefreshHistoryButtons()
        {
            if (undoButton != null)
            {
                undoButton.interactable = placementManager != null && placementManager.CanUndo;
            }

            if (redoButton != null)
            {
                redoButton.interactable = placementManager != null && placementManager.CanRedo;
            }
        }
    }
}
