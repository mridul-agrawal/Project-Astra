using UnityEngine;

namespace ProjectAstra.Core.Combat.Playback
{
    // UI-10 — player's preferred combat animation speed, with a one-shot
    // per-combat override (set by GridCursor.HandleConfirm when SkipAnimation
    // is held during the targeting Confirm, cleared in the dispatcher's
    // finally after the combat resolves).
    //
    // Persistence: PlayerPrefs (key "combat.anim.speed"). _default seeds the
    // value the first time the game runs; subsequent runs read the stored
    // preference.
    [CreateAssetMenu(menuName = "Project Astra/Combat/Combat Animation Settings")]
    public class CombatAnimationSettings : ScriptableObject
    {
        private const string PrefsKey = "combat.anim.speed";

        [Tooltip("Speed used when no preference is persisted yet.")]
        [SerializeField] private CombatAnimationSpeed _default = CombatAnimationSpeed.Skip;

        // NOT serialized — purely runtime, cleared at the end of each combat.
        private CombatAnimationSpeed? _runtimeOverride;

        public CombatAnimationSpeed Persisted
        {
            get => (CombatAnimationSpeed)PlayerPrefs.GetInt(PrefsKey, (int)_default);
            set { PlayerPrefs.SetInt(PrefsKey, (int)value); PlayerPrefs.Save(); }
        }

        public CombatAnimationSpeed EffectiveSpeed => _runtimeOverride ?? Persisted;

        public void SetOneShotOverride(CombatAnimationSpeed speed) => _runtimeOverride = speed;
        public void ClearOneShotOverride() => _runtimeOverride = null;
    }
}
