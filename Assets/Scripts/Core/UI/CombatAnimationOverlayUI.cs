using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Combat animation overlay — confirm returns to battle map.
    /// </summary>
    public class CombatAnimationOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _returnButton;

        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            _returnButton.image.color = Selected;
        }

        private void AddListenersToMouseClicks()
        {
            _returnButton.onClick.AddListener(ReturnToBattle);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm += ReturnToBattle;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
        }

        private void RemoveListenersToMouseClicks()
        {
            _returnButton.onClick.RemoveListener(ReturnToBattle);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= ReturnToBattle;
        }

        private void ReturnToBattle() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(CombatAnimationOverlayUI));
    }
}
