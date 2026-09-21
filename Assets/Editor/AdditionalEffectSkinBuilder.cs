using System;
using System.Collections.Generic;
using System.IO;
using BallGame.Shop;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BallGame.Editor
{
    // Additive: existing prefab values and catalog entries are never rebuilt.
    [InitializeOnLoad]
    public static class AdditionalEffectSkinBuilder
    {
        private const string Folder = "Assets/Resources/Shop/Effects";
        private const string CatalogPath = "Assets/Resources/Shop/EffectSkinCatalog.asset";
        private const string CheckFolder = "BuildChecks/EffectThemes";
        private const string RequestPath = CheckFolder + "/create.request";
        private static double nextPoll;
        private static readonly string[] Ids = { "lightning", "rainbow", "sakura", "bubble", "ink", "music" };
        private static readonly string[] Names = { "雷电", "彩虹", "樱花", "水泡", "水墨", "音符" };
        private static readonly Color[] Colors =
        {
            new Color(0.25f, 0.65f, 1f), Color.white, new Color(1f, 0.36f, 0.62f),
            new Color(0.32f, 0.78f, 1f), new Color(0.12f, 0.11f, 0.15f), new Color(0.68f, 0.32f, 0.92f)
        };

        static AdditionalEffectSkinBuilder() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (Application.isBatchMode || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(RequestPath) || File.ReadAllText(RequestPath).Trim() != "create-six-free-v1") return;
            File.WriteAllText(RequestPath, "claimed");
            try
            {
                Create();
                File.WriteAllText(CheckFolder + "/result.txt", "PASS: six new free themes created and appended; all original entries preserved; shader/reference checks passed.");
                EffectSkinVisualCheck.RenderShopPages();
                File.AppendAllText(CheckFolder + "/result.txt", Environment.NewLine + "PASS: catalog-driven shop pages rendered and category/card/page checks passed.");
            }
            catch (Exception error)
            {
                File.AppendAllText(CheckFolder + "/result.txt", Environment.NewLine + error);
                Debug.LogException(error);
            }
        }

        // Batch execution stays inside this original project and writes only declared outputs.
        public static void BuildAndCheck()
        {
            Directory.CreateDirectory(CheckFolder);
            File.WriteAllText(RequestPath, "claimed");
            try
            {
                Create();
                File.WriteAllText(CheckFolder + "/result.txt", "PASS: six free themes created; original entries preserved; reference and shader checks passed.");
                EffectSkinVisualCheck.RenderShopPages();
                File.AppendAllText(CheckFolder + "/result.txt", Environment.NewLine + "PASS: all catalog pages, card bounds and Chinese glyphs checked.");
            }
            catch (Exception error)
            {
                File.AppendAllText(CheckFolder + "/result.txt", Environment.NewLine + error);
                throw;
            }
        }
        [MenuItem("Ball Game/Effects/Add Six Theme Skins")]
        public static void Create()
        {
            CheckPath(CatalogPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BallSkinCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Existing effect catalog is missing.");
            var shader = Shader.Find("BallGame/Theme Trail");
            if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Theme shader is missing or has errors.");
            foreach (var id in Ids)
            {
                if (catalog.Find(id) != null) throw new InvalidOperationException("Theme already exists: " + id);
                foreach (string suffix in new[] { ".prefab", "_Ribbon.mat", "_Particle.mat", "_Preview.asset" })
                {
                    string path = Folder + "/" + id + suffix;
                    CheckPath(path);
                    if (File.Exists(path) || File.Exists(path + ".meta")) throw new IOException("Existing output refused: " + path);
                }
            }

            Directory.CreateDirectory(CheckFolder);
            var oldEntries = new List<string>();
            foreach (var entry in catalog.Skins) oldEntries.Add(JsonUtility.ToJson(entry));
            string oldDefault = catalog.DefaultSkinId;
            var prefabs = new GameObject[Ids.Length];
            var icons = new Sprite[Ids.Length];
            for (int i = 0; i < Ids.Length; i++)
            {
                var ribbon = NewMaterial(shader, i, false);
                var particle = NewMaterial(shader, i, true);
                prefabs[i] = NewPrefab(i, ribbon, particle);
                icons[i] = NewPreview(i, ribbon, particle);
            }
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("skins");
            int first = entries.arraySize;
            entries.arraySize += Ids.Length;
            for (int i = 0; i < Ids.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(first + i);
                entry.FindPropertyRelative("id").stringValue = Ids[i];
                entry.FindPropertyRelative("displayName").stringValue = Names[i];
                entry.FindPropertyRelative("previewSprite").objectReferenceValue = icons[i];
                entry.FindPropertyRelative("runtimeSprite").objectReferenceValue = null;
                entry.FindPropertyRelative("effectPrefab").objectReferenceValue = prefabs[i];
                entry.FindPropertyRelative("sphereProjectionRadius").floatValue = 1f;
                entry.FindPropertyRelative("featured").boolValue = false;
                entry.FindPropertyRelative("initiallyUnlocked").boolValue = true;
                entry.FindPropertyRelative("unlockType").enumValueIndex = (int)BallSkinUnlockType.Free;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
            if (catalog.DefaultSkinId != oldDefault || catalog.Skins.Count != oldEntries.Count + 6)
                throw new InvalidOperationException("Catalog default/count changed unexpectedly.");
            for (int i = 0; i < oldEntries.Count; i++)
                if (JsonUtility.ToJson(catalog.Skins[i]) != oldEntries[i])
                    throw new InvalidOperationException("Original catalog entry changed: " + i);
            foreach (var id in Ids)
            {
                var entry = catalog.Find(id);
                if (entry == null || entry.effectPrefab == null || entry.previewSprite == null || entry.unlockType != BallSkinUnlockType.Free)
                    throw new InvalidOperationException("Invalid new theme: " + id);
                foreach (var renderer in entry.effectPrefab.GetComponentsInChildren<Renderer>(true))
                    if (renderer.sharedMaterial == null || ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))
                        throw new InvalidOperationException("Invalid theme material: " + id);
                foreach (var transform in entry.effectPrefab.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new InvalidOperationException("Missing script: " + id);
            }
            Debug.Log("Added six free effect skins. Existing themes retained.");
        }

        private static Material NewMaterial(Shader shader, int style, bool particle)
        {
            var material = new Material(shader) { name = Ids[style] + (particle ? "_Particle" : "_Ribbon") };
            material.SetFloat("_Style", style);
            material.SetFloat("_Particle", particle ? 1 : 0);
            AssetDatabase.CreateAsset(material, Folder + "/" + material.name + ".mat");
            return material;
        }

        private static Gradient Fade(Color start, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start,0), new GradientColorKey(end,1) },
                new[] { new GradientAlphaKey(0.85f,0),new GradientAlphaKey(0f,1) });
            return gradient;
        }

        private static GameObject NewPrefab(int style, Material ribbon, Material particle)
        {
            var root = new GameObject(Ids[style]);
            try
            {
                var trail = root.AddComponent<TrailRenderer>();
                trail.sharedMaterial = ribbon;
                trail.time = style == 0 ? 0.3f : 0.45f;
                trail.widthMultiplier = style == 0 || style == 1 ? 0.48f : 0.4f;
                trail.widthCurve = AnimationCurve.Linear(0,1,1,0);
                trail.colorGradient = Fade(Colors[style], Colors[style]);
                trail.minVertexDistance = 0.035f;
                trail.numCapVertices = 3;
                trail.numCornerVertices = 3;
                trail.emitting = false;
                trail.autodestruct = false;
                trail.sortingOrder = -2;
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.receiveShadows = false;
                var child = new GameObject("Particles");
                child.transform.SetParent(root.transform, false);
                var system = child.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.playOnAwake = false;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 80;
                main.startLifetime = new ParticleSystem.MinMaxCurve(style == 0 ? 0.18f : 0.3f, style == 3 ? 0.7f : 0.55f);
                main.startSize = new ParticleSystem.MinMaxCurve(style == 5 ? 0.28f : 0.18f, style == 5 ? 0.46f : 0.38f);
                main.startSpeed = 0f;
                main.startColor = Colors[style];
                if (style == 1)
                {
                    var palette = new Gradient();
                    palette.SetKeys(new[]
                    {
                        new GradientColorKey(Color.red,0), new GradientColorKey(Color.yellow,0.2f),
                        new GradientColorKey(Color.green,0.4f), new GradientColorKey(Color.cyan,0.6f),
                        new GradientColorKey(Color.blue,0.8f),new GradientColorKey(Color.magenta,1)
                    }, new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(1,1) });
                    main.startColor = new ParticleSystem.MinMaxGradient(palette) { mode = ParticleSystemGradientMode.RandomColor };
                }
                float rotation = style == 2 ? Mathf.PI : style == 5 ? 0.2f : 0.4f;
                main.startRotation = new ParticleSystem.MinMaxCurve(-rotation, rotation);
                var spin = system.rotationOverLifetime;
                spin.enabled = style == 2 || style == 5;
                spin.z = new ParticleSystem.MinMaxCurve(-0.6f,0.6f);
                var emission = system.emission;
                emission.rateOverTime = 0;
                emission.rateOverDistance = style == 5 ? 5f : 8f;
                var shape = system.shape;
                shape.enabled = false;
                var size = system.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, style == 3
                    ? new AnimationCurve(new Keyframe(0,0.6f),new Keyframe(0.75f,1f),new Keyframe(1,0.95f))
                    : new AnimationCurve(new Keyframe(0,1),new Keyframe(0.6f,0.85f),new Keyframe(1,0.3f)));
                var color = system.colorOverLifetime;
                color.enabled = true;
                color.color = Fade(Color.white,Color.white);
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = particle;
                renderer.sortingOrder = -1;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + Ids[style] + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Sprite NewPreview(int style, Material ribbon, Material particle)
        {
            const int dimension = 256;
            var target = RenderTexture.GetTemporary(dimension,dimension,0,RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                RenderTexture.active = target;
                GL.Clear(true,true,Color.clear);
                GL.PushMatrix();
                try
                {
                    GL.LoadOrtho();
                    for (int j = 0; j < 48; j++)
                    {
                        float t = j/48f;
                        float t1 = (j+1)/48f;
                        float x = 0.08f + t*0.78f;
                        float x1 = 0.08f + t1*0.78f;
                        float y = 0.46f + Mathf.Sin(t*Mathf.PI)*0.12f;
                        float y1 = 0.46f + Mathf.Sin(t1*Mathf.PI)*0.12f;
                        float w = Mathf.Lerp(0.005f,0.045f,t);
                        Color color = Colors[style]; color.a = Mathf.Lerp(0.12f,0.95f,t);
                        ribbon.SetPass(0);
                        GL.Begin(GL.QUADS); GL.Color(color);
                        GL.TexCoord2(t,0); GL.Vertex3(x,y-w,0);
                        GL.TexCoord2(t1,0); GL.Vertex3(x1,y1-w,0);
                        GL.TexCoord2(t1,1); GL.Vertex3(x1,y1+w,0);
                        GL.TexCoord2(t,1); GL.Vertex3(x,y+w,0);
                        GL.End();
                    }
                    for (int j = 0; j < 6; j++)
                    {
                        float t = 0.23f+j*0.105f;
                        float y = 0.46f+Mathf.Sin(t*Mathf.PI)*0.12f + (j%2==0 ? 0.12f : -0.12f);
                        Color color = style == 1 ? Color.HSVToRGB(j/6f,0.8f,1f) : Colors[style];
                        Quad(particle,new Rect(0.08f+t*0.78f-0.075f,y-0.075f,0.15f,0.15f),color);
                    }
                }
                finally { GL.PopMatrix(); }
                texture = new Texture2D(dimension,dimension,TextureFormat.RGBA32,false) { name = Ids[style] + "_Preview" };
                texture.ReadPixels(new Rect(0,0,dimension,dimension),0,0);
                texture.Apply();
                File.WriteAllBytes(CheckFolder + "/" + Ids[style] + ".png", texture.EncodeToPNG());
                AssetDatabase.CreateAsset(texture,Folder + "/" + Ids[style] + "_Preview.asset");
                var sprite = Sprite.Create(texture,new Rect(0,0,dimension,dimension),new Vector2(0.5f,0.5f),100);
                sprite.name = Ids[style] + "_Icon";
                AssetDatabase.AddObjectToAsset(sprite,texture);
                AssetDatabase.SaveAssetIfDirty(texture);
                return sprite;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (texture != null && !EditorUtility.IsPersistent(texture)) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Quad(Material material, Rect rect, Color color)
        {
            material.SetPass(0); GL.Begin(GL.QUADS); GL.Color(color);
            GL.TexCoord2(0,0); GL.Vertex3(rect.xMin,rect.yMin,0);
            GL.TexCoord2(1,0); GL.Vertex3(rect.xMax,rect.yMin,0);
            GL.TexCoord2(1,1); GL.Vertex3(rect.xMax,rect.yMax,0);
            GL.TexCoord2(0,1); GL.Vertex3(rect.xMin,rect.yMax,0);
            GL.End();
        }

        private static void CheckPath(string path)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string full = Path.GetFullPath(Path.Combine(project,path));
            string allowed = Path.GetFullPath(Path.Combine(project,Folder)) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)
                && full != Path.GetFullPath(Path.Combine(project,CatalogPath))) throw new IOException("Unexpected destination: " + full);
            for (string item = full; !string.IsNullOrEmpty(item); item = Path.GetDirectoryName(item))
                if ((File.Exists(item)||Directory.Exists(item)) && (File.GetAttributes(item)&FileAttributes.ReparsePoint)!=0)
                    throw new IOException("Linked paths are forbidden: " + item);
        }
    }
}