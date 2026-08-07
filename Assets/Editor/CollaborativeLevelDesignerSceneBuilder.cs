using System.Collections.Generic;
using System.IO;
using BallGame.LevelDesign;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class CollaborativeLevelDesignerSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LevelDesigner.unity";
        private const string UiPrefabPath = "Assets/LevelDesigner/DesignerUI.prefab";
        private const string BrowserWebGlDefine = "BALLGAME_BROWSER_WEBGL";
        private static readonly string[] TapBrowserPluginPaths =
        {
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jslib",
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jspre",
            "Packages/com.taptap.minigame/Runtime/Plugins/TapSDK.jslib",
            "Packages/com.taptap.minigame/Runtime/Plugins/minihost-plugin.taptap.runtime.dll",
            "Packages/com.taptap.minigame/Runtime/Plugins/TapExtManagerHandler.dll"
        };
        private static readonly HashSet<string> TapBrowserPluginsRequiringReimport = new HashSet<string>
        {
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jslib",
            "Packages/com.taptap.minigame/Runtime/Plugins/MiniHost.jspre",
            "Packages/com.taptap.minigame/Runtime/Plugins/TapSDK.jslib"
        };

        [InitializeOnLoadMethod]
        private static void QueueCreateMissingAssets()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                CollaborativeLevelAssetRegistry registry = AssetDatabase.LoadAssetAtPath<CollaborativeLevelAssetRegistry>("Assets/Resources/LevelDesigner/CollaborativeLevelAssetRegistry.asset");
                if (registry == null || RegistryNeedsToolDefinitions(registry)) CollaborativeLevelRegistryBuilder.CreateRegistry();
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                    CreateScene();
                EnsureDesignerUiInstance();
            };
        }

        private static bool RegistryNeedsToolDefinitions(CollaborativeLevelAssetRegistry registry)
        {
            foreach (CollaborativeLevelAssetEntry entry in registry.Entries)
                if (entry != null && entry.kind == CollaborativeObjectKind.Tool && entry.assetKey != "obstacle" && entry.toolDefinition == null) return true;
            return false;
        }

        [MenuItem("Ball Game/Level Design/Create Level Designer Scene")]
        public static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("Level designer scene already exists: " + ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.88f, 0.92f, 0.94f, 1f);
            new GameObject("Level Designer", typeof(CollaborativeLevelDesignerController));
            EditorSceneManager.SaveScene(scene, ScenePath, false);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + ScenePath);
        }

        private static void EnsureDesignerUiInstance()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            bool exists = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "DesignerUI") continue;
                exists = true;
                break;
            }
            if (!exists)
            {
                PrefabUtility.InstantiatePrefab(prefab, scene);
                EditorSceneManager.SaveScene(scene, ScenePath, false);
            }
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        [MenuItem("Ball Game/Level Design/Upgrade Designer UI Size Fields")]
        private static void EnsureDesignerUiSizeFields()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null) return;
            if (FindChild(prefab.transform, "Size XInput") != null
                && FindChild(prefab.transform, "Size YInput") != null
                && FindChild(prefab.transform, "Size X Label") != null
                && FindChild(prefab.transform, "Size Y Label") != null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                Transform inspector = FindChild(root.transform, "Inspector");
                TMP_InputField sizeX = FindChild(root.transform, "Scale XInput")?.GetComponent<TMP_InputField>();
                TMP_InputField sizeY = FindChild(root.transform, "Scale YInput")?.GetComponent<TMP_InputField>();
                TMP_InputField rotation = FindChild(root.transform, "RotationInput")?.GetComponent<TMP_InputField>();
                TMP_InputField pathWidth = FindChild(root.transform, "Path widthInput")?.GetComponent<TMP_InputField>();
                Button closed = FindChild(root.transform, "ClosedButton")?.GetComponent<Button>();
                Button collision = FindChild(root.transform, "CollisionButton")?.GetComponent<Button>();
                if (inspector == null || sizeX == null || sizeY == null)
                    throw new System.InvalidOperationException("DesignerUI is missing the expected size input controls.");

                sizeX.name = "Size XInput";
                sizeY.name = "Size YInput";
                SetPosition(sizeX.transform, new Vector2(190f, -495f));
                SetPosition(sizeY.transform, new Vector2(190f, -550f));
                if (rotation != null) SetPosition(rotation.transform, new Vector2(150f, -440f));
                if (pathWidth != null) SetPosition(pathWidth.transform, new Vector2(150f, -610f));
                if (closed != null) SetPosition(closed.transform, new Vector2(80f, -665f));
                if (collision != null) SetPosition(collision.transform, new Vector2(220f, -665f));

                CreateSizeLabel(inspector, "Size X Label", "Size X", new Vector2(55f, -495f), sizeX.textComponent);
                CreateSizeLabel(inspector, "Size Y Label", "Size Y", new Vector2(55f, -550f), sizeY.textComponent);
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateSizeLabel(Transform parent, string name, string value, Vector2 position, TMP_Text source)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(80f, 30f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = source != null ? source.font : null;
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            label.raycastTarget = false;
        }

        private static void SetPosition(Transform target, Vector2 position)
        {
            if (target is RectTransform rect) rect.anchoredPosition = position;
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name == name) return children[i];
            return null;
        }

        [MenuItem("Ball Game/Level Design/Build WebGL Designer")]
        public static void BuildWebGLDesigner()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Build Stopped", "Create the Level Designer scene first.", "OK");
                return;
            }

            string output = EditorUtility.SaveFolderPanel("WebGL Designer Output", "Build", "LevelDesignerWebGL");
            if (string.IsNullOrEmpty(output)) return;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                subtarget = 0,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { BrowserWebGlDefine }
            };
            Dictionary<string, bool> tapPluginCompatibility = new Dictionary<string, bool>();
            BuildReport report;
            try
            {
                DisableTapBrowserPlugins(tapPluginCompatibility);
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                RestoreTapBrowserPlugins(tapPluginCompatibility);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build Failed", "The browser WebGL designer build did not complete successfully. Check the Console for details.", "OK");
                return;
            }

            string contaminatedFile = FindTapBridgeReference(output);
            if (!string.IsNullOrEmpty(contaminatedFile))
            {
                Debug.LogError("Browser WebGL build still contains a TapTap mini-game bridge: " + contaminatedFile);
                EditorUtility.DisplayDialog(
                    "Build Validation Failed",
                    "The output still contains TapTap mini-game bridge code and must not be uploaded to a browser host. Check the Console for details.",
                    "OK");
                return;
            }

            Debug.Log("Created browser-safe Level Designer WebGL build: " + output);
            EditorUtility.DisplayDialog("Build Complete", "Browser WebGL designer created successfully.\n\n" + output, "OK");
        }

        private static void DisableTapBrowserPlugins(Dictionary<string, bool> compatibility)
        {
            for (int i = 0; i < TapBrowserPluginPaths.Length; i++)
            {
                string path = TapBrowserPluginPaths[i];
                PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null)
                    throw new FileNotFoundException("TapTap browser plugin importer was not found.", path);

                bool wasCompatible = importer.GetCompatibleWithPlatform(BuildTarget.WebGL);
                compatibility[path] = wasCompatible;
                if (!wasCompatible) continue;
                importer.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
                if (TapBrowserPluginsRequiringReimport.Contains(path)) importer.SaveAndReimport();
            }
        }

        private static void RestoreTapBrowserPlugins(Dictionary<string, bool> compatibility)
        {
            foreach (KeyValuePair<string, bool> entry in compatibility)
            {
                PluginImporter importer = AssetImporter.GetAtPath(entry.Key) as PluginImporter;
                if (importer == null || importer.GetCompatibleWithPlatform(BuildTarget.WebGL) == entry.Value) continue;
                importer.SetCompatibleWithPlatform(BuildTarget.WebGL, entry.Value);
                if (TapBrowserPluginsRequiringReimport.Contains(entry.Key)) importer.SaveAndReimport();
            }
        }

        private static string FindTapBridgeReference(string output)
        {
            string[] frameworkFiles = Directory.GetFiles(output, "*.framework.js", SearchOption.AllDirectories);
            for (int i = 0; i < frameworkFiles.Length; i++)
            {
                string contents = File.ReadAllText(frameworkFiles[i]);
                if (contents.Contains("TJWASMSDK"))
                    return frameworkFiles[i];
            }

            string[] symbolFiles = Directory.GetFiles(output, "*.symbols.json", SearchOption.AllDirectories);
            for (int i = 0; i < symbolFiles.Length; i++)
            {
                string contents = File.ReadAllText(symbolFiles[i]);
                if (contents.Contains("TapSDKManagerHandler_")) return symbolFiles[i];
            }

            return null;
        }
    }
}
