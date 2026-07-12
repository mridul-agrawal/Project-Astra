using System;
using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.Events
{
    // ScriptableObject event bus for unit deaths. Mirrors the other channels —
    // Register / Unregister / Raise. Reached only through EventService; listeners
    // include DeathRegistry, BattleVictoryWatcher, LordDeathWatcher, future analytics.
    [CreateAssetMenu(fileName = "UnitDeathEventChannel",
        menuName = "Project Astra/Core/Unit Death Event Channel")]
    public class UnitDeathEventChannel : ScriptableObject
    {
        private Action<UnitDeathEventArgs> onUnitDied;

        public void Register(Action<UnitDeathEventArgs> listener) => onUnitDied += listener;
        public void Unregister(Action<UnitDeathEventArgs> listener) => onUnitDied -= listener;

        public void Raise(UnitDeathEventArgs args) => onUnitDied?.Invoke(args);
    }
}
