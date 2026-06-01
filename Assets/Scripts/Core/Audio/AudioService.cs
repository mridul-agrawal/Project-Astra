using UnityEngine;
using ProjectAstra.Core.Combat.Playback;

namespace ProjectAstra.Core.Audio
{
    // Lightweight audio routing singleton. Bootstrapped in BootScene alongside
    // OverlayManager (DontDestroyOnLoad). Both AudioSources are pre-configured
    // siblings on the AudioService GameObject (not runtime-instantiated):
    //   _sfx   — PlayOneShot, concurrency-friendly (brave-rush impacts overlap).
    //   _voice — Stop+Play, one voice line at a time. Voice early-outs when
    //            CombatTiming.ShouldPlayVoice is false (Fast/Skip).
    [RequireComponent(typeof(AudioListener))]
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; }

        [Tooltip("AudioSource sibling component used for one-shot SFX (PlayOneShot).")]
        [SerializeField] private AudioSource _sfx;

        [Tooltip("AudioSource sibling component used for voice barks (Stop+Play).")]
        [SerializeField] private AudioSource _voice;

        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float _voiceVolume = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_sfx == null)   Debug.LogError("[AudioService] _sfx AudioSource not wired in inspector.");
            if (_voice == null) Debug.LogError("[AudioService] _voice AudioSource not wired in inspector.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _sfx == null) return;
            _sfx.PlayOneShot(clip, Mathf.Clamp01(_sfxVolume * volumeScale));
        }

        public void PlayVoiceBark(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _voice == null) return;
            if (!CombatTiming.ShouldPlayVoice()) return;
            _voice.Stop();
            _voice.clip = clip;
            _voice.volume = Mathf.Clamp01(_voiceVolume * volumeScale);
            _voice.Play();
        }
    }
}
