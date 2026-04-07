using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    [InitializeOnLoad]
    public static class AutoRecorder
    {
        private static RecorderController _controller;

        private const string EnabledPrefKey = "ProjectAstra_AutoRecorder_Enabled";

        static AutoRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Project Astra/Auto Recorder/Enable")]
        private static void Enable()
        {
            EditorPrefs.SetBool(EnabledPrefKey, true);
            Debug.Log("AutoRecorder: Enabled — recordings will auto-start on Play.");
        }

        [MenuItem("Project Astra/Auto Recorder/Disable")]
        private static void Disable()
        {
            EditorPrefs.SetBool(EnabledPrefKey, false);
            Debug.Log("AutoRecorder: Disabled.");
        }

        [MenuItem("Project Astra/Auto Recorder/Enable", true)]
        private static bool EnableValidate() => !IsEnabled();

        [MenuItem("Project Astra/Auto Recorder/Disable", true)]
        private static bool DisableValidate() => IsEnabled();

        private static bool IsEnabled() => EditorPrefs.GetBool(EnabledPrefKey, true);

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!IsEnabled()) return;

            if (state == PlayModeStateChange.EnteredPlayMode)
                StartRecording();
            else if (state == PlayModeStateChange.ExitingPlayMode)
                StopRecording();
        }

        private static void StartRecording()
        {
            string outputFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
            Directory.CreateDirectory(outputFolder);

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 30f;

            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.Enabled = true;
            movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.WebM;
            movieSettings.VideoBitRateMode = VideoBitrateMode.High;
            movieSettings.ImageInputSettings = new GameViewInputSettings();
            movieSettings.AudioInputSettings.PreserveAudio = true;
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            movieSettings.OutputFile = Path.Combine(outputFolder, $"devlog_{timestamp}");

            controllerSettings.AddRecorderSettings(movieSettings);

            _controller = new RecorderController(controllerSettings);
            _controller.PrepareRecording();
            _controller.StartRecording();

            Debug.Log($"AutoRecorder: Recording started → {outputFolder}");
        }

        private static void StopRecording()
        {
            if (_controller == null || !_controller.IsRecording()) return;

            _controller.StopRecording();
            _controller = null;
            Debug.Log("AutoRecorder: Recording saved.");
        }
    }
}
