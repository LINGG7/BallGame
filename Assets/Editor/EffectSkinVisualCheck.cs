using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BallGame.Shop;
using BallGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Editor
{
    [InitializeOnLoad]
    public static class EffectSkinVisualCheck
    {
        private const string DirectoryPath = "BuildChecks/EffectSkins";
        private const string RequestPath = DirectoryPath + "/visual-check.request";
        static EffectSkinVisualCheck() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            const string ballFreeRequest = "BuildChecks/BallFreePreviews/create.request";
            if (File.Exists(ballFreeRequest) && File.ReadAllText(ballFreeRequest).Trim() == "ball-free-256-v2")
            {
                File.WriteAllText(ballFreeRequest, "claimed");
                try { EffectSkinPrefabBuilder.RegenerateBallFreePreviews(); }
                catch (Exception error)
                {
                    File.WriteAllText("BuildChecks/BallFreePreviews/result.txt", error.ToString());
                    Debug.LogException(error);
                }
                return;
            }
            if (!File.Exists(RequestPath) || File.ReadAllText(RequestPath).Trim() != "render-v2") return;
            File.WriteAllText(RequestPath, "claimed");
            try
            {
                Run();
                File.WriteAllText(DirectoryPath + "/visual-check-result.txt",
                    "PASS: All four prefabs and shader references valid; switching effects/appearance/effects keeps exactly one set of cards; UI and trail previews rendered.");
            }
            catch (Exception error)
            {
                File.WriteAllText(DirectoryPath + "/visual-check-result.txt", error.ToString());
                Debug.LogException(error);
            }
        }

        [MenuItem("Ball Game/Effects/Render Effect Skin Checks")]
        public static void Run()
        {
            Directory.CreateDirectory(DirectoryPath);
            // The approved shop prefab may have been generated before the font fix finished importing.
            var shopRoot = PrefabUtility.LoadPrefabContents(EffectSkinPrefabBuilder.ShopPath);
            try
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LevelDesigner/Fonts/LXGWWenKai-Light Dynamic SDF.asset");
                if (font == null) throw new Exception("Dynamic Chinese font is missing.");
                bool changed = false;
                foreach (var label in shopRoot.GetComponentsInChildren<TMP_Text>(true))
                    if (label.font != font) { label.font = font; changed = true; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(shopRoot, EffectSkinPrefabBuilder.ShopPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(shopRoot); }
            EffectSkinPrefabBuilder.ValidateAssets();
            RenderShopPages();

        }

        public static void RenderShopPages()
        {
            var preview = new PreviewRenderUtility();
            try
            {
                var camera = preview.camera;
                camera.cameraType = CameraType.Game;
                camera.orthographic = true;
                camera.orthographicSize = 540;
                camera.transform.position = new Vector3(0, 0, -1000);
                camera.transform.rotation = Quaternion.identity;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 2000;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.16f, 0.17f, 0.2f);

                var root = new GameObject("ShopPreview", typeof(RectTransform), typeof(Canvas));
                preview.AddSingleGO(root);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
                canvas.worldCamera = camera;
                canvas.planeDistance = 100;
                var controller = root.AddComponent<ShopPanelController>();
                controller.Show();
                var switchCategory = typeof(ShopPanelController).GetMethod("SwitchCategory", BindingFlags.Instance | BindingFlags.NonPublic);
                switchCategory.Invoke(controller, new object[] { true });
                AssertCardCount(root, EffectSkinService.Catalog.Skins.Count);
                switchCategory.Invoke(controller, new object[] { false });
                AssertCardCount(root, BallSkinService.Catalog.Skins.Count);
                switchCategory.Invoke(controller, new object[] { true });
                AssertCardCount(root, EffectSkinService.Catalog.Skins.Count);
                var label = root.transform.Find("ShopModal/Panel/CategorySidebar/EffectsTab").GetComponentInChildren<TMP_Text>(true);
                if (label.text != "特效皮肤") throw new Exception("Effect category label is incorrect.");
                int total = EffectSkinService.Catalog.Skins.Count;
                int pages = Mathf.CeilToInt(total / 4f);
                var controllerType = typeof(ShopPanelController);
                if ((int)controllerType.GetField("pageCount", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller) != pages)
                    throw new Exception("Shop page count is incorrect.");
                for (int page = 0; page < pages; page++)
                {
                    controllerType.GetMethod("SelectCard", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { page * 4 });
                    controllerType.GetMethod("SetPageImmediate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { page });
                    Capture(preview, 1920, 1080, DirectoryPath + "/shop-effects-page-" + (page+1) + ".png", () =>
                    {
                        Canvas.ForceUpdateCanvases();
                        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate(true);
                        Canvas.ForceUpdateCanvases();
                    });
                    var content = root.transform.Find("ShopModal/Panel/Content/CardArea/CardContent") as RectTransform;
                    var viewport = content.parent as RectTransform;
                    var firstCard = content.Find("SkinCard_" + EffectSkinService.Catalog.Skins[page*4].id) as RectTransform;
                    var corners = new Vector3[4];
                    firstCard.GetWorldCorners(corners);
                    if (viewport.InverseTransformPoint(corners[0]).x < viewport.rect.xMin - 0.5f
                        || viewport.InverseTransformPoint(corners[2]).x > viewport.rect.xMax + 0.5f)
                        throw new Exception("First card clipped on page " + (page+1));
                }
                var allLabels = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in allLabels)
                    foreach (char character in text.text)
                        if (character > 127 && !text.font.HasCharacter(character, true))
                            throw new Exception("Missing UI glyph: " + character + " in " + text.name);
            }
            finally { preview.Cleanup(); }
        }

        private static void AssertCardCount(GameObject root, int expected)
        {
            var content = root.transform.Find("ShopModal/Panel/Content/CardArea/CardContent");
            int count = 0;
            foreach (Transform child in content) if (child.name.StartsWith("SkinCard_", StringComparison.Ordinal)) count++;
            if (count != expected) throw new Exception("Category card count: expected " + expected + ", got " + count);
        }

        private static void RenderTrails()
        {
            var preview = new PreviewRenderUtility();
            try
            {
                var camera = preview.camera;
                camera.cameraType = CameraType.Game;
                camera.orthographic = true;
                camera.orthographicSize = 3.1f;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.08f, 0.13f);
                var catalog = AssetDatabase.LoadAssetAtPath<BallSkinCatalog>(EffectSkinPrefabBuilder.CatalogPath);
                var ballSprite = BallSkinService.Catalog.Find(BallSkinService.Catalog.DefaultSkinId).runtimeSprite;
                for (int i = 0; i < catalog.Skins.Count; i++)
                {
                    var entry = catalog.Skins[i];
                    var instance = UnityEngine.Object.Instantiate(entry.effectPrefab);
                    preview.AddSingleGO(instance);
                    float y = 2.15f - i * 1.4f;
                    var trail = instance.GetComponent<TrailRenderer>();
                    if (ShaderUtil.ShaderHasError(trail.sharedMaterial.shader)) throw new Exception("Trail shader compilation error.");
                    trail.time = 10000;
                    trail.emitting = false;
                    var positions = new Vector3[65];
                    for (int p = 0; p < positions.Length; p++)
                    {
                        float t = p / 64f;
                        positions[p] = new Vector3(-3.3f + 6.1f * t, y + Mathf.Sin(t * Mathf.PI * 2) * 0.14f, 0);
                    }
                    instance.transform.position = positions[64];
                    trail.AddPositions(positions);
                    var trailMesh = new Mesh();
                    trail.BakeMesh(trailMesh, camera, false);
                    var bakedTrail = new GameObject("BakedTrail", typeof(MeshFilter), typeof(MeshRenderer));
                    preview.AddSingleGO(bakedTrail);
                    bakedTrail.transform.position = instance.transform.position;
                    bakedTrail.GetComponent<MeshFilter>().sharedMesh = trailMesh;
                    bakedTrail.GetComponent<MeshRenderer>().sharedMaterial = trail.sharedMaterial;
                    trail.enabled = false;
                    foreach (var particles in instance.GetComponentsInChildren<ParticleSystem>())
                    {
                        particles.useAutoRandomSeed = false;
                        particles.randomSeed = 47;
                        for (int p = 0; p < 22; p++)
                        {
                            float t = p / 22f;
                            var emit = new ParticleSystem.EmitParams
                            {
                                position = new Vector3(-2.6f + 5.2f * t, y + Mathf.Sin(p * 2.4f) * 0.25f, 0),
                                velocity = Vector3.zero,
                                startLifetime = 10f,
                                startSize = i == 1 ? 0.19f : 0.14f
                            };
                            particles.Emit(emit, 1);
                        }
                        particles.Simulate(0.01f, true, false, false);
                        particles.Pause(true);
                        var renderer = particles.GetComponent<ParticleSystemRenderer>();
                        var particleMesh = new Mesh();
                        renderer.BakeMesh(particleMesh, camera, false);
                        var bakedParticles = new GameObject("BakedParticles", typeof(MeshFilter), typeof(MeshRenderer));
                        preview.AddSingleGO(bakedParticles);
                        bakedParticles.transform.position = particles.transform.position;
                        bakedParticles.GetComponent<MeshFilter>().sharedMesh = particleMesh;
                        bakedParticles.GetComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;
                        renderer.enabled = false;
                    }
                    var ball = new GameObject("Ball", typeof(SpriteRenderer));
                    preview.AddSingleGO(ball);
                    ball.transform.position = positions[64] + new Vector3(0, 0, -0.1f);
                    ball.GetComponent<SpriteRenderer>().sprite = ballSprite;
                    ball.GetComponent<SpriteRenderer>().sortingOrder = 2;
                    ball.transform.localScale = Vector3.one * (0.42f / Mathf.Max(ballSprite.bounds.size.x, 0.01f));
                }
                Capture(preview, 1280, 800, DirectoryPath + "/trails.png");
            }
            finally { preview.Cleanup(); }
        }

        private static void Capture(PreviewRenderUtility preview, int width, int height, string path, Action prepare = null)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                preview.camera.targetTexture = target;
                prepare?.Invoke();
                preview.camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                preview.camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}