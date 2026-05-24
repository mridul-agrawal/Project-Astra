using System;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Broadcasts battle-map moments (unit selected, move confirmed, pre-combat) so the
    // tutorial trigger driver can react without the cursor ever knowing about dialogue.
    // PlayerPhaseStarted is NOT raised here — it already lives on TurnEventChannel.
    [CreateAssetMenu(fileName = "BattleDialogueEventChannel", menuName = "Project Astra/Dialogue/Battle Dialogue Event Channel")]
    public class BattleDialogueEventChannel : ScriptableObject
    {
        private Action<BattleDialogueEventType> _onEvent;

        public void Register(Action<BattleDialogueEventType> listener) => _onEvent += listener;
        public void Unregister(Action<BattleDialogueEventType> listener) => _onEvent -= listener;

        public void Raise(BattleDialogueEventType eventType) => _onEvent?.Invoke(eventType);
    }
}
