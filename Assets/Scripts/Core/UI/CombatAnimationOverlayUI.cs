using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>Combat animation overlay — confirm returns to battle map.</summary>
    public class CombatAnimationOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _returnButton;

        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            _returnButton.onClick.AddListener(ReturnToBattle);
            _returnButton.image.color = Selected;

            InputManager.Instance.OnConfirm += ReturnToBattle;
        }

        private void OnDisable()
        {
            _returnButton.onClick.RemoveListener(ReturnToBattle);

            InputManager.Instance.OnConfirm -= ReturnToBattle;
        }

        private void ReturnToBattle() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(CombatAnimationOverlayUI));
    }
}
