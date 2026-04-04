using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(fileName = "GameStateChanged", menuName = "Project Astra/Core/Game State Event Channel")]
    /// <summary>ScriptableObject-based event bus for broadcasting game state transitions to decoupled listeners.</summary>
    public class GameStateEventChannel : ScriptableObject
    {
        public struct StateChangeArgs
        {
            public GameState PreviousState;
            public GameState NewState;
        }

        private Action<StateChangeArgs> _onStateChanged;

        public void Register(Action<StateChangeArgs> listener) => _onStateChanged += listener;
        public void Unregister(Action<StateChangeArgs> listener) => _onStateChanged -= listener;

        public void Raise(StateChangeArgs args) => _onStateChanged?.Invoke(args);
    }
}
