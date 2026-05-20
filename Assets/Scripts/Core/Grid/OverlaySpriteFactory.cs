using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // Generates the 16×16 hover-highlight sprite at runtime: a 1-pixel transparent border with
    // a diagonal brightness gradient inside. Cached after first call — same sprite reused for
    // every overlay instance.
    public static class OverlaySpriteFactory
    {
        const int TextureSize = 16;
        const int Border = 1;
        const float GradientStrength = 0.22f;

        private static Sprite _cachedSprite;

        public static Sprite GetOverlaySprite()
        {
            if (_cachedSprite == null)
                _cachedSprite = CreateOverlaySprite();
            return _cachedSprite;
        }

        private static Sprite CreateOverlaySprite()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            texture.SetPixels32(BuildPixels());
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize
            );
        }

        private static Color32[] BuildPixels()
        {
            int innerSize = TextureSize - 2 * Border;
            var pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    pixels[y * TextureSize + x] = IsEdgePixel(x, y)
                        ? new Color32(0, 0, 0, 0)
                        : InteriorPixel(x, y, innerSize);
                }
            }

            return pixels;
        }

        private static bool IsEdgePixel(int x, int y) =>
            x < Border || x >= TextureSize - Border ||
            y < Border || y >= TextureSize - Border;

        // Diagonal gradient: top-left bright, bottom-right dimmer. Note y=0 is bottom in
        // texture space, so we invert ny to make "top" the bright corner.
        private static Color32 InteriorPixel(int x, int y, int innerSize)
        {
            float nx = (float)(x - Border) / (innerSize - 1);
            float ny = (float)(y - Border) / (innerSize - 1);
            float gradient = 1.0f - GradientStrength * ((nx + (1.0f - ny)) * 0.5f);
            byte brightness = (byte)(Mathf.Clamp01(gradient) * 255);
            return new Color32(brightness, brightness, brightness, 255);
        }
    }
}
