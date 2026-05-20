using System;
using UnityEngine;

namespace ProjectAstra.Core.Input
{
    public enum CursorDirection { Up, Down, Left, Right }

    // Delayed Auto-Shift. Hold a direction and the cursor moves once right away,
    // pauses, then repeats at a steady rate. InputManager owns one of these, feeds
    // it key presses and the frame's delta time, and re-broadcasts CursorMoveTriggered
    // as its own OnCursorMove. Deliberately free of Unity input and game code so it
    // can be unit-tested on its own.
    public sealed class DelayedAutoShift
    {
        public event Action<Vector2Int> CursorMoveTriggered;

        private const int DirectionCount = 4;

        private static readonly Vector2Int[] DirectionVectors =
        {
            Vector2Int.up,    // CursorDirection.Up
            Vector2Int.down,  // CursorDirection.Down
            Vector2Int.left,  // CursorDirection.Left
            Vector2Int.right  // CursorDirection.Right
        };

        private readonly float _initialDelay;
        private readonly float _repeatRate;
        private readonly float _fastRepeatRate;

        private readonly float[] _timers = new float[DirectionCount];
        private readonly bool[] _inInitialDelay = new bool[DirectionCount];
        private readonly bool[] _held = new bool[DirectionCount];

        public DelayedAutoShift(float initialDelay, float repeatRate, float fastRepeatRate)
        {
            _initialDelay = initialDelay;
            _repeatRate = repeatRate;
            _fastRepeatRate = fastRepeatRate;
        }

        public void Press(CursorDirection direction)
        {
            int directionIndex = (int)direction;
            _held[directionIndex] = true;
            _timers[directionIndex] = 0f;
            _inInitialDelay[directionIndex] = true;
            CursorMoveTriggered?.Invoke(DirectionVectors[directionIndex]);
        }

        public void Release(CursorDirection direction)
        {
            ClearSlot((int)direction);
        }

        public void Tick(float deltaTime, bool fastCursorHeld)
        {
            float repeatRate = fastCursorHeld ? _fastRepeatRate : _repeatRate;
            for (int directionIndex = 0; directionIndex < DirectionCount; directionIndex++)
            {
                if (_held[directionIndex])
                    Advance(directionIndex, deltaTime, repeatRate);
            }
        }

        // Carries fractional overshoot into the next tick so repeats stay on rate when frame time jitters.
        private void Advance(int directionIndex, float deltaTime, float repeatRate)
        {
            _timers[directionIndex] += deltaTime;

            bool inInitialDelay = _inInitialDelay[directionIndex];
            float threshold = inInitialDelay ? _initialDelay : repeatRate;
            if (_timers[directionIndex] < threshold) return;

            _timers[directionIndex] = inInitialDelay ? 0f : _timers[directionIndex] - repeatRate;
            _inInitialDelay[directionIndex] = false;
            CursorMoveTriggered?.Invoke(DirectionVectors[directionIndex]);
        }

        public void Reset()
        {
            for (int directionIndex = 0; directionIndex < DirectionCount; directionIndex++)
                ClearSlot(directionIndex);
        }

        private void ClearSlot(int directionIndex)
        {
            _held[directionIndex] = false;
            _timers[directionIndex] = 0f;
            _inInitialDelay[directionIndex] = false;
        }
    }
}
