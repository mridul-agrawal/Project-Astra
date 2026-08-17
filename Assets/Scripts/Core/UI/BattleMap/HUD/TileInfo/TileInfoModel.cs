using System.Collections.Generic;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // One label/value pair inside a strip, e.g. "Avo +30". A flag is the same shape with no
    // value: a bare word like "Impassable".
    public sealed class TileEffectChip
    {
        public string Label;
        public int Value;
        public bool IsFlag;

        public static TileEffectChip Stat(string label, int value) =>
            new TileEffectChip { Label = label, Value = value };

        public static TileEffectChip Flag(string label) =>
            new TileEffectChip { Label = label, IsFlag = true };

        public bool IsNegative => !IsFlag && Value < 0;
    }

    // One rendered rectangle of effects. §5 turns the whole strip's chevron red when any chip
    // inside it is negative, so the strip owns that question rather than each chip.
    public sealed class TileEffectStrip
    {
        public List<TileEffectChip> Chips = new();

        public bool HasNegative
        {
            get
            {
                foreach (TileEffectChip chip in Chips)
                    if (chip.IsNegative) return true;
                return false;
            }
        }
    }

    public sealed class TileInfoModel
    {
        // §6: composite tiles join their parts with " + ", e.g. "Ground + Flames".
        public string TerrainName;

        // §3 renders at most three. Today's data fills one; the list keeps the shape open.
        public List<TileEffectStrip> Strips = new();

        public HudCorner Corner;
    }
}
