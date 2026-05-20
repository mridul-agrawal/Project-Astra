using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat
{
    // How a unit died. Only Combat is wired into gameplay today (UM-01 scope).
    // Poison / Environmental / Other are listed so the payload struct is stable
    // when the future damage paths (USE-04 etc.) ship.
    public enum CauseOfDeath { Combat, Poison, Environmental, Other }

    // War's Ledger faction bucket — NOT the gameplay Faction. This is the
    // registry classification used for the ledger's columns: named player
    // losses, named enemy commanders, generic enemies (counted but not
    // itemised), and civilians (routed through the civilian thread).
    public enum DeathFaction { Player, EnemyNamed, EnemyGeneric, Civilian }

    [Serializable]
    public struct UnitSupportSnapshot
    {
        public string partnerId;
        public BondStage rank;
    }

    // Payload fired from the death hook at GridCursor.ApplyCombatResult.
    // Everything the DeathRegistry + War's Ledger need is bundled here so the
    // registry can stay runtime-scoped today and slot into a future save
    // system without the event surface changing.
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

        // Frozen at time of death so surviving partners can reference this
        // for epitaph lookup. Empty list if the unit had no active supports.
        public List<UnitSupportSnapshot> activeSupports;

        // One-line identity authored on UnitDefinition for named enemies;
        // empty for player / generic / civilian entries.
        public string oneLineIdentity;

        // UM-02: true when the victim was the player's Lord. Drives
        // LordDeathWatcher to pre-empt the faction-wipe path and play the
        // Lord's authored last-words sequence before transitioning to GameOver.
        public bool isLord;

        // Transient reference to the dying unit so LordDeathWatcher can fade
        // the sprite before hiding. NonSerialized so the rest of the struct
        // stays Unity-serializable.
        [NonSerialized] public TestUnit victim;
    }

    // ScriptableObject event bus for unit deaths. Mirrors GameStateEventChannel
    // — Register / Unregister / Raise. Listeners: DeathRegistry,
    // BattleVictoryWatcher, LordDeathWatcher, future analytics.
    [CreateAssetMenu(fileName = "UnitDeathEventChannel",
        menuName = "Project Astra/Core/Unit Death Event Channel")]
    public class UnitDeathEventChannel : ScriptableObject
    {
        private Action<UnitDeathEventArgs> _onUnitDied;

        public void Register(Action<UnitDeathEventArgs> listener) => _onUnitDied += listener;
        public void Unregister(Action<UnitDeathEventArgs> listener) => _onUnitDied -= listener;

        public void Raise(UnitDeathEventArgs args) => _onUnitDied?.Invoke(args);
    }
}
