using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Assembles a cursor variant out of the composable modules and drives it every frame.
    // Lives on a child of GridCursor so it can lag the logical position — the root snaps
    // instantly (logic and camera tracking stay exact) while the art slides in behind it.
    //
    // The whole variant system funnels through here: swap the profile and the cursor
    // re-skins live, mid-state, without the FSM noticing.
    [DefaultExecutionOrder(50)]
    public class CursorVisualDirector : MonoBehaviour
    {
        [Header("Variants")]
        [Tooltip("Every variant available to the debug overlay's picker and the 1-4 hotkeys. The first entry is what the cursor starts on.")]
        [SerializeField] private List<CursorVariantProfile> profiles = new();

        [Header("Wiring")]
        [Tooltip("Left empty, the director finds the GridCursor on its parent.")]
        [SerializeField] private GridCursor cursor;

        private readonly CursorPose[] targets = new CursorPose[CursorSlotGeometry.SlotCount];
        private readonly bool[] validDirections = new bool[DirectionalHintModule.DirectionCount];
        private readonly CursorShapeRenderer shapes = new();
        private CursorPiece[] pieces;

        private CursorVariantProfile activeProfile;
        private CursorVisualState visualState = CursorVisualState.Idle;
        private bool morphedToEdges;

        private float breathPhase;
        private Vector3 slideOffset;
        private Vector3 slideVelocity;
        private Vector3 lastRootPosition;

        private float shakeElapsed;
        private Vector3 shakeAxis;

        // Supplied by whoever knows the current movement range, so the arrows can be limited
        // to steps the player can actually take. Null means "anything on the map".
        private Func<Vector2Int, bool> reachabilityTest;

        // The selection flow and the debug overlay are plain C# and MonoBehaviours that don't
        // own a reference to the director, so it publishes itself the same way
        // UnitVisualSettingsRef and TurnManager do.
        public static CursorVisualDirector Current { get; private set; }

        public CursorVariantProfile ActiveProfile => activeProfile;
        public IReadOnlyList<CursorVariantProfile> Profiles => profiles;

        // Zero when no profile is loaded, which makes the range appear all at once.
        public static float RangeFloodDuration =>
            Current != null && Current.activeProfile != null ? Current.activeProfile.RangeFloodDuration : 0f;

        private void Awake()
        {
            Current = this;
            if (cursor == null) cursor = GetComponentInParent<GridCursor>();
            BuildPieces();
            activeProfile = profiles.Count > 0 ? profiles[0] : null;
            lastRootPosition = transform.parent != null ? transform.parent.position : transform.position;
        }

        private void OnEnable()
        {
            if (cursor != null)
            {
                cursor.StateMachine.StateChanged += HandleStateChanged;
                cursor.StateMachine.HoverChanged += HandleHoverChanged;
            }
            SubscribeToEvents();
            RefreshSprites();
            RebuildTargets(0f);
        }

        private void OnDisable()
        {
            if (cursor != null)
            {
                cursor.StateMachine.StateChanged -= HandleStateChanged;
                cursor.StateMachine.HoverChanged -= HandleHoverChanged;
            }
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            shapes.Dispose();
            if (pieces == null) return;
            foreach (var piece in pieces) piece.Destroy();
        }

        // --- Public API ---

        public void SetProfile(CursorVariantProfile profile)
        {
            if (profile == null || profile == activeProfile) return;
            activeProfile = profile;
            RefreshSprites();
            RebuildTargets(profile.StateBlend);
        }

        public void SetProfileByIndex(int index)
        {
            if (index < 0 || index >= profiles.Count) return;
            SetProfile(profiles[index]);
        }

        // The selection flow hands over its reachable set while a unit is picked up, and
        // clears it on deselect.
        public void SetReachabilityTest(Func<Vector2Int, bool> test)
        {
            reachabilityTest = test;
            RebuildTargets(activeProfile != null ? activeProfile.StateBlend : 0f);
        }

        // --- Frame loop ---

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            TrackRootMovement(dt);
            TickShake(dt);
            TickPieces(dt);
        }

        // The root has already snapped to the new tile by the time we run. Take the jump as a
        // negative local offset and ease it away, so the art appears to slide across.
        private void TrackRootMovement(float dt)
        {
            Vector3 root = transform.parent != null ? transform.parent.position : transform.position;

            if (root != lastRootPosition)
            {
                slideOffset += lastRootPosition - root;
                lastRootPosition = root;
            }

            float slide = SlideDuration();
            if (slide <= 0f)
            {
                slideOffset = Vector3.zero;
                slideVelocity = Vector3.zero;
                return;
            }

            slideOffset = Vector3.SmoothDamp(slideOffset, Vector3.zero, ref slideVelocity, slide, Mathf.Infinity, dt);
        }

        // Clamped to the repeat interval so a held direction can never queue slides up behind
        // each other — the cursor always arrives before the next step is allowed.
        private float SlideDuration()
        {
            if (activeProfile == null) return 0f;
            return Mathf.Min(activeProfile.StepSlide, activeProfile.HeldRepeatStep);
        }

        private void TickShake(float dt)
        {
            if (shakeElapsed <= 0f) return;
            shakeElapsed = Mathf.Max(0f, shakeElapsed - dt);
        }

        private void TickPieces(float dt)
        {
            if (pieces == null || activeProfile == null) return;

            CursorStateVisual visual = activeProfile.VisualFor(visualState);

            // Cheap when nothing changed, so this is also what picks up a shape slider being
            // dragged in the Inspector mid-play.
            shapes.Refresh(visual.shape);
            RefreshSprites();

            AdvanceBreath(dt, visual);

            float breath = BreathValue(visual);
            Vector3 shake = CurrentShakeOffset();

            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].Tick(dt);

                Vector2 outward = pieces[i].Current.offset.normalized;
                Vector2 breathOffset = outward * (breath * visual.breathAmplitude);
                pieces[i].Apply(breathOffset + (Vector2)slideOffset + (Vector2)shake, 1f);
            }
        }

        // One shared phase for all eight pieces — a composite variant only reads as a single
        // object if every element breathes on the same beat.
        private void AdvanceBreath(float dt, in CursorStateVisual visual)
        {
            if (visual.breathAmplitude <= 0f || visual.breathPeriod <= 0f) return;
            breathPhase += dt / visual.breathPeriod;
            if (breathPhase > 1f) breathPhase -= 1f;
        }

        private float BreathValue(in CursorStateVisual visual) =>
            visual.breathAmplitude <= 0f ? 0f : Mathf.Sin(breathPhase * 2f * Mathf.PI);

        private Vector3 CurrentShakeOffset()
        {
            if (shakeElapsed <= 0f || activeProfile == null) return Vector3.zero;

            float remaining = shakeElapsed / activeProfile.ErrorShakeDuration;
            float wave = Mathf.Sin(remaining * Mathf.PI * 6f) * remaining;
            return shakeAxis * (wave * activeProfile.ErrorShakeDistance);
        }

        // --- Target assembly ---

        private void RebuildTargets(float blend)
        {
            if (pieces == null || activeProfile == null) return;

            CursorStateVisual visual = activeProfile.VisualFor(visualState);
            UpdateValidDirections();

            bool morph = activeProfile.UseMorph;
            morphedToEdges = morph && visualState != CursorVisualState.Idle;

            if (morph)
                MorphDriver.WriteTargets(targets, visual, morphedToEdges, DirectionFilter());
            else
            {
                CornerBracketModule.WriteTargets(targets, visual, activeProfile.UseCornerBrackets);
                EdgeArrowModule.WriteTargets(targets, visual, activeProfile.UseEdgeArrows, DirectionFilter());
            }

            float duration = morph ? activeProfile.MorphDuration : blend;
            int arc = morph ? activeProfile.MorphDirection : 0;

            for (int i = 0; i < pieces.Length; i++)
            {
                bool sweeping = morph && CursorSlotGeometry.IsCorner(i);
                pieces[i].MorphTo(targets[i], duration, sweeping ? arc : 0);
            }

            RefreshSprites();
        }

        private bool[] DirectionFilter() =>
            activeProfile != null && activeProfile.HideInvalidDirections ? validDirections : null;

        private void UpdateValidDirections()
        {
            if (activeProfile == null || !activeProfile.HideInvalidDirections)
            {
                DirectionalHintModule.SetAll(validDirections, true);
                return;
            }

            if (cursor == null)
            {
                DirectionalHintModule.SetAll(validDirections, true);
                return;
            }

            DirectionalHintModule.Compute(validDirections, cursor.GridPosition, reachabilityTest);
        }

        // --- Pieces ---

        private void BuildPieces()
        {
            pieces = new CursorPiece[CursorSlotGeometry.SlotCount];
            for (int i = 0; i < pieces.Length; i++)
                pieces[i] = new CursorPiece(transform, $"CursorPiece_{(CursorSlot)i}", i);
        }

        // Per-state authored art wins over the parametric shape; empty slots fall back to it.
        private void RefreshSprites()
        {
            if (pieces == null || activeProfile == null) return;

            CursorStateVisual visual = activeProfile.VisualFor(visualState);
            Sprite bracket = visual.bracketSprite != null ? visual.bracketSprite : shapes.BracketSprite;
            Sprite arrow = visual.arrowSprite != null ? visual.arrowSprite : shapes.ArrowSprite;

            for (int i = 0; i < pieces.Length; i++)
            {
                bool wantsArrow = !CursorSlotGeometry.IsCorner(i)
                    || (activeProfile.UseMorph && MorphDriver.ShouldShowArrowSprite(morphedToEdges));

                pieces[i].SetSprite(wantsArrow ? arrow : bracket);
            }
        }

        // --- Event handling ---

        private void HandleStateChanged(CursorState previous, CursorState next)
        {
            visualState = CursorVisualStateMap.From(next, cursor.CurrentHover);
            RebuildTargets(activeProfile != null ? activeProfile.StateBlend : 0f);
        }

        private void HandleHoverChanged(CursorHover hover)
        {
            visualState = CursorVisualStateMap.From(cursor.CurrentState, hover);
            RebuildTargets(activeProfile != null ? activeProfile.StateBlend : 0f);
            PlayIfSet(activeProfile != null ? activeProfile.SoundForHover(hover) : SoundId.None);
        }

        private void SubscribeToEvents()
        {
            var events = EventService.Instance;
            if (events == null) return;

            events.SubscribeCursorStepped(HandleStepped);
            events.SubscribeUnitSelected(HandleUnitSelected);
            events.SubscribeMoveConfirmed(HandleMoveConfirmed);
            events.SubscribeMoveCancelled(HandleMoveCancelled);
            events.SubscribeSelectionCancelled(HandleSelectionCancelled);
            events.SubscribeUnitSpentTurn(HandleUnitSpentTurn);
            events.SubscribeCursorError(HandleError);
        }

        private void UnsubscribeFromEvents()
        {
            var events = EventService.Instance;
            if (events == null) return;

            events.UnsubscribeCursorStepped(HandleStepped);
            events.UnsubscribeUnitSelected(HandleUnitSelected);
            events.UnsubscribeMoveConfirmed(HandleMoveConfirmed);
            events.UnsubscribeMoveCancelled(HandleMoveCancelled);
            events.UnsubscribeSelectionCancelled(HandleSelectionCancelled);
            events.UnsubscribeUnitSpentTurn(HandleUnitSpentTurn);
            events.UnsubscribeCursorError(HandleError);
        }

        private void HandleStepped(Vector2Int position)
        {
            RebuildTargets(activeProfile != null ? activeProfile.StateBlend : 0f);
            PlayIfSet(activeProfile != null ? activeProfile.SteppedSound : SoundId.None);
        }

        private void HandleUnitSelected(TestUnit unit) =>
            PlayIfSet(activeProfile != null ? activeProfile.UnitSelectedSound : SoundId.None);

        private void HandleMoveConfirmed(TestUnit unit, Vector2Int destination) =>
            PlayIfSet(activeProfile != null ? activeProfile.MoveConfirmedSound : SoundId.None);

        private void HandleMoveCancelled(TestUnit unit) =>
            PlayIfSet(activeProfile != null ? activeProfile.MoveCancelledSound : SoundId.None);

        private void HandleSelectionCancelled() =>
            PlayIfSet(activeProfile != null ? activeProfile.SelectionCancelledSound : SoundId.None);

        private void HandleUnitSpentTurn(TestUnit unit) =>
            PlayIfSet(activeProfile != null ? activeProfile.UnitSpentTurnSound : SoundId.None);

        private void HandleError(CursorErrorKind kind)
        {
            if (activeProfile == null) return;
            shakeElapsed = activeProfile.ErrorShakeDuration;
            shakeAxis = Vector3.right;
            PlayIfSet(activeProfile.ErrorSound);
        }

        private static void PlayIfSet(SoundId id)
        {
            if (id == SoundId.None) return;
            AudioManager.Instance?.Play(id);
        }
    }
}
