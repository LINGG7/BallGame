using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Tests.Editor
{
    public sealed class PlacementHistoryTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        private System.Type managerType;
        private System.Type placeableType;
        private System.Type portalType;
        private GameObject managerObject;
        private Transform placedRoot;
        private Component manager;
        private UnityEngine.Object plank;

        [SetUp]
        public void SetUp()
        {
            managerType = System.Type.GetType("BallGame.Placement.PlacementManager, Assembly-CSharp");
            placeableType = System.Type.GetType("BallGame.Placement.PlaceableTool, Assembly-CSharp");
            portalType = System.Type.GetType("BallGame.Tools.PortalTool, Assembly-CSharp");
            Assert.That(managerType, Is.Not.Null);
            Assert.That(placeableType, Is.Not.Null);
            Assert.That(portalType, Is.Not.Null);

            plank = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Tools/Plank.asset");
            Assert.That(plank, Is.Not.Null);

            managerObject = new GameObject("Placement History Test");
            placedRoot = new GameObject("Placed Tools").transform;
            placedRoot.SetParent(managerObject.transform);
            manager = managerObject.AddComponent(managerType);
            SetField(manager, "placedRoot", placedRoot);
            AddStock(plank, 3);
        }

        [TearDown]
        public void TearDown()
        {
            if (managerObject != null) Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void PlaceUndoRedo_RestoresToolsInventoryAndCost()
        {
            Place(plank, new Vector3(2f, 3f, 0f), Quaternion.Euler(0f, 0f, 15f));

            Assert.That(PlacedTools.Count, Is.EqualTo(1));
            Assert.That(GetRemaining(plank), Is.EqualTo(2));
            Assert.That(CurrentCost, Is.EqualTo(ToolCost(plank)));
            Assert.That(CanUndo, Is.True);

            Call(manager, "Undo");
            Assert.That(PlacedTools.Count, Is.Zero);
            Assert.That(GetRemaining(plank), Is.EqualTo(3));
            Assert.That(CurrentCost, Is.Zero);
            Assert.That(CanRedo, Is.True);

            Call(manager, "Redo");
            Assert.That(PlacedTools.Count, Is.EqualTo(1));
            Assert.That(GetRemaining(plank), Is.EqualTo(2));
            Assert.That(CurrentCost, Is.EqualTo(ToolCost(plank)));
            Assert.That(PlacedTool(0).transform.position, Is.EqualTo(new Vector3(2f, 3f, 0f)));
        }

        [Test]
        public void TransformDragDeleteAndClear_AreUndoable()
        {
            Place(plank, Vector3.zero, Quaternion.identity);
            Select(PlacedTool(0));
            Call(manager, "RotateSelected", 1f);
            Quaternion rotated = PlacedTool(0).transform.rotation;
            Assert.That(rotated, Is.Not.EqualTo(Quaternion.identity));
            Call(manager, "Undo");
            Assert.That(PlacedTool(0).transform.rotation, Is.EqualTo(Quaternion.identity));
            Call(manager, "Redo");
            Assert.That(PlacedTool(0).transform.rotation, Is.EqualTo(rotated));

            Select(PlacedTool(0));
            Vector3 beforeMove = PlacedTool(0).transform.position;
            Call(manager, "MoveSelected", Vector2.right);
            Vector3 afterMove = PlacedTool(0).transform.position;
            Assert.That(afterMove, Is.Not.EqualTo(beforeMove));
            Call(manager, "Undo");
            Assert.That(PlacedTool(0).transform.position, Is.EqualTo(beforeMove));

            Component dragged = PlacedTool(0);
            Select(dragged);
            SetField(manager, "draggedPlacedTool", dragged);
            SetField(manager, "draggedOriginalPosition", dragged.transform.position);
            SetField(manager, "draggedOriginalState", Invoke(manager, "CaptureState"));
            dragged.transform.position += Vector3.up * 2f;
            Invoke(manager, "EndPlacedToolDrag", true);
            Call(manager, "Undo");
            Assert.That(PlacedTool(0).transform.position, Is.EqualTo(beforeMove));

            Select(PlacedTool(0));
            Call(manager, "DeleteSelected");
            Assert.That(PlacedTools.Count, Is.Zero);
            Call(manager, "Undo");
            Assert.That(PlacedTools.Count, Is.EqualTo(1));

            Place(plank, new Vector3(4f, 0f, 0f), Quaternion.identity);
            Call(manager, "ClearAllPlacedTools");
            Assert.That(PlacedTools.Count, Is.Zero);
            Call(manager, "Undo");
            Assert.That(PlacedTools.Count, Is.EqualTo(2));
        }

        [Test]
        public void SimulationAndLevelReset_UpdateHistoryAvailability()
        {
            Place(plank, Vector3.zero, Quaternion.identity);
            Assert.That(CanUndo, Is.True);

            Call(manager, "SetSimulationMode", true);
            Assert.That(CanUndo, Is.False);
            Call(manager, "Undo");
            Assert.That(PlacedTools.Count, Is.EqualTo(1));

            Call(manager, "SetSimulationMode", false);
            Assert.That(CanUndo, Is.True);
            Call(manager, "ResetForLevel", new object[] { null });
            Assert.That(CanUndo, Is.False);
            Assert.That(CanRedo, Is.False);
        }

        [Test]
        public void NewOperationAfterUndo_ClearsRedo()
        {
            Place(plank, Vector3.zero, Quaternion.identity);
            Call(manager, "Undo");
            Assert.That(CanRedo, Is.True);

            Place(plank, Vector3.right, Quaternion.identity);
            Assert.That(CanRedo, Is.False);
        }

        [Test]
        public void PortalPairId_IsRestoredAfterDeleteUndo()
        {
            UnityEngine.Object portalDefinition = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Tools/Portal.asset");
            Assert.That(portalDefinition, Is.Not.Null);
            AddStock(portalDefinition, 1);
            Place(portalDefinition, Vector3.zero, Quaternion.identity);
            Component portal = PlacedTool(0).GetComponent(portalType);
            Assert.That(portal, Is.Not.Null);
            Call(portal, "SetPairId", 7);

            Select(PlacedTool(0));
            Call(manager, "DeleteSelected");
            Call(manager, "Undo");

            Component restoredPortal = PlacedTool(0).GetComponent(portalType);
            Assert.That((int)portalType.GetProperty("PairId").GetValue(restoredPortal), Is.EqualTo(7));
        }

        [Test]
        public void GameHud_HasUndoRedoButtonReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/GameHud.prefab");
            System.Type controlsType = System.Type.GetType("BallGame.UI.PlacementControls, Assembly-CSharp");
            Component controls = prefab.GetComponentInChildren(controlsType, true);
            Assert.That(controls, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(controls);
            Button undo = serialized.FindProperty("undoButton").objectReferenceValue as Button;
            Button redo = serialized.FindProperty("redoButton").objectReferenceValue as Button;
            Assert.That(undo, Is.Not.Null);
            Assert.That(redo, Is.Not.Null);
            Assert.That(undo.name, Is.EqualTo("Undo"));
            Assert.That(redo.name, Is.EqualTo("Redo"));
        }

        private IList PlacedTools => (IList)managerType.GetField("placedTools", InstancePrivate).GetValue(manager);
        private bool CanUndo => (bool)managerType.GetProperty("CanUndo").GetValue(manager);
        private bool CanRedo => (bool)managerType.GetProperty("CanRedo").GetValue(manager);
        private int CurrentCost => (int)managerType.GetProperty("CurrentCost").GetValue(manager);

        private Component PlacedTool(int index) => (Component)PlacedTools[index];

        private void Place(UnityEngine.Object definition, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = (GameObject)definition.GetType().GetProperty("Prefab").GetValue(definition);
            GameObject instance = Object.Instantiate(prefab, placedRoot);
            instance.transform.SetPositionAndRotation(position, rotation);
            Component tool = instance.GetComponent(placeableType);
            Call(tool, "Initialize", definition);
            SetField(manager, "preview", tool);
            SetField(manager, "activeDefinition", definition);
            Invoke(manager, "PlacePreview");
        }

        private void Select(Component tool)
        {
            Invoke(manager, "Select", tool);
        }

        private void AddStock(UnityEngine.Object definition, int count)
        {
            IDictionary remaining = (IDictionary)managerType.GetField("remaining", InstancePrivate).GetValue(manager);
            remaining[definition] = count;
        }

        private int GetRemaining(UnityEngine.Object definition) => (int)Call(manager, "GetRemaining", definition);
        private static int ToolCost(UnityEngine.Object definition) =>
            (int)definition.GetType().GetProperty("Cost").GetValue(definition);

        private static object Call(object target, string methodName, params object[] arguments)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo method = target.GetType().GetMethod(methodName, flags);
            return method.Invoke(target, arguments);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            return target.GetType().GetMethod(methodName, InstancePrivate).Invoke(target, arguments);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, InstancePrivate).SetValue(target, value);
        }
    }
}
