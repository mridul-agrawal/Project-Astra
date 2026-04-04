using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>Pre-battle preparation UI — confirm starts the battle.</summary>
    public class PreBattlePrepUI : MonoBehaviour
    {
        [SerializeField] private Button _battleMapButton;

        private static readonly Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private void OnEnable()
        {
            _battleMapButton.onClick.AddListener(GoToBattleMap);
            _battleMapButton.image.color = Selected;

            InputManager.Instance.OnConfirm += GoToBattleMap;
        }

        private void OnDisable()
        {
            _battleMapButton.onClick.RemoveListener(GoToBattleMap);

            InputManager.Instance.OnConfirm -= GoToBattleMap;
        }

        private void GoToBattleMap() => GameStateManager.Instance.RequestTransition(GameState.BattleMap, nameof(PreBattlePrepUI));
    }
}
