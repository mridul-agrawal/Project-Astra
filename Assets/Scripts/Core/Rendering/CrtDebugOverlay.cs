using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAstra.Core.Rendering
{
    // Live CRT tuning panel, on F2. Edits the profile the current quality setting selects, so
    // what you drag is always what you can see.
    //
    // Sits on its own scene root and draws with IMGUI, so it needs no Canvas and is not subject
    // to the project's disabled-by-default UI rule. F2 is unbound in the input asset and the
    // context filter only touches actions inside the Gameplay map, so it never collides with
    // gameplay input. Editor and development builds only.
    public class CrtDebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Tooltip("Whether the panel is showing when the map loads. Off by default; F2 brings it up.")]
        [SerializeField] private bool panelVisible;

        private const int PanelWidth = 330;

        private GUIStyle headerStyle;
        private GUIStyle readoutStyle;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame) panelVisible = !panelVisible;
        }

        private void OnGUI()
        {
            if (!panelVisible) return;

            var binder = CrtProfileBinder.Current;
            if (binder == null) return;

            EnsureStyles();

            GUILayout.BeginArea(new Rect(Screen.width - PanelWidth - 12, 12, PanelWidth, 620), GUI.skin.box);
            DrawQuality(binder);

            var profile = binder.ActiveProfile;
            if (binder.Quality == CrtQuality.Off || profile == null)
            {
                GUILayout.Label("Filter is off — nothing to tune.", readoutStyle);
            }
            else
            {
                GUILayout.Space(6);
                DrawProfileEditor(profile);
            }

            GUILayout.Space(6);
            GUILayout.Label("F2 hides this panel. Changes are saved to the profile asset.", readoutStyle);
            GUILayout.EndArea();
        }

        private void DrawQuality(CrtProfileBinder binder)
        {
            GUILayout.Label("CRT FILTER", headerStyle);

            var quality = binder.Quality;
            GUILayout.BeginHorizontal();
            if (Toggle("Off", quality == CrtQuality.Off)) binder.Quality = CrtQuality.Off;
            if (Toggle("Subtle", quality == CrtQuality.Subtle)) binder.Quality = CrtQuality.Subtle;
            if (Toggle("Full", quality == CrtQuality.Full)) binder.Quality = CrtQuality.Full;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Editing: {(binder.ActiveProfile != null ? binder.ActiveProfile.name : "none")}",
                readoutStyle);
        }

        // Writes straight into the ScriptableObject via SerializedObject so edits persist to the
        // asset rather than evaporating when play mode ends.
        private static void DrawProfileEditor(CrtProfile profile)
        {
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(profile);

            Slider(so, "scanlineStrength", "Scanline", 0f, 1f);
            Slider(so, "beamWidth", "Beam width", 0.15f, 1f);
            Slider(so, "horizontalBleed", "H. bleed", 0f, 2f);
            Slider(so, "gamma", "Gamma", 1f, 2.5f);
            Slider(so, "gain", "Gain", 0.5f, 2.5f);
            Slider(so, "bloomAmount", "Bloom", 0f, 2f);
            Slider(so, "halationStrength", "Halation", 0f, 1f);
            Slider(so, "halationRadius", "Halo radius", 0f, 0.03f);
            Slider(so, "halationThreshold", "Halo thresh", 0f, 1f);
            Slider(so, "maskStrength", "Mask", 0f, 1f);
            Slider(so, "maskPitch", "Mask pitch", 2f, 12f);
            Slider(so, "curvature", "Curvature", 0f, 0.3f);
            Slider(so, "vignetteStrength", "Vignette", 0f, 1f);

            so.ApplyModifiedPropertiesWithoutUndo();
#else
            GUILayout.Label("Profile editing is editor-only.");
#endif
        }

#if UNITY_EDITOR
        private static void Slider(UnityEditor.SerializedObject so, string field, string label,
            float min, float max)
        {
            var property = so.FindProperty(field);
            if (property == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}", GUILayout.Width(90));
            property.floatValue = GUILayout.HorizontalSlider(property.floatValue, min, max,
                GUILayout.Width(150));
            GUILayout.Label(property.floatValue.ToString("0.###"), GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }
#endif

        private static bool Toggle(string label, bool active)
        {
            var style = active ? GUI.skin.box : GUI.skin.button;
            return GUILayout.Button(label, style) && !active;
        }

        private void EnsureStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            readoutStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
        }
#endif
    }
}
