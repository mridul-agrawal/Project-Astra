using UnityEngine;
using UnityEngine.Audio;

namespace ProjectAstra.Core.Audio
{
    // A single playable sound asset. Holds one or more clips (the manager picks
    // one at random each play), a volume range, a pitch range, and the mixer
    // group it routes through. Designers author one of these per cue (click,
    // cursor move, weapon impact, etc.) and gameplay code holds a typed
    // reference instead of a string key or raw AudioClip.
    [CreateAssetMenu(menuName = "Project Astra/Audio/Sound", fileName = "Sound")]
    public class SoundSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;
        [SerializeField] private AudioMixerGroup _mixerGroup;

        [Tooltip("X = min volume, Y = max volume. Each play picks a random value in this range.")]
        [SerializeField] private Vector2 _volumeRange = new(1f, 1f);

        [Tooltip("X = min pitch, Y = max pitch. Each play picks a random value in this range.")]
        [SerializeField] private Vector2 _pitchRange = new(1f, 1f);

        [Tooltip("If on, playing this sound stops any other source already playing the same sound. " +
                 "Use for voice lines and other cues that should never overlap with themselves.")]
        [SerializeField] private bool _singleInstance;

        public AudioMixerGroup MixerGroup => _mixerGroup;
        public bool SingleInstance => _singleInstance;

        public AudioClip PickClip() =>
            _clips != null && _clips.Length > 0
                ? _clips[Random.Range(0, _clips.Length)]
                : null;

        public float PickVolume() => Random.Range(_volumeRange.x, _volumeRange.y);
        public float PickPitch()  => Random.Range(_pitchRange.x, _pitchRange.y);

        public void Play()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(this);
        }
    }
}
