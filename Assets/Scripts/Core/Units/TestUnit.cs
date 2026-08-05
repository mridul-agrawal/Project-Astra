using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Pathfinding;

namespace ProjectAstra.Core.Units
{
    // Scene-level placeholder for a battle unit — holds grid position, basic
    // stats, an inventory, and binds to a runtime UnitInstance at Start when a
    // UnitDefinition is supplied. Named "Test" because the original purpose
    // was exercising cursor / movement / targeting before the full unit
    // system landed; now serves as the actual scene-spawned unit component.
    [RequireComponent(typeof(UnitInventory))]
    public class TestUnit : MonoBehaviour
    {
        // Used only when no UnitVisualSettings is wired (tests, bare scenes).
        private static readonly Color DefaultActedTint = new(0.4f, 0.4f, 0.4f, 0.7f);

        // The acted-unit tint, sourced from the designer-tunable settings asset.
        private static Color ActedTint =>
            UnitVisualSettingsRef.Current != null ? UnitVisualSettingsRef.Current.ActedTint : DefaultActedTint;

        [Header("Unit Identity")]
        public Faction faction = Faction.Player;

        // TODO(UM-02-followups): cannot-dismiss guard + mandatory-deployment
        // guard land when roster and deployment systems ship.
        //
        // Authoritative source is UnitDefinition.IsLord — set in Start when a
        // definition drives the unit. Kept serialized so scene-only test units
        // (no definition) can still flag themselves.
        public bool isLord;

        [Header("Unit Stats")]
        public Vector2Int gridPosition = new(2, 2);
        public int movementPoints = 3;
        public MovementType movementType = MovementType.Foot;

        // Fallback attack reach for bare scene units with no usable weapon; armed units derive their
        // range from their equipped weapons via GetAttackRange, so authored weapon range drives it.
        public int attackRangeMin = 1;
        public int attackRangeMax = 1;

        [Header("Equippability fallback")]
        [SerializeField] private WeaponType[] allowedWeaponTypes;

        [Header("HP")]
        public int maxHP = 20;
        public int currentHP = 20;

        [Header("Turn State")]
        public bool hasActed;
        public Vector2Int preMovementPosition;

        [Header("Unit System (optional)")]
        [SerializeField] private UnitDefinition unitDefinition;
        [Tooltip("Optional class override — when set, the unit is spawned in this class instead of the definition's DefaultClass. Useful for scene-level overrides (e.g. promoting a test unit to a Flying class without editing the character asset).")]
        [SerializeField] private ClassDefinition classOverride;

        private UnitInstance unitInstance;
        private UnitInventory inventory;
        private SpriteRenderer spriteRenderer;
        private Color normalColor;

        public UnitInstance UnitInstance => unitInstance;
        // Serialized UnitDefinition reference, accessible before Start binds a UnitInstance.
        public UnitDefinition UnitDefinition => unitDefinition;
        public WeaponRankTracker WeaponRankTracker { get; set; }
        public WeaponType[] AllowedWeaponTypes => allowedWeaponTypes;

        public UnitInventory Inventory
        {
            get
            {
                if (inventory != null) return inventory;
                inventory = GetComponent<UnitInventory>();
                if (inventory == null) inventory = gameObject.AddComponent<UnitInventory>();
                return inventory;
            }
        }

        // Back-compat accessor for the unit's active weapon. Reads delegate to
        // the inventory's first equippable-weapon scan; writes place the weapon
        // in slot 0 so existing setup code (CursorSceneSetup) keeps working.
        public WeaponData equippedWeapon
        {
            get => Inventory.GetEquippedWeapon();
            set => Inventory.SetSlot(0, InventoryItem.FromWeapon(value));
        }

        private void Awake()
        {
            inventory = GetComponent<UnitInventory>();
        }

        private void Start()
        {
            CacheSpriteRenderer();
            BindUnitInstanceFromDefinitionIfNeeded();
            SyncLordFlagFromDefinition();
            EnsureFlyingHoverAnimator();
            EnsureUnitAnimator();
            SnapToGridPosition();
        }

        private void OnValidate()
        {
            SnapToGridPosition();
        }

