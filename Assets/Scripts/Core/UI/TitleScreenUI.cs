using UnityEngine;

namespace ProjectAstra.Core.UI
{
    /// <summary>Title screen UI — single "Start" button that transitions to MainMenu.</summary>
    public class TitleScreenUI : MonoBehaviour
    {
        private void OnEnable() => InputManager.Instance.OnConfirm += PlayerPressedConfirm;

        private void OnDisable() => InputManager.Instance.OnConfirm -= PlayerPressedConfirm;

        private void PlayerPressedConfirm() => GameStateManager.Instance.RequestTransition(GameState.MainMenu, "TitleScreenUI");
    }
}
