using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Minimal test unit for exercising cursor, movement, and targeting flows.
    /// NOT a real unit system — just data + a visual on the Units layer.
    /// </summary>
    [RequireComponent(typeof(UnitInventory))]
    public class TestUnit : MonoBehaviour
    {
        private static readonly Color ActedColor = new(0.4f, 0.4f, 0.4f, 0.7f);

        [Header("Unit Identity")]
        public Faction faction = Faction.Player;

        [Header("Unit Stats")]
        public Vector2Int gridPosition = new(2, 2);
        public int movementPoints = 3;
        public MovementType movementType = MovementType.Foot;
        public int attackRangeMin = 1;
        public int attackRangeMax = 1;

        [Header("Equippability fallback")]
        [SerializeField] private WeaponType[] _allowedWeaponTypes;

        [Header("HP")]
        public int maxHP = 20;
        public int currentHP = 20;

        [Header("Turn State")]
        public bool hasActed;
        public Vector2Int preMovementPosition;

        [Header("Unit System (optional)")]
        [SerializeField] private UnitDefinition _unitDefinition;

        private UnitInstance _unitInstance;
        private UnitInventory _inventory;
        private SpriteRenderer _spriteRenderer;
        private Color _normalColor;

        public UnitInstance UnitInstance => _unitInstance;
        public WeaponRankTracker WeaponRankTracker { get; set; }
        public WeaponType[] AllowedWeaponTypes => _allowedWeaponTypes;

        public UnitInventory Inventory
        {
            get
            {
                if (_inventory == null)
                {
                    _inventory = GetComponent<UnitInventory>();
                    if (_inventory == null) _inventory = gameObject.AddComponent<UnitInventory>();
                }
                return _inventory;
            }
        }

        /// <summary>
        /// Backwards-compatible accessor for the unit's active weapon. Reads delegate to
        /// the inventory's first equippable weapon scan; writes place the weapon in slot 0
        /// so existing setup code (e.g., CursorSceneSetup) keeps working without changes.
        /// </summary>
        public WeaponData equippedWeapon
        {
            get => Inventory.GetEquippedWeapon();
            set => Inventory.SetSlot(0, InventoryItem.FromWeapon(value));
        }

        public void BindUnitInstance(UnitInstance instance)
        {
            _unitInstance = instance;
            movementPoints = instance.EffectiveMovement;
            movementType = instance.MovementType;
        }

        private void Awake()
        {
            _inventory = GetComponent<UnitInventory>();
        }

        private void Start()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
                _normalColor = _spriteRenderer.color;

            if (_unitDefinition != null && _unitInstance == null)
                BindUnitInstance(new UnitInstance(_unitDefinition));

            SnapToGridPosition();
        }

        private void OnValidate()
        {
            SnapToGridPosition();
        }

        public void MarkActed()
        {
            hasActed = true;
            SetSpriteColor(ActedColor);
        }

        public void ResetActed()
        {
            hasActed = false;
            SetSpriteColor(_normalColor);
        }

        public void SnapToGridPosition()
        {
            transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0f);
        }

        private void SetSpriteColor(Color color)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }
    }
}
