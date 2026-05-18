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

        private static readonly Vector2Int[] Vectors =
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
            int dir = (int)direction;
            _held[dir] = true;
            _timers[dir] = 0f;
            _inInitialDelay[dir] = true;
            CursorMoveTriggered?.Invoke(Vectors[dir]);
        }

        public void Release(CursorDirection direction)
        {
            int dir = (int)direction;
            _held[dir] = false;
            _timers[dir] = 0f;
            _inInitialDelay[dir] = false;
        }

        public void Tick(float deltaTime, bool fastCursorHeld)
        {
            float repeatRate = fastCursorHeld ? _fastRepeatRate : _repeatRate;
            for (int dir = 0; dir < DirectionCount; dir++)
            {
                if (_held[dir])
                    Advance(dir, deltaTime, repeatRate);
            }
        }

        public void Reset()
        {
            for (int dir = 0; dir < DirectionCount; dir++)
            {
                _held[dir] = false;
                _timers[dir] = 0f;
                _inInitialDelay[dir] = false;
            }
        }

        // Carries fractional overshoot into the next tick so repeats stay on rate when frame time jitters.
        private void Advance(int dir, float deltaTime, float repeatRate)
        {
            _timers[dir] += deltaTime;

            bool inInitialDelay = _inInitialDelay[dir];
            float threshold = inInitialDelay ? _initialDelay : repeatRate;
            if (_timers[dir] < threshold) return;

            _timers[dir] = inInitialDelay ? 0f : _timers[dir] - repeatRate;
            _inInitialDelay[dir] = false;
            CursorMoveTriggered?.Invoke(Vectors[dir]);
        }
    }
}
