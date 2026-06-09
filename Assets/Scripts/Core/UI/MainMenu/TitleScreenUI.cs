using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.MainMenu
{
    // Title screen UI — single "Start" button that transitions to MainMenu.
    public class TitleScreenUI : MonoBehaviour
    {
        private void OnEnable()
        {
            InputManager.Instance.OnConfirm += PlayerPressedConfirm;
            AudioManager.Instance?.PlayMusic(SoundId.MusicTitle);
        }

        private void OnDisable() => InputManager.Instance.OnConfirm -= PlayerPressedConfirm;

        private void PlayerPressedConfirm()
        {
            AudioManager.Instance?.Play(SoundId.ConfirmStartGame);
            GameStateManager.Instance.RequestTransition(GameState.MainMenu, "TitleScreenUI");
        }
    }
}
