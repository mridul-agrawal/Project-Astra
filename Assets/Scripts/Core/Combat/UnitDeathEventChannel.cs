using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// How a unit died. Only Combat is wired in gameplay today (UM-01 scope).
    /// Poison / Environmental / Other ship in the enum so the payload struct is
    /// stable when those damage paths (future USE-04 etc.) land.
    /// </summary>
    public enum CauseOfDeath { Combat, Poison, Environmental, Other }

    /// <summary>
    /// War's Ledger faction bucket. This is NOT the gameplay-layer Faction —
    /// it's the registry classification used for the ledger's three columns:
    /// named player losses, named enemy commanders, generic enemies (counted
    /// but not itemised), and civilians (routed through the civilian thread).
    /// </summary>
    public enum DeathFaction { Player, EnemyNamed, EnemyGeneric, Civilian }

    [Serializable]
    public struct UnitSupportSnapshot
    {
        public string partnerId;
        public BondStage rank;
    }

    /// <summary>
    /// Payload fired from the death hook at GridCursor.ApplyCombatResult.
    /// Everything the DeathRegistry + War's Ledger need is bundled here so
    /// the registry can survive runtime-scoped today and slot into a future
    /// save system without the event surface changing.
    /// </summary>
    [Serializable]
    public struct UnitDeathEventArgs
    {
        public string unitId;
        public string unitName;
        public string className;
        public DeathFaction faction;

        public string killerUnitId;          // null if no identifiable killer
        public CauseOfDeath causeOfDeath;

        public int chapterNumber;
        public Vector2Int tileCoordinates;

        // Frozen at time of death — surviving partners can reference this for
        // epitaph lookup. Empty list if the unit had no active supports.
        public List<UnitSupportSnapshot> activeSupports;

        // One-line identity authored on UnitDefinition for named enemies; empty
        // for player / generic / civilian entries.
        public string oneLineIdentity;
    }

    [CreateAssetMenu(fileName = "UnitDeathEventChannel",
        menuName = "Project Astra/Core/Unit Death Event Channel")]
    /// <summary>ScriptableObject event bus for unit deaths. Mirrors
    /// GameStateEventChannel — Register/Unregister/Raise. Listeners: DeathRegistry,
    /// BattleVictoryWatcher, future analytics/achievements.</summary>
    public class UnitDeathEventChannel : ScriptableObject
    {
        private Action<UnitDeathEventArgs> _onUnitDied;

        public void Register(Action<UnitDeathEventArgs> listener)   => _onUnitDied += listener;
        public void Unregister(Action<UnitDeathEventArgs> listener) => _onUnitDied -= listener;

        public void Raise(UnitDeathEventArgs args) => _onUnitDied?.Invoke(args);
    }
}
