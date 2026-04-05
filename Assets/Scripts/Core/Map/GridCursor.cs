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
        [Header("Dependencies")]
        [SerializeField] private MapRenderer _mapRenderer;
        [SerializeField] private TerrainStatTable _terrainStatTable;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private RangeHighlighter _rangeHighlighter;
        [SerializeField] private PathArrowRenderer _pathArrowRenderer;
        [SerializeField] private UnitMover _unitMover;

        [Header("Rendering")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Animation")]
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _alphaMin = 0.4f;
        [SerializeField] private float _alphaMax = 1.0f;

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

        public Vector2Int GridPosition => _gridPosition;
        public CursorMode CurrentMode => _currentMode;

        /// <summary>Fired after the cursor moves to a new tile.</summary>
        public event Action<Vector2Int> OnCursorMoved;

        private void Awake()
        {
            if (_spriteRenderer != null)
                _animator = new CursorAnimator(_spriteRenderer, _pulseSpeed, _alphaMin, _alphaMax);
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove += HandleCursorMove;
                InputManager.Instance.OnConfirm += HandleConfirm;
                InputManager.Instance.OnCancel += HandleCancel;
            }

            if (_stateChangedChannel != null)
                _stateChangedChannel.Register(OnGameStateChanged);
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove -= HandleCursorMove;
                InputManager.Instance.OnConfirm -= HandleConfirm;
                InputManager.Instance.OnCancel -= HandleCancel;
            }

            if (_stateChangedChannel != null)
                _stateChangedChannel.Unregister(OnGameStateChanged);
        }

        private void Start()
        {
            if (_mapRenderer != null && _terrainStatTable != null)
                _pathfindingService = new PathfindingService(_mapRenderer, _terrainStatTable);

            SetPosition(Vector2Int.zero);
            UpdateModeFromGameState();
        }

        private void Update()
        {
            _animator?.UpdatePulse();
        }

        // --- Public API ---

        public void SetMode(CursorMode mode)
        {
            _currentMode = mode;

            if (_spriteRenderer != null)
                _spriteRenderer.enabled = (mode != CursorMode.Locked);
        }

        public void SetPosition(Vector2Int position)
        {
            Vector2Int newPos = ClampToMapBounds(position);
            bool changed = newPos != _gridPosition;
            _gridPosition = newPos;
            SnapToGridPosition();
            if (changed)
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

        // --- Input handlers ---

        internal void HandleCursorMove(Vector2Int direction)
        {
            if (_currentMode == CursorMode.Locked) return;
            if (BattleMapUI.HasInputFocus) return;
            if (_unitMover != null && _unitMover.IsMoving) return;

            Vector2Int newPos = ClampToMapBounds(_gridPosition + direction);

            // In constrained modes, only allow moves to valid tiles
            if (_validMoveTiles != null && !_validMoveTiles.Contains(newPos))
                return;

            if (newPos == _gridPosition) return;

            _gridPosition = newPos;
            SnapToGridPosition();
            OnCursorMoved?.Invoke(_gridPosition);

            // Update path arrow preview while in UnitSelected mode
            if (_currentMode == CursorMode.UnitSelected)
                UpdatePathArrow();
        }

        internal void HandleConfirm()
        {
            if (_currentMode == CursorMode.Locked) return;
            if (BattleMapUI.HasInputFocus) return;
            if (_unitMover != null && _unitMover.IsMoving) return;

            switch (_currentMode)
            {
                case CursorMode.Free:
                    TrySelectUnit();
                    break;
                case CursorMode.UnitSelected:
                    TryCommitMovement();
                    break;
                case CursorMode.Targeting:
                    TryCommitAttack();
                    break;
            }
        }

        internal void HandleCancel()
        {
            if (_currentMode == CursorMode.Locked) return;
            if (BattleMapUI.HasInputFocus) return;
            if (_unitMover != null && _unitMover.IsMoving) return;

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

        // --- FREE mode: unit selection ---

        private void TrySelectUnit()
        {
            TestUnit unit = FindUnitAt(_gridPosition);
            if (unit == null) return;
            if (unit.hasActed) return; // Already acted this phase

            _selectedUnit = unit;
            EnterUnitSelectedMode();
        }

        private void EnterUnitSelectedMode()
        {
            if (_pathfindingService == null || _selectedUnit == null) return;

            _currentReachability = _pathfindingService.ComputeReachability(
                _selectedUnit.gridPosition,
                _selectedUnit.movementPoints,
                _selectedUnit.movementType,
                pos => GetOccupantType(pos));

            // Constrain cursor to reachable + pass-through tiles
            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);

            _rangeHighlighter?.ShowMovementRange(
                _currentReachability.Destinations,
                _currentReachability.PassThrough);

            SetPositionWithMemory(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
        }

        private void DeselectUnit()
        {
            _selectedUnit = null;
            _validMoveTiles = null;
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            ReturnToMemorizedPosition();
            SetMode(CursorMode.Free);
        }

        // --- Path arrow preview ---

        private void UpdatePathArrow()
        {
            if (_pathArrowRenderer == null || _selectedUnit == null) return;

            // Only show path to valid destination tiles (not pass-through)
            if (!_currentReachability.Destinations.Contains(_gridPosition))
            {
                _pathArrowRenderer.Clear();
                return;
            }

            var path = Pathfinder.ReconstructPath(
                _selectedUnit.gridPosition, _gridPosition, _currentReachability);
            _pathArrowRenderer.ShowPath(path);
        }

        // --- UNIT_SELECTED mode: commit movement ---

        private void TryCommitMovement()
        {
            if (!_currentReachability.Destinations.Contains(_gridPosition))
                return; // Can't stop on pass-through tiles

            _committedDestination = _gridPosition;
            _selectedUnit.preMovementPosition = _selectedUnit.gridPosition;

            var path = Pathfinder.ReconstructPath(
                _selectedUnit.gridPosition, _committedDestination, _currentReachability);

            // Clear overlays and lock cursor during movement animation
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            SetMode(CursorMode.Locked);

            if (_unitMover != null && path != null && path.Count > 1)
                _unitMover.MoveAlongPath(_selectedUnit, path, OnMovementComplete);
            else
                OnMovementComplete();
        }

        private void OnMovementComplete()
        {
            EnterTargetingMode();
        }

        private void EnterTargetingMode()
        {
            var attackRange = _pathfindingService.ComputeAttackRange(
                new HashSet<Vector2Int> { _committedDestination },
                _selectedUnit.attackRangeMin,
                _selectedUnit.attackRangeMax);

            if (attackRange.Count == 0)
            {
                CompleteAction();
                return;
            }

            _validMoveTiles = attackRange;
            _rangeHighlighter?.ShowAttackRange(attackRange);

            SetPosition(_committedDestination);
            SetMode(CursorMode.Targeting);
        }

        private void CancelTargeting()
        {
            // Undo movement — snap unit back to pre-movement position
            if (_unitMover != null)
                _unitMover.UndoMove(_selectedUnit, _selectedUnit.preMovementPosition);

            // Return to UNIT_SELECTED — restore movement highlights
            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);

            _rangeHighlighter?.ShowMovementRange(
                _currentReachability.Destinations,
                _currentReachability.PassThrough);

            SetPosition(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
        }

        // --- TARGETING mode: commit attack ---

        private void TryCommitAttack()
        {
            Debug.Log($"GridCursor: Attack committed at ({_gridPosition.x}, {_gridPosition.y})");
            CompleteAction();
        }

        private void CompleteAction()
        {
            if (_selectedUnit != null)
                _selectedUnit.MarkActed();

            _selectedUnit = null;
            _validMoveTiles = null;
            _memorizedPosition = null;
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            SetMode(CursorMode.Free);
        }

        // --- Game state integration ---

        private void OnGameStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap)
                SetMode(CursorMode.Free);
            else
                SetMode(CursorMode.Locked);
        }

        private void UpdateModeFromGameState()
        {
            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState == GameState.BattleMap)
                SetMode(CursorMode.Free);
            else
                SetMode(CursorMode.Free); // Default to Free when loaded directly
        }

        // --- Helpers ---

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

        private TestUnit FindUnitAt(Vector2Int pos)
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (unit.gridPosition == pos)
                    return unit;
            }
            return null;
        }

        private Pathfinder.OccupantType GetOccupantType(Vector2Int pos)
        {
            return Pathfinder.OccupantType.None;
        }

        // --- Test initialization ---

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

        internal void SetUnitMover(UnitMover mover)
        {
            _unitMover = mover;
        }
    }
}
