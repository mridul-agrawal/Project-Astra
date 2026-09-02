using TMPro;
using UnityEngine;
using ProjectAstra.Core.UI.BattleMap.HUD;

namespace ProjectAstra.Core.UI.Hub.Objective
{
    // What the hub currently wants her to do, plus a count when the objective has several targets.
    //
    // Reuses ObjectiveRowVM, which already carries exactly this shape, but not the battle panel's
    // controller or GameObject — that one is welded to MapService, TurnManager and literal WIN/LOSE
    // headers, and is authored inline in BattleMap.unity rather than as a prefab.
    public sealed class HubObjectiveModel
    {
        public bool Visible;
        public ObjectiveRowVM Row = new();
    }

    public sealed class HubObjectiveView : MonoBehaviour
    {
        // Long enough to read, short enough not to hold up whatever comes next — the spec asks for a
        // cue that is clear but doesn't halt play.
        private const float CueSeconds = 1.4f;

        public GameObject content;
        public TextMeshProUGUI objectiveLabel;
        public TextMeshProUGUI counterLabel;
        public GameObject cueContent;
        public TextMeshProUGUI cueLabel;

        private Coroutine runningCue;

        private void Awake() => HideCue();

        public void Render(HubObjectiveModel model)
        {
            SetVisible(model.Visible);
            if (!model.Visible) return;

            if (objectiveLabel != null) objectiveLabel.text = model.Row.Text;
            if (counterLabel == null) return;

            counterLabel.gameObject.SetActive(model.Row.HasCounter);
            if (model.Row.HasCounter) counterLabel.text = model.Row.CounterText;
        }

        // Shown alongside the next objective appearing, rather than blocking it.
        public void ShowCompletedCue(string objectiveText)
        {
            if (cueContent == null || !isActiveAndEnabled) return;

            if (cueLabel != null) cueLabel.text = $"{objectiveText} — done";
            cueContent.SetActive(true);

            if (runningCue != null) StopCoroutine(runningCue);
            runningCue = StartCoroutine(HideCueAfterAMoment());
        }

        private System.Collections.IEnumerator HideCueAfterAMoment()
        {
            yield return new WaitForSeconds(CueSeconds);
            HideCue();
            runningCue = null;
        }

        private void HideCue()
        {
            if (cueContent != null) cueContent.SetActive(false);
        }

        // Toggles the contents rather than this GameObject, so the cue's coroutine survives the
        // panel being hidden during a conversation.
        public void SetVisible(bool visible)
        {
            if (content != null) content.SetActive(visible);
        }
    }
}
