using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // Validated state machine for the grid cursor. GridCursor owns one and routes every
    // mode change through it, so the visual layer has a single place to listen and illegal
    // transitions get rejected and logged instead of silently corrupting state.
    //
    // Deliberately plain C# with a code-side transition set rather than a ScriptableObject:
    // these states are a code contract, and a designer-editable asset would only invite the
    // regeneration hazard that bit the game-state transition table.
    public class CursorStateMachine
    {
        private static readonly HashSet<(CursorState, CursorState)> LegalTransitions = BuildLegalTransitions();

        private CursorState currentState = CursorState.Free;
        private CursorHover currentHover = CursorHover.Empty;
        private CursorState stateBeforeSuspend = CursorState.Free;

        public CursorState CurrentState => currentState;
        public CursorHover CurrentHover => currentHover;

        // Fires with (previous, next) so a variant can animate the specific edge, not just
        // the destination — Selected→Free reads differently from ActionMenu→Free.
        public event Action<CursorState, CursorState> StateChanged;
        public event Action<CursorHover> HoverChanged;

        public bool TryTransition(CursorState next)
        {
            if (next == currentState) return true;

            if (!LegalTransitions.Contains((currentState, next)))
            {
                Debug.LogWarning($"[CursorStateMachine] Illegal transition: {currentState} -> {next}. Rejected.");
                return false;
            }

            if (next == CursorState.Suspended)
                stateBeforeSuspend = currentState;

            CursorState previous = currentState;
            currentState = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }

        // Returning to the battle map restores whatever the player was doing before the
        // detour into combat, dialogue or the unit-info screen, instead of dropping the
        // selection on the floor.
        public bool RestoreFromSuspend()
        {
            if (currentState != CursorState.Suspended) return false;
            return TryTransition(stateBeforeSuspend);
        }

        public void SetHover(CursorHover hover)
        {
            if (hover == currentHover) return;
            currentHover = hover;
            HoverChanged?.Invoke(hover);
        }

        // Hover only means anything while browsing; anything else forces Empty so a stale
        // ReadyAlly cue can't survive into the selected or targeting states.
        public void ResetHoverIfNotFree()
        {
            if (currentState != CursorState.Free)
                SetHover(CursorHover.Empty);
        }

        internal static bool IsLegal(CursorState from, CursorState to) =>
            from == to || LegalTransitions.Contains((from, to));

        private static HashSet<(CursorState, CursorState)> BuildLegalTransitions()
        {
            var t = new HashSet<(CursorState, CursorState)>
            {
                (CursorState.Free, CursorState.Selected),
                (CursorState.Selected, CursorState.Moving),
                (CursorState.Selected, CursorState.Free),
                (CursorState.Moving, CursorState.ActionMenu),
                (CursorState.ActionMenu, CursorState.Free),
                (CursorState.ActionMenu, CursorState.Selected),
                (CursorState.ActionMenu, CursorState.Targeting),
                (CursorState.Targeting, CursorState.ActionMenu),
                (CursorState.Targeting, CursorState.Selected),
                (CursorState.Targeting, CursorState.Free),

                // Canto hands a unit straight back to Selected after it acts, without a move.
                (CursorState.Moving, CursorState.Selected),
                (CursorState.Moving, CursorState.Free),
            };

            // Any state can be suspended (leaving the battle map) and resumed.
            foreach (CursorState state in Enum.GetValues(typeof(CursorState)))
            {
                if (state == CursorState.Suspended) continue;
                t.Add((state, CursorState.Suspended));
                t.Add((CursorState.Suspended, state));
            }

            return t;
        }
    }
}
