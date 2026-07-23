using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Turn;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public class BattleHUDUIController : MonoBehaviour
    {
        // UI Views:
        [SerializeField] private UnitCardView unitCardView;
        [SerializeField] private TileInfoView tileInfoView;
        [SerializeField] private ObjectiveView objectiveView;

        // UI Controllers:
        private UnitCardController unitCardController;
        private TileInfoController tileInfoController;
        private ObjectiveController objectiveController;

        private GridCursor cursor;

        private void Awake()
        {
            CreateUIControllers();
            SetCursor();
            SubscribeToEvents();
        }

        private void CreateUIControllers()
        {
            unitCardController = new UnitCardController(unitCardView);
            tileInfoController = new TileInfoController(tileInfoView);
            objectiveController = new ObjectiveController(objectiveView);
        }

        private void SetCursor() => cursor = FindAnyObjectByType<GridCursor>();

        private void SubscribeToEvents()
        {
            if (cursor != null)
                cursor.OnCursorMoved += HandleCursorMoved;
            EventService.Instance.SubscribePhaseStarted(HandlePhaseStarted);
            EventService.Instance.SubscribeUnitDeath(HandleUnitDeath);
            if (InputManager.Instance != null)
                InputManager.Instance.OnPeekObjective += HandlePeek;
        }

        private void HandleCursorMoved(Vector2Int pos)
        {
            HudPlacement place = ResolvePlacement(pos);
            unitCardController.HandleCursorMoved(pos, place.Unit);
            tileInfoController.HandleCursorMoved(pos, place.Tile);
            objectiveController.HandleCursorMoved(place.Objective);
        }

        private void HandlePhaseStarted(BattlePhase phase, int turnNumber)
        {
            Vector2Int pos = cursor.GridPosition;
            HudPlacement place = ResolvePlacement(pos);
            unitCardController.HandlePhaseStarted(phase, pos, place.Unit);
            tileInfoController.HandlePhaseStarted(phase, pos, place.Tile);
            objectiveController.HandlePhaseStarted(phase, turnNumber, place.Objective);
        }

        private void HandleUnitDeath(UnitDeathEventArgs args) => objectiveController.HandleUnitDeath();

        private void HandlePeek(bool peeking) => objectiveController.HandlePeek(peeking);

        // FE GBA musical chairs: the cursor owns one quadrant; the three panels fill the
        // rest. Unit card sits diagonally opposite the cursor, the objective takes the
        // free top corner, tile info the free bottom corner. None ever cover the cursor.
        private static HudPlacement ResolvePlacement(Vector2Int cursorPos)
        {
            switch (QuadrantOf(cursorPos))
            {
                case HudCorner.TopLeft:    return new HudPlacement(HudCorner.TopRight, HudCorner.BottomRight, HudCorner.BottomLeft);
                case HudCorner.TopRight:   return new HudPlacement(HudCorner.TopLeft,  HudCorner.BottomLeft,  HudCorner.BottomRight);
                case HudCorner.BottomLeft: return new HudPlacement(HudCorner.TopLeft,  HudCorner.TopRight,    HudCorner.BottomRight);
                default:                   return new HudPlacement(HudCorner.TopRight, HudCorner.TopLeft,     HudCorner.BottomLeft); // BottomRight
            }
        }

        // Which quadrant the cursor sits in, by grid halves (y increases upward = top).
        private static HudCorner QuadrantOf(Vector2Int pos)
        {
            MapData map = MapService.Instance.CurrentMap;
            bool left = pos.x < map.Width / 2;
            bool top = pos.y >= map.Height / 2;
            if (top) return left ? HudCorner.TopLeft : HudCorner.TopRight;
            return left ? HudCorner.BottomLeft : HudCorner.BottomRight;
        }

        private void OnDestroy()
        {
            if (cursor != null)
                cursor.OnCursorMoved -= HandleCursorMoved;
            if (EventService.Instance != null)
            {
                EventService.Instance.UnsubscribePhaseStarted(HandlePhaseStarted);
                EventService.Instance.UnsubscribeUnitDeath(HandleUnitDeath);
            }
            if (InputManager.Instance != null)
                InputManager.Instance.OnPeekObjective -= HandlePeek;
        }

        // Which corner each panel docks to for the current cursor quadrant.
        private readonly struct HudPlacement
        {
            public readonly HudCorner Objective;
            public readonly HudCorner Unit;
            public readonly HudCorner Tile;

            public HudPlacement(HudCorner objective, HudCorner unit, HudCorner tile)
            {
                Objective = objective;
                Unit = unit;
                Tile = tile;
            }
        }
    }
}
