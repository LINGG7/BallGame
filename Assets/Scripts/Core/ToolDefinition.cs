using UnityEngine;

namespace BallGame.Core
{
    [CreateAssetMenu(menuName = "Ball Game/Tool Definition")]
    public sealed class ToolDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Tool";
        [SerializeField] private GameObject prefab;
        [SerializeField] private BallGame.UI.ToolInventoryButton inventoryButtonPrefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color iconColor = Color.white;
        [SerializeField] private int defaultCount = 1;
        [SerializeField, Min(1)] private int cost = 1;
        [SerializeField] private bool canRotate = true;
        [SerializeField] private float rotationStep = 15f;
        [SerializeField] private float placementRadius = 0.45f;
        [SerializeField] private int clickSortingOrder;

        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public BallGame.UI.ToolInventoryButton InventoryButtonPrefab => inventoryButtonPrefab;
        public Sprite Icon => icon;
        public Color IconColor => iconColor;
        public int DefaultCount => Mathf.Max(0, defaultCount);
        public int Cost => Mathf.Max(1, cost);
        public bool CanRotate => canRotate;
        public float RotationStep => Mathf.Max(1f, rotationStep);
        public float PlacementRadius => Mathf.Max(0.05f, placementRadius);
        public int ClickSortingOrder => clickSortingOrder;
    }
}
