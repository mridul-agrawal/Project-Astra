using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Turn;
using System;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD

{
    public class BattleHUDUIController : MonoBehaviour
    {
        // UI Views:
        [SerializeField] private UnitCardView unitCardView;
        [SerializeField] private TileInfoView tileInfoView;
        // [SerializeField] private ObjectiveView objectiveView;

        // UI COntrollers:
        private UnitCardController unitCardController;
        private TileInfoController tileInfoController;
        // private ObjectiveController objectiveController;

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
            // objectiveController = new ObjectiveController(objectiveView);
        }

        private void SetCursor() => cursor = FindAnyObjectByType<GridCursor>();

        private void SubscribeToEvents()
        {
            if (cursor != null) 
                cursor.OnCursorMoved += HandleCursorMoved;
            EventService.Instance.SubscribePhaseStarted(HandlePhaseStarted);
        }


        private void HandleCursorMoved(Vector2Int pos)
        {
            unitCardController.HandleCursorMoved(pos);
            tileInfoController.HandleCursorMoved(pos);
        }

        private void HandlePhaseStarted(BattlePhase phase, int arg2)
        {
            unitCardController.HandlePhaseStarted(phase, cursor.GridPosition);
            tileInfoController.HandlePhaseStarted(phase, cursor.GridPosition);
            // objectiveController.HandlePhaseStarted(phase, arg2);
        }

        private void OnDestroy()
        {
            if (cursor != null) 
                cursor.OnCursorMoved -= HandleCursorMoved;
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribePhaseStarted(HandlePhaseStarted);
        }

    }
}
