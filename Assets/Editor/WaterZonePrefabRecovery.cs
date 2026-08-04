using System;
using BallGame.Placement;
using BallGame.Tools;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class WaterZonePrefabRecovery
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/WaterZone.prefab";
        private const string SquareSpritePath = "Assets/Resources/Art/UI/Square.png";
        private const string BodyMaterialPath = "Assets/Resources/Materials/WaterBodyAnimated.mat";
        private const string SurfaceMaterialPath = "Assets/Resources/Materials/WaterSurfaceAnimated.mat";
        private const string ParticleMaterialPath = "Assets/Resources/Materials/FireworkParticle.mat";
        private const string BubbleMaterialPath = "Assets/Resources/Materials/BubbleParticle.mat";
        private const string ToolDefinitionPath = "Assets/Tools/WaterZone.asset";
        [MenuItem("Ball Game/Effects/Recover Missing WaterZone Prefab")]
        public static void RecoverMissingWaterZonePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                Debug.Log("WaterZone prefab already exists; recovery skipped: " + PrefabPath);
                return;
            }

            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            Material surfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SurfaceMaterialPath);
            Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            Material bubbleMaterial = AssetDatabase.LoadAssetAtPath<Material>(BubbleMaterialPath);
            UnityEngine.Object toolDefinition = AssetDatabase.LoadMainAssetAtPath(ToolDefinitionPath);
            if (square == null
                || bodyMaterial == null
                || surfaceMaterial == null
                || particleMaterial == null
                || bubbleMaterial == null
                || toolDefinition == null)
            {
                throw new InvalidOperationException("WaterZone recovery dependencies are missing.");
            }

            GameObject root = new GameObject("WaterZone");
            try
            {
                BoxCollider2D waterCollider = root.AddComponent<BoxCollider2D>();
                waterCollider.isTrigger = true;
                waterCollider.size = new Vector2(3f, 2f);

                SpriteRenderer waterRenderer = CreateWaterLayer(
                    root.transform,
                    "WaterBody",
                    square,
                    bodyMaterial,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(3f, 2f, 1f),
                    3);
                SpriteRenderer surfaceRenderer = CreateWaterLayer(
                    root.transform,
                    "SurfaceLine",
                    square,
                    surfaceMaterial,
                    new Vector3(0f, 1f, -0.01f),
                    new Vector3(3f, 0.18f, 1f),
                    4);

                ParticleSystem splash = CreateSplashEffect(root.transform, particleMaterial);
                ParticleSystem bubbleRise = CreateBubbleEffect(root.transform, "BubbleRiseEffect", false, bubbleMaterial, 5);
                ParticleSystem bubblePop = CreateBubbleEffect(root.transform, "BubblePopEffect", true, bubbleMaterial, 7);

                PlaceableTool placeable = root.AddComponent<PlaceableTool>();
                SerializedObject placeableObject = new SerializedObject(placeable);
                placeableObject.FindProperty("definition").objectReferenceValue = toolDefinition;
                SerializedProperty renderers = placeableObject.FindProperty("renderers");
                renderers.arraySize = 2;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = waterRenderer;
                renderers.GetArrayElementAtIndex(1).objectReferenceValue = surfaceRenderer;
                placeableObject.ApplyModifiedPropertiesWithoutUndo();

                WaterZone waterZone = root.AddComponent<WaterZone>();
                SerializedObject waterObject = new SerializedObject(waterZone);
                waterObject.FindProperty("size").vector2Value = new Vector2(3f, 2f);
                waterObject.FindProperty("waterRenderer").objectReferenceValue = waterRenderer;
                waterObject.FindProperty("surfaceLine").objectReferenceValue = surfaceRenderer.transform;
                waterObject.FindProperty("splashEffect").objectReferenceValue = splash;
                waterObject.FindProperty("bubbleRiseEffect").objectReferenceValue = bubbleRise;
                waterObject.FindProperty("bubblePopEffect").objectReferenceValue = bubblePop;
                waterObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Recovered missing WaterZone prefab: " + PrefabPath);
        }

        private static SpriteRenderer CreateWaterLayer(
            Transform parent,
            string name,
            Sprite sprite,
            Material material,
            Vector3 localPosition,
            Vector3 worldSize,
            int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            Vector2 spriteSize = sprite.bounds.size;
            child.transform.localScale = new Vector3(
                spriteSize.x > Mathf.Epsilon ? worldSize.x / spriteSize.x : worldSize.x,
                spriteSize.y > Mathf.Epsilon ? worldSize.y / spriteSize.y : worldSize.y,
                worldSize.z);
            return renderer;
        }

        private static ParticleSystem CreateSplashEffect(Transform parent, Material material)
        {
            GameObject child = new GameObject("SplashEffect");
            child.transform.SetParent(parent, false);
            child.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem effect = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = effect.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.startColor = new Color(0.55f, 0.88f, 1f, 0.9f);
            main.gravityModifier = 0.55f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            ParticleSystem.ShapeModule shape = effect.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.08f;

            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 6;
            return effect;
        }

        private static ParticleSystem CreateBubbleEffect(
            Transform parent,
            string name,
            bool expands,
            Material material,
            int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            ParticleSystem effect = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = effect.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.maxParticles = 128;
            ParticleSystem.EmissionModule emission = effect.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = false;
            ParticleSystem.SizeOverLifetimeModule size = effect.sizeOverLifetime;
            size.enabled = expands;
            if (expands)
            {
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.55f, 1f, 1.45f));
            }

            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            return effect;
        }
    }
}
