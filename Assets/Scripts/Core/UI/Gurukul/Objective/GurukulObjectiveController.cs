using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.Core.UI.Gurukul.Objective
{
    // Keeps the objective line and its counter in step with the visit's progression.
    //
    // Hidden while a conversation or an event is running and restored with current text when the
    // player gets control back, which is what the spec asks for — the HUD should never talk over a
    // scene.
    public sealed class GurukulObjectiveController
    {
        public GurukulObjectiveView objectiveView;
        public GurukulObjectiveModel objectiveModel;

        private ObjectiveSequenceRunner objectives;
        private bool hasObjective;
        private bool suppressed;

        public GurukulObjectiveController(GurukulObjectiveView view)
        {
            objectiveView = view;
            objectiveModel = new GurukulObjectiveModel();
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

        // Free exploration is the only state the objective belongs on screen in.
        public void HandleSubStateChanged(GurukulSubState previous, GurukulSubState next)
        {
            suppressed = next != GurukulSubState.FreeExploration;
            Render();
        }

        private void HandleObjectiveChanged(GurukulObjective objective) => Refresh();
        private void HandleProgressChanged() => Refresh();

        // Skipped while a conversation or event owns the screen — an objective that completes
        // mid-scene has its cue deferred until control comes back, which is what the spec asks for.
        private void HandleObjectiveCompleted(GurukulObjective objective)
        {
            if (suppressed) return;
            objectiveView.ShowCompletedCue(objective.DisplayText);
        }

        private void Refresh()
        {
            GurukulObjective active = objectives?.ActiveObjective;
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
