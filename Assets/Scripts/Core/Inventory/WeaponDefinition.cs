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
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [SerializeField] private MagicSchool magicSchool = MagicSchool.None;
        [SerializeField] private StaffEffect staffEffect = StaffEffect.None;
        [SerializeField] private WeaponTier tier = WeaponTier.Iron;
        [SerializeField] private WeaponRank minRank = WeaponRank.E;

        [Header("Combat")]
        [SerializeField] private int might;
        [SerializeField, Range(0, 100)] private int hit = 80;
        [SerializeField, Range(0, 100)] private int crit;
        [SerializeField] private int weight;
        [Tooltip("0 is allowed for self-centred AoE staves (e.g. Fortify).")]
        [SerializeField, Min(0)] private int minRange = 1;
        [SerializeField, Min(0)] private int maxRange = 1;

        [Header("Durability")]
        [SerializeField, Min(0)] private int maxUses = 45;
        [Tooltip("Personal/legendary weapons that never break.")]
        [SerializeField] private bool indestructible;

        [Header("Special")]
        [Tooltip("Brave weapons attack twice per round.")]
        [SerializeField] private bool brave;
        [Tooltip("Classes this weapon deals tripled might against (e.g. Bow vs Flying).")]
        [SerializeField] private ClassType[] effectivenessTargets;
        [Tooltip("If set, only the unit with the matching id below may wield it.")]
        [SerializeField] private bool characterLocked;
        [SerializeField] private string ownerUnitId;

        public WeaponData ToRuntime() => new()
        {
            name = DisplayName,
            weaponType = weaponType,
            damageType = damageType,
            magicSchool = magicSchool,
            staffEffect = staffEffect,
            tier = tier,
            minRank = minRank,
            might = might,
            hit = hit,
            crit = crit,
            weight = weight,
            minRange = minRange,
            maxRange = maxRange,
            maxUses = maxUses,
            currentUses = maxUses,
            indestructible = indestructible,
            brave = brave,
            effectivenessTargets = effectivenessTargets,
            characterLocked = characterLocked,
            ownerUnitId = ownerUnitId,
        };

        public override InventoryItem ToInventoryItem() => InventoryItem.FromWeapon(ToRuntime());
    }
}
