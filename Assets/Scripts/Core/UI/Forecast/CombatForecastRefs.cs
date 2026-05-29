using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.Forecast
{
    // Ref-holder attached to the CombatForecast prefab root. CombatForecastUI reads
    // these fields to drive the live text + HP bars + state badges. Only references
    // the UI actually drives are kept — static chrome (panel art, header/footer text,
    // ATK/DEF tags) lives in the prefab and is never touched by code.
    public class CombatForecastRefs : MonoBehaviour
    {
        [Serializable]
        public class UnitSide
        {
            [Header("Identity")]
            public TextMeshProUGUI unitName;
            public TextMeshProUGUI unitSub;     // "Class · House"

            [Header("Meta")]
            public TextMeshProUGUI levelValue;
            public TextMeshProUGUI classValue;

            [Header("Weapon row")]
            public Image weaponIcon;
            public TextMeshProUGUI weaponName;
            public TextMeshProUGUI weaponArrow; // "▲" / "▼" / hidden

            [Header("HP")]
            public TextMeshProUGUI hpNumeric;      // "34 / 34"
            public TextMeshProUGUI hpDelta;        // "−9" (vermillion) or hidden
            public Image hpTrackBg;
            public Image hpFill;                    // width driven by current%
            public Image hpPredOverlay;             // width + left driven by delta
            public RectTransform koBadgeTransform;  // KO badge parent (activate/deactivate)

            [Header("Badges")]
            public GameObject effectiveChip;        // activate when isEffective

            [Header("Doubling chip")]
            public GameObject doubleChipRoot;       // hidden by default
            public Image doubleChipBg;              // swap: chipDoubleAs / Brave / Combined
            public TextMeshProUGUI doubleChipNumber;  // "×2" / "×4"
            public TextMeshProUGUI doubleChipTag;     // "AS DOUBLE" / "BRAVE" / "COMBINED"
        }

        [Header("DMG hero numbers (spine)")]
        public TextMeshProUGUI attackerDmgNum;
        public TextMeshProUGUI defenderDmgNum;

        [Header("Stat row values (HIT, CRIT)")]
        public TextMeshProUGUI hitAttackerVal, hitDefenderVal;
        public TextMeshProUGUI critAttackerVal, critDefenderVal;

        [Header("No-counter ribbon")]
        public GameObject noCounterRibbon;
        public TextMeshProUGUI noCounterLabel;    // "CANNOT COUNTER" / "OUT OF RANGE"

        [Header("Unit sides")]
        public UnitSide left  = new UnitSide();
        public UnitSide right = new UnitSide();

        [Header("Doubling-chip sprites (state swap)")]
        public Sprite chipDoubleAs;
        public Sprite chipDoubleBrave;
        public Sprite chipDoubleCombined;

        [Header("HP-fill sprites (state swap)")]
        public Sprite hpFillGreen;
        public Sprite hpFillYellow;
        public Sprite hpFillRed;

        [Header("Weapon-icon sigils (reused from InventoryPopup)")]
        public Sprite sigilSword;
        public Sprite sigilLance;
        public Sprite sigilAxe;
        public Sprite sigilBow;
        public Sprite sigilStaff;
        public Sprite sigilAnima;
        public Sprite sigilLight;
        public Sprite sigilDark;
    }
}
