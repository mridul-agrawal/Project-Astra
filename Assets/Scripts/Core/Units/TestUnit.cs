using UnityEngine;
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
        private static readonly Color ActedColor = new(0.4f, 0.4f, 0.4f, 0.7f);

        [Header("Unit Identity")]
        public Faction faction = Faction.Player;

        // TODO(UM-02-followups): cannot-dismiss guard + mandatory-deployment
        // guard land when roster and deployment systems ship.
        //
        // Source of truth is UnitDefinition.IsLord — synced in Start. Kept
        // serialized so scene-only test units (no definition) can still flag
        // themselves.
        public bool isLord;

        [Header("Unit Stats")]
        public Vector2Int gridPosition = new(2, 2);
        public int movementPoints = 3;
        public MovementType movementType = MovementType.Foot;
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

        // Runtime injection used by UnitSpawner: sets the authored definition before Start so
        // TestUnit's own binding step builds the UnitInstance from it.
        public void InitializeFromDefinition(UnitDefinition definition, ClassDefinition classOverride = null)
        {
            unitDefinition = definition;
            if (classOverride != null) this.classOverride = classOverride;
        }

        public void MarkActed()
        {
            hasActed = true;
            SetSpriteColor(ActedColor);
        }

        public void ResetActed()
        {
            hasActed = false;
            SetSpriteColor(normalColor);
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
            if (unitDefinition != null && unitDefinition.IsLord)
                isLord = true;
        }

        private void EnsureFlyingHoverAnimator()
        {
            if (movementType != MovementType.Flying || spriteRenderer == null) return;
            if (spriteRenderer.GetComponent<FlyingHoverAnimator>() != null) return;

            spriteRenderer.gameObject.AddComponent<FlyingHoverAnimator>();
        }

        private void SetSpriteColor(Color color)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }
    }
}
