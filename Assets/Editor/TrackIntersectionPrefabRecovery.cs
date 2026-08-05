using System;
using System.Linq;
using BallGame.Core;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class TrackIntersectionPrefabRecovery
    {
        private const string SourcePrefabPath = "Assets/Resources/Prefabs/CurveObstacle.prefab";
        private const string OutputPrefabPath = "Assets/Resources/Prefabs/TrackIntersection.prefab";
        private const string UpperMaterialPath = "Assets/Resources/Materials/RollerCoasterTrackUpper.mat";
        private const string LowerMaterialPath = "Assets/Resources/Materials/RollerCoasterTrackLower.mat";

        [MenuItem("Ball Game/Recovery/Create Track Intersection Prefab")]
        public static void CreateTrackIntersectionPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath) != null)
            {
                Debug.LogWarning($"Track intersection prefab already exists; nothing was overwritten: {OutputPrefabPath}");
                return;
            }

            GameObject curvePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (curvePrefab == null)
            {
                throw new InvalidOperationException($"CurveObstacle prefab was not found: {SourcePrefabPath}");
            }

            GameObject root = new GameObject("TrackIntersection");
            try
            {
                GameObject upper = CreateCurveInstance(curvePrefab, root.transform, "Upper", 0f, 21);
                GameObject lower = CreateCurveInstance(curvePrefab, root.transform, "Lower", 90f, 0);

                ConfigureVisibleRendererOrders(root, upper, lower);

                Collider2D[] upperColliders = GetInitiallyEnabledColliders(upper);
                Collider2D[] lowerColliders = GetInitiallyEnabledColliders(lower);
                if (upperColliders.Length == 0 || lowerColliders.Length == 0)
                {
                    throw new InvalidOperationException("CurveObstacle must contain at least one enabled Collider2D.");
                }

                TrackLayerSwitcher2D switcher = root.AddComponent<TrackLayerSwitcher2D>();
                SerializedObject switcherObject = new SerializedObject(switcher);
                AssignColliderArray(switcherObject.FindProperty("upperColliders"), upperColliders);
                AssignColliderArray(switcherObject.FindProperty("lowerColliders"), lowerColliders);
                switcherObject.FindProperty("defaultUseUpper").boolValue = true;
                switcherObject.ApplyModifiedPropertiesWithoutUndo();

                CreateTrigger(root.transform, switcher, "Upper Entry A", new Vector2(3.05f, -1.11f), 0f, true);
                CreateTrigger(root.transform, switcher, "Upper Entry B", new Vector2(-3.05f, 1.11f), 0f, true);
                CreateTrigger(root.transform, switcher, "Lower Entry A", new Vector2(1.11f, 3.05f), 90f, false);
                CreateTrigger(root.transform, switcher, "Lower Entry B", new Vector2(-1.11f, -3.05f), 90f, false);

                PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
                Debug.Log($"Created track intersection prefab: {OutputPrefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [MenuItem("Ball Game/Recovery/Add Visible Orders To Track Intersection")]
        public static void AddVisibleOrderFieldsToExistingPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null)
            {
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(OutputPrefabPath);
            try
            {
                Transform upper = FindTrack(contents.transform, "Upper Sorting Group", "Upper");
                Transform lower = FindTrack(contents.transform, "Lower Sorting Group", "Lower");
                if (upper == null || lower == null)
                {
                    throw new InvalidOperationException("TrackIntersection must contain Upper and Lower children.");
                }

                ConfigureVisibleRendererOrders(contents, upper.gameObject, lower.gameObject);
                PrefabUtility.SaveAsPrefabAsset(contents, OutputPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Added visible renderer order fields to: {OutputPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureVisibleRendererOrders(GameObject root, GameObject upper, GameObject lower)
        {
            Renderer[] upperRenderers = upper.GetComponentsInChildren<Renderer>(true);
            Renderer[] lowerRenderers = lower.GetComponentsInChildren<Renderer>(true);
            AssignLayeredMaterial(upperRenderers, UpperMaterialPath, 3021);
            AssignLayeredMaterial(lowerRenderers, LowerMaterialPath, 3000);
            TrackIntersectionRendererOrder2D orderController =
                root.GetComponent<TrackIntersectionRendererOrder2D>() ??
                root.AddComponent<TrackIntersectionRendererOrder2D>();
            SerializedObject orderObject = new SerializedObject(orderController);
            AssignRendererArray(orderObject.FindProperty("upperRenderers"), upperRenderers);
            AssignRendererArray(orderObject.FindProperty("lowerRenderers"), lowerRenderers);
            orderObject.FindProperty("upperOrder").intValue = 21;
            orderObject.FindProperty("lowerOrder").intValue = 0;
            orderObject.ApplyModifiedPropertiesWithoutUndo();
            orderController.Apply();
        }

        private static Transform FindTrack(Transform root, string wrapperName, string trackName)
        {
            Transform wrapper = root.Find(wrapperName);
            return wrapper != null ? wrapper.Find(trackName) : root.Find(trackName);
        }

        private static void AssignLayeredMaterial(Renderer[] renderers, string materialPath, int renderQueue)
        {
            if (renderers.Length == 0 || renderers[0].sharedMaterial == null)
            {
                throw new InvalidOperationException("Track renderer or source material is missing.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(renderers[0].sharedMaterial)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.renderQueue = renderQueue;
            EditorUtility.SetDirty(material);
            foreach (Renderer trackRenderer in renderers)
            {
                trackRenderer.sharedMaterial = material;
            }
        }

        private static GameObject CreateCurveInstance(
            GameObject source,
            Transform parent,
            string name,
            float rotationDegrees,
            int sortingOrder)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = new Vector3(1.385f, 1.95f, 0f);
            instance.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            instance.transform.localScale = Vector3.one;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingOrder = sortingOrder;
            }

            return instance;
        }

        private static Collider2D[] GetInitiallyEnabledColliders(GameObject instance)
        {
            return instance.GetComponentsInChildren<Collider2D>(true)
                .Where(collider => collider.enabled && !collider.isTrigger)
                .ToArray();
        }

        private static void AssignColliderArray(SerializedProperty property, Collider2D[] colliders)
        {
            property.arraySize = colliders.Length;
            for (int i = 0; i < colliders.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
            }
        }

        private static void AssignRendererArray(SerializedProperty property, Renderer[] renderers)
        {
            property.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
        }

        private static void CreateTrigger(
            Transform parent,
            TrackLayerSwitcher2D switcher,
            string name,
            Vector2 position,
            float rotationDegrees,
            bool useUpper)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.SetParent(parent, false);
            triggerObject.transform.localPosition = position;
            triggerObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

            BoxCollider2D collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.25f, 1.25f);

            TrackLayerSwitchTrigger2D trigger = triggerObject.AddComponent<TrackLayerSwitchTrigger2D>();
            SerializedObject triggerObjectData = new SerializedObject(trigger);
            triggerObjectData.FindProperty("switcher").objectReferenceValue = switcher;
            triggerObjectData.FindProperty("useUpper").boolValue = useUpper;
            triggerObjectData.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
