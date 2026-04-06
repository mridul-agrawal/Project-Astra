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
            _animator?.UpdatePulse();
        }
        #endregion

        #region Helpers for initialization and event management
        private void InitializeCursorAnimator()
        {
            if (_spriteRenderer != null)
                _animator = new CursorAnimator(_spriteRenderer, _pulseSpeed, _alphaMin, _alphaMax);
        }

        private void AddListenersToInputEvents()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove += HandleCursorMove;
                InputManager.Instance.OnConfirm += HandleConfirm;
                InputManager.Instance.OnCancel += HandleCancel;
            }
        }

        private void RemoveListenersFromInputEvents()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCursorMove -= HandleCursorMove;
                InputManager.Instance.OnConfirm -= HandleConfirm;
                InputManager.Instance.OnCancel -= HandleCancel;
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

            Vector2Int targetGridPosition = ClampToMapBounds(_gridPosition + direction);

            if (ValidMoveTilesContains(targetGridPosition))
                return;

            if (targetGridPosition == _gridPosition) 
                return;

            _gridPosition = targetGridPosition;
            SnapToGridPosition();
            OnCursorMoved?.Invoke(_gridPosition);

            if (_currentMode == CursorMode.UnitSelected)
                UpdatePathArrow();
        }

        private bool CanCursorMove()
        {
            if (_currentMode == CursorMode.Locked) return false;
            if (BattleMapUI.HasInputFocus) return false;
            if (_unitMover != null && _unitMover.IsMoving) return false;
            return true;
        }

        private bool ValidMoveTilesContains(Vector2Int pos)
        {
            return _validMoveTiles != null && _validMoveTiles.Contains(pos);
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
            return unit != null && !unit.hasActed;
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
            EnterTargetingMode();
        }

        private void EnterTargetingMode()
        {
            var attackRange = _pathfindingService.ComputeAttackRange(new HashSet<Vector2Int> { _committedDestination }, _selectedUnit.attackRangeMin, _selectedUnit.attackRangeMax);

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

        private void CompleteAction()
        {
            if (_selectedUnit != null)
                _selectedUnit.MarkActed();

            _memorizedPosition = null;
            ResetUnitTilesMode();
        }

        private void TryCommitAttack()
        {
            Debug.Log($"GridCursor: Attack committed at ({_gridPosition.x}, {_gridPosition.y})");
            CompleteAction();
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
            _rangeHighlighter?.ClearAll();
            _pathArrowRenderer?.Clear();
            SetMode(CursorMode.Free);
        }

        private void CancelTargeting()
        {
            // Undo movement — snap unit back to pre-movement position
            if (_unitMover != null)
                _unitMover.UndoMove(_selectedUnit, _selectedUnit.preMovementPosition);

            // Return to UNIT_SELECTED — restore movement highlights
            _validMoveTiles = new HashSet<Vector2Int>(_currentReachability.Destinations);
            _validMoveTiles.UnionWith(_currentReachability.PassThrough);

            _rangeHighlighter?.ShowMovementRange(_currentReachability.Destinations, _currentReachability.PassThrough);

            SetPosition(_selectedUnit.gridPosition);
            SetMode(CursorMode.UnitSelected);
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

        internal void SetUnitMover(UnitMover mover)
        {
            _unitMover = mover;
        }
        #endregion

    }
}
