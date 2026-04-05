using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    public static class PlaceholderCursorGenerator
    {
        private const int TileSize = 16;
        private const string OutputFolder = "Assets/Art/Cursor";

        [MenuItem("Project Astra/Map/Generate Placeholder Cursor & Unit Sprites")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);
            GenerateCursorSprite();
            GenerateUnitCircleSprite();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Placeholder cursor and unit sprites generated.");
        }

        private static void GenerateCursorSprite()
        {
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color32[TileSize * TileSize];
            var cyan = new Color32(0, 255, 255, 255);
            var transparent = new Color32(0, 0, 0, 0);

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = transparent;

            // Draw 1-pixel outline border
            for (int i = 0; i < TileSize; i++)
            {
                pixels[i] = cyan;                              // bottom row
                pixels[(TileSize - 1) * TileSize + i] = cyan;  // top row
                pixels[i * TileSize] = cyan;                    // left column
                pixels[i * TileSize + (TileSize - 1)] = cyan;  // right column
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            SaveSprite(texture, $"{OutputFolder}/PlaceholderCursor.png");
            Object.DestroyImmediate(texture);
        }

        private static void GenerateUnitCircleSprite()
        {
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color32[TileSize * TileSize];
            var transparent = new Color32(0, 0, 0, 0);
            var blue = new Color32(60, 100, 220, 255);

            float center = (TileSize - 1) / 2f;
            float radius = TileSize / 2f - 1.5f;

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    pixels[y * TileSize + x] = (dx * dx + dy * dy <= radius * radius)
                        ? blue : transparent;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            SaveSprite(texture, $"{OutputFolder}/PlaceholderUnitCircle.png");
            Object.DestroyImmediate(texture);
        }

        private static void SaveSprite(Texture2D texture, string path)
        {
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), texture.EncodeToPNG());
            AssetDatabase.Refresh();

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TileSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
