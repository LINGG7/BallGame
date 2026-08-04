using System;
using System.Collections.Generic;
using System.IO;
using BallGame.Core;
using BallGame.Placement;
using BallGame.Tools;
using BallGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using EmissionModule = UnityEngine.ParticleSystem.EmissionModule;
using ShapeModule = UnityEngine.ParticleSystem.ShapeModule;
using SizeOverLifetimeModule = UnityEngine.ParticleSystem.SizeOverLifetimeModule;
using MinMaxCurve = UnityEngine.ParticleSystem.MinMaxCurve;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;
using Burst = UnityEngine.ParticleSystem.Burst;

namespace BallGame.Editor
{
	public static class BallGamePrototypeBuilder
	{
		private enum AnchorPreset
		{
			TopStretch,
			BottomStretch,
			LeftStretch
		}

		private readonly struct ObstacleSpec
		{
			public readonly Vector2 position;

			public readonly Vector2 size;

			public readonly float rotation;

			public ObstacleSpec(Vector2 position, Vector2 size, float rotation)
			{
				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
				//IL_0003: Unknown result type (might be due to invalid IL or missing references)
				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				this.position = position;
				this.size = size;
				this.rotation = rotation;
			}
		}

		private const string LevelScenePath = "Assets/Scenes/Levels";

		private const string ArtPath = "Assets/Art/Generated";

		private const string PrefabPath = "Assets/Resources/Prefabs";

		private const string LevelPath = "Assets/Levels";

		private const string ToolPath = "Assets/Tools";

		private const string UiPath = "Assets/Resources/UI";

		private const string ParticleMaterialPath = "Assets/Materials/FireworkParticle.mat";

		private const string WaterBodyMaterialPath = "Assets/Materials/WaterBodyAnimated.mat";

		private const string WaterSurfaceMaterialPath = "Assets/Materials/WaterSurfaceAnimated.mat";

		private const string AntiGravityMaterialPath = "Assets/Materials/AntiGravityFlow.mat";

		private const string BubbleMaterialPath = "Assets/Materials/BubbleParticle.mat";

		[MenuItem("Ball Game/Create Missing Prototype Assets")]
		public static void CreateMissingPrototypeAssets()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_022e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0260: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
			EnsureFolders();
			Sprite val = CreateSprite("Square", Color.white);
			Sprite val2 = CreateSprite("Circle", new Color(1f, 1f, 1f, 1f));
			Sprite arrowSprite = CreateArrowSprite();
			GameObject orCreateBoxPrefab = GetOrCreateBoxPrefab("Plank", val, new Color(0.62f, 0.38f, 0.16f), new Vector2(2.2f, 0.22f), null, trigger: false);
			GameObject orCreateBoxPrefab2 = GetOrCreateBoxPrefab("Spring", val, new Color(0.1f, 0.9f, 0.35f), new Vector2(1.2f, 0.25f), typeof(SpringPad), trigger: false);
			GameObject orCreateBoxPrefab3 = GetOrCreateBoxPrefab("Fan", val, new Color(0.25f, 0.7f, 1f), new Vector2(0.8f, 0.8f), typeof(FanZone), trigger: false);
			GameObject orCreateBoxPrefab4 = GetOrCreateBoxPrefab("Conveyor", val, new Color(0.1f, 0.85f, 0.85f), new Vector2(1.8f, 0.25f), typeof(ConveyorBelt), trigger: false);
			GameObject orCreateBoxPrefab5 = GetOrCreateBoxPrefab("Magnet", val2, new Color(0.95f, 0.15f, 0.75f), new Vector2(0.8f, 0.8f), typeof(MagnetTool), trigger: false);
			GameObject orCreateBoxPrefab6 = GetOrCreateBoxPrefab("Portal", val2, new Color(0.2f, 0.25f, 1f), new Vector2(0.9f, 0.9f), typeof(PortalTool), trigger: true);
			GameObject orCreateWaterZonePrefab = GetOrCreateWaterZonePrefab(val);
			GameObject orCreateInventoryButtonPrefab = GetOrCreateInventoryButtonPrefab(val);
			ToolInventoryButton component = orCreateInventoryButtonPrefab.GetComponent<ToolInventoryButton>();
			ToolDefinition orCreateTool = GetOrCreateTool("Plank", orCreateBoxPrefab, component, val, new Color(0.62f, 0.38f, 0.16f), 3, canRotate: true, 15f, 1.1f);
			ToolDefinition orCreateTool2 = GetOrCreateTool("Spring", orCreateBoxPrefab2, component, val, new Color(0.1f, 0.9f, 0.35f), 1, canRotate: true, 15f, 0.75f);
			ToolDefinition orCreateTool3 = GetOrCreateTool("Fan", orCreateBoxPrefab3, component, val, new Color(0.25f, 0.7f, 1f), 1, canRotate: true, 15f, 0.7f);
			ToolDefinition orCreateTool4 = GetOrCreateTool("Conveyor", orCreateBoxPrefab4, component, val, new Color(0.1f, 0.85f, 0.85f), 1, canRotate: true, 15f, 0.9f);
			ToolDefinition orCreateTool5 = GetOrCreateTool("Magnet", orCreateBoxPrefab5, component, val2, new Color(0.95f, 0.15f, 0.75f), 1, canRotate: false, 15f, 0.75f);
			ToolDefinition orCreateTool6 = GetOrCreateTool("Portal", orCreateBoxPrefab6, component, val2, new Color(0.2f, 0.25f, 1f), 2, canRotate: true, 15f, 0.75f);
			ToolDefinition orCreateTool7 = GetOrCreateTool("WaterZone", orCreateWaterZonePrefab, component, val, new Color(0.12f, 0.62f, 1f), 1, canRotate: true, 15f, 1.2f, "水面区域");
			EnsurePlaceableDefinition("Assets/Resources/Prefabs/WaterZone.prefab", orCreateTool7);
			GetOrCreateBallPrefab(val2);
			GetOrCreateGoalPrefab(val2);
			GetOrCreateGameHudPrefab(component, arrowSprite);
			GetOrCreateWorldBackgroundPrefab(val);
			GetOrCreateObstaclePrefab(val);
			EnsureAntiGravityPrefabConfigured();
			GetOrCreateLevels(orCreateTool, orCreateTool2, orCreateTool3, orCreateTool4, orCreateTool5, orCreateTool6);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log((object)"Missing Ball Game prototype assets created.");
		}

		[MenuItem("Ball Game/Rebuild Demo Level Scenes (Overwrite)")]
		public static void RebuildLevelScenes()
		{
			EnsureFolders();
			CreateMissingPrototypeAssets();
			GameObject ballPrefab = LoadRequiredAsset<GameObject>("Assets/Resources/Prefabs/Ball.prefab");
			GameObject goalPrefab = LoadRequiredAsset<GameObject>("Assets/Resources/Prefabs/Goal.prefab");
			GameObject gameHudPrefab = LoadRequiredAsset<GameObject>("Assets/Resources/UI/GameHud.prefab");
			GameObject backgroundPrefab = LoadRequiredAsset<GameObject>("Assets/Resources/Prefabs/WorldBackground.prefab");
			GameObject obstaclePrefab = LoadRequiredAsset<GameObject>("Assets/Resources/Prefabs/Obstacle.prefab");
			LevelDefinition[] array = LoadLevels();
			if (array.Length == 0)
			{
				throw new InvalidOperationException("No LevelDefinition assets found under Assets/Levels. Run Ball Game > Create Missing Prototype Assets.");
			}
			string[] array2 = new string[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = string.Format("{0}/Level_{1:00}.unity", "Assets/Scenes/Levels", i + 1);
			}
			for (int j = 0; j < array.Length; j++)
			{
				string nextSceneName = ((j < array.Length - 1) ? $"Level_{j + 2:00}" : string.Empty);
				BuildLevelScene(ballPrefab, goalPrefab, gameHudPrefab, backgroundPrefab, obstaclePrefab, array[j], array2[j], nextSceneName, j);
			}
			EditorBuildSettings.scenes = BuildEditorScenes(array2);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log((object)"Ball Game multi-level prototype rebuilt.");
		}

