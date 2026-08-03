using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Editor
{
    // Builds a placeholder animated environment PREFAB and wires it onto Map 1,
    // proving the prefab pipeline end to end without real art:
    //   - a flowing RIVER: a seamless tile on Ground, tiled across the water band
    //     (a base-layer animation that occludes the painted river),
    //   - swaying FLOWERS sharing one clip, phase-desynced by AmbientAnimator,
    //   - a swaying TREE (a non-tile-sized decoration, bottom-anchored),
    //   - a flapping BIRD on the Sky layer flying point-to-point over units.
    // A stand-in the designer replaces by authoring the real prefab in the
    // Environment Builder; the wiring (MapData.environmentPrefab) stays the same.
    public static class PlaceholderEnvironmentGenerator
    {
        private const string ArtRoot = "Assets/Art/Animation/Environment/Placeholder";
        private const string AnimRoot = "Assets/Animation/Environment/Placeholder";
        private const string PrefabPath = "Assets/Prefabs/Environment/map1_bridge.prefab";

        [MenuItem("Project Astra/Animation/Generate Placeholder Environment PREFAB (Map 1)")]
        public static void Generate()
        {
            MapData map = FindMap1();
            if (map == null) { Debug.LogError("[Animation] Could not find Map 1 MapData."); return; }

            WaterBand band = WaterBand.Detect(map);
            var controllers = BuildAllControllers();
            GameObject prefab = BuildPrefab(controllers, map.Width, band);
            WirePrefabToMap(map, prefab);

            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            Debug.Log($"[Animation] Placeholder environment prefab built for '{map.MapName}' " +
                      $"(river band y={band.MinY}..{band.MaxY}) → {PrefabPath}");
        }

        // ---- frame generation + controllers ----

        private static System.Collections.Generic.Dictionary<string, AnimatorController> BuildAllControllers()
        {
            WriteFrames("river", RiverFrame, 4);
            WriteFrames("flower", FlowerFrame, 4);
            WriteFrames("tree", TreeFrame, 3);
            WriteFrames("bird", BirdFrame, 2);
            AssetDatabase.Refresh();

            ConfigureFrames($"{ArtRoot}/river", SpriteAlignment.Center, fullRect: true);     // tileable base-layer
            ConfigureFrames($"{ArtRoot}/flower", SpriteAlignment.BottomCenter, false);        // sits on the ground
            ConfigureFrames($"{ArtRoot}/tree", SpriteAlignment.BottomCenter, false);
            ConfigureFrames($"{ArtRoot}/bird", SpriteAlignment.Center, false);

            return new System.Collections.Generic.Dictionary<string, AnimatorController>
            {
                ["river"] = LoopingAnimatorBuilder.BuildFromFolder("river", $"{ArtRoot}/river", 6, $"{AnimRoot}/river"),
                ["flower"] = LoopingAnimatorBuilder.BuildFromFolder("flower", $"{ArtRoot}/flower", 4, $"{AnimRoot}/flower"),
                ["tree"] = LoopingAnimatorBuilder.BuildFromFolder("tree", $"{ArtRoot}/tree", 3, $"{AnimRoot}/tree"),
                ["bird"] = LoopingAnimatorBuilder.BuildFromFolder("bird", $"{ArtRoot}/bird", 6, $"{AnimRoot}/bird"),
            };
        }

        private static void WriteFrames(string name, System.Func<int, byte[]> render, int count)
        {
            string folder = $"{ArtRoot}/{name}";
            Directory.CreateDirectory(AbsolutePath(folder));
            for (int i = 0; i < count; i++)
                File.WriteAllBytes(AbsolutePath($"{folder}/{name}_{i}.png"), render(i));
        }

        private static void ConfigureFrames(string folder, SpriteAlignment alignment, bool fullRect)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)alignment;
                if (fullRect) settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        // ---- prefab assembly ----

        private static GameObject BuildPrefab(
            System.Collections.Generic.Dictionary<string, AnimatorController> c, int mapWidth, WaterBand band)
        {
            var root = new GameObject("Environment");

            MakeDeco("River", root, c["river"], "Ground", 10,
                new Vector2(mapWidth / 2f, band.CenterY), new Vector2(mapWidth, band.Height), false);
            MakeDeco("Flower_A", root, c["flower"], "Object", 5, new Vector2(3.5f, 3f), Vector2.zero, false);
            MakeDeco("Flower_B", root, c["flower"], "Object", 5, new Vector2(6.5f, 3f), Vector2.zero, false);
            MakeDeco("Flower_C", root, c["flower"], "Object", 5, new Vector2(9.5f, 3f), Vector2.zero, false);
            MakeDeco("Tree", root, c["tree"], "Object", 0, new Vector2(13.5f, 2f), Vector2.zero, false);
            MakeDeco("Bird", root, c["bird"], "Sky", 0, new Vector2(2f, 7f), Vector2.zero, moves: true);

            EnsureFolder("Assets/Prefabs/Environment");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void MakeDeco(string name, GameObject parent, AnimatorController controller,
            string sortingLayer, int order, Vector2 localPos, Vector2 tileSize, bool moves)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FirstSprite(controller);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            if (tileSize != Vector2.zero) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = tileSize; }

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            go.AddComponent<AmbientAnimator>();                          // random phase → desynced loops
            if (moves) go.AddComponent<WaypointMover>();                 // straight A→B (bird)
        }

        private static void WirePrefabToMap(MapData map, GameObject prefab)
        {
            var so = new SerializedObject(map);
            so.FindProperty("environmentPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(map);
        }

        private static Sprite FirstSprite(AnimatorController controller)
        {
            if (controller == null || controller.animationClips.Length == 0) return null;
            AnimationClip clip = controller.animationClips[0];
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (bindings.Length == 0) return null;
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
            return keys.Length > 0 ? keys[0].value as Sprite : null;
        }

        private static MapData FindMap1()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MapData"))
            {
                var map = AssetDatabase.LoadAssetAtPath<MapData>(AssetDatabase.GUIDToAssetPath(guid));
                if (map != null && (map.MapId == "map1_bridge" || map.MapName.Contains("Bridge")))
                    return map;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ---- placeholder pixel art ----

        // A 32px seamless water tile (period 16 divides 32, so it tiles cleanly).
        private static byte[] RiverFrame(int frame)
        {
            const int s = 32;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = (x + frame * 8 + y) % 16 < 3
                        ? new Color32(120, 190, 255, 255)
                        : new Color32(38, 90, 210, 255);
            return Encode(px, s, s);
        }

        private static byte[] FlowerFrame(int frame)
        {
            const int s = 24;
            var px = Transparent(s, s);
            int sway = new[] { -2, 0, 2, 0 }[frame % 4];
            FillRect(px, s, 11, 2, 13, 13, new Color32(60, 160, 70, 255));       // stem
            Disc(px, s, 12 + sway, 16, 5, new Color32(230, 110, 180, 255));       // petals
            Disc(px, s, 12 + sway, 16, 2, new Color32(250, 220, 90, 255));        // center
            return Encode(px, s, s);
        }

        private static byte[] TreeFrame(int frame)
        {
            const int w = 48, h = 64;
            var px = Transparent(w, h);
            int sway = new[] { -1, 0, 1 }[frame % 3];
            FillRect(px, w, 22, 2, 26, 28, new Color32(120, 80, 45, 255));        // trunk
            Disc(px, w, 24 + sway, 42, 16, new Color32(50, 140, 60, 255));        // canopy
            Disc(px, w, 18 + sway, 38, 9, new Color32(70, 165, 80, 255));
            Disc(px, w, 31 + sway, 40, 9, new Color32(40, 125, 55, 255));
            return Encode(px, w, h);
        }

        private static byte[] BirdFrame(int frame)
        {
            const int s = 16;
            var px = Transparent(s, s);
            int dir = frame % 2 == 0 ? 1 : -1;                                     // wings up vs down
            var body = new Color32(60, 60, 70, 255);
            for (int k = 0; k <= 6; k++)
            {
                SetPx(px, s, 8 - k, 8 + dir * k, body);
                SetPx(px, s, 8 + k, 8 + dir * k, body);
                SetPx(px, s, 8 - k, 8 + dir * k + 1, body);
                SetPx(px, s, 8 + k, 8 + dir * k + 1, body);
            }
            return Encode(px, s, s);
        }

        // ---- tiny raster helpers ----

        private static Color32[] Transparent(int w, int h)
        {
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            return px;
        }

        private static void Disc(Color32[] px, int w, int cx, int cy, int r, Color32 c)
        {
            int h = px.Length / w;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * w + x] = c;
        }

        private static void FillRect(Color32[] px, int w, int x0, int y0, int x1, int y1, Color32 c)
        {
            int h = px.Length / w;
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                    px[y * w + x] = c;
        }

        private static void SetPx(Color32[] px, int w, int x, int y, Color32 c)
        {
            int h = px.Length / w;
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            px[y * w + x] = c;
        }

        private static byte[] Encode(Color32[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        private static string AbsolutePath(string assetPath) =>
            Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }
}
