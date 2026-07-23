using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Controller for the Objective panel. Win/lose lines come from the map data; the
    // turn number and enemy count track the battle via events fanned in by
    // BattleHUDUIController. Placement (which top corner) also arrives from there.
    public sealed class ObjectiveController
    {
        public ObjectiveView objectiveView;
        public ObjectiveModel objectiveModel;

        public ObjectiveController(ObjectiveView objectiveView)
        {
            this.objectiveView = objectiveView;
            this.objectiveModel = new ObjectiveModel();
            LoadConditions();
        }

        public void HandleCursorMoved(HudCorner corner)
        {
            objectiveModel.Corner = corner;
            Render();
        }

        public void HandlePhaseStarted(BattlePhase phase, int turnNumber, HudCorner corner)
        {
            if (string.IsNullOrEmpty(objectiveModel.WinText)) LoadConditions();
            objectiveModel.Corner = corner;
            objectiveModel.Turn = turnNumber;
            objectiveModel.EnemiesRemaining = CountEnemies();
            Render();
        }

        public void HandleUnitDeath()
        {
            objectiveModel.EnemiesRemaining = CountEnemies();
            Render();
        }

        public void HandlePeek(bool peeking)
        {
            if (objectiveView != null) objectiveView.SetPeek(peeking);
        }

        // Hook for a running map to rewrite the win line when the beat flips.
        public void SetWinText(string winText)
        {
            objectiveModel.WinText = winText;
            Render();
        }

        private void LoadConditions()
        {
            MapData map = MapService.Instance?.CurrentMap;
            if (map == null) return;
            objectiveModel.WinText = map.WinConditionText;
            objectiveModel.LoseText = map.LoseConditionText;
        }

        private int CountEnemies()
        {
            TurnManager tm = TurnManager.Instance;
            return tm != null ? tm.UnitRegistry.GetUnitsForFaction(Faction.Enemy).Count : 0;
        }

        private void Render()
        {
            if (objectiveView != null) objectiveView.Render(objectiveModel);
        }
    }
}
