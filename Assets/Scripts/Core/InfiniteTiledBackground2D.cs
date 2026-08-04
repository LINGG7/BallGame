using UnityEngine;

namespace BallGame.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class InfiniteTiledBackground2D : MonoBehaviour
    {
        private const int TileGridSize = 3;
        private const int TileCount = TileGridSize * TileGridSize;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 tileSizeOverride;
        [SerializeField] private Vector2 tilePadding;

        private readonly SpriteRenderer[] tiles = new SpriteRenderer[TileCount];
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

            EnsureTiles();
        }

        private void EnsureTiles()
        {
            for (int i = 0; i < TileCount; i++)
            {
                if (tiles[i] == null)
                {
                    Transform child = transform.Find($"Tile {i:00}");
                    if (child == null)
                    {
                        GameObject tileObject = new GameObject($"Tile {i:00}");
                        tileObject.transform.SetParent(transform, false);
                        child = tileObject.transform;
                    }

                    tiles[i] = child.GetComponent<SpriteRenderer>();
                    if (tiles[i] == null)
                    {
                        tiles[i] = child.gameObject.AddComponent<SpriteRenderer>();
                    }
                }
            }
        }

        private void RefreshTiles()
        {
            if (sourceRenderer == null || sourceRenderer.sprite == null)
            {
                return;
            }

            sourceRenderer.enabled = false;
            Vector2 tileSize = GetTileSize();
            Vector3 cameraPosition = GetCameraPosition();
            float originX = Mathf.Round(cameraPosition.x / tileSize.x) * tileSize.x;
            float originY = Mathf.Round(cameraPosition.y / tileSize.y) * tileSize.y;

            int index = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    SpriteRenderer tile = tiles[index++];
                    ApplySourceSettings(tile);
                    tile.transform.localScale = Vector3.one;
                    tile.transform.position = new Vector3(originX + x * tileSize.x, originY + y * tileSize.y, transform.position.z);
                }
            }
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
