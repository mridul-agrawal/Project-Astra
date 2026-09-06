using System.Collections.Generic;
using ProjectAstra.Core.Quests;
using ProjectAstra.Core.State;
using ProjectAstra.Core.UI.BattleMap.HUD;

namespace ProjectAstra.Core.UI.Hub.Objective
{
    // Feeds the battle map's objective panel from a hub visit's quest, so both places are the same
    // interface with different content behind it.
    //
    // Every stage of the quest is a row, not just the one she is on: a checklist is what the panel
    // draws, and a visit's stages are already an ordered list of them.
    public sealed class HubObjectiveController
    {
        public ObjectiveView objectiveView;
        public ObjectiveModel objectiveModel;

        private ObjectiveStatus status = ObjectiveStatus.None;
        private bool suppressed;

        public HubObjectiveController(ObjectiveView view)
        {
            objectiveView = view;
            objectiveModel = new ObjectiveModel
            {
                // A visit has no win or lose, and the hub never switches sides.
                ShowConditions = false,
                Corner = HudCorner.TopRight
            };
            Render();
        }

        public void HandleObjectiveActivated(ObjectiveStatus activated)
        {
            status = activated;
            Render();
        }

        public void HandleObjectiveProgressed(ObjectiveStatus progressed)
        {
            status = progressed;
            Render();
        }

        public void HandleObjectiveCompleted(QuestObjective completed) => Render();

        public void HandleQuestCompleted(QuestData quest)
        {
            status = ObjectiveStatus.None;
            Render();
        }

        // Free roaming is the only state the panel belongs on screen in.
        public void HandleGameStateChanged(StateChangeArgs args)
        {
            suppressed = args.NewState != GameState.HubExploration;
            Render();
        }

        // Held, exactly as on the battle map. A suppressed panel refuses, so a key still down when a
        // conversation opens cannot slide it out over the dialogue.
        public void HandlePeek(bool peeking)
        {
            if (objectiveView == null) return;
            objectiveView.SetPeek(peeking && !suppressed);
        }

        private void Render()
        {
            if (objectiveView == null) return;

            objectiveModel.Objectives = Rows();
            objectiveView.SetVisible(!suppressed && objectiveModel.HasObjectives);
            objectiveView.Render(objectiveModel);
        }

        // The whole quest, with the stage she is on carrying the counter. Stages after it read as
        // outstanding, which is what a checklist is for.
        private List<ObjectiveRowVM> Rows()
        {
            var rows = new List<ObjectiveRowVM>();

            QuestManager quests = QuestManager.Instance;
            QuestData quest = quests?.Runner?.Quest;
            if (quest == null) return rows;

            foreach (QuestObjective stage in quest.Objectives)
            {
                if (stage == null) continue;
                rows.Add(RowFor(stage, quests.Progress));
            }
            return rows;
        }

        private ObjectiveRowVM RowFor(QuestObjective stage, QuestProgress progress)
        {
            bool done = progress != null && progress.IsObjectiveCompleted(stage.ObjectiveId);
            bool current = !done && status.HasObjective && status.Objective == stage;

            return new ObjectiveRowVM
            {
                Text = stage.DisplayText,
                Complete = done,
                Current = current ? status.Current : 0,
                Max = current && status.ShowsCounter ? status.Required : 0
            };
        }
    }
}
