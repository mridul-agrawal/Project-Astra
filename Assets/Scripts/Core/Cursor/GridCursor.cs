using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.UI.CombatAnimation;
using ProjectAstra.Core.Dialogue;
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

        [Header("Combat Animation")]
        [SerializeField] private SkipModePlaybackController _skipModeController;

        [Header("Tutorial Dialogue")]
        [SerializeField] private BattleDialogueEventChannel _battleDialogueChannel;

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
        private UnitSelectionFlow _unitSelectionFlow;


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

        // Safety net: scene unload during the same frame as a queued input
        // callback can leave OnDisable's unsubscribe behind, leaking a stale
        // delegate into the DontDestroyOnLoad InputManager.
        private void OnDestroy()
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
            InitializeUnitSelectionFlow();
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

        // Stashes the current cursor position so a later
        // ReturnToMemorizedPosition can restore it (e.g. unit deselect).
        // The memory itself lives on _unitSelectionFlow; this method stays
        // on the cursor for test surface compatibility.
        public void SetPositionWithMemory(Vector2Int position)
        {
            _unitSelectionFlow?.RecordMemorizedPosition(_gridPosition);
            SetPosition(position);
        }

        public void ReturnToMemorizedPosition()
        {
            if (_unitSelectionFlow == null) return;
            if (_unitSelectionFlow.TryConsumeMemorizedPosition(out var pos))
                SetPosition(pos);
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

            if (_unitSelectionFlow.IsMovementConstrained
                && !_unitSelectionFlow.ValidMoveTiles.Contains(targetGridPosition))
                return;

            if (targetGridPosition == _gridPosition)
                return;

            _gridPosition = targetGridPosition;
            SnapToGridPosition();
            OnCursorMoved?.Invoke(_gridPosition);

            if (_currentMode == CursorMode.UnitSelected)
                _unitSelectionFlow.UpdatePathArrow(_gridPosition);
        }

        internal void HandleConfirm()
        {
            if (!CanCursorMove()) return;

            switch (_currentMode)
            {
                case CursorMode.Free:
                    _unitSelectionFlow.TrySelectUnit(_gridPosition);
                    if (_currentMode == CursorMode.UnitSelected)
                        _battleDialogueChannel?.Raise(BattleDialogueEventType.UnitSelected);
                    break;
                case CursorMode.UnitSelected:
                    _unitSelectionFlow.TryCommitMovement(_gridPosition);
                    _battleDialogueChannel?.Raise(BattleDialogueEventType.MoveConfirmed);
                    break;
                case CursorMode.Targeting:
                    var selected = _unitSelectionFlow.SelectedUnit;
                    var target = FindUnitAt(_gridPosition);
                    if (_targetingFlow.IsHealTargeting)
                        _staffExecutor.TryCommitHeal(selected, target, _unitSelectionFlow.CompleteAction);
                    else
                    {
                        if (target != null) _battleDialogueChannel?.Raise(BattleDialogueEventType.PreCombat);
                        ApplyPerCombatSpeedOverrideIfHeld();
                        _combatExecutor.TryCommitAttack(selected, target, _unitSelectionFlow.CompleteAction);
                    }
                    break;
            }
        }

        internal void HandleCancel()
        {
            if (!CanCursorMove()) return;

            switch (_currentMode)
            {
                case CursorMode.UnitSelected:
                    if (_cantoFlow.IsCantoMode) { _unitSelectionFlow.FinishCantoFromCancel(); break; }
                    _unitSelectionFlow.DeselectUnit();
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

        // Init methods are idempotent — both Start() and the Initialize()
        // test seam call them. Already-constructed sub-controllers are not
        // replaced so test-injected state survives Start().

        private void InitializePathFindingService()
        {
            if (_pathfindingService != null) return;
            if (_mapRenderer != null && _terrainStatTable != null)
                _pathfindingService = new PathfindingService(_mapRenderer, _terrainStatTable);
        }

        private void InitializeCombatExecutor()
        {
            if (_combatExecutor != null) return;
            var dispatcher = new CombatPlaybackDispatcher(_skipModeController);
            _combatExecutor = new CombatExecutor(
                _mapRenderer, _terrainStatTable, _deathEventChannel,
                _combatForecastUI, _toastUI, dispatcher);
        }

        private void InitializeStaffExecutor()
        {
            if (_staffExecutor != null) return;
            _staffExecutor = new StaffExecutor(_combatForecastUI, _toastUI);
        }

        private void InitializeTargetingFlow()
        {
            if (_targetingFlow != null) return;
            _targetingFlow = new TargetingFlow(
                _pathfindingService, _mapRenderer,
                _rangeHighlighter, _combatForecastUI, this);
        }

        private void InitializeActionMenuFlow()
        {
            if (_actionMenuFlow != null) return;
            _actionMenuFlow = new ActionMenuFlow(
                _actionMenuUI, _inventoryMenuUI, _confirmDialogUI,
                _tradeUI, _convoyUI, _toastUI,
                _targetingFlow, _staffExecutor, this);
        }

        private void InitializeCantoFlow()
        {
            if (_cantoFlow != null) return;
            _cantoFlow = new CantoFlow(this, _actionMenuFlow);
        }

        private void InitializeUnitSelectionFlow()
        {
            if (_unitSelectionFlow != null) return;
            _unitSelectionFlow = new UnitSelectionFlow(
                _pathfindingService, _unitMover,
                _rangeHighlighter, _pathArrowRenderer, _combatForecastUI,
                this, _cantoFlow, _actionMenuFlow);
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

        // Hold SkipAnimation while confirming a target to flip the combat-anim
        // speed for that single combat. Persisted Normal/Fast → Skip; persisted
        // Skip → Normal. The dispatcher clears the override on combat complete.
        private static void ApplyPerCombatSpeedOverrideIfHeld()
        {
            var im = InputManager.Instance;
            var settings = CombatAnimationSettingsRef.Current;
            if (im == null || settings == null) return;
            if (!im.IsActionHeld(InputContext.SkipAnimation)) return;
            var current = settings.EffectiveSpeed;
            var flipped = current == CombatAnimationSpeed.Skip
                ? CombatAnimationSpeed.Normal
                : CombatAnimationSpeed.Skip;
            settings.SetOneShotOverride(flipped);
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

        // --- Selection-flow seams (forward to _unitSelectionFlow) ---

        internal void SetValidMoveTiles(HashSet<Vector2Int> tiles) =>
            _unitSelectionFlow.SetValidMoveTiles(tiles);

        internal void EnsureMoveTileAllowed(Vector2Int tile) =>
            _unitSelectionFlow.EnsureMoveTileAllowed(tile);

        internal void EnterUnitSelectedMode() =>
            _unitSelectionFlow.EnterUnitSelectedMode();

        // --- Cancel / cleanup ---

        private void CancelTargeting()
        {
            _targetingFlow.Cancel();
            SetPosition(_unitSelectionFlow.CommittedDestination);
            _unitSelectionFlow.ShowActionMenu();
        }

        // --- Lookup helper kept on the cursor because the input handlers
        // (HandleNextUnit / HandlePrevUnit / HandleOpenUnitInfo /
        // HandleConfirm.Targeting) need it at cursor scope, not selection
        // scope. The selection flow has its own copy internally. ---

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                if (unit.gridPosition == pos)
                    return unit;
            return null;
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
            _gridPosition = Vector2Int.zero;
            _currentMode = CursorMode.Free;

            // Build the sub-controllers eagerly so tests can drive the cursor
            // without going through Start(). Idempotent guards inside the
            // initializers prevent double-construction when Start() later runs
            // in a non-test scenario.
            InitializePathFindingService();
            InitializeCombatExecutor();
            InitializeStaffExecutor();
            InitializeTargetingFlow();
            InitializeActionMenuFlow();
            InitializeCantoFlow();
            InitializeUnitSelectionFlow();
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
