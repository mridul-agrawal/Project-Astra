using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAstra.Core.Cursor.Debugging
{
    // The variant evaluation harness, living in the real battle map rather than a sandbox —
    // the whole point is to judge the cursor over the actual map art, with real units and the
    // real flow, because that is where the decision gets made.
    //
    // Sits on its own scene root and draws with IMGUI, so it needs no Canvas and is not subject
    // to the project's disabled-by-default UI rule. Reads the keyboard directly: digits 1-4 and
    // F1 are unbound in the input asset and the context filter only ever touches actions inside
    // the Gameplay map, so these never collide with gameplay input.
    //
    // Editor and development builds only; it compiles out of a release build entirely.
    public class CursorDebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Tooltip("Left empty, the overlay finds the GridCursor and CursorVisualDirector in the scene.")]
        [SerializeField] private GridCursor cursor;
        [SerializeField] private CursorVisualDirector director;

        [Tooltip("Whether the panel is showing when the map loads. The hotkeys work either way.")]
        [SerializeField] private bool panelVisible = true;

        private const int PanelWidth = 230;

        private GUIStyle headerStyle;
        private GUIStyle readoutStyle;
        private GUIStyle activeVariantStyle;

        private void Awake() => FindReferences();

        // The cursor is a scene object and this may load before it, so re-resolve lazily rather
        // than caching a null forever.
        private void FindReferences()
        {
            if (cursor == null) cursor = FindAnyObjectByType<GridCursor>();
            if (director == null) director = FindAnyObjectByType<CursorVisualDirector>();
        }

        private void Update()
        {
            if (cursor == null || director == null) FindReferences();
            PollHotkeys();
        }

        // 1-4 pick a variant, and deliberately work in every state including mid-selection:
        // proving a variant can be swapped without disturbing the state machine is the point.
        private void PollHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) panelVisible = !panelVisible;
            if (director == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) director.SetProfileByIndex(0);
            if (keyboard.digit2Key.wasPressedThisFrame) director.SetProfileByIndex(1);
            if (keyboard.digit3Key.wasPressedThisFrame) director.SetProfileByIndex(2);
            if (keyboard.digit4Key.wasPressedThisFrame) director.SetProfileByIndex(3);
        }

        private void OnGUI()
        {
            if (!panelVisible || cursor == null) return;

            EnsureStyles();

            GUILayout.BeginArea(new Rect(12, 12, PanelWidth, 320), GUI.skin.box);
            DrawReadout();
            GUILayout.Space(6);
            DrawVariantPicker();
            GUILayout.Space(6);
            GUILayout.Label("F1 hides this panel.", readoutStyle);
            GUILayout.EndArea();
        }

        private void DrawReadout()
        {
            GUILayout.Label("CURSOR", headerStyle);
            GUILayout.Label($"State   {cursor.CurrentState}", readoutStyle);
            GUILayout.Label($"Hover   {cursor.CurrentHover}", readoutStyle);
            GUILayout.Label($"Visual  {CursorVisualStateMap.From(cursor.CurrentState, cursor.CurrentHover)}", readoutStyle);
            GUILayout.Label($"Tile    {cursor.GridPosition.x}, {cursor.GridPosition.y}", readoutStyle);
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
                var style = active ? activeVariantStyle : GUI.skin.button;
                if (GUILayout.Button($"{i + 1}. {profile.DisplayName}", style)) director.SetProfile(profile);
            }
        }

        private void EnsureStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            readoutStyle = new GUIStyle(GUI.skin.label);
            activeVariantStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            activeVariantStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        }
#endif
    }
}
