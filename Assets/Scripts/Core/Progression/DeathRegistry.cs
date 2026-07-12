using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Events;

namespace ProjectAstra.Core.Progression
{
    [Serializable]
    public struct DeathEntry
    {
        public string unitId;
        public string unitName;
        public string className;
        public DeathFaction faction;
        public int chapterOfDeath;
        public CauseOfDeath causeOfDeath;
        public string killerUnitId;      // nullable string
        public string epitaph;           // resolved via IDeathEpitaphProvider at write time
        public Vector2Int tileCoordinates;

        public bool IsNamed =>
            faction == DeathFaction.Player ||
            faction == DeathFaction.EnemyNamed ||
            faction == DeathFaction.Civilian;
    }

    [Serializable]
    public struct DeathRegistryDto
    {
        public DeathEntry[] entries;
        public int unnamedEnemyDeathCount;
    }

    // Session-scoped registry of every recorded death. Drop on a scene-level
    // GameObject in BattleMap; subscribes to unit-death events via EventService on Awake.
    // The IPersistable hook is a forward-compatible no-op today — the save
    // system ticket will eventually consume Serialize/Restore.
    public class DeathRegistry : MonoBehaviour, IPersistable<DeathRegistryDto>
    {
        public static DeathRegistry Instance { get; private set; }

        private readonly List<DeathEntry> entries = new();
        private int unnamedEnemyDeathCount;

        // Pluggable at runtime — scene setup can swap the default impl for
        // a richer one (e.g. support-log-backed epitaphs).
        public IDeathEpitaphProvider EpitaphProvider { get; set; } = DefaultDeathEpitaphProvider.Instance;

        public int UnnamedEnemyDeathCount => unnamedEnemyDeathCount;
        public IReadOnlyList<DeathEntry> All => entries;

        public IReadOnlyList<DeathEntry> ForCurrentChapter()
        {
            int ch = ChapterContext.CurrentChapterNumber;
            var filtered = new List<DeathEntry>();
            foreach (var e in entries)
                if (e.chapterOfDeath == ch) filtered.Add(e);
            return filtered;
        }

        public int UnnamedEnemyDeathCountForCurrentChapter() => unnamedEnemyDeathCount;

        private void Awake()
        {
            // Last-write-wins singleton — scene reloads discard the previous
            // instance's data naturally. Matches the session-scoped semantics
            // UM-01 ships with; replace when the save system lands.
            if (Instance != null && Instance != this) Destroy(Instance.gameObject);
            Instance = this;

            EventService.Instance.SubscribeUnitDeath(OnUnitDied);
        }

        private void OnDestroy()
        {
            if (EventService.Instance != null) EventService.Instance.UnsubscribeUnitDeath(OnUnitDied);
            if (Instance == this) Instance = null;
        }

        private void OnUnitDied(UnitDeathEventArgs args)
        {
            if (args.faction == DeathFaction.EnemyGeneric)
            {
                unnamedEnemyDeathCount++;
                return;
            }

            entries.Add(new DeathEntry
            {
                unitId          = args.unitId,
                unitName        = args.unitName,
                className       = args.className,
                faction         = args.faction,
                chapterOfDeath  = args.chapterNumber,
                causeOfDeath    = args.causeOfDeath,
                killerUnitId    = args.killerUnitId,
                epitaph         = EpitaphProvider.Resolve(args),
                tileCoordinates = args.tileCoordinates,
            });
        }

        public void ResetForNewChapter()
        {
            entries.Clear();
            unnamedEnemyDeathCount = 0;
        }

        public DeathRegistryDto Serialize() => new DeathRegistryDto
        {
            entries = entries.ToArray(),
            unnamedEnemyDeathCount = unnamedEnemyDeathCount,
        };

        public void Restore(DeathRegistryDto dto)
        {
            entries.Clear();
            if (dto.entries != null) entries.AddRange(dto.entries);
            unnamedEnemyDeathCount = dto.unnamedEnemyDeathCount;
        }
    }
}
