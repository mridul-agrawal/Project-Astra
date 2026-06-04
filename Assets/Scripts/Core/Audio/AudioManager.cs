using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectAstra.Core.Audio
{
    // Plays all game audio — SFX, music, and ambient — by SoundId.
    // Lives in BootScene and stays alive across every scene.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioLibrary library;
        [SerializeField] private int initialSfxSources = 8;
        [SerializeField] private float defaultMusicFade = 1f;

        private readonly List<AudioSource> sfxPool = new();
        private readonly Dictionary<AudioBus, AudioMixerGroup> groups = new();
        private AudioSource[] musicSources;
        private AudioSource ambientSource;
        private int activeMusicIndex;
        private Coroutine musicFade;

        private const float SILENCE_DECIBELS = -80f;
        private const float MIN_AUDIBLE_LINEAR = 0.0001f;

        // --- Lifecycle -------------------------------------------------

        private void Awake()
        {
            if (!EnsureSingleInstance()) return;
            BuildAudioSources();
            RestoreSavedVolumes();
        }

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
            musicSources = new[] { CreateSource("Music A", AudioBus.Music), CreateSource("Music B", AudioBus.Music) };
            ambientSource = CreateSource("Ambient", AudioBus.Ambient);
            ambientSource.loop = true;
            for (int i = 0; i < initialSfxSources; i++)
                sfxPool.Add(CreateSource($"SFX {i}", AudioBus.Sfx));
        }

        private void CacheMixerGroups()
        {
            if (mixer == null) return;
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
            {
                var matchingGroups = mixer.FindMatchingGroups(bus.ToString());
                if (matchingGroups.Length > 0) groups[bus] = matchingGroups[0];
            }
        }

        private AudioSource CreateSource(string label, AudioBus bus)
        {
            var sourceObject = new GameObject(label);
            sourceObject.transform.SetParent(transform);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = GroupFor(bus);
            return source;
        }

        private void RestoreSavedVolumes()
        {
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
                SetVolume(bus, GetVolume(bus));
        }

        // --- Play a one-shot sound (SFX / UI) --------------------------

        public void Play(SoundId id) => PlaySound(FindSound(id));

        private SoundSO FindSound(SoundId id)
        {
            var sound = library != null ? library.GetSound(id) : null;
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

        private bool IsPlayable(SoundSO sound)
        {
            if (sound != null && sound.HasClip) return true;
            if (sound != null) Debug.LogWarning($"[AudioManager] '{sound.name}' has no clip assigned.");
            return false;
        }

        private AudioSource ReserveSfxSource()
        {
            foreach (var source in sfxPool)
                if (!source.isPlaying) return source;

            var newSource = CreateSource($"SFX {sfxPool.Count}", AudioBus.Sfx);
            sfxPool.Add(newSource);
            return newSource;
        }

        private void ConfigureOneShot(AudioSource source, SoundSO sound)
        {
            source.clip = sound.PickClip();
            source.volume = sound.Volume;
            source.pitch = sound.PickPitch();
            source.loop = false;
            source.outputAudioMixerGroup = GroupFor(sound.Bus);
        }

        // --- Music (crossfaded between two sources) --------------------

        public void PlayMusic(SoundId id) => PlayMusic(id, defaultMusicFade);

        public void PlayMusic(SoundId id, float fadeSeconds) => PlayTrack(FindSound(id), fadeSeconds);

        private void PlayTrack(SoundSO music, float fadeSeconds)
        {
            if (!IsPlayable(music)) return;
            RestartMusicFade(CrossfadeRoutine(music, fadeSeconds));
        }

        public void StopMusic(float fadeSeconds)
        {
            RestartMusicFade(FadeOutRoutine(musicSources[activeMusicIndex], fadeSeconds));
        }

        private void RestartMusicFade(IEnumerator routine)
        {
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(routine);
        }

        private IEnumerator CrossfadeRoutine(SoundSO music, float fadeSeconds)
        {
            var outgoing = musicSources[activeMusicIndex];
            activeMusicIndex = 1 - activeMusicIndex;
            var incoming = musicSources[activeMusicIndex];

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
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                float progress = elapsed / seconds;
                outgoing.volume = Mathf.Lerp(fromVolume, 0f, progress);
                incoming.volume = Mathf.Lerp(0f, targetVolume, progress);
                yield return null;
            }
            incoming.volume = targetVolume;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float seconds)
        {
            float fromVolume = source.volume;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(fromVolume, 0f, elapsed / seconds);
                yield return null;
            }
            source.Stop();
        }

        // --- Ambient loop ----------------------------------------------

        public void PlayAmbient(SoundId id) => PlayLoop(FindSound(id));

        private void PlayLoop(SoundSO ambient)
        {
            if (!IsPlayable(ambient)) return;
            ambientSource.clip = ambient.PickClip();
            ambientSource.volume = ambient.Volume;
            ambientSource.outputAudioMixerGroup = GroupFor(AudioBus.Ambient);
            ambientSource.Play();
        }

        public void StopAmbient() => ambientSource.Stop();

        // --- Volume (mixer + remembered between launches) --------------

        public void SetVolume(AudioBus bus, float linear)
        {
            float clampedVolume = Mathf.Clamp01(linear);
            if (mixer != null) mixer.SetFloat(ParamFor(bus), LinearToDecibels(clampedVolume));
            PlayerPrefs.SetFloat(PrefKey(bus), clampedVolume);
            PlayerPrefs.Save();
        }

        public float GetVolume(AudioBus bus) => PlayerPrefs.GetFloat(PrefKey(bus), 1f);

        private static string ParamFor(AudioBus bus) => $"{bus}Volume";

        private static string PrefKey(AudioBus bus) => $"audio.volume.{bus}";

        private static float LinearToDecibels(float linear) => linear <= MIN_AUDIBLE_LINEAR ? SILENCE_DECIBELS : Mathf.Log10(linear) * 20f;

        // --- Shared ----------------------------------------------------

        private AudioMixerGroup GroupFor(AudioBus bus) => groups.TryGetValue(bus, out var group) ? group : null;
    }
}
