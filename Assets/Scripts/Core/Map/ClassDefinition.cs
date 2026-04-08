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

        [Header("Promotion")]
        [SerializeField] private bool _canPromote;
        [SerializeField] private bool _isPromoted;
        [SerializeField] private ClassDefinition[] _promotionTargets;
        [SerializeField] private ClassDefinition _baseClass;
        [SerializeField] private StatArray _promotionBonuses;

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
        public bool CanPromote => _canPromote;
        public bool IsPromoted => _isPromoted;
        public ClassDefinition[] PromotionTargets => _promotionTargets;
        public ClassDefinition BaseClass => _baseClass;
        public StatArray PromotionBonuses => _promotionBonuses;
        public string[] ClassAbilities => _classAbilities;
        public string MapSpriteId => _mapSpriteId;
        public string CombatAnimationSetId => _combatAnimationSetId;
    }
}
