using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Rendering;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Overlays
{
    // Settings menu overlay — return or cancel goes back to previous state.
    // Adds a combat-anim speed dropdown (UI-10): Normal / Fast / Skip,
    // written through to CombatAnimationSettings.Persisted (PlayerPrefs).
    public class SettingsMenuOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button returnButton;

        [Header("Combat animation speed (UI-10)")]
        [SerializeField] private TMP_Dropdown combatSpeedDropdown;
        [Tooltip("Optional tooltip line shown below the dropdown.")]
        [SerializeField] private TMP_Text combatSpeedHint;

        [Header("CRT filter")]
        [Tooltip("Off / Subtle / Full. Leave unassigned until the dropdown exists in the prefab.")]
        [SerializeField] private TMP_Dropdown crtDropdown;
        [SerializeField] private CrtSettings crtSettings;

        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private const string SpeedHintText =
            "Hold Skip while confirming an attack to swap speed for that combat only.";

        private void OnEnable()
        {
            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            returnButton.image.color = Selected;
            PopulateSpeedDropdown();
            PopulateCrtDropdown();
            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        private void AddListenersToMouseClicks()
        {
            returnButton.onClick.AddListener(Return);
            if (combatSpeedDropdown != null)
                combatSpeedDropdown.onValueChanged.AddListener(OnSpeedChanged);
            if (crtDropdown != null)
                crtDropdown.onValueChanged.AddListener(OnCrtChanged);
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
            returnButton.onClick.RemoveListener(Return);
            if (combatSpeedDropdown != null)
                combatSpeedDropdown.onValueChanged.RemoveListener(OnSpeedChanged);
            if (crtDropdown != null)
                crtDropdown.onValueChanged.RemoveListener(OnCrtChanged);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= Return;
            InputManager.Instance.OnCancel -= Return;
        }

        private void PopulateSpeedDropdown()
        {
            if (combatSpeedDropdown == null) return;
            var settings = CombatAnimationSettingsRef.Current;
            if (settings == null) return;

            combatSpeedDropdown.ClearOptions();
            combatSpeedDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Normal", "Fast", "Skip"
            });
            // Enum order: Normal=0, Fast=1, Skip=2 — matches dropdown index.
            combatSpeedDropdown.SetValueWithoutNotify((int)settings.Persisted);
            if (combatSpeedHint != null) combatSpeedHint.text = SpeedHintText;
        }

        private void PopulateCrtDropdown()
        {
            if (crtDropdown == null || crtSettings == null) return;

            crtDropdown.ClearOptions();
            crtDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Off", "Subtle", "Full"
            });
            // Enum order: Off=0, Subtle=1, Full=2 — matches dropdown index.
            crtDropdown.SetValueWithoutNotify((int)crtSettings.Persisted);
        }

        private void OnCrtChanged(int index)
        {
            if (crtSettings == null) return;
            crtSettings.Persisted = (CrtQuality)index;
        }

        private void OnSpeedChanged(int index)
        {
            var settings = CombatAnimationSettingsRef.Current;
            if (settings == null) return;
            settings.Persisted = (CombatAnimationSpeed)index;
        }

        private void Return()
        {
            AudioManager.Instance?.Play(SoundId.UiCancel);
            GameStateManager.Instance.ReturnFromContextMenu(nameof(SettingsMenuOverlayUI));
        }
    }
}
