using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;

namespace BallGame.Placement
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlaceableTool : MonoBehaviour
    {
        [SerializeField] private ToolDefinition definition;
        [SerializeField] private bool canEdit = true;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.55f;

        private Collider2D[] colliders;
        private Color[] originalColors;

        public ToolDefinition Definition => definition;
        public bool CanEdit => canEdit;
        public bool IsPreview { get; private set; }
        public IReadOnlyList<Collider2D> Colliders => colliders;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<SpriteRenderer>();
            }

            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
            }
        }

        public void Initialize(ToolDefinition toolDefinition)
        {
            definition = toolDefinition;
        }

        public void SetPreview(bool isPreview, bool isValid)
        {
            IsPreview = isPreview;
            SetCollidersEnabled(true);

            Color color = isValid ? new Color(0.35f, 1f, 0.45f, 0.55f) : new Color(1f, 0.25f, 0.25f, 0.55f);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].color = isPreview ? color : originalColors[i];
            }
        }

        public void SetSelected(bool selected)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = originalColors[i];
                if (selected)
                {
                    color.a *= selectedAlpha;
                }

                renderers[i].color = color;
            }
        }

        public void SetSimulationMode(bool simulating)
        {
            SetSelected(false);
            SetCollidersEnabled(true);
        }

        public void Rotate(float degrees)
        {
            if (!IsPreview && !canEdit)
            {
                return;
            }

            if (definition == null || !definition.CanRotate)
            {
                return;
            }

            transform.Rotate(0f, 0f, degrees);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (colliders == null)
            {
                colliders = GetComponentsInChildren<Collider2D>();
            }

            foreach (Collider2D toolCollider in colliders)
            {
                toolCollider.enabled = enabled;
            }
        }
    }
}
