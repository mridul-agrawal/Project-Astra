using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // What the protagonist can and can't walk through, at half-tile resolution.
    //
    // Half a tile rather than a whole one because a tall sprite blocks only its lower part — a tree
    // that stops you at its trunk while you pass behind its canopy is a 16px rect, not a 32px cell.
    // Whole-tile blocking would also stop the player far enough from a wall to look wrong at this
    // zoom.
    //
    // Props are reference-counted rather than baked flat, so an interactable that opens or is
    // removed mid-visit can un-stamp exactly what it stamped, even where two props overlap.
    public class GurukulCollisionMap
    {
        public const float CellSize = 0.5f;
        private const float Epsilon = 1e-4f;

        private readonly int cellsWide;
        private readonly int cellsHigh;
        private readonly bool[] groundBlocked;
        private readonly int[] propCount;

        public GurukulCollisionMap(int cellsWide, int cellsHigh, bool[] groundBlocked = null)
        {
            this.cellsWide = Mathf.Max(0, cellsWide);
            this.cellsHigh = Mathf.Max(0, cellsHigh);

            int total = this.cellsWide * this.cellsHigh;
            this.groundBlocked = new bool[total];
            propCount = new int[total];

            if (groundBlocked != null)
                for (int i = 0; i < total && i < groundBlocked.Length; i++)
                    this.groundBlocked[i] = groundBlocked[i];
        }

        public int CellsWide => cellsWide;
        public int CellsHigh => cellsHigh;
        public float WidthInTiles => cellsWide * CellSize;
        public float HeightInTiles => cellsHigh * CellSize;

        public void SetGroundBlocked(int cellX, int cellY, bool blocked)
        {
            if (!InBounds(cellX, cellY)) return;
            groundBlocked[Index(cellX, cellY)] = blocked;
        }

        public void Stamp(Rect footprint) => AddToCells(footprint, 1);
        public void Unstamp(Rect footprint) => AddToCells(footprint, -1);

        // Anything off the edge counts as blocked, so the location's bounds need no separate walls.
        public bool IsCellBlocked(int cellX, int cellY)
        {
            if (!InBounds(cellX, cellY)) return true;
            int index = Index(cellX, cellY);
            return groundBlocked[index] || propCount[index] > 0;
        }

        public bool IsRectBlocked(Rect footprint)
        {
            CellRange(footprint, out int minX, out int minY, out int maxX, out int maxY);

            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (IsCellBlocked(x, y)) return true;

            return false;
        }

        private void AddToCells(Rect footprint, int delta)
        {
            CellRange(footprint, out int minX, out int minY, out int maxX, out int maxY);

            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    if (!InBounds(x, y)) continue;
                    int index = Index(x, y);
                    propCount[index] = Mathf.Max(0, propCount[index] + delta);
                }
        }

        // The nudges keep a rect that ends exactly on a cell boundary from claiming the next cell,
        // which floating point would otherwise do intermittently.
        private static void CellRange(Rect footprint, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = Mathf.FloorToInt(footprint.xMin / CellSize + Epsilon);
            minY = Mathf.FloorToInt(footprint.yMin / CellSize + Epsilon);
            maxX = Mathf.CeilToInt(footprint.xMax / CellSize - Epsilon) - 1;
            maxY = Mathf.CeilToInt(footprint.yMax / CellSize - Epsilon) - 1;

            if (maxX < minX) maxX = minX;
            if (maxY < minY) maxY = minY;
        }

        private bool InBounds(int cellX, int cellY) =>
            cellX >= 0 && cellX < cellsWide && cellY >= 0 && cellY < cellsHigh;

        private int Index(int cellX, int cellY) => cellY * cellsWide + cellX;
    }
}
