using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Quests
{
    // Finished by talking to people. Several targets make the spec's 0/5 counter.
    [Serializable]
    public sealed class TalkCondition : ObjectiveCondition
    {
        // A conversation id credits the target; the character id is only so a marker has someone to
        // stand over. A dialogue script does not name its own cast, which is why both are here.
        [Serializable]
        public struct Target
        {
            [Tooltip("The conversation that must reach its end for this target to count.")]
            [HubPick(HubIdKind.Conversation)] public string conversationId;

            [Tooltip("Whose conversation it is. Leave empty for one nobody needs pointing at.")]
            [HubPick(HubIdKind.Character)] public string characterId;
        }

        [SerializeField] private Target[] targets = Array.Empty<Target>();

        private List<string> conversationIds;

        public override IReadOnlyList<string> Targets => conversationIds ??= BuildIds();
        public override int RequiredCount => targets.Length;

        public override bool Matches(GameplaySignal signal, out string targetId)
        {
            targetId = null;
            if (signal.Kind != GameplaySignalKind.ConversationFinished) return false;

            foreach (Target target in targets)
            {
                if (target.conversationId != signal.Id) continue;
                targetId = target.conversationId;
                return true;
            }
            return false;
        }

        public override string MarkerFor(string targetId)
        {
            foreach (Target target in targets)
                if (target.conversationId == targetId) return target.characterId;
            return null;
        }

        private List<string> BuildIds()
        {
            var ids = new List<string>(targets.Length);
            foreach (Target target in targets) ids.Add(target.conversationId);
            return ids;
        }

        // Authoring helper for the content builders and the tests.
        public void Configure(params Target[] authored)
        {
            targets = authored ?? Array.Empty<Target>();
            conversationIds = null;
        }

        public static Target With(string conversationId, string characterId = null) =>
            new() { conversationId = conversationId, characterId = characterId };
    }
}
