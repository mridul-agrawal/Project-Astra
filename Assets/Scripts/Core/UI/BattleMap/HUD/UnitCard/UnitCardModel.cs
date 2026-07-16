using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Snapshot of the unit under the cursor, shown by the Unit Card panel.
    // The ViewModel fills this from game state; the View only reads it.
    public sealed class UnitCardModel
    {
        public bool HasUnit;
        public string Name;
        public string ClassName;
        public int CurrentHP;
        public int MaxHP;
        public string Weapon;
        public Faction Faction;
        public Sprite Portrait;

        public float HpFraction => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;

        // Cursor is over an empty tile: the View hides the card.
        public static UnitCardModel Empty() => new UnitCardModel { HasUnit = false };
    }
}
