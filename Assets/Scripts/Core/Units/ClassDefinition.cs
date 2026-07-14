using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core.Units
{
    // Authored data for a unit class (Infantry / Cavalry / Mage / …). Holds
    // movement profile, weapon access, stat caps + growths, promotion graph,
    // and visual ids. One asset per class; UnitInstance carries the live state.
    [CreateAssetMenu(menuName = "Project Astra/Units/Class Definition")]
    public class ClassDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string className;
        [SerializeField] private ClassType classType;

        [Header("Movement")]
        [SerializeField] private int movementRange = 5;
        [SerializeField] private MovementType movementType = MovementType.Foot;

        [Header("Weapons")]
        [SerializeField] private WeaponType[] weaponWhitelist;

        [Header("Stats")]
        [Tooltip("Per stat: a signed modifier (-100..100) ADDED to each unit's personal growth % (negative for a stat this class is weak at). The sum with the unit's growth is the real per-level chance.")]
        [SerializeField, GrowthRate(-100, 100)] private StatArray statGrowthModifiers;
        [Tooltip("Maximum each stat can reach for units of this class. Level-up and promotion gains stop here.")]
        [SerializeField] private StatArray statCaps;
        [Tooltip("How many HP points HP rises by each time its growth roll succeeds (every OTHER stat rises by 1). This is the growth STEP for HP, not the chance — the chance is the HP growth rate.")]
        [SerializeField] private int hpGainOnLevelUp = 2;

        [Tooltip("Flat crit-rate bonus in percentage points for this class. Combat crit% = Skill/2 + weapon crit + this - target's Niyati; a crit deals 3x damage. An integer, not a 0-100 fraction.")]
        [SerializeField] private int critBonus;

        [Header("Promotion (mostly reserved — only Is Promoted is wired today)")]
        [Tooltip("RESERVED: no promotion trigger exists in gameplay yet, so this flag isn't read anywhere — authored for a future promotion system.")]
        [SerializeField] private bool canPromote;
        [Tooltip("LIVE: marks an advanced (tier-2) class. Promoted units level slower (effective level +20), cap at level 20, and give +20 bonus EXP when killed.")]
        [SerializeField] private bool isPromoted;
        [Tooltip("RESERVED: the classes this could promote into — no promotion system consumes this yet.")]
        [SerializeField, HubRef] private ClassDefinition[] promotionTargets;
        [Tooltip("RESERVED: the base class this promoted from — not consumed yet.")]
        [SerializeField, HubRef] private ClassDefinition baseClass;
        [Tooltip("RESERVED: flat stat points added on promotion (added directly, then clamped to caps). Only applied by the not-yet-wired promotion flow.")]
        [SerializeField] private StatArray promotionBonuses;

        [Header("EXP (Experience Scaling)")]
        [Tooltip("How fast units of this class gain levels. Fast = few kills per level; Very Slow = grindy. Maps to the EXP formula's divisor internally, so designers pick a pace, not a number.")]
        [SerializeField] private LevelingSpeed levelingSpeed = LevelingSpeed.Fast;

        [Header("Abilities (reserved)")]
        [Tooltip("Authored ability ids. NOTE: not yet consumed by any runtime system — reserved for a future abilities pass.")]
        [SerializeField] private string[] classAbilities;

        [Header("Visuals (reserved — the on-map sprite comes from the Unit, not here)")]
        [Tooltip("RESERVED / UNUSED: no system reads this. A unit's on-map token is its Map Sprite on the UnitDefinition. Intended for a future class-based art lookup.")]
        [SerializeField] private string mapSpriteId;
        [Tooltip("RESERVED / UNUSED: no system reads this yet. Intended to key a shared combat-animation set per class later.")]
        [SerializeField] private string combatAnimationSetId;

        public string ClassName => className;
        public ClassType ClassType => classType;
        public int MovementRange => movementRange;
        public MovementType MovementType => movementType;
        public WeaponType[] WeaponWhitelist => weaponWhitelist;
        public StatArray StatGrowthModifiers => statGrowthModifiers;
        public StatArray StatCaps => statCaps;
        public int HPGainOnLevelUp => hpGainOnLevelUp;
        public int CritBonus => critBonus;
        public bool CanPromote => canPromote;
        public bool IsPromoted => isPromoted;
        public ClassDefinition[] PromotionTargets => promotionTargets;
        public ClassDefinition BaseClass => baseClass;
        public StatArray PromotionBonuses => promotionBonuses;
        public LevelingSpeed LevelingSpeed => levelingSpeed;
        public float ExpPowerFactor => LevelingSpeedToFactor(levelingSpeed);
        public string[] ClassAbilities => classAbilities;
        public string MapSpriteId => mapSpriteId;
        public string CombatAnimationSetId => combatAnimationSetId;

        // Canto — cavalry and flying units keep any unused movement after a primary action.
        public bool HasCanto => classType == ClassType.Cavalry || classType == ClassType.Flying;

        // The designer picks a Leveling Speed; the FE-style EXP formula wants a divisor.
        // Higher divisor = less EXP per action = slower leveling. Fast (=1) preserves the old default.
        private static float LevelingSpeedToFactor(LevelingSpeed speed) => speed switch
        {
            LevelingSpeed.Fast => 1f,
            LevelingSpeed.Normal => 3f,
            LevelingSpeed.Slow => 4.5f,
            LevelingSpeed.VerySlow => 6f,
            _ => 1f,
        };
    }
}
