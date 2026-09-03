namespace ProjectAstra.Core.Quests
{
    // Everything a listener needs to draw the current objective, so none of them has to hold a
    // reference back to the runner.
    public readonly struct ObjectiveStatus
    {
        public readonly QuestObjective Objective;
        public readonly int Current;
        public readonly int Required;

        public ObjectiveStatus(QuestObjective objective, int current, int required)
        {
            Objective = objective;
            Current = current;
            Required = required;
        }

        public bool HasObjective => Objective != null;
        public string DisplayText => Objective != null ? Objective.DisplayText : null;
        public bool ShowsCounter => Objective != null && Objective.ShowCounter;

        public static readonly ObjectiveStatus None = new(null, 0, 0);
    }
}
