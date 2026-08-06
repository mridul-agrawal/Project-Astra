using UnityEngine;

namespace ProjectAstra.Core.Rendering
{
    // The player's CRT filter preference, persisted across runs.
    //
    // Mirrors CombatAnimationSettings: a ScriptableObject wrapping a PlayerPrefs key, with a
    // serialized default that seeds the very first run. The settings menu reads and writes
    // Persisted; everything else reads it through CrtProfileBinder.
    [CreateAssetMenu(fileName = "CrtSettings", menuName = "Project Astra/Rendering/CRT Settings")]
    public class CrtSettings : ScriptableObject
    {
        private const string PrefsKey = "crt.quality";

        [Tooltip("Used until the player picks something. Off is the safe default for a first run "
            + "— the filter is a taste, not an improvement everyone agrees on.")]
        [SerializeField] private CrtQuality @default = CrtQuality.Off;

        public CrtQuality Persisted
        {
            get => (CrtQuality)PlayerPrefs.GetInt(PrefsKey, (int)@default);
            set { PlayerPrefs.SetInt(PrefsKey, (int)value); PlayerPrefs.Save(); }
        }
    }

    public enum CrtQuality
    {
        Off = 0,
        Subtle = 1,
        Full = 2,
    }
}
