using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Flow;

namespace ProjectAstra.Core.UI.Cutscene
{
    // Presents whatever cutscene the campaign is on, then reports back so the flow advances.
    // The script is chosen by GameFlow — this controller is deliberately "dumb": it doesn't
    // know which cutscene this is or what comes after. _directPlayFallbackScript only matters
    // when pressing Play on this scene in the editor without the boot flow running.
    public class CutsceneUI : MonoBehaviour
    {
        [Tooltip("Editor direct-play only — used when GameFlow isn't running (no boot scene).")]
        [SerializeField] private DialogueScript _directPlayFallbackScript;

        private void OnEnable()
        {
            DialogueScript script = ResolveScript();
            if (DialogueService.Instance != null && script != null)
                DialogueService.Instance.Play(script, DialogueTriggeringContext.Cutscene, OnCutsceneComplete);
            else
                OnCutsceneComplete();
        }

        private DialogueScript ResolveScript()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow != null && flow.CurrentCutsceneScript != null)
                return flow.CurrentCutsceneScript;
            return _directPlayFallbackScript;
        }

        private void OnCutsceneComplete()
        {
            if (GameFlow.Instance != null)
                GameFlow.Instance.NotifyCutsceneFinished();
            else
                Debug.LogWarning("[CutsceneUI] No GameFlow running (editor direct-play?) — campaign can't advance.");
        }
    }
}
