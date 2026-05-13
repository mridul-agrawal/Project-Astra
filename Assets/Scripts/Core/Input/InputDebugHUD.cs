using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Input
{
    // IMGUI overlay that logs the last N InputManager events in real time. Drop onto any
    // GameObject to debug input dispatch — what fired, which device, which state.
    public class InputDebugHUD : MonoBehaviour
    {
        private const int MaxLines = 12;
        private const float PanelWidth = 300f;
        private const float PanelHeight = 280f;
        private const float PanelMargin = 10f;

        private readonly List<string> _log = new();
        private Vector2 _scrollPos;
        private bool _visible = true;

        private void Start()
        {
            if (InputManager.Instance == null)
            {
                Log("ERROR: InputManager.Instance is null");
                return;
            }

            InputManager.Instance.OnCursorMove += dir => Log($"CursorMove {dir}");
            InputManager.Instance.OnConfirm += () => Log("Confirm");
            InputManager.Instance.OnCancel += () => Log("Cancel");
            InputManager.Instance.OnOpenMapMenu += () => Log("OpenMapMenu");
            InputManager.Instance.OnOpenUnitInfo += () => Log("OpenUnitInfo");
            InputManager.Instance.OnToggleMapOverlay += () => Log("ToggleMapOverlay");
            InputManager.Instance.OnPause += () => Log("Pause");
            InputManager.Instance.OnSkipAnimation += () => Log("SkipAnimation");
            InputManager.Instance.OnSkipDialogue += () => Log("SkipDialogue");
            InputManager.Instance.OnHoldAdvanceDialogue += held => Log($"HoldAdvanceDialogue {(held ? "ON" : "OFF")}");
            InputManager.Instance.OnNextUnit += () => Log("NextUnit");
            InputManager.Instance.OnPrevUnit += () => Log("PrevUnit");
            InputManager.Instance.OnDeviceChanged += device => Log($"Device → {device}");
        }

        private void OnGUI()
        {
            if (!_visible) return;

            float x = Screen.width - PanelWidth - PanelMargin;
            GUILayout.BeginArea(new Rect(x, PanelMargin, PanelWidth, PanelHeight), GUI.skin.box);

            GUILayout.Label($"<b>Input Debug</b>  |  State: {GameStateManager.Instance?.CurrentState}");
            GUILayout.Label($"Device: {InputManager.Instance?.ActiveDevice}  |  FastCursor: {InputManager.Instance?.IsFastCursorHeld}");
            GUILayout.Space(4);

            foreach (var line in _log)
                GUILayout.Label(line);

            GUILayout.EndArea();
        }

        private void Log(string msg)
        {
            _log.Add($"[{Time.frameCount}] {msg}");
            if (_log.Count > MaxLines) _log.RemoveAt(0);
        }
    }
}
