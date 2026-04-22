using System;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// Session-scoped registry of every recorded death. Lives on a scene-level
    /// GameObject in BattleMap; subscribes to UnitDeathEventChannel on Awake.
    /// IPersistable hook is for the future campaign-save ticket; today nothing
    /// consumes Serialize/Restore.
    /// </summary>
    public class DeathRegistry : MonoBehaviour, IPersistable<DeathRegistryDto>
    {
        public static DeathRegistry Instance { get; private set; }

        [SerializeField] private UnitDeathEventChannel _deathChannel;

        private readonly List<DeathEntry> _entries = new();
        private int _unnamedEnemyDeathCount;

        // Pluggable at runtime — scene setup can swap the default impl for
        // a richer one (e.g. support-log-backed epitaphs).
        public IDeathEpitaphProvider EpitaphProvider { get; set; } = DefaultDeathEpitaphProvider.Instance;

        public int UnnamedEnemyDeathCount => _unnamedEnemyDeathCount;
        public IReadOnlyList<DeathEntry> All => _entries;

        public IReadOnlyList<DeathEntry> ForCurrentChapter()
        {
            int ch = ChapterContext.CurrentChapterNumber;
            var filtered = new List<DeathEntry>();
            foreach (var e in _entries) if (e.chapterOfDeath == ch) filtered.Add(e);
            return filtered;
        }

        public int UnnamedEnemyDeathCountForCurrentChapter() => _unnamedEnemyDeathCount;

        private void Awake()
        {
            // Last-write-wins singleton — scene reloads discard the previous
            // instance's data naturally. This is the session-scoped semantic
            // UM-01 ships with; replace when the save system lands.
            if (Instance != null && Instance != this) Destroy(Instance.gameObject);
            Instance = this;

            if (_deathChannel != null) _deathChannel.Register(OnUnitDied);
        }

        private void OnDestroy()
        {
            if (_deathChannel != null) _deathChannel.Unregister(OnUnitDied);
            if (Instance == this) Instance = null;
        }

        private void OnUnitDied(UnitDeathEventArgs args)
        {
            if (args.faction == DeathFaction.EnemyGeneric)
            {
                _unnamedEnemyDeathCount++;
                return;
            }

            _entries.Add(new DeathEntry {
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
            _entries.Clear();
            _unnamedEnemyDeathCount = 0;
        }

        public DeathRegistryDto Serialize() => new DeathRegistryDto {
            entries = _entries.ToArray(),
            unnamedEnemyDeathCount = _unnamedEnemyDeathCount,
        };

        public void Restore(DeathRegistryDto dto)
        {
            _entries.Clear();
            if (dto.entries != null) _entries.AddRange(dto.entries);
            _unnamedEnemyDeathCount = dto.unnamedEnemyDeathCount;
        }
    }
}
