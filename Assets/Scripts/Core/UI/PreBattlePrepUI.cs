using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Pre-battle preparation UI — confirm starts the battle.
    /// </summary>
    public class PreBattlePrepUI : MonoBehaviour
    {
        [SerializeField] private Button _battleMapButton;

        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            _battleMapButton.image.color = Selected;
        }

        private void AddListenersToMouseClicks()
        {
            _battleMapButton.onClick.AddListener(GoToBattleMap);
        }

        private void AddListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm += GoToBattleMap;
        }

        private void OnDisable()
        {
            RemoveListenersToMouseClicks();
            RemoveListenerToGameplayInputs();
        }

        private void RemoveListenersToMouseClicks()
        {
            _battleMapButton.onClick.RemoveListener(GoToBattleMap);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= GoToBattleMap;
        }

        private void GoToBattleMap() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(PreBattlePrepUI));
    }
}