		private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
		{
			T val = AssetDatabase.LoadAssetAtPath<T>(path);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				throw new InvalidOperationException("Required asset missing: " + path + ". Run Ball Game > Create Missing Prototype Assets.");
			}
			return val;
		}

		private static LevelDefinition[] LoadLevels()
		{
			string[] array = AssetDatabase.FindAssets("t:LevelDefinition", new string[1] { "Assets/Levels" });
			List<LevelDefinition> list = new List<LevelDefinition>();
			string[] array2 = array;
			foreach (string text in array2)
			{
				string text2 = AssetDatabase.GUIDToAssetPath(text);
				LevelDefinition val = AssetDatabase.LoadAssetAtPath<LevelDefinition>(text2);
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					list.Add(val);
				}
			}
			list.Sort((LevelDefinition a, LevelDefinition b) => string.CompareOrdinal(a.LevelTitle, b.LevelTitle));
			return list.ToArray();
		}

		private static void EnsureFolders()
		{
			string[] array = new string[7] { "Assets/Art", "Assets/Art/Generated", "Assets/Resources/Prefabs", "Assets/Levels", "Assets/Tools", "Assets/Resources/UI", "Assets/Scenes/Levels" };
			foreach (string text in array)
			{
				if (!AssetDatabase.IsValidFolder(text))
				{
					string text2 = Path.GetDirectoryName(text).Replace("\\", "/");
					string fileName = Path.GetFileName(text);
					AssetDatabase.CreateFolder(text2, fileName);
				}
			}
		}

		private static Sprite CreateSprite(string name, Color color)
		{
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Expected O, but got Unknown
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Art/Generated/" + name + ".png";
			if (!File.Exists(text))
			{
				Texture2D val = new Texture2D(32, 32, (TextureFormat)4, false);
				Color[] array = (Color[])(object)new Color[1024];
				Vector2 val2 = default(Vector2);
				val2 = new Vector2(15.5f, 15.5f);
				for (int i = 0; i < 32; i++)
				{
					for (int j = 0; j < 32; j++)
					{
						bool flag = !(name == "Circle") || Vector2.Distance(new Vector2((float)j, (float)i), val2) <= 15f;
						array[i * 32 + j] = (flag ? color : Color.clear);
					}
				}
				val.SetPixels(array);
				val.Apply();
				File.WriteAllBytes(text, ImageConversion.EncodeToPNG(val));
			}
			AssetDatabase.ImportAsset(text);
			TextureImporter val3 = (TextureImporter)AssetImporter.GetAtPath(text);
			val3.textureType = (TextureImporterType)8;
			val3.spritePixelsPerUnit = 32f;
			((AssetImporter)val3).SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Sprite>(text);
		}

		private static Sprite CreateArrowSprite()
		{
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Expected O, but got Unknown
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Expected O, but got Unknown
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Art/Generated/TargetArrow.png";
			if (!File.Exists(text))
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
				File.WriteAllBytes(text, ImageConversion.EncodeToPNG(val));
			}
			AssetDatabase.ImportAsset(text);
			TextureImporter val2 = (TextureImporter)AssetImporter.GetAtPath(text);
			val2.textureType = (TextureImporterType)8;
			val2.spritePixelsPerUnit = 64f;
			((AssetImporter)val2).SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Sprite>(text);
		}

		private static GameObject GetOrCreateBoxPrefab(string name, Sprite sprite, Color color, Vector2 size, Type behaviorType, bool trigger)
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Expected O, but got Unknown
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Expected O, but got Unknown
			string text = "Assets/Resources/Prefabs/" + name + ".prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				EnsureInventoryButtonModeLabels(text);
				return val;
			}
			GameObject val2 = new GameObject(name);
			SpriteRenderer val3 = val2.AddComponent<SpriteRenderer>();
			val3.sprite = sprite;
			val3.color = color;
			((Renderer)val3).sortingOrder = 5;
			val2.transform.localScale = new Vector3(size.x, size.y, 1f);
			Collider2D val4 = (Collider2D)((name == "Magnet" || name == "Portal") ? ((object)val2.AddComponent<CircleCollider2D>()) : ((object)val2.AddComponent<BoxCollider2D>()));
			val4.isTrigger = trigger;
			PlaceableTool val5 = val2.AddComponent<PlaceableTool>();
			SerializedObject val6 = new SerializedObject((UnityEngine.Object)(object)val5);
			val6.FindProperty("renderers").arraySize = 1;
			val6.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = (UnityEngine.Object)(object)val3;
			val6.ApplyModifiedPropertiesWithoutUndo();
			if (behaviorType != null)
			{
				val2.AddComponent(behaviorType);
				if ((behaviorType == typeof(FanZone) || behaviorType == typeof(SpringPad)) && (UnityEngine.Object)(object)val2.GetComponent<ForceDirectionIndicator>() == (UnityEngine.Object)null)
				{
					val2.AddComponent<ForceDirectionIndicator>();
				}
			}
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static GameObject GetOrCreateObstaclePrefab(Sprite square)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Resources/Prefabs/Obstacle.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				EnsureObstaclePrefabConfigured(text);
				return val;
			}
			GameObject val2 = new GameObject("Obstacle");
			SpriteRenderer val3 = val2.AddComponent<SpriteRenderer>();
			val3.sprite = square;
			val3.color = new Color(0.33f, 0.34f, 0.36f);
			((Renderer)val3).sortingOrder = 0;
			val3.drawMode = (SpriteDrawMode)1;
			val2.AddComponent<BoxCollider2D>();
			val2.AddComponent<ObstacleSize2D>();
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static GameObject GetOrCreateWaterZonePrefab(Sprite square)
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Expected O, but got Unknown
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Expected O, but got Unknown
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Expected O, but got Unknown
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			//IL_0189: Expected O, but got Unknown
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_01db: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0209: Unknown result type (might be due to invalid IL or missing references)
			//IL_022a: Unknown result type (might be due to invalid IL or missing references)
			//IL_022f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0241: Unknown result type (might be due to invalid IL or missing references)
			//IL_0257: Unknown result type (might be due to invalid IL or missing references)
			//IL_025c: Unknown result type (might be due to invalid IL or missing references)
			//IL_026e: Unknown result type (might be due to invalid IL or missing references)
			//IL_028a: Unknown result type (might be due to invalid IL or missing references)
			//IL_028f: Unknown result type (might be due to invalid IL or missing references)
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_032a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0331: Expected O, but got Unknown
			//IL_0387: Unknown result type (might be due to invalid IL or missing references)
			//IL_038e: Expected O, but got Unknown
			string text = "Assets/Resources/Prefabs/WaterZone.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				EnsureWaterZoneMaterials(text);
				return val;
			}
			Material sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WaterBodyAnimated.mat");
			Material sharedMaterial2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WaterSurfaceAnimated.mat");
			GameObject val2 = new GameObject("WaterZone");
			BoxCollider2D val3 = val2.AddComponent<BoxCollider2D>();
			((Collider2D)val3).isTrigger = true;
			val3.size = new Vector2(3f, 2f);
			GameObject val4 = new GameObject("WaterBody");
			val4.transform.SetParent(val2.transform, false);
			val4.transform.localScale = new Vector3(3f, 2f, 1f);
			SpriteRenderer val5 = val4.AddComponent<SpriteRenderer>();
			val5.sprite = square;
			val5.color = Color.white;
			((Renderer)val5).sharedMaterial = sharedMaterial;
			((Renderer)val5).sortingOrder = 3;
			GameObject val6 = new GameObject("SurfaceLine");
			val6.transform.SetParent(val2.transform, false);
			val6.transform.localPosition = new Vector3(0f, 1f, -0.01f);
			val6.transform.localScale = new Vector3(3f, 0.18f, 1f);
			SpriteRenderer val7 = val6.AddComponent<SpriteRenderer>();
			val7.sprite = square;
			val7.color = Color.white;
			((Renderer)val7).sharedMaterial = sharedMaterial2;
			((Renderer)val7).sortingOrder = 4;
			GameObject val8 = new GameObject("SplashEffect");
			val8.transform.SetParent(val2.transform, false);
			ParticleSystem val9 = val8.AddComponent<ParticleSystem>();
			MainModule main = val9.main;
			main.playOnAwake = false;
			main.loop = false;
			main.duration = 0.35f;
			main.startLifetime = new MinMaxCurve(0.25f, 0.55f);
			main.startSpeed = new MinMaxCurve(1.4f, 3.2f);
			main.startSize = new MinMaxCurve(0.05f, 0.13f);
			main.startColor = new MinMaxGradient(new Color(0.55f, 0.88f, 1f, 0.9f));
			main.gravityModifier = 0.55f;
			main.simulationSpace = (ParticleSystemSimulationSpace)1;
			EmissionModule emission = val9.emission;
			emission.enabled = true;
			emission.rateOverTime = 0f;
			emission.SetBursts((Burst[])(object)new Burst[1]
			{
				new Burst(0f, (short)14)
			});
			ShapeModule shape = val9.shape;
			shape.shapeType = (ParticleSystemShapeType)4;
			shape.angle = 28f;
			shape.radius = 0.08f;
			val8.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
			ParticleSystemRenderer component = val8.GetComponent<ParticleSystemRenderer>();
			((Renderer)component).sortingOrder = 6;
			Material val10 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FireworkParticle.mat");
			if ((UnityEngine.Object)(object)val10 != (UnityEngine.Object)null)
			{
				((Renderer)component).sharedMaterial = val10;
			}
			PlaceableTool val11 = val2.AddComponent<PlaceableTool>();
			SerializedObject val12 = new SerializedObject((UnityEngine.Object)(object)val11);
			SerializedProperty val13 = val12.FindProperty("renderers");
			val13.arraySize = 2;
			val13.GetArrayElementAtIndex(0).objectReferenceValue = (UnityEngine.Object)(object)val5;
			val13.GetArrayElementAtIndex(1).objectReferenceValue = (UnityEngine.Object)(object)val7;
			val12.ApplyModifiedPropertiesWithoutUndo();
			WaterZone val14 = val2.AddComponent<WaterZone>();
			EnsureWaterBubbleEffects(val2, out var riseEffect, out var popEffect);
			SerializedObject val15 = new SerializedObject((UnityEngine.Object)(object)val14);
			val15.FindProperty("waterRenderer").objectReferenceValue = (UnityEngine.Object)(object)val5;
			val15.FindProperty("surfaceLine").objectReferenceValue = (UnityEngine.Object)(object)val6.transform;
			val15.FindProperty("splashEffect").objectReferenceValue = (UnityEngine.Object)(object)val9;
			val15.FindProperty("bubbleRiseEffect").objectReferenceValue = (UnityEngine.Object)(object)riseEffect;
			val15.FindProperty("bubblePopEffect").objectReferenceValue = (UnityEngine.Object)(object)popEffect;
			val15.ApplyModifiedPropertiesWithoutUndo();
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static void EnsureWaterZoneMaterials(string prefabPath)
		{
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Expected O, but got Unknown
			GameObject val = PrefabUtility.LoadPrefabContents(prefabPath);
			Transform val2 = val.transform.Find("WaterBody");
			Transform val3 = val.transform.Find("SurfaceLine");
			Material val4 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WaterBodyAnimated.mat");
			Material val5 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WaterSurfaceAnimated.mat");
			SpriteRenderer val6 = default(SpriteRenderer);
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null && ((Component)val2).TryGetComponent<SpriteRenderer>(out val6))
			{
				val6.color = Color.white;
				if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
				{
					((Renderer)val6).sharedMaterial = val4;
				}
			}
			SpriteRenderer val7 = default(SpriteRenderer);
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null && ((Component)val3).TryGetComponent<SpriteRenderer>(out val7))
			{
				val7.color = Color.white;
				if ((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
				{
					((Renderer)val7).sharedMaterial = val5;
				}
			}
			WaterZone component = val.GetComponent<WaterZone>();
			EnsureWaterBubbleEffects(val, out var riseEffect, out var popEffect);
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				SerializedObject val8 = new SerializedObject((UnityEngine.Object)(object)component);
				val8.FindProperty("bubbleRiseEffect").objectReferenceValue = (UnityEngine.Object)(object)riseEffect;
				val8.FindProperty("bubblePopEffect").objectReferenceValue = (UnityEngine.Object)(object)popEffect;
				val8.ApplyModifiedPropertiesWithoutUndo();
			}
			PrefabUtility.SaveAsPrefabAsset(val, prefabPath);
			PrefabUtility.UnloadPrefabContents(val);
		}

		[MenuItem("Ball Game/Upgrade WaterZone Effects")]
		public static void UpgradeWaterZoneEffects()
		{
			string text = "Assets/Resources/Prefabs/WaterZone.prefab";
			if ((UnityEngine.Object)(object)AssetDatabase.LoadAssetAtPath<GameObject>(text) != (UnityEngine.Object)null)
			{
				EnsureWaterZoneMaterials(text);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			Debug.Log((object)"WaterZone effects upgraded.");
		}

		private static void EnsureWaterBubbleEffects(GameObject root, out ParticleSystem riseEffect, out ParticleSystem popEffect)
		{
			riseEffect = GetOrCreateChildParticleSystem(root.transform, "BubbleRiseEffect");
			popEffect = GetOrCreateChildParticleSystem(root.transform, "BubblePopEffect");
			Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BubbleParticle.mat");
			ConfigureWaterBubbleEffect(riseEffect, expands: false, 5, material);
			ConfigureWaterBubbleEffect(popEffect, expands: true, 7, material);
		}

		private static ParticleSystem GetOrCreateChildParticleSystem(Transform root, string childName)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Expected O, but got Unknown
			Transform val = root.Find(childName);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				GameObject val2 = new GameObject(childName);
				val2.transform.SetParent(root, false);
				val = val2.transform;
			}
			ParticleSystem component = ((Component)val).GetComponent<ParticleSystem>();
			return ((UnityEngine.Object)(object)component != (UnityEngine.Object)null) ? component : ((Component)val).gameObject.AddComponent<ParticleSystem>();
		}

		private static void ConfigureWaterBubbleEffect(ParticleSystem effect, bool expands, int sortingOrder, Material material)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			MainModule main = effect.main;
			main.playOnAwake = false;
			main.loop = false;
			main.simulationSpace = (ParticleSystemSimulationSpace)1;
			main.gravityModifier = 0f;
			main.maxParticles = 128;
			EmissionModule emission = effect.emission;
			emission.enabled = false;
			ShapeModule shape = effect.shape;
			shape.enabled = false;
			SizeOverLifetimeModule sizeOverLifetime = effect.sizeOverLifetime;
			sizeOverLifetime.enabled = expands;
			if (expands)
			{
				sizeOverLifetime.size = new MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.55f, 1f, 1.45f));
			}
			ParticleSystemRenderer component = ((Component)effect).GetComponent<ParticleSystemRenderer>();
			((Renderer)component).sortingOrder = sortingOrder;
			if ((UnityEngine.Object)(object)material != (UnityEngine.Object)null)
			{
				((Renderer)component).sharedMaterial = material;
			}
		}

		[MenuItem("Ball Game/Upgrade Anti-Gravity Visuals")]
		public static void UpgradeAntiGravityVisuals()
		{
			EnsureAntiGravityPrefabConfigured();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log((object)"Anti-gravity visuals upgraded.");
		}

		private static void EnsureAntiGravityPrefabConfigured()
		{
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Resources/Prefabs/AntiGravity.prefab";
			if ((UnityEngine.Object)(object)AssetDatabase.LoadAssetAtPath<GameObject>(text) == (UnityEngine.Object)null)
			{
				return;
			}
			GameObject val = PrefabUtility.LoadPrefabContents(text);
			Transform obj = val.transform.Find("RangeHint");
			SpriteRenderer val2 = ((obj != null) ? ((Component)obj).GetComponent<SpriteRenderer>() : null);
			Material val3 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AntiGravityFlow.mat");
			ConfigureAntiGravityMaterial(val3);
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				val2.color = Color.white;
				if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
				{
					((Renderer)val2).sharedMaterial = val3;
				}
			}
			PrefabUtility.SaveAsPrefabAsset(val, text);
			PrefabUtility.UnloadPrefabContents(val);
		}

		private static void ConfigureAntiGravityMaterial(Material material)
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)material == (UnityEngine.Object)null))
			{
				material.SetColor("_BaseColor", new Color(36f / 85f, 0.3019608f, 1f, 0.18f));
				material.SetColor("_FlowColor", new Color(0.7490196f, 2f / 3f, 1f, 0.7f));
				material.SetColor("_ArrowColor", new Color(0.94509804f, 0.9254902f, 1f, 0.9f));
				material.SetFloat("_FlowDensity", 3f);
				material.SetFloat("_FlowSpeed", 1.2f);
				material.SetFloat("_ArrowStrength", 0.65f);
				material.SetFloat("_EdgePulseStrength", 0.35f);
				EditorUtility.SetDirty((UnityEngine.Object)(object)material);
			}
		}

		private static void EnsurePlaceableDefinition(string prefabPath, ToolDefinition definition)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Expected O, but got Unknown
			GameObject val = PrefabUtility.LoadPrefabContents(prefabPath);
			PlaceableTool component = val.GetComponent<PlaceableTool>();
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				SerializedObject val2 = new SerializedObject((UnityEngine.Object)(object)component);
				val2.FindProperty("definition").objectReferenceValue = (UnityEngine.Object)(object)definition;
				val2.ApplyModifiedPropertiesWithoutUndo();
				PrefabUtility.SaveAsPrefabAsset(val, prefabPath);
			}
			PrefabUtility.UnloadPrefabContents(val);
		}

		[MenuItem("Ball Game/Upgrade Obstacle Prefab")]
		public static void UpgradeObstaclePrefab()
		{
			string text = "Assets/Resources/Prefabs/Obstacle.prefab";
			if ((UnityEngine.Object)(object)AssetDatabase.LoadAssetAtPath<GameObject>(text) == (UnityEngine.Object)null)
			{
				CreateMissingPrototypeAssets();
				return;
			}
			EnsureObstaclePrefabConfigured(text);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log((object)"Obstacle prefab upgraded.");
		}

		private static void EnsureObstaclePrefabConfigured(string path)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = PrefabUtility.LoadPrefabContents(path);
			SpriteRenderer val2 = val.GetComponent<SpriteRenderer>();
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				val2 = val.AddComponent<SpriteRenderer>();
				val2.color = new Color(0.33f, 0.34f, 0.36f);
				((Renderer)val2).sortingOrder = 0;
			}
			val2.drawMode = (SpriteDrawMode)1;
			if ((UnityEngine.Object)(object)val.GetComponent<BoxCollider2D>() == (UnityEngine.Object)null)
			{
				val.AddComponent<BoxCollider2D>();
			}
			if ((UnityEngine.Object)(object)val.GetComponent<ObstacleSize2D>() == (UnityEngine.Object)null)
			{
				val.AddComponent<ObstacleSize2D>();
			}
			val.transform.localScale = Vector3.one;
			PrefabUtility.SaveAsPrefabAsset(val, path);
			PrefabUtility.UnloadPrefabContents(val);
		}

		private static GameObject GetOrCreateWorldBackgroundPrefab(Sprite square)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Resources/Prefabs/WorldBackground.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			GameObject val2 = new GameObject("WorldBackground");
			SpriteRenderer val3 = val2.AddComponent<SpriteRenderer>();
			val3.sprite = square;
			val3.color = new Color(0.09f, 0.11f, 0.14f);
			((Renderer)val3).sortingOrder = -100;
			val2.AddComponent<InfiniteTiledBackground2D>();
			val2.transform.position = new Vector3(0f, 0f, 4f);
			val2.transform.localScale = Vector3.one;
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static ToolDefinition GetOrCreateTool(string name, GameObject prefab, ToolInventoryButton inventoryButtonPrefab, Sprite icon, Color color, int count, bool canRotate, float rotationStep, float radius, string displayName = null)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected O, but got Unknown
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Tools/" + name + ".asset";
			ToolDefinition val = AssetDatabase.LoadAssetAtPath<ToolDefinition>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			val = ScriptableObject.CreateInstance<ToolDefinition>();
			AssetDatabase.CreateAsset((UnityEngine.Object)(object)val, text);
			SerializedObject val2 = new SerializedObject((UnityEngine.Object)(object)val);
			val2.FindProperty("displayName").stringValue = (string.IsNullOrEmpty(displayName) ? name : displayName);
			val2.FindProperty("prefab").objectReferenceValue = (UnityEngine.Object)(object)prefab;
			val2.FindProperty("inventoryButtonPrefab").objectReferenceValue = (UnityEngine.Object)(object)inventoryButtonPrefab;
			val2.FindProperty("icon").objectReferenceValue = (UnityEngine.Object)(object)icon;
			val2.FindProperty("iconColor").colorValue = color;
			val2.FindProperty("defaultCount").intValue = count;
			val2.FindProperty("cost").intValue = 1;
			val2.FindProperty("canRotate").boolValue = canRotate;
			val2.FindProperty("rotationStep").floatValue = rotationStep;
			val2.FindProperty("placementRadius").floatValue = radius;
			val2.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty((UnityEngine.Object)(object)val);
			return val;
		}

		private static LevelDefinition[] GetOrCreateLevels(params ToolDefinition[] tools)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			ToolDefinition tool = tools[0];
			ToolDefinition tool2 = tools[1];
			ToolDefinition tool3 = tools[2];
			ToolDefinition tool4 = tools[3];
			ToolDefinition tool5 = tools[4];
			ToolDefinition tool6 = tools[5];
			return (LevelDefinition[])(object)new LevelDefinition[5]
			{
				GetOrCreateLevel("01 Plank Path", (LevelDefinition.ToolStock[])(object)new LevelDefinition.ToolStock[1] { Stock(tool, 3) }),
				GetOrCreateLevel("02 Spring Gap", (LevelDefinition.ToolStock[])(object)new LevelDefinition.ToolStock[2]
				{
					Stock(tool, 2),
					Stock(tool2, 1)
				}),
				GetOrCreateLevel("03 Fan Lift", (LevelDefinition.ToolStock[])(object)new LevelDefinition.ToolStock[2]
				{
					Stock(tool, 2),
					Stock(tool3, 1)
				}),
				GetOrCreateLevel("04 Conveyor Timing", (LevelDefinition.ToolStock[])(object)new LevelDefinition.ToolStock[3]
				{
					Stock(tool, 2),
					Stock(tool2, 1),
					Stock(tool4, 1)
				}),
				GetOrCreateLevel("05 Field Tricks", (LevelDefinition.ToolStock[])(object)new LevelDefinition.ToolStock[4]
				{
					Stock(tool, 2),
					Stock(tool3, 1),
					Stock(tool5, 1),
					Stock(tool6, 2)
				})
			};
		}

		private static LevelDefinition GetOrCreateLevel(string title, LevelDefinition.ToolStock[] stocks)
		{
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Expected O, but got Unknown
			string text = "Assets/Levels/" + title.Replace(' ', '_') + ".asset";
			LevelDefinition val = AssetDatabase.LoadAssetAtPath<LevelDefinition>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			val = ScriptableObject.CreateInstance<LevelDefinition>();
			AssetDatabase.CreateAsset((UnityEngine.Object)(object)val, text);
			SerializedObject val2 = new SerializedObject((UnityEngine.Object)(object)val);
			val2.FindProperty("levelTitle").stringValue = title;
			SerializedProperty val3 = val2.FindProperty("tools");
			val3.arraySize = stocks.Length;
			for (int i = 0; i < stocks.Length; i++)
			{
				SerializedProperty arrayElementAtIndex = val3.GetArrayElementAtIndex(i);
				arrayElementAtIndex.FindPropertyRelative("tool").objectReferenceValue = (UnityEngine.Object)(object)stocks[i].tool;
				arrayElementAtIndex.FindPropertyRelative("count").intValue = stocks[i].count;
			}
			val2.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty((UnityEngine.Object)(object)val);
			return val;
		}

		private static LevelDefinition.ToolStock Stock(ToolDefinition tool, int count)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			return new LevelDefinition.ToolStock
			{
				tool = tool,
				count = count
			};
		}

		private static GameObject GetOrCreateInventoryButtonPrefab(Sprite square)
		{
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Expected O, but got Unknown
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Expected O, but got Unknown
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c5: Expected O, but got Unknown
			string text = "Assets/Resources/UI/ToolInventoryButton.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			GameObject val2 = new GameObject("ToolInventoryButton", new Type[4]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(Button),
				typeof(ToolInventoryButton)
			});
			RectTransform component = val2.GetComponent<RectTransform>();
			component.sizeDelta = new Vector2(116f, 64f);
			((Graphic)val2.GetComponent<Image>()).color = new Color(0.14f, 0.16f, 0.18f, 0.9f);
			Image objectReferenceValue = CreateUiImage("Icon", val2.transform, square, new Vector2(26f, 26f), new Vector2(0f, 10f));
			TMP_Text objectReferenceValue2 = CreateUiText("Label", val2.transform, "Tool", 13, new Vector2(72f, 20f), new Vector2(4f, -15f));
			TMP_Text objectReferenceValue3 = CreateUiText("Count", val2.transform, "0", 15, new Vector2(26f, 22f), new Vector2(42f, 14f));
			GameObject val3 = new GameObject("CostLabel", new Type[1] { typeof(RectTransform) });
			val3.transform.SetParent(val2.transform, false);
			TMP_Text objectReferenceValue4 = CreateUiText("CostCount", val3.transform, "1", 15, new Vector2(50f, 22f), Vector2.zero);
			val3.SetActive(false);
			ToolInventoryButton component2 = val2.GetComponent<ToolInventoryButton>();
			SerializedObject val4 = new SerializedObject((UnityEngine.Object)(object)component2);
			val4.FindProperty("button").objectReferenceValue = (UnityEngine.Object)(object)val2.GetComponent<Button>();
			val4.FindProperty("icon").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue;
			val4.FindProperty("label").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue2;
			val4.FindProperty("countLabel").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue3;
			val4.FindProperty("costLabel").objectReferenceValue = (UnityEngine.Object)(object)val3;
			val4.FindProperty("costCount").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue4;
			val4.ApplyModifiedPropertiesWithoutUndo();
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static void EnsureInventoryButtonModeLabels(string path)
		{
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Expected O, but got Unknown
			if (!((UnityEngine.Object)(object)AssetDatabase.LoadAssetAtPath<GameObject>(path) == (UnityEngine.Object)null))
			{
				GameObject val = PrefabUtility.LoadPrefabContents(path);
				ToolInventoryButton component = val.GetComponent<ToolInventoryButton>();
				Transform val2 = val.transform.Find("Count");
				Transform val3 = val.transform.Find("CostLabel");
				Transform val4 = (((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null) ? val3.Find("CostCount") : null);
				if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
				{
					SerializedObject val5 = new SerializedObject((UnityEngine.Object)(object)component);
					val5.FindProperty("countLabel").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? ((Component)val2).GetComponent<TMP_Text>() : null);
					val5.FindProperty("costLabel").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null) ? ((Component)val3).gameObject : null);
					val5.FindProperty("costCount").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null) ? ((Component)val4).GetComponent<TMP_Text>() : null);
					val5.ApplyModifiedPropertiesWithoutUndo();
					PrefabUtility.SaveAsPrefabAsset(val, path);
				}
				PrefabUtility.UnloadPrefabContents(val);
			}
		}

		private static void BuildLevelScene(GameObject ballPrefab, GameObject goalPrefab, GameObject gameHudPrefab, GameObject backgroundPrefab, GameObject obstaclePrefab, LevelDefinition level, string scenePath, string nextSceneName, int levelIndex)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Expected O, but got Unknown
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Expected O, but got Unknown
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Expected O, but got Unknown
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Expected O, but got Unknown
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Expected O, but got Unknown
			//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			Scene val = EditorSceneManager.NewScene((NewSceneSetup)0, (NewSceneMode)0);
			Camera val2 = new GameObject("Main Camera").AddComponent<Camera>();
			((Component)val2).tag = "MainCamera";
			val2.orthographic = true;
			val2.orthographicSize = 5f;
			((Component)val2).transform.position = new Vector3(0f, 0f, -10f);
			val2.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
			CameraController2D cameraController = ((Component)val2).gameObject.AddComponent<CameraController2D>();
			GameObject val3 = new GameObject("Game Systems");
			GameManager manager = val3.AddComponent<GameManager>();
			PlacementManager placement = val3.AddComponent<PlacementManager>();
			MapBounds2D mapBounds = CreateMapBounds(levelIndex);
			Transform transform = new GameObject("Placed Tools").transform;
			GameObject backgroundObject = (GameObject)PrefabUtility.InstantiatePrefab((UnityEngine.Object)(object)backgroundPrefab);
			AssignInfiniteBackground(backgroundObject, val2);
			GameObject val4 = (GameObject)PrefabUtility.InstantiatePrefab((UnityEngine.Object)(object)ballPrefab);
			GameObject val5 = (GameObject)PrefabUtility.InstantiatePrefab((UnityEngine.Object)(object)goalPrefab);
			GameObject val6 = (GameObject)PrefabUtility.InstantiatePrefab((UnityEngine.Object)(object)gameHudPrefab);
			ApplyLevelLayout(levelIndex, val4, val5, obstaclePrefab);
			BallController component = val4.GetComponent<BallController>();
			GoalTrigger component2 = val5.GetComponent<GoalTrigger>();
			GameHudRefs component3 = val6.GetComponent<GameHudRefs>();
			if ((UnityEngine.Object)(object)UnityEngine.Object.FindObjectOfType<EventSystem>() == (UnityEngine.Object)null)
			{
				new GameObject("EventSystem", new Type[2]
				{
					typeof(EventSystem),
					typeof(StandaloneInputModule)
				});
			}
			AssignPlacement(placement, val2, transform);
			AssignControls(component3.PlacementControls, placement);
			AssignManager(manager, component, component2, cameraController, placement, component3.InventoryPanel, component3.TitleText, component3.StatusText, component3.RunButton, component3.ResetButton, component3.EditButton, component3.NextButton, level, nextSceneName);
			AssignCameraController(cameraController, manager, component, placement, mapBounds);
			AssignTargetIndicators(component3, manager, val2, val4.transform, val5.transform);
			Selection.activeObject = (UnityEngine.Object)(object)val6;
			EditorSceneManager.SaveScene(val, scenePath);
		}

		private static MapBounds2D CreateMapBounds(int levelIndex)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Expected O, but got Unknown
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("Map Bounds");
			MapBounds2D val2 = val.AddComponent<MapBounds2D>();
			Vector2 val3 = default(Vector2);
			val3 = new Vector2(0f, -0.4f);
			Vector2 val4 = ((levelIndex == 2 || levelIndex == 4) ? new Vector2(20f, 10.5f) : new Vector2(20f, 9f));
			val2.SetBounds(val3, val4);
			return val2;
		}

		private static void ApplyLevelLayout(int levelIndex, GameObject ballObject, GameObject goalObject, GameObject obstaclePrefab)
		{
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_0168: Unknown result type (might be due to invalid IL or missing references)
			//IL_0188: Unknown result type (might be due to invalid IL or missing references)
			//IL_0197: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0202: Unknown result type (might be due to invalid IL or missing references)
			//IL_0222: Unknown result type (might be due to invalid IL or missing references)
			//IL_0231: Unknown result type (might be due to invalid IL or missing references)
			//IL_025e: Unknown result type (might be due to invalid IL or missing references)
			//IL_026d: Unknown result type (might be due to invalid IL or missing references)
			//IL_028d: Unknown result type (might be due to invalid IL or missing references)
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_032c: Unknown result type (might be due to invalid IL or missing references)
			//IL_032d: Unknown result type (might be due to invalid IL or missing references)
			//IL_033e: Unknown result type (might be due to invalid IL or missing references)
			//IL_033f: Unknown result type (might be due to invalid IL or missing references)
			//IL_034f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0376: Unknown result type (might be due to invalid IL or missing references)
			//IL_037d: Expected O, but got Unknown
			//IL_0386: Unknown result type (might be due to invalid IL or missing references)
			//IL_038b: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = default(Vector2);
			val = new Vector2(-7f, -2.4f);
			Vector2 val2 = default(Vector2);
			val2 = new Vector2(7f, -2.3f);
			if (1 == 0)
			{
			}
			ObstacleSpec[] array = levelIndex switch
			{
				0 => new ObstacleSpec[2]
				{
					Obstacle(new Vector2(0f, -3.4f), new Vector2(16f, 0.4f), 0f),
					Obstacle(new Vector2(1.5f, -1.3f), new Vector2(2.5f, 0.35f), -18f)
				}, 
				1 => new ObstacleSpec[3]
				{
					Obstacle(new Vector2(-4f, -3.4f), new Vector2(7f, 0.4f), 0f),
					Obstacle(new Vector2(5f, -3.4f), new Vector2(5f, 0.4f), 0f),
					Obstacle(new Vector2(0f, -3f), new Vector2(1.3f, 1f), 0f)
				}, 
				2 => new ObstacleSpec[2]
				{
					Obstacle(new Vector2(0f, -3.4f), new Vector2(16f, 0.4f), 0f),
					Obstacle(new Vector2(4.5f, -1.2f), new Vector2(4f, 0.35f), 12f)
				}, 
				3 => new ObstacleSpec[3]
				{
					Obstacle(new Vector2(-4f, -3.4f), new Vector2(7f, 0.4f), 0f),
					Obstacle(new Vector2(4f, -3.4f), new Vector2(7f, 0.4f), 0f),
					Obstacle(new Vector2(0f, -1.5f), new Vector2(2f, 0.35f), 0f)
				}, 
				_ => new ObstacleSpec[3]
				{
					Obstacle(new Vector2(0f, -3.4f), new Vector2(16f, 0.4f), 0f),
					Obstacle(new Vector2(2.2f, -0.8f), new Vector2(3.5f, 0.35f), -15f),
					Obstacle(new Vector2(6.5f, 0.5f), new Vector2(2.5f, 0.35f), 0f)
				}, 
			};
			if (1 == 0)
			{
			}
			ObstacleSpec[] array2 = array;
			switch (levelIndex)
			{
			case 2:
				val2 = new Vector2(7f, 0.25f);
				break;
			case 4:
				val2 = new Vector2(7f, 1.2f);
				break;
			}
			ballObject.transform.position = (Vector2)(val);
			goalObject.transform.position = (Vector2)(val2);
			Transform transform = new GameObject("Map Obstacles").transform;
			ObstacleSpec[] array3 = array2;
			for (int i = 0; i < array3.Length; i++)
			{
				ObstacleSpec obstacleSpec = array3[i];
				GameObject val3 = (GameObject)PrefabUtility.InstantiatePrefab((UnityEngine.Object)(object)obstaclePrefab, transform);
				val3.transform.position = (Vector2)(obstacleSpec.position);
				val3.transform.rotation = Quaternion.Euler(0f, 0f, obstacleSpec.rotation);
				ApplyObstacleSize(val3, obstacleSpec.size);
			}
		}

		private static void ApplyObstacleSize(GameObject obstacleObject, Vector2 size)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			obstacleObject.transform.localScale = Vector3.one;
			ObstacleSize2D component = obstacleObject.GetComponent<ObstacleSize2D>();
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				component.SetSize(size);
				return;
			}
			SpriteRenderer component2 = obstacleObject.GetComponent<SpriteRenderer>();
			if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
			{
				component2.drawMode = (SpriteDrawMode)1;
				component2.size = size;
			}
			BoxCollider2D component3 = obstacleObject.GetComponent<BoxCollider2D>();
			if ((UnityEngine.Object)(object)component3 != (UnityEngine.Object)null)
			{
				((Collider2D)component3).offset = Vector2.zero;
				component3.size = size;
			}
		}

		private static ObstacleSpec Obstacle(Vector2 position, Vector2 size, float rotation)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			return new ObstacleSpec(position, size, rotation);
		}

		private static GameObject GetOrCreateBallPrefab(Sprite circle)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Resources/Prefabs/Ball.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			GameObject val2 = new GameObject("Ball");
			val2.transform.position = new Vector3(-7f, -2.5f, 0f);
			SpriteRenderer val3 = val2.AddComponent<SpriteRenderer>();
			val3.sprite = circle;
			val3.color = new Color(1f, 0.85f, 0.25f);
			((Renderer)val3).sortingOrder = 20;
			val2.transform.localScale = Vector3.one * 0.55f;
			Rigidbody2D val4 = val2.AddComponent<Rigidbody2D>();
			val4.gravityScale = 1.2f;
			val4.collisionDetectionMode = (CollisionDetectionMode2D)1;
			val4.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/Ball Physics Material 2D.physicsMaterial2D");
			CircleCollider2D val5 = val2.AddComponent<CircleCollider2D>();
			((Collider2D)val5).sharedMaterial = val4.sharedMaterial;
			val2.AddComponent<BallController>();
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static GameObject GetOrCreateGoalPrefab(Sprite circle)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			string text = "Assets/Resources/Prefabs/Goal.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				return val;
			}
			GameObject val2 = new GameObject("Goal");
			val2.transform.position = new Vector3(7f, -2.35f, 0f);
			SpriteRenderer val3 = val2.AddComponent<SpriteRenderer>();
			val3.sprite = circle;
			val3.color = new Color(0.25f, 1f, 0.35f, 0.65f);
			((Renderer)val3).sortingOrder = 10;
			val2.transform.localScale = Vector3.one * 0.9f;
			((Collider2D)val2.AddComponent<CircleCollider2D>()).isTrigger = true;
			val2.AddComponent<GoalTrigger>();
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		[MenuItem("Ball Game/Upgrade GameHud Target Indicators")]
		public static void UpgradeGameHudTargetIndicators()
		{
			EnsureFolders();
			EnsureGameHudTargetIndicators("Assets/Resources/UI/GameHud.prefab", CreateArrowSprite());
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log((object)"GameHud target indicators upgraded.");
		}

		private static GameObject GetOrCreateGameHudPrefab(ToolInventoryButton inventoryButtonPrefab, Sprite arrowSprite)
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Expected O, but got Unknown
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_015b: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_020d: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_0252: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			//IL_0288: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0302: Unknown result type (might be due to invalid IL or missing references)
			//IL_0311: Unknown result type (might be due to invalid IL or missing references)
			//IL_0338: Unknown result type (might be due to invalid IL or missing references)
			//IL_0347: Unknown result type (might be due to invalid IL or missing references)
			//IL_0368: Unknown result type (might be due to invalid IL or missing references)
			//IL_037f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0396: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b3: Expected O, but got Unknown
			//IL_0463: Unknown result type (might be due to invalid IL or missing references)
			//IL_0472: Unknown result type (might be due to invalid IL or missing references)
			//IL_0493: Unknown result type (might be due to invalid IL or missing references)
			//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0519: Unknown result type (might be due to invalid IL or missing references)
			//IL_0530: Unknown result type (might be due to invalid IL or missing references)
			//IL_0547: Unknown result type (might be due to invalid IL or missing references)
			//IL_0584: Unknown result type (might be due to invalid IL or missing references)
			//IL_058b: Expected O, but got Unknown
			//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0623: Unknown result type (might be due to invalid IL or missing references)
			//IL_0649: Unknown result type (might be due to invalid IL or missing references)
			//IL_0658: Unknown result type (might be due to invalid IL or missing references)
			//IL_0679: Unknown result type (might be due to invalid IL or missing references)
			//IL_0690: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_06cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_06dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_06fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0714: Unknown result type (might be due to invalid IL or missing references)
			//IL_072b: Unknown result type (might be due to invalid IL or missing references)
			//IL_075b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0762: Expected O, but got Unknown
			//IL_0782: Unknown result type (might be due to invalid IL or missing references)
			//IL_078f: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_07bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_07ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0801: Expected O, but got Unknown
			//IL_082b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0842: Unknown result type (might be due to invalid IL or missing references)
			//IL_0859: Unknown result type (might be due to invalid IL or missing references)
			//IL_0870: Unknown result type (might be due to invalid IL or missing references)
			//IL_087d: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_08f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_091c: Unknown result type (might be due to invalid IL or missing references)
			//IL_092b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0952: Unknown result type (might be due to invalid IL or missing references)
			//IL_0961: Unknown result type (might be due to invalid IL or missing references)
			//IL_096f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0976: Expected O, but got Unknown
			//IL_09d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_09d8: Expected O, but got Unknown
			string text = "Assets/Resources/UI/GameHud.prefab";
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				EnsureGameHudTargetIndicators(text, arrowSprite);
				EnsureGameHudCostText(text);
				return val;
			}
			GameObject val2 = new GameObject("GameHud", new Type[5]
			{
				typeof(RectTransform),
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster),
				typeof(GameHudRefs)
			});
			RemoveUiBackgroundIfPresent(val2.transform);
			Canvas component = val2.GetComponent<Canvas>();
			component.renderMode = (RenderMode)0;
			CanvasScaler component2 = val2.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			component2.referenceResolution = new Vector2(1920f, 1080f);
			component2.matchWidthOrHeight = 0.5f;
			GameObject val3 = CreatePanel("Top Bar", val2.transform, AnchorPreset.TopStretch, new Vector2(0f, 72f), new Vector2(0f, -36f));
			TMP_Text objectReferenceValue = CreateUiText("Title", val3.transform, "Ball Puzzle", 26, new Vector2(360f, 42f), new Vector2(-660f, 0f));
			TMP_Text objectReferenceValue2 = CreateUiText("Status", val3.transform, "Build", 20, new Vector2(520f, 42f), new Vector2(-170f, 0f));
			TMP_Text val4 = CreateUiText("Cost", val3.transform, string.Empty, 18, new Vector2(300f, 42f), new Vector2(155f, 0f));
			((Component)val4).gameObject.SetActive(false);
			Button objectReferenceValue3 = CreateButton("Run", val3.transform, "Run", new Vector2(100f, 44f), new Vector2(410f, 0f));
			Button objectReferenceValue4 = CreateButton("Reset", val3.transform, "Reset", new Vector2(110f, 44f), new Vector2(530f, 0f));
			Button objectReferenceValue5 = CreateButton("Edit", val3.transform, "Edit", new Vector2(100f, 44f), new Vector2(650f, 0f));
			Button objectReferenceValue6 = CreateButton("Next", val3.transform, "Next", new Vector2(100f, 44f), new Vector2(760f, 0f));
			WorldTargetIndicator objectReferenceValue7 = CreateTargetIndicator("BallArrow", val2.transform, arrowSprite, new Color(1f, 0.86f, 0.18f, 0.95f));
			WorldTargetIndicator objectReferenceValue8 = CreateTargetIndicator("GoalArrow", val2.transform, arrowSprite, new Color(0.25f, 1f, 0.35f, 0.95f));
			GameObject val5 = CreatePanel("Tool Bar", val2.transform, AnchorPreset.LeftStretch, new Vector2(140f, -92f), new Vector2(10f, -36f));
			Button val6 = CreateButton("Toggle", val5.transform, "<", new Vector2(44f, 64f), new Vector2(22f, 0f));
			RectTransform component3 = ((Component)val6).GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(1f, 0.5f);
			component3.anchorMax = new Vector2(1f, 0.5f);
			component3.pivot = new Vector2(0.5f, 0.5f);
			ToolBarCollapser val7 = val5.AddComponent<ToolBarCollapser>();
			SerializedObject val8 = new SerializedObject((UnityEngine.Object)(object)val7);
			val8.FindProperty("toolBar").objectReferenceValue = (UnityEngine.Object)(object)val5.GetComponent<RectTransform>();
			val8.FindProperty("toggleButton").objectReferenceValue = (UnityEngine.Object)(object)val6;
			val8.FindProperty("toggleLabel").objectReferenceValue = (UnityEngine.Object)(object)((Component)val6).GetComponentInChildren<TMP_Text>();
			val8.FindProperty("expandedX").floatValue = 10f;
			val8.FindProperty("collapsedX").floatValue = -140f;
			val8.FindProperty("duration").floatValue = 0.35f;
			val8.ApplyModifiedPropertiesWithoutUndo();
			TMP_Text val9 = CreateUiText("InventoryTitle", val5.transform, "物品栏", 22, new Vector2(120f, 34f), new Vector2(0f, -25f));
			RectTransform rectTransform = val9.rectTransform;
			rectTransform.anchorMin = new Vector2(0.5f, 1f);
			rectTransform.anchorMax = new Vector2(0.5f, 1f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			TMP_Text val10 = CreateUiText("InventoryPageLabel", val5.transform, "0/0", 16, new Vector2(120f, 24f), new Vector2(0f, 20f));
			RectTransform rectTransform2 = val10.rectTransform;
			rectTransform2.anchorMin = new Vector2(0.5f, 0f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			GameObject val11 = new GameObject("Inventory", new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(ToolInventoryPanel)
			});
			val11.transform.SetParent(val5.transform, false);
			RectTransform component4 = val11.GetComponent<RectTransform>();
			component4.anchorMin = Vector2.zero;
			component4.anchorMax = Vector2.one;
			component4.pivot = new Vector2(0.5f, 0.5f);
			component4.sizeDelta = new Vector2(-20f, -85f);
			component4.anchoredPosition = new Vector2(0f, -12.5f);
			((Graphic)val11.GetComponent<Image>()).color = new Color(1f, 1f, 1f, 0f);
			Button val12 = CreateButton("InventoryPrevPage", val11.transform, "^", new Vector2(64f, 44f), new Vector2(0f, -25f));
			RectTransform component5 = ((Component)val12).GetComponent<RectTransform>();
			component5.anchorMin = new Vector2(0.5f, 1f);
			component5.anchorMax = new Vector2(0.5f, 1f);
			component5.pivot = new Vector2(0.5f, 0.5f);
			Button val13 = CreateButton("InventoryNextPage", val11.transform, "v", new Vector2(64f, 44f), new Vector2(0f, 25f));
			RectTransform component6 = ((Component)val13).GetComponent<RectTransform>();
			component6.anchorMin = new Vector2(0.5f, 0f);
			component6.anchorMax = new Vector2(0.5f, 0f);
			component6.pivot = new Vector2(0.5f, 0.5f);
			GameObject val14 = new GameObject("Viewport", new Type[2]
			{
				typeof(RectTransform),
				typeof(RectMask2D)
			});
			val14.transform.SetParent(val11.transform, false);
			RectTransform component7 = val14.GetComponent<RectTransform>();
			component7.anchorMin = Vector2.zero;
			component7.anchorMax = Vector2.one;
			component7.pivot = new Vector2(0.5f, 0.5f);
			component7.sizeDelta = new Vector2(-10f, -110f);
			component7.anchoredPosition = Vector2.zero;
			GameObject val15 = new GameObject("Content", new Type[2]
			{
				typeof(RectTransform),
				typeof(VerticalLayoutGroup)
			});
			val15.transform.SetParent(val14.transform, false);
			RectTransform component8 = val15.GetComponent<RectTransform>();
			component8.anchorMin = new Vector2(0.5f, 1f);
			component8.anchorMax = new Vector2(0.5f, 1f);
			component8.pivot = new Vector2(0.5f, 1f);
			component8.sizeDelta = new Vector2(100f, 100f);
			component8.anchoredPosition = Vector2.zero;
			VerticalLayoutGroup component9 = val15.GetComponent<VerticalLayoutGroup>();
			((Behaviour)component9).enabled = false;
			((HorizontalOrVerticalLayoutGroup)component9).spacing = 20f;
			((HorizontalOrVerticalLayoutGroup)component9).childForceExpandWidth = false;
			((HorizontalOrVerticalLayoutGroup)component9).childForceExpandHeight = false;
			ToolInventoryPanel component10 = val11.GetComponent<ToolInventoryPanel>();
			PlacementControls val16 = val5.AddComponent<PlacementControls>();
			Button objectReferenceValue9 = CreateButton("RotateLeft", val5.transform, "<", new Vector2(56f, 56f), new Vector2(610f, 0f));
			Button objectReferenceValue10 = CreateButton("RotateRight", val5.transform, ">", new Vector2(56f, 56f), new Vector2(676f, 0f));
			Button objectReferenceValue11 = CreateButton("Delete", val5.transform, "Del", new Vector2(72f, 56f), new Vector2(754f, 0f));
			SerializedObject val17 = new SerializedObject((UnityEngine.Object)(object)val16);
			val17.FindProperty("rotateLeftButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue9;
			val17.FindProperty("rotateRightButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue10;
			val17.FindProperty("deleteButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue11;
			val17.ApplyModifiedPropertiesWithoutUndo();
			AssignInventory(component10, inventoryButtonPrefab, component8, val12, val13, val10);
			SerializedObject val18 = new SerializedObject((UnityEngine.Object)(object)val2.GetComponent<GameHudRefs>());
			val18.FindProperty("inventoryPanel").objectReferenceValue = (UnityEngine.Object)(object)component10;
			val18.FindProperty("placementControls").objectReferenceValue = (UnityEngine.Object)(object)val16;
			val18.FindProperty("ballIndicator").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue7;
			val18.FindProperty("goalIndicator").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue8;
			val18.FindProperty("titleText").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue;
			val18.FindProperty("statusText").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue2;
			val18.FindProperty("costText").objectReferenceValue = (UnityEngine.Object)(object)val4;
			val18.FindProperty("runButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue3;
			val18.FindProperty("resetButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue4;
			val18.FindProperty("editButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue5;
			val18.FindProperty("nextButton").objectReferenceValue = (UnityEngine.Object)(object)objectReferenceValue6;
			val18.FindProperty("toolBarCollapser").objectReferenceValue = (UnityEngine.Object)(object)val7;
			Transform val19 = val2.transform.Find("Move Bar");
			val18.FindProperty("moveBar").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val19 != (UnityEngine.Object)null) ? ((Component)val19).gameObject : null);
			val18.ApplyModifiedPropertiesWithoutUndo();
			GameObject result = PrefabUtility.SaveAsPrefabAsset(val2, text);
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)val2);
			return result;
		}

		private static void EnsureGameHudTargetIndicators(string path, Sprite arrowSprite)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Expected O, but got Unknown
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				GameObject val2 = PrefabUtility.LoadPrefabContents(path);
				WorldTargetIndicator orCreateTargetIndicator = GetOrCreateTargetIndicator(val2.transform, "BallArrow", arrowSprite, new Color(1f, 0.86f, 0.18f, 0.95f));
				WorldTargetIndicator orCreateTargetIndicator2 = GetOrCreateTargetIndicator(val2.transform, "GoalArrow", arrowSprite, new Color(0.25f, 1f, 0.35f, 0.95f));
				GameHudRefs component = val2.GetComponent<GameHudRefs>();
				if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
				{
					SerializedObject val3 = new SerializedObject((UnityEngine.Object)(object)component);
					val3.FindProperty("ballIndicator").objectReferenceValue = (UnityEngine.Object)(object)orCreateTargetIndicator;
					val3.FindProperty("goalIndicator").objectReferenceValue = (UnityEngine.Object)(object)orCreateTargetIndicator2;
					Transform val4 = val2.transform.Find("Top Bar/Return");
					val3.FindProperty("returnButton").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null) ? ((Component)val4).GetComponent<Button>() : null);
					Transform val5 = val2.transform.Find("Tool Bar");
					val3.FindProperty("toolBarCollapser").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null) ? ((Component)val5).GetComponent<ToolBarCollapser>() : null);
					Transform val6 = val2.transform.Find("Move Bar");
					val3.FindProperty("moveBar").objectReferenceValue = (UnityEngine.Object)(object)(((UnityEngine.Object)(object)val6 != (UnityEngine.Object)null) ? ((Component)val6).gameObject : null);
					val3.ApplyModifiedPropertiesWithoutUndo();
				}
				PrefabUtility.SaveAsPrefabAsset(val2, path);
				PrefabUtility.UnloadPrefabContents(val2);
			}
		}

		private static void EnsureGameHudCostText(string path)
		{
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Expected O, but got Unknown
			GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				GameObject val2 = PrefabUtility.LoadPrefabContents(path);
				GameHudRefs component = val2.GetComponent<GameHudRefs>();
				Transform val3 = val2.transform.Find("Top Bar");
				object obj;
				if (!((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null))
				{
					obj = null;
				}
				else
				{
					Transform obj2 = val3.Find("Status");
					obj = ((obj2 != null) ? ((Component)obj2).GetComponent<TMP_Text>() : null);
				}
				TMP_Text val4 = (TMP_Text)obj;
				object obj3;
				if (!((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null))
				{
					obj3 = null;
				}
				else
				{
					Transform obj4 = val3.Find("Cost");
					obj3 = ((obj4 != null) ? ((Component)obj4).GetComponent<TMP_Text>() : null);
				}
				TMP_Text val5 = (TMP_Text)obj3;
				if ((UnityEngine.Object)(object)val5 == (UnityEngine.Object)null && (UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
				{
					val5 = UnityEngine.Object.Instantiate<TMP_Text>(val4, val3);
					((UnityEngine.Object)val5).name = "Cost";
					val5.text = string.Empty;
					val5.fontSize = 18f;
					val5.rectTransform.sizeDelta = new Vector2(300f, 42f);
					val5.rectTransform.anchoredPosition = new Vector2(155f, 0f);
					((Component)val5).gameObject.SetActive(false);
				}
				if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && (UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
				{
					SerializedObject val6 = new SerializedObject((UnityEngine.Object)(object)component);
					val6.FindProperty("costText").objectReferenceValue = (UnityEngine.Object)(object)val5;
					val6.ApplyModifiedPropertiesWithoutUndo();
					PrefabUtility.SaveAsPrefabAsset(val2, path);
				}
				PrefabUtility.UnloadPrefabContents(val2);
			}
		}

		private static WorldTargetIndicator GetOrCreateTargetIndicator(Transform parent, string name, Sprite arrowSprite, Color color)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			Transform val = parent.Find(name);
			return ((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ConfigureTargetIndicator(((Component)val).gameObject, arrowSprite, color) : CreateTargetIndicator(name, parent, arrowSprite, color);
		}

		private static WorldTargetIndicator CreateTargetIndicator(string name, Transform parent, Sprite arrowSprite, Color color)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(WorldTargetIndicator)
			});
			val.transform.SetParent(parent, false);
			return ConfigureTargetIndicator(val, arrowSprite, color);
		}

		private static WorldTargetIndicator ConfigureTargetIndicator(GameObject root, Sprite arrowSprite, Color color)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			RectTransform component = root.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.5f, 0.5f);
			component.anchorMax = new Vector2(0.5f, 0.5f);
			component.pivot = new Vector2(0.5f, 0.5f);
			component.sizeDelta = new Vector2(42f, 42f);
			component.anchoredPosition = Vector2.zero;
			Image component2 = root.GetComponent<Image>();
			component2.sprite = arrowSprite;
			((Graphic)component2).color = color;
			((Graphic)component2).raycastTarget = true;
			((Behaviour)component2).enabled = false;
			return root.GetComponent<WorldTargetIndicator>();
		}

		private static void RemoveUiBackgroundIfPresent(Transform root)
		{
			Transform val = root.Find("Background");
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)((Component)val).gameObject);
			}
		}

		private static GameObject CreatePanel(string name, Transform parent, AnchorPreset preset, Vector2 size, Vector2 position)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			ApplyAnchor(component, preset);
			component.sizeDelta = size;
			component.anchoredPosition = position;
			((Graphic)val.GetComponent<Image>()).color = new Color(0.08f, 0.1f, 0.12f, 0.82f);
			return val;
		}

		private static Button CreateButton(string name, Transform parent, string text, Vector2 size, Vector2 position)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(Button)
			});
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.sizeDelta = size;
			component.anchoredPosition = position;
			((Graphic)val.GetComponent<Image>()).color = new Color(0.22f, 0.26f, 0.3f);
			CreateUiText("Text", val.transform, text, 18, size, Vector2.zero);
			return val.GetComponent<Button>();
		}

		private static Image CreateUiImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.sizeDelta = size;
			component.anchoredPosition = position;
			Image component2 = val.GetComponent<Image>();
			component2.sprite = sprite;
			return component2;
		}

		private static TMP_Text CreateUiText(string name, Transform parent, string text, int fontSize, Vector2 size, Vector2 position)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.sizeDelta = size;
			component.anchoredPosition = position;
			TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
			((TMP_Text)component2).text = text;
			((TMP_Text)component2).fontSize = fontSize;
			((Graphic)component2).color = Color.white;
			((TMP_Text)component2).alignment = (TextAlignmentOptions)514;
			((TMP_Text)component2).enableAutoSizing = true;
			((TMP_Text)component2).fontSizeMin = 10f;
			((TMP_Text)component2).fontSizeMax = fontSize;
			return (TMP_Text)(object)component2;
		}

		private static void AssignPlacement(PlacementManager placement, Camera camera, Transform placedRoot)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)placement);
			val.FindProperty("worldCamera").objectReferenceValue = (UnityEngine.Object)(object)camera;
			val.FindProperty("placedRoot").objectReferenceValue = (UnityEngine.Object)(object)placedRoot;
			val.FindProperty("blockedPlacementMask").intValue = -1;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void AssignControls(PlacementControls controls, PlacementManager placement)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)controls);
			val.FindProperty("placementManager").objectReferenceValue = (UnityEngine.Object)(object)placement;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void AssignInventory(ToolInventoryPanel inventory, ToolInventoryButton prefab, RectTransform contentRoot, Button previousPageButton, Button nextPageButton, TMP_Text pageLabel)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)inventory);
			val.FindProperty("buttonPrefab").objectReferenceValue = (UnityEngine.Object)(object)prefab;
			val.FindProperty("contentRoot").objectReferenceValue = (UnityEngine.Object)(object)contentRoot;
			val.FindProperty("previousPageButton").objectReferenceValue = (UnityEngine.Object)(object)previousPageButton;
			val.FindProperty("nextPageButton").objectReferenceValue = (UnityEngine.Object)(object)nextPageButton;
			val.FindProperty("pageLabel").objectReferenceValue = (UnityEngine.Object)(object)pageLabel;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void AssignManager(GameManager manager, BallController ball, GoalTrigger goal, CameraController2D cameraController, PlacementManager placement, ToolInventoryPanel inventory, TMP_Text title, TMP_Text status, Button run, Button reset, Button edit, Button next, LevelDefinition level, string nextSceneName)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected O, but got Unknown
			SerializedObject val = new SerializedObject((UnityEngine.Object)(object)manager);
			val.FindProperty("ball").objectReferenceValue = (UnityEngine.Object)(object)ball;
			val.FindProperty("goal").objectReferenceValue = (UnityEngine.Object)(object)goal;
			val.FindProperty("cameraController").objectReferenceValue = (UnityEngine.Object)(object)cameraController;
			val.FindProperty("placementManager").objectReferenceValue = (UnityEngine.Object)(object)placement;
			val.FindProperty("inventoryPanel").objectReferenceValue = (UnityEngine.Object)(object)inventory;
			val.FindProperty("titleText").objectReferenceValue = (UnityEngine.Object)(object)title;
			val.FindProperty("statusText").objectReferenceValue = (UnityEngine.Object)(object)status;
			val.FindProperty("runButton").objectReferenceValue = (UnityEngine.Object)(object)run;
			val.FindProperty("resetButton").objectReferenceValue = (UnityEngine.Object)(object)reset;
			val.FindProperty("editButton").objectReferenceValue = (UnityEngine.Object)(object)edit;
			val.FindProperty("nextButton").objectReferenceValue = (UnityEngine.Object)(object)next;
			val.FindProperty("level").objectReferenceValue = (UnityEngine.Object)(object)level;
			val.FindProperty("nextSceneName").stringValue = nextSceneName;
			val.FindProperty("failureResetDelay").floatValue = 2f;
			val.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void AssignCameraController(CameraController2D cameraController, GameManager manager, BallController ball, PlacementManager placement, MapBounds2D mapBounds)
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

		private static void AssignTargetIndicators(GameHudRefs hud, GameManager manager, Camera camera, Transform ball, Transform goal)
		{
			if (!((UnityEngine.Object)(object)hud == (UnityEngine.Object)null))
			{
				Canvas component = ((Component)hud).GetComponent<Canvas>();
				AssignTargetIndicator(hud.BallIndicator, manager, camera, ball, component);
				AssignTargetIndicator(hud.GoalIndicator, manager, camera, goal, component);
			}
		}

		private static void AssignTargetIndicator(WorldTargetIndicator indicator, GameManager manager, Camera camera, Transform target, Canvas canvas)
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

		private static void AssignInfiniteBackground(GameObject backgroundObject, Camera targetCamera)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Expected O, but got Unknown
			if (!((UnityEngine.Object)(object)backgroundObject == (UnityEngine.Object)null))
			{
				InfiniteTiledBackground2D val = backgroundObject.GetComponent<InfiniteTiledBackground2D>();
				if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
				{
					val = backgroundObject.AddComponent<InfiniteTiledBackground2D>();
				}
				((Component)val).transform.position = new Vector3(0f, 0f, 4f);
				((Component)val).transform.localScale = Vector3.one;
				SerializedObject val2 = new SerializedObject((UnityEngine.Object)(object)val);
				val2.FindProperty("targetCamera").objectReferenceValue = (UnityEngine.Object)(object)targetCamera;
				val2.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		private static EditorBuildSettingsScene[] BuildEditorScenes(string[] scenePaths)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			EditorBuildSettingsScene[] array = (EditorBuildSettingsScene[])(object)new EditorBuildSettingsScene[scenePaths.Length];
			for (int i = 0; i < scenePaths.Length; i++)
			{
				array[i] = new EditorBuildSettingsScene(scenePaths[i], true);
			}
			return array;
		}

		private static void ApplyAnchor(RectTransform rect, AnchorPreset preset)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			switch (preset)
			{
			case AnchorPreset.TopStretch:
				rect.anchorMin = new Vector2(0f, 1f);
				rect.anchorMax = new Vector2(1f, 1f);
				rect.pivot = new Vector2(0.5f, 1f);
				break;
			case AnchorPreset.BottomStretch:
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(1f, 0f);
				rect.pivot = new Vector2(0.5f, 0f);
				break;
			default:
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(0f, 1f);
				rect.pivot = new Vector2(0f, 0.5f);
				break;
			}
		}
	}
}
