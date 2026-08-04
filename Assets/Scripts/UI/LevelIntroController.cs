using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class LevelIntroController : MonoBehaviour
    {
        private const float FadeDuration = 0.8f;
        private const float RevealDuration = 1.2f;
        private const float HoldDuration = 1f;
        private const float DissolveDuration = 1.1f;
        private const float TailDuration = 0.4f;
        private const float HudFadeDuration = 0.2f;
        private const float SkipInputDelay = 0.2f;
        private const float FastForwardMultiplier = 4f;

        [Header("Ink Brush Background")]
        [SerializeField] private Sprite brushSprite;
        [SerializeField] private Color brushColor = new Color(0.09f, 0.08f, 0.07f, 0.8f);
        [SerializeField, Min(0.01f)] private float brushRevealDuration = 0.55f;
        [SerializeField, Min(1f)] private float brushMinWidth = 720f;
        [SerializeField, Min(1f)] private float brushMaxWidth = 1580f;
        [SerializeField, Min(0f)] private float brushHorizontalPadding = 260f;
        [SerializeField, Min(1f)] private float brushHeight = 280f;

        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
        private static readonly int DissolveProgressId = Shader.PropertyToID("_DissolveProgress");
        private static readonly int TextMinXId = Shader.PropertyToID("_TextMinX");
        private static readonly int TextMaxXId = Shader.PropertyToID("_TextMaxX");

        private struct InkSample
        {
            public Vector2 position;
            public float dissolveOrder;
        }

        private readonly List<CanvasGroup> gameplayGroups = new List<CanvasGroup>();
        private readonly List<InkSample> inkSamples = new List<InkSample>(768);

        private RectTransform overlayRect;
        private Image blackOverlay;
        private RectTransform brushRect;
        private Image brushBackground;
        private TextMeshProUGUI introTitle;
        private InkParticleGraphic particles;
        private Material inkMaterial;
        private Material brushMaterial;
        private Coroutine sequence;
        private Action completedCallback;
        private float introStartedAt;
        private float playbackSpeed = 1f;
        private int nextParticleSample;
        private bool hasInkShader;
        private float brushRevealProgress;
        private float brushDissolveProgress;

        public bool IsPlaying => sequence != null;

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void Configure(TMP_FontAsset font)
        {
            EnsureHierarchy();
            if (font != null)
            {
                introTitle.font = font;
                if (inkMaterial != null)
                {
                    inkMaterial.mainTexture = font.atlasTexture;
                    introTitle.fontSharedMaterial = inkMaterial;
                }
            }
        }

        public void Play(string title, Action completed)
        {
            EnsureHierarchy();
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            completedCallback = completed;
            introStartedAt = Time.unscaledTime;
            playbackSpeed = 1f;
            nextParticleSample = 0;
            overlayRect.gameObject.SetActive(true);
            overlayRect.SetAsLastSibling();
            SetGameplayVisibility(0f, false);
            SetBlackAlpha(1f);
            particles.ClearParticles();

            introTitle.text = string.IsNullOrWhiteSpace(title) ? "Ball Puzzle" : title;
            introTitle.alpha = 1f;
            Canvas.ForceUpdateCanvases();
            introTitle.ForceMeshUpdate(true, true);
            Bounds textBounds = introTitle.textBounds;
            float backgroundWidth = Mathf.Clamp(introTitle.preferredWidth + brushHorizontalPadding, brushMinWidth, brushMaxWidth);
            brushRect.sizeDelta = new Vector2(backgroundWidth, brushHeight);
            SetMaterialFloat(TextMinXId, textBounds.min.x);
            SetMaterialFloat(TextMaxXId, Mathf.Max(textBounds.min.x + 1f, textBounds.max.x));
            SetBrushReveal(0f);
            SetBrushDissolve(0f);
            SetReveal(0f);
            SetDissolve(0f);
            BuildInkSamples();
            sequence = StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            yield return RunPhase(FadeDuration, progress => SetBlackAlpha(1f - Smooth(progress)));
            SetBlackAlpha(0f);

            yield return RunPhase(brushRevealDuration, progress => SetBrushReveal(Smooth(progress)));
            SetBrushReveal(1f);

            yield return RunPhase(RevealDuration, progress => SetReveal(Smooth(progress)));
            SetReveal(1f);

            yield return RunPhase(HoldDuration, null);

            yield return RunPhase(DissolveDuration, progress =>
            {
                float eased = Smooth(progress);
                SetDissolve(eased);
                SetBrushDissolve(eased);
                EmitParticlesThrough(eased);
            });
            SetDissolve(1f);
            SetBrushDissolve(1f);
            EmitParticlesThrough(1f);

            yield return RunPhase(TailDuration, progress =>
            {
                float hudStart = 1f - HudFadeDuration / TailDuration;
                float hudProgress = Mathf.InverseLerp(hudStart, 1f, progress);
                SetGameplayVisibility(Smooth(hudProgress), false);
            });

            SetGameplayVisibility(1f, true);
            overlayRect.gameObject.SetActive(false);
            sequence = null;
            Action callback = completedCallback;
            completedCallback = null;
            callback?.Invoke();
        }

        private IEnumerator RunPhase(float duration, Action<float> update)
        {
            float elapsed = 0f;
            update?.Invoke(0f);
            while (elapsed < duration)
            {
                UpdateFastForward();
                elapsed += Time.unscaledDeltaTime * playbackSpeed;
                update?.Invoke(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        private void UpdateFastForward()
        {
            if (playbackSpeed > 1f || Time.unscaledTime - introStartedAt < SkipInputDelay)
            {
                return;
            }

            bool pressed = Input.GetMouseButtonDown(0);
            if (!pressed && Input.touchCount > 0)
            {
                pressed = Input.GetTouch(0).phase == TouchPhase.Began;
            }

            if (pressed)
            {
                playbackSpeed = FastForwardMultiplier;
            }
        }

        private void EnsureHierarchy()
        {
            if (overlayRect != null)
            {
                return;
            }

            CaptureGameplayGroups();

            GameObject overlayObject = new GameObject("LevelIntro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(transform, false);
            Stretch(overlayRect);
            blackOverlay = overlayObject.GetComponent<Image>();
            blackOverlay.color = Color.black;
            blackOverlay.raycastTarget = true;

            GameObject brushObject = new GameObject("InkBrushBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            brushRect = brushObject.GetComponent<RectTransform>();
            brushRect.SetParent(overlayRect, false);
            brushRect.anchorMin = new Vector2(0.5f, 0.5f);
            brushRect.anchorMax = new Vector2(0.5f, 0.5f);
            brushRect.pivot = new Vector2(0.5f, 0.5f);
            brushRect.anchoredPosition = Vector2.zero;
            brushRect.sizeDelta = new Vector2(brushMaxWidth, brushHeight);
            brushBackground = brushObject.GetComponent<Image>();
            brushBackground.sprite = brushSprite;
            brushBackground.color = brushColor;
            brushBackground.raycastTarget = false;
            brushBackground.preserveAspect = false;
            brushBackground.gameObject.SetActive(brushSprite != null);

            Shader brushShader = Resources.Load<Shader>("Shaders/UIInkBrushIntro");
            if (brushShader == null)
            {
                brushShader = Shader.Find("BallGame/UI/Ink Brush Intro");
            }
            if (brushShader != null)
            {
                brushMaterial = new Material(brushShader)
                {
                    name = "Level Intro Brush (Instance)"
                };
                brushBackground.material = brushMaterial;
            }

            GameObject titleObject = new GameObject("InkTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.SetParent(overlayRect, false);
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(1680f, 260f);
            titleRect.anchoredPosition = Vector2.zero;

            introTitle = titleObject.GetComponent<TextMeshProUGUI>();
            introTitle.alignment = TextAlignmentOptions.Center;
            introTitle.color = new Color(0.91f, 0.89f, 0.82f, 1f);
            introTitle.fontSize = 112f;
            introTitle.enableAutoSizing = true;
            introTitle.fontSizeMin = 46f;
            introTitle.fontSizeMax = 112f;
            introTitle.enableWordWrapping = false;
            introTitle.overflowMode = TextOverflowModes.Ellipsis;
            introTitle.raycastTarget = false;
            introTitle.font = TMP_Settings.defaultFontAsset;

            Shader shader = Resources.Load<Shader>("Shaders/TMPInkIntro");
            if (shader == null)
            {
                shader = Shader.Find("BallGame/UI/TMP Ink Intro");
            }
            hasInkShader = shader != null;
            if (hasInkShader)
            {
                inkMaterial = new Material(shader)
                {
                    name = "Level Intro Ink (Instance)"
                };
                if (introTitle.font != null)
                {
                    inkMaterial.mainTexture = introTitle.font.atlasTexture;
                }
                introTitle.fontSharedMaterial = inkMaterial;
            }

            GameObject particleObject = new GameObject("InkParticles", typeof(RectTransform), typeof(CanvasRenderer), typeof(InkParticleGraphic));
            RectTransform particleRect = particleObject.GetComponent<RectTransform>();
            particleRect.SetParent(overlayRect, false);
            Stretch(particleRect);
            particles = particleObject.GetComponent<InkParticleGraphic>();

            overlayRect.SetAsLastSibling();
            overlayRect.gameObject.SetActive(false);
        }

        private void CaptureGameplayGroups()
        {
            gameplayGroups.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = child.AddComponent<CanvasGroup>();
                }
                gameplayGroups.Add(group);
            }
        }

        private void BuildInkSamples()
        {
            inkSamples.Clear();
            TMP_TextInfo textInfo = introTitle.textInfo;
            Texture2D atlas = introTitle.font != null ? introTitle.font.atlasTexture : null;
            bool canReadAtlas = atlas != null && atlas.isReadable;

            for (int characterIndex = 0; characterIndex < textInfo.characterCount; characterIndex++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[characterIndex];
                if (!character.isVisible)
                {
                    continue;
                }

                TMP_MeshInfo meshInfo = textInfo.meshInfo[character.materialReferenceIndex];
                int vertexIndex = character.vertexIndex;
                if (meshInfo.vertices == null || vertexIndex + 3 >= meshInfo.vertices.Length)
                {
                    continue;
                }

                Vector3 bottomLeft = meshInfo.vertices[vertexIndex];
                Vector3 topLeft = meshInfo.vertices[vertexIndex + 1];
                Vector3 topRight = meshInfo.vertices[vertexIndex + 2];
                Vector3 bottomRight = meshInfo.vertices[vertexIndex + 3];
                Vector2 uvBottomLeft = meshInfo.uvs0[vertexIndex];
                Vector2 uvTopLeft = meshInfo.uvs0[vertexIndex + 1];
                Vector2 uvTopRight = meshInfo.uvs0[vertexIndex + 2];
                Vector2 uvBottomRight = meshInfo.uvs0[vertexIndex + 3];

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        float u = (x + 0.5f + StableNoise(characterIndex * 31 + x * 7 + y * 13) * 0.42f) / 8f;
                        float v = (y + 0.5f + StableNoise(characterIndex * 47 + x * 11 + y * 5) * 0.42f) / 8f;
                        Vector3 bottom = Vector3.Lerp(bottomLeft, bottomRight, u);
                        Vector3 top = Vector3.Lerp(topLeft, topRight, u);
                        Vector3 localPosition = Vector3.Lerp(bottom, top, v);
                        Vector2 uvBottom = Vector2.Lerp(uvBottomLeft, uvBottomRight, u);
                        Vector2 uvTop = Vector2.Lerp(uvTopLeft, uvTopRight, u);
                        Vector2 uv = Vector2.Lerp(uvBottom, uvTop, v);
                        if (canReadAtlas && atlas.GetPixelBilinear(uv.x, uv.y).a < 0.48f)
                        {
                            continue;
                        }

                        Vector3 worldPosition = introTitle.rectTransform.TransformPoint(localPosition);
                        Vector2 particlePosition = particles.rectTransform.InverseTransformPoint(worldPosition);
                        inkSamples.Add(new InkSample
                        {
                            position = particlePosition,
                            dissolveOrder = Mathf.Lerp(0.08f, 0.92f, InkNoise(localPosition))
                        });
                    }
                }
            }

            inkSamples.Sort((left, right) => left.dissolveOrder.CompareTo(right.dissolveOrder));
        }

        private void EmitParticlesThrough(float progress)
        {
            while (nextParticleSample < inkSamples.Count && inkSamples[nextParticleSample].dissolveOrder <= progress)
            {
                InkSample sample = inkSamples[nextParticleSample++];
                float horizontal = Mathf.Sign(sample.position.x);
                particles.Emit(sample.position, new Vector2(horizontal * 0.35f, 1f).normalized);
            }
        }

        private void SetReveal(float progress)
        {
            if (hasInkShader)
            {
                SetMaterialFloat(RevealProgressId, progress);
            }
            else
            {
                introTitle.alpha = progress;
            }
        }

        private void SetDissolve(float progress)
        {
            if (hasInkShader)
            {
                SetMaterialFloat(DissolveProgressId, progress);
            }
            else
            {
                introTitle.alpha = 1f - progress;
            }
        }

        private void SetBrushReveal(float progress)
        {
            brushRevealProgress = Mathf.Clamp01(progress);
            if (brushMaterial != null)
            {
                brushMaterial.SetFloat(RevealProgressId, brushRevealProgress);
            }
            else
            {
                ApplyFallbackBrushAlpha();
            }
        }

        private void SetBrushDissolve(float progress)
        {
            brushDissolveProgress = Mathf.Clamp01(progress);
            if (brushMaterial != null)
            {
                brushMaterial.SetFloat(DissolveProgressId, brushDissolveProgress);
            }
            else
            {
                ApplyFallbackBrushAlpha();
            }
        }

        private void ApplyFallbackBrushAlpha()
        {
            if (brushBackground == null)
            {
                return;
            }

            Color color = brushColor;
            color.a *= brushRevealProgress * (1f - brushDissolveProgress);
            brushBackground.color = color;
        }

        private void SetMaterialFloat(int propertyId, float value)
        {
            if (inkMaterial != null)
            {
                inkMaterial.SetFloat(propertyId, value);
            }
        }

        private void SetBlackAlpha(float alpha)
        {
            Color color = blackOverlay.color;
            color.a = Mathf.Clamp01(alpha);
            blackOverlay.color = color;
        }

        private void SetGameplayVisibility(float alpha, bool interactive)
        {
            for (int i = 0; i < gameplayGroups.Count; i++)
            {
                CanvasGroup group = gameplayGroups[i];
                if (group == null)
                {
                    continue;
                }

                group.alpha = alpha;
                group.interactable = interactive;
                group.blocksRaycasts = interactive;
            }
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float StableNoise(int seed)
        {
            return Mathf.Repeat(Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f, 1f) - 0.5f;
        }

        private static float InkNoise(Vector2 position)
        {
            Vector2 scaled = position * 0.075f;
            Vector2 cell = new Vector2(Mathf.Floor(scaled.x), Mathf.Floor(scaled.y));
            Vector2 blend = new Vector2(Mathf.Repeat(scaled.x, 1f), Mathf.Repeat(scaled.y, 1f));
            blend = new Vector2(
                blend.x * blend.x * (3f - 2f * blend.x),
                blend.y * blend.y * (3f - 2f * blend.y));
            float bottom = Mathf.Lerp(Hash21(cell), Hash21(cell + Vector2.right), blend.x);
            float top = Mathf.Lerp(Hash21(cell + Vector2.up), Hash21(cell + Vector2.one), blend.x);
            return Mathf.Lerp(bottom, top, blend.y);
        }

        private static float Hash21(Vector2 value)
        {
            value = new Vector2(
                Mathf.Repeat(value.x * 123.34f, 1f),
                Mathf.Repeat(value.y * 456.21f, 1f));
            float dot = Vector2.Dot(value, value + Vector2.one * 45.32f);
            value += Vector2.one * dot;
            return Mathf.Repeat(value.x * value.y, 1f);
        }

        private void OnDestroy()
        {
            if (inkMaterial != null)
            {
                Destroy(inkMaterial);
            }

            if (brushMaterial != null)
            {
                Destroy(brushMaterial);
            }
        }

        private void OnValidate()
        {
            brushRevealDuration = Mathf.Max(0.01f, brushRevealDuration);
            brushMinWidth = Mathf.Max(1f, brushMinWidth);
            brushMaxWidth = Mathf.Max(brushMinWidth, brushMaxWidth);
            brushHorizontalPadding = Mathf.Max(0f, brushHorizontalPadding);
            brushHeight = Mathf.Max(1f, brushHeight);
        }
    }
}
