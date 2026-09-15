using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BallGame.Tests.Editor
{
    public sealed class PlacementHintTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private GameObject root;
        private Component manager, game, controller, camera, provider;
        private Object definition;
        private ScriptableObject level;
        private object target;
        private Button button;
        private Type targetType;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Hint Tests");
            manager = root.AddComponent(RuntimeType("BallGame.Placement.PlacementManager"));
            game = root.AddComponent(RuntimeType("BallGame.Core.GameManager"));
            var cameraObject = new GameObject("Camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.GetComponent<Camera>().orthographic = true;
            camera = cameraObject.AddComponent(RuntimeType("BallGame.Core.CameraController2D"));
            Set(camera, "gameManager", game);
            Set(camera, "placementManager", manager);
            Call(camera, "Awake");
            Set(manager, "worldCamera", cameraObject.GetComponent<Camera>());
            Set(manager, "placedRoot", root.transform);
            Call(manager, "Awake");
            level = ScriptableObject.CreateInstance(RuntimeType("BallGame.Core.LevelDefinition"));
            definition = AssetDatabase.LoadAssetAtPath<Object>("Assets/Tools/Plank.asset");
            Assert.That(definition, Is.Not.Null);
            var stocks = (IList)Get(level, "tools");
            var stock = Activator.CreateInstance(level.GetType().GetNestedType("ToolStock"));
            stock.GetType().GetField("tool").SetValue(stock, definition);
            stock.GetType().GetField("count").SetValue(stock, 3);
            stocks.Add(stock);
            Set(game, "level", level);
            Set(game, "placementManager", manager);
            Set(game, "<State>k__BackingField", Enum.Parse(RuntimeType("BallGame.Core.GameState"), "Build"));
            Call(manager, "ResetForLevel", level);
            provider = root.AddComponent(RuntimeType("BallGame.Placement.MockHintAdProvider"));
            Set(provider, "manualCompletion", true);
            controller = root.AddComponent(RuntimeType("BallGame.Placement.PlacementHintController"));
            Call(controller, "OnDisable");
            Set(controller, "gameManager", game);
            Set(controller, "placementManager", manager);
            Set(controller, "cameraController", camera);
            Set(controller, "adProvider", provider);
            var buttonObject = new GameObject("Hint Button", typeof(RectTransform), typeof(Button));
            buttonObject.transform.SetParent(root.transform);
            button = buttonObject.GetComponent<Button>();
            Set(controller, "hintButton", button);
            targetType = RuntimeType("BallGame.Placement.PlacementHintTarget");
            target = AddTarget(Vector3.zero, 30f);
            Call(controller, "OnEnable");
        }
        [TearDown]
        public void TearDown()
        {
            if (controller != null) Call(controller, "OnDisable");
            if (root != null) Object.DestroyImmediate(root);
            if (level != null) Object.DestroyImmediate(level);
        }
        [Test]
        public void Reward_DuplicateCallbackAndClicks_GrantOnlyOneGhost()
        {
            Call(controller, "RequestHint");
            var callback = (Delegate)Get(provider, "pending");
            Assert.That(callback, Is.Not.Null);
            Assert.That(button.interactable, Is.False);
            Call(controller, "RequestHint");
            Assert.That(Get(provider, "pending"), Is.SameAs(callback));
            Call(provider, "CompleteRewarded");
            var ghost = Get(controller, "ghost");
            callback.DynamicInvoke(Enum.Parse(RuntimeType("BallGame.Placement.HintAdResult"), "Rewarded"));
            Assert.That(Get(controller, "ghost"), Is.SameAs(ghost));
            Assert.That(State, Is.EqualTo("Assisting"));
        }
        [TestCase("CompleteCancelled")]
        [TestCase("CompleteFailed")]
        public void NoReward_RestoresInteraction(string completion)
        {
            Call(controller, "RequestHint");
            Call(provider, completion);
            Assert.That(State, Is.EqualTo("Idle"));
            Assert.That(Get(controller, "ghost"), Is.Null);
            Assert.That(Property(manager, "IsBuildInteractionAvailable"), Is.EqualTo(true));
            Assert.That(button.interactable, Is.True);
        }
        [Test]
        public void LevelReset_LateCallbackCannotAffectNextRequest()
        {
            Call(controller, "RequestHint");
            var oldCallback = (Delegate)Get(provider, "pending");
            Call(manager, "ResetForLevel", level);
            Call(controller, "RequestHint");
            oldCallback.DynamicInvoke(Enum.Parse(RuntimeType("BallGame.Placement.HintAdResult"), "Rewarded"));
            Assert.That(State, Is.EqualTo("WaitingForAd"));
            Assert.That(Get(controller, "ghost"), Is.Null);
            Call(provider, "CompleteRewarded");
            Assert.That(State, Is.EqualTo("Assisting"));
        }
        [Test]
        public void EmptyOrInvalidTargets_DoNotStartAd()
        {
            Set(target, "tool", null);
            Call(controller, "RequestHint");
            Assert.That(State, Is.EqualTo("Idle"));
            Assert.That(Get(provider, "pending"), Is.Null);
            ((IList)Get(level, "placementHints")).Clear();
            Call(controller, "Update");
            Assert.That(button.interactable, Is.False);
        }
        [Test]
        public void MatchingRelease_CommitsInventoryAndHistoryOnce_ThenCanMoveAgain()
        {
            Activate();
            var tool = CreatePreview(new Vector3(0.2f, 0f, 0f));
            var screen = ((Camera)PropertyCamera()).WorldToScreenPoint(tool.transform.position);
            Call(manager, "FinishPlaceDrag", (Vector2)screen, true);
            Assert.That(State, Is.EqualTo("Idle"));
            Assert.That(tool.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(tool.transform.rotation, Quaternion.Euler(0f, 0f, 30f)), Is.LessThan(0.01f));
            Assert.That(Call(manager, "GetRemaining", definition), Is.EqualTo(2));
            Assert.That(((IList)Get(manager, "placedTools")).Count, Is.EqualTo(1));
            Assert.That(Call(controller, "FindNextTarget"), Is.Null);
            Call(manager, "Undo");
            Assert.That(Call(manager, "GetRemaining", definition), Is.EqualTo(3));
            Assert.That(Call(controller, "FindNextTarget"), Is.SameAs(target));
            Call(manager, "Redo");
            var restored = (Component)((IList)Get(manager, "placedTools"))[0];
            restored.transform.position = Vector3.right * 3f;
            Assert.That(Call(controller, "FindNextTarget"), Is.SameAs(target));
        }
        [Test]
        public void OutOfRangeOrWrongTool_DoesNotConsumeHint()
        {
            Activate();
            var tool = CreatePreview(Vector3.right * 5f);
            Assert.That(Call(controller, "TrySnap", tool), Is.EqualTo(false));
            tool.transform.position = Vector3.zero;
            var other = AssetDatabase.LoadAssetAtPath<Object>("Assets/Tools/Portal.asset");
            Assert.That(other, Is.Not.Null);
            Call(tool, "Initialize", other);
            Assert.That(Call(controller, "TrySnap", tool), Is.EqualTo(false));
            Call(controller, "NotifyPlacementCommitted", tool);
            Assert.That(State, Is.EqualTo("Assisting"));
        }
        [Test]
        public void BlockedTarget_DoesNotConsumeOrSpendStock()
        {
            Activate();
            Set(manager, "blockedPlacementMask", (LayerMask)(1 << 0));
            Call(manager, "Awake");
            var obstacle = new GameObject("Blocker", typeof(BoxCollider2D));
            obstacle.transform.SetParent(root.transform);
            obstacle.GetComponent<BoxCollider2D>().size = Vector2.one * 10f;
            var tool = CreatePreview(Vector3.right * 0.2f);
            var screen = ((Camera)PropertyCamera()).WorldToScreenPoint(tool.transform.position);
            Call(manager, "FinishPlaceDrag", (Vector2)screen, true);
            Assert.That(State, Is.EqualTo("Assisting"));
            Assert.That(Call(manager, "GetRemaining", definition), Is.EqualTo(3));
            Assert.That(((IList)Get(manager, "placedTools")).Count, Is.Zero);
        }
        [Test]
        public void Ghost_HasNoPhysicsOrGameplayComponents_AndCameraKeepsFocus()
        {
            Set(target, "cameraPosition", new Vector2(4f, 3f));
            Activate();
            var ghost = (Component)Get(controller, "ghost");
            Assert.That(ghost.GetComponentsInChildren<Collider2D>(), Is.Empty);
            Assert.That(ghost.GetComponentsInChildren<Rigidbody2D>(), Is.Empty);
            var textType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true);
            foreach (var behaviour in ghost.GetComponentsInChildren<MonoBehaviour>())
                Assert.That(behaviour == ghost || textType.IsInstanceOfType(behaviour), Is.True);
            var instruction = ghost.GetComponentInChildren(textType);
            Assert.That(instruction, Is.Not.Null);
            Assert.That(Property(instruction, "text"), Is.EqualTo("拖动道具至虚影"));
            Assert.That(Property(instruction, "raycastTarget"), Is.EqualTo(false));
            Assert.That(ghost.GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThan(0));
            Assert.That(camera.transform.position, Is.EqualTo(new Vector3(4f, 3f, -10f)));
            Call(controller, "ClearHint");
            Assert.That(camera.transform.position, Is.EqualTo(new Vector3(4f, 3f, -10f)));
        }
        [Test]
        public void OrderedTargets_SkipSatisfiedAndReturnMovedTarget()
        {
            var second = AddTarget(Vector3.right * 4f, 0f);
            var tool = CreatePreview(Vector3.zero);
            tool.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            Call(manager, "PlacePreview");
            Assert.That(Call(controller, "FindNextTarget"), Is.SameAs(second));
            tool.transform.position = Vector3.up * 4f;
            Assert.That(Call(controller, "FindNextTarget"), Is.SameAs(target));
        }
        [Test]
        public void HintLock_DoesNotOverrideExistingInteractionLock()
        {
            Call(manager, "SetInteractionEnabled", false);
            Call(controller, "RequestHint");
            Assert.That(State, Is.EqualTo("Idle"));
            Call(controller, "ClearHint");
            Assert.That(Property(manager, "IsBuildInteractionAvailable"), Is.EqualTo(false));
        }
        [Test]
        public void ExistingTool_RotationOnlySnapIsUndoable()
        {
            var tool = CreatePreview(Vector3.zero);
            tool.transform.rotation = Quaternion.identity;
            Call(manager, "PlacePreview");
            Activate();
            Set(manager, "draggedPlacedTool", tool);
            Set(manager, "draggedOriginalPosition", tool.transform.position);
            Set(manager, "draggedOriginalRotation", tool.transform.rotation);
            Set(manager, "draggedOriginalState", Call(manager, "CaptureState"));
            Call(tool, "SetPreview", true, true);
            Call(manager, "EndPlacedToolDrag", true);
            Assert.That(State, Is.EqualTo("Idle"));
            Assert.That(Call(manager, "GetRemaining", definition), Is.EqualTo(2));
            Call(manager, "Undo");
            var restored = (Component)((IList)Get(manager, "placedTools"))[0];
            Assert.That(Quaternion.Angle(restored.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(Call(manager, "GetRemaining", definition), Is.EqualTo(2));
        }
        [Test]
        public void PendingAdTimeout_IgnoresLateReward()
        {
            Call(controller, "RequestHint");
            var callback = (Delegate)Get(provider, "pending");
            Set(controller, "requestStartedAt", Time.unscaledTime - 121f);
            Call(controller, "Update");
            Assert.That(State, Is.EqualTo("Idle"));
            callback.DynamicInvoke(Enum.Parse(RuntimeType("BallGame.Placement.HintAdResult"), "Rewarded"));
            Assert.That(Get(controller, "ghost"), Is.Null);
            Assert.That(Property(manager, "IsBuildInteractionAvailable"), Is.EqualTo(true));
        }
        [Test]
        public void SceneComponentDisabled_CleansGhostAndRejectsLateCallback()
        {
            Call(controller, "RequestHint");
            var callback = (Delegate)Get(provider, "pending");
            Call(controller, "OnDisable");
            callback.DynamicInvoke(Enum.Parse(RuntimeType("BallGame.Placement.HintAdResult"), "Rewarded"));
            Assert.That(State, Is.EqualTo("Idle"));
            Assert.That(Get(controller, "ghost"), Is.Null);
            Assert.That(Property(manager, "IsBuildInteractionAvailable"), Is.EqualTo(true));
        }
        [Test]
        public void GameHudPrefab_HelpReferenceUsesExistingButton()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/GameHud.prefab");
            Assert.That(prefab, Is.Not.Null);
            var hud = prefab.GetComponent(RuntimeType("BallGame.UI.GameHudRefs"));
            var help = prefab.transform.Find("Top Bar/Help").GetComponent<Button>();
            Assert.That(new SerializedObject(hud).FindProperty("helpButton").objectReferenceValue, Is.SameAs(help));
        }
        [Test]
        public void GameManager_BindsExistingHelpWithoutSceneSetup_AndRebindIsIdempotent()
        {
            var hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(root.transform);
            hudObject.SetActive(false);
            var hud = hudObject.AddComponent(RuntimeType("BallGame.UI.GameHudRefs"));
            var helpObject = new GameObject("Help", typeof(RectTransform), typeof(Button));
            helpObject.transform.SetParent(hudObject.transform);
            var help = helpObject.GetComponent<Button>();
            Set(hud, "helpButton", help);
            Set(game, "hudRefs", hud);
            Set(game, "cameraController", camera);
            Call(game, "BindPlacementHints");
            Call(game, "BindPlacementHints");
            Assert.That(Get(controller, "hintButton"), Is.SameAs(help));
            Assert.That(root.GetComponents(controller.GetType()).Length, Is.EqualTo(1));
            Assert.That(Get(controller, "adProvider"), Is.SameAs(provider));
            button.onClick.Invoke();
            Assert.That(State, Is.EqualTo("Idle"), "Old button must be detached.");
            help.onClick.Invoke();
            Assert.That(State, Is.EqualTo("WaitingForAd"));
            var callback = Get(provider, "pending");
            Call(game, "BindPlacementHints");
            Assert.That(Get(provider, "pending"), Is.SameAs(callback));
            Assert.That(State, Is.EqualTo("WaitingForAd"), "Rebinding must not cancel an active reward request.");
        }
        [Test]
        public void Bind_AddsDefaultDevelopmentProviderOnlyWhenMissing()
        {
            var owner = new GameObject("Automatic Hint Owner");
            owner.transform.SetParent(root.transform);
            var hints = owner.AddComponent(controller.GetType());
            try
            {
                Call(hints, "Bind", game, manager, camera, button, null);
                var automaticProvider = Get(hints, "adProvider");
                Assert.That(automaticProvider.GetType().Name, Is.EqualTo("MockHintAdProvider"));
                Call(hints, "Bind", game, manager, camera, button, null);
                Assert.That(Get(hints, "adProvider"), Is.SameAs(automaticProvider));
                Assert.That(owner.GetComponents(RuntimeType("BallGame.Placement.HintRewardedAdProvider")).Length, Is.EqualTo(1));
            }
            finally
            {
                Call(hints, "OnDisable");
                Object.DestroyImmediate(owner);
            }
        }
        private void Activate()
        {
            Set(target, "cameraMoveDuration", 0f);
            Call(controller, "RequestHint");
            Call(provider, "CompleteRewarded");
            Call(camera, "LateUpdate");
            Call(controller, "Update");
            Assert.That(State, Is.EqualTo("Assisting"));
            Assert.That(Property(manager, "IsBuildInteractionAvailable"), Is.EqualTo(true));
        }
        private Component CreatePreview(Vector3 position)
        {
            var prefab = (GameObject)Property(definition, "Prefab");
            var instance = Object.Instantiate(prefab, root.transform);
            instance.transform.position = position;
            var tool = instance.GetComponent(RuntimeType("BallGame.Placement.PlaceableTool"));
            Call(tool, "Awake");
            Call(tool, "Initialize", definition);
            Call(tool, "SetPreview", true, true);
            Set(manager, "preview", tool);
            Set(manager, "activeDefinition", definition);
            return tool;
        }
        private object AddTarget(Vector3 position, float rotation)
        {
            var entry = Activator.CreateInstance(targetType);
            Set(entry, "tool", definition);
            Set(entry, "position", position);
            Set(entry, "rotationDegrees", rotation);
            ((IList)Get(level, "placementHints")).Add(entry);
            return entry;
        }
        private object PropertyCamera() => camera.GetComponent<Camera>();
        private string State => Property(controller, "State").ToString();
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static object Property(object obj, string name) => obj.GetType().GetProperty(name, Flags).GetValue(obj);
        private static object Get(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
        private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    }
}
