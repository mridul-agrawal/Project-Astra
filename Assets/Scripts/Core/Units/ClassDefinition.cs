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
        [SerializeField] private StatArray statGrowthModifiers;
        [SerializeField] private StatArray statCaps;
        [SerializeField] private int hpGainOnLevelUp = 2;

        [Tooltip("UC-08. Class-level crit bonus folded into the Crit formula.")]
        [SerializeField] private int critBonus;

        [Header("Promotion")]
        [SerializeField] private bool canPromote;
        [SerializeField] private bool isPromoted;
        [SerializeField, HubRef] private ClassDefinition[] promotionTargets;
        [SerializeField, HubRef] private ClassDefinition baseClass;
        [SerializeField] private StatArray promotionBonuses;

        [Header("EXP (Experience Scaling)")]
        [Tooltip("Divisor in the FE GBA EXP formula. Higher = less EXP per action. FE GBA canon: Myrmidon ≈ 2, most classes ≈ 3, Lord typically 1.0 so the protagonist levels faster.")]
        [SerializeField, Min(0.1f)] private float expPowerFactor = 1f;

        [Header("Abilities")]
        [Tooltip("Authored ability ids. NOTE: not yet consumed by any runtime system — reserved for a future abilities pass.")]
        [SerializeField] private string[] classAbilities;

        [Header("Visuals")]
        [SerializeField] private string mapSpriteId;
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
        public float ExpPowerFactor => expPowerFactor;
        public string[] ClassAbilities => classAbilities;
        public string MapSpriteId => mapSpriteId;
        public string CombatAnimationSetId => combatAnimationSetId;

        // Canto — cavalry and flying units keep any unused movement after a primary action.
        public bool HasCanto => classType == ClassType.Cavalry || classType == ClassType.Flying;
    }
}
