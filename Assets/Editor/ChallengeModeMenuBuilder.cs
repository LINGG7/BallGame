using BallGame.Core;
using BallGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    public static class ChallengeModeMenuBuilder
    {
        // Recovery revision: rebuild serialized Challenge menu references after damaged prefab cleanup.
        private const string MainMenuPrefabPath = "Assets/Resources/UI/MainMenuUI.prefab";
        private const string LevelSelectPrefabPath = "Assets/Resources/UI/LevelSelectUI.prefab";
        private const string ChallengeLevelSelectPrefabPath = "Assets/Resources/UI/ChallengeLevelSelectUI.prefab";
        private const string ChallengeCatalogPath = "Assets/LevelDefinitions/ChallengeLevelCatalog.asset";
        private const int LevelsPerPage = 9;

        [DidReloadScripts]
        private static void EnsureAfterReload()
        {
            EditorApplication.delayCall += EnsureChallengeMenuAssets;
        }

        [MenuItem("Ball Game/UI/Rebuild Challenge Mode Menu")]
        public static void EnsureChallengeMenuAssets()
        {
            LevelCatalog catalog = EnsureChallengeCatalog();
            GameObject challengeSelectPrefab = EnsureChallengeLevelSelectPrefab();
            if (catalog == null || challengeSelectPrefab == null)
            {
                return;
            }

            EnsureMainMenuPrefab(catalog, challengeSelectPrefab);
            ValidateChallengeCatalog();
        }

        private static LevelCatalog EnsureChallengeCatalog()
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ChallengeCatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, ChallengeCatalogPath);
            return catalog;
        }

        private static GameObject EnsureChallengeLevelSelectPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(LevelSelectPrefabPath);
            if (source == null)
            {
                Debug.LogError("Missing level select prefab: " + LevelSelectPrefabPath);
                return null;
            }

            GameObject root;
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(ChallengeLevelSelectPrefabPath) != null;
            if (exists)
            {
                root = PrefabUtility.LoadPrefabContents(ChallengeLevelSelectPrefabPath);
            }
            else
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            }

            root.name = "ChallengeLevelSelectUI";
            Transform panel = root.transform.Find("Panel");
            if (panel == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                Debug.LogError("Level select prefab is missing Panel.");
                return null;
            }

            TMP_Text panelTitle = panel.Find("PanelTitle")?.GetComponent<TMP_Text>();
            if (panelTitle != null)
            {
                panelTitle.text = "挑战模式选关";
            }

            Transform grid = panel.Find("LevelGrid");
            if (grid != null)
            {
                for (int i = 0; i < LevelsPerPage; i++)
                {
                    Transform slot = grid.Find("LevelSlot_" + (i + 1).ToString("00"));
                    EnsureBestCostLabel(slot);
                }
            }

            EnsureEmptyText(panel, panelTitle);
            PrefabUtility.SaveAsPrefabAsset(root, ChallengeLevelSelectPrefabPath);
            if (exists)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(ChallengeLevelSelectPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ChallengeLevelSelectPrefabPath);
        }

        private static void EnsureBestCostLabel(Transform slot)
        {
            if (slot == null)
            {
                return;
            }

            TMP_Text title = slot.Find("Text")?.GetComponent<TMP_Text>();
            if (title == null)
            {
                return;
            }

            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.05f, 0.38f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TMP_Text bestCost = slot.Find("BestCost")?.GetComponent<TMP_Text>();
            if (bestCost == null)
            {
                bestCost = UnityEngine.Object.Instantiate(title, slot);
                bestCost.name = "BestCost";
                bestCost.text = "历史最低成本：未挑战";
            }
        }

        private static void EnsureEmptyText(Transform panel, TMP_Text panelTitle)
        {
            TMP_Text emptyText = panel.Find("EmptyText")?.GetComponent<TMP_Text>();
            if (emptyText == null && panelTitle != null)
            {
                emptyText = UnityEngine.Object.Instantiate(panelTitle, panel);
                emptyText.name = "EmptyText";
            }

            if (emptyText == null)
            {
                return;
            }

            emptyText.text = "暂无挑战关卡";
            emptyText.fontSize = 32f;
            emptyText.fontSizeMax = 32f;
            emptyText.fontSizeMin = 18f;
            RectTransform rect = emptyText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(500f, 100f);
        }

        private static void EnsureMainMenuPrefab(LevelCatalog challengeCatalog, GameObject challengeSelectPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath) == null)
            {
                Debug.LogError("Missing main menu prefab: " + MainMenuPrefabPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            MainMenuController controller = root.GetComponent<MainMenuController>();
            Button startButton = root.transform.Find("StartButton")?.GetComponent<Button>();
            Button challengeButton = root.transform.Find("ChallengeButton")?.GetComponent<Button>();
            if (challengeButton == null && startButton != null)
            {
                challengeButton = UnityEngine.Object.Instantiate(startButton, root.transform);
                challengeButton.name = "ChallengeButton";
                challengeButton.GetComponent<RectTransform>().anchoredPosition = startButton.GetComponent<RectTransform>().anchoredPosition + new Vector2(0f, -110f);
            }

            TMP_Text challengeButtonText = challengeButton != null ? challengeButton.transform.Find("Text")?.GetComponent<TMP_Text>() : null;
            if (challengeButtonText != null)
            {
                challengeButtonText.text = "挑战模式";
            }

            Transform challengeRoot = root.transform.Find("ChallengeLevelSelectUI");
            if (challengeRoot == null)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(challengeSelectPrefab, root.transform);
                created.name = "ChallengeLevelSelectUI";
                challengeRoot = created.transform;
            }

            RectTransform challengeRect = challengeRoot as RectTransform;
            if (challengeRect != null)
            {
                challengeRect.anchorMin = Vector2.zero;
                challengeRect.anchorMax = Vector2.one;
                challengeRect.offsetMin = Vector2.zero;
                challengeRect.offsetMax = Vector2.zero;
            }

            challengeRoot.gameObject.SetActive(false);
            AssignControllerReferences(controller, challengeCatalog, challengeButton, challengeRoot.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignControllerReferences(MainMenuController controller, LevelCatalog catalog, Button challengeButton, GameObject challengeRoot)
        {
            if (controller == null || challengeRoot == null)
            {
                return;
            }

            Transform panel = challengeRoot.transform.Find("Panel");
            Transform grid = panel != null ? panel.Find("LevelGrid") : null;
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("challengeLevelCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("challengeButton").objectReferenceValue = challengeButton;
            serialized.FindProperty("challengeLevelSelectRoot").objectReferenceValue = challengeRoot;
            serialized.FindProperty("challengeLevelSelectPanel").objectReferenceValue = panel != null ? panel.GetComponent<RectTransform>() : null;
            serialized.FindProperty("closeChallengeLevelSelectButton").objectReferenceValue = panel?.Find("CloseButton")?.GetComponent<Button>();
            serialized.FindProperty("challengePreviousPageButton").objectReferenceValue = panel?.Find("PreviousPageButton")?.GetComponent<Button>();
            serialized.FindProperty("challengeNextPageButton").objectReferenceValue = panel?.Find("NextPageButton")?.GetComponent<Button>();
            serialized.FindProperty("challengePageText").objectReferenceValue = panel?.Find("PageText")?.GetComponent<TMP_Text>();
            serialized.FindProperty("challengeEmptyText").objectReferenceValue = panel?.Find("EmptyText")?.GetComponent<TMP_Text>();

            SerializedProperty buttons = serialized.FindProperty("challengeLevelButtons");
            SerializedProperty titles = serialized.FindProperty("challengeLevelLabels");
            SerializedProperty costs = serialized.FindProperty("challengeBestCostLabels");
            buttons.arraySize = LevelsPerPage;
            titles.arraySize = LevelsPerPage;
            costs.arraySize = LevelsPerPage;
            for (int i = 0; i < LevelsPerPage; i++)
            {
                Transform slot = grid?.Find("LevelSlot_" + (i + 1).ToString("00"));
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = slot != null ? slot.GetComponent<Button>() : null;
                titles.GetArrayElementAtIndex(i).objectReferenceValue = slot?.Find("Text")?.GetComponent<TMP_Text>();
                costs.GetArrayElementAtIndex(i).objectReferenceValue = slot?.Find("BestCost")?.GetComponent<TMP_Text>();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Ball Game/Validation/Validate Challenge Level Catalog")]
        public static void ValidateChallengeCatalog()
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ChallengeCatalogPath);
            if (catalog == null)
            {
                Debug.LogError("Missing challenge level catalog.");
                return;
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                LevelCatalog.LevelEntry entry = catalog.Levels[i];
                int expectedNumber = i + 1;
                string expectedSceneName = "Challenge_" + expectedNumber.ToString("00");
                if (entry.LevelNumber != expectedNumber || entry.SceneName != expectedSceneName)
                {
                    Debug.LogError("Challenge catalog entries must be sequential and use Challenge_XX scene names: " + entry.DisplayTitle, catalog);
                }

                if (entry.LevelDefinition == null || entry.LevelDefinition.Mode != LevelMode.LowestCost || entry.LevelDefinition.ToolCatalog == null)
                {
                    Debug.LogError("Challenge level must use a LowestCost LevelDefinition with a ToolCatalog: " + entry.DisplayTitle, catalog);
                }
            }
        }
    }
}
