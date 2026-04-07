using UnityEngine;

namespace ProjectAstra.Core
{
    public static class OverlaySpriteFactory
    {
        private static Sprite _cachedSprite;

        const int TextureSize = 16;
        const int Border = 1;
        const float GradientStrength = 0.22f;

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

            int innerSize = TextureSize - 2 * Border;
            var pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    bool isEdge = x < Border || x >= TextureSize - Border
                               || y < Border || y >= TextureSize - Border;

                    if (isEdge)
                    {
                        pixels[y * TextureSize + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float nx = (float)(x - Border) / (innerSize - 1);
                    float ny = (float)(y - Border) / (innerSize - 1);

                    // Diagonal gradient: top-left bright, bottom-right dimmer
                    // y=0 is bottom in texture space, so invert for "top = bright"
                    float gradient = 1.0f - GradientStrength * ((nx + (1.0f - ny)) * 0.5f);
                    byte brightness = (byte)(Mathf.Clamp01(gradient) * 255);
                    pixels[y * TextureSize + x] = new Color32(brightness, brightness, brightness, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize
            );
        }
    }
}
