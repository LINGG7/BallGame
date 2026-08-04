using BallGame.Core;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class WorldBackgroundUtility
    {
        private const string WorldBackgroundPath = "Assets/Resources/Prefabs/WorldBackground.prefab";

        [MenuItem("Ball Game/Convert WorldBackground To Infinite Tiled")]
        public static void ConvertWorldBackgroundToInfiniteTiled()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldBackgroundPath);
            if (prefab == null)
            {
                Debug.LogWarning($"WorldBackground prefab not found at {WorldBackgroundPath}.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(WorldBackgroundPath);
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = -100;
            }

            if (root.GetComponent<InfiniteTiledBackground2D>() == null)
            {
                root.AddComponent<InfiniteTiledBackground2D>();
            }

            root.transform.position = new Vector3(0f, 0f, 4f);
            root.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, WorldBackgroundPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("WorldBackground converted to infinite tiled background.");
        }
    }
}
