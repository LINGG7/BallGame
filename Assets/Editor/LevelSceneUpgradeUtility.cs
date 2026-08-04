using System;
using System.IO;
using BallGame.Core;
using BallGame.Placement;
using BallGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallGame.Editor
{
	public static class LevelSceneUpgradeUtility
	{
		private const string LevelScenePath = "Assets/Scenes/Levels";

		[MenuItem("Ball Game/Upgrade Existing Level Scenes")]
		public static void UpgradeExistingLevelScenes()
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				Scene activeScene = SceneManager.GetActiveScene();
				string path = activeScene.path;
				string[] array = AssetDatabase.FindAssets("t:Scene", new string[1] { "Assets/Scenes/Levels" });
				string[] array2 = array;
				foreach (string text in array2)
				{
					string text2 = AssetDatabase.GUIDToAssetPath(text);
					Scene val = EditorSceneManager.OpenScene(text2, (OpenSceneMode)0);
					UpgradeOpenScene(text2);
					EditorSceneManager.MarkSceneDirty(val);
					EditorSceneManager.SaveScene(val);
				}
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				if (!string.IsNullOrEmpty(path))
				{
					EditorSceneManager.OpenScene(path, (OpenSceneMode)0);
				}
				Debug.Log((object)$"Upgraded {array.Length} existing level scene(s) without rebuilding map content.");
			}
		}

		private static void UpgradeOpenScene(string scenePath)
		{
			Camera orCreateMainCamera = GetOrCreateMainCamera();
			GameManager orCreateGameManager = GetOrCreateGameManager();
			PlacementManager orCreatePlacementManager = GetOrCreatePlacementManager(((Component)orCreateGameManager).gameObject);
			MapBounds2D orCreateMapBounds = GetOrCreateMapBounds(scenePath);
			Transform transform = GetOrCreateRoot("Placed Tools").transform;
			BallController ball = UnityEngine.Object.FindObjectOfType<BallController>();
			GoalTrigger goal = UnityEngine.Object.FindObjectOfType<GoalTrigger>();
			GameHudRefs hud = UnityEngine.Object.FindObjectOfType<GameHudRefs>();
			CameraController2D val = ((Component)orCreateMainCamera).GetComponent<CameraController2D>();
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = ((Component)orCreateMainCamera).gameObject.AddComponent<CameraController2D>();
			}
			UpgradeWorldBackground(orCreateMainCamera);
			UpgradePlacement(orCreatePlacementManager, orCreateMainCamera, transform);
			UpgradeGameManager(orCreateGameManager, ball, goal, val, orCreatePlacementManager, hud);
			UpgradeCameraController(val, orCreateGameManager, ball, orCreatePlacementManager, orCreateMapBounds);
			UpgradeHud(hud, orCreateGameManager, orCreateMainCamera, ball, goal, orCreatePlacementManager);
		}

		private static Camera GetOrCreateMainCamera()
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected O, but got Unknown
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			Camera val = (((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null) ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>());
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				((Component)val).tag = "MainCamera";
				return val;
			}
			GameObject val2 = new GameObject("Main Camera");
			val = val2.AddComponent<Camera>();
			((Component)val).tag = "MainCamera";
			val.orthographic = true;
			val.orthographicSize = 5f;
			((Component)val).transform.position = new Vector3(0f, 0f, -10f);
			return val;
		}

		private static GameManager GetOrCreateGameManager()
		{
			GameManager val = UnityEngine.Object.FindObjectOfType<GameManager>();
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			return GetOrCreateRoot("Game Systems").AddComponent<GameManager>();
		}

		private static PlacementManager GetOrCreatePlacementManager(GameObject systems)
		{
			PlacementManager val = UnityEngine.Object.FindObjectOfType<PlacementManager>();
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			return systems.AddComponent<PlacementManager>();
		}

		private static MapBounds2D GetOrCreateMapBounds(string scenePath)
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			MapBounds2D val = UnityEngine.Object.FindObjectOfType<MapBounds2D>();
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			GameObject orCreateRoot = GetOrCreateRoot("Map Bounds");
			val = orCreateRoot.AddComponent<MapBounds2D>();
			Vector2 val2 = ((scenePath.Contains("Level_03") || scenePath.Contains("Level_05")) ? new Vector2(20f, 10.5f) : new Vector2(20f, 9f));
			val.SetBounds(new Vector2(0f, -0.4f), val2);
			return val;
		}

		private static GameObject GetOrCreateRoot(string name)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = GameObject.Find(name);
			return (GameObject)(((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ((object)val) : ((object)new GameObject(name)));
		}

		private static void UpgradeWorldBackground(Camera camera)
		{
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Expected O, but got Unknown
			GameObject val = GameObject.Find("WorldBackground");
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				InfiniteTiledBackground2D val2 = val.GetComponent<InfiniteTiledBackground2D>();
				if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
				{
					val2 = val.AddComponent<InfiniteTiledBackground2D>();
				}
				((Component)val2).transform.position = new Vector3(0f, 0f, 4f);
				((Component)val2).transform.localScale = Vector3.one;
				SerializedObject val3 = new SerializedObject((UnityEngine.Object)(object)val2);
				val3.FindProperty("targetCamera").objectReferenceValue = (UnityEngine.Object)(object)camera;
				val3.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		private static void UpgradePlacement(PlacementManager placement, Camera camera, Transform placedRoot)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)placement);
			val.FindProperty("worldCamera").objectReferenceValue = (UnityEngine.Object)(object)camera;
			val.FindProperty("placedRoot").objectReferenceValue = (UnityEngine.Object)(object)placedRoot;
			val.FindProperty("blockedPlacementMask").intValue = -1;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void UpgradeGameManager(GameManager manager, BallController ball, GoalTrigger goal, CameraController2D cameraController, PlacementManager placement, GameHudRefs hud)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)manager);
			val.FindProperty("ball").objectReferenceValue = (UnityEngine.Object)(object)ball;
			val.FindProperty("goal").objectReferenceValue = (UnityEngine.Object)(object)goal;
			val.FindProperty("cameraController").objectReferenceValue = (UnityEngine.Object)(object)cameraController;
			val.FindProperty("placementManager").objectReferenceValue = (UnityEngine.Object)(object)placement;
			if ((UnityEngine.Object)(object)hud != (UnityEngine.Object)null)
			{
				val.FindProperty("inventoryPanel").objectReferenceValue = (UnityEngine.Object)(object)hud.InventoryPanel;
				val.FindProperty("titleText").objectReferenceValue = (UnityEngine.Object)(object)hud.TitleText;
				val.FindProperty("statusText").objectReferenceValue = (UnityEngine.Object)(object)hud.StatusText;
				val.FindProperty("runButton").objectReferenceValue = (UnityEngine.Object)(object)hud.RunButton;
				val.FindProperty("resetButton").objectReferenceValue = (UnityEngine.Object)(object)hud.ResetButton;
				val.FindProperty("editButton").objectReferenceValue = (UnityEngine.Object)(object)hud.EditButton;
				val.FindProperty("nextButton").objectReferenceValue = (UnityEngine.Object)(object)hud.NextButton;
				val.FindProperty("returnButton").objectReferenceValue = (UnityEngine.Object)(object)hud.ReturnButton;
			}
			val.ApplyModifiedPropertiesWithoutUndo();
			if ((UnityEngine.Object)(object)goal != (UnityEngine.Object)null)
			{
				goal.Initialize(manager);
			}
		}

		private static void UpgradeCameraController(CameraController2D cameraController, GameManager manager, BallController ball, PlacementManager placement, MapBounds2D mapBounds)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)cameraController);
			val.FindProperty("gameManager").objectReferenceValue = (UnityEngine.Object)(object)manager;
			val.FindProperty("targetBall").objectReferenceValue = (UnityEngine.Object)(object)ball;
			val.FindProperty("placementManager").objectReferenceValue = (UnityEngine.Object)(object)placement;
			val.FindProperty("mapBounds").objectReferenceValue = (UnityEngine.Object)(object)mapBounds;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void UpgradeHud(GameHudRefs hud, GameManager manager, Camera camera, BallController ball, GoalTrigger goal, PlacementManager placement)
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Expected O, but got Unknown
			if (!((UnityEngine.Object)(object)hud == (UnityEngine.Object)null))
			{
				EnsureHudIndicators(hud);
				if ((UnityEngine.Object)(object)hud.PlacementControls != (UnityEngine.Object)null)
				{
					SerializedObject val = new SerializedObject((UnityEngine.Object)(object)hud.PlacementControls);
					val.FindProperty("placementManager").objectReferenceValue = (UnityEngine.Object)(object)placement;
					val.ApplyModifiedPropertiesWithoutUndo();
				}
				Canvas component = ((Component)hud).GetComponent<Canvas>();
				AssignIndicator(hud.BallIndicator, manager, camera, ((UnityEngine.Object)(object)ball != (UnityEngine.Object)null) ? ((Component)ball).transform : null, component);
				AssignIndicator(hud.GoalIndicator, manager, camera, ((UnityEngine.Object)(object)goal != (UnityEngine.Object)null) ? ((Component)goal).transform : null, component);
			}
		}

		private static void EnsureHudIndicators(GameHudRefs hud)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Expected O, but got Unknown
			Sprite orCreateArrowSprite = GetOrCreateArrowSprite();
			WorldTargetIndicator orCreateIndicator = GetOrCreateIndicator(((Component)hud).transform, "BallArrow", orCreateArrowSprite, new Color(1f, 0.86f, 0.18f, 0.95f));
			WorldTargetIndicator orCreateIndicator2 = GetOrCreateIndicator(((Component)hud).transform, "GoalArrow", orCreateArrowSprite, new Color(0.25f, 1f, 0.35f, 0.95f));
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)hud);
			val.FindProperty("ballIndicator").objectReferenceValue = (UnityEngine.Object)(object)orCreateIndicator;
			val.FindProperty("goalIndicator").objectReferenceValue = (UnityEngine.Object)(object)orCreateIndicator2;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static WorldTargetIndicator GetOrCreateIndicator(Transform parent, string name, Sprite arrowSprite, Color color)
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			Transform val = parent.Find(name);
			GameObject val2 = (GameObject)(((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ((object)((Component)val).gameObject) : ((object)new GameObject(name, new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(WorldTargetIndicator)
			})));
			val2.transform.SetParent(parent, false);
			RectTransform component = val2.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.5f, 0.5f);
			component.anchorMax = new Vector2(0.5f, 0.5f);
			component.pivot = new Vector2(0.5f, 0.5f);
			component.sizeDelta = new Vector2(42f, 42f);
			component.anchoredPosition = Vector2.zero;
			Image component2 = val2.GetComponent<Image>();
			component2.sprite = arrowSprite;
			((Graphic)component2).color = color;
			((Graphic)component2).raycastTarget = true;
			((Behaviour)component2).enabled = false;
			return val2.GetComponent<WorldTargetIndicator>();
		}

		private static Sprite GetOrCreateArrowSprite()
		{
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Expected O, but got Unknown
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Expected O, but got Unknown
			//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			if (!AssetDatabase.IsValidFolder("Assets/Art"))
			{
				AssetDatabase.CreateFolder("Assets", "Art");
			}
			if (!AssetDatabase.IsValidFolder("Assets/Art/Generated"))
			{
				AssetDatabase.CreateFolder("Assets/Art", "Generated");
			}
			if (!File.Exists("Assets/Art/Generated/TargetArrow.png"))
			{
				Texture2D val = new Texture2D(64, 64, (TextureFormat)4, false);
				Color[] array = (Color[])(object)new Color[4096];
				for (int i = 0; i < 64; i++)
				{
					for (int j = 0; j < 64; j++)
					{
						float num = Mathf.Lerp(5f, 24f, (float)i / 63f);
						bool flag = i >= 8 && i <= 56 && Mathf.Abs((float)j - 31.5f) <= num;
						array[i * 64 + j] = (flag ? Color.white : Color.clear);
					}
				}
				val.SetPixels(array);
				val.Apply();
				File.WriteAllBytes("Assets/Art/Generated/TargetArrow.png", ImageConversion.EncodeToPNG(val));
			}
			AssetDatabase.ImportAsset("Assets/Art/Generated/TargetArrow.png");
			TextureImporter val2 = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Generated/TargetArrow.png");
			val2.textureType = (TextureImporterType)8;
			val2.spritePixelsPerUnit = 64f;
			((AssetImporter)val2).SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/TargetArrow.png");
		}

		private static void AssignIndicator(WorldTargetIndicator indicator, GameManager manager, Camera camera, Transform target, Canvas canvas)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Expected O, but got Unknown
			if (!((UnityEngine.Object)(object)indicator == (UnityEngine.Object)null))
			{
				SerializedObject val = new SerializedObject((UnityEngine.Object)(object)indicator);
				val.FindProperty("gameManager").objectReferenceValue = (UnityEngine.Object)(object)manager;
				val.FindProperty("worldCamera").objectReferenceValue = (UnityEngine.Object)(object)camera;
				val.FindProperty("target").objectReferenceValue = (UnityEngine.Object)(object)target;
				val.FindProperty("canvas").objectReferenceValue = (UnityEngine.Object)(object)canvas;
				val.ApplyModifiedPropertiesWithoutUndo();
			}
		}
	}
}
