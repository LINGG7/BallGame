using System;
using System.IO;
using BallGame.Shop;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    // Explicit menu only: never regenerate or overwrite assets on script reload.
    public static class EffectSkinPrefabBuilder
    {
        public const string OutputFolder = "Assets/Resources/Shop/Effects";
        public const string CatalogPath = "Assets/Resources/Shop/EffectSkinCatalog.asset";
        public const string ShopPath = "Assets/Resources/UI/ShopModal.prefab";
        private static readonly string[] Ids = { "basic", "fire", "ice", "star" };
        private static readonly string[] Names = { "基础光带", "火焰", "冰霜", "星光" };
        private static readonly Color[] Colors =
        {
            new Color(0.88f, 0.96f, 1f), new Color(1f, 0.4f, 0.06f),
            new Color(0.3f, 0.85f, 1f), new Color(1f, 0.76f, 0.22f)
        };

        [MenuItem("Ball Game/Effects/Create Effect Skin Assets")]
        public static void CreateAssets()
        {
            // Preflight every destination before creating anything; existing assets are never overwritten.
            CheckPath(OutputFolder);
            CheckPath(CatalogPath);
            CheckPath(ShopPath);
            RequireNew(CatalogPath);
            RequireNew(OutputFolder + "/Ribbon.mat");
            for (int i = 0; i < Ids.Length; i++)
            {
                RequireNew(OutputFolder + "/" + Ids[i] + ".prefab");
                RequireNew(OutputFolder + "/" + Ids[i] + "_Preview.asset");
                if (i > 0) RequireNew(OutputFolder + "/" + Ids[i] + "_Particle.mat");
            }
            var shader = Shader.Find("BallGame/Trail Skin");
            if (shader == null) throw new InvalidOperationException("Trail Skin shader did not import.");
            var shop = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPath);
            if (shop == null || shop.transform.Find("Panel/CategorySidebar/SkinsTab") == null)
                throw new InvalidOperationException("Shop category template is missing.");

            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Resources/Shop", "Effects");
            var ribbon = new Material(shader) { name = "Ribbon" };
            AssetDatabase.CreateAsset(ribbon, OutputFolder + "/Ribbon.mat");
            var catalog = ScriptableObject.CreateInstance<BallSkinCatalog>();
            var serialized = new SerializedObject(catalog);
            serialized.FindProperty("defaultSkinId").stringValue = "basic";
            var entries = serialized.FindProperty("skins");
            entries.arraySize = Ids.Length;
            for (int i = 0; i < Ids.Length; i++)
            {
                Material particleMaterial = null;
                if (i > 0)
                {
                    particleMaterial = new Material(shader) { name = Ids[i] + "_Particle" };
                    particleMaterial.SetFloat("_Shape", i);
                    AssetDatabase.CreateAsset(particleMaterial, OutputFolder + "/" + Ids[i] + "_Particle.mat");
                }
                var prefab = CreateEffect(i, ribbon, particleMaterial);
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = Ids[i];
                entry.FindPropertyRelative("displayName").stringValue = Names[i];
                entry.FindPropertyRelative("previewSprite").objectReferenceValue = CreatePreview(i);
                entry.FindPropertyRelative("effectPrefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("initiallyUnlocked").boolValue = i == 0;
                entry.FindPropertyRelative("unlockType").enumValueIndex = i == 0 ? 0 : 1;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AddShopTab();
            AssetDatabase.SaveAssetIfDirty(catalog);
            ValidateAssets();
            Debug.Log("Effect skin assets created and validated.");
        }

        private static GameObject CreateEffect(int style, Material ribbon, Material particleMaterial)
        {
            var root = new GameObject(Ids[style] + "_Trail");
            try
            {
                var trail = root.AddComponent<TrailRenderer>();
                trail.sharedMaterial = ribbon;
                trail.time = style == 2 ? 0.5f : 0.35f;
                trail.minVertexDistance = 0.06f;
                trail.widthMultiplier = style == 1 ? 0.68f : 0.44f;
                trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                trail.colorGradient = Fade(Colors[style], style == 1 ? new Color(1f, 0.08f, 0.01f) : Colors[style]);
                trail.numCapVertices = 3;
                trail.numCornerVertices = 3;
                trail.emitting = false;
                trail.autodestruct = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                if (style > 0)
                {
                    var child = new GameObject("Particles");
                    child.transform.SetParent(root.transform, false);
                    var particles = child.AddComponent<ParticleSystem>();
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particles.main;
                    main.playOnAwake = false;
                    main.loop = true;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.maxParticles = 64;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, style == 3 ? 0.65f : 0.45f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, style == 1 ? 0.46f : 0.34f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.45f);
                    main.startColor = Colors[style];
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.gravityModifier = style == 1 ? -0.06f : 0f;
                    var emission = particles.emission;
                    emission.rateOverTime = 0f;
                    emission.rateOverDistance = style == 3 ? 6f : 12f;
                    var shape = particles.shape;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.12f;
                    var size = particles.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
                    var color = particles.colorOverLifetime;
                    color.enabled = true;
                    color.color = Fade(Color.white, Color.white);
                    var renderer = particles.GetComponent<ParticleSystemRenderer>();
                    renderer.sharedMaterial = particleMaterial;
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, OutputFolder + "/" + Ids[style] + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); } // In-memory object only.
        }

        private static Gradient Fade(Color start, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start, 0), new GradientColorKey(end, 1) },
                new[] { new GradientAlphaKey(0.85f, 0), new GradientAlphaKey(0f, 1) });
            return gradient;
        }

        private static Sprite CreatePreview(int style)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = Ids[style] + "_Preview" };
            PaintPreview(texture, style);
            string path = OutputFolder + "/" + Ids[style] + "_Preview.asset";
            AssetDatabase.CreateAsset(texture, path);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = Ids[style] + "_Icon";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssetIfDirty(texture);
            return sprite;
        }

        private static void PaintPreview(Texture2D texture, int style)
        {
            int size = texture.width;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float t = Mathf.Clamp01((u - 0.08f) / 0.78f);
                float center = 0.46f + 0.12f * Mathf.Sin(t * Mathf.PI);
                float distance = Mathf.Abs(y / (float)(size - 1) - center);
                float alpha = u < 0.08f || u > 0.86f ? 0f : Mathf.Clamp01(1f - distance / Mathf.Lerp(0.005f, 0.045f, t)) * Mathf.Lerp(0.12f, 0.95f, t);
                if (style > 0)
                {
                    for (int n = 0; n < 6; n++)
                    {
                        float particleT = 0.23f + n * 0.105f;
                        float px = 0.08f + particleT * 0.78f;
                        float py = 0.46f + Mathf.Sin(particleT * Mathf.PI) * 0.12f + (n % 2 == 0 ? 0.12f : -0.12f);
                        float dx = Mathf.Abs(u - px);
                        float dy = Mathf.Abs(y / (float)(size - 1) - py);
                        float d = style == 3 ? Mathf.Sqrt(dx * dy) + (dx + dy) * 0.16f
                            : style == 2 ? dx + dy : Mathf.Sqrt(dx * dx * 2f + dy * dy);
                        alpha = Mathf.Max(alpha, Mathf.Clamp01((0.036f - d) * 140f));
                    }
                }
                Color color = Color.Lerp(Colors[style], Color.white, 0.1f);
                color.a = alpha;
                pixels[y * size + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        // Keep existing texture and Sprite IDs so catalog references remain intact.
        public static void RegenerateBallFreePreviews()
        {
            var textures = new Texture2D[Ids.Length];
            for (int i = 0; i < Ids.Length; i++)
            {
                string path = OutputFolder + "/" + Ids[i] + "_Preview.asset";
                CheckPath(path);
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (textures[i] == null || !textures[i].isReadable)
                    throw new InvalidOperationException("Missing or unreadable preview: " + path);
            }
            Directory.CreateDirectory("BuildChecks/BallFreePreviews");
            for (int i = 0; i < Ids.Length; i++)
            {
                var texture = textures[i];
                string path = OutputFolder + "/" + Ids[i] + "_Preview.asset";
                var catalog = AssetDatabase.LoadAssetAtPath<BallSkinCatalog>(CatalogPath);
                var icon = catalog.Find(Ids[i]).previewSprite;
                if (icon == null || AssetDatabase.GetAssetPath(icon) != path)
                    throw new InvalidOperationException("Unexpected preview reference: " + path);
                if (!texture.Reinitialize(256, 256, TextureFormat.RGBA32, false))
                    throw new InvalidOperationException("Cannot resize preview: " + path);
                PaintPreview(texture, i);
                var resizedIcon = Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f);
                resizedIcon.name = icon.name;
                try { EditorUtility.CopySerialized(resizedIcon, icon); }
                finally { UnityEngine.Object.DestroyImmediate(resizedIcon); }
                EditorUtility.SetDirty(icon);
                AssetDatabase.SaveAssetIfDirty(icon);
                if (icon.rect.width != 256 || icon.rect.height != 256 || icon.texture != texture)
                    throw new InvalidOperationException("Invalid resized sprite: " + path);
                EditorUtility.SetDirty(textures[i]);
                AssetDatabase.SaveAssetIfDirty(textures[i]);
                File.WriteAllBytes("BuildChecks/BallFreePreviews/" + Ids[i] + ".png", textures[i].EncodeToPNG());
            }
            EffectSkinVisualCheck.RenderShopPages();
            File.WriteAllText("BuildChecks/BallFreePreviews/result.txt", "PASS: four previews and Sprite rectangles unified to 256x256 without ball; framing aligned; shop pages checked.");
        }
        private static void AddShopTab()
        {
            var root = PrefabUtility.LoadPrefabContents(ShopPath);
            try
            {
                var sidebar = root.transform.Find("Panel/CategorySidebar");
                if (sidebar.Find("EffectsTab") != null) return;
                var source = sidebar.Find("SkinsTab").GetComponent<Button>();
                var tab = UnityEngine.Object.Instantiate(source, sidebar);
                tab.name = "EffectsTab";
                tab.onClick = new Button.ButtonClickedEvent();
                var rect = tab.GetComponent<RectTransform>();
                rect.anchoredPosition = source.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, -90);
                var label = tab.GetComponentInChildren<TMP_Text>(true);
                label.text = "特效皮肤";
                label.fontSize = 26;
                // Reuse the project's matching dynamic font so new theme names (including 霜) render.
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LevelDesigner/Fonts/LXGWWenKai-Light Dynamic SDF.asset");
                if (font == null) throw new InvalidOperationException("Dynamic Chinese UI font is missing.");
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font = font;
                PrefabUtility.SaveAsPrefabAsset(root, ShopPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("Ball Game/Effects/Validate Effect Skin Assets")]
        public static void ValidateAssets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BallSkinCatalog>(CatalogPath);
            if (catalog == null || catalog.Skins.Count < 4 || catalog.DefaultSkinId != "basic")
                throw new InvalidOperationException("Effect catalog is incomplete.");
            foreach (var entry in catalog.Skins)
            {
                if (entry.effectPrefab == null || entry.previewSprite == null)
                    throw new InvalidOperationException("Missing effect references: " + entry.id);
                var trail = entry.effectPrefab.GetComponent<TrailRenderer>();
                if (trail == null || trail.sharedMaterial == null || trail.sharedMaterial.shader == null)
                    throw new InvalidOperationException("Missing trail renderer/material: " + entry.id);
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(entry.effectPrefab) != 0)
                    throw new InvalidOperationException("Missing script: " + entry.id);
            }
            var shop = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPath);
            if (shop == null || shop.transform.Find("Panel/CategorySidebar/EffectsTab") == null)
                throw new InvalidOperationException("Effect category tab is missing.");
        }

        private static void RequireNew(string path)
        {
            CheckPath(path);
            if (File.Exists(path) || File.Exists(path + ".meta"))
                throw new IOException("Refusing to overwrite an existing asset or meta: " + path);
        }

        private static void CheckPath(string path)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string full = Path.GetFullPath(Path.Combine(project, path));
            string allowed = Path.GetFullPath(Path.Combine(project, "Assets/Resources/Shop")) + Path.DirectorySeparatorChar;
            string shop = Path.GetFullPath(Path.Combine(project, ShopPath));
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && full != shop)
                throw new IOException("Output is outside the approved paths: " + full);
            for (string item = full; !string.IsNullOrEmpty(item); item = Path.GetDirectoryName(item))
                if ((Directory.Exists(item) || File.Exists(item)) && (File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked paths are forbidden: " + item);
        }
    }
}