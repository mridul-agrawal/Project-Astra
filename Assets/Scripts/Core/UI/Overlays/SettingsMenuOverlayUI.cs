using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Overlays
{
    // Settings menu overlay — return or cancel goes back to previous state.
    // Adds a combat-anim speed dropdown (UI-10): Normal / Fast / Skip,
    // written through to CombatAnimationSettings.Persisted (PlayerPrefs).
    public class SettingsMenuOverlayUI : MonoBehaviour
    {
        [SerializeField] private Button _returnButton;

        [Header("Combat animation speed (UI-10)")]
        [SerializeField] private TMP_Dropdown _combatSpeedDropdown;
        [Tooltip("Optional tooltip line shown below the dropdown.")]
        [SerializeField] private TMP_Text _combatSpeedHint;

        [SerializeField] private Color Selected = new(0.4f, 0.4f, 0.6f, 1f);

        private const string SpeedHintText =
            "Hold Skip while confirming an attack to swap speed for that combat only.";

        private void OnEnable()
        {
            AddListenersToMouseClicks();
            AddListenerToGameplayInputs();
            _returnButton.image.color = Selected;
            PopulateSpeedDropdown();
        }

        private void AddListenersToMouseClicks()
        {
            _returnButton.onClick.AddListener(Return);
            if (_combatSpeedDropdown != null)
                _combatSpeedDropdown.onValueChanged.AddListener(OnSpeedChanged);
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
            if (_combatSpeedDropdown != null)
                _combatSpeedDropdown.onValueChanged.RemoveListener(OnSpeedChanged);
        }

        private void RemoveListenerToGameplayInputs()
        {
            InputManager.Instance.OnConfirm -= Return;
            InputManager.Instance.OnCancel -= Return;
        }

        private void PopulateSpeedDropdown()
        {
            if (_combatSpeedDropdown == null) return;
            var settings = CombatAnimationSettingsRef.Current;
            if (settings == null) return;

            _combatSpeedDropdown.ClearOptions();
            _combatSpeedDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Normal", "Fast", "Skip"
            });
            // Enum order: Normal=0, Fast=1, Skip=2 — matches dropdown index.
            _combatSpeedDropdown.SetValueWithoutNotify((int)settings.Persisted);
            if (_combatSpeedHint != null) _combatSpeedHint.text = SpeedHintText;
        }

        private void OnSpeedChanged(int index)
        {
            var settings = CombatAnimationSettingsRef.Current;
            if (settings == null) return;
            settings.Persisted = (CombatAnimationSpeed)index;
        }

        private void Return() => GameStateManager.Instance.ReturnFromContextMenu(nameof(SettingsMenuOverlayUI));
    }
}
