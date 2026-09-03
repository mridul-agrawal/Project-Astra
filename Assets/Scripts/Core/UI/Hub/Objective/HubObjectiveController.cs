using ProjectAstra.Core.Quests;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Hub.Objective
{
    // Keeps the objective line and its counter in step with the quest.
    public sealed class HubObjectiveController
    {
        public HubObjectiveView objectiveView;
        public HubObjectiveModel objectiveModel;

        private ObjectiveStatus status = ObjectiveStatus.None;
        private bool suppressed;

        public HubObjectiveController(HubObjectiveView view)
        {
            objectiveView = view;
            objectiveModel = new HubObjectiveModel();
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

        // The cue for the stage that just ended, not the one starting. Skipped while a conversation
        // or an event owns the screen, which is the spec's deferred update.
        public void HandleObjectiveCompleted(QuestObjective completed)
        {
            if (suppressed || completed == null) return;
            objectiveView.ShowCompletedCue(completed.DisplayText);
        }

        public void HandleQuestCompleted(QuestData quest)
        {
            status = ObjectiveStatus.None;
            Render();
        }

        // Free roaming is the only state the objective belongs on screen in.
        public void HandleGameStateChanged(StateChangeArgs args)
        {
            suppressed = args.NewState != GameState.HubExploration;
            Render();
        }

        private void Render()
        {
            objectiveModel.Visible = status.HasObjective && !suppressed;
            if (status.HasObjective)
            {
                objectiveModel.Row.Text = status.DisplayText;
                objectiveModel.Row.Current = status.Current;
                objectiveModel.Row.Max = status.ShowsCounter ? status.Required : 0;
            }
            objectiveView.Render(objectiveModel);
        }
    }
}
