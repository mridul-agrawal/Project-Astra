using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>Dialogue overlay — confirm ends dialogue and returns to battle map.</summary>
    public class DialogueOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _endDialogueButton;

        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            _endDialogueButton.onClick.AddListener(EndDialogue);
            _endDialogueButton.image.color = Selected;

            InputManager.Instance.OnConfirm += EndDialogue;
        }

        private void OnDisable()
        {
            _endDialogueButton.onClick.RemoveListener(EndDialogue);

            InputManager.Instance.OnConfirm -= EndDialogue;
        }

        private void EndDialogue() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(DialogueOverlayUI));
    }
}
