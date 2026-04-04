using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Save menu overlay — return or cancel goes back to previous state.
    /// </summary>
    public class SaveMenuOverlayUI : MonoBehaviour
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
            _returnButton.onClick.AddListener(Return);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm += Return;
            InputManager.Instance.OnCancel += Return;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
        }

        private void RemoveListenersToMouseClicks()
        {
            _returnButton.onClick.RemoveListener(Return);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= Return;
            InputManager.Instance.OnCancel -= Return;
        }

        private void Return() => GameStateManager.Instance.ReturnFromContextMenu(nameof(SaveMenuOverlayUI));
    }
}
