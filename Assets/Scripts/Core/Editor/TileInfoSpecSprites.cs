using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Bakes the three sprites the tile info spec needs.
    //
    // The two gradients carry their colour AND alpha in the pixels rather than relying on an Image
    // tint. §5 gives each gradient a different colour and a different opacity at each stop, and a
    // single tint cannot express two alphas - so the stops are baked and the Image draws at white.
    //
    // The chevron is a sprite because §6 asks for U+2794, which Noto Sans does not contain at any
    // of the three weights; as a font glyph it renders as tofu.
    //
    // Run via 'Project Astra/Bake Tile Info Sprites'. Safe to re-run.
    // ==========================================================================================
    public static class TileInfoSpecSprites
    {
        const string OutputDir = "Assets/UI/BattleMapHUD/Generated";

        // Tall enough that the ramp is smooth once stretched over the panel.
        const int GradientWidth = 8;
        const int GradientHeight = 64;

        const int ChevronSize = 64;
        const int SamplesPerAxis = 4;

        [MenuItem("Project Astra/Bake Tile Info Sprites")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutputDir);

            // §5 name plate: 92% down to 80%.
            BakeGradient("plate_gradient",
                new Color32(0x09, 0x0D, 0x1A, 0xEB),
                new Color32(0x0F, 0x16, 0x2A, 0xCC));

            // §5 effect strip: deliberately lighter, 78% down to 62%.
            BakeGradient("strip_gradient",
                new Color32(0x09, 0x0D, 0x1A, 0xC7),
                new Color32(0x0F, 0x16, 0x2A, 0x9E));

            BakeChevron();

            AssetDatabase.Refresh();
            Debug.Log("[TileInfoSpecSprites] Baked tile info sprites into " + OutputDir);
        }

        // ---- gradients -----------------------------------------------------------------------

        static void BakeGradient(string name, Color top, Color bottom)
        {
            var pixels = new Color[GradientWidth * GradientHeight];

            for (int y = 0; y < GradientHeight; y++)
            {
                // Row 0 is the bottom of a Unity texture, so the ramp runs bottom stop to top stop.
                float t = (y + 0.5f) / GradientHeight;
                Color row = LerpStraight(bottom, top, t);
                for (int x = 0; x < GradientWidth; x++)
                    pixels[y * GradientWidth + x] = row;
            }

            WriteSprite(name, GradientWidth, GradientHeight, pixels);
        }

        // Colour and alpha are interpolated independently. Unity's Color.Lerp would do the same,
        // but naming it says why the alpha ramp is intentional rather than a side effect.
        static Color LerpStraight(Color a, Color b, float t) => new Color(
            Mathf.Lerp(a.r, b.r, t),
            Mathf.Lerp(a.g, b.g, t),
            Mathf.Lerp(a.b, b.b, t),
            Mathf.Lerp(a.a, b.a, t));

        // ---- chevron -------------------------------------------------------------------------

        // The U+2794 silhouette: a short thick shaft into a large triangular head.
        static void BakeChevron()
        {
            var pixels = new Color[ChevronSize * ChevronSize];

            for (int y = 0; y < ChevronSize; y++)
            for (int x = 0; x < ChevronSize; x++)
                pixels[y * ChevronSize + x] = new Color(1f, 1f, 1f, Coverage(x, y));

            WriteSprite("chevron_arrow", ChevronSize, ChevronSize, pixels);
        }

        static float Coverage(int x, int y)
        {
            int hits = 0;
            for (int sy = 0; sy < SamplesPerAxis; sy++)
            for (int sx = 0; sx < SamplesPerAxis; sx++)
            {
                float u = (x + (sx + 0.5f) / SamplesPerAxis) / ChevronSize;
                float v = (y + (sy + 0.5f) / SamplesPerAxis) / ChevronSize;
                if (InArrow(u, v)) hits++;
            }
            return (float)hits / (SamplesPerAxis * SamplesPerAxis);
        }

        static bool InArrow(float u, float v)
        {
            const float shaftTop = 0.62f, shaftBottom = 0.38f;
            const float shaftStart = 0.04f, headStart = 0.44f, tip = 0.96f;
            const float headTop = 0.88f, headBottom = 0.12f;

            if (u >= shaftStart && u <= headStart && v <= shaftTop && v >= shaftBottom)
                return true;

            if (u < headStart || u > tip) return false;

            // The head narrows linearly from its base to the tip.
            float along = (u - headStart) / (tip - headStart);
            float halfHeight = Mathf.Lerp((headTop - headBottom) * 0.5f, 0f, along);
            return Mathf.Abs(v - 0.5f) <= halfHeight;
        }

        // ---- writing -------------------------------------------------------------------------

        static void WriteSprite(string name, int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            string path = $"{OutputDir}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Configure(path);
        }

        static void Configure(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
