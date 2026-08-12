using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // The statuses the §2 chip row can draw. Only Stressed is backed by game data today;
    // the other three are spec fixtures and light up on their own once a status system lands.
    public enum UnitStatusKind { Stressed, Poisoned, Slowed, Shielded }

    // Left-column summary — shared by both the STATS and GEAR tabs.
    public sealed class UnitSummaryModel
    {
        public string UnitName;
        public Sprite Portrait;
        public string ClassName;
        public Sprite ClassIcon;
        public int Level;
        public int CurrentHP;
        public int MaxHP;

        public bool ShowExp;            // hidden for enemies
        public string ExpText;          // "0 / 100", "MAX", or "--"
        public float ExpFraction;

        public bool ShowStatus;         // hidden when the unit has no status
        public string StatusText;       // e.g. "STRESSED" / a modifier summary
        public List<UnitStatusKind> Statuses = new();

        public bool IsActed;            // §4 greys the portrait and shows the ACTED chip
        public bool IsEnemy;

        public float HpFraction => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
    }
}
