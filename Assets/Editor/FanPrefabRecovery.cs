using System;
using System.Linq;
using System.Reflection;
using BallGame.Placement;
using BallGame.Tools;
using BallGame.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BallGame.Editor
{
    public static class FanPrefabRecovery
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/Fan.prefab";
        private const string ExpectedPrefabGuid = "a39a93d4b09666f4a8bf7c4f988fb678";
        private const string SpriteSheetPath = "Assets/Fan_BladeSpin_6Frame_SpriteSheet.png";
        private const string AnimationPath = "Assets/Resources/Animations/Fan/Fan Animation.anim";
        private const string ControllerPath = "Assets/Resources/Animations/Fan/Fan Animator Controller.controller";
        private const string ToolDefinitionPath = "Assets/Tools/Fan.asset";
        private const long OriginalRootGameObjectId = 4751090511571632332L;
        private const long OriginalRootTransformId = 4172742011393732167L;
        private const long OriginalFanZoneId = 9218832499934462027L;

        [MenuItem("Ball Game/Effects/Recover Missing Fan Prefab")]
        public static void RecoverMissingFanPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                Debug.Log("Fan prefab already exists; recovery skipped: " + PrefabPath);
                return;
            }

            if (AssetDatabase.AssetPathToGUID(PrefabPath) != ExpectedPrefabGuid)
            {
                AssetDatabase.ImportAsset(PrefabPath + ".meta", ImportAssetOptions.ForceSynchronousImport);
            }

            Sprite[] frames = ImportFanFrames();
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (clip == null || controller == null)
            {
                throw new InvalidOperationException("Fan animation assets are missing.");
            }

            RestoreAnimationFrames(clip, frames);

            GameObject root = new GameObject("Fan");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.color = Color.white;
                renderer.sortingOrder = 5;

                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.25f, 1.25f);
                collider.isTrigger = false;

                PlaceableTool placeable = root.AddComponent<PlaceableTool>();
                SerializedObject placeableObject = new SerializedObject(placeable);
                SerializedProperty renderers = placeableObject.FindProperty("renderers");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                placeableObject.ApplyModifiedPropertiesWithoutUndo();

                FanZone fanZone = root.AddComponent<FanZone>();
                root.AddComponent<ForceDirectionIndicator>();
                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.enabled = false;

                Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/FireworkParticle.mat");
                ParticleSystem upperFlow = CreateAirFlow(root.transform, "AirFlowEffect", 0.34f, particleMaterial, 7);
                ParticleSystem middleFlow = CreateAirFlow(root.transform, "AirFlowEffect1", 0f, particleMaterial, 8);
                ParticleSystem lowerFlow = CreateAirFlow(root.transform, "AirFlowEffect2", -0.34f, particleMaterial, 7);

                SerializedObject fanObject = new SerializedObject(fanZone);
                fanObject.FindProperty("range").floatValue = 2.5f;
                fanObject.FindProperty("width").floatValue = 1.6f;
                fanObject.FindProperty("force").floatValue = 12f;
                fanObject.FindProperty("animator").objectReferenceValue = animator;
                fanObject.FindProperty("animatedRenderer").objectReferenceValue = renderer;
                fanObject.FindProperty("airFlowEffect").objectReferenceValue = upperFlow;
                fanObject.FindProperty("airFlowEffect1").objectReferenceValue = middleFlow;
                fanObject.FindProperty("airFlowEffect2").objectReferenceValue = lowerFlow;
                fanObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
            TryRestoreOriginalLocalIds();
            RestoreToolDefinitionReference(frames[0]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string actualGuid = AssetDatabase.AssetPathToGUID(PrefabPath);
            if (actualGuid != ExpectedPrefabGuid)
            {
                throw new InvalidOperationException("Recovered Fan prefab has the wrong GUID: " + actualGuid);
            }

            Debug.Log("Recovered missing Fan prefab: " + PrefabPath);
        }

        [MenuItem("Ball Game/Effects/Repair Fan Particle Velocity Modes")]
        public static void RepairFanParticleVelocityModes()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                string[] effectNames = { "AirFlowEffect", "AirFlowEffect1", "AirFlowEffect2" };
                foreach (string effectName in effectNames)
                {
                    ParticleSystem effect = prefabRoot.transform.Find(effectName)?.GetComponent<ParticleSystem>();
                    if (effect == null)
                    {
                        throw new InvalidOperationException("Fan prefab is missing " + effectName + ".");
                    }

                    SetAirFlowVelocityCurves(effect);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Repaired Fan particle velocity curve modes: " + PrefabPath);
        }

        private static Sprite[] ImportFanFrames()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpriteSheetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Recovered fan sprite sheet was not found: " + SpriteSheetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            SpriteMetaData[] slices = new SpriteMetaData[6];
            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = new SpriteMetaData
                {
                    name = "Fan_BladeSpin_" + i.ToString("00"),
                    rect = new Rect(i * 148f, 0f, 148f, 148f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

#pragma warning disable CS0618
            importer.spritesheet = slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length != 6)
            {
                throw new InvalidOperationException("Expected 6 recovered fan frames, found " + frames.Length + ".");
            }

            return frames;
        }

        private static void RestoreAnimationFrames(AnimationClip clip, Sprite[] frames)
        {
            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                propertyName = "m_Sprite",
                type = typeof(SpriteRenderer)
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / 60f,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static ParticleSystem CreateAirFlow(Transform parent, string name, float localY, Material material, int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = new Vector3(0.7f, localY, 0f);

            ParticleSystem effect = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = effect.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.55f, 0.95f, 1f, 0.7f),
                new Color(0.2f, 0.7f, 1f, 0.25f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 48;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 14f;

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.05f, 0.18f, 0.01f);

            SetAirFlowVelocityCurves(effect);

            ParticleSystem.ColorOverLifetimeModule color = effect.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            ParticleSystemRenderer particleRenderer = child.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.lengthScale = 0.5f;
            particleRenderer.velocityScale = 0.08f;
            particleRenderer.sortingOrder = sortingOrder;
            return effect;
        }

        private static void SetAirFlowVelocityCurves(ParticleSystem effect)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = effect.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(2.85f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }

        private static void TryRestoreOriginalLocalIds()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            MethodInfo method = typeof(Unsupported).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(candidate => candidate.Name == "SetLocalIdentifierInFileForPersistentObject" && candidate.GetParameters().Length == 2);
            if (prefab == null || method == null)
            {
                Debug.LogWarning("Could not restore original Fan local file IDs; scene overrides may require migration.");
                return;
            }

            SetLocalId(method, prefab, OriginalRootGameObjectId);
            SetLocalId(method, prefab.transform, OriginalRootTransformId);
            SetLocalId(method, prefab.GetComponent<FanZone>(), OriginalFanZoneId);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(new[] { PrefabPath }, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void SetLocalId(MethodInfo method, UnityEngine.Object target, long localId)
        {
            if (target == null)
            {
                throw new InvalidOperationException("Cannot restore a local ID for a missing Fan component.");
            }

            ParameterInfo idParameter = method.GetParameters()[1];
            object convertedId = Convert.ChangeType(localId, idParameter.ParameterType);
            method.Invoke(null, new[] { (object)target, convertedId });
        }

        private static void RestoreToolDefinitionReference(Sprite icon)
        {
            UnityEngine.Object toolDefinition = AssetDatabase.LoadMainAssetAtPath(ToolDefinitionPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (toolDefinition == null || prefab == null)
            {
                throw new InvalidOperationException("Fan ToolDefinition could not be restored.");
            }

            SerializedObject serializedTool = new SerializedObject(toolDefinition);
            serializedTool.FindProperty("prefab").objectReferenceValue = prefab;
            if (serializedTool.FindProperty("icon").objectReferenceValue == null)
            {
                serializedTool.FindProperty("icon").objectReferenceValue = icon;
            }
            serializedTool.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(toolDefinition);
        }
    }
}
