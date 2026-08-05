using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.Cursor.Debugging
{
    // The evaluation harness: what state the cursor is in right now, buttons to force it
    // somewhere, and a way to flip between variants without leaving play mode.
    //
    // Reads the keyboard directly rather than going through InputManager, so the shipped
    // .inputactions asset and GameInputAction enum stay clean of debug-only bindings.
    // Development builds and the editor only.
    public class CursorDebugOverlay : MonoBehaviour
    {
        [Tooltip("Left empty, the overlay finds the GridCursor and CursorVisualDirector in the scene.")]
        [SerializeField] private GridCursor cursor;
        [SerializeField] private CursorVisualDirector director;

        [Tooltip("Hides the panel without disabling the 1-4 hotkeys. Handy for clean screenshots.")]
        [SerializeField] private bool panelVisible = true;

        private const int PanelWidth = 250;
        private const int LineHeight = 22;

        private GUIStyle headerStyle;
        private GUIStyle readoutStyle;

        private void Awake()
        {
            if (cursor == null) cursor = FindAnyObjectByType<GridCursor>();
            if (director == null) director = FindAnyObjectByType<CursorVisualDirector>();
        }

        private void Update() => PollVariantHotkeys();

        // 1-4 pick a variant. Deliberately live in every state, including mid-selection —
        // proving a variant can be swapped without disturbing the FSM is the point.
        private void PollVariantHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || director == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) director.SetProfileByIndex(0);
            if (keyboard.digit2Key.wasPressedThisFrame) director.SetProfileByIndex(1);
            if (keyboard.digit3Key.wasPressedThisFrame) director.SetProfileByIndex(2);
            if (keyboard.digit4Key.wasPressedThisFrame) director.SetProfileByIndex(3);
            if (keyboard.f1Key.wasPressedThisFrame) panelVisible = !panelVisible;
        }

        private void OnGUI()
        {
            if (!panelVisible || cursor == null) return;

            EnsureStyles();

            GUILayout.BeginArea(new Rect(12, 12, PanelWidth, 460), GUI.skin.box);
            DrawReadout();
            GUILayout.Space(8);
            DrawVariantPicker();
            GUILayout.Space(8);
            DrawForceStateButtons();
            GUILayout.Space(8);
            DrawResetButton();
            GUILayout.EndArea();
        }

        private void DrawReadout()
        {
            GUILayout.Label("CURSOR", headerStyle);
            GUILayout.Label($"State    {cursor.CurrentState}", readoutStyle);
            GUILayout.Label($"Hover    {cursor.CurrentHover}", readoutStyle);
            GUILayout.Label($"Mode     {cursor.CurrentMode}", readoutStyle);
            GUILayout.Label($"Tile     {cursor.GridPosition.x}, {cursor.GridPosition.y}", readoutStyle);

            var visual = CursorVisualStateMap.From(cursor.CurrentState, cursor.CurrentHover);
            GUILayout.Label($"Visual   {visual}", readoutStyle);
        }

        private void DrawVariantPicker()
        {
            GUILayout.Label("VARIANT   (1-4)", headerStyle);

            if (director == null || director.Profiles.Count == 0)
            {
                GUILayout.Label("No profiles assigned.", readoutStyle);
                return;
            }

            for (int i = 0; i < director.Profiles.Count; i++)
            {
                var profile = director.Profiles[i];
                if (profile == null) continue;

                bool active = profile == director.ActiveProfile;
                string label = $"{i + 1}. {profile.DisplayName}{(active ? "   <" : "")}";
                if (GUILayout.Button(label)) director.SetProfile(profile);
            }
        }

        private void DrawForceStateButtons()
        {
            GUILayout.Label("FORCE STATE", headerStyle);

            if (GUILayout.Button("Free")) cursor.SetMode(CursorMode.Free);
            if (GUILayout.Button("Locked")) cursor.SetMode(CursorMode.Locked);
        }

        private void DrawResetButton()
        {
            GUILayout.Label("SCENARIO", headerStyle);

            if (GUILayout.Button("Reset"))
            {
                cursor.SetMode(CursorMode.Free);
                cursor.SetPosition(Vector2Int.zero);
                FindAnyObjectByType<CursorLabBootstrapper>()?.Rebuild();
                TurnManager.Instance?.StartBattle();
            }

            GUILayout.Label("F1 hides this panel.", readoutStyle);
        }

        private void EnsureStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            readoutStyle = new GUIStyle(GUI.skin.label) { fixedHeight = LineHeight };
        }
    }
}
