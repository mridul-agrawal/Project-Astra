using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Editor
{
    // Generates obviously-placeholder unit frames (a bobbing, pulsing disc, one
    // colour per state) and runs them through the setup tool to prove the whole
    // animation pipeline end to end without any real art. Each state's distinct
    // colour makes the facing switch easy to read: run north = green, east =
    // yellow, south = red, west = cyan, idle = blue, selected = gold. Swap the
    // resulting override controller for real PixelLab art any time.
    public static class PlaceholderUnitAnimationGenerator
    {
        private const string Folder = "Assets/Art/Animation/Units/Placeholder";
        private const int Size = 32;

        [MenuItem("Project Astra/Animation/Generate Placeholder Unit (frames + AOC)")]
        public static void Generate()
        {
            Directory.CreateDirectory(AbsolutePath(Folder));

            var written = new List<string>();
            written.AddRange(WriteState("idle", new Color(0.35f, 0.55f, 1f), null, 2));
            written.AddRange(WriteState("run_n", new Color(0.3f, 0.9f, 0.4f), Facing.North, 4));
            written.AddRange(WriteState("run_e", new Color(1f, 0.85f, 0.2f), Facing.East, 4));
            written.AddRange(WriteState("run_s", new Color(1f, 0.4f, 0.35f), Facing.South, 4));
            written.AddRange(WriteState("run_w", new Color(0.3f, 0.85f, 0.9f), Facing.West, 4));
            written.AddRange(WriteState("selected", new Color(1f, 0.82f, 0.28f), null, 2));

            AssetDatabase.Refresh();
            foreach (string path in written)
                ForceSingleSprite(path);

            UnitAnimatorSetupTool.BuildFrom("Placeholder", Folder, 8);
        }

        // Placeholder frames are one sprite per file; pin that so the base name
        // stays "state_N" for the setup tool to group by.
        private static void ForceSingleSprite(string assetPath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            else
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static IEnumerable<string> WriteState(string state, Color body, Facing? arrow, int frameCount)
        {
            var paths = new List<string>();
            for (int frame = 0; frame < frameCount; frame++)
            {
                string assetPath = $"{Folder}/{state}_{frame}.png";
                File.WriteAllBytes(AbsolutePath(assetPath), RenderFrame(body, arrow, frame, frameCount));
                paths.Add(assetPath);
            }
            return paths;
        }

        private static byte[] RenderFrame(Color body, Facing? arrow, int frame, int frameCount)
        {
            var pixels = new Color32[Size * Size];   // starts fully transparent

            int bob = frame % 2 == 0 ? 0 : 2;
            float pulse = 0.7f + 0.3f * (frameCount > 1 ? frame / (float)(frameCount - 1) : 1f);
            Color tint = body * pulse;

            DrawDisc(pixels, 16, 13 + bob, 9, tint);
            if (arrow.HasValue) DrawArrow(pixels, arrow.Value, bob);

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        private static void DrawDisc(Color32[] pixels, int cx, int cy, int radius, Color color)
        {
            var solid = (Color32)new Color(color.r, color.g, color.b, 1f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                        pixels[y * Size + x] = solid;
        }

        private static void DrawArrow(Color32[] pixels, Facing facing, int bob)
        {
            Vector2Int center = facing switch
            {
                Facing.North => new Vector2Int(16, 26 + bob),
                Facing.South => new Vector2Int(16, 4 + bob),
                Facing.East => new Vector2Int(26, 14 + bob),
                Facing.West => new Vector2Int(6, 14 + bob),
                _ => new Vector2Int(16, 16)
            };
            var white = new Color32(255, 255, 255, 255);
            for (int y = -2; y <= 2; y++)
                for (int x = -2; x <= 2; x++)
                    SetPixel(pixels, center.x + x, center.y + y, white);
        }

        private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            pixels[y * Size + x] = color;
        }

        private static string AbsolutePath(string assetPath) =>
            Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }
}
