using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Bakes the handful of sprites the Unit Stat Screen spec needs that uGUI cannot draw itself.
    //
    // MPUIKit already draws rectangles, triangles, circles and hairline strokes procedurally, so
    // the only things baked here are the ones it has no shape for: the focus glow, the nine stat
    // glyphs, the gold weapon mark, and the dashed strips for empty gear slots.
    //
    // Everything is white with a shaped alpha, so a single Image.color tints it to whatever the
    // palette asks for - one sprite serves the ally accent, the enemy accent and every group hue.
    //
    // Run via 'Project Astra/Bake Unit Info Sprites'. Safe to re-run; it overwrites in place.
    // ==========================================================================================
    public static class UnitInfoSpecSprites
    {
        const string OutputDir = "Assets/UI/UnitInfoScreen/Generated";

        const int GlyphSize = 64;
        const int SamplesPerAxis = 4;       // 4x4 supersampling keeps the diagonals clean

        // The glow reaches 6 logical px past the focused row, which is 24px at the project's 4x.
        const int GlowReach = 24;
        const float GlowPeakAlpha = 0.55f;

        [MenuItem("Project Astra/Bake Unit Info Sprites")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutputDir);

            BakeGlow();
            BakeGlyphs();
            BakeDashStrips();
            BakeBarFill();
            BakePlaceholderBust();

            AssetDatabase.Refresh();
            Debug.Log($"[UnitInfoSpecSprites] Baked spec sprites into {OutputDir}.");
        }

        // ---- focus glow ----------------------------------------------------------------------

        // A 9-sliced halo. Alpha peaks where the focused row's own edge sits and fades outward,
        // so the marker rect is the row inflated by GlowReach and the crisp 1px border is drawn
        // separately as an MPUIKit stroke.
        static void BakeGlow()
        {
            int size = GlowReach * 2 + 2;    // 2px stretchable centre
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color(1f, 1f, 1f, GlowAlphaAt(x, y, size));

            WriteSprite("focus_glow", size, size, pixels, border: GlowReach, tiled: false);
        }

        static float GlowAlphaAt(int x, int y, int size)
        {
            float outward = DistanceOutsideInnerRect(x, y, size);
            if (outward <= 0f) return 0f;

            float falloff = 1f - Mathf.Clamp01(outward / GlowReach);
            return GlowPeakAlpha * falloff * falloff;
        }

        // How far the pixel sits outside the rect inset by GlowReach - 0 anywhere inside it.
        static float DistanceOutsideInnerRect(int x, int y, int size)
        {
            float dx = Mathf.Max(GlowReach - (x + 0.5f), (x + 0.5f) - (size - GlowReach));
            float dy = Mathf.Max(GlowReach - (y + 0.5f), (y + 0.5f) - (size - GlowReach));
            dx = Mathf.Max(dx, 0f);
            dy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ---- stat glyphs ---------------------------------------------------------------------

        // §6 asks for geometric marks. Baking all nine keeps one code path in the restyle pass
        // instead of six different MPUIKit shape structs with rotations.
        static void BakeGlyphs()
        {
            BakeGlyph("glyph_triangle_up", UpTriangle());
            BakeGlyph("glyph_triangle_down", DownTriangle());
            BakeGlyph("glyph_triangle_right", RightTriangle());
            BakeGlyph("glyph_diamond", Diamond());
            BakeGlyph("glyph_square", Square());
            BakeGlyph("glyph_pentagon", Pentagon());
            BakeGlyph("glyph_ring", Ring());
            BakeGlyph("glyph_plus", Plus());
            BakeGlyph("glyph_double_chevron", DoubleChevron());
            BakeGlyph("glyph_blade", Blade());
        }

        static void BakeGlyph(string name, Func<Vector2, bool> covers)
        {
            var pixels = new Color[GlyphSize * GlyphSize];

            for (int y = 0; y < GlyphSize; y++)
            for (int x = 0; x < GlyphSize; x++)
                pixels[y * GlyphSize + x] = new Color(1f, 1f, 1f, Coverage(covers, x, y));

            WriteSprite(name, GlyphSize, GlyphSize, pixels, border: 0, tiled: false);
        }

        // Supersampled so the shape gets an antialiased edge rather than a stair-stepped one.
        static float Coverage(Func<Vector2, bool> covers, int x, int y)
        {
            int hits = 0;
            for (int sy = 0; sy < SamplesPerAxis; sy++)
            for (int sx = 0; sx < SamplesPerAxis; sx++)
            {
                float u = (x + (sx + 0.5f) / SamplesPerAxis) / GlyphSize;
                float v = (y + (sy + 0.5f) / SamplesPerAxis) / GlyphSize;
                if (covers(new Vector2(u, v))) hits++;
            }
            return (float)hits / (SamplesPerAxis * SamplesPerAxis);
        }

        // ---- glyph shapes, in a normalised 0..1 box with y pointing up -----------------------

        static Func<Vector2, bool> UpTriangle() =>
            InPolygon(new Vector2(0.5f, 0.95f), new Vector2(0.95f, 0.08f), new Vector2(0.05f, 0.08f));

        static Func<Vector2, bool> DownTriangle() =>
            InPolygon(new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.92f), new Vector2(0.5f, 0.05f));

        static Func<Vector2, bool> RightTriangle() =>
            InPolygon(new Vector2(0.12f, 0.95f), new Vector2(0.92f, 0.5f), new Vector2(0.12f, 0.05f));

        static Func<Vector2, bool> Diamond() =>
            InPolygon(new Vector2(0.5f, 0.97f), new Vector2(0.97f, 0.5f),
                      new Vector2(0.5f, 0.03f), new Vector2(0.03f, 0.5f));

        static Func<Vector2, bool> Square() =>
            p => p.x >= 0.1f && p.x <= 0.9f && p.y >= 0.1f && p.y <= 0.9f;

        static Func<Vector2, bool> Pentagon() => InPolygon(RegularPolygon(5, 0.47f, 90f));

        static Func<Vector2, bool> Ring()
        {
            const float outer = 0.45f, inner = 0.27f;
            return p =>
            {
                float d = (p - new Vector2(0.5f, 0.5f)).magnitude;
                return d <= outer && d >= inner;
            };
        }

        static Func<Vector2, bool> Plus()
        {
            const float thin = 0.36f, thick = 0.64f, end = 0.94f, start = 0.06f;
            return p =>
                (p.x >= start && p.x <= end && p.y >= thin && p.y <= thick) ||
                (p.y >= start && p.y <= end && p.x >= thin && p.x <= thick);
        }

        // The MOV mark - two nested arrowheads, like a guillemet.
        static Func<Vector2, bool> DoubleChevron()
        {
            var near = Chevron(0.06f);
            var far = Chevron(0.46f);
            return p => near(p) || far(p);
        }

        static Func<Vector2, bool> Chevron(float x)
        {
            const float thickness = 0.14f;
            return InPolygon(
                new Vector2(x, 0.92f),
                new Vector2(x + 0.42f, 0.5f),
                new Vector2(x, 0.08f),
                new Vector2(x, 0.08f + thickness),
                new Vector2(x + 0.42f - thickness * 1.4f, 0.5f),
                new Vector2(x, 0.92f - thickness));
        }

        // §5 only calls for "a gold geometric glyph", so this is a plain blade silhouette.
        static Func<Vector2, bool> Blade() =>
            InPolygon(new Vector2(0.5f, 0.97f), new Vector2(0.68f, 0.62f), new Vector2(0.6f, 0.16f),
                      new Vector2(0.4f, 0.16f), new Vector2(0.32f, 0.62f));

        static Vector2[] RegularPolygon(int sides, float radius, float startDegrees)
        {
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.Deg2Rad * (startDegrees + 360f * i / sides);
                points[i] = new Vector2(0.5f + radius * Mathf.Cos(angle), 0.5f + radius * Mathf.Sin(angle));
            }
            return points;
        }

        static Func<Vector2, bool> InPolygon(params Vector2[] points) => p =>
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                bool straddles = points[i].y > p.y != points[j].y > p.y;
                if (!straddles) continue;

                float crossingX = points[i].x +
                    (p.y - points[i].y) / (points[j].y - points[i].y) * (points[j].x - points[i].x);
                if (p.x < crossingX) inside = !inside;
            }
            return inside;
        };

        // ---- dashed strips -------------------------------------------------------------------

        // Empty gear slots get a dashed edge. A 9-sliced dashed frame would smear its dashes when
        // stretched, so the restyle pass tiles these two strips along the four edges instead.
        static void BakeDashStrips()
        {
            const int dash = 8, gap = 8, thickness = 4;
            int length = dash + gap;

            var horizontal = new Color[length * thickness];
            for (int y = 0; y < thickness; y++)
            for (int x = 0; x < length; x++)
                horizontal[y * length + x] = new Color(1f, 1f, 1f, x < dash ? 1f : 0f);
            WriteSprite("dash_horizontal", length, thickness, horizontal, border: 0, tiled: true);

            var vertical = new Color[thickness * length];
            for (int y = 0; y < length; y++)
            for (int x = 0; x < thickness; x++)
                vertical[y * thickness + x] = new Color(1f, 1f, 1f, y < dash ? 1f : 0f);
            WriteSprite("dash_vertical", thickness, length, vertical, border: 0, tiled: true);
        }

        // ---- bar fill ------------------------------------------------------------------------

        // Plain white, and the reason it exists at all: a uGUI Image set to Filled needs a sprite
        // to fill. Without one it draws the whole quad and fillAmount is quietly ignored, which
        // reads as every bar sitting at 100%.
        static void BakeBarFill()
        {
            const int size = 8;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

            WriteSprite("bar_fill", size, size, pixels, border: 0, tiled: false);
        }

        // ---- placeholder bust ----------------------------------------------------------------

        // §4's stand-in portrait, in the tones the spec names, so the frame does not read as empty
        // until real art lands.
        static void BakePlaceholderBust()
        {
            const int w = 256, h = 248;
            var background = new Color32(0x25, 0x2b, 0x36, 0xff);
            var shoulders  = new Color32(0x59, 0x63, 0x7a, 0xff);
            var collar     = new Color32(0xae, 0xb6, 0xc4, 0xff);
            var neck       = new Color32(0x7d, 0x87, 0x97, 0xff);
            var head       = new Color32(0x97, 0xa1, 0xb0, 0xff);
            var hair       = new Color32(0x39, 0x41, 0x4f, 0xff);

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float v = (y + 0.5f) / h;          // 0 at the bottom
                pixels[y * w + x] = BustTone(u, v, background, shoulders, collar, neck, head, hair);
            }

            WriteSprite("bust_placeholder", w, h, pixels, border: 0, tiled: false);
        }

        static Color BustTone(float u, float v, Color background, Color shoulders, Color collar,
                              Color neck, Color head, Color hair)
        {
            const float headY = 0.66f, headR = 0.20f;

            float dx = (u - 0.5f) * 1.15f;         // the head reads round on a non-square canvas
            float headDist = Mathf.Sqrt(dx * dx + (v - headY) * (v - headY));
            if (headDist <= headR)
                return v > headY + headR * 0.28f ? hair : head;

            if (Mathf.Abs(u - 0.5f) < 0.085f && v > 0.34f && v < headY)
                return neck;

            float shoulderDist = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) * 0.85f + (v - 0.02f) * (v - 0.02f));
            if (shoulderDist <= 0.40f)
                return shoulderDist > 0.385f ? collar : shoulders;

            return background;
        }

        // ---- writing -------------------------------------------------------------------------

        static void WriteSprite(string name, int width, int height, Color[] pixels, int border, bool tiled)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            string path = $"{OutputDir}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path, border, tiled);
        }

        static void ConfigureImporter(string path, int border, bool tiled)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
