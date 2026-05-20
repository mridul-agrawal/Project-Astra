using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.UI.Convoy;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.UI.Inventory;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.UI.Trade;
using ProjectAstra.Core.UI.UnitInfo;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // TODO(refactor): split this class. It currently owns cursor movement,
    // mode state, unit selection, action-menu orchestration, targeting cycle,
    // combat resolution, staff/heal flow, canto, trade, and convoy. Sensible
    // extractions: ActionMenuController, CombatExecutor, StaffActionExecutor,
    // TargetingController, CantoController.
    //
    // Grid-snapped cursor for the tactical battle map. Tracks integer grid
    // position, drives the five cursor modes, constrains movement to valid
    // tile sets, and orchestrates the action flow once a unit is selected.
    // DAS input repeat is owned by InputManager — this class just reacts to
    // OnCursorMove.
    public class GridCursor : MonoBehaviour
    {
        // --- Inspector dependencies ---

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
        [SerializeField] private TradeScreenUI _tradeUI;
        [SerializeField] private ConvoyUI _convoyUI;
        [SerializeField] private UnitInfoPanelUI _unitInfoPanelUI;
        [SerializeField] private CombatForecastUI _combatForecastUI;

        [Header("UM-01 War's Ledger")]
        [SerializeField] private UnitDeathEventChannel _deathEventChannel;

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

        // --- Runtime state ---

        private Vector2Int _gridPosition;
        private CursorMode _currentMode = CursorMode.Locked;
        private CursorAnimator _animator;
        private PathfindingService _pathfindingService;
        private CombatExecutor _combatExecutor;
        private StaffExecutor _staffExecutor;
        private TargetingFlow _targetingFlow;

        // Movement constraint — null means unconstrained (Free mode).
        private HashSet<Vector2Int> _validMoveTiles;

        // Cursor position to restore on cancel (e.g., back to the unit tile).
        private Vector2Int? _memorizedPosition;

        // Unit selection + reachability.
        private TestUnit _selectedUnit;
        private Pathfinder.ReachabilityResult _currentReachability;
        private Vector2Int _committedDestination;

        // Caches populated when the action menu is built.
        private List<Vector2Int> _cachedEnemyTiles = new();
        private List<Vector2Int> _cachedHealTiles = new();
        private List<ActionChoice> _cachedActionChoices = new();
        private List<TestUnit> _cachedAdjacentAllies = new();

        // Canto — cavalry/flying units re-enter movement mode after a primary action.
        private bool _isCantoMode;
        private int _preCantoMovementPoints;
        private ActionChoice? _lastActionChoice;

        private enum ActionChoice { Attack, Heal, Fortify, Item, Trade, Supply, Wait }

        public Vector2Int GridPosition => _gridPosition;
        public CursorMode CurrentMode => _currentMode;

        // Fires after the cursor moves to a new tile.
        public event Action<Vector2Int> OnCursorMoved;

        // --- Unity lifecycle ---

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
            InitializeCombatExecutor();
            InitializeStaffExecutor();
            InitializeTargetingFlow();
            SetPosition(Vector2Int.zero);
            UpdateModeFromGameState();
        }

        private void Update()
        {
            _animator?.UpdatePulse(_pulseSpeed, _alphaMin, _alphaMax, _scaleMin, _scaleMax);
        }

        // --- Public API: mode and position ---

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

        // --- Input entry points ---

        internal void HandleCursorMove(Vector2Int direction)
        {
            if (!CanCursorMove()) return;

            if (_currentMode == CursorMode.Targeting)
            {
                _targetingFlow.Cycle(direction);
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

        internal void HandleConfirm()
        {
            if (!CanCursorMove()) return;

            switch (_currentMode)
            {
                case CursorMode.Free:
                    TrySelectUnit();
                    break;
                case CursorMode.UnitSelected:
                    TryCommitMovement();
                    break;
                case CursorMode.Targeting:
                    if (_targetingFlow.IsHealTargeting)
                        _staffExecutor.TryCommitHeal(_selectedUnit, FindUnitAt(_gridPosition), CompleteAction);
                    else
                        _combatExecutor.TryCommitAttack(_selectedUnit, FindUnitAt(_gridPosition), CompleteAction);
                    break;
            }
        }

        internal void HandleCancel()
        {
            if (!CanCursorMove()) return;

            switch (_currentMode)
            {
                case CursorMode.UnitSelected:
                    if (_isCantoMode) { ClearOverlay(); FinalizeCanto(); break; }
                    DeselectUnit();
                    break;
                case CursorMode.Targeting:
                    CancelTargeting();
                    break;
            }
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

        private void HandleOpenUnitInfo()
        {
            if (!CanCursorMove()) return;
            if (_currentMode == CursorMode.Locked) return;

            TestUnit unit = FindUnitAt(_gridPosition);
            if (unit == null) return;

            _unitInfoPanelUI?.Show(unit, UnitInfoContext.BattleMap);
        }

        // --- Initialization helpers ---

        private void InitializeCursorAnimator()
        {
            if (_spriteRenderer != null)
                _animator = new CursorAnimator(_spriteRenderer);
        }

        private void InitializePathFindingService()
        {
            if (_mapRenderer != null && _terrainStatTable != null)
                _pathfindingService = new PathfindingService(_mapRenderer, _terrainStatTable);
        }

        private void InitializeCombatExecutor()
        {
            _combatExecutor = new CombatExecutor(
                _mapRenderer, _terrainStatTable, _deathEventChannel,
                _combatForecastUI, _toastUI);
        }

        private void InitializeStaffExecutor()
        {
            _staffExecutor = new StaffExecutor(_combatForecastUI, _toastUI);
        }

        private void InitializeTargetingFlow()
        {
            _targetingFlow = new TargetingFlow(
                _pathfindingService, _mapRenderer,
                _rangeHighlighter, _combatForecastUI, this);
        }

        private void AddListenersToInputEvents()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += HandleCursorMove;
            InputManager.Instance.OnConfirm += HandleConfirm;
            InputManager.Instance.OnCancel += HandleCancel;
            InputManager.Instance.OnNextUnit += HandleNextUnit;
            InputManager.Instance.OnPrevUnit += HandlePrevUnit;
            InputManager.Instance.OnOpenUnitInfo += HandleOpenUnitInfo;
        }

        private void RemoveListenersFromInputEvents()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= HandleCursorMove;
            InputManager.Instance.OnConfirm -= HandleConfirm;
            InputManager.Instance.OnCancel -= HandleCancel;
            InputManager.Instance.OnNextUnit -= HandleNextUnit;
            InputManager.Instance.OnPrevUnit -= HandlePrevUnit;
            InputManager.Instance.OnOpenUnitInfo -= HandleOpenUnitInfo;
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

        // --- Mode/position internals ---

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
                Mathf.Clamp(pos.y, 0, map.Height - 1));
        }

        private void SnapToGridPosition()
        {
            transform.position = new Vector3(_gridPosition.x + 0.5f, _gridPosition.y + 0.5f, 0f);
        }

        private bool CanCursorMove()
        {
            if (_currentMode == CursorMode.Locked) return false;
            if (_currentMode == CursorMode.ActionMenu) return false;
            if (BattleMapUI.HasInputFocus) return false;
            if (UnitActionMenuUI.HasInputFocus) return false;
            if (InventoryMenuUI.HasInputFocus) return false;
            if (ConfirmDialogUI.HasInputFocus) return false;
            if (TradeScreenUI.HasInputFocus) return false;
            if (ConvoyUI.HasInputFocus) return false;
            if (UnitInfoPanelUI.HasInputFocus) return false;
            if (_unitMover != null && _unitMover.IsMoving) return false;
            return true;
        }

        private bool IsMovementConstrained() => _validMoveTiles != null;

        // --- Cursor movement details ---

        private void UpdatePathArrow()
        {
            if (_pathArrowRenderer == null || _selectedUnit == null) return;

            if (!IsCurrentTileReachable())
            {
                _pathArrowRenderer.Clear();
                return;
            }

            var path = Pathfinder.ReconstructPath(_selectedUnit.gridPosition, _gridPosition, _currentReachability);
            _pathArrowRenderer.ShowPath(path);
        }

        private bool IsCurrentTileReachable() =>
            _currentReachability.Destinations.Contains(_gridPosition);

        // --- Unit selection ---

        private void TrySelectUnit()
        {
            TestUnit unit = FindUnitAt(_gridPosition);
            if (!IsUnitSelectable(unit)) return;

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
                return true;
            }

            // Test seam: no TurnManager means we fall back to the unit's own flag.
            return !unit.hasActed;
        }

        private void EnterUnitSelectedMode()
        {
            if (_pathfindingService == null || _selectedUnit == null) return;

            _currentReachability = _pathfindingService.ComputeReachability(
                _selectedUnit.gridPosition, _selectedUnit.movementPoints,
                _selectedUnit.movementType, GetOccupantType);

            RestoreMovementConstraintsAndOverlay();
            SetPositionWithMemory(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
        }

        // TODO(refactor): hook this up to a real unit-occupancy service when
        // the unit-management system lands. Returning None makes every tile
        // look unoccupied to the pathfinder.
        private Pathfinder.OccupantType GetOccupantType(Vector2Int pos)
        {
            return Pathfinder.OccupantType.None;
        }

        private void RestoreMovementConstraintsAndOverlay()
        {
            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);
            _rangeHighlighter?.ShowMovementRange(_currentReachability.Destinations, _currentReachability.PassThrough);
        }

        // --- Movement commit ---

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

        private bool PathExists(List<Vector2Int> path) =>
            _unitMover != null && path != null && path.Count > 1;

        private void OnMovementComplete()
        {
            if (_isCantoMode)
            {
                FinalizeCanto();
                return;
            }
            ShowActionMenu();
        }

        // --- Action menu ---

        private void ShowActionMenu()
        {
            var labels = new List<string>();
            _cachedActionChoices.Clear();
            _cachedEnemyTiles = _targetingFlow.GetEnemiesInAttackRange(_selectedUnit, _committedDestination);

            TryAddAttackAction(labels);
            TryAddStaffAction(labels);
            TryAddItemAction(labels);
            TryAddTradeAction(labels);
            TryAddSupplyAction(labels);

            labels.Add("Wait");
            _cachedActionChoices.Add(ActionChoice.Wait);

            SetMode(CursorMode.ActionMenu);
            _actionMenuUI?.Show(labels, OnActionSelected, OnActionCancelled);
        }

        private void TryAddAttackAction(List<string> labels)
        {
            if (_cachedEnemyTiles.Count == 0) return;
            if (_selectedUnit == null || _selectedUnit.Inventory.IsUnarmed) return;

            labels.Add("Attack");
            _cachedActionChoices.Add(ActionChoice.Attack);
        }

        private void TryAddStaffAction(List<string> labels)
        {
            if (_selectedUnit == null) return;

            var staff = _selectedUnit.equippedWeapon;
            if (staff.weaponType != WeaponType.Staff) return;
            if (staff.staffEffect == StaffEffect.None || staff.IsBroken) return;

            if (staff.staffEffect == StaffEffect.AreaOfEffect)
                TryAddFortifyAction(labels);
            else
                TryAddHealAction(labels);
        }

        private void TryAddFortifyAction(List<string> labels)
        {
            var staff = _selectedUnit.equippedWeapon;
            int mag = GetMagStat(_selectedUnit);
            var allUnits = FindObjectsByType<TestUnit>(FindObjectsSortMode.None);

            foreach (var u in allUnits)
            {
                if (u == _selectedUnit) continue;
                if (!StaffEffects.CanHealTarget(_selectedUnit, u, staff, mag, out _)) continue;

                labels.Add("Fortify");
                _cachedActionChoices.Add(ActionChoice.Fortify);
                return;
            }
        }

        private void TryAddHealAction(List<string> labels)
        {
            _cachedHealTiles = _targetingFlow.GetAlliesInHealRange(
                _selectedUnit, _committedDestination, GetMagStat(_selectedUnit));
            if (_cachedHealTiles.Count == 0) return;

            labels.Add("Heal");
            _cachedActionChoices.Add(ActionChoice.Heal);
        }

        private void TryAddItemAction(List<string> labels)
        {
            if (_selectedUnit == null || _selectedUnit.Inventory.OccupiedCount == 0) return;

            labels.Add("Item");
            _cachedActionChoices.Add(ActionChoice.Item);
        }

        private void TryAddTradeAction(List<string> labels)
        {
            _cachedAdjacentAllies = _selectedUnit != null
                ? AdjacentAllyFinder.FindAdjacentAllies(
                    _committedDestination, Faction.Player, _selectedUnit, FindUnitAt)
                : new List<TestUnit>();

            if (_cachedAdjacentAllies.Count == 0) return;

            labels.Add("Trade");
            _cachedActionChoices.Add(ActionChoice.Trade);
        }

        private void TryAddSupplyAction(List<string> labels)
        {
            if (_selectedUnit == null || !_selectedUnit.isLord) return;
            if (!Convoy.Current.IsAvailable) return;

            labels.Add("Supply");
            _cachedActionChoices.Add(ActionChoice.Supply);
        }

        private void OnActionSelected(int index)
        {
            if (index < 0 || index >= _cachedActionChoices.Count)
            {
                _lastActionChoice = ActionChoice.Wait;
                CompleteAction();
                return;
            }

            _lastActionChoice = _cachedActionChoices[index];

            switch (_cachedActionChoices[index])
            {
                case ActionChoice.Attack: EnterTargetingMode(); break;
                case ActionChoice.Heal: EnterHealTargetingMode(); break;
                case ActionChoice.Fortify: _staffExecutor.TryCommitFortify(_selectedUnit, CompleteAction); break;
                case ActionChoice.Item: OpenInventoryMenu(); break;
                case ActionChoice.Trade: ShowTradeTargetMenu(); break;
                case ActionChoice.Supply: OpenConvoyUI(); break;
                case ActionChoice.Wait:
                default: CompleteAction(); break;
            }
        }

        private void OnActionCancelled()
        {
            _combatForecastUI?.Hide();

            if (_unitMover != null)
                _unitMover.UndoMove(_selectedUnit, _selectedUnit.preMovementPosition);

            RestoreMovementConstraintsAndOverlay();
            SetPosition(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
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
            if (_selectedUnit == null || _convoyUI == null) { ShowActionMenu(); return; }

            var convoy = Convoy.Current as SupplyConvoy;
            if (convoy == null) { ShowActionMenu(); return; }

            _convoyUI.Show(convoy, _selectedUnit, _toastUI, onClose: () => CompleteAction());
        }

        // --- Targeting (entry points; the work lives on _targetingFlow) ---

        private void EnterTargetingMode()
        {
            if (_cachedEnemyTiles.Count == 0)
            {
                CompleteAction();
                return;
            }

            _validMoveTiles = new HashSet<Vector2Int>(_cachedEnemyTiles);
            _targetingFlow.EnterAttackTargeting(_selectedUnit, _cachedEnemyTiles);
        }

        private void EnterHealTargetingMode()
        {
            if (_cachedHealTiles.Count == 0)
            {
                ShowActionMenu();
                return;
            }

            _validMoveTiles = new HashSet<Vector2Int>(_cachedHealTiles);
            _targetingFlow.EnterHealTargeting(_selectedUnit, _cachedHealTiles);
        }

        // Used by TryAddFortifyAction to gate staff targeting on the unit's Magic stat.
        private static int GetMagStat(TestUnit unit) =>
            unit.UnitInstance != null ? unit.UnitInstance.Stats[StatIndex.Mag] : 0;

        // --- Cancel / cleanup ---

        private void DeselectUnit()
        {
            ResetUnitTilesMode();
            ReturnToMemorizedPosition();
        }

        private void ResetUnitTilesMode()
        {
            _isCantoMode = false;
            _lastActionChoice = null;
            _selectedUnit = null;
            _validMoveTiles = null;
            _targetingFlow?.ClearState();
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            SetMode(CursorMode.Free);
        }

        private void CancelTargeting()
        {
            _targetingFlow.Cancel();
            SetPosition(_committedDestination);
            ShowActionMenu();
        }

        // --- Canto ---

        private void CompleteAction()
        {
            if (TryEnterCanto()) return;
            FinishSelectedUnitTurn();
        }

        private bool TryEnterCanto()
        {
            if (_isCantoMode) return false;
            if (_selectedUnit == null) return false;
            if (_lastActionChoice == ActionChoice.Wait || _lastActionChoice == null) return false;

            var cls = _selectedUnit.UnitInstance?.CurrentClass;
            if (cls == null || !cls.HasCanto) return false;

            int costPaid = _currentReachability.CostMap != null
                && _currentReachability.CostMap.TryGetValue(_selectedUnit.gridPosition, out var c) ? c : 0;
            int remaining = _selectedUnit.movementPoints - costPaid;
            if (remaining <= 0) return false;

            _preCantoMovementPoints = _selectedUnit.movementPoints;
            _selectedUnit.movementPoints = remaining;
            _isCantoMode = true;

            EnterUnitSelectedMode();

            // The unit's own tile is a legal "stay put" exit during canto, so
            // make sure the constraint set includes it even when reachability
            // would otherwise exclude it.
            if (_validMoveTiles != null && !_validMoveTiles.Contains(_selectedUnit.gridPosition))
                _validMoveTiles.Add(_selectedUnit.gridPosition);

            return true;
        }

        private void FinalizeCanto()
        {
            _isCantoMode = false;
            if (_selectedUnit != null)
                _selectedUnit.movementPoints = _preCantoMovementPoints;
            _lastActionChoice = null;

            FinishSelectedUnitTurn();
        }

        private void FinishSelectedUnitTurn()
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

        // --- Game state events ---

        private void OnGameStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap)
                SetMode(CursorMode.Free);
            else
                SetMode(CursorMode.Locked);
        }

        private void UpdateModeFromGameState()
        {
            // Always start in Free. OnGameStateChanged will lock us back down
            // if we're not actually in the BattleMap state — and when the
            // cursor is loaded into a scene directly (test seam), Free is the
            // correct default anyway.
            SetMode(CursorMode.Free);
        }

        // --- Test seams ---

        internal void Initialize(MapRenderer mapRenderer, TerrainStatTable terrainStatTable)
        {
            _mapRenderer = mapRenderer;
            _terrainStatTable = terrainStatTable;
            if (_mapRenderer != null && _terrainStatTable != null)
                _pathfindingService = new PathfindingService(_mapRenderer, _terrainStatTable);
            _gridPosition = Vector2Int.zero;
            _currentMode = CursorMode.Free;
        }

        internal void SetSpriteRenderer(SpriteRenderer sr) => _spriteRenderer = sr;
        internal void SetRangeHighlighter(RangeHighlighter rh) => _rangeHighlighter = rh;
        internal void SetPathArrowRenderer(PathArrowRenderer par) => _pathArrowRenderer = par;
        internal void SetActionMenuUI(UnitActionMenuUI ui) => _actionMenuUI = ui;
        internal void SetUnitMover(UnitMover mover) => _unitMover = mover;

        internal void SetModeSprites(Sprite idle, Sprite selected, Sprite targeting)
        {
            _idleSprite = idle;
            _selectedSprite = selected;
            _targetingSprite = targeting;
        }
    }
}
