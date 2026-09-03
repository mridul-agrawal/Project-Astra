using System.Collections.Generic;
using System.Linq;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // What one visit does differently from the one before it, in the words a designer would use.
    //
    // Flipping between two visits shows the difference; this says it, so a character who quietly
    // stayed put is not mistaken for one who was moved on purpose.
    public static class HubVisitDiff
    {
        public static IReadOnlyList<string> Describe(HubVisitData before, HubVisitData after)
        {
            if (before == null || after == null) return System.Array.Empty<string>();

            var lines = new List<string>();
            lines.AddRange(WhoChanged(before, after));
            lines.AddRange(WhatOpened(before, after));
            lines.AddRange(WhatChangedState(before, after));
            AddIfDifferent(lines, "opens in", before.StartLocationId, after.StartLocationId);
            AddIfDifferent(lines, "runs the quest", before.QuestId, after.QuestId);

            return lines.Count > 0 ? lines : new List<string> { "Nothing differs from the visit before." };
        }

        private static IEnumerable<string> WhoChanged(HubVisitData before, HubVisitData after)
        {
            Dictionary<string, HubCharacterPlacement> was = ByCharacter(before);
            Dictionary<string, HubCharacterPlacement> now = ByCharacter(after);

            foreach (string who in now.Keys.Where(who => !was.ContainsKey(who)))
                yield return $"{who} is here now";

            foreach (string who in was.Keys.Where(who => !now.ContainsKey(who)))
                yield return $"{who} has gone";

            foreach (string who in now.Keys.Where(was.ContainsKey))
                foreach (string change in Moved(who, was[who], now[who]))
                    yield return change;
        }

        private static IEnumerable<string> Moved(string who, HubCharacterPlacement was, HubCharacterPlacement now)
        {
            if (was.locationId != now.locationId) yield return $"{who} has moved to {now.locationId}";
            else if (was.position != now.position) yield return $"{who} stands somewhere else";

            if (was.conversationId != now.conversationId) yield return $"{who} says something else";
        }

        private static IEnumerable<string> WhatOpened(HubVisitData before, HubVisitData after)
        {
            foreach (string gate in after.OpenGates.Except(before.OpenGates))
                yield return $"'{gate}' starts open";

            foreach (string gate in before.OpenGates.Except(after.OpenGates))
                yield return $"'{gate}' starts shut again";
        }

        private static IEnumerable<string> WhatChangedState(HubVisitData before, HubVisitData after)
        {
            Dictionary<string, HubInteractableState> was = ByInteractable(before);

            foreach (HubInteractableOverride change in after.InteractableOverrides)
            {
                bool knew = was.TryGetValue(change.interactableId, out HubInteractableState previous);
                if (knew && previous == change.state) continue;

                yield return $"{change.interactableId} is {change.state}";
            }
        }

        private static void AddIfDifferent(List<string> lines, string what, string before, string after)
        {
            if (before != after) lines.Add($"{what} {after}");
        }

        // The last placement wins, which is what the loader does with a character listed twice.
        private static Dictionary<string, HubCharacterPlacement> ByCharacter(HubVisitData visit)
        {
            var byId = new Dictionary<string, HubCharacterPlacement>();
            foreach (HubCharacterPlacement placement in visit.CharacterPlacements)
                if (!string.IsNullOrEmpty(placement.characterId)) byId[placement.characterId] = placement;
            return byId;
        }

        private static Dictionary<string, HubInteractableState> ByInteractable(HubVisitData visit)
        {
            var byId = new Dictionary<string, HubInteractableState>();
            foreach (HubInteractableOverride change in visit.InteractableOverrides)
                if (!string.IsNullOrEmpty(change.interactableId)) byId[change.interactableId] = change.state;
            return byId;
        }
    }
}
