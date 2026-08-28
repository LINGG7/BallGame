using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace BallGame.Editor
{
    public static class SimulationMoverDesignerUiUtility
    {
        private const string PrefabPath = "Assets/LevelDesigner/DesignerUI.prefab";

        [MenuItem("Ball Game/Level Design/Add SimulationMover Panel To Designer UI")]
        public static void AddPanelToDesignerUi()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new System.InvalidOperationException("DesignerUI prefab not found: " + PrefabPath);
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform inspector = root.transform.Find("Inspector");
                if (inspector == null) throw new System.InvalidOperationException("DesignerUI is missing Inspector.");
                Transform existing = inspector.Find("SimulationMoverPanel");
                GameObject panel = existing != null ? existing.gameObject : new GameObject("SimulationMoverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                if (existing == null) panel.transform.SetParent(inspector, false);
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(8f, -595f);
                rect.sizeDelta = new Vector2(414f, 470f);
                panel.GetComponent<Image>().color = new Color(0.86f, 0.88f, 0.90f, 0.96f);
            BuildControls(panel.transform);
            Transform legacyUp = panel.transform.Find("UpButton");
            Transform legacyDown = panel.transform.Find("DownButton");
            if (legacyUp != null) Object.DestroyImmediate(legacyUp.gameObject);
            if (legacyDown != null) Object.DestroyImmediate(legacyDown.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildControls(Transform panel)
        {
            for (int i = panel.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.GetChild(i).gameObject);
            MethodInfo addInput = typeof(BallGame.LevelDesign.CollaborativeLevelDesignerController).GetMethod("AddInput", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo addButton = typeof(BallGame.LevelDesign.CollaborativeLevelDesignerController).GetMethod("AddButton", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo addDropdown = typeof(BallGame.LevelDesign.CollaborativeLevelDesignerController).GetMethod("AddDropdown", BindingFlags.NonPublic | BindingFlags.Static);
            if (addInput == null || addButton == null || addDropdown == null) throw new System.InvalidOperationException("Designer UI factory methods are unavailable.");
            AddLabel(panel, "Title", "SIMULATION MOVER", new Vector2(207f, -22f), 18f);
            AddToggle(panel, "Simulation Mover", new Vector2(207f, -58f));
            AddToggle(panel, "Move Enabled", new Vector2(102f, -98f)); AddToggle(panel, "Ping Pong", new Vector2(312f, -98f));
            AddToggle(panel, "Closed Path", new Vector2(102f, -138f)); AddToggle(panel, "Rotate", new Vector2(312f, -138f));
            addInput.Invoke(null, new object[] { panel, "Move Speed", new Vector2(102f, -186f), new UnityEngine.Events.UnityAction<string>(_ => { }) });
            addInput.Invoke(null, new object[] { panel, "Angular Speed", new Vector2(312f, -186f), new UnityEngine.Events.UnityAction<string>(_ => { }) });
            AddLabel(panel, "Waypoints", "WAYPOINTS", new Vector2(207f, -222f), 16f);
            addDropdown.Invoke(null, new object[] { panel, "Waypoint", new Vector2(207f, -280f), new Vector2(380f, 40f), new[] { "No waypoints" } });
            SetButtonWidth((Button)addButton.Invoke(null, new object[] { panel, "Add Waypoint", new Vector2(102f, -332f), new UnityEngine.Events.UnityAction(() => { }) }), 190f);
            SetButtonWidth((Button)addButton.Invoke(null, new object[] { panel, "Remove Waypoint", new Vector2(312f, -332f), new UnityEngine.Events.UnityAction(() => { }) }), 190f);
            addInput.Invoke(null, new object[] { panel, "Waypoint X", new Vector2(102f, -436f), new UnityEngine.Events.UnityAction<string>(_ => { }) });
            addInput.Invoke(null, new object[] { panel, "Waypoint Y", new Vector2(312f, -436f), new UnityEngine.Events.UnityAction<string>(_ => { }) });
        }

        private static void AddToggle(Transform parent, string label, Vector2 position)
        {
            if (parent.Find(label + "Toggle") != null) return;
            GameObject root = new GameObject(label + "Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            root.transform.SetParent(parent, false); RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(190f, 34f);
            Image image = root.GetComponent<Image>(); image.color = new Color(0.16f, 0.18f, 0.2f, 1f); Toggle toggle = root.GetComponent<Toggle>(); toggle.targetGraphic = image;
            GameObject checkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); checkObject.transform.SetParent(root.transform, false); RectTransform checkRect = checkObject.GetComponent<RectTransform>(); checkRect.anchorMin = new Vector2(0f, 0.5f); checkRect.anchorMax = new Vector2(0f, 0.5f); checkRect.pivot = new Vector2(0.5f, 0.5f); checkRect.anchoredPosition = new Vector2(18f, 0f); checkRect.sizeDelta = new Vector2(16f, 16f); Image checkImage = checkObject.GetComponent<Image>(); checkImage.color = new Color(0.25f, 0.9f, 0.35f, 1f); checkImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Checkmark.psd"); checkImage.raycastTarget = false; toggle.graphic = checkImage;
            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); textObject.transform.SetParent(root.transform, false); RectTransform textRect = textObject.GetComponent<RectTransform>(); textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f); textRect.sizeDelta = new Vector2(115f, 28f); textRect.anchoredPosition = new Vector2(8f, 0f); TMP_Text text = textObject.GetComponent<TMP_Text>(); text.text = label; text.fontSize = 13f; text.alignment = TextAlignmentOptions.MidlineLeft; text.color = Color.white; text.raycastTarget = false;
        }

        private static void AddLabel(Transform parent, string name, string value, Vector2 position, float size)
        {
            if (parent.Find(name) != null) return;
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); textObject.transform.SetParent(parent, false); RectTransform rect = textObject.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(380f, 30f); rect.anchoredPosition = position; TMP_Text text = textObject.GetComponent<TMP_Text>(); text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = new Color(0.12f, 0.14f, 0.16f, 1f); text.raycastTarget = false;
        }

        private static void SetButtonWidth(Button button, float width)
        {
            RectTransform rect = button.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }
    }
}
