using UnityEngine;

namespace ProjectAstra.Core.Audio
{
    // The whole game's audio playback. One singleton, one AudioListener, one
    // pool of AudioSources that any system rents from. Call SoundSO.Play() (or
    // AudioManager.Instance.Play(sound)) from anywhere — gameplay, UI, dialogue.
    [RequireComponent(typeof(AudioListener))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Tooltip("How many sounds can play at the same time. Higher = more concurrent SFX but more memory.")]
        [SerializeField] private int _poolSize = 10;

        private AudioSource[] _pool;
        private SoundSO[] _currentSoundPerSource;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Play(SoundSO sound)
        {
            if (sound == null) return;
            var clip = sound.PickClip();
            if (clip == null) return;

            if (sound.SingleInstance) StopAnyPriorPlaysOf(sound);

            int slot = FindFreeSlotOrSteal();
            var src = _pool[slot];
            src.clip = clip;
            src.volume = sound.PickVolume();
            src.pitch  = sound.PickPitch();
            src.outputAudioMixerGroup = sound.MixerGroup;
            src.Play();
            _currentSoundPerSource[slot] = sound;
        }

        private void BuildPool()
        {
            _pool = new AudioSource[_poolSize];
            _currentSoundPerSource = new SoundSO[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                _pool[i] = src;
            }
        }

        private int FindFreeSlotOrSteal()
        {
            for (int i = 0; i < _pool.Length; i++)
                if (!_pool[i].isPlaying) return i;
            return 0;  // Pool exhausted — overwrite slot 0 (rare; tune _poolSize if it happens).
        }

        private void StopAnyPriorPlaysOf(SoundSO sound)
        {
            for (int i = 0; i < _pool.Length; i++)
                if (_currentSoundPerSource[i] == sound && _pool[i].isPlaying) _pool[i].Stop();
        }
    }
}
