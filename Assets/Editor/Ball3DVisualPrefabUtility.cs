using BallGame.Core;
using UnityEditor;
using UnityEngine;

public static class Ball3DVisualPrefabUtility
{
    private const string BallPrefabPath = "Assets/Resources/Prefabs/Ball.prefab";
    private const string FallbackMaterialPath = "Assets/Resources/Materials/Ball3DFallback.mat";

    [MenuItem("Ball Game/Upgrade Ball 3D Visual Prefab")]
    public static void UpgradePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BallPrefabPath);
        if (root == null)
        {
            Debug.LogError("Ball prefab was not found: " + BallPrefabPath);
            return;
        }

        try
        {
            SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("Ball prefab root has no SpriteRenderer.", root);
                return;
            }

            BallVisual3DTransition transition = root.GetComponent<BallVisual3DTransition>();
            if (transition == null)
            {
                transition = root.AddComponent<BallVisual3DTransition>();
            }

            Transform visualTransform = root.transform.Find("Ball3DVisual");
            GameObject visualObject;
            if (visualTransform == null)
            {
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visualObject.name = "Ball3DVisual";
                visualObject.transform.SetParent(root.transform, false);
                visualObject.transform.localPosition = Vector3.zero;
                visualObject.transform.localRotation = Quaternion.identity;
                visualObject.transform.localScale = Vector3.one * 0.9f;
            }
            else
            {
                visualObject = visualTransform.gameObject;
            }

            Collider collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer meshRenderer = visualObject.GetComponent<MeshRenderer>();
            Material fallbackMaterial = GetOrCreateFallbackMaterial();
            meshRenderer.sharedMaterial = fallbackMaterial;
            visualObject.SetActive(false);

            SerializedObject serializedTransition = new SerializedObject(transition);
            serializedTransition.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serializedTransition.FindProperty("meshRenderer").objectReferenceValue = meshRenderer;
            serializedTransition.FindProperty("sphereObject").objectReferenceValue = visualObject;
            serializedTransition.FindProperty("fallbackMaterial").objectReferenceValue = fallbackMaterial;
            serializedTransition.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BallPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Ball 3D visual was saved to " + BallPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Material GetOrCreateFallbackMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
        Shader shader = Shader.Find("BallGame/SphericalSpriteProjection");
        if (shader == null)
        {
            throw new System.InvalidOperationException("Ball spherical projection shader is missing.");
        }

        if (material != null)
        {
            if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        material = new Material(shader)
        {
            name = "Ball3DFallback"
        };
        AssetDatabase.CreateAsset(material, FallbackMaterialPath);
        return material;
    }
}
