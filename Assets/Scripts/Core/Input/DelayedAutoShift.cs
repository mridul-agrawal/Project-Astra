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

        private float initialDelay;
        private float repeatRate;
        private float fastRepeatRate;

        private readonly float[] timers = new float[DirectionCount];
        private readonly bool[] inInitialDelay = new bool[DirectionCount];
        private readonly bool[] held = new bool[DirectionCount];

        public DelayedAutoShift(float initialDelay, float repeatRate, float fastRepeatRate)
        {
            this.initialDelay = initialDelay;
            this.repeatRate = repeatRate;
            this.fastRepeatRate = fastRepeatRate;
        }

        // Rates are re-applied every frame from the active cursor profile, so a designer
        // dragging the slider in play mode feels the change immediately. In-flight timers are
        // left alone — retiming a held direction mid-repeat would stutter it.
        public void SetTimings(float initialDelay, float repeatRate, float fastRepeatRate)
        {
            this.initialDelay = initialDelay;
            this.repeatRate = repeatRate;
            this.fastRepeatRate = fastRepeatRate;
        }

        public void Press(CursorDirection direction)
        {
            int directionIndex = (int)direction;
            held[directionIndex] = true;
            timers[directionIndex] = 0f;
            inInitialDelay[directionIndex] = true;
            CursorMoveTriggered?.Invoke(DirectionVectors[directionIndex]);
        }

        public void Release(CursorDirection direction)
        {
            ClearSlot((int)direction);
        }

        public void Tick(float deltaTime, bool fastCursorHeld)
        {
            float repeatRate = fastCursorHeld ? fastRepeatRate : this.repeatRate;
            for (int directionIndex = 0; directionIndex < DirectionCount; directionIndex++)
            {
                if (held[directionIndex])
                    Advance(directionIndex, deltaTime, repeatRate);
            }
        }

        // Carries fractional overshoot into the next tick so repeats stay on rate when frame time jitters.
        private void Advance(int directionIndex, float deltaTime, float repeatRate)
        {
            timers[directionIndex] += deltaTime;

            bool inInitialDelay = this.inInitialDelay[directionIndex];
            float threshold = inInitialDelay ? initialDelay : repeatRate;
            if (timers[directionIndex] < threshold) return;

            timers[directionIndex] = inInitialDelay ? 0f : timers[directionIndex] - repeatRate;
            this.inInitialDelay[directionIndex] = false;
            CursorMoveTriggered?.Invoke(DirectionVectors[directionIndex]);
        }

        public void Reset()
        {
            for (int directionIndex = 0; directionIndex < DirectionCount; directionIndex++)
                ClearSlot(directionIndex);
        }

        private void ClearSlot(int directionIndex)
        {
            held[directionIndex] = false;
            timers[directionIndex] = 0f;
            inInitialDelay[directionIndex] = false;
        }
    }
}
