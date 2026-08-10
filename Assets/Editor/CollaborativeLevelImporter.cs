using System;
using System.Collections.Generic;
using System.IO;
using BallGame.Core;
using BallGame.LevelDesign;
using BallGame.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallGame.Editor
{
    public static class CollaborativeLevelImporter
    {
        private const string RegistryPath = "Assets/Resources/LevelDesigner/CollaborativeLevelAssetRegistry.asset";
        private const string TemplateScenePath = "Assets/Scenes/NormalLevels/Level_Template.unity";
        private const string SceneDirectory = "Assets/Scenes/NormalLevels";
        private const string DefinitionDirectory = "Assets/LevelDefinitions";
        private const string CatalogPath = "Assets/LevelDefinitions/LevelCatalog.asset";
        private const string SharedToolCatalogPath = "Assets/Tools/ToolCatalog.asset";

        [MenuItem("Ball Game/Level Design/Import Collaborative JSON...")]
        public static void ImportFromFile()
        {
            string jsonPath = EditorUtility.OpenFilePanel("Import Collaborative Level JSON", string.Empty, "json");
            if (string.IsNullOrEmpty(jsonPath)) return;
            string json = File.ReadAllText(jsonPath);
            ImportJson(json, Path.GetFileNameWithoutExtension(jsonPath));
        }

        public static bool ImportJson(string json, string suggestedName)
        {
            CollaborativeLevelDocument document;
            try
            {
                document = CollaborativeLevelDocument.FromJson(json);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Level Import Failed", exception.Message, "OK");
                return false;
            }

            CollaborativeLevelAssetRegistry registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>(RegistryPath);
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Level Import Failed", "Create the asset registry first from Ball Game > Level Design > Create Asset Registry.", "OK");
                return false;
            }

            List<string> errors = CollaborativeLevelValidator.Validate(document, registry);
            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("Level JSON Invalid", string.Join("\n", errors), "OK");
                return false;
            }

            ToolCatalog sharedToolCatalog = null;
            if (document.mode == "LowestCost")
            {
                sharedToolCatalog = AssetDatabase.LoadAssetAtPath<ToolCatalog>(SharedToolCatalogPath);
                if (sharedToolCatalog == null)
                {
                    EditorUtility.DisplayDialog(
                        "Level Import Failed",
                        "LowestCost requires the shared ToolCatalog at " + SharedToolCatalogPath
                            + ". Run Ball Game > Lowest Cost > Refresh Shared Tool Catalog, then import again.",
                        "OK");
                    return false;
                }
            }

            int levelNumber = GetNextLevelNumber();
            string slug = SanitizeName(string.IsNullOrWhiteSpace(suggestedName) ? document.title : suggestedName);
            string scenePath = SceneDirectory + "/Level_" + levelNumber.ToString("00") + "_" + slug + ".unity";
            string definitionPath = DefinitionDirectory + "/Level_" + levelNumber.ToString("00") + "_" + slug + ".asset";
            if (File.Exists(scenePath) || File.Exists(definitionPath))
            {
                EditorUtility.DisplayDialog("Level Import Stopped", "Output already exists; no files were changed:\n" + scenePath, "OK");
                return false;
            }

            if (!EditorUtility.DisplayDialog("Import Collaborative Level", "Create:\n" + scenePath + "\n" + definitionPath, "Create", "Cancel"))
                return false;

            return GenerateScene(document, registry, sharedToolCatalog, levelNumber, scenePath, definitionPath);
        }

        private static bool GenerateScene(CollaborativeLevelDocument document, CollaborativeLevelAssetRegistry registry, ToolCatalog sharedToolCatalog, int levelNumber, string scenePath, string definitionPath)
        {
            EnsureFolder(SceneDirectory);
            EnsureFolder(DefinitionDirectory);
            string templateScenePath = GetTemplateScenePath(document);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(templateScenePath) == null)
                throw new InvalidOperationException("Level template was not found: " + templateScenePath);
            if (!AssetDatabase.CopyAsset(templateScenePath, scenePath))
                throw new InvalidOperationException("Could not copy the level template " + templateScenePath + " to " + scenePath);
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
            Scene generated = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            RemoveTemplateBallAndGoal(generated, registry);
            Transform placedTools = FindOrCreateRoot("Placed Tools");
            Transform mapObstacles = FindOrCreateRoot("Map Obstacles");
            ClearChildren(placedTools);
            ClearChildren(mapObstacles);

            foreach (CollaborativeObjectRecord record in document.objects)
            {
                CollaborativeLevelAssetEntry entry = registry.Find(record.assetKey);
                if (entry == null || entry.prefab == null) continue;
                Transform parent = entry.kind == CollaborativeObjectKind.Tool || entry.kind == CollaborativeObjectKind.SplineTool ? placedTools : mapObstacles;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, generated);
                instance.transform.SetParent(parent, false);
                instance.name = record.id;
                instance.transform.localPosition = record.position;
                instance.transform.localRotation = Quaternion.Euler(0f, 0f, record.rotation);
                if (record.hasSize && SizeEditable2DUtility.TryGet(instance, out ISizeEditable2D editable))
                    editable.SetSize(record.size);
                if (record.kind == CollaborativeObjectKind.SplineTool)
                    CollaborativeSplineUtility.Apply(instance, record.splinePoints, record.splineWidth, record.splineClosed, record.splineCollisionMode);
                TrackIntersectionEditable2D trackIntersection = instance.GetComponent<TrackIntersectionEditable2D>();
                if (trackIntersection != null)
                {
                    CollaborativeTrackIntersectionData data = record.trackIntersection ?? trackIntersection.CaptureData();
                    trackIntersection.Apply(data);
                }
                if (record.assetKey == "portal") ApplyPortalPair(instance, record);
            }

            ApplyMapBounds(generated, document);

            LevelDefinition definition = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(definition, definitionPath);
            ConfigureLevelDefinition(definition, document, registry, sharedToolCatalog);
            EditorUtility.SetDirty(definition);

            AssignLevelDefinition(generated, definition);
            EditorSceneManager.SaveScene(generated, scenePath, true);
            AddToLevelCatalog(levelNumber, document.title, scenePath, definition);
            AddToBuildSettings(scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Level Imported", "Created " + scenePath, "OK");
            return true;
        }

        private static string GetTemplateScenePath(CollaborativeLevelDocument document)
        {
            return TemplateScenePath;
        }

        private static void ConfigureLevelDefinition(LevelDefinition definition, CollaborativeLevelDocument document, CollaborativeLevelAssetRegistry registry, ToolCatalog sharedToolCatalog)
        {
            bool lowestCost = document.mode == "LowestCost";
            SerializedObject definitionSerialized = new SerializedObject(definition);
            definitionSerialized.FindProperty("levelTitle").stringValue = document.title;
            definitionSerialized.FindProperty("mode").enumValueIndex = lowestCost ? (int)LevelMode.LowestCost : (int)LevelMode.Normal;
            definitionSerialized.FindProperty("toolCatalog").objectReferenceValue = lowestCost ? sharedToolCatalog : null;
            SerializedProperty tools = definitionSerialized.FindProperty("tools");
            tools.arraySize = 0;
            if (!lowestCost && document.toolStock != null)
            {
                foreach (CollaborativeToolStock stock in document.toolStock)
                {
                    if (stock == null || stock.count <= 0) continue;
                    CollaborativeLevelAssetEntry entry = registry.Find(stock.assetKey);
                    if (entry == null || entry.toolDefinition == null) continue;
                    int index = tools.arraySize++;
                    SerializedProperty item = tools.GetArrayElementAtIndex(index);
                    item.FindPropertyRelative("tool").objectReferenceValue = entry.toolDefinition;
                    item.FindPropertyRelative("count").intValue = stock.count;
                }
            }
            definitionSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveTemplateBallAndGoal(Scene scene, CollaborativeLevelAssetRegistry registry)
        {
            HashSet<GameObject> sourcePrefabs = new HashSet<GameObject>();
            foreach (CollaborativeLevelAssetEntry entry in registry.Entries)
            {
                if (entry == null || entry.prefab == null) continue;
                if (entry.kind == CollaborativeObjectKind.Ball || entry.kind == CollaborativeObjectKind.Goal)
                    sourcePrefabs.Add(entry.prefab);
            }

            HashSet<GameObject> instances = new HashSet<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transforms[i].gameObject);
                    if (instanceRoot == null) continue;
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceRoot);
                    if (sourcePrefabs.Contains(source)) instances.Add(instanceRoot);
                }
            }

            foreach (GameObject instance in instances)
                UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void AssignLevelDefinition(Scene scene, LevelDefinition definition)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameManager manager = root.GetComponentInChildren<GameManager>(true);
                if (manager == null) continue;
                SerializedObject serialized = new SerializedObject(manager);
                serialized.FindProperty("level").objectReferenceValue = definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
                return;
            }
        }

        private static void ApplyPortalPair(GameObject instance, CollaborativeObjectRecord record)
        {
            PortalTool portal = instance.GetComponentInChildren<PortalTool>();
            if (portal == null || record.properties == null) return;
            foreach (CollaborativeProperty property in record.properties)
            {
                if (property != null && property.key == "pairId" && int.TryParse(property.value, out int pairId))
                {
                    portal.SetPairId(pairId);
                    return;
                }
            }
        }

        private static void ApplyMapBounds(Scene scene, CollaborativeLevelDocument document)
        {
            MapBounds2D mapBounds = null;
            CameraController2D cameraController = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (mapBounds == null) mapBounds = root.GetComponentInChildren<MapBounds2D>(true);
                if (cameraController == null) cameraController = root.GetComponentInChildren<CameraController2D>(true);
            }
            if (mapBounds == null)
            {
                GameObject boundsObject = new GameObject("Map Bounds", typeof(MapBounds2D));
                SceneManager.MoveGameObjectToScene(boundsObject, scene);
                mapBounds = boundsObject.GetComponent<MapBounds2D>();
            }

            mapBounds.SetBounds(document.canvasCenter, new Vector2(document.canvasWidth, document.canvasHeight));
            EditorUtility.SetDirty(mapBounds);
            if (cameraController == null) return;
            SerializedObject serialized = new SerializedObject(cameraController);
            SerializedProperty property = serialized.FindProperty("mapBounds");
            if (property != null)
            {
                property.objectReferenceValue = mapBounds;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cameraController);
            }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
                if (scenes[i].path == scenePath) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddToLevelCatalog(int levelNumber, string title, string scenePath, LevelDefinition definition)
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Missing level catalog: " + CatalogPath);
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty levels = serialized.FindProperty("levels");
            int index = levels.arraySize++;
            SerializedProperty entry = levels.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("levelNumber").intValue = levelNumber;
            entry.FindPropertyRelative("sceneName").stringValue = Path.GetFileNameWithoutExtension(scenePath);
            entry.FindPropertyRelative("displayTitle").stringValue = levelNumber.ToString("00") + " " + title;
            entry.FindPropertyRelative("levelDefinition").objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static int GetNextLevelNumber()
        {
            int next = 1;
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { SceneDirectory });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.StartsWith("Level_", StringComparison.OrdinalIgnoreCase)) continue;
                string number = file.Substring(6, Math.Min(2, file.Length - 6));
                if (int.TryParse(number, out int value)) next = Mathf.Max(next, value + 1);
            }
            return next;
        }

        private static Transform FindOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing.transform;
            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, SceneManager.GetActiveScene());
            return created.transform;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Collaborative";
            char[] invalid = Path.GetInvalidFileNameChars();
            string result = value.Trim();
            for (int i = 0; i < invalid.Length; i++) result = result.Replace(invalid[i].ToString(), string.Empty);
            return string.IsNullOrEmpty(result) ? "Collaborative" : result.Replace(' ', '_');
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
