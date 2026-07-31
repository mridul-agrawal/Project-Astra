using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Editor
{
    // Builds a placeholder animated Environment prefab and hangs it on Map 1, to
    // prove the environment + base-layer pipeline end to end without real art:
    //   - a flowing RIVER patch sized to the map's water band (a base-layer
    //     animation that occludes the painted river and reads as animated ground),
    //   - swaying FLOWERS sharing one clip (AmbientAnimator phase-offsets them so
    //     they don't sway in lockstep),
    //   - a swaying TREE (a non-tile-sized decoration),
    //   - a flapping BIRD that flies point-to-point (WaypointMover).
    // Swap any controller for real PixelLab art later; the prefab wiring stays.
    public static class PlaceholderEnvironmentGenerator
    {
        private const string ArtRoot = "Assets/Art/Animation/Environment/Placeholder";
        private const string AnimRoot = "Assets/Animation/Environment/Placeholder";
        private const string PrefabPath = "Assets/Animation/Environment/PlaceholderEnvironment.prefab";

        [MenuItem("Project Astra/Animation/Generate Placeholder Environment (Map 1)")]
        public static void Generate()
        {
            MapData map = FindMap1();
            if (map == null) { Debug.LogError("[Animation] Could not find Map 1 MapData."); return; }

            RiverBand band = DetectWaterBand(map);
            var controllers = BuildAllControllers(map.Width, band.Height);
            GameObject prefab = BuildPrefab(controllers, map.Width, band);
            AssignToMap(map, prefab);

            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            Debug.Log($"[Animation] Placeholder environment built and assigned to '{map.MapName}' " +
                      $"(river band y={band.MinY}..{band.MaxY}).");
        }

        // ---- frame generation + controllers ----

        private static Dictionary<string, AnimatorController> BuildAllControllers(int mapWidth, int bandHeight)
        {
            WriteFrames("river", i => RiverFrame(i, mapWidth * 32, bandHeight * 32), 4);
            WriteFrames("flower", i => FlowerFrame(i), 4);
            WriteFrames("tree", i => TreeFrame(i), 3);
            WriteFrames("bird", i => BirdFrame(i), 2);
            AssetDatabase.Refresh();

            return new Dictionary<string, AnimatorController>
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
            {
                string assetPath = $"{folder}/{name}_{i}.png";
                File.WriteAllBytes(AbsolutePath(assetPath), render(i));
            }
        }

        // ---- prefab assembly ----

        private static GameObject BuildPrefab(Dictionary<string, AnimatorController> controllers, int mapWidth, RiverBand band)
        {
            var root = new GameObject("PlaceholderEnvironment");

            // Base-layer river: on Ground above the painted PNG, below everything else.
            MakeDeco("River", root, controllers["river"], "Ground", 10,
                new Vector3(mapWidth / 2f, band.CenterY, 0f), ambient: true);

            // Decorations on the Object layer.
            MakeDeco("Flower_A", root, controllers["flower"], "Object", 5, new Vector3(3.5f, 3.5f, 0f), true);
            MakeDeco("Flower_B", root, controllers["flower"], "Object", 5, new Vector3(6.5f, 3.5f, 0f), true);
            MakeDeco("Flower_C", root, controllers["flower"], "Object", 5, new Vector3(9.5f, 3.5f, 0f), true);
            MakeDeco("Tree", root, controllers["tree"], "Object", 0, new Vector3(13.5f, 2.5f, 0f), true);

            var bird = MakeDeco("Bird", root, controllers["bird"], "Object", 50, new Vector3(2f, 7f, 0f), true);
            bird.AddComponent<WaypointMover>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject MakeDeco(string name, GameObject parent, AnimatorController controller,
            string sortingLayer, int order, Vector3 localPos, bool ambient)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.sprite = FirstSprite(controller);   // seed so it shows before the Animator ticks

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (ambient) go.AddComponent<AmbientAnimator>();
            return go;
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

        private static void AssignToMap(MapData map, GameObject prefab)
        {
            var so = new SerializedObject(map);
            so.FindProperty("environmentPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(map);
        }

        // ---- map + water band ----

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

        private static RiverBand DetectWaterBand(MapData map)
        {
            int minY = int.MaxValue, maxY = int.MinValue;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (IsWater(map.TerrainAt(x, y))) { minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }

            if (maxY < minY) { minY = map.Height / 2; maxY = minY; }   // no water: fall back to the middle row
            return new RiverBand { MinY = minY, MaxY = maxY };
        }

        private static bool IsWater(TerrainType t) =>
            t == TerrainType.Water || t == TerrainType.River || t == TerrainType.Sea;

        private struct RiverBand
        {
            public int MinY, MaxY;
            public int Height => MaxY - MinY + 1;
            public float CenterY => (MinY + MaxY + 1) / 2f;
        }

        // ---- placeholder pixel art ----

        private static byte[] RiverFrame(int frame, int w, int h)
        {
            var px = Transparent(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = new Color32(38, 90, 210, 255);
                    if ((x + frame * 10 + y) % 20 < 3) c = new Color32(120, 190, 255, 255);
                    px[y * w + x] = c;
                }
            return Encode(px, w, h);
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
