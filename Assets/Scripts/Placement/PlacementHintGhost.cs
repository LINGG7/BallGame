using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace BallGame.Placement
{
    // Copy only transforms and sprites. No prefab instantiation: Awake, particles, colliders,
    // tool behaviours and simulation components must never run on the hint visual.
    public sealed class PlacementHintGhost : MonoBehaviour
    {
        private readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        private SpriteRenderer glow;
        private TextMeshPro instruction;
        private Texture2D glowTexture;
        private Sprite glowSprite;
        private Vector3 glowBaseScale;
        private bool highlighted;
        private float highlightBlend;
        private float elapsed;
        private const float PulsePeriod = 1.6f;
        private static readonly Color Normal = new Color(1f, 0.85f, 0.2f, 0.32f);
        private static readonly Color Highlight = new Color(0.35f, 1f, 0.45f, 0.65f);

        public static PlacementHintGhost Create(PlacementHintTarget target, TMP_FontAsset font = null)
        {
            var root = new GameObject("Placement Hint Ghost");
            var ghost = root.AddComponent<PlacementHintGhost>();
            CopyVisual(target.tool.Prefab.transform, root.transform, ghost.sprites, true);
            root.transform.SetPositionAndRotation(target.position, target.Rotation);
            ghost.CreateGlow();
            ghost.CreateInstruction(font);
            ghost.ApplyLight();
            return ghost;
        }

        public void SetHighlighted(bool value)
        {
            // The controller calls this every frame. Do not restart the light animation.
            highlighted = value;
        }

        private void Update()
        {
            elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, PulsePeriod);
            highlightBlend = Mathf.MoveTowards(highlightBlend, highlighted ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            ApplyLight();
        }

        private void CreateInstruction(TMP_FontAsset font)
        {
            var labelObject = new GameObject("Hint Instruction");
            labelObject.transform.SetParent(transform, false);
            instruction = labelObject.AddComponent<TextMeshPro>();
            instruction.font = font != null ? font : TMP_Settings.defaultFontAsset;
            instruction.text = "拖动道具至虚影";
            instruction.fontSize = 3f;
            instruction.alignment = TextAlignmentOptions.Center;
            instruction.enableWordWrapping = false;
            instruction.raycastTarget = false;
            instruction.color = new Color(0.12f, 0.16f, 0.18f, 1f);
            instruction.rectTransform.sizeDelta = new Vector2(5f, 0.6f);
            if (sprites.Count > 0)
            {
                var top = sprites[0];
                foreach (var sprite in sprites)
                    if (SortingLayer.GetLayerValueFromID(sprite.sortingLayerID) > SortingLayer.GetLayerValueFromID(top.sortingLayerID)
                        || (sprite.sortingLayerID == top.sortingLayerID && sprite.sortingOrder > top.sortingOrder)) top = sprite;
                instruction.renderer.sortingLayerID = top.sortingLayerID;
                instruction.renderer.sortingOrder = Mathf.Min(32767, top.sortingOrder + 2);
            }
            PositionInstruction();
        }

        private void LateUpdate() => PositionInstruction();

        private void PositionInstruction()
        {
            if (instruction == null) return;
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool initialized = false;
            foreach (var sprite in sprites)
            {
                if (sprite == null) continue;
                if (!initialized) { bounds = sprite.bounds; initialized = true; }
                else bounds.Encapsulate(sprite.bounds);
            }
            // Stay above the visible tool, horizontal and independent of its rotation/scale.
            var labelTransform = instruction.transform;
            labelTransform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.max.y + 0.45f, transform.position.z), Quaternion.identity);
            labelTransform.localScale = Vector3.one;
            Vector3 inheritedScale = labelTransform.lossyScale;
            labelTransform.localScale = new Vector3(
                1f / Mathf.Max(0.001f, Mathf.Abs(inheritedScale.x)),
                1f / Mathf.Max(0.001f, Mathf.Abs(inheritedScale.y)),
                1f / Mathf.Max(0.001f, Mathf.Abs(inheritedScale.z)));
        }

        private void ApplyLight()
        {
            float pulse = 0.5f - 0.5f * Mathf.Cos(elapsed * Mathf.PI * 2f / PulsePeriod);
            Color body = Color.Lerp(Normal, Highlight, highlightBlend);
            body.a *= Mathf.Lerp(0.8f, 1.15f, pulse);
            foreach (var sprite in sprites) if (sprite != null) sprite.color = body;
            if (glow == null) return;
            Color light = Color.Lerp(new Color(242f / 255f, 140f / 255f, 69f / 255f), new Color(0.4f, 1f, 0.55f), highlightBlend);
            light.a = Mathf.Lerp(0.2f, 0.42f, pulse) * Mathf.Lerp(1f, 1.45f, highlightBlend);
            glow.color = light;
            glow.transform.localScale = glowBaseScale * Mathf.Lerp(0.96f, 1.08f, pulse);
        }

        private void CreateGlow()
        {
            if (sprites.Count == 0) return;
            // A soft radial sprite keeps the light visible without requiring bloom or a new shader.
            const int size = 64;
            glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Hint Glow (Runtime)",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
                float falloff = Mathf.Clamp01(1f - radius);
                float alpha = Mathf.SmoothStep(0f, 1f, falloff);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            glowTexture.SetPixels32(pixels);
            glowTexture.Apply(false, true);
            glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size,
                0, SpriteMeshType.FullRect);
            glowSprite.name = "Hint Glow (Runtime)";
            glowSprite.hideFlags = HideFlags.HideAndDontSave;

            Bounds bounds = default;
            bool initialized = false;
            var lowest = sprites[0];
            foreach (var sprite in sprites)
            {
                if (SortingLayer.GetLayerValueFromID(sprite.sortingLayerID) < SortingLayer.GetLayerValueFromID(lowest.sortingLayerID)
                    || (sprite.sortingLayerID == lowest.sortingLayerID && sprite.sortingOrder < lowest.sortingOrder))
                    lowest = sprite;
                Bounds local = sprite.localBounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                {
                    Vector3 corner = local.center + new Vector3(local.extents.x * x, local.extents.y * y, 0f);
                    Vector3 point = transform.InverseTransformPoint(sprite.transform.TransformPoint(corner));
                    if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(point);
                }
            }
            var lightObject = new GameObject("Hint Glow");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = bounds.center;
            glowBaseScale = new Vector3(Mathf.Max(0.5f, bounds.size.x) * 1.6f, Mathf.Max(0.5f, bounds.size.y) * 1.6f, 1f);
            glow = lightObject.AddComponent<SpriteRenderer>();
            glow.sprite = glowSprite;
            glow.sortingLayerID = lowest.sortingLayerID;
            glow.sortingOrder = Mathf.Max(-32768, lowest.sortingOrder - 1);
        }

        private void OnDestroy()
        {
            // These are exclusively runtime-created resources, never source assets.
            if (Application.isPlaying)
            {
                if (glowSprite != null) Destroy(glowSprite);
                if (glowTexture != null) Destroy(glowTexture);
            }
            else
            {
                if (glowSprite != null) DestroyImmediate(glowSprite);
                if (glowTexture != null) DestroyImmediate(glowTexture);
            }
        }

        private static void CopyVisual(Transform source, Transform destination, List<SpriteRenderer> sprites, bool root)
        {
            if (!root)
            {
                destination.localPosition = source.localPosition;
                destination.localRotation = source.localRotation;
            }
            destination.localScale = source.localScale;
            var original = source.GetComponent<SpriteRenderer>();
            if (original != null && original.enabled && original.sprite != null)
            {
                var copy = destination.gameObject.AddComponent<SpriteRenderer>();
                copy.sprite = original.sprite;
                copy.flipX = original.flipX;
                copy.flipY = original.flipY;
                copy.drawMode = original.drawMode;
                copy.size = original.size;
                copy.tileMode = original.tileMode;
                copy.sortingLayerID = original.sortingLayerID;
                copy.sortingOrder = original.sortingOrder + 1;
                copy.maskInteraction = original.maskInteraction;
                sprites.Add(copy);
            }
            foreach (Transform child in source)
            {
                if (!child.gameObject.activeSelf) continue;
                var next = new GameObject(child.name).transform;
                next.SetParent(destination, false);
                CopyVisual(child, next, sprites, false);
            }
        }
    }
}