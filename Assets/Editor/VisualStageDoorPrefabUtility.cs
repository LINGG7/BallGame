using BallGame.Core;
using UnityEditor;
using UnityEngine;

public static class VisualStageDoorPrefabUtility
{
    // Doors must draw over the ball's 2D SpriteRenderer (sorting order 20).
    private const int DoorSortingOrder = 100;
    private const string To3DPrefabPath = "Assets/Resources/Prefabs/VisualTo3DDoor.prefab";
    private const string To2DPrefabPath = "Assets/Resources/Prefabs/VisualTo2DDoor.prefab";
    private const string To3DMaterialPath = "Assets/Resources/Materials/VisualTo3DDoor.mat";
    private const string To2DMaterialPath = "Assets/Resources/Materials/VisualTo2DDoor.mat";

    [MenuItem("Ball Game/Build Visual Stage Door Prefabs")]
    public static void EnsurePrefabs()
    {
        ConfigureDoor(To3DPrefabPath, "VisualTo3DDoor", VisualStageTarget.To3D);

        GameObject to2DPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(To2DPrefabPath);
        if (to2DPrefab == null)
        {
            GameObject sourceRoot = PrefabUtility.LoadPrefabContents(To3DPrefabPath);
            if (sourceRoot == null)
            {
                Debug.LogError("VisualTo3DDoor prefab was not found: " + To3DPrefabPath);
                return;
            }

            try
            {
                sourceRoot.name = "VisualTo2DDoor";
                SetTarget(sourceRoot, VisualStageTarget.To2D);
                PrefabUtility.SaveAsPrefabAsset(sourceRoot, To2DPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sourceRoot);
            }
        }

        ConfigureDoor(To2DPrefabPath, "VisualTo2DDoor", VisualStageTarget.To2D);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureDoor(string path, string objectName, VisualStageTarget target)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) return;
        try
        {
            root.name = objectName;
            SetTarget(root, target);
            AssignMaterial(root, target == VisualStageTarget.To3D ? To3DMaterialPath : To2DMaterialPath);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AssignMaterial(GameObject root, string materialPath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            throw new System.InvalidOperationException("Door material was not found: " + materialPath);
        }

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
            renderers[i].sortingOrder = DoorSortingOrder;
            EditorUtility.SetDirty(renderers[i]);
        }
    }

    private static void SetTarget(GameObject root, VisualStageTarget target)
    {
        LevelTestDoorTrigger trigger = root.GetComponent<LevelTestDoorTrigger>();
        if (trigger == null)
        {
            throw new System.InvalidOperationException(root.name + " is missing LevelTestDoorTrigger.");
        }

        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("target").enumValueIndex = (int)target;
        serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
    }
}
