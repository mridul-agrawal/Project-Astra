using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Overlays
{
    // Owns the lifecycle of prefab-based overlay UIs. Listens to game state changes;
    // instantiates the matching prefab from Resources/Overlays/ when entering an overlay
    // state, tears it down when leaving. In-scene overlays (WarLedger, LevelUpScreen)
    // are not handled here — they self-manage from inside their parent scene.
    public class OverlayManager : MonoBehaviour
    {
        const string OverlayResourceFolder = "Overlays";

        [SerializeField] private GameStateEventChannel _stateChangedChannel;

        private GameObject _activeOverlay;

        private static readonly HashSet<GameState> ManagedOverlays = new()
        {
            GameState.BattleMapPaused,
            GameState.CombatAnimation,
            GameState.Dialogue,
            GameState.SaveMenu,
            GameState.SettingsMenu,
        };

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void OnEnable()  => _stateChangedChannel.Register(OnStateChanged);
        private void OnDisable() => _stateChangedChannel.Unregister(OnStateChanged);

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            DestroyActiveOverlay();
            if (ManagedOverlays.Contains(args.NewState))
                ShowOverlayFor(args.NewState);
        }

        private void ShowOverlayFor(GameState state)
        {
            var prefab = Resources.Load<GameObject>($"{OverlayResourceFolder}/{state}");
            if (prefab != null)
                _activeOverlay = Instantiate(prefab);
            else
                Debug.LogError($"[OverlayManager] Overlay prefab not found: Resources/{OverlayResourceFolder}/{state}");
        }

        private void DestroyActiveOverlay()
        {
            if (_activeOverlay == null) return;
            Destroy(_activeOverlay);
            _activeOverlay = null;
        }
    }
}
