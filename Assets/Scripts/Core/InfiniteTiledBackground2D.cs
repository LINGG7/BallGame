using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class InfiniteTiledBackground2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 tileSizeOverride;
        [SerializeField] private Vector2 tilePadding;
        [SerializeField, Min(1)] private int perspectiveRadiusX = 2;
        [SerializeField, Min(1)] private int perspectiveRadiusY = 2;

        private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        private SpriteRenderer sourceRenderer;

        private void Awake()
        {
            Initialize();
            RefreshTiles();
        }

        private void OnEnable()
        {
            Initialize();
            RefreshTiles();
        }

        private void OnValidate()
        {
            if (!gameObject.scene.IsValid()) return;
            Initialize();
            RefreshTiles();
        }

        private void LateUpdate()
        {
            Initialize();
            RefreshTiles();
        }

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            RefreshTiles();
        }

        private void Initialize()
        {
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponent<SpriteRenderer>();
            }

        }

        private void EnsureTiles(int count)
        {
            for (int i = tiles.Count; i < count; i++)
            {
                Transform child = transform.Find($"Tile {i:00}");
                if (child == null)
                {
                    GameObject tileObject = new GameObject($"Tile {i:00}");
                    tileObject.transform.SetParent(transform, false);
                    child = tileObject.transform;
                }
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
                tiles.Add(renderer);
            }
        }

        private void RefreshTiles()
        {
            if (!gameObject.scene.IsValid()) return;
            if (sourceRenderer == null || sourceRenderer.sprite == null)
            {
                return;
            }

            sourceRenderer.enabled = false;
            Vector2 tileSize = GetTileSize();
            Vector3 cameraPosition = GetCameraPosition();
            GetGridRadius(tileSize, out int radiusX, out int radiusY);
            int requiredCount = (radiusX * 2 + 1) * (radiusY * 2 + 1);
            EnsureTiles(requiredCount);
            float originX = Mathf.Round(cameraPosition.x / tileSize.x) * tileSize.x;
            float originY = Mathf.Round(cameraPosition.y / tileSize.y) * tileSize.y;

            int index = 0;
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    SpriteRenderer tile = tiles[index++];
                    ApplySourceSettings(tile);
                    tile.transform.localScale = Vector3.one;
                    tile.transform.position = new Vector3(originX + x * tileSize.x, originY + y * tileSize.y, transform.position.z);
                }
            }
            for (int i = index; i < tiles.Count; i++)
                if (tiles[i] != null) tiles[i].enabled = false;
        }

        private void GetGridRadius(Vector2 tileSize, out int radiusX, out int radiusY)
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (!Application.isPlaying || cameraToUse == null)
            {
                radiusX = 1;
                radiusY = 1;
                return;
            }
            if (!cameraToUse.orthographic)
            {
                radiusX = Mathf.Max(1, perspectiveRadiusX);
                radiusY = Mathf.Max(1, perspectiveRadiusY);
                return;
            }
            float halfHeight = cameraToUse.orthographicSize;
            float halfWidth = halfHeight * cameraToUse.aspect;
            radiusX = Mathf.Max(1, Mathf.CeilToInt(halfWidth / tileSize.x) + 1);
            radiusY = Mathf.Max(1, Mathf.CeilToInt(halfHeight / tileSize.y) + 1);
        }

        private Vector2 GetTileSize()
        {
            Vector2 spriteSize = sourceRenderer.sprite.bounds.size;
            Vector2 size = new Vector2(
                tileSizeOverride.x > 0f ? tileSizeOverride.x : spriteSize.x,
                tileSizeOverride.y > 0f ? tileSizeOverride.y : spriteSize.y);

            size += tilePadding;
            return new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        }

        private Vector3 GetCameraPosition()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            return cameraToUse != null ? cameraToUse.transform.position : Vector3.zero;
        }

        private void ApplySourceSettings(SpriteRenderer tile)
        {
            tile.sprite = sourceRenderer.sprite;
            tile.color = sourceRenderer.color;
            tile.sharedMaterial = sourceRenderer.sharedMaterial;
            tile.sortingLayerID = sourceRenderer.sortingLayerID;
            tile.sortingOrder = sourceRenderer.sortingOrder;
            tile.flipX = sourceRenderer.flipX;
            tile.flipY = sourceRenderer.flipY;
            tile.drawMode = SpriteDrawMode.Simple;
            tile.maskInteraction = sourceRenderer.maskInteraction;
            tile.enabled = true;
        }
    }
}
