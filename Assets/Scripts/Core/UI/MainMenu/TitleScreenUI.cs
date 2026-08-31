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
        private bool hasStartedCampaign;

        private void OnEnable()
        {
            InputManager.Instance.OnConfirm += PlayerPressedConfirm;
            AudioManager.Instance?.PlayMusic(SoundId.MusicTitle);
        }

        private void OnDisable() => InputManager.Instance.OnConfirm -= PlayerPressedConfirm;

        // This scene stays loaded and listening all through the fade out, so without the guard a
        // second press would start the campaign again on top of the start already under way.
        private void PlayerPressedConfirm()
        {
            if (hasStartedCampaign) return;
            hasStartedCampaign = true;

            AudioManager.Instance?.Play(SoundId.ConfirmStartGame);

            if (GameFlow.Instance != null) GameFlow.Instance.Begin();
            else GameStateManager.Instance.RequestTransition(GameState.Cutscene, "TitleScreenUI");
        }
    }
}
