using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>Save menu overlay — return or cancel goes back to previous state.</summary>
    public class SaveMenuOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _returnButton;

        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            _returnButton.onClick.AddListener(Return);
            _returnButton.image.color = Selected;

            InputManager.Instance.OnConfirm += Return;
            InputManager.Instance.OnCancel += Return;
        }

        private void OnDisable()
        {
            _returnButton.onClick.RemoveListener(Return);

            InputManager.Instance.OnConfirm -= Return;
            InputManager.Instance.OnCancel -= Return;
        }

        private void Return() => GameStateManager.Instance.ReturnFromContextMenu(nameof(SaveMenuOverlayUI));
    }
}
