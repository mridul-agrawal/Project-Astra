using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Generates the placeholder cursor art at runtime: a corner bracket and an edge arrow,
    // both flat white shapes with a dark outline so a profile can tint them to any accent.
    // Deliberately geometric — the design brief asks for simplicity, and white-plus-tint
    // means one texture serves every variant.
    //
    // Cached statically, same as OverlaySpriteFactory. A profile that assigns its own sprite
    // never touches this.
    public static class CursorSpriteFactory
    {
        private const int TextureSize = 32;
        private const int PixelsPerUnit = 32;
        private const int Outline = 2;
        private const int ArmLength = 13;
        private const int ArmThickness = 5;

        private static readonly Color32 Fill = new(255, 255, 255, 255);
        private static readonly Color32 Edge = new(18, 16, 22, 255);
        private static readonly Color32 Empty = new(0, 0, 0, 0);

        private static Sprite cachedBracket;
        private static Sprite cachedArrow;

        // An L drawn into the top-right of the texture, pivoted at the texture centre so the
        // slot rotation swings it to any corner.
        public static Sprite GetBracketSprite()
        {
            if (cachedBracket == null) cachedBracket = Build(BracketMask);
            return cachedBracket;
        }

        // A solid triangle pointing up, same pivot convention as the bracket.
        public static Sprite GetArrowSprite()
        {
            if (cachedArrow == null) cachedArrow = Build(ArrowMask);
            return cachedArrow;
        }

        private static Sprite Build(System.Func<int, int, bool> mask)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            texture.SetPixels32(Rasterize(mask));
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // Anything inside the mask is fill; anything within the outline width of the mask
        // edge is the dark keyline. One pass, no separate stroke step.
        private static Color32[] Rasterize(System.Func<int, int, bool> mask)
        {
            var pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Color32 colour = Empty;
                    if (mask(x, y)) colour = Fill;
                    else if (IsNearMask(mask, x, y)) colour = Edge;
                    pixels[y * TextureSize + x] = colour;
                }
            }

            return pixels;
        }

        private static bool IsNearMask(System.Func<int, int, bool> mask, int x, int y)
        {
            for (int dy = -Outline; dy <= Outline; dy++)
            {
                for (int dx = -Outline; dx <= Outline; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= TextureSize || ny >= TextureSize) continue;
                    if (mask(nx, ny)) return true;
                }
            }
            return false;
        }

        private static bool BracketMask(int x, int y)
        {
            int right = TextureSize - Outline;
            int top = TextureSize - Outline;

            bool horizontalArm = y >= top - ArmThickness && y < top && x >= right - ArmLength && x < right;
            bool verticalArm = x >= right - ArmThickness && x < right && y >= top - ArmLength && y < top;
            return horizontalArm || verticalArm;
        }

        private static bool ArrowMask(int x, int y)
        {
            const int baseY = 10;
            const int tipY = TextureSize - Outline - 2;
            if (y < baseY || y > tipY) return false;

            float t = (float)(y - baseY) / (tipY - baseY);
            float halfWidth = Mathf.Lerp(9f, 0.5f, t);
            return Mathf.Abs(x - (TextureSize - 1) * 0.5f) <= halfWidth;
        }
    }
}
