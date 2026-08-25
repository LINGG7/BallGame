using BallGame.Core;
using UnityEditor;
using UnityEngine;

public static class LevelTestDoorPrefabUtility
{
    private const string PrefabPath = "Assets/Resources/Prefabs/VisualTo3DDoor.prefab";
    private const string MaterialPath = "Assets/Resources/Materials/LevelTestDoor.mat";

    [MenuItem("Ball Game/Upgrade Level Test Door Prefab")]
    public static void UpgradePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError("Level test door prefab was not found: " + PrefabPath);
            return;
        }

        try
        {
            Transform visualTransform = root.transform.Find("DoorVisual");
            GameObject visual;
            if (visualTransform == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "DoorVisual";
                visual.transform.SetParent(root.transform, false);
            }
            else
            {
                visual = visualTransform.gameObject;
            }

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.25f, 5f, 0.4f);

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial();
            // Keep the door in front of the ball's 2D SpriteRenderer.
            renderer.sortingOrder = 100;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Level test door visual was saved to " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Material GetOrCreateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;

        material = new Material(Shader.Find("Standard"))
        {
            name = "LevelTestDoor",
            color = new Color(0.15f, 0.65f, 0.95f)
        };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }
}
