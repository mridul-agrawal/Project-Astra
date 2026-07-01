using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // Audition audio clips straight from the Project window — no Inspector round-trip.
    // Select a clip and press the shortcut to hear it, or flip on Auto-Play to hear
    // each clip as you arrow through the list. Useful for surfing thousands of SFX.
    // Drives UnityEditor's internal AudioUtil by reflection (there is no public API).
    public static class AudioClipPreview
    {
        const string AutoPlayPrefKey = "ProjectAstra.AudioClipPreview.AutoPlay";
        const string AutoPlayMenu    = "Tools/Audio/Auto-Play Clips On Select";

        // Defaults: Shift+P plays, Shift+Alt+P stops. Rebind in Edit > Shortcuts
        // (search "Play Selected Audio Clip") if these collide with your layout.
        [Shortcut("Audio/Play Selected Audio Clip", KeyCode.P, ShortcutModifiers.Shift)]
        static void PlaySelectedShortcut() => PlaySelected();

        [Shortcut("Audio/Stop Audio Preview", KeyCode.P, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
        static void StopShortcut() => StopAll();

        static void PlaySelected()
        {
            if (Selection.activeObject is AudioClip clip)
            {
                StopAll();
                Preview(clip);
            }
        }

        // --- Auto-play on selection -------------------------------------

        [InitializeOnLoadMethod]
        static void HookOnLoad()
        {
            if (EditorPrefs.GetBool(AutoPlayPrefKey, false))
                Selection.selectionChanged += OnSelectionChanged;
        }

        [MenuItem(AutoPlayMenu)]
        static void ToggleAutoPlay()
        {
            bool enabled = !EditorPrefs.GetBool(AutoPlayPrefKey, false);
            EditorPrefs.SetBool(AutoPlayPrefKey, enabled);
            Selection.selectionChanged -= OnSelectionChanged;
            if (enabled) Selection.selectionChanged += OnSelectionChanged;
        }

        [MenuItem(AutoPlayMenu, validate = true)]
        static bool ToggleAutoPlayValidate()
        {
            Menu.SetChecked(AutoPlayMenu, EditorPrefs.GetBool(AutoPlayPrefKey, false));
            return true;
        }

        static void OnSelectionChanged()
        {
            if (Selection.activeObject is AudioClip clip)
            {
                StopAll();
                Preview(clip);
            }
        }

        // --- Internal AudioUtil reflection (method names vary by version) ---

        static MethodInfo _play;
        static MethodInfo _stop;

        static Type AudioUtilType => typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        public static void Preview(AudioClip clip)
        {
            if (clip == null) return;
            if (_play == null)
            {
                var t = AudioUtilType;
                _play = t?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
                            null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                     ?? t?.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public,
                            null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                     ?? t?.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public,
                            null, new[] { typeof(AudioClip) }, null);
            }
            if (_play == null)
            {
                Debug.LogWarning("[AudioClipPreview] AudioUtil play method not found in this Unity version.");
                return;
            }
            object[] args = _play.GetParameters().Length == 3
                ? new object[] { clip, 0, false }
                : new object[] { clip };
            _play.Invoke(null, args);
        }

        public static void StopAll()
        {
            if (_stop == null)
            {
                var t = AudioUtilType;
                _stop = t?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
                     ?? t?.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);
            }
            _stop?.Invoke(null, null);
        }
    }
}
