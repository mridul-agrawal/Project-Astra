using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using ProjectAstra.Core.UI;

[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]

namespace ProjectAstra.Core
{
    /// <summary>
    /// Grid-snapped cursor for the tactical battle map. Tracks integer grid position,
    /// manages four operational modes (Free/UnitSelected/Targeting/Locked), constrains
    /// movement to valid tile sets, and fires events for downstream systems.
    /// DAS input repeat is handled by InputManager — this class just responds to OnCursorMove.
    /// </summary>
    public class GridCursor : MonoBehaviour
    {
        #region Properties and fields
        [Header("Dependencies")]
        [SerializeField] private MapRenderer _mapRenderer;
        [SerializeField] private TerrainStatTable _terrainStatTable;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private RangeHighlighter _rangeHighlighter;
        [SerializeField] private PathArrowRenderer _pathArrowRenderer;
        [SerializeField] private UnitMover _unitMover;
        [SerializeField] private UnitActionMenuUI _actionMenuUI;
        [SerializeField] private InventoryMenuUI _inventoryMenuUI;
        [SerializeField] private ConfirmDialogUI _confirmDialogUI;
        [SerializeField] private ToastNotificationUI _toastUI;
        [SerializeField] private TradeUI _tradeUI;
        [SerializeField] private ConvoyUI _convoyUI;

        [Header("Rendering")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _targetingSprite;

        [Header("Animation")]
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _alphaMin = 0.85f;
        [SerializeField] private float _alphaMax = 1.0f;
        [SerializeField] private float _scaleMin = 1f;
        [SerializeField] private float _scaleMax = 1.04f;

        private Vector2Int _gridPosition;
        private CursorMode _currentMode = CursorMode.Locked;
        private CursorAnimator _animator;

        // Movement constraint — null means unconstrained (FREE mode)
        private HashSet<Vector2Int> _validMoveTiles;

        // Cursor memory for mode transitions (e.g., return to unit tile on cancel)
        private Vector2Int? _memorizedPosition;

        // Unit selection state
        private TestUnit _selectedUnit;
        private Pathfinder.ReachabilityResult _currentReachability;
        private Vector2Int _committedDestination;
        private PathfindingService _pathfindingService;

        // Targeting mode — cycle through attack tiles instead of grid-walking
        private List<Vector2Int> _targetTiles;
        private int _targetIndex;
        private List<Vector2Int> _cachedEnemyTiles = new();
        private List<Vector2Int> _cachedHealTiles = new();
        private List<ActionChoice> _cachedActionChoices = new();
        private List<TestUnit> _cachedAdjacentAllies = new();
        private bool _isHealTargeting;

        private enum ActionChoice { Attack, Heal, Fortify, Item, Trade, Supply, Wait }

        public Vector2Int GridPosition => _gridPosition;
        public CursorMode CurrentMode => _currentMode;

        /// <summary>Fired after the cursor moves to a new tile.</summary>
        public event Action<Vector2Int> OnCursorMoved;
        #endregion

        #region Monobehaviour lifecycle
        private void Awake()
        {
            InitializeCursorAnimator();
        }

        private void OnEnable()
        {
            AddListenersToInputEvents();
            AddListenersToGameStateEvents();
        }

        private void OnDisable()
        {
            RemoveListenersFromInputEvents();
            RemoveListenersFromGameStateEvents();
        }

        private void Start()
        {
            InitializePathFindingService();
            SetPosition(Vector2Int.zero);
            UpdateModeFromGameState();
        }

        private void Update()
        {
            _animator?.UpdatePulse(_pulseSpeed, _alphaMin, _alphaMax, _scaleMin, _scaleMax);
        }
        #endregion

        #region Helpers for initialization and event management
        private void InitializeCursorAnimator()
        {
            if (_spriteRenderer != null)
                _animator = new CursorAnimator(_spriteRenderer);
        }

        private void AddListenersToInputEvents()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove += HandleCursorMove;
                InputManager.Instance.OnConfirm += HandleConfirm;
                InputManager.Instance.OnCancel += HandleCancel;
                InputManager.Instance.OnNextUnit += HandleNextUnit;
                InputManager.Instance.OnPrevUnit += HandlePrevUnit;
            }
        }

