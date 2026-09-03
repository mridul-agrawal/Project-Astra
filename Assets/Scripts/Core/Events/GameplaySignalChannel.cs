using System;
using UnityEngine;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Events
{
    // ScriptableObject event bus for things that happened in the world. Reached only through
    // EventService; whoever it happened to raises one, and the quest system is what listens.
    [CreateAssetMenu(fileName = "GameplaySignalChannel",
        menuName = "Project Astra/Core/Gameplay Signal Channel")]
    public class GameplaySignalChannel : ScriptableObject
    {
        private Action<GameplaySignal> onSignal;

        public void Register(Action<GameplaySignal> listener) => onSignal += listener;
        public void Unregister(Action<GameplaySignal> listener) => onSignal -= listener;

        public void Raise(GameplaySignal signal) => onSignal?.Invoke(signal);
    }
}
