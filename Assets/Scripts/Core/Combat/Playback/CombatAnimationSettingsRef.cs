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
        [SerializeField] private CombatAnimationSettings _asset;

        public static CombatAnimationSettings Current { get; private set; }

        private void Awake() { if (_asset != null) Current = _asset; }
        private void OnDestroy() { if (Current == _asset) Current = null; }
    }
}
