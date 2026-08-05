using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Draws a CursorShape into a bracket sprite and an arrow sprite, and redraws them when the
    // shape changes. Owns exactly two textures for the lifetime of the cursor and rewrites them
    // in place — a slider drag would otherwise leak a texture per frame.
    //
    // Shapes are supersampled rather than drawn pixel-by-pixel: this is HD art, so the edges
    // want to be smooth, not stair-stepped.
    public class CursorShapeRenderer
    {
        private const int TextureSize = 64;

        // 256 px/unit over a 64 px texture puts one piece at a quarter of a tile, and keeps it
        // crisp when a profile scales it up.
        private const int PixelsPerUnit = 256;
        private const int SuperSample = 3;

        private static readonly Color FillColour = Color.white;
        private static readonly Color OutlineColour = new(0.07f, 0.06f, 0.09f, 1f);

        private Texture2D bracketTexture;
        private Texture2D arrowTexture;
        private Sprite bracketSprite;
        private Sprite arrowSprite;

        private CursorShape lastShape;
        private bool hasDrawn;

        public Sprite BracketSprite => bracketSprite;
        public Sprite ArrowSprite => arrowSprite;

        public void Refresh(in CursorShape shape)
        {
            if (hasDrawn && lastShape.Matches(shape)) return;

            EnsureTextures();
            Draw(bracketTexture, shape, bracket: true);
            Draw(arrowTexture, shape, bracket: false);

            lastShape = shape;
            hasDrawn = true;
        }

        public void Dispose()
        {
            if (bracketTexture != null) Object.Destroy(bracketTexture);
            if (arrowTexture != null) Object.Destroy(arrowTexture);
            bracketTexture = arrowTexture = null;
            bracketSprite = arrowSprite = null;
            hasDrawn = false;
        }

        private void EnsureTextures()
        {
            if (bracketTexture != null) return;

            bracketTexture = NewTexture();
            arrowTexture = NewTexture();
            bracketSprite = NewSprite(bracketTexture);
            arrowSprite = NewSprite(arrowTexture);
        }

        private static Texture2D NewTexture() =>
            new(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

        private static Sprite NewSprite(Texture2D texture) =>
            Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);

        private static void Draw(Texture2D texture, in CursorShape shape, bool bracket)
        {
            var pixels = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                    pixels[y * TextureSize + x] = SampleWithCoverage(x, y, shape, bracket);
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        // Averages a grid of sub-samples per pixel. Fill wins over outline wherever they
        // overlap, so the keyline always sits outside the shape rather than eating into it.
        private static Color SampleWithCoverage(int px, int py, in CursorShape shape, bool bracket)
        {
            float fill = 0f, outline = 0f;
            const float step = 1f / (SuperSample + 1);

            for (int sy = 1; sy <= SuperSample; sy++)
            {
                for (int sx = 1; sx <= SuperSample; sx++)
                {
                    float u = (px + sx * step) / TextureSize;
                    float v = (py + sy * step) / TextureSize;

                    float distance = bracket
                        ? BracketDistance(u, v, shape)
                        : ArrowDistance(u, v, shape);

                    if (distance <= 0f) fill++;
                    else if (distance <= shape.outlineWeight) outline++;
                }
            }

            int total = SuperSample * SuperSample;
            float fillAlpha = fill / total;
            float outlineAlpha = outline / total;

            if (fillAlpha <= 0f && outlineAlpha <= 0f) return Color.clear;

            Color colour = Color.Lerp(OutlineColour, FillColour, fillAlpha / Mathf.Max(fillAlpha + outlineAlpha, 0.0001f));
            colour.a = Mathf.Clamp01(fillAlpha + outlineAlpha);
            return colour;
        }

        // Signed distance to an L sitting in the top-right of the texture: negative inside,
        // positive outside. Two rounded boxes unioned, which is what gives the corner radius.
        private static float BracketDistance(float u, float v, in CursorShape shape)
        {
            float margin = shape.outlineWeight + 0.02f;
            float outer = 1f - margin;
            float inner = outer - shape.thickness;
            float reach = outer - shape.armLength;

            float horizontal = RoundedBox(u, v,
                reach, inner, outer, outer, shape.cornerRadius * shape.thickness);
            float vertical = RoundedBox(u, v,
                inner, reach, outer, outer, shape.cornerRadius * shape.thickness);

            return Mathf.Min(horizontal, vertical);
        }

        // Distance to an isosceles triangle pointing up. Sharpness pulls the base corners
        // downward, which is what turns a blunt chevron into a needle.
        private static float ArrowDistance(float u, float v, in CursorShape shape)
        {
            float margin = shape.outlineWeight + 0.02f;
            float tipY = 1f - margin;
            float baseY = margin + (1f - shape.arrowSharpness) * 0.25f;
            if (v < baseY || v > tipY) return DistanceOutsideBand(v, baseY, tipY);

            float t = (v - baseY) / Mathf.Max(tipY - baseY, 0.0001f);
            float halfWidth = Mathf.Lerp(shape.arrowWidth * 0.5f, 0.01f, Mathf.Pow(t, 1f - shape.arrowSharpness * 0.5f));

            return Mathf.Abs(u - 0.5f) - halfWidth;
        }

        private static float DistanceOutsideBand(float v, float low, float high) =>
            v < low ? low - v : v - high;

        private static float RoundedBox(float u, float v, float minX, float minY, float maxX, float maxY, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(maxX - minX, maxY - minY) * 0.5f);

            float centreX = Mathf.Clamp(u, minX + radius, maxX - radius);
            float centreY = Mathf.Clamp(v, minY + radius, maxY - radius);

            float dx = u - centreX;
            float dy = v - centreY;
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }
    }
}
