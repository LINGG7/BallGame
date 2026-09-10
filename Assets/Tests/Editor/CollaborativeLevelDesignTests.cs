using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallGame.Tests.Editor
{
    public sealed class CollaborativeLevelDesignTests
    {
        private const string RegistryPath = "Assets/LevelDesigner/CollaborativeLevelAssetRegistry.asset";
        private const string ScenePath = "Assets/Scenes/LevelDesigner.unity";

        [Test]
        public void WoodSize_ResizesMeshAndColliderWithoutChangingRootScale()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Wood_3D.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Type sizeType = Type.GetType("BallGame.Core.WoodSize2D, Assembly-CSharp");
                Component component = instance.GetComponent(sizeType);
                Assert.That(component, Is.Not.Null);
                Vector3 rootScale = instance.transform.localScale;
                Transform visual = instance.transform.Find("Visual");
                float depth = visual.localScale.z;
                MethodInfo setSize = sizeType.GetMethod("SetSize");
                setSize.Invoke(component, new object[] { new Vector2(6f, 2f) });
                Assert.That(instance.transform.localScale, Is.EqualTo(rootScale));
                Assert.That(visual.localScale, Is.EqualTo(new Vector3(2f, 4f, depth)));
                Assert.That(instance.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(2f, 4f)));
                Assert.That(sizeType.GetProperty("Size").GetValue(component), Is.EqualTo(new Vector2(6f, 2f)));
                setSize.Invoke(component, new object[] { new Vector2(3f, 0.5f) });
                Assert.That(visual.localScale, Is.EqualTo(Vector3.one), "Repeated edits must not accumulate scaling.");
                Assert.That(instance.GetComponent<BoxCollider2D>().size, Is.EqualTo(Vector2.one));
                Assert.That(prefab.transform.localScale, Is.EqualTo(rootScale));
                Assert.That(prefab.GetComponent<BoxCollider2D>().size, Is.EqualTo(Vector2.one), "Resizing an instance must not modify the prefab.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
        [Test]
        public void DesignerAssets_ExistAndRegistryUsesStableKeys()
        {
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty entries = serialized.FindProperty("entries");
            Assert.That(entries.arraySize, Is.GreaterThanOrEqualTo(10));
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                Assert.That(entry.FindPropertyRelative("assetKey").stringValue, Is.Not.Empty);
                Assert.That(entry.FindPropertyRelative("prefab").objectReferenceValue, Is.Not.Null);
            }
        }

        [Test]
        public void EmptyDocument_IsRejectedForMissingBallAndGoal()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type registryType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelAssetRegistry, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            Assert.That(documentType, Is.Not.Null);
            Assert.That(registryType, Is.Not.Null);
            Assert.That(validatorType, Is.Not.Null);
            object document = Activator.CreateInstance(documentType);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            Assert.That(errors.Count, Is.GreaterThanOrEqualTo(2));
            string combined = string.Join("\n", errors.CastToStrings());
            StringAssert.Contains("Ball", combined);
            StringAssert.Contains("Goal", combined);
        }

        [Test]
        public void CurrentDocument_UsesVersionFourAndRejectsLegacyTransformScale()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            Assert.That(documentType, Is.Not.Null);
            object current = Activator.CreateInstance(documentType);
            Assert.That((int)documentType.GetField("formatVersion").GetValue(current), Is.EqualTo(4));
            Assert.That((string)documentType.GetField("mode").GetValue(current), Is.EqualTo("Normal"));
            Assert.That(((IList)documentType.GetField("toolStock").GetValue(current)).Count, Is.EqualTo(0));

            string json = "{\"formatVersion\":1,\"title\":\"Legacy\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"position\":{\"x\":0,\"y\":0},\"scale\":{\"x\":2,\"y\":1}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"position\":{\"x\":2,\"y\":0},\"scale\":{\"x\":1,\"y\":1}}]}";
            object document = JsonUtility.FromJson(json, documentType);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            StringAssert.Contains("must match the registered prefab scale", string.Join("\n", errors.CastToStrings()));
        }

        [Test]
        public void VersionTwo_RejectsSizeForNonResizableObject()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            string json = "{\"formatVersion\":2,\"title\":\"Invalid Size\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"position\":{\"x\":0,\"y\":0},\"scale\":{\"x\":1,\"y\":1},\"hasSize\":true,\"size\":{\"x\":2,\"y\":2}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"position\":{\"x\":2,\"y\":0},\"scale\":{\"x\":1,\"y\":1}}]}";
            object document = JsonUtility.FromJson(json, documentType);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            StringAssert.Contains("does not support size editing", string.Join("\n", errors.CastToStrings()));
        }

        [Test]
        public void VersionTwo_DefaultsCanvasCenterToOrigin()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            object document = JsonUtility.FromJson("{\"formatVersion\":2,\"title\":\"Legacy\",\"canvasWidth\":24,\"canvasHeight\":14}", documentType);
            Vector2 center = (Vector2)documentType.GetField("canvasCenter").GetValue(document);
            Assert.That(center, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void InfiniteBackgroundPrefab_UsesTiledBackgroundComponent()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/WorldBackground.prefab");
            Assert.That(prefab, Is.Not.Null);
            Type backgroundType = Type.GetType("BallGame.Core.InfiniteTiledBackground2D, Assembly-CSharp");
            Assert.That(prefab.GetComponent(backgroundType), Is.Not.Null);
        }

        [Test]
        public void DesignerSelection_UsesToolClickOrderThenNewerSibling()
        {
            Type controllerType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDesignerController, Assembly-CSharp");
            Type markerType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelObject, Assembly-CSharp");
            Type registryType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelAssetRegistry, Assembly-CSharp");
            Type entryType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelAssetEntry, Assembly-CSharp");
            Type toolType = Type.GetType("BallGame.Core.ToolDefinition, Assembly-CSharp");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(markerType, Is.Not.Null);
            Assert.That(registryType, Is.Not.Null);
            Assert.That(entryType, Is.Not.Null);
            Assert.That(toolType, Is.Not.Null);

            GameObject host = new GameObject("Inactive Designer Selection Test");
            host.SetActive(false);
            GameObject world = new GameObject("Selection World");
            ScriptableObject registry = ScriptableObject.CreateInstance(registryType);
            ScriptableObject toolDefinition = ScriptableObject.CreateInstance(toolType);
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            try
            {
                object withoutDefinitionEntry = Activator.CreateInstance(entryType);
                entryType.GetField("assetKey").SetValue(withoutDefinitionEntry, "without_definition");
                object toolEntry = Activator.CreateInstance(entryType);
                entryType.GetField("assetKey").SetValue(toolEntry, "tool");
                entryType.GetField("toolDefinition").SetValue(toolEntry, toolDefinition);
                IList entries = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entryType));
                entries.Add(withoutDefinitionEntry);
                entries.Add(toolEntry);
                registryType.GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(registry, entries);

                GameObject withoutDefinition = new GameObject("Without Definition", typeof(SpriteRenderer), typeof(BoxCollider2D));
                withoutDefinition.transform.SetParent(world.transform, false);
                withoutDefinition.GetComponent<SpriteRenderer>().sprite = sprite;
                Component withoutDefinitionMarker = withoutDefinition.AddComponent(markerType);
                GameObject tool = new GameObject("Tool", typeof(SpriteRenderer), typeof(BoxCollider2D));
                tool.transform.SetParent(world.transform, false);
                tool.GetComponent<SpriteRenderer>().sprite = sprite;
                Component toolMarker = tool.AddComponent(markerType);
                MethodInfo initialize = markerType.GetMethod("Initialize", new[] { typeof(string), entryType });
                initialize.Invoke(withoutDefinitionMarker, new[] { "without_definition_1", withoutDefinitionEntry });
                initialize.Invoke(toolMarker, new[] { "tool_2", toolEntry });

                Component controller = host.AddComponent(controllerType);
                controllerType.GetField("registry", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, registry);
                controllerType.GetField("worldRoot", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, world.transform);
                MethodInfo findObject = controllerType.GetMethod("FindObjectAt", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo isObjectAt = controllerType.GetMethod("IsObjectAt", BindingFlags.NonPublic | BindingFlags.Static);
                FieldInfo clickOrder = toolType.GetField("clickSortingOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                Collider2D[] hits = { tool.GetComponent<BoxCollider2D>(), withoutDefinition.GetComponent<BoxCollider2D>() };

                Assert.That((bool)isObjectAt.Invoke(null, new object[] { withoutDefinitionMarker, Vector2.zero, hits }), Is.True,
                    "A selected object under the pointer must remain eligible for direct dragging even when objects overlap.");
                Assert.That((bool)isObjectAt.Invoke(null, new object[] { withoutDefinitionMarker, new Vector2(5f, 5f), Array.Empty<Collider2D>() }), Is.False);

                clickOrder.SetValue(toolDefinition, 4);
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, hits }), Is.EqualTo(withoutDefinitionMarker),
                    "Objects without a ToolDefinition must use the default click order 5.");
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, Array.Empty<Collider2D>() }), Is.EqualTo(withoutDefinitionMarker),
                    "Sprite bounds fallback must use the same click ordering.");

                clickOrder.SetValue(toolDefinition, 5);
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, hits }), Is.EqualTo(toolMarker),
                    "The newer sibling must win when click orders are equal.");

                clickOrder.SetValue(toolDefinition, 6);
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, Array.Empty<Collider2D>() }), Is.EqualTo(toolMarker),
                    "A higher ToolDefinition click order must win in sprite bounds fallback.");

                PropertyInfo selectionLocked = markerType.GetProperty("SelectionLocked");
                Assert.That(selectionLocked.GetValue(toolMarker), Is.False, "New objects default to unlocked.");
                selectionLocked.SetValue(toolMarker, true);
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, hits }), Is.EqualTo(withoutDefinitionMarker),
                    "A locked front object must allow collider selection of the object behind it.");
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, Array.Empty<Collider2D>() }), Is.EqualTo(withoutDefinitionMarker),
                    "Renderer bounds fallback must also skip locked objects.");

                Assert.That(isObjectAt.Invoke(null, new object[] { toolMarker, Vector2.zero, hits }), Is.False,
                    "An already selected locked object must not bypass world selection filtering.");
                object lockedRecord = markerType.GetMethod("ToRecord").Invoke(toolMarker, null);
                string lockedJson = JsonUtility.ToJson(lockedRecord);
                object restoredRecord = JsonUtility.FromJson(lockedJson, lockedRecord.GetType());
                selectionLocked.SetValue(toolMarker, false);
                markerType.GetMethod("Initialize", new[] { restoredRecord.GetType() }).Invoke(toolMarker, new[] { restoredRecord });
                Assert.That(selectionLocked.GetValue(toolMarker), Is.True, "Lock state must survive JSON round trips.");
                selectionLocked.SetValue(toolMarker, false);
                Assert.That(findObject.Invoke(controller, new object[] { Vector2.zero, hits }), Is.EqualTo(toolMarker),
                    "Unlocking must restore world selection.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(world);
                UnityEngine.Object.DestroyImmediate(registry);
                UnityEngine.Object.DestroyImmediate(toolDefinition);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ObstacleSelection_CreatesTwoSizeXEndpointsAndKeepsOppositeEndpointFixed()
        {
            Type controllerType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDesignerController, Assembly-CSharp");
            Type markerType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelObject, Assembly-CSharp");
            Type handleType = Type.GetType("BallGame.LevelDesign.CollaborativeSplineHandle, Assembly-CSharp");
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            object obstacleEntry = registry.GetType().GetMethod("Find").Invoke(registry, new object[] { "obstacle" });
            GameObject prefab = (GameObject)obstacleEntry.GetType().GetField("prefab").GetValue(obstacleEntry);
            GameObject host = new GameObject("Inactive Obstacle Endpoint Test");
            host.SetActive(false);
            GameObject obstacle = UnityEngine.Object.Instantiate(prefab);
            GameObject handleRoot = new GameObject("Obstacle Endpoint Handles");
            try
            {
                Component marker = obstacle.AddComponent(markerType);
                markerType.GetMethod("Initialize", new[] { typeof(string), obstacleEntry.GetType() })
                    .Invoke(marker, new[] { "obstacle_1", obstacleEntry });
                Component editable = obstacle.GetComponentInChildren(Type.GetType("BallGame.Core.ObstacleSize2D, Assembly-CSharp"), true);
                editable.GetType().GetMethod("SetSize").Invoke(editable, new object[] { new Vector2(4f, 1f) });
                obstacle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

                Component controller = host.AddComponent(controllerType);
                controllerType.GetField("selected", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, marker);
                controllerType.GetField("splineHandleRoot", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, handleRoot.transform);
                controllerType.GetMethod("RebuildSplineHandles", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller, null);

                Component[] handles = handleRoot.GetComponentsInChildren(handleType, true);
                Assert.That(handles.Length, Is.EqualTo(2));
                CollectionAssert.AreEquivalent(new[] { -1, 1 }, new[]
                {
                    (int)handleType.GetProperty("SizeHandleSide").GetValue(handles[0]),
                    (int)handleType.GetProperty("SizeHandleSide").GetValue(handles[1])
                });
                Assert.That(Array.TrueForAll(handles,
                    handle => (bool)handleType.GetProperty("IsObstacleSizeHandle").GetValue(handle)), Is.True);

                MethodInfo resize = controllerType.GetMethod("ResizeObstacleFromEndpoint", BindingFlags.NonPublic | BindingFlags.Static);
                bool resized = (bool)resize.Invoke(null, new object[]
                {
                    marker,
                    1,
                    new Vector2(0f, -2f),
                    Vector2.up,
                    new Vector2(0f, 4f)
                });
                Vector2 size = (Vector2)editable.GetType().GetProperty("Size").GetValue(editable);
                Assert.That(resized, Is.True);
                Assert.That(size.x, Is.EqualTo(6f).Within(0.001f));
                Assert.That(size.y, Is.EqualTo(1f).Within(0.001f));
                Assert.That(Vector2.Distance(obstacle.transform.position, new Vector2(0f, 1f)), Is.LessThan(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(obstacle);
                UnityEngine.Object.DestroyImmediate(handleRoot);
            }
        }

        [Test]
        public void DesignerUi_ContainsSizeInputsAndPersistentLabels()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LevelDesigner/DesignerUI.prefab");
            UnityEngine.Object designerFont = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/LevelDesigner/Fonts/LXGWWenKai-Light Dynamic SDF.asset");
            GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LevelDesigner/LevelSettingsPanel.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(designerFont, Is.Not.Null);
            Assert.That(designerFont.GetType().GetProperty("atlasPopulationMode").GetValue(designerFont).ToString(), Is.EqualTo("Dynamic"));
            Assert.That(settingsPrefab, Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size XInput"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size YInput"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size X Label"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Size Y Label"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Scale XInput"), Is.Null);
            Assert.That(FindChild(prefab.transform, "Scale YInput"), Is.Null);
            Assert.That(FindChild(prefab.transform, "Level SettingsButton"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "track_intersectionButton"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Track PartDropdown"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Entry XInput"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Entry YInput"), Is.Not.Null);
            Assert.That(FindChild(prefab.transform, "Entry RotationInput"), Is.Not.Null);
            Transform palette = FindChild(prefab.transform, "Palette");
            Assert.That(palette, Is.Not.Null);
            Transform scrollView = FindChild(palette, "ToolScrollView");
            Assert.That(scrollView, Is.Not.Null);
            Transform content = FindChild(scrollView, "Content");
            Transform scrollbarObject = FindChild(palette, "Scrollbar");
            RectTransform paletteRect = palette as RectTransform;
            Assert.That(paletteRect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(paletteRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(paletteRect.sizeDelta.x, Is.EqualTo(-350f).Within(0.01f));
            Assert.That(FindChild(palette, "Title").parent, Is.EqualTo(palette));
            Assert.That(content, Is.Not.Null);
            GridLayoutGroup paletteGrid = content.GetComponent<GridLayoutGroup>();
            Assert.That(paletteGrid, Is.Not.Null);
            Assert.That(content.GetComponent<VerticalLayoutGroup>(), Is.Null);
            Assert.That(paletteGrid.cellSize, Is.EqualTo(new Vector2(230f, 48f)));
            Assert.That(paletteGrid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(paletteGrid.constraintCount, Is.EqualTo(10));
            Assert.That(content.GetComponent<ContentSizeFitter>(), Is.Not.Null);
            Assert.That(scrollbarObject, Is.Not.Null);
            ScrollRect paletteScroll = scrollView.GetComponent<ScrollRect>();
            Assert.That(paletteScroll, Is.Not.Null);
            Assert.That(paletteScroll.content, Is.EqualTo(content));
            Assert.That(paletteScroll.horizontal, Is.False);
            Assert.That(paletteScroll.vertical, Is.True);
            Assert.That(paletteScroll.verticalScrollbar, Is.EqualTo(scrollbarObject.GetComponent<Scrollbar>()));
            Transform placedHierarchy = FindChild(prefab.transform, "PlacedHierarchy");
            Assert.That(placedHierarchy, Is.Not.Null);
            RectTransform hierarchyRect = placedHierarchy as RectTransform;
            Assert.That(hierarchyRect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(hierarchyRect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(hierarchyRect.offsetMax.x, Is.EqualTo(270f).Within(0.01f));
            Transform hierarchyScrollObject = FindChild(placedHierarchy, "PlacedHierarchyScrollView");
            Transform hierarchyContent = FindChild(placedHierarchy, "PlacedHierarchyContent");
            Transform hierarchyTemplate = FindChild(placedHierarchy, "HierarchyItemTemplate");
            Assert.That(hierarchyScrollObject, Is.Not.Null);
            Assert.That(hierarchyContent, Is.Not.Null);
            Assert.That(hierarchyContent.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(hierarchyContent.GetComponent<ContentSizeFitter>(), Is.Not.Null);
            Assert.That(hierarchyTemplate, Is.Not.Null);
            Assert.That(hierarchyTemplate.GetComponent<Button>(), Is.Not.Null);
            Assert.That(hierarchyTemplate.gameObject.activeSelf, Is.False);
            ScrollRect hierarchyScroll = hierarchyScrollObject.GetComponent<ScrollRect>();
            Assert.That(hierarchyScroll, Is.Not.Null);
            Assert.That(hierarchyScroll.content, Is.EqualTo(hierarchyContent));
            Assert.That(hierarchyScroll.horizontal, Is.False);
            Assert.That(hierarchyScroll.vertical, Is.True);
            SerializedProperty paletteEntries = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath)).FindProperty("entries");
            for (int i = 0; i < paletteEntries.arraySize; i++)
            {
                string assetKey = paletteEntries.GetArrayElementAtIndex(i).FindPropertyRelative("assetKey").stringValue;
                Transform toolButton = FindChild(content, assetKey + "Button");
                Assert.That(toolButton, Is.Not.Null, "Palette is missing " + assetKey + "Button.");
                Assert.That(toolButton.parent, Is.EqualTo(content));
            }
            Type inputType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            Component titleInput = FindChild(prefab.transform, "Level titleInput").GetComponent(inputType);
            Assert.That(titleInput, Is.Not.Null);
            Assert.That(inputType.GetProperty("characterValidation").GetValue(titleInput).ToString(), Is.EqualTo("None"));
            object textComponent = inputType.GetProperty("textComponent").GetValue(titleInput);
            Assert.That(textComponent.GetType().GetProperty("font").GetValue(textComponent), Is.EqualTo(designerFont));
            Assert.That(FindChild(settingsPrefab.transform, "LevelModeDropdown"), Is.Not.Null);
            Assert.That(FindChild(settingsPrefab.transform, "Save SettingsButton"), Is.Not.Null);
            Assert.That(FindChild(settingsPrefab.transform, "plank StockInput"), Is.Not.Null);
            Assert.That(FindChild(settingsPrefab.transform, "track_intersection StockInput"), Is.Null);
        }

        [Test]
        public void FormalBuildScenes_DoNotReferenceDesignerDynamicFont()
        {
            const string designerFontPath = "Assets/LevelDesigner/Fonts/LXGWWenKai-Light Dynamic SDF.asset";
            HashSet<string> dependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || scene.path == ScenePath) continue;
                foreach (string dependency in AssetDatabase.GetDependencies(scene.path, true))
                    dependencies.Add(dependency);
            }

            Assert.That(dependencies.Contains(designerFontPath), Is.False,
                "The Level Designer dynamic font must not be referenced by formal build scenes.");
        }

        [Test]
        public void Registry_OnlyExposesSizeCapabilityForApprovedObjects()
        {
            Type interfaceType = Type.GetType("BallGame.Core.ISizeEditable2D, Assembly-CSharp");
            Assert.That(interfaceType, Is.Not.Null);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            SerializedProperty entries = new SerializedObject(registry).FindProperty("entries");
            string[] approved = { "anti_gravity", "water", "obstacle" };
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string key = entry.FindPropertyRelative("assetKey").stringValue;
                GameObject prefab = (GameObject)entry.FindPropertyRelative("prefab").objectReferenceValue;
                bool supportsSize = false;
                foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    supportsSize |= behaviour != null && interfaceType.IsInstanceOfType(behaviour);
                Assert.That(supportsSize, Is.EqualTo(Array.IndexOf(approved, key) >= 0), key);
            }
        }

        [Test]
        public void Registry_ObstacleIsNotAnInventoryTool()
        {
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo find = registry.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance);
            object obstacle = find.Invoke(registry, new object[] { "obstacle" });
            object plank = find.Invoke(registry, new object[] { "plank" });
            object trackIntersection = find.Invoke(registry, new object[] { "track_intersection" });
            PropertyInfo isInventoryTool = obstacle.GetType().GetProperty("IsInventoryTool", BindingFlags.Public | BindingFlags.Instance);
            Assert.That((bool)isInventoryTool.GetValue(obstacle), Is.False);
            Assert.That(trackIntersection, Is.Not.Null);
            Assert.That((bool)isInventoryTool.GetValue(trackIntersection), Is.False);
            Assert.That((bool)isInventoryTool.GetValue(plank), Is.True);
        }

        [Test]
        public void TrackIntersectionPrefab_HasEditableCompoundDataAndRoundTrips()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/TrackIntersection.prefab");
            Type editableType = Type.GetType("BallGame.LevelDesign.TrackIntersectionEditable2D, Assembly-CSharp");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(editableType, Is.Not.Null);
            Component prefabEditable = prefab.GetComponent(editableType);
            Assert.That(prefabEditable, Is.Not.Null);
            Assert.That((bool)editableType.GetProperty("IsConfigured").GetValue(prefabEditable), Is.True);

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Component editable = instance.GetComponent(editableType);
                MethodInfo capture = editableType.GetMethod("CaptureData");
                MethodInfo apply = editableType.GetMethod("Apply");
                object data = capture.Invoke(editable, new object[] { null });
                Type dataType = data.GetType();
                object upper = dataType.GetField("upperCurve").GetValue(data);
                object lower = dataType.GetField("lowerCurve").GetValue(data);
                IList upperPoints = (IList)upper.GetType().GetField("points").GetValue(upper);
                IList lowerPoints = (IList)lower.GetType().GetField("points").GetValue(lower);
                Assert.That(upperPoints.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(lowerPoints.Count, Is.GreaterThanOrEqualTo(2));
                int originalLowerCount = lowerPoints.Count;
                upper.GetType().GetField("width").SetValue(upper, 0.73f);

                object upperEntry = dataType.GetField("upperEntryA").GetValue(data);
                upperEntry.GetType().GetField("positionOffset").SetValue(upperEntry, new Vector2(0.25f, -0.15f));
                upperEntry.GetType().GetField("rotationOffset").SetValue(upperEntry, 12f);
                bool attachToStart = (bool)upperEntry.GetType().GetField("attachToStart").GetValue(upperEntry);
                object attachedPoint = upperPoints[attachToStart ? 0 : upperPoints.Count - 1];
                FieldInfo pointPosition = attachedPoint.GetType().GetField("position");
                pointPosition.SetValue(attachedPoint, (Vector2)pointPosition.GetValue(attachedPoint) + new Vector2(0.4f, 0.2f));
                Transform entryTransform = (Transform)editableType.GetMethod("GetEntry").Invoke(editable, new object[] { true });
                Vector3 entryBefore = entryTransform.position;

                string json = JsonUtility.ToJson(data);
                object parsed = JsonUtility.FromJson(json, dataType);
                apply.Invoke(editable, new[] { parsed });
                Assert.That(Vector3.Distance(entryTransform.position, entryBefore), Is.GreaterThan(0.05f));
                object captured = capture.Invoke(editable, new[] { parsed });
                object capturedUpper = dataType.GetField("upperCurve").GetValue(captured);
                object capturedLower = dataType.GetField("lowerCurve").GetValue(captured);
                object capturedEntry = dataType.GetField("upperEntryA").GetValue(captured);
                Assert.That((float)capturedUpper.GetType().GetField("width").GetValue(capturedUpper), Is.EqualTo(0.73f).Within(0.001f));
                Assert.That(((IList)capturedLower.GetType().GetField("points").GetValue(capturedLower)).Count, Is.EqualTo(originalLowerCount));
                Vector2 capturedOffset = (Vector2)capturedEntry.GetType().GetField("positionOffset").GetValue(capturedEntry);
                Assert.That(Vector2.Distance(capturedOffset, new Vector2(0.25f, -0.15f)), Is.LessThan(0.001f));
                Assert.That((float)capturedEntry.GetType().GetField("rotationOffset").GetValue(capturedEntry), Is.EqualTo(12f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void TrackIntersection_VersionFourRequiresCompoundDataButVersionThreeUsesPrefabDefaults()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            MethodInfo fromJson = documentType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            string objects = "["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"scale\":{\"x\":1,\"y\":1}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"scale\":{\"x\":1,\"y\":1}},"
                + "{\"id\":\"track\",\"assetKey\":\"track_intersection\",\"kind\":2,\"scale\":{\"x\":1,\"y\":1}}]";
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);

            object versionThree = fromJson.Invoke(null, new object[] { "{\"formatVersion\":3,\"title\":\"Legacy Track\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":" + objects + "}" });
            IList v3Errors = (IList)validate.Invoke(null, new[] { versionThree, registry });
            StringAssert.DoesNotContain("compound edit data", string.Join("\n", v3Errors.CastToStrings()));

            object versionFour = fromJson.Invoke(null, new object[] { "{\"formatVersion\":4,\"title\":\"V4 Track\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":" + objects + "}" });
            IList v4Errors = (IList)validate.Invoke(null, new[] { versionFour, registry });
            StringAssert.Contains("compound edit data", string.Join("\n", v4Errors.CastToStrings()));
        }

        [Test]
        public void NonTrackObjects_IgnoreJsonUtilityGeneratedTrackIntersectionDefaults()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            MethodInfo fromJson = documentType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            string emptyCompound = "\"trackIntersection\":{\"upperCurve\":{\"width\":0.5,\"points\":[]},\"lowerCurve\":{\"width\":0.5,\"points\":[]}}";
            string json = "{\"formatVersion\":4,\"title\":\"Generated Defaults\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"scale\":{\"x\":1,\"y\":1}," + emptyCompound + "},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"scale\":{\"x\":1,\"y\":1}," + emptyCompound + "}]}";
            object document = fromJson.Invoke(null, new object[] { json });
            IList objects = (IList)documentType.GetField("objects").GetValue(document);
            Assert.That(objects[0].GetType().GetField("trackIntersection").GetValue(objects[0]), Is.Null);
            Assert.That(objects[1].GetType().GetField("trackIntersection").GetValue(objects[1]), Is.Null);

            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            StringAssert.DoesNotContain("trackIntersection", string.Join("\n", errors.CastToStrings()));
        }

        [Test]
        public void Importer_RemovesTemplateBallAndGoalBeforeCreatingJsonObjects()
        {
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Ball.prefab");
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Goal.prefab");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                GameObject ball = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab, scene);
                GameObject goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab, scene);
                GameObject survivor = new GameObject("Survivor");
                SceneManager.MoveGameObjectToScene(survivor, scene);

                Type importerType = Type.GetType("BallGame.Editor.CollaborativeLevelImporter, Assembly-CSharp-Editor");
                MethodInfo removeTemplateObjects = importerType.GetMethod(
                    "RemoveTemplateBallAndGoal",
                    BindingFlags.NonPublic | BindingFlags.Static);
                removeTemplateObjects.Invoke(null, new object[] { scene, registry });

                Assert.That(ball == null, Is.True);
                Assert.That(goal == null, Is.True);
                Assert.That(survivor == null, Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Importer_UsesLevelTemplateForNormalMode()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type importerType = Type.GetType("BallGame.Editor.CollaborativeLevelImporter, Assembly-CSharp-Editor");
            MethodInfo getTemplate = importerType.GetMethod("GetTemplateScenePath", BindingFlags.NonPublic | BindingFlags.Static);
            object normal = Activator.CreateInstance(documentType);
            object lowestCost = Activator.CreateInstance(documentType);
            documentType.GetField("mode").SetValue(normal, "Normal");
            documentType.GetField("mode").SetValue(lowestCost, "LowestCost");

            Assert.That(getTemplate.Invoke(null, new[] { normal }), Is.EqualTo("Assets/Scenes/NormalLevels/Level_Template.unity"));
            Assert.That(getTemplate.Invoke(null, new[] { lowestCost }), Is.EqualTo("Assets/Scenes/NormalLevels/Level_Template.unity"));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/NormalLevels/Level_Template.unity"), Is.Not.Null);
        }

        [Test]
        public void LegacyDocument_DefaultsToNormalAndInvalidModeIsRejected()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            MethodInfo fromJson = documentType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            string objects = "["
                + "{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"scale\":{\"x\":1,\"y\":1}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"scale\":{\"x\":1,\"y\":1}}]";
            object legacy = fromJson.Invoke(null, new object[] { "{\"formatVersion\":3,\"title\":\"Legacy\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":" + objects + "}" });
            Assert.That((string)documentType.GetField("mode").GetValue(legacy), Is.EqualTo("Normal"));

            object invalid = fromJson.Invoke(null, new object[] { "{\"formatVersion\":3,\"title\":\"Invalid\",\"mode\":\"Arcade\",\"canvasWidth\":24,\"canvasHeight\":14,\"objects\":" + objects + "}" });
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            IList errors = (IList)validate.Invoke(null, new[] { invalid, registry });
            StringAssert.Contains("mode must be Normal or LowestCost", string.Join("\n", errors.CastToStrings()));
        }

        [Test]
        public void Validator_RejectsInvalidManualToolStock()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type validatorType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelValidator, Assembly-CSharp");
            MethodInfo fromJson = documentType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static);
            string json = "{\"formatVersion\":3,\"title\":\"Invalid Stock\",\"mode\":\"Normal\",\"canvasWidth\":24,\"canvasHeight\":14,"
                + "\"toolStock\":[{\"assetKey\":\"plank\",\"count\":-1},{\"assetKey\":\"plank\",\"count\":2},{\"assetKey\":\"missing\",\"count\":1}],"
                + "\"objects\":[{\"id\":\"ball\",\"assetKey\":\"ball\",\"kind\":0,\"scale\":{\"x\":1,\"y\":1}},"
                + "{\"id\":\"goal\",\"assetKey\":\"goal\",\"kind\":1,\"scale\":{\"x\":1,\"y\":1}}]}";
            object document = fromJson.Invoke(null, new object[] { json });
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            MethodInfo validate = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            IList errors = (IList)validate.Invoke(null, new[] { document, registry });
            string combined = string.Join("\n", errors.CastToStrings());
            StringAssert.Contains("unique", combined);
            StringAssert.Contains("cannot be negative", combined);
            StringAssert.Contains("Unknown tool stock assetKey: missing", combined);
        }

        [Test]
        public void CaptureDocument_PreservesManualToolStockWithoutSceneTools()
        {
            Type controllerType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDesignerController, Assembly-CSharp");
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            MethodInfo fromJson = documentType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static);
            object document = fromJson.Invoke(null, new object[] { "{\"formatVersion\":3,\"title\":\"Stock\",\"mode\":\"Normal\",\"canvasWidth\":24,\"canvasHeight\":14,\"toolStock\":[{\"assetKey\":\"plank\",\"count\":4}]}" });
            GameObject host = new GameObject("Inactive Designer Test");
            host.SetActive(false);
            try
            {
                Component controller = host.AddComponent(controllerType);
                GameObject world = new GameObject("World");
                world.transform.SetParent(host.transform, false);
                controllerType.GetField("document", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, document);
                controllerType.GetField("worldRoot", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, world.transform);
                MethodInfo capture = controllerType.GetMethod("CaptureDocument", BindingFlags.NonPublic | BindingFlags.Instance);
                object captured = capture.Invoke(controller, null);
                IList stock = (IList)documentType.GetField("toolStock").GetValue(captured);
                Assert.That(stock.Count, Is.EqualTo(1));
                Assert.That((string)stock[0].GetType().GetField("assetKey").GetValue(stock[0]), Is.EqualTo("plank"));
                Assert.That((int)stock[0].GetType().GetField("count").GetValue(stock[0]), Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Importer_ConfiguresNormalAndLowestCostDefinitions()
        {
            Type documentType = Type.GetType("BallGame.LevelDesign.CollaborativeLevelDocument, Assembly-CSharp");
            Type levelDefinitionType = Type.GetType("BallGame.Core.LevelDefinition, Assembly-CSharp");
            Type importerType = Type.GetType("BallGame.Editor.CollaborativeLevelImporter, Assembly-CSharp-Editor");
            MethodInfo fromJson = documentType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static);
            MethodInfo configure = importerType.GetMethod("ConfigureLevelDefinition", BindingFlags.NonPublic | BindingFlags.Static);
            UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RegistryPath);
            UnityEngine.Object catalog = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Tools/ToolCatalog.asset");
            ScriptableObject normal = ScriptableObject.CreateInstance(levelDefinitionType);
            ScriptableObject lowestCost = ScriptableObject.CreateInstance(levelDefinitionType);
            try
            {
                object normalDocument = fromJson.Invoke(null, new object[] { "{\"formatVersion\":3,\"title\":\"Normal Level\",\"mode\":\"Normal\",\"toolStock\":[{\"assetKey\":\"plank\",\"count\":3},{\"assetKey\":\"spring\",\"count\":0}]}" });
                configure.Invoke(null, new[] { normal, normalDocument, registry, null });
                SerializedObject normalSerialized = new SerializedObject(normal);
                Assert.That(normalSerialized.FindProperty("mode").enumValueIndex, Is.EqualTo(0));
                Assert.That(normalSerialized.FindProperty("toolCatalog").objectReferenceValue, Is.Null);
                Assert.That(normalSerialized.FindProperty("tools").arraySize, Is.EqualTo(1));
                Assert.That(normalSerialized.FindProperty("tools").GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue, Is.EqualTo(3));

                object lowestDocument = fromJson.Invoke(null, new object[] { "{\"formatVersion\":3,\"title\":\"Lowest Level\",\"mode\":\"LowestCost\",\"toolStock\":[{\"assetKey\":\"plank\",\"count\":3}]}" });
                configure.Invoke(null, new[] { lowestCost, lowestDocument, registry, catalog });
                SerializedObject lowestSerialized = new SerializedObject(lowestCost);
                Assert.That(lowestSerialized.FindProperty("mode").enumValueIndex, Is.EqualTo(1));
                Assert.That(lowestSerialized.FindProperty("toolCatalog").objectReferenceValue, Is.EqualTo(catalog));
                Assert.That(lowestSerialized.FindProperty("tools").arraySize, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(normal);
                UnityEngine.Object.DestroyImmediate(lowestCost);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name == name) return children[i];
            return null;
        }
    }

    internal static class CollaborativeTestListExtensions
    {
        public static string[] CastToStrings(this IList values)
        {
            string[] result = new string[values.Count];
            for (int i = 0; i < values.Count; i++) result[i] = values[i]?.ToString();
            return result;
        }
    }
}
