using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Dialogue overlay — confirm ends dialogue and returns to battle map.
    /// </summary>
    public class DialogueOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _endDialogueButton;

        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            _endDialogueButton.image.color = Selected;
        }

        private void AddListenersToMouseClicks()
        {
            _endDialogueButton.onClick.AddListener(EndDialogue);
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
            _endDialogueButton.onClick.RemoveListener(EndDialogue);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= EndDialogue;
        }

        private void EndDialogue() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(DialogueOverlayUI));
    }
}
