namespace ProjectAstra.Core.Grid
{
    // The vertical span of water rows on a map — used to size and place a base-layer
    // river patch over exactly the water. Pure, so it can be unit-tested and shared
    // by the Map Editor's "Add river patch" button and the placeholder generator.
    public readonly struct WaterBand
    {
        public readonly int MinY;
        public readonly int MaxY;

        public WaterBand(int minY, int maxY)
        {
            MinY = minY;
            MaxY = maxY;
        }

        public int Height => MaxY - MinY + 1;
        public float CenterY => (MinY + MaxY + 1) / 2f;

        public static WaterBand Detect(MapData map)
        {
            int minY = int.MaxValue, maxY = int.MinValue;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (IsWater(map.TerrainAt(x, y)))
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }

            if (maxY < minY) { minY = map.Height / 2; maxY = minY; }   // no water: fall back to the middle row
            return new WaterBand(minY, maxY);
        }

        // Overload for the Map Editor's in-memory working terrain (unsaved edits).
        public static WaterBand Detect(TerrainType[] terrain, int width, int height)
        {
            int minY = int.MaxValue, maxY = int.MinValue;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (i < terrain.Length && IsWater(terrain[i])) { if (y < minY) minY = y; if (y > maxY) maxY = y; }
                }

            if (maxY < minY) { minY = height / 2; maxY = minY; }
            return new WaterBand(minY, maxY);
        }

        private static bool IsWater(TerrainType terrain) =>
            terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Sea;
    }
}
