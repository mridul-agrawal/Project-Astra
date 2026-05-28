using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core
{
    // Designer-authored weapon. Bakes into the runtime WeaponData struct that
    // combat, durability and the inventory UI already understand — so adding a
    // weapon is now "create an asset", not "edit a C# factory".
    [CreateAssetMenu(menuName = "Project Astra/Items/Weapon", fileName = "NewWeapon")]
    public class WeaponDefinition : ItemDefinition
    {
        [Header("Type")]
        [SerializeField] private WeaponType _weaponType;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private MagicSchool _magicSchool = MagicSchool.None;
        [SerializeField] private StaffEffect _staffEffect = StaffEffect.None;
        [SerializeField] private WeaponTier _tier = WeaponTier.Iron;
        [SerializeField] private WeaponRank _minRank = WeaponRank.E;

        [Header("Combat")]
        [SerializeField] private int _might;
        [SerializeField, Range(0, 100)] private int _hit = 80;
        [SerializeField, Range(0, 100)] private int _crit;
        [SerializeField] private int _weight;
        [SerializeField, Min(1)] private int _minRange = 1;
        [SerializeField, Min(1)] private int _maxRange = 1;

        [Header("Durability")]
        [SerializeField, Min(0)] private int _maxUses = 45;
        [Tooltip("Personal/legendary weapons that never break.")]
        [SerializeField] private bool _indestructible;

        [Header("Special")]
        [Tooltip("Brave weapons attack twice per round.")]
        [SerializeField] private bool _brave;
        [Tooltip("Classes this weapon deals tripled might against (e.g. Bow vs Flying).")]
        [SerializeField] private ClassType[] _effectivenessTargets;
        [Tooltip("If set, only the unit with the matching id below may wield it.")]
        [SerializeField] private bool _characterLocked;
        [SerializeField] private string _ownerUnitId;

        public WeaponData ToRuntime() => new()
        {
            name = DisplayName,
            weaponType = _weaponType,
            damageType = _damageType,
            magicSchool = _magicSchool,
            staffEffect = _staffEffect,
            tier = _tier,
            minRank = _minRank,
            might = _might,
            hit = _hit,
            crit = _crit,
            weight = _weight,
            minRange = _minRange,
            maxRange = _maxRange,
            maxUses = _maxUses,
            currentUses = _maxUses,
            indestructible = _indestructible,
            brave = _brave,
            effectivenessTargets = _effectivenessTargets,
            characterLocked = _characterLocked,
            ownerUnitId = _ownerUnitId,
        };

        public override InventoryItem ToInventoryItem() => InventoryItem.FromWeapon(ToRuntime());
    }
}
