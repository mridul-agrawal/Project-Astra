using System.Collections.Generic;
using UnityEngine;
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

        // Seams for a running battle to tick an objective off or move its counter. Nothing calls
        // these yet - no system drives objective completion - but they are the only sanctioned way
        // to change one, because they touch the runtime copy and never the MapData asset.
        public void SetObjectiveComplete(int index, bool complete)
        {
            ObjectiveRowVM row = RowAt(index);
            if (row == null) return;

            row.Complete = complete;
            Render();
        }

        public void SetProgress(int index, int current)
        {
            ObjectiveRowVM row = RowAt(index);
            if (row == null) return;

            row.Current = Mathf.Clamp(current, 0, row.Max);
            row.Complete = row.Max > 0 && row.Current >= row.Max;
            Render();
        }

        private ObjectiveRowVM RowAt(int index)
        {
            var rows = objectiveModel.Objectives;
            return rows != null && index >= 0 && index < rows.Count ? rows[index] : null;
        }

        private void LoadConditions()
        {
            MapData map = MapService.Instance?.CurrentMap;
            if (map == null) return;
            objectiveModel.WinText = map.WinConditionText;
            objectiveModel.LoseText = map.LoseConditionText;
            objectiveModel.Objectives = CopyObjectives(map);
        }

        // The authored objectives are the map's starting state, so they are copied out. Mutating
        // the ScriptableObject at runtime would dirty the asset and persist across editor sessions.
        private static List<ObjectiveRowVM> CopyObjectives(MapData map)
        {
            var rows = new List<ObjectiveRowVM>();
            SecondaryObjective[] authored = map.SecondaryObjectives;
            if (authored == null) return rows;

            foreach (SecondaryObjective source in authored)
            {
                if (string.IsNullOrWhiteSpace(source.text)) continue;
                rows.Add(new ObjectiveRowVM
                {
                    Text = source.text,
                    Complete = source.complete,
                    Current = source.current,
                    Max = source.max,
                });
            }
            return rows;
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
