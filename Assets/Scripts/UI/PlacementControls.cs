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

        public void BindPlacementManager(PlacementManager manager)
        {
            placementManager = manager;
        }

        private void Start()
        {
            if (rotateLeftButton != null)
            {
                rotateLeftButton.onClick.AddListener(() => placementManager.RotateSelected(-1f));
            }

            if (rotateRightButton != null)
            {
                rotateRightButton.onClick.AddListener(() => placementManager.RotateSelected(1f));
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => placementManager.DeleteSelected());
            }

            if (moveUpButton != null)
            {
                moveUpButton.onClick.AddListener(() => placementManager.MoveSelected(Vector2.up));
            }

            if (moveDownButton != null)
            {
                moveDownButton.onClick.AddListener(() => placementManager.MoveSelected(Vector2.down));
            }

            if (moveLeftButton != null)
            {
                moveLeftButton.onClick.AddListener(() => placementManager.MoveSelected(Vector2.left));
            }

            if (moveRightButton != null)
            {
                moveRightButton.onClick.AddListener(() => placementManager.MoveSelected(Vector2.right));
            }

            if (clearAllButton != null)
            {
                clearAllButton.onClick.AddListener(() => placementManager.ClearAllPlacedTools());
            }
        }
    }
}
