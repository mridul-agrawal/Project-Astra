using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public sealed class UnitCardModel
    {
        public string UnitName;
        public Sprite unitCardPortriat;
        public int UnitLevel;
        public int UnitExp;
        public int CurrentHP;
        public int MaxHP;
        public float HpFraction => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
        public Faction UnitFaction;
        public HudCorner Corner;

        // Greys the card out to match the on-map frozen grammar (spec §9).
        public bool HasActed;
    }
}
