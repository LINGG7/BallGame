using BallGame.Shop;
using UnityEngine;

namespace BallGame.Core
{
    public sealed class BallVisual3DTransition : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject sphereObject;
        [SerializeField] private Material fallbackMaterial;

        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRect");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int ProjectionRadiusId = Shader.PropertyToID("_ProjectionRadius");

        private bool subscribed;
        private MaterialPropertyBlock propertyBlock;

        public bool Is3D { get; private set; }

        public void Initialize()
        {
            if (spriteRenderer == null || meshRenderer == null || sphereObject == null)
            {
                Debug.LogError("Ball 3D visual references are missing on " + name + ".", this);
                return;
            }

            if (!subscribed)
            {
                BallSkinService.SkinChanged += HandleSkinChanged;
                subscribed = true;
            }

            ApplySkin();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (subscribed)
            {
                BallSkinService.SkinChanged -= HandleSkinChanged;
                subscribed = false;
            }
        }

        public void SwitchTo3D()
        {
            Initialize();
            Is3D = true;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (sphereObject != null) sphereObject.SetActive(true);
            ApplySkin();
        }

        public void ResetTo2D()
        {
            Is3D = false;
            if (sphereObject != null) sphereObject.SetActive(false);
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        private void HandleSkinChanged(string id)
        {
            ApplySkin();
        }

        private void ApplySkin()
        {
            if (meshRenderer == null) return;
            BallSkinEntry entry = BallSkinService.Catalog?.Find(BallSkinService.CurrentSkinId);
            Sprite skinSprite = entry != null ? entry.runtimeSprite : null;
            if (skinSprite == null && spriteRenderer != null)
            {
                skinSprite = spriteRenderer.sprite;
            }

            Texture texture = skinSprite != null ? skinSprite.texture : Texture2D.whiteTexture;
            Vector4 spriteRect = new Vector4(0f, 0f, 1f, 1f);
            if (skinSprite != null && texture != null)
            {
                Rect rect = skinSprite.textureRect;
                spriteRect = new Vector4(
                    rect.x / texture.width,
                    rect.y / texture.height,
                    rect.width / texture.width,
                    rect.height / texture.height);
            }

            if (fallbackMaterial != null && meshRenderer.sharedMaterial != fallbackMaterial)
            {
                meshRenderer.sharedMaterial = fallbackMaterial;
            }

            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(MainTextureId, texture);
            propertyBlock.SetVector(SpriteRectId, spriteRect);
            propertyBlock.SetColor(TintId, Color.white);
            propertyBlock.SetFloat(ProjectionRadiusId,
                entry != null ? Mathf.Clamp(entry.sphereProjectionRadius, 0.5f, 1f) : 1f);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
