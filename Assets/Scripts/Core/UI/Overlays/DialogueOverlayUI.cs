using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Overlays
{
    // Dialogue overlay — confirm ends dialogue and returns to battle map.
    public class DialogueOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button endDialogueButton;

        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            endDialogueButton.image.color = Selected;
        }

        private void AddListenersToMouseClicks()
        {
            endDialogueButton.onClick.AddListener(EndDialogue);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm += EndDialogue;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
        }

        private void RemoveListenersToMouseClicks()
        {
            endDialogueButton.onClick.RemoveListener(EndDialogue);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= EndDialogue;
        }

        private void EndDialogue() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(DialogueOverlayUI));
    }
}
