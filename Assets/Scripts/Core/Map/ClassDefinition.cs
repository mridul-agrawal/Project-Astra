using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Units/Class Definition")]
    public class ClassDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _className;
        [SerializeField] private ClassType _classType;

        [Header("Movement")]
        [SerializeField] private int _movementRange = 5;
        [SerializeField] private MovementType _movementType = MovementType.Foot;

        [Header("Weapons")]
        [SerializeField] private WeaponType[] _weaponWhitelist;

        [Header("Stats")]
        [SerializeField] private StatArray _statGrowthModifiers;
        [SerializeField] private StatArray _statCaps;
        [SerializeField] private int _hpGainOnLevelUp = 2;

        [Tooltip("UC-08. Class-level crit bonus folded into the Crit formula.")]
        [SerializeField] private int _critBonus;

        [Header("Promotion")]
        [SerializeField] private bool _canPromote;
        [SerializeField] private bool _isPromoted;
        [SerializeField] private ClassDefinition[] _promotionTargets;
        [SerializeField] private ClassDefinition _baseClass;
        [SerializeField] private StatArray _promotionBonuses;

        [Header("EXP (Experience Scaling)")]
        [Tooltip("Divisor in the FE GBA EXP formula. Higher = less EXP per action. FE GBA canon: Myrmidon ≈ 2, most classes ≈ 3, Lord typically 1.0 so the protagonist levels faster.")]
        [SerializeField, Min(0.1f)] private float _expPowerFactor = 1f;

        [Header("Abilities")]
        [SerializeField] private string[] _classAbilities;

        [Header("Visuals")]
        [SerializeField] private string _mapSpriteId;
        [SerializeField] private string _combatAnimationSetId;

        public string ClassName => _className;
        public ClassType ClassType => _classType;
        public int MovementRange => _movementRange;
        public MovementType MovementType => _movementType;
        public WeaponType[] WeaponWhitelist => _weaponWhitelist;
        public StatArray StatGrowthModifiers => _statGrowthModifiers;
        public StatArray StatCaps => _statCaps;
        public int HPGainOnLevelUp => _hpGainOnLevelUp;
        public int CritBonus => _critBonus;
        public bool CanPromote => _canPromote;
        public bool IsPromoted => _isPromoted;
        public ClassDefinition[] PromotionTargets => _promotionTargets;
        public ClassDefinition BaseClass => _baseClass;
        public StatArray PromotionBonuses => _promotionBonuses;
        public float ExpPowerFactor => _expPowerFactor;
        public string[] ClassAbilities => _classAbilities;
        public string MapSpriteId => _mapSpriteId;
        public string CombatAnimationSetId => _combatAnimationSetId;
    }
}
