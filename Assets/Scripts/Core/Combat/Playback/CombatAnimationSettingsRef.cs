using UnityEngine;

namespace ProjectAstra.Core.Combat.Playback
{
    // Scene bootstrap MonoBehaviour that exposes the active CombatAnimationSettings
    // asset to static callers (CombatTiming, CombatPlaybackDispatcher). Mirrors
    // the DialogueSettings access pattern.
    //
    // Place one instance per scene that needs combat (BattleMap). The single
    // serialized asset reference is shared across that scene.
    public class CombatAnimationSettingsRef : MonoBehaviour
    {
        [SerializeField] private CombatAnimationSettings asset;

        public static CombatAnimationSettings Current { get; private set; }

        private void Awake() { if (asset != null) Current = asset; }
        private void OnDestroy() { if (Current == asset) Current = null; }
    }
}
