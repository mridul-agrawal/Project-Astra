using ProjectAstra.Core.Events;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Hub.Objective
{
    // Keeps the objective line and its counter in step with the visit's progression.
    public sealed class HubObjectiveController
    {
        public HubObjectiveView objectiveView;
        public HubObjectiveModel objectiveModel;

        private ObjectiveSequenceRunner objectives;
        private bool hasObjective;
        private bool suppressed;

        public HubObjectiveController(HubObjectiveView view)
        {
            objectiveView = view;
            objectiveModel = new HubObjectiveModel();
            Render();
        }

        public void Bind(ObjectiveSequenceRunner runner)
        {
            Unbind();
            objectives = runner;
            if (objectives == null) return;

            objectives.ObjectiveChanged += HandleObjectiveChanged;
            objectives.ProgressChanged += HandleProgressChanged;
            objectives.ObjectiveCompleted += HandleObjectiveCompleted;
            Refresh();
        }

        public void Unbind()
        {
            if (objectives == null) return;
            objectives.ObjectiveChanged -= HandleObjectiveChanged;
            objectives.ProgressChanged -= HandleProgressChanged;
            objectives.ObjectiveCompleted -= HandleObjectiveCompleted;
            objectives = null;
        }

        // Free roaming is the only state the objective belongs on screen in — a conversation or a
        // scripted sequence owns the screen while it runs.
        public void HandleGameStateChanged(StateChangeArgs args)
        {
            suppressed = args.NewState != GameState.HubExploration;
            Render();
        }

        private void HandleObjectiveChanged(HubObjectiveData objective) => Refresh();
        private void HandleProgressChanged() => Refresh();

        // Skipped while a conversation or event owns the screen — an objective that completes
        // mid-scene has its cue deferred until control comes back, which is what the spec asks for.
        private void HandleObjectiveCompleted(HubObjectiveData objective)
        {
            if (suppressed) return;
            objectiveView.ShowCompletedCue(objective.DisplayText);
        }

        private void Refresh()
        {
            HubObjectiveData active = objectives?.ActiveObjective;
            hasObjective = active != null;

            if (hasObjective)
            {
                objectiveModel.Row.Text = active.DisplayText;
                objectiveModel.Row.Current = objectives.CurrentProgress;
                objectiveModel.Row.Max = objectives.ShowsCounter ? objectives.RequiredProgress : 0;
            }
            Render();
        }

        private void Render()
        {
            objectiveModel.Visible = hasObjective && !suppressed;
            objectiveView.Render(objectiveModel);
        }
    }
}
