using System;
using UnityEngine;

namespace ProjectAstra.Core.Animation
{
    // One sliced cell of a sprite sheet: where it sits in the texture (bottom-left
    // origin, the convention Unity sprite rects use) and the name it should carry.
    public readonly struct SpriteSlice
    {
        public readonly Rect Rect;
        public readonly string Name;

        public SpriteSlice(Rect rect, string name)
        {
            Rect = rect;
            Name = name;
        }
    }

    // Pure grid-slicing math for a sprite sheet. No Unity editor calls, so it can
    // be unit-tested. Frames are numbered in reading order (left to right, top row
    // first) and named to match what the animation builders expect: "base_0..N",
    // or "rowLabel_col" when per-row labels are supplied (units: idle / run_n / …).
    public static class SpriteSheetGrid
    {
        public static SpriteSlice[] SliceByGrid(
            int texWidth, int texHeight, int columns, int rows, string baseName, string[] rowLabels = null)
        {
            if (columns <= 0 || rows <= 0 || texWidth <= 0 || texHeight <= 0)
                return Array.Empty<SpriteSlice>();

            return Build(texHeight, columns, rows, texWidth / columns, texHeight / rows, baseName, rowLabels);
        }

        public static SpriteSlice[] SliceByCell(
            int texWidth, int texHeight, int cellWidth, int cellHeight, string baseName, string[] rowLabels = null)
        {
            if (cellWidth <= 0 || cellHeight <= 0 || texWidth <= 0 || texHeight <= 0)
                return Array.Empty<SpriteSlice>();

            return Build(texHeight, texWidth / cellWidth, texHeight / cellHeight, cellWidth, cellHeight, baseName, rowLabels);
        }

        private static SpriteSlice[] Build(
            int texHeight, int columns, int rows, int cellWidth, int cellHeight, string baseName, string[] rowLabels)
        {
            if (columns <= 0 || rows <= 0 || cellWidth <= 0 || cellHeight <= 0)
                return Array.Empty<SpriteSlice>();

            var slices = new SpriteSlice[columns * rows];
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                int y = texHeight - (row + 1) * cellHeight;   // row 0 is the top row of the image
                for (int col = 0; col < columns; col++)
                {
                    var rect = new Rect(col * cellWidth, y, cellWidth, cellHeight);
                    slices[index++] = new SpriteSlice(rect, NameFor(baseName, rowLabels, row, col, columns));
                }
            }
            return slices;
        }

        private static string NameFor(string baseName, string[] rowLabels, int row, int col, int columns)
        {
            if (rowLabels != null && row < rowLabels.Length && !string.IsNullOrEmpty(rowLabels[row]))
                return $"{rowLabels[row]}_{col}";
            return $"{baseName}_{row * columns + col}";
        }
    }
}
