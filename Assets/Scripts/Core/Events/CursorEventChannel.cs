using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Events
{
    // A pub/sub channel for everything the grid cursor does: stepping, hovering, selecting,
    // previewing a path, committing or cancelling a move, spending a unit's turn, and
    // refusing an input. Stored as a ScriptableObject so the cursor variants, their audio,
    // and any future effect can all hang off the same asset.
    //
    // The point of the granularity is that a designer can attach a sound or an effect to any
    // one of these without a code change — nothing here is wired to a specific listener.
    [CreateAssetMenu(fileName = "CursorEventChannel", menuName = "Project Astra/Core/Cursor Event Channel")]
    public class CursorEventChannel : ScriptableObject
    {
        private Action<Vector2Int> onCursorStepped;
        private Action<CursorHover, TestUnit> onHoverChanged;
        private Action<TestUnit> onUnitSelected;
        private Action<IReadOnlyList<Vector2Int>> onPathPreviewChanged;
        private Action<TestUnit, Vector2Int> onMoveConfirmed;
        private Action<TestUnit> onMoveCancelled;
        private Action onSelectionCancelled;
        private Action<TestUnit> onUnitSpentTurn;
        private Action<CursorErrorKind> onErrorFeedback;

        public void RegisterCursorStepped(Action<Vector2Int> listener) => onCursorStepped += listener;
        public void UnregisterCursorStepped(Action<Vector2Int> listener) => onCursorStepped -= listener;

        // Carries the unit as well as the kind so a listener can react to who is under the
        // cursor without repeating the tile lookup.
        public void RegisterHoverChanged(Action<CursorHover, TestUnit> listener) => onHoverChanged += listener;
        public void UnregisterHoverChanged(Action<CursorHover, TestUnit> listener) => onHoverChanged -= listener;

        public void RegisterUnitSelected(Action<TestUnit> listener) => onUnitSelected += listener;
        public void UnregisterUnitSelected(Action<TestUnit> listener) => onUnitSelected -= listener;

        // Null when the cursor is off the reachable set, i.e. there is no path to preview.
        public void RegisterPathPreviewChanged(Action<IReadOnlyList<Vector2Int>> listener) => onPathPreviewChanged += listener;
        public void UnregisterPathPreviewChanged(Action<IReadOnlyList<Vector2Int>> listener) => onPathPreviewChanged -= listener;

        public void RegisterMoveConfirmed(Action<TestUnit, Vector2Int> listener) => onMoveConfirmed += listener;
        public void UnregisterMoveConfirmed(Action<TestUnit, Vector2Int> listener) => onMoveConfirmed -= listener;

        public void RegisterMoveCancelled(Action<TestUnit> listener) => onMoveCancelled += listener;
        public void UnregisterMoveCancelled(Action<TestUnit> listener) => onMoveCancelled -= listener;

        public void RegisterSelectionCancelled(Action listener) => onSelectionCancelled += listener;
        public void UnregisterSelectionCancelled(Action listener) => onSelectionCancelled -= listener;

        public void RegisterUnitSpentTurn(Action<TestUnit> listener) => onUnitSpentTurn += listener;
        public void UnregisterUnitSpentTurn(Action<TestUnit> listener) => onUnitSpentTurn -= listener;

        public void RegisterErrorFeedback(Action<CursorErrorKind> listener) => onErrorFeedback += listener;
        public void UnregisterErrorFeedback(Action<CursorErrorKind> listener) => onErrorFeedback -= listener;

        public void RaiseCursorStepped(Vector2Int position) => onCursorStepped?.Invoke(position);
        public void RaiseHoverChanged(CursorHover hover, TestUnit unit) => onHoverChanged?.Invoke(hover, unit);
        public void RaiseUnitSelected(TestUnit unit) => onUnitSelected?.Invoke(unit);
        public void RaisePathPreviewChanged(IReadOnlyList<Vector2Int> path) => onPathPreviewChanged?.Invoke(path);
        public void RaiseMoveConfirmed(TestUnit unit, Vector2Int destination) => onMoveConfirmed?.Invoke(unit, destination);
        public void RaiseMoveCancelled(TestUnit unit) => onMoveCancelled?.Invoke(unit);
        public void RaiseSelectionCancelled() => onSelectionCancelled?.Invoke();
        public void RaiseUnitSpentTurn(TestUnit unit) => onUnitSpentTurn?.Invoke(unit);
        public void RaiseErrorFeedback(CursorErrorKind kind) => onErrorFeedback?.Invoke(kind);
    }
}
