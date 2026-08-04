using BallGame.Effects;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class FireworkPrefabBuilder
    {
        private const string MaterialPath = "Assets/Resources/Materials/FireworkParticle.mat";
        private const string PrefabPath = "Assets/Resources/Prefabs/Firework.prefab";
        private const string RecoveredExplosionPrefabPath = "Assets/Resources/Prefabs/FireworkBoom.prefab";
        private const string LauncherPrefabPath = "Assets/Resources/Prefabs/FireworkLauncher.prefab";
        private const float DefaultLiftDuration = 0.8f;
        private const float DefaultLiftHeight = 1.35f;

        [MenuItem("Ball Game/Effects/Rebuild Firework Prefab")]
        public static void RebuildFireworkPrefab()
        {
            Material particleMaterial = GetOrCreateParticleMaterial();
            bool hasSavedLiftShellColor = TryGetSavedLiftShellColor(out ParticleSystem.MinMaxGradient savedLiftShellColor);

            GameObject root = new GameObject("Firework");
            FireworkEffect fireworkEffect = root.AddComponent<FireworkEffect>();

            ParticleSystem liftShell = CreateLiftShell(root.transform, particleMaterial);
            if (hasSavedLiftShellColor)
            {
                ParticleSystem.MainModule liftShellMain = liftShell.main;
                liftShellMain.startColor = savedLiftShellColor;
            }

            ParticleSystem liftTrail = CreateLiftTrail(root.transform, particleMaterial);
            Transform explosionRoot = new GameObject("Explosion").transform;
            explosionRoot.SetParent(root.transform, false);

            ParticleSystem coreFlash = CreateCoreFlash(explosionRoot, particleMaterial);
            Transform debrisRoot = new GameObject("ColorDebris").transform;
            debrisRoot.SetParent(explosionRoot, false);

            Color[] debrisColors =
            {
                new Color(1f, 0.18f, 0.12f, 1f),
                new Color(1f, 0.85f, 0.08f, 1f),
                new Color(0.25f, 1f, 0.18f, 1f),
                new Color(0.12f, 0.72f, 1f, 1f),
                new Color(0.28f, 0.22f, 1f, 1f),
                new Color(1f, 0.16f, 0.95f, 1f)
            };

            ParticleSystem[] debrisSystems = new ParticleSystem[debrisColors.Length];
            for (int i = 0; i < debrisColors.Length; i++)
            {
                debrisSystems[i] = CreateDebrisEmitter(debrisRoot, particleMaterial, debrisColors[i], i);
            }

            ParticleSystem sparkleFades = CreateSparkleFades(explosionRoot, particleMaterial);
            AssignParticleSystems(fireworkEffect, liftShell, liftTrail, coreFlash, debrisSystems, sparkleFades);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            RebuildFireworkLauncherPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rebuilt firework prefab: " + PrefabPath);
        }

        [MenuItem("Ball Game/Effects/Recover Firework Explosion")]
        public static void RecoverFireworkExplosion()
        {
            GameObject recoveredExplosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecoveredExplosionPrefabPath);
            if (recoveredExplosionPrefab == null)
            {
                throw new System.IO.FileNotFoundException("Recovered explosion prefab was not found.", RecoveredExplosionPrefabPath);
            }

            Transform recoveredColorDebris = recoveredExplosionPrefab.transform.Find("ColorDebris");
            Transform recoveredSparkleFades = recoveredExplosionPrefab.transform.Find("SparkleFades");
            if (recoveredColorDebris == null || recoveredSparkleFades == null)
            {
                throw new System.InvalidOperationException("FireworkBoom.prefab does not contain the complete recovered explosion hierarchy.");
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform explosionRoot = prefabRoot.transform.Find("Explosion");
                ParticleSystem coreFlash = explosionRoot != null
                    ? explosionRoot.Find("CoreFlash")?.GetComponent<ParticleSystem>()
                    : null;
                if (explosionRoot == null || coreFlash == null)
                {
                    throw new System.InvalidOperationException("Firework.prefab is missing Explosion/CoreFlash.");
                }

                ReplaceRecoveredChild(explosionRoot, recoveredColorDebris);
                ReplaceRecoveredChild(explosionRoot, recoveredSparkleFades);

                Transform colorDebris = explosionRoot.Find("ColorDebris");
                ParticleSystem sparkleFades = explosionRoot.Find("SparkleFades")?.GetComponent<ParticleSystem>();
                ParticleSystem[] debrisSystems = new ParticleSystem[6];
                for (int i = 0; i < debrisSystems.Length; i++)
                {
                    debrisSystems[i] = colorDebris?.Find("Debris_" + i.ToString("00"))?.GetComponent<ParticleSystem>();
                    if (debrisSystems[i] == null)
                    {
                        throw new System.InvalidOperationException("Recovered explosion is missing Debris_" + i.ToString("00") + ".");
                    }
                }

                if (sparkleFades == null)
                {
                    throw new System.InvalidOperationException("Recovered explosion is missing SparkleFades ParticleSystem.");
                }

                FireworkEffect fireworkEffect = prefabRoot.GetComponent<FireworkEffect>();
                if (fireworkEffect == null)
                {
                    throw new System.InvalidOperationException("Firework.prefab is missing FireworkEffect.");
                }

                SerializedObject serializedObject = new SerializedObject(fireworkEffect);
                SerializedProperty systems = serializedObject.FindProperty("explosionSystems");
                systems.arraySize = 8;
                systems.GetArrayElementAtIndex(0).objectReferenceValue = coreFlash;
                for (int i = 0; i < debrisSystems.Length; i++)
                {
                    systems.GetArrayElementAtIndex(i + 1).objectReferenceValue = debrisSystems[i];
                }

                systems.GetArrayElementAtIndex(7).objectReferenceValue = sparkleFades;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Recovered Firework Explosion from: " + RecoveredExplosionPrefabPath);
        }

        private static void ReplaceRecoveredChild(Transform destinationParent, Transform recoveredChild)
        {
            Transform existing = destinationParent.Find(recoveredChild.name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject recoveredCopy = UnityEngine.Object.Instantiate(recoveredChild.gameObject, destinationParent);
            recoveredCopy.name = recoveredChild.name;
            recoveredCopy.transform.SetSiblingIndex(destinationParent.childCount - 1);
        }

        [MenuItem("Ball Game/Effects/Rebuild Firework Launcher Prefab")]
        public static void RebuildFireworkLauncherPrefab()
        {
            FireworkEffect fireworkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)?.GetComponent<FireworkEffect>();
            if (fireworkPrefab == null)
            {
                Debug.LogWarning("Cannot rebuild FireworkLauncher before Firework.prefab exists.");
                return;
            }

            GameObject root = new GameObject("FireworkLauncher");
            FireworkLauncher launcher = root.AddComponent<FireworkLauncher>();
            SerializedObject serializedObject = new SerializedObject(launcher);
            serializedObject.FindProperty("fireworkPrefab").objectReferenceValue = fireworkPrefab;
            serializedObject.FindProperty("playOnEnable").boolValue = false;
            serializedObject.FindProperty("horizontalRange").floatValue = 8f;
            serializedObject.FindProperty("minimumExplosionHeight").floatValue = 2f;
            serializedObject.FindProperty("maximumExplosionHeight").floatValue = 5f;
            serializedObject.FindProperty("fireworksPerRound").intValue = 8;
            serializedObject.FindProperty("launchInterval").floatValue = 0.35f;
            serializedObject.FindProperty("roundInterval").floatValue = 1.5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, LauncherPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt firework launcher prefab: " + LauncherPrefabPath);
        }

        private static Material GetOrCreateParticleMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Additive");
            }

            material = new Material(shader)
            {
                name = "FireworkParticle",
                renderQueue = 3000
            };

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 2f);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static ParticleSystem CreateCoreFlash(Transform parent, Material material)
        {
            ParticleSystem effect = CreateParticleSystem("CoreFlash", parent, material, 30);
            ParticleSystem.MainModule main = effect.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.94f, 0.55f, 0.8f), new Color(1f, 0.9f, 0.5f, 0.8f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            EnableSizeAndAlphaFade(effect, 0.95f, 0f);
            effect.gameObject.SetActive(true);
            return effect;
        }

        private static ParticleSystem CreateLiftTrail(Transform parent, Material material)
        {
            ParticleSystem effect = CreateParticleSystem("LiftTrail", parent, material, 10);
            effect.transform.localPosition = new Vector3(0f, -DefaultLiftHeight, 0f);

            ParticleSystem.MainModule main = effect.main;
            main.duration = 0.72f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.22f, 1f), new Color(1f, 0.95f, 0.65f, 1f));
            main.gravityModifier = -0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 50f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.035f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.VelocityOverLifetimeModule velocity = effect.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            velocity.y = new ParticleSystem.MinMaxCurve(1.2f, 1.9f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.TrailModule trails = effect.trails;
            trails.enabled = true;
            trails.lifetime = 0.22f;
            trails.ratio = 0.85f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.38f);

            EnableSizeAndAlphaFade(effect, 1f, 0f);
            effect.gameObject.SetActive(true);
            return effect;
        }

        private static ParticleSystem CreateLiftShell(Transform parent, Material material)
        {
            ParticleSystem effect = CreateParticleSystem("LiftShell", parent, material, 18);
            effect.transform.localPosition = new Vector3(0f, -DefaultLiftHeight, 0f);

            ParticleSystem.MainModule main = effect.main;
            float effectiveGravity = 2f * DefaultLiftHeight / (DefaultLiftDuration * DefaultLiftDuration);
            float gravityMagnitude = Mathf.Max(Mathf.Abs(Physics.gravity.y), 0.0001f);
            main.duration = 0.12f;
            main.loop = false;
            main.startLifetime = DefaultLiftDuration;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.26f);
            main.gravityModifier = effectiveGravity / gravityMagnitude;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0f;
            shape.arc = 360f;

            ParticleSystem.VelocityOverLifetimeModule velocity = effect.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = 0f;
            float upwardVelocity = 2f * DefaultLiftHeight / DefaultLiftDuration;
            velocity.y = upwardVelocity;
            velocity.z = 0f;

            ParticleSystem.TrailModule trails = effect.trails;
            trails.enabled = false;
            trails.lifetime = 0.08f;
            trails.ratio = 1f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.22f);

            EnableSizeAndAlphaFade(effect, 1f, 0.35f);
            effect.gameObject.SetActive(true);
            return effect;
        }

        private static ParticleSystem CreateDebrisEmitter(Transform parent, Material material, Color color, int index)
        {
            ParticleSystem effect = CreateParticleSystem("Debris_" + index.ToString("00"), parent, material, 20 + index);
            ParticleSystem.MainModule main = effect.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startDelay = index * 0.012f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = color;
            main.gravityModifier = 0.42f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.06f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            ParticleSystem.VelocityOverLifetimeModule velocity = effect.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);
            velocity.radial = new ParticleSystem.MinMaxCurve(0.1f, 0.45f);

            EnableSizeAndAlphaFade(effect, 1f, 0f);
            effect.gameObject.SetActive(true);
            return effect;
        }

        private static ParticleSystem CreateSparkleFades(Transform parent, Material material)
        {
            ParticleSystem effect = CreateParticleSystem("SparkleFades", parent, material, 35);
            ParticleSystem.MainModule main = effect.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startDelay = 0.08f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 3.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.35f, 1f), new Color(0.45f, 0.95f, 1f, 1f));
            main.gravityModifier = 0.68f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 42) });

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.04f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            ParticleSystem.TrailModule trails = effect.trails;
            trails.enabled = true;
            trails.lifetime = 0.18f;
            trails.ratio = 0.6f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.45f);

            EnableSizeAndAlphaFade(effect, 0.85f, 0f);
            effect.gameObject.SetActive(true);
            return effect;
        }

        private static ParticleSystem CreateParticleSystem(string name, Transform parent, Material material, int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.SetActive(false);
            child.transform.SetParent(parent, false);

            ParticleSystem effect = child.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.trailMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = sortingOrder;
            return effect;
        }

        private static void EnableSizeAndAlphaFade(ParticleSystem effect, float startSize, float endSize)
        {
            ParticleSystem.SizeOverLifetimeModule size = effect.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, startSize),
                new Keyframe(0.68f, startSize * 0.72f),
                new Keyframe(1f, endSize));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystem.ColorOverLifetimeModule color = effect.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.62f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;
        }

        private static void AssignParticleSystems(FireworkEffect effect, ParticleSystem liftShell, ParticleSystem liftTrail, ParticleSystem coreFlash, ParticleSystem[] debrisSystems, ParticleSystem sparkleFades)
        {
            SerializedObject serializedObject = new SerializedObject(effect);
            serializedObject.FindProperty("playOnAwake").boolValue = true;
            serializedObject.FindProperty("destroyAfterFinished").boolValue = true;
            serializedObject.FindProperty("playLiftBeforeExplosion").boolValue = true;
            serializedObject.FindProperty("liftDuration").floatValue = DefaultLiftDuration;
            serializedObject.FindProperty("liftHeight").floatValue = DefaultLiftHeight;
            serializedObject.FindProperty("liftSystem").objectReferenceValue = null;

            SerializedProperty liftSystems = serializedObject.FindProperty("liftSystems");
            liftSystems.arraySize = 2;
            liftSystems.GetArrayElementAtIndex(0).objectReferenceValue = liftShell;
            liftSystems.GetArrayElementAtIndex(1).objectReferenceValue = liftTrail;

            SerializedProperty systems = serializedObject.FindProperty("explosionSystems");
            systems.arraySize = debrisSystems.Length + 2;
            systems.GetArrayElementAtIndex(0).objectReferenceValue = coreFlash;
            for (int i = 0; i < debrisSystems.Length; i++)
            {
                systems.GetArrayElementAtIndex(i + 1).objectReferenceValue = debrisSystems[i];
            }

            systems.GetArrayElementAtIndex(systems.arraySize - 1).objectReferenceValue = sparkleFades;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool NeedsFireworkRebuild()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                return true;
            }

            Transform liftTrail = prefab.transform.Find("LiftTrail");

            return prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0
                || prefab.transform.Find("LiftShell") == null
                || liftTrail == null
                || prefab.transform.Find("Explosion") == null
                || !Mathf.Approximately(liftTrail.localPosition.y, -DefaultLiftHeight)
                || !IsLiftShellCurrent(prefab.transform.Find("LiftShell").GetComponent<ParticleSystem>());
        }

        private static bool NeedsLauncherRebuild()
        {
            GameObject launcherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LauncherPrefabPath);
            return launcherPrefab == null || launcherPrefab.GetComponent<FireworkLauncher>() == null;
        }

        private static bool TryGetSavedLiftShellColor(out ParticleSystem.MinMaxGradient color)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            ParticleSystem liftShell = prefab != null
                ? prefab.transform.Find("LiftShell")?.GetComponent<ParticleSystem>()
                : null;
            if (liftShell == null)
            {
                color = default;
                return false;
            }

            color = liftShell.main.startColor;
            return true;
        }

        private static bool IsLiftShellCurrent(ParticleSystem liftShell)
        {
            if (liftShell == null)
            {
                return false;
            }

            ParticleSystem.MainModule main = liftShell.main;
            ParticleSystem.ShapeModule shape = liftShell.shape;
            ParticleSystem.VelocityOverLifetimeModule velocity = liftShell.velocityOverLifetime;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[liftShell.emission.burstCount];
            liftShell.emission.GetBursts(bursts);

            return Mathf.Approximately(main.startLifetime.constant, DefaultLiftDuration)
                && Mathf.Approximately(main.startSpeed.constant, 0f)
                && Mathf.Approximately(shape.radius, 0f)
                && Mathf.Approximately(velocity.x.constant, 0f)
                && Mathf.Approximately(velocity.z.constant, 0f)
                && bursts.Length == 1
                && bursts[0].maxCount == 1;
        }
    }
}