        public void BindUnitInstance(UnitInstance instance)
        {
            unitInstance = instance;
            movementPoints = instance.EffectiveMovement;
            movementType = instance.MovementType;
            maxHP = instance.MaxHP;
            currentHP = instance.CurrentHP;
        }

        // Runtime injection used by UnitSpawner: sets the authored definition and binds
        // the UnitInstance immediately, so the instance is ready for anything that reads
        // it at battle start (e.g. the HUD's unit card) before this unit's own Start runs.
        public void InitializeFromDefinition(UnitDefinition definition, ClassDefinition classOverride = null)
        {
            unitDefinition = definition;
            if (classOverride != null) this.classOverride = classOverride;
            BindUnitInstanceFromDefinitionIfNeeded();
        }

        public void MarkActed()
        {
            hasActed = true;
            SetSpriteColor(ActedTint);
            SetIdleAnimationFrozen(true);
        }

        public void ResetActed()
        {
            hasActed = false;
            SetSpriteColor(normalColor);
            SetIdleAnimationFrozen(false);
        }

        // A spent unit goes still as well as grey — motion is the cue that survives at a
        // glance and without colour. The flyer's hover bob is a separate component from the
        // Animator, so both have to be stopped.
        private void SetIdleAnimationFrozen(bool frozen)
        {
            var unitAnimator = GetComponentInChildren<UnitAnimator>();
            if (unitAnimator != null) unitAnimator.SetSpent(frozen);

            var hover = GetComponentInChildren<FlyingHoverAnimator>();
            if (hover != null) hover.enabled = !frozen;
        }

        public void SnapToGridPosition()
        {
            transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0f);
        }

        private void CacheSpriteRenderer()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                normalColor = spriteRenderer.color;
        }

        private void BindUnitInstanceFromDefinitionIfNeeded()
        {
            if (unitDefinition == null || unitInstance != null) return;

            var instance = classOverride != null
                ? new UnitInstance(unitDefinition, classOverride, unitDefinition.BaseLevel, unitDefinition.BaseStats)
                : new UnitInstance(unitDefinition);
            BindUnitInstance(instance);
        }

        private void SyncLordFlagFromDefinition()
        {
            if (unitDefinition != null)
                isLord = unitDefinition.IsLord;   // the definition is authoritative when present
        }

        // Attack reach = union of the ranges of every usable, damaging weapon the unit carries.
        // Falls back to the serialized attackRangeMin/Max only when the unit has no usable weapon
        // (bare scene test units). This is what makes a weapon's authored range drive who it can hit.
        public void GetAttackRange(out int min, out int max)
        {
            min = int.MaxValue;
            max = 0;
            for (int i = 0; i < UnitInventory.Capacity; i++)
            {
                InventoryItem item = Inventory.GetSlot(i);
                if (item.kind != ItemKind.Weapon) continue;

                WeaponData weapon = item.weapon;
                if (weapon.weaponType == WeaponType.Staff) continue;   // staves heal, they don't attack
                if (!EquipResolver.CanEquip(this, weapon)) continue;

                if (weapon.minRange < min) min = weapon.minRange;
                if (weapon.maxRange > max) max = weapon.maxRange;
            }
            if (max == 0) { min = attackRangeMin; max = attackRangeMax; }
        }

        private void EnsureFlyingHoverAnimator()
        {
            if (movementType != MovementType.Flying || spriteRenderer == null) return;
            if (spriteRenderer.GetComponent<FlyingHoverAnimator>() != null) return;

            spriteRenderer.gameObject.AddComponent<FlyingHoverAnimator>();
        }

        // Wires up the Animator-driven map animation when the definition supplies
        // an override controller; otherwise the static Map sprite stays as-is.
        private void EnsureUnitAnimator()
        {
            if (spriteRenderer == null) return;
            RuntimeAnimatorController controller = unitDefinition != null ? unitDefinition.MapAnimator : null;
            if (controller == null) return;

            GameObject spriteObject = spriteRenderer.gameObject;
            if (spriteObject.GetComponent<UnitAnimator>() != null) return;

            Animator animator = spriteObject.GetComponent<Animator>();
            if (animator == null) animator = spriteObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            spriteObject.AddComponent<UnitAnimator>();
        }

        private void SetSpriteColor(Color color)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }
    }
}
