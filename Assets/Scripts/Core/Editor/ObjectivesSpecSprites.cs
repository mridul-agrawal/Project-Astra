using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Bakes the two sprites the objectives panel spec needs.
    //
    // The bullseye is baked rather than assembled from three MPUIKit circles because §A2 gives it
    // fractional radii (7.1 and 3.7 logical px) - supersampling lands those exactly, where three
    // stacked procedural rings would each round.
    //
    // The check mark is baked because U+2713 is absent from Noto Sans at all three weights, the
    // same situation as the tile panel's U+2794 chevron.
    //
    // The plate gradient is NOT baked here: §A3 and §B5 both ask for #090D1AEB -> #0F162ACC, which
    // is byte-identical to the tile panel's plate, so that sprite is reused.
    //
    // Run via 'Project Astra/Bake Objectives Sprites'. Safe to re-run.
    // ==========================================================================================
    public static class ObjectivesSpecSprites
    {
        const string OutputDir = "Assets/UI/BattleMapHUD/Generated";

        const float Scale = 4f;
        static float Sc(float logical) => logical * Scale;

        const int SamplesPerAxis = 4;

        [MenuItem("Project Astra/Bake Objectives Sprites")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutputDir);

            BakeBullseye();
            BakeCheck();

            AssetDatabase.Refresh();
            Debug.Log("[ObjectivesSpecSprites] Baked objectives sprites into " + OutputDir);
        }

        // ---- bullseye ------------------------------------------------------------------------

        // §A2: 17px overall, outer ring r 7.1 stroke 1.4, middle ring r 3.7 stroke 1.4, dot r 1.4.
        // Rings and dot are separate alpha channels of the same shape, so the restyle pass can tint
        // the rings white-70% and the dot cyan - they are drawn as two sprites over each other.
        static void BakeBullseye()
        {
            int size = Mathf.RoundToInt(Sc(17f));

            BakeRadial(size, "objectives_bullseye_rings", (d, half) =>
                InRing(d, Sc(7.1f), Sc(1.4f)) || InRing(d, Sc(3.7f), Sc(1.4f)));

            BakeRadial(size, "objectives_bullseye_dot", (d, half) => d <= Sc(1.4f));
        }

        static void BakeRadial(int size, string name, System.Func<float, float, bool> covers)
        {
            var pixels = new Color[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color(1f, 1f, 1f, Coverage(x, y, half, covers));

            WriteSprite(name, size, size, pixels);
        }

        // A stroked ring: inside the outer radius, outside the inner one.
        static bool InRing(float distance, float radius, float stroke) =>
            distance <= radius && distance >= radius - stroke;

        static float Coverage(int x, int y, float half, System.Func<float, float, bool> covers)
        {
            int hits = 0;
            for (int sy = 0; sy < SamplesPerAxis; sy++)
            for (int sx = 0; sx < SamplesPerAxis; sx++)
            {
                float px = x + (sx + 0.5f) / SamplesPerAxis - half;
                float py = y + (sy + 0.5f) / SamplesPerAxis - half;
                if (covers(Mathf.Sqrt(px * px + py * py), half)) hits++;
            }
            return (float)hits / (SamplesPerAxis * SamplesPerAxis);
        }

        // ---- check mark ----------------------------------------------------------------------

        // §B4's tick, drawn as two strokes of a polyline so it reads at 28px.
        static void BakeCheck()
        {
            int size = Mathf.RoundToInt(Sc(7f));
            var pixels = new Color[size * size];

            var a = new Vector2(0.14f, 0.52f);
            var b = new Vector2(0.42f, 0.24f);
            var c = new Vector2(0.86f, 0.74f);
            float thickness = 0.15f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < SamplesPerAxis; sy++)
                for (int sx = 0; sx < SamplesPerAxis; sx++)
                {
                    var p = new Vector2((x + (sx + 0.5f) / SamplesPerAxis) / size,
                                        (y + (sy + 0.5f) / SamplesPerAxis) / size);
                    if (NearSegment(p, a, b, thickness) || NearSegment(p, b, c, thickness)) hits++;
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f,
                    (float)hits / (SamplesPerAxis * SamplesPerAxis));
            }

            WriteSprite("objectives_check", size, size, pixels);
        }

        static bool NearSegment(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t) <= thickness * 0.5f;
        }

        // ---- writing -------------------------------------------------------------------------

        static void WriteSprite(string name, int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            string path = $"{OutputDir}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

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
