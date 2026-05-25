using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Cutscene
{
    // Plays the opening narrative when the Cutscene scene loads, then hands off to
    // the first battle map. The script itself is authored data (DialogueScript);
    // this controller just kicks it off and listens for completion.
    public class CutsceneUI : MonoBehaviour
    {
        [SerializeField] private DialogueScript _openingScript;

        private void OnEnable()
        {
            if (DialogueService.Instance != null && _openingScript != null)
                DialogueService.Instance.Play(_openingScript, DialogueTriggeringContext.Cutscene, GoToBattleMap);
            else
                FallBackToBattleMap();
        }

        private void GoToBattleMap()
            => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(CutsceneUI));

        private void FallBackToBattleMap()
        {
            Debug.LogWarning("[CutsceneUI] No DialogueService or opening script wired; skipping straight to BattleMap.");
            GoToBattleMap();
        }
    }
}
