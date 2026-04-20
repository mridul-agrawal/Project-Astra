using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    // Ref-holder attached to the CombatForecast prefab root by CombatForecastBuilder.
    // CombatForecastUI reads these fields to drive the live text + HP bars + state badges.
    public class CombatForecastRefs : MonoBehaviour
    {
        [Serializable]
        public class UnitSide
        {
            [Header("Header band")]
            public Image sideTagBg;            // tag_atk or tag_def sprite
            public TextMeshProUGUI sideTagLabel;
            public TextMeshProUGUI unitName;
            public TextMeshProUGUI unitSub;     // "Class · House"

            [Header("Meta")]
            public TextMeshProUGUI levelValue;
            public TextMeshProUGUI classValue;

            [Header("Weapon row")]
            public Image weaponRowBg;
            public Image weaponIcon;
            public TextMeshProUGUI weaponName;
            public TextMeshProUGUI weaponArrow; // "▲" / "▼" / hidden

            [Header("HP")]
            public TextMeshProUGUI hpNumeric;      // "34 / 34"
            public TextMeshProUGUI hpDelta;        // "−9" (vermillion) or hidden
            public Image hpTrackBg;
            public Image hpFill;                    // width driven by current%
            public Image hpPredOverlay;             // width + left driven by delta
            public RectTransform koBadgeTransform;  // parent of the KO badge (activate/deactivate)

            [Header("Badges")]
            public GameObject effectiveChip;        // activate when isEffective

            [Header("Doubling chip")]
            public GameObject doubleChipRoot;       // hidden by default
            public Image doubleChipBg;              // swap: chip_double_as / chip_double_brave / chip_double_combined
            public TextMeshProUGUI doubleChipNumber;  // "×2" / "×4"
            public TextMeshProUGUI doubleChipTag;     // "AS Double" / "Brave" / "Combined"
        }

        [Header("Panel chrome")]
        public Image panelComposite;          // the big 960×720 baked chrome
        public TextMeshProUGUI headerTitle;   // "COMBAT FORECAST"
        public TextMeshProUGUI vsTag;

        [Header("DMG hero numbers (spine)")]
        public TextMeshProUGUI attackerDmgNum;
        public TextMeshProUGUI defenderDmgNum;
        public TextMeshProUGUI attackerDmgLabel;
        public TextMeshProUGUI defenderDmgLabel;

        [Header("Stat rows (HIT, CRIT)")]
        public TextMeshProUGUI hitLabel, critLabel;
        public TextMeshProUGUI hitAttackerVal, hitDefenderVal;
        public TextMeshProUGUI critAttackerVal, critDefenderVal;

        [Header("No-counter ribbon")]
        public GameObject noCounterRibbon;
        public TextMeshProUGUI noCounterLabel;    // "CANNOT COUNTER" / "OUT OF RANGE"

        [Header("Unit sides")]
        public UnitSide left  = new UnitSide();
        public UnitSide right = new UnitSide();

        [Header("Footer hints")]
        public TextMeshProUGUI footerHints;

        [Header("Shared sprites (state swaps)")]
        public Sprite chipEffective;
        public Sprite chipDoubleAs;
        public Sprite chipDoubleBrave;
        public Sprite chipDoubleCombined;
        public Sprite tagAtk;
        public Sprite tagDef;
        public Sprite hpTrack;
        public Sprite hpFillGreen;
        public Sprite hpFillYellow;
        public Sprite hpFillRed;
        public Sprite hpPred;
        public Sprite badgeKo;
        public Sprite ribbonNoCounter;
        public Sprite weaponRow;

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
