using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Events;
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

        private GameObject _activeOverlay;
        private bool subscribed;

        // Dialogue is intentionally not here: DialogueService owns one persistent
        // dialogue view across scenes, so OverlayManager must not also spawn one.
        private static readonly HashSet<GameState> ManagedOverlays = new()
        {
            GameState.BattleMapPaused,
            GameState.CombatAnimation,
            GameState.SaveMenu,
            GameState.SettingsMenu,
        };

        private void Awake() => DontDestroyOnLoad(gameObject);

        // Subscribe in Start (not OnEnable) so EventService.Instance is set — this boots
        // alongside EventService, so relying on OnEnable ordering isn't safe.
        private void Start()
        {
            EventService.Instance.SubscribeGameStateChanged(OnStateChanged);
            subscribed = true;
        }

        private void OnDestroy()
        {
            if (subscribed && EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnStateChanged);
        }

        private void OnStateChanged(StateChangeArgs args)
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
