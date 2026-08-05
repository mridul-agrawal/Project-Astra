using System;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.Core.Cursor
{
    // Everything one cursor variant is: which modules it assembles, what its pieces look like
    // in each of the five visual states, how it moves, and what it sounds like.
    //
    // A fifth variant is a duplicate of one of these with different numbers — there is no
    // per-variant code. Nothing in the visual system reads a value that isn't on this asset.
    [CreateAssetMenu(fileName = "CursorVariantProfile", menuName = "Project Astra/UI/Cursor Variant Profile")]
    public class CursorVariantProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Shown in the debug overlay's variant picker. Purely cosmetic.")]
        [SerializeField] private string displayName = "Unnamed Variant";

        [Header("Modules")]
        [Tooltip("Four open brackets at the tile corners. GBA Heirloom and Bracket Compass use these.")]
        [SerializeField] private bool useCornerBrackets = true;

        [Tooltip("Four arrows at the edge midpoints. Compass Petals and Bracket Compass use these.")]
        [SerializeField] private bool useEdgeArrows;

        [Tooltip("Corner brackets sweep along the tile edge and reshape into arrows when the cursor is over a unit. This is what makes Morphing Compass morph — leave it off for the static variants.")]
        [SerializeField] private bool useMorph;

        [Tooltip("Hide arrows pointing at tiles the cursor can't actually reach — off the map edge in Idle, outside the movement range once a unit is selected. Recomputed every step.")]
        [SerializeField] private bool hideInvalidDirections = true;

        [Header("Visual states — each carries its own shape and art")]
        [SerializeField] private CursorStateVisual idle = CursorStateVisual.DefaultIdle;
        [SerializeField] private CursorStateVisual selectable = CursorStateVisual.DefaultSelectable;
        [SerializeField] private CursorStateVisual selected = CursorStateVisual.DefaultSelected;
        [SerializeField] private CursorStateVisual acted = CursorStateVisual.DefaultActed;
        [SerializeField] private CursorStateVisual enemy = CursorStateVisual.DefaultEnemy;

        [Header("Motion")]
        [Tooltip("Seconds the cursor takes to slide between tiles. Clamped down to the held-repeat interval at runtime so steps can never queue up. Simulator default was 0.08.")]
        [Range(0f, 0.3f)][SerializeField] private float stepSlide = 0.08f;

        [Tooltip("Seconds for pieces to settle into a new state's pose — the hover emphasis. Simulator default was 0.10.")]
        [Range(0f, 0.5f)][SerializeField] private float stateBlend = 0.1f;

        [Tooltip("Seconds for a morph sweep from corners to edges and back. Simulator default was 0.09-0.12. Below about 0.06 the sweep stops reading as motion.")]
        [Range(0.02f, 0.4f)][SerializeField] private float morphDuration = 0.1f;

        [Tooltip("Which way the pieces sweep during a morph. All four always travel together — mixed directions read as noise, not as one object.")]
        [SerializeField] private MorphRotation morphRotation = MorphRotation.CounterClockwise;

        [Tooltip("How far the cursor jolts when an input is refused, in world units. 0.0625 is the 2-pixel equivalent.")]
        [Range(0f, 0.25f)][SerializeField] private float errorShakeDistance = 0.0625f;

        [Tooltip("Seconds the error jolt lasts. Simulator default was 0.09.")]
        [Range(0.02f, 0.4f)][SerializeField] private float errorShakeDuration = 0.09f;

        [Header("Feel — held repeat")]
        [Tooltip("Seconds a direction must be held before the cursor starts repeating. The game currently ships 0.4; the simulator preferred 0.25.")]
        [Range(0.05f, 1f)][SerializeField] private float heldRepeatDelay = 0.4f;

        [Tooltip("Seconds between repeated steps once repeating has started. The game currently ships 0.1; the simulator preferred 0.06.")]
        [Range(0.02f, 0.5f)][SerializeField] private float heldRepeatStep = 0.1f;

        [Header("Feel — range reveal")]
        [Tooltip("Seconds for the movement range to flood outward from the unit in BFS ring order. 0 shows it all at once. Simulator capped this at 0.2.")]
        [Range(0f, 0.6f)][SerializeField] private float rangeFloodDuration = 0.2f;

        [Header("Audio — leave as None for silence")]
        [SerializeField] private SoundId steppedSound = SoundId.None;
        [SerializeField] private SoundId hoverSelectableSound = SoundId.None;
        [SerializeField] private SoundId hoverEnemySound = SoundId.None;
        [SerializeField] private SoundId unitSelectedSound = SoundId.None;
        [SerializeField] private SoundId moveConfirmedSound = SoundId.None;
        [SerializeField] private SoundId moveCancelledSound = SoundId.None;
        [SerializeField] private SoundId selectionCancelledSound = SoundId.None;
        [SerializeField] private SoundId unitSpentTurnSound = SoundId.None;
        [SerializeField] private SoundId errorSound = SoundId.None;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public bool UseCornerBrackets => useCornerBrackets;
        public bool UseEdgeArrows => useEdgeArrows;
        public bool UseMorph => useMorph;
        public bool HideInvalidDirections => hideInvalidDirections;

        public float StepSlide => stepSlide;
        public float StateBlend => stateBlend;
        public float MorphDuration => morphDuration;
        public int MorphDirection => morphRotation == MorphRotation.CounterClockwise ? 1 : -1;
        public float ErrorShakeDistance => errorShakeDistance;
        public float ErrorShakeDuration => errorShakeDuration;
        public float HeldRepeatDelay => heldRepeatDelay;
        public float HeldRepeatStep => heldRepeatStep;
        public float RangeFloodDuration => rangeFloodDuration;

        public CursorStateVisual VisualFor(CursorVisualState state) => state switch
        {
            CursorVisualState.Selectable => selectable,
            CursorVisualState.Selected => selected,
            CursorVisualState.Acted => acted,
            CursorVisualState.Enemy => enemy,
            _ => idle,
        };

        public SoundId SoundForHover(CursorHover hover) => hover switch
        {
            CursorHover.ReadyAlly => hoverSelectableSound,
            CursorHover.Enemy => hoverEnemySound,
            _ => SoundId.None,
        };

        // A profile authored before the shape settings existed deserialises them as zeros,
        // which would draw nothing at all. Arm length has a 0.1 floor on its slider, so a zero
        // can only mean "never set" — fill those in rather than letting the cursor vanish.
        private void OnValidate()
        {
            MigrateShapeIfUnset(ref idle);
            MigrateShapeIfUnset(ref selectable);
            MigrateShapeIfUnset(ref selected);
            MigrateShapeIfUnset(ref acted);
            MigrateShapeIfUnset(ref enemy);
        }

        private static void MigrateShapeIfUnset(ref CursorStateVisual visual)
        {
            if (visual.shape.armLength < 0.1f) visual.shape = CursorShape.Default;
        }

        public SoundId SteppedSound => steppedSound;
        public SoundId UnitSelectedSound => unitSelectedSound;
        public SoundId MoveConfirmedSound => moveConfirmedSound;
        public SoundId MoveCancelledSound => moveCancelledSound;
        public SoundId SelectionCancelledSound => selectionCancelledSound;
        public SoundId UnitSpentTurnSound => unitSpentTurnSound;
        public SoundId ErrorSound => errorSound;
    }

    public enum MorphRotation
    {
        CounterClockwise,
        Clockwise
    }

    // One state's worth of look. States must differ by shape or motion as well as colour, so
    // inset and idle amplitude matter as much as the tint — a colourblind player reads the
    // brackets tightening, not the hue shifting.
    [Serializable]
    public struct CursorStateVisual
    {
        [Tooltip("Colour multiplied onto the piece sprites. Keep the sprites white so this reads true.")]
        public Color tint;

        [Tooltip("The silhouette this state draws. Leave the sprite slots below empty and tune these instead — the shapes redraw live while the game is running.")]
        public CursorShape shape;

        [Tooltip("Optional. A hand-authored bracket for this state, replacing the shape settings above. Authored as a top-right corner; the cursor rotates it to the other three.")]
        public Sprite bracketSprite;

        [Tooltip("Optional. A hand-authored arrow for this state. Authored pointing up.")]
        public Sprite arrowSprite;

        [Tooltip("Distance from the tile centre to each piece, in world units. One tile is 1.0, so 0.5 sits a piece exactly on the tile edge. Smaller values pull the pieces in toward the unit.")]
        [Range(0f, 0.9f)] public float inset;

        [Tooltip("Size multiplier on each piece.")]
        [Range(0.1f, 3f)] public float pieceScale;

        [Tooltip("How far pieces drift outward and back on the idle breath, in world units. Set to 0 to stop the motion dead — that is what makes the Acted state read as inert.")]
        [Range(0f, 0.2f)] public float breathAmplitude;

        [Tooltip("Seconds for one full breath in and out. All eight pieces share this rhythm so a composite variant reads as one object.")]
        [Range(0.2f, 6f)] public float breathPeriod;

        [Tooltip("Flip the edge arrows to point inward at the unit instead of outward. Compass Petals uses this to say 'this one can act'.")]
        public bool arrowsPointInward;

        public static CursorStateVisual DefaultIdle => new()
        {
            tint = new Color(1f, 0.98f, 0.9f, 0.9f),
            shape = CursorShape.Default,
            inset = 0.42f,
            pieceScale = 1f,
            breathAmplitude = 0.045f,
            breathPeriod = 2.2f,
            arrowsPointInward = false,
        };

        public static CursorStateVisual DefaultSelectable => new()
        {
            tint = new Color(1f, 0.85f, 0.35f, 1f),
            shape = CursorShape.Default,
            inset = 0.34f,
            pieceScale = 1.05f,
            breathAmplitude = 0.03f,
            breathPeriod = 1.4f,
            arrowsPointInward = true,
        };

        public static CursorStateVisual DefaultSelected => new()
        {
            tint = new Color(0.45f, 0.85f, 1f, 1f),
            shape = CursorShape.Default,
            inset = 0.3f,
            pieceScale = 1.05f,
            breathAmplitude = 0f,
            breathPeriod = 1.4f,
            arrowsPointInward = false,
        };

        public static CursorStateVisual DefaultActed => new()
        {
            tint = new Color(0.5f, 0.5f, 0.55f, 0.75f),
            shape = CursorShape.Default,
            inset = 0.42f,
            pieceScale = 0.95f,
            breathAmplitude = 0f,
            breathPeriod = 2.2f,
            arrowsPointInward = false,
        };

        public static CursorStateVisual DefaultEnemy => new()
        {
            tint = new Color(1f, 0.3f, 0.28f, 1f),
            shape = CursorShape.Default,
            inset = 0.44f,
            pieceScale = 1.05f,
            breathAmplitude = 0.05f,
            breathPeriod = 0.9f,
            arrowsPointInward = false,
        };
    }
}
