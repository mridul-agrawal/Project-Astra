using System;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Walks one visit's objectives in order, and owns the only path by which progress is credited.
    public class ObjectiveSequenceRunner
    {
        private readonly HubObjectiveData[] objectives;
        private readonly HubRuntimeState state;

        // Fires with the objective that just became active, or null once the visit is finished.
        public event Action<HubObjectiveData> ObjectiveChanged;
        public event Action<HubObjectiveData> ObjectiveCompleted;
        public event Action ProgressChanged;

        // Raised instead of applied: firing an event needs the world, which this class can't see.
        public event Action<string> EventRequested;

        public ObjectiveSequenceRunner(HubObjectiveData[] objectives, HubRuntimeState state)
        {
            this.objectives = objectives ?? Array.Empty<HubObjectiveData>();
            this.state = state;
        }

        public HubObjectiveData ActiveObjective =>
            state.ObjectiveIndex >= 0 && state.ObjectiveIndex < objectives.Length
                ? objectives[state.ObjectiveIndex]
                : null;

        public bool IsVisitComplete => ActiveObjective == null;

        public int CurrentProgress => state.SatisfiedTargetCount;
        public int RequiredProgress => ActiveObjective?.Completion?.RequiredCount ?? 0;
        public bool ShowsCounter => ActiveObjective?.Completion?.showCounter ?? false;

        // Call once when the visit's world is ready. Announces the opening objective and clears any
        // leading Immediate stages, so control never returns to a stage that was already done.
        public void Begin()
        {
            SettleImmediateObjectives();
            ObjectiveChanged?.Invoke(ActiveObjective);
        }

        // The single entry point for progress. Returns true only when this report was new, valid,
        // and for the objective that is active right now.
        public bool Report(HubConditionKind kind, string targetId)
        {
            HubObjectiveData active = ActiveObjective;
            if (active == null) return false;
            if (!active.Completion.Accepts(kind, targetId)) return false;
            if (!state.Satisfy(targetId)) return false;

            ProgressChanged?.Invoke();
            if (CurrentProgress >= RequiredProgress) Complete(active);
            return true;
        }

        public bool IsMarkerTargetOutstanding(string targetId)
        {
            HubObjectiveData active = ActiveObjective;
            if (active == null || Array.IndexOf(active.MarkerTargetIds, targetId) < 0) return false;
            return !state.HasSatisfied(targetId);
        }

        private void Complete(HubObjectiveData objective)
        {
            state.CompleteObjective(objective.ObjectiveId);
            ApplyEffects(objective);
            ObjectiveCompleted?.Invoke(objective);

            SettleImmediateObjectives();
            ObjectiveChanged?.Invoke(ActiveObjective);
        }

        // An Immediate objective exists only to run its effects, so it finishes the instant it
        // activates. Looping covers a run of them back to back.
        private void SettleImmediateObjectives()
        {
            HubObjectiveData active = ActiveObjective;
            while (active != null && active.Completion.kind == HubConditionKind.Immediate)
            {
                state.CompleteObjective(active.ObjectiveId);
                ApplyEffects(active);
                ObjectiveCompleted?.Invoke(active);
                active = ActiveObjective;
            }
        }

        private void ApplyEffects(HubObjectiveData objective)
        {
            foreach (HubEffect effect in objective.OnComplete)
                Apply(effect);
        }

        private void Apply(HubEffect effect)
        {
            switch (effect.kind)
            {
                case HubEffectKind.SetGate:
                    state.SetGate(effect.targetId, effect.open);
                    break;
                case HubEffectKind.SetInteractableState:
                    state.SetInteractableState(effect.targetId, effect.state);
                    break;
                case HubEffectKind.RelocateCharacter:
                    state.Relocate(effect.targetId, effect.locationId, effect.position, effect.facing);
                    break;
                case HubEffectKind.SetCharacterConversation:
                    state.SetConversationOverride(effect.targetId, effect.valueId);
                    break;
                case HubEffectKind.FireEvent:
                    EventRequested?.Invoke(effect.valueId);
                    break;
            }
        }

        // Content check, run by the validation tooling: a stage nobody can finish would strand the
        // visit with no way forward and no error.
        public static bool IsAuthoredCorrectly(HubObjectiveData objective, out string problem)
        {
            problem = null;
            if (objective == null) { problem = "objective is missing"; return false; }
            if (string.IsNullOrEmpty(objective.ObjectiveId)) { problem = "empty objectiveId"; return false; }
            if (string.IsNullOrEmpty(objective.DisplayText)) { problem = "no player-facing text"; return false; }

            HubCondition completion = objective.Completion;
            if (completion == null) { problem = "no completion condition"; return false; }
            if (completion.kind != HubConditionKind.Immediate && completion.RequiredCount == 0)
            {
                problem = $"condition {completion.kind} has no targets, so it can never complete";
                return false;
            }
            return true;
        }
    }
}
