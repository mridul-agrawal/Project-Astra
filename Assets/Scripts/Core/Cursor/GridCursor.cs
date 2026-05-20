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
        private ActionMenuFlow _actionMenuFlow;
        private CantoFlow _cantoFlow;

        // Movement constraint — null means unconstrained (Free mode).
        private HashSet<Vector2Int> _validMoveTiles;

        // Cursor position to restore on cancel (e.g., back to the unit tile).
        private Vector2Int? _memorizedPosition;

        // Unit selection + reachability.
        private TestUnit _selectedUnit;
        private Pathfinder.ReachabilityResult _currentReachability;
        private Vector2Int _committedDestination;


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
            InitializeActionMenuFlow();
            InitializeCantoFlow();
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
                    if (_cantoFlow.IsCantoMode) { ClearOverlay(); FinalizeCanto(); break; }
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

        private void InitializeActionMenuFlow()
        {
            _actionMenuFlow = new ActionMenuFlow(
                _actionMenuUI, _inventoryMenuUI, _confirmDialogUI,
                _tradeUI, _convoyUI, _toastUI,
                _targetingFlow, _staffExecutor, this);
        }

        private void InitializeCantoFlow()
        {
            _cantoFlow = new CantoFlow(this, _actionMenuFlow);
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

        // Lets ActionMenuFlow swap the cursor's allowed-tile set when
        // entering targeting mode (attack/heal tile set replaces the
        // movement set).
        internal void SetValidMoveTiles(HashSet<Vector2Int> tiles) => _validMoveTiles = tiles;

        // Lets CantoFlow add the unit's current tile to the allowed-set so a
        // "stay put" confirm during canto is legal even if reachability
        // wouldn't otherwise include it.
        internal void EnsureMoveTileAllowed(Vector2Int tile)
        {
            if (_validMoveTiles != null && !_validMoveTiles.Contains(tile))
                _validMoveTiles.Add(tile);
        }

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

        internal void EnterUnitSelectedMode()
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
            if (_cantoFlow.IsCantoMode)
            {
                FinalizeCanto();
                return;
            }
            ShowActionMenu();
        }

        // --- Action menu (thin shim; flow lives on _actionMenuFlow) ---

        private void ShowActionMenu()
        {
            _actionMenuFlow.Show(_selectedUnit, _committedDestination,
                onComplete: CompleteAction,
                onCancelToUnitSelected: OnActionCancelled);
        }

        // The action menu's Cancel button: undo the unit's movement and
        // unwind back to UnitSelected. Stays in GridCursor because it
        // touches selection-flow state (_currentReachability, _validMoveTiles,
        // cursor mode).
        private void OnActionCancelled()
        {
            _combatForecastUI?.Hide();

            if (_unitMover != null)
                _unitMover.UndoMove(_selectedUnit, _selectedUnit.preMovementPosition);

            RestoreMovementConstraintsAndOverlay();
            SetPosition(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
        }

        // --- Cancel / cleanup ---

        private void DeselectUnit()
        {
            ResetUnitTilesMode();
            ReturnToMemorizedPosition();
        }

        private void ResetUnitTilesMode()
        {
            _cantoFlow?.ResetState();
            _actionMenuFlow?.ClearLastChoice();
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

        // --- Post-action dispatch ---

        // Action's onComplete callback. Either canto fires (giving the unit
        // a second move-only round) or the turn ends.
        private void CompleteAction()
        {
            if (_cantoFlow.TryEnterCanto(_selectedUnit, _currentReachability, _validMoveTiles)) return;
            FinishSelectedUnitTurn();
        }

        private void FinalizeCanto()
        {
            _cantoFlow.FinalizeCanto(_selectedUnit);
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
