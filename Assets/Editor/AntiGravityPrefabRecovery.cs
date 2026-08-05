using System;
using BallGame.Placement;
using BallGame.Tools;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class AntiGravityPrefabRecovery
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/AntiGravity.prefab";
        private const string SquareSpritePath = "Assets/Resources/Art/UI/Square.png";
        private const string FlowMaterialPath = "Assets/Resources/Materials/AntiGravityFlow.mat";
        private const string ToolDefinitionPath = "Assets/Tools/AntiGravity.asset";
        [MenuItem("Ball Game/Effects/Recover Missing AntiGravity Prefab")]
        public static void RecoverMissingAntiGravityPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                Debug.Log("AntiGravity prefab already exists; recovery skipped: " + PrefabPath);
                return;
            }

            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            Material flowMaterial = AssetDatabase.LoadAssetAtPath<Material>(FlowMaterialPath);
            UnityEngine.Object toolDefinition = AssetDatabase.LoadMainAssetAtPath(ToolDefinitionPath);
            if (square == null || flowMaterial == null || toolDefinition == null)
            {
                throw new InvalidOperationException("AntiGravity recovery dependencies are missing.");
            }

            GameObject root = new GameObject("AntiGravity");
            try
            {
                BoxCollider2D fieldCollider = root.AddComponent<BoxCollider2D>();
                fieldCollider.isTrigger = true;
                fieldCollider.size = new Vector2(3f, 2f);

                GameObject rangeHint = new GameObject("RangeHint");
                rangeHint.transform.SetParent(root.transform, false);
                SpriteRenderer rangeRenderer = rangeHint.AddComponent<SpriteRenderer>();
                rangeRenderer.sprite = square;
                rangeRenderer.color = Color.white;
                rangeRenderer.sharedMaterial = flowMaterial;
                rangeRenderer.sortingOrder = 2;
                Vector2 spriteSize = square.bounds.size;
                rangeHint.transform.localScale = new Vector3(
                    spriteSize.x > Mathf.Epsilon ? 3f / spriteSize.x : 3f,
                    spriteSize.y > Mathf.Epsilon ? 2f / spriteSize.y : 2f,
                    1f);

                PlaceableTool placeable = root.AddComponent<PlaceableTool>();
                SerializedObject placeableObject = new SerializedObject(placeable);
                placeableObject.FindProperty("definition").objectReferenceValue = toolDefinition;
                SerializedProperty renderers = placeableObject.FindProperty("renderers");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = rangeRenderer;
                placeableObject.ApplyModifiedPropertiesWithoutUndo();

                AntiGravityField field = root.AddComponent<AntiGravityField>();
                SerializedObject fieldObject = new SerializedObject(field);
                fieldObject.FindProperty("size").vector2Value = new Vector2(3f, 2f);
                fieldObject.FindProperty("affectedLayers").intValue = ~0;
                fieldObject.FindProperty("rangeHintRenderer").objectReferenceValue = rangeRenderer;
                fieldObject.FindProperty("showRangeHint").boolValue = true;
                fieldObject.FindProperty("allowOtherToolsInside").boolValue = true;
                fieldObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Recovered missing AntiGravity prefab: " + PrefabPath);
        }
    }
}
