using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.MainMenu
{
    // Title screen UI — Confirm starts the campaign at its first beat (the opening
    // cutscene) via GameFlow, instead of routing through a main menu.
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
            // Boot flow owns GameFlow; the fallback keeps editor direct-play of the title working.
            if (GameFlow.Instance != null) GameFlow.Instance.Begin();
            else GameStateManager.Instance.RequestTransition(GameState.Cutscene, "TitleScreenUI");
        }
    }
}
