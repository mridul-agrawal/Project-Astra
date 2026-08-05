using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.UI.CombatAnimation;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.ActionMenu;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.UI.Convoy;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.UI.Inventory;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.UI.Trade;
using ProjectAstra.Core.UI.UnitInfo;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Animation;

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
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private TerrainStatTable terrainStatTable;
        [SerializeField] private RangeHighlighter rangeHighlighter;
        [SerializeField] private PathArrowRenderer pathArrowRenderer;
        [SerializeField] private UnitMover unitMover;
        [SerializeField] private SelectionMenuView actionMenuUI;
        [SerializeField] private InventoryMenuUI inventoryMenuUI;
        [SerializeField] private ConfirmDialogUI confirmDialogUI;
        [SerializeField] private ToastNotificationUI toastUI;
        [SerializeField] private TradeScreenUI tradeUI;
        [SerializeField] private ConvoyUI convoyUI;
        [SerializeField] private UnitInfoUIController unitInfoUIController;
        [SerializeField] private CombatForecastUIController combatForecastUI;

        [Header("Combat Animation")]
        [SerializeField] private SkipModePlaybackController skipModeController;

        // Kept only so the cursor can be hidden wholesale while locked. Everything else about
        // how the cursor looks now lives on CursorVisualDirector and its variant profile.
        [Header("Rendering")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // --- Runtime state ---

        private Vector2Int gridPosition;
        private CursorMode currentMode = CursorMode.Locked;
        private PathfindingService pathfindingService;
        private CombatExecutor combatExecutor;
        private StaffExecutor staffExecutor;
        private TargetingFlow targetingFlow;
        private ActionMenuController actionMenuController;
        private CantoFlow cantoFlow;
        private UnitSelectionFlow unitSelectionFlow;
        private HoverSelectionDriver hoverSelectionDriver;
        private readonly CursorStateMachine stateMachine = new();
        private bool moveInFlight;
        private TestUnit hoveredUnit;


        public Vector2Int GridPosition => gridPosition;
        public CursorMode CurrentMode => currentMode;

        // The richer state the visual variants subscribe to. CursorMode stays the input
        // contract; this says what the player is actually doing.
        public CursorStateMachine StateMachine => stateMachine;
        public CursorState CurrentState => stateMachine.CurrentState;
        public CursorHover CurrentHover => stateMachine.CurrentHover;

        // Fires after the cursor moves to a new tile.
        public event Action<Vector2Int> OnCursorMoved;

        // --- Unity lifecycle ---

        private void Awake()
        {
            stateMachine.HoverChanged += OnHoverChanged;
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
            OnCursorMoved -= RefreshHoverSelection;
            stateMachine.HoverChanged -= OnHoverChanged;
        }

        private void Start()
        {
            InitializePathFindingService();
            InitializeCombatExecutor();
            InitializeStaffExecutor();
            InitializeTargetingFlow();
            InitializeActionMenuController();
            InitializeCantoFlow();
            InitializeUnitSelectionFlow();
            InitializeHoverSelection();
            SetPosition(FindInitialCursorCell());
            UpdateModeFromGameState();
        }

        // --- Public API: mode and position ---

        public void SetMode(CursorMode mode)
        {
            CursorState target = ToState(mode);
            currentMode = mode;
            if (mode != CursorMode.Locked) moveInFlight = false;

            stateMachine.TryTransition(target);
            RefreshHover();
            ToggleSpriteRendererBasedOnCursorMode();
            hoverSelectionDriver?.Refresh();
        }

        // A walking unit and a cursor parked for a cutscene both sit in CursorMode.Locked.
        // UnitSelectionFlow calls this instead so the two stay distinguishable downstream.
        internal void BeginMove()
        {
            moveInFlight = true;
            SetMode(CursorMode.Locked);
        }

        public void SetPosition(Vector2Int position)
        {
            Vector2Int newClampedPosition = ClampToMapBounds(position);
            bool hasCursorPositionActuallyChanged = newClampedPosition != gridPosition;

            gridPosition = newClampedPosition;
            SnapToGridPosition();
            RefreshHover();

            if (hasCursorPositionActuallyChanged)
                OnCursorMoved?.Invoke(gridPosition);
        }

        // Stashes the current cursor position so a later
        // ReturnToMemorizedPosition can restore it (e.g. unit deselect).
        // The memory itself lives on _unitSelectionFlow; this method stays
        // on the cursor for test surface compatibility.
        public void SetPositionWithMemory(Vector2Int position)
        {
            unitSelectionFlow?.RecordMemorizedPosition(gridPosition);
            SetPosition(position);
        }

        public void ReturnToMemorizedPosition()
        {
            if (unitSelectionFlow == null) return;
            if (unitSelectionFlow.TryConsumeMemorizedPosition(out var pos))
                SetPosition(pos);
        }

        // --- Input entry points ---

        internal void HandleCursorMove(Vector2Int direction)
        {
            if (!CanCursorMove()) return;

            if (currentMode == CursorMode.Targeting)
            {
                targetingFlow.Cycle(direction);
                return;
            }

            Vector2Int targetGridPosition = ClampToMapBounds(gridPosition + direction);

            if (unitSelectionFlow.IsMovementConstrained
                && !unitSelectionFlow.ValidMoveTiles.Contains(targetGridPosition))
                return;

            if (targetGridPosition == gridPosition)
                return;

            gridPosition = targetGridPosition;
            SnapToGridPosition();
            RefreshHover();
            OnCursorMoved?.Invoke(gridPosition);
            EventService.Instance?.RaiseCursorStepped(gridPosition);
            AudioManager.Instance?.Play(SoundId.CursorMove);

            if (currentMode == CursorMode.UnitSelected)
                unitSelectionFlow.UpdatePathArrow(gridPosition);
        }

        internal void HandleConfirm()
        {
            if (!CanCursorMove()) return;

            switch (currentMode)
            {
                case CursorMode.Free:
                    unitSelectionFlow.TrySelectUnit(gridPosition);
                    if (currentMode == CursorMode.UnitSelected)
                    {
                        AudioManager.Instance?.Play(SoundId.ConfirmUnitSelect);
                        EventService.Instance?.RaiseBattleDialogue(BattleDialogueEventType.UnitSelected);
                    }
                    else
                    {
                        EventService.Instance?.RaiseCursorError(CursorHoverClassifier.ErrorFor(stateMachine.CurrentHover));
                    }
                    break;
                case CursorMode.UnitSelected:
                    if (!unitSelectionFlow.CanCommitMovementTo(gridPosition))
                    {
                        EventService.Instance?.RaiseCursorError(CursorErrorKind.InvalidTile);
                        break;
                    }
                    AudioManager.Instance?.Play(SoundId.ConfirmMove);
                    unitSelectionFlow.TryCommitMovement(gridPosition);
                    EventService.Instance?.RaiseBattleDialogue(BattleDialogueEventType.MoveConfirmed);
                    break;
                case CursorMode.Targeting:
                    var selected = unitSelectionFlow.SelectedUnit;
                    var target = FindUnitAt(gridPosition);
                    if (targetingFlow.IsHealTargeting)
                        staffExecutor.TryCommitHeal(selected, target, unitSelectionFlow.CompleteAction);
                    else
                    {
                        // Held-skip first, then PreCombat — a map script may force a speed
                        // for its scripted combats and must win over the held key.
                        ApplyPerCombatSpeedOverrideIfHeld();
                        if (target != null) EventService.Instance?.RaiseBattleDialogue(BattleDialogueEventType.PreCombat);
                        AudioManager.Instance?.Play(SoundId.ConfirmEngage);
                        combatExecutor.TryCommitAttack(selected, target, unitSelectionFlow.CompleteAction);
                    }
                    break;
            }
        }

        internal void HandleCancel()
        {
            if (!CanCursorMove()) return;

            switch (currentMode)
            {
                case CursorMode.UnitSelected:
                    AudioManager.Instance?.Play(SoundId.CancelGrid);
                    if (cantoFlow.IsCantoMode) { unitSelectionFlow.FinishCantoFromCancel(); break; }
                    unitSelectionFlow.DeselectUnit();
                    break;
                case CursorMode.Targeting:
                    AudioManager.Instance?.Play(SoundId.CancelGrid);
                    CancelTargeting();
                    break;
            }
        }

        private void HandleNextUnit()
        {
            if (!IsBattleMapState() || currentMode != CursorMode.Free || TurnManager.Instance == null) return;
            var next = TurnManager.Instance.UnitRegistry.GetNextUnactedUnit(Faction.Player, FindUnitAt(gridPosition));
            if (next != null) SetPosition(next.gridPosition);
        }

        private void HandlePrevUnit()
        {
            if (!IsBattleMapState() || currentMode != CursorMode.Free || TurnManager.Instance == null) return;
            var prev = TurnManager.Instance.UnitRegistry.GetPrevUnactedUnit(Faction.Player, FindUnitAt(gridPosition));
            if (prev != null) SetPosition(prev.gridPosition);
        }

        private void HandleOpenUnitInfo()
        {
            if (!CanCursorMove()) return;
            if (currentMode == CursorMode.Locked) return;

            TestUnit unit = FindUnitAt(gridPosition);
            if (unit == null) return;

            unitInfoUIController?.Open(unit);
        }

        // --- Initialization helpers ---

        // Init methods are idempotent — both Start() and the Initialize()
        // test seam call them. Already-constructed sub-controllers are not
        // replaced so test-injected state survives Start().

        private void InitializePathFindingService()
        {
            if (pathfindingService != null) return;
            if (mapRenderer != null && terrainStatTable != null)
                pathfindingService = new PathfindingService(mapRenderer, terrainStatTable);
        }

        private void InitializeCombatExecutor()
        {
            if (combatExecutor != null) return;
            var dispatcher = new CombatPlaybackDispatcher(skipModeController);
            combatExecutor = new CombatExecutor(
                mapRenderer, terrainStatTable,
                combatForecastUI, toastUI, dispatcher);
        }

        private void InitializeStaffExecutor()
        {
            if (staffExecutor != null) return;
            staffExecutor = new StaffExecutor(combatForecastUI, toastUI);
        }

        private void InitializeTargetingFlow()
        {
            if (targetingFlow != null) return;
            targetingFlow = new TargetingFlow(
                pathfindingService, mapRenderer,
                rangeHighlighter, combatForecastUI, this);
        }

        private void InitializeActionMenuController()
        {
            if (actionMenuController != null) return;
            actionMenuController = new ActionMenuController(
                actionMenuUI, inventoryMenuUI, confirmDialogUI,
                tradeUI, convoyUI, toastUI,
                targetingFlow, staffExecutor, this);
        }

        private void InitializeCantoFlow()
        {
            if (cantoFlow != null) return;
            cantoFlow = new CantoFlow(this, actionMenuController);
        }

        private void InitializeUnitSelectionFlow()
        {
            if (unitSelectionFlow != null) return;
            unitSelectionFlow = new UnitSelectionFlow(
                pathfindingService, unitMover,
                rangeHighlighter, pathArrowRenderer, combatForecastUI,
                this, cantoFlow, actionMenuController);
        }

        // Runtime-only (Start, not the test Initialize): lights up the unit under
        // the cursor — or the picked-up unit — with its selected animation.
        private void InitializeHoverSelection()
        {
            hoverSelectionDriver = new HoverSelectionDriver(
                () => currentMode,
                () => gridPosition,
                () => unitSelectionFlow.SelectedUnit,
                FindUnitForHover,
                UnitSelectionFlow.IsUnitSelectable);
            OnCursorMoved += RefreshHoverSelection;
        }

        // The registry is the fast path, but it is empty until TurnManager starts the battle —
        // and the cursor is already sitting on a unit before that happens. Falling through to a
        // scene scan on a miss is what keeps the opening hover from reading as an empty tile.
        private TestUnit FindUnitForHover(Vector2Int pos)
        {
            var registered = TurnManager.Instance != null
                ? TurnManager.Instance.UnitRegistry.GetUnitAt(pos)
                : null;

            return registered != null ? registered : FindUnitAt(pos);
        }

        private void RefreshHoverSelection(Vector2Int _) => hoverSelectionDriver?.Refresh();

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
            EventService.Instance.SubscribeGameStateChanged(OnGameStateChanged);
        }

        private void RemoveListenersFromGameStateEvents()
        {
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnGameStateChanged);
        }

        // --- State machine internals ---

        private CursorState ToState(CursorMode mode) => mode switch
        {
            CursorMode.Free => CursorState.Free,
            CursorMode.UnitSelected => CursorState.Selected,
            CursorMode.ActionMenu => CursorState.ActionMenu,
            CursorMode.Targeting => CursorState.Targeting,
            _ => moveInFlight ? CursorState.Moving : CursorState.Suspended,
        };

        private static CursorMode ToMode(CursorState state) => state switch
        {
            CursorState.Free => CursorMode.Free,
            CursorState.Selected => CursorMode.UnitSelected,
            CursorState.ActionMenu => CursorMode.ActionMenu,
            CursorState.Targeting => CursorMode.Targeting,
            _ => CursorMode.Locked,
        };

        private void RefreshHover()
        {
            if (stateMachine.CurrentState != CursorState.Free)
            {
                hoveredUnit = null;
                stateMachine.SetHover(CursorHover.Empty);
                return;
            }

            hoveredUnit = FindUnitForHover(gridPosition);
            stateMachine.SetHover(CursorHoverClassifier.Classify(FactionOf(hoveredUnit), CanUnitAct(hoveredUnit)));
        }

        // Re-raised on the event bus so audio and the visual variants don't have to know
        // the state machine exists.
        private void OnHoverChanged(CursorHover hover) =>
            EventService.Instance?.RaiseHoverChanged(hover, hoveredUnit);

        private static Faction? FactionOf(TestUnit unit) =>
            unit == null ? null : Registered(unit)?.GetFaction(unit) ?? unit.faction;

        // Both the registry's CanAct and its GetFaction report a unit that was never registered
        // exactly like one that has already acted, which would grey out every unit on the map
        // before the battle starts. Fall back to the unit's own flag until it is registered.
        private static bool CanUnitAct(TestUnit unit)
        {
            if (unit == null) return false;
            return Registered(unit) != null
                ? TurnManager.Instance.UnitRegistry.CanAct(unit)
                : !unit.hasActed;
        }

        private static UnitRegistry Registered(TestUnit unit)
        {
            var registry = TurnManager.Instance != null ? TurnManager.Instance.UnitRegistry : null;
            return registry != null && registry.GetFaction(unit) != null ? registry : null;
        }

        // --- Mode/position internals ---

        private void ToggleSpriteRendererBasedOnCursorMode()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.enabled = (currentMode != CursorMode.Locked);
        }

        // Hold SkipAnimation while confirming a target to flip the combat-anim
        // speed for that single combat. Persisted Normal/Fast → Skip; persisted
        // Skip → Normal. The dispatcher clears the override on combat complete.
        private static void ApplyPerCombatSpeedOverrideIfHeld()
        {
            var im = InputManager.Instance;
            var settings = CombatAnimationSettingsRef.Current;
            if (im == null || settings == null) return;
            if (!im.IsActionHeld(GameInputAction.SkipAnimation)) return;
            var current = settings.EffectiveSpeed;
            var flipped = current == CombatAnimationSpeed.Skip
                ? CombatAnimationSpeed.Normal
                : CombatAnimationSpeed.Skip;
            settings.SetOneShotOverride(flipped);
        }

        private Vector2Int ClampToMapBounds(Vector2Int pos)
        {
            MapData map = mapRenderer != null ? mapRenderer.CurrentMap : null;
            if (map == null) return pos;

            return new Vector2Int(
                Mathf.Clamp(pos.x, 0, map.Width - 1),
                Mathf.Clamp(pos.y, 0, map.Height - 1));
        }

        private void SnapToGridPosition()
        {
            transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0f);
        }

        private bool CanCursorMove()
        {
            if (currentMode == CursorMode.Locked) return false;
            if (currentMode == CursorMode.ActionMenu) return false;
            if (BattleMapUI.HasInputFocus) return false;
            if (SelectionMenuView.HasInputFocus) return false;
            if (InventoryMenuUI.HasInputFocus) return false;
            if (ConfirmDialogUI.HasInputFocus) return false;
            if (TradeScreenUI.HasInputFocus) return false;
            if (ConvoyUI.HasInputFocus) return false;
            if (!IsBattleMapState()) return false;
            if (unitMover != null && unitMover.IsMoving) return false;
            return true;
        }

        // The map cursor + unit-cycling only act while on the battle map; a dedicated
        // GameState (e.g. UnitInfoScreen) suppresses them without a per-screen focus flag.
        private static bool IsBattleMapState() =>
            GameStateManager.Instance == null || GameStateManager.Instance.CurrentState == GameState.BattleMap;

        // --- Selection-flow seams (forward to _unitSelectionFlow) ---

        internal void SetValidMoveTiles(HashSet<Vector2Int> tiles) =>
            unitSelectionFlow.SetValidMoveTiles(tiles);

        // Tutorial seam: restricts the selected unit's movement to the given tiles and
        // redraws the highlight to match, so the guidance is visible rather than implied.
        public void ConstrainMovementTo(HashSet<Vector2Int> tiles)
        {
            unitSelectionFlow.SetValidMoveTiles(tiles);
            rangeHighlighter?.ShowMovementRange(tiles, null);
        }

        internal void EnsureMoveTileAllowed(Vector2Int tile) =>
            unitSelectionFlow.EnsureMoveTileAllowed(tile);

        internal void EnterUnitSelectedMode() =>
            unitSelectionFlow.EnterUnitSelectedMode();

        // --- Cancel / cleanup ---

        private void CancelTargeting()
        {
            targetingFlow.Cancel();
            SetPosition(unitSelectionFlow.CommittedDestination);
            unitSelectionFlow.ShowActionMenu();
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

        // Where the cursor rests when the battle map first loads: the player's
        // commander (Lord) if one is marked, else the first player unit — the
        // Fire-Emblem "start on your lord" convention. Falls back to the origin.
        private static Vector2Int FindInitialCursorCell()
        {
            TestUnit lord = null, firstPlayer = null;
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (unit.faction != Faction.Player) continue;
                firstPlayer ??= unit;
                if (IsLord(unit)) { lord = unit; break; }
            }
            TestUnit start = lord != null ? lord : firstPlayer;
            return start != null ? start.gridPosition : Vector2Int.zero;
        }

        private static bool IsLord(TestUnit unit) =>
            (unit.UnitDefinition != null && unit.UnitDefinition.IsLord) || unit.isLord;

        // --- Game state events ---

        private void OnGameStateChanged(StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap)
                ResumeCursor();
            else
                SuspendCursor();
        }

        private void SuspendCursor()
        {
            moveInFlight = false;
            currentMode = CursorMode.Locked;
            stateMachine.TryTransition(CursorState.Suspended);
            stateMachine.SetHover(CursorHover.Empty);
            ToggleSpriteRendererBasedOnCursorMode();
        }

        // Coming back from combat, dialogue or the unit-info screen used to force Free,
        // which silently dropped a unit the player had already picked up. Only a live
        // selection is worth restoring — a menu or reticle that was up before the detour
        // is gone from the UI, so those resolve to Free.
        private void ResumeCursor()
        {
            stateMachine.RestoreFromSuspend();

            bool selectionSurvived = stateMachine.CurrentState == CursorState.Selected
                && unitSelectionFlow?.SelectedUnit != null;

            stateMachine.TryTransition(selectionSurvived ? CursorState.Selected : CursorState.Free);
            SetMode(ToMode(stateMachine.CurrentState));
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
            this.mapRenderer = mapRenderer;
            this.terrainStatTable = terrainStatTable;
            gridPosition = Vector2Int.zero;
            currentMode = CursorMode.Free;

            // Build the sub-controllers eagerly so tests can drive the cursor
            // without going through Start(). Idempotent guards inside the
            // initializers prevent double-construction when Start() later runs
            // in a non-test scenario.
            InitializePathFindingService();
            InitializeCombatExecutor();
            InitializeStaffExecutor();
            InitializeTargetingFlow();
            InitializeActionMenuController();
            InitializeCantoFlow();
            InitializeUnitSelectionFlow();
        }

        internal void SetSpriteRenderer(SpriteRenderer sr) => spriteRenderer = sr;
        internal void SetRangeHighlighter(RangeHighlighter rh) => rangeHighlighter = rh;
        internal void SetPathArrowRenderer(PathArrowRenderer par) => pathArrowRenderer = par;
        internal void SetActionMenuUI(SelectionMenuView ui) => actionMenuUI = ui;
        internal void SetUnitMover(UnitMover mover) => unitMover = mover;
    }
}