        private void RemoveListenersFromInputEvents()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove -= HandleCursorMove;
                InputManager.Instance.OnConfirm -= HandleConfirm;
                InputManager.Instance.OnCancel -= HandleCancel;
                InputManager.Instance.OnNextUnit -= HandleNextUnit;
                InputManager.Instance.OnPrevUnit -= HandlePrevUnit;
            }
        }

        private void AddListenersToGameStateEvents()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Register(OnGameStateChanged);
        }

        private void RemoveListenersFromGameStateEvents()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Unregister(OnGameStateChanged);
        }

        private void InitializePathFindingService()
        {
            if (_mapRenderer != null && _terrainStatTable != null)
                _pathfindingService = new PathfindingService(_mapRenderer, _terrainStatTable);
        }
        #endregion

        #region Methods for mode and position management

        public void SetMode(CursorMode mode)
        {
            _currentMode = mode;
            ToggleSpriteRendererBasedOnCursorMode();
            UpdateSpriteForMode();
        }

        public void SetPosition(Vector2Int position)
        {
            Vector2Int newClampedPosition = ClampToMapBounds(position);
            bool hasCursorPositionActuallyChanged = newClampedPosition != _gridPosition;
            
            _gridPosition = newClampedPosition;
            SnapToGridPosition();
            
            if (hasCursorPositionActuallyChanged)
                OnCursorMoved?.Invoke(_gridPosition);
        }

        public void SetPositionWithMemory(Vector2Int position)
        {
            _memorizedPosition = _gridPosition;
            SetPosition(position);
        }

        public void ReturnToMemorizedPosition()
        {
            if (!_memorizedPosition.HasValue) return;
            SetPosition(_memorizedPosition.Value);
            _memorizedPosition = null;
        }

        private void ToggleSpriteRendererBasedOnCursorMode()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.enabled = (_currentMode != CursorMode.Locked);
        }

        private void UpdateSpriteForMode()
        {
            if (_spriteRenderer == null) return;

            Sprite target = _currentMode switch
            {
                CursorMode.Free => _idleSprite,
                CursorMode.UnitSelected => _selectedSprite,
                CursorMode.Targeting => _targetingSprite,
                CursorMode.ActionMenu => _selectedSprite,
                _ => _idleSprite,
            };

            if (target != null)
                _spriteRenderer.sprite = target;
        }

        private Vector2Int ClampToMapBounds(Vector2Int pos)
        {
            MapData map = _mapRenderer != null ? _mapRenderer.CurrentMap : null;
            if (map == null) return pos;

            return new Vector2Int(
                Mathf.Clamp(pos.x, 0, map.Width - 1),
                Mathf.Clamp(pos.y, 0, map.Height - 1)
                );
        }

        private void SnapToGridPosition()
        {
            transform.position = new Vector3(_gridPosition.x + 0.5f, _gridPosition.y + 0.5f, 0f);
        }
        #endregion

        #region Methods for handling input events and game state changes
        internal void HandleCursorMove(Vector2Int direction)
        {
            if(!CanCursorMove())
                return;

            if (_currentMode == CursorMode.Targeting)
            {
                CycleThroughTargets(direction);
                return;
            }

            Vector2Int targetGridPosition = ClampToMapBounds(_gridPosition + direction);

            if (IsMovementConstrained() && !_validMoveTiles.Contains(targetGridPosition))
                return;

            if (targetGridPosition == _gridPosition)
                return;

            _gridPosition = targetGridPosition;
            SnapToGridPosition();
            OnCursorMoved?.Invoke(_gridPosition);

            if (_currentMode == CursorMode.UnitSelected)
                UpdatePathArrow();
        }

        private void CycleThroughTargets(Vector2Int direction)
        {
            if (_targetTiles == null || _targetTiles.Count == 0) return;

            int step = (direction.x > 0 || direction.y > 0) ? 1 : -1;
            _targetIndex = (_targetIndex + step + _targetTiles.Count) % _targetTiles.Count;

            _gridPosition = _targetTiles[_targetIndex];
            SnapToGridPosition();
            OnCursorMoved?.Invoke(_gridPosition);
        }

        private bool CanCursorMove()
        {
            if (_currentMode == CursorMode.Locked) return false;
            if (_currentMode == CursorMode.ActionMenu) return false;
            if (BattleMapUI.HasInputFocus) return false;
            if (UnitActionMenuUI.HasInputFocus) return false;
            if (InventoryMenuUI.HasInputFocus) return false;
            if (ConfirmDialogUI.HasInputFocus) return false;
            if (TradeUI.HasInputFocus) return false;
            if (ConvoyUI.HasInputFocus) return false;
            if (_unitMover != null && _unitMover.IsMoving) return false;
            return true;
        }

        private bool IsMovementConstrained()
        {
            return _validMoveTiles != null;
        }


        private void HandleNextUnit()
        {
            if (_currentMode != CursorMode.Free || TurnManager.Instance == null) return;
            var next = TurnManager.Instance.UnitRegistry.GetNextUnactedUnit(Faction.Player, FindUnitAt(_gridPosition));
            if (next != null) SetPosition(next.gridPosition);
        }

        private void HandlePrevUnit()
        {
            if (_currentMode != CursorMode.Free || TurnManager.Instance == null) return;
            var prev = TurnManager.Instance.UnitRegistry.GetPrevUnactedUnit(Faction.Player, FindUnitAt(_gridPosition));
            if (prev != null) SetPosition(prev.gridPosition);
        }

        private void UpdatePathArrow()
        {
            if (_pathArrowRenderer == null || _selectedUnit == null) 
                return;

            if (!IsCurrentTileReachable())
            {
                _pathArrowRenderer.Clear();
                return;
            }

            var path = Pathfinder.ReconstructPath(_selectedUnit.gridPosition, _gridPosition, _currentReachability);
            _pathArrowRenderer.ShowPath(path);
        }

        private bool IsCurrentTileReachable()
        {
            return _currentReachability.Destinations.Contains(_gridPosition);
        }

        internal void HandleConfirm()
        {
            if (!CanCursorMove())
                return;

            switch (_currentMode)
            {
                case CursorMode.Free:
                    TrySelectUnit();
                    break;
                case CursorMode.UnitSelected:
                    TryCommitMovement();
                    break;
                case CursorMode.Targeting:
                    if (_isHealTargeting)
                        TryCommitHeal();
                    else
                        TryCommitAttack();
                    break;
            }
        }

        private void TrySelectUnit()
        {
            TestUnit unit = FindUnitAt(_gridPosition);

            if (!IsUnitSelectable(unit)) 
                return;

            _selectedUnit = unit;
            EnterUnitSelectedMode();
        }

        private TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                if (unit.gridPosition == pos)
                    return unit;
            return null;
        }

        private bool IsUnitSelectable(TestUnit unit)
        {
            if (unit == null) return false;

            if (TurnManager.Instance != null)
            {
                var registry = TurnManager.Instance.UnitRegistry;
                if (!registry.CanAct(unit)) return false;
                if (registry.GetFaction(unit) != Faction.Player) return false;
                if (TurnManager.Instance.CurrentPhase != BattlePhase.PlayerPhase) return false;
            }
            else if (unit.hasActed)
            {
                return false;
            }

            return true;
        }

        private void EnterUnitSelectedMode()
        {
            if (_pathfindingService == null || _selectedUnit == null) return;

            _currentReachability = _pathfindingService.ComputeReachability(_selectedUnit.gridPosition, _selectedUnit.movementPoints, _selectedUnit.movementType, pos => GetOccupantType(pos));

            // Constrain cursor to reachable + pass-through tiles
            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);

            _rangeHighlighter?.ShowMovementRange(_currentReachability.Destinations, _currentReachability.PassThrough);

            SetPositionWithMemory(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
        }

        private Pathfinder.OccupantType GetOccupantType(Vector2Int pos)
        {
            return Pathfinder.OccupantType.None;
        }

        private void TryCommitMovement()
        {
            if (!_currentReachability.Destinations.Contains(_gridPosition))
                return;

            _committedDestination = _gridPosition;
            _selectedUnit.preMovementPosition = _selectedUnit.gridPosition;

            var path = Pathfinder.ReconstructPath(_selectedUnit.gridPosition, _committedDestination, _currentReachability);

            ClearOverlay();
            SetMode(CursorMode.Locked);

            if (PathExists(path))
                _unitMover.MoveAlongPath(_selectedUnit, path, OnMovementComplete);
            else
                OnMovementComplete();
        }

        private void ClearOverlay()
        {
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
        }

        private bool PathExists(List<Vector2Int> path)
        {
            return (_unitMover != null && path != null && path.Count > 1);
        }

        private void OnMovementComplete()
        {
            ShowActionMenu();
        }

        private void ShowActionMenu()
        {
            var labels = new List<string>();
            _cachedActionChoices.Clear();
            _cachedEnemyTiles = FindEnemiesInAttackRange();

            if (_cachedEnemyTiles.Count > 0 && _selectedUnit != null && !_selectedUnit.Inventory.IsUnarmed)
            {
                labels.Add("Attack");
                _cachedActionChoices.Add(ActionChoice.Attack);
            }

            if (_selectedUnit != null)
            {
                var staff = _selectedUnit.equippedWeapon;
                if (staff.weaponType == WeaponType.Staff && staff.staffEffect != StaffEffect.None && !staff.IsBroken)
                {
                    if (staff.staffEffect == StaffEffect.AreaOfEffect)
                    {
                        var allUnits = new List<TestUnit>(FindObjectsByType<TestUnit>(FindObjectsSortMode.None));
                        int mag = GetMagStat(_selectedUnit);
                        bool anyHealable = false;
                        foreach (var u in allUnits)
                        {
                            if (u == _selectedUnit) continue;
                            if (StaffEffects.CanHealTarget(_selectedUnit, u, staff, mag, out _))
                            { anyHealable = true; break; }
                        }
                        if (anyHealable)
                        {
                            labels.Add("Fortify");
                            _cachedActionChoices.Add(ActionChoice.Fortify);
                        }
                    }
                    else
                    {
                        _cachedHealTiles = FindAlliesInHealRange();
                        if (_cachedHealTiles.Count > 0)
                        {
                            labels.Add("Heal");
                            _cachedActionChoices.Add(ActionChoice.Heal);
                        }
                    }
                }
            }

            if (_selectedUnit != null && _selectedUnit.Inventory.OccupiedCount > 0)
            {
                labels.Add("Item");
                _cachedActionChoices.Add(ActionChoice.Item);
            }

            _cachedAdjacentAllies = _selectedUnit != null
                ? AdjacentAllyFinder.FindAdjacentAllies(
                    _committedDestination, Faction.Player, _selectedUnit, FindUnitAt)
                : new List<TestUnit>();
            if (_cachedAdjacentAllies.Count > 0)
            {
                labels.Add("Trade");
                _cachedActionChoices.Add(ActionChoice.Trade);
            }

            if (_selectedUnit != null && _selectedUnit.isLord && Convoy.Current.IsAvailable)
            {
                labels.Add("Supply");
                _cachedActionChoices.Add(ActionChoice.Supply);
            }

            labels.Add("Wait");
            _cachedActionChoices.Add(ActionChoice.Wait);

            SetMode(CursorMode.ActionMenu);
            _actionMenuUI?.Show(labels, OnActionSelected, OnActionCancelled);
        }

        private void OnActionSelected(int index)
        {
            if (index < 0 || index >= _cachedActionChoices.Count)
            {
                CompleteAction();
                return;
            }

            switch (_cachedActionChoices[index])
            {
                case ActionChoice.Attack:
                    EnterTargetingMode();
                    break;
                case ActionChoice.Heal:
                    EnterHealTargetingMode();
                    break;
                case ActionChoice.Fortify:
                    TryCommitFortify();
                    break;
                case ActionChoice.Item:
                    OpenInventoryMenu();
                    break;
                case ActionChoice.Trade:
                    ShowTradeTargetMenu();
                    break;
                case ActionChoice.Supply:
                    OpenConvoyUI();
                    break;
                case ActionChoice.Wait:
                default:
                    CompleteAction();
                    break;
            }
        }

        private void OpenInventoryMenu()
        {
            if (_selectedUnit == null || _inventoryMenuUI == null)
            {
                ShowActionMenu();
                return;
            }

            _inventoryMenuUI.Show(_selectedUnit, _confirmDialogUI,
                onConsumableUsed: () => CompleteAction(),
                onClose: () => ShowActionMenu());
        }

        private void ShowTradeTargetMenu()
        {
            if (_cachedAdjacentAllies.Count == 1)
            {
                OpenTrade(_cachedAdjacentAllies[0]);
                return;
            }

            var names = new List<string>();
            foreach (var ally in _cachedAdjacentAllies)
                names.Add(ally.name);

            _actionMenuUI?.Show(names,
                index => OpenTrade(_cachedAdjacentAllies[index]),
                () => ShowActionMenu());
        }

        private void OpenTrade(TestUnit target)
        {
            if (_selectedUnit == null || _tradeUI == null)
            {
                ShowActionMenu();
                return;
            }

            var session = new TradeSession(_selectedUnit, target);
            _tradeUI.Show(session, _confirmDialogUI,
                onConfirm: () => ShowActionMenu(),
                onCancel: () => ShowActionMenu());
        }

        private void OpenConvoyUI()
        {
            if (_selectedUnit == null || _convoyUI == null)
            {
                ShowActionMenu();
                return;
            }
            var convoy = Convoy.Current as SupplyConvoy;
            if (convoy == null)
            {
                ShowActionMenu();
                return;
            }
            _convoyUI.Show(convoy, _selectedUnit, _toastUI, onClose: () => CompleteAction());
        }

        private void OnActionCancelled()
        {
            if (_unitMover != null)
                _unitMover.UndoMove(_selectedUnit, _selectedUnit.preMovementPosition);

            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);
            _rangeHighlighter?.ShowMovementRange(_currentReachability.Destinations, _currentReachability.PassThrough);

            SetPosition(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
        }

        private void EnterTargetingMode()
        {
            _isHealTargeting = false;

            if (_cachedEnemyTiles.Count == 0)
            {
                CompleteAction();
                return;
            }

            var enemyTileSet = new HashSet<Vector2Int>(_cachedEnemyTiles);
            _validMoveTiles = enemyTileSet;
            _targetTiles = new List<Vector2Int>(_cachedEnemyTiles);
            _targetTiles.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            _targetIndex = 0;

            _rangeHighlighter?.ShowAttackRange(enemyTileSet);

            SetPosition(_targetTiles[0]);
            SetMode(CursorMode.Targeting);
        }

        private List<Vector2Int> FindEnemiesInAttackRange()
        {
            var attackRange = _pathfindingService.ComputeAttackRange(
                new HashSet<Vector2Int> { _committedDestination },
                _selectedUnit.attackRangeMin, _selectedUnit.attackRangeMax);

            var enemyTiles = new List<Vector2Int>();
            foreach (var tile in attackRange)
            {
                var unit = FindUnitAt(tile);
                if (unit == null) continue;

                bool isEnemy = TurnManager.Instance != null
                    ? TurnManager.Instance.UnitRegistry.GetFaction(unit) == Faction.Enemy
                    : unit.faction == Faction.Enemy;

                if (isEnemy)
                    enemyTiles.Add(tile);
            }
            return enemyTiles;
        }

        private List<Vector2Int> FindAlliesInHealRange()
        {
            var staff = _selectedUnit.equippedWeapon;
            int mag = GetMagStat(_selectedUnit);

            var healRange = new HashSet<Vector2Int>();
            var map = _mapRenderer.CurrentMap;
            StaffRangeResolver.GetTargetTiles(staff, mag, _committedDestination, map.Width, map.Height, healRange);

            var allyTiles = new List<Vector2Int>();
            foreach (var tile in healRange)
            {
                var unit = FindUnitAt(tile);
                if (unit == null || unit == _selectedUnit) continue;

                bool isAlly = TurnManager.Instance != null
                    ? TurnManager.Instance.UnitRegistry.GetFaction(unit) != Faction.Enemy
                    : unit.faction != Faction.Enemy;

                if (!isAlly) continue;

                int hp = unit.UnitInstance != null ? unit.UnitInstance.CurrentHP : unit.currentHP;
                int maxHP = unit.UnitInstance != null ? unit.UnitInstance.MaxHP : unit.maxHP;

                if (hp < maxHP)
                    allyTiles.Add(tile);
            }
            return allyTiles;
        }

        private void EnterHealTargetingMode()
        {
            if (_cachedHealTiles.Count == 0)
            {
                ShowActionMenu();
                return;
            }

            _isHealTargeting = true;

            var healTileSet = new HashSet<Vector2Int>(_cachedHealTiles);
            _validMoveTiles = healTileSet;
            _targetTiles = new List<Vector2Int>(_cachedHealTiles);
            _targetTiles.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            _targetIndex = 0;

            _rangeHighlighter?.ShowHealRange(healTileSet);

            SetPosition(_targetTiles[0]);
            SetMode(CursorMode.Targeting);
        }

        private void TryCommitHeal()
        {
            var target = FindUnitAt(_gridPosition);
            if (target == null)
            {
                CompleteAction();
                return;
            }

            AnnounceBreaks(_selectedUnit, () =>
            {
                if (_selectedUnit.Inventory.TryUseStaff(target, out int healed, out string fail))
                    Debug.Log($"[Staff] {_selectedUnit.name} healed {target.name} for {healed} HP.");
                else
                    Debug.LogWarning($"[Staff] Heal failed: {fail}");
            });

            CompleteAction();
        }

        private void TryCommitFortify()
        {
            var allUnits = new List<TestUnit>(FindObjectsByType<TestUnit>(FindObjectsSortMode.None));

            AnnounceBreaks(_selectedUnit, () =>
            {
                if (_selectedUnit.Inventory.TryUseFortify(allUnits, out var healed, out string fail))
                {
                    foreach (var (unit, amount) in healed)
                        Debug.Log($"[Staff] {_selectedUnit.name} healed {unit.name} for {amount} HP (Fortify).");
                }
                else
                {
                    Debug.LogWarning($"[Staff] Fortify failed: {fail}");
                }
            });

            CompleteAction();
        }

        private static int GetMagStat(TestUnit unit)
        {
            return unit.UnitInstance != null ? unit.UnitInstance.Stats[StatIndex.Mag] : 0;
        }

        private void CompleteAction()
        {
            if (_selectedUnit != null)
            {
                if (TurnManager.Instance != null)
                    TurnManager.Instance.UnitRegistry.MarkActed(_selectedUnit);
                else
                    _selectedUnit.MarkActed();
            }

            _memorizedPosition = null;
            ResetUnitTilesMode();

            TurnManager.Instance?.CheckAutoEndPlayerPhase();
        }

        private void TryCommitAttack()
        {
            var defender = FindUnitAt(_gridPosition);
            if (defender == null)
            {
                CompleteAction();
                return;
            }

            if (_selectedUnit.Inventory.IsUnarmed)
            {
                Debug.LogWarning($"[Combat] {_selectedUnit.name} is unarmed; cannot attack.");
                CompleteAction();
                return;
            }

            var result = ResolveCombat(_selectedUnit, defender);
            LogCombatResult(_selectedUnit, defender, result);
            ApplyCombatResult(_selectedUnit, defender, result);
            ApplyDurability(_selectedUnit, defender, result);

            CompleteAction();
        }

        private void ApplyDurability(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            if (result.AttackerFired)
            {
                AnnounceBreaks(attacker, () => attacker.Inventory.ConsumeEquippedWeaponUses(1));
            }
            if (result.DefenderFired)
            {
                AnnounceBreaks(defender, () => defender.Inventory.ConsumeEquippedWeaponUses(1));
            }
        }

        private void AnnounceBreaks(TestUnit unit, Action mutate)
        {
            void OnDestroyed(InventoryItem item)
            {
                if (_toastUI != null)
                {
                    string message = item.kind == ItemKind.Weapon
                        ? $"{item.weapon.name} broke!"
                        : $"{item.consumable.name} depleted";
                    _toastUI.Show(message);
                }
            }

            unit.Inventory.OnItemDestroyed += OnDestroyed;
            try { mutate(); }
            finally { unit.Inventory.OnItemDestroyed -= OnDestroyed; }
        }

        private CombatResult ResolveCombat(TestUnit attacker, TestUnit defender)
        {
            int distance = Mathf.Abs(attacker.gridPosition.x - defender.gridPosition.x)
                         + Mathf.Abs(attacker.gridPosition.y - defender.gridPosition.y);

            var atkData = BuildCombatantData(attacker, distance);
            var defData = BuildCombatantData(defender, distance);

            var (defTerrainDef, defTerrainAvo) = GetTerrainBonuses(defender);
            var (atkTerrainDef, atkTerrainAvo) = GetTerrainBonuses(attacker);

            var atkClass = attacker.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;
            var defClass = defender.UnitInstance?.CurrentClass?.ClassType ?? ClassType.Infantry;

            return CombatRound.Resolve(atkData, defData, defTerrainDef, defTerrainAvo,
                atkTerrainDef, atkTerrainAvo, new UnityRng(), atkClass, defClass);
        }

        private CombatantData BuildCombatantData(TestUnit unit, int distance)
        {
            if (unit.UnitInstance != null)
            {
                return CombatantData.FromStats(unit.UnitInstance.Stats,
                    unit.UnitInstance.CurrentHP, unit.UnitInstance.MaxHP,
                    unit.equippedWeapon, distance);
            }

            var stats = StatArray.From(unit.maxHP, 8, 3, 7, 9, 5, 2, 6, 5);
            return CombatantData.FromStats(stats, unit.currentHP, unit.maxHP, unit.equippedWeapon, distance);
        }

        private (int def, int avo) GetTerrainBonuses(TestUnit unit)
        {
            if (_mapRenderer == null || _terrainStatTable == null)
                return (0, 0);

            var terrain = _mapRenderer.GetTerrainType(unit.gridPosition.x, unit.gridPosition.y);
            var stats = _terrainStatTable.GetStats(terrain);
            return TerrainStatTable.GetTerrainBonuses(stats, unit.movementType);
        }

        private static void LogCombatResult(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            foreach (var hit in result.Hits)
            {
                string target = hit.Attacker == "Attacker" ? defender.name : attacker.name;
                string source = hit.Attacker == "Attacker" ? attacker.name : defender.name;

                if (hit.Hit)
                {
                    string critText = hit.Crit ? " CRITICAL!" : "";
                    Debug.Log($"[Combat] {source} → {target}: {hit.Damage} damage{critText}");
                }
                else
                {
                    Debug.Log($"[Combat] {source} → {target}: MISS");
                }
            }

            if (result.TriangleAdvantage != 0)
                Debug.Log($"[Combat] Weapon Triangle: {(result.TriangleAdvantage > 0 ? "Attacker advantage" : "Defender advantage")}");
            if (result.AttackerEffective)
                Debug.Log($"[Combat] Effective weapon!");
            Debug.Log($"[Combat] Result: {attacker.name} HP={result.AttackerHPAfter}, {defender.name} HP={result.DefenderHPAfter}");
        }

        private static void ApplyCombatResult(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            if (attacker.UnitInstance != null)
                attacker.UnitInstance.SetCurrentHP(result.AttackerHPAfter);
            else
                attacker.currentHP = result.AttackerHPAfter;

            if (defender.UnitInstance != null)
                defender.UnitInstance.SetCurrentHP(result.DefenderHPAfter);
            else
                defender.currentHP = result.DefenderHPAfter;

            if (result.DefenderDied)
            {
                defender.gameObject.SetActive(false);
                if (defender.isLord) Convoy.Current = NullConvoy.Instance;
            }
            if (result.AttackerDied)
            {
                attacker.gameObject.SetActive(false);
                if (attacker.isLord) Convoy.Current = NullConvoy.Instance;
            }
        }

        internal void HandleCancel()
        {
            if (!CanCursorMove())
                return;

            switch (_currentMode)
            {
                case CursorMode.UnitSelected:
                    DeselectUnit();
                    break;
                case CursorMode.Targeting:
                    CancelTargeting();
                    break;
            }
        }

        private void DeselectUnit()
        {
            ResetUnitTilesMode();
            ReturnToMemorizedPosition();
        }

        private void ResetUnitTilesMode()
        {
            _selectedUnit = null;
            _validMoveTiles = null;
            _targetTiles = null;
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            SetMode(CursorMode.Free);
        }

        private void CancelTargeting()
        {
            _isHealTargeting = false;
            _targetTiles = null;
            _rangeHighlighter?.ClearAll();

            SetPosition(_committedDestination);
            ShowActionMenu();
        }

        private void OnGameStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap)
                SetMode(CursorMode.Free);
            else
                SetMode(CursorMode.Locked);
        }

        private void UpdateModeFromGameState()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.BattleMap)
                SetMode(CursorMode.Free);
            else
                SetMode(CursorMode.Free); // Default to Free when loaded directly
        }
