using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectAstra.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioLibrary _library;
        [SerializeField] private int _initialSfxSources = 8;
        [SerializeField] private float _defaultMusicFade = 1f;

        private readonly List<AudioSource> _sfxPool = new();
        private readonly Dictionary<AudioBus, AudioMixerGroup> _groups = new();
        private AudioSource[] _musicSources;
        private AudioSource _ambientSource;
        private int _activeMusicIndex;
        private Coroutine _musicFade;

        private void Awake()
        {
            if (!EnsureSingleInstance()) return;
            BuildAudioSources();
            RestoreSavedVolumes();
        }

        // --- Public API (play by id) -----------------------------------

        public void Play(SoundId id) => PlaySound(Resolve(id));

        public void PlayMusic(SoundId id) => PlayMusic(id, _defaultMusicFade);

        public void PlayMusic(SoundId id, float fadeSeconds) => PlayTrack(Resolve(id), fadeSeconds);

        public void PlayAmbient(SoundId id) => PlayLoop(Resolve(id));

        public void StopMusic(float fadeSeconds)
        {
            RestartMusicFade(FadeOutRoutine(_musicSources[_activeMusicIndex], fadeSeconds));
        }

        public void StopAmbient() => _ambientSource.Stop();

        public void SetVolume(AudioBus bus, float linear)
        {
            float clamped = Mathf.Clamp01(linear);
            if (_mixer != null) _mixer.SetFloat(ParamFor(bus), LinearToDecibels(clamped));
            PlayerPrefs.SetFloat(PrefKey(bus), clamped);
            PlayerPrefs.Save();
        }

        public float GetVolume(AudioBus bus) => PlayerPrefs.GetFloat(PrefKey(bus), 1f);

        // --- Resolve + playback ----------------------------------------

        private SoundSO Resolve(SoundId id)
        {
            var sound = _library != null ? _library.Resolve(id) : null;
            if (sound == null) Debug.LogWarning($"[AudioManager] No sound mapped for id '{id}'.");
            return sound;
        }

        private void PlaySound(SoundSO sound)
        {
            if (!IsPlayable(sound)) return;
            var source = ReserveSfxSource();
            ConfigureOneShot(source, sound);
            source.Play();
        }

        private void PlayTrack(SoundSO music, float fadeSeconds)
        {
            if (!IsPlayable(music)) return;
            RestartMusicFade(CrossfadeRoutine(music, fadeSeconds));
        }

        private void PlayLoop(SoundSO ambient)
        {
            if (!IsPlayable(ambient)) return;
            _ambientSource.clip = ambient.PickClip();
            _ambientSource.volume = ambient.Volume;
            _ambientSource.outputAudioMixerGroup = GroupFor(AudioBus.Ambient);
            _ambientSource.Play();
        }

        // --- Setup ------------------------------------------------------

        private bool EnsureSingleInstance()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return false;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }

        private void BuildAudioSources()
        {
            CacheMixerGroups();
            _musicSources = new[] { CreateSource("Music A", AudioBus.Music), CreateSource("Music B", AudioBus.Music) };
            _ambientSource = CreateSource("Ambient", AudioBus.Ambient);
            _ambientSource.loop = true;
            for (int i = 0; i < _initialSfxSources; i++)
                _sfxPool.Add(CreateSource($"SFX {i}", AudioBus.Sfx));
        }

        private void CacheMixerGroups()
        {
            if (_mixer == null) return;
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
            {
                var matches = _mixer.FindMatchingGroups(bus.ToString());
                if (matches.Length > 0) _groups[bus] = matches[0];
            }
        }

        private AudioSource CreateSource(string label, AudioBus bus)
        {
            var host = new GameObject(label);
            host.transform.SetParent(transform);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = GroupFor(bus);
            return source;
        }

        private void RestoreSavedVolumes()
        {
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
                SetVolume(bus, GetVolume(bus));
        }

        // --- Playback helpers ------------------------------------------

        private bool IsPlayable(SoundSO sound)
        {
            if (sound != null && sound.HasClip) return true;
            if (sound != null) Debug.LogWarning($"[AudioManager] '{sound.name}' has no clip assigned.");
            return false;
        }

        private void ConfigureOneShot(AudioSource source, SoundSO sound)
        {
            source.clip = sound.PickClip();
            source.volume = sound.Volume;
            source.pitch = sound.PickPitch();
            source.loop = false;
            source.outputAudioMixerGroup = GroupFor(sound.Bus);
        }

        private AudioSource ReserveSfxSource()
        {
            foreach (var source in _sfxPool)
                if (!source.isPlaying) return source;

            var grown = CreateSource($"SFX {_sfxPool.Count}", AudioBus.Sfx);
            _sfxPool.Add(grown);
            return grown;
        }

        private AudioMixerGroup GroupFor(AudioBus bus) => _groups.TryGetValue(bus, out var group) ? group : null;

        // --- Music fading ----------------------------------------------

        private void RestartMusicFade(IEnumerator routine)
        {
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicFade = StartCoroutine(routine);
        }

        private IEnumerator CrossfadeRoutine(SoundSO music, float fadeSeconds)
        {
            var outgoing = _musicSources[_activeMusicIndex];
            _activeMusicIndex = 1 - _activeMusicIndex;
            var incoming = _musicSources[_activeMusicIndex];

            StartLoopingTrack(incoming, music);
            yield return Crossfade(outgoing, incoming, music.Volume, fadeSeconds);
            outgoing.Stop();
        }

        private void StartLoopingTrack(AudioSource source, SoundSO music)
        {
            source.clip = music.PickClip();
            source.pitch = 1f;
            source.loop = true;
            source.volume = 0f;
            source.outputAudioMixerGroup = GroupFor(AudioBus.Music);
            source.Play();
        }

        private IEnumerator Crossfade(AudioSource outgoing, AudioSource incoming, float targetVolume, float seconds)
        {
            float fromVolume = outgoing.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                outgoing.volume = Mathf.Lerp(fromVolume, 0f, k);
                incoming.volume = Mathf.Lerp(0f, targetVolume, k);
                yield return null;
            }
            incoming.volume = targetVolume;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float seconds)
        {
            float fromVolume = source.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(fromVolume, 0f, t / seconds);
                yield return null;
            }
            source.Stop();
        }

        // --- Volume conversion -----------------------------------------

        private static string ParamFor(AudioBus bus) => $"{bus}Volume";

        private static string PrefKey(AudioBus bus) => $"audio.volume.{bus}";

        private static float LinearToDecibels(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