#endregion

        #region Debug utilities
        internal void Initialize(MapRenderer mapRenderer, TerrainStatTable terrainStatTable)
        {
            _mapRenderer = mapRenderer;
            _terrainStatTable = terrainStatTable;
            if (_mapRenderer != null && _terrainStatTable != null)
                _pathfindingService = new PathfindingService(_mapRenderer, _terrainStatTable);
            _gridPosition = Vector2Int.zero;
            _currentMode = CursorMode.Free;
        }

        internal void SetSpriteRenderer(SpriteRenderer sr)
        {
            _spriteRenderer = sr;
        }

        internal void SetRangeHighlighter(RangeHighlighter rh)
        {
            _rangeHighlighter = rh;
        }

        internal void SetPathArrowRenderer(PathArrowRenderer par)
        {
            _pathArrowRenderer = par;
        }

        internal void SetActionMenuUI(UnitActionMenuUI ui)
        {
            _actionMenuUI = ui;
        }

        internal void SetUnitMover(UnitMover mover)
        {
            _unitMover = mover;
        }

        internal void SetModeSprites(Sprite idle, Sprite selected, Sprite targeting)
        {
            _idleSprite = idle;
            _selectedSprite = selected;
            _targetingSprite = targeting;
        }
        #endregion

    }
}
