using System;
using UnityEngine;
using ProjectAstra.Core.Pathfinding;

namespace ProjectAstra.Core.Grid
{
    // The lookup table for per-terrain combat bonuses, movement costs, and special effects
    // (heal, capture). One row per TerrainType, indexed by enum value. Stored as a
    // ScriptableObject so designers can tune values in the Inspector.
    [CreateAssetMenu(menuName = "Project Astra/Map/Terrain Stat Table")]
    public class TerrainStatTable : ScriptableObject
    {
        // Must equal the number of TerrainType enum entries. A test asserts the drift.
        public const int ExpectedTerrainCount = 19;

        [SerializeField] private TerrainStats[] _stats = new TerrainStats[ExpectedTerrainCount];

        public int TerrainCount => _stats.Length;

        public TerrainStats GetStats(TerrainType terrain)
        {
            int index = (int)terrain;
            if (index < 0 || index >= _stats.Length)
                return TerrainStats.Default;
            return _stats[index];
        }

        // Terrain DEF/AVO bonuses apply uniformly across movement types — flying units are
        // immune to movement costs (handled by Pathfinder) but not to terrain cover. The
        // moveType parameter is kept for forward compatibility with future special cases.
        public static (int def, int avo) GetTerrainBonuses(TerrainStats stats, MovementType moveType)
        {
            return (stats.defenceBonus, stats.avoidBonus);
        }

        public static bool IsPassable(TerrainStats stats, MovementType moveType)
        {
            return Pathfinder.GetMovementCost(stats, moveType) > 0;
        }

        private void OnValidate()
        {
            if (_stats.Length != ExpectedTerrainCount)
                Array.Resize(ref _stats, ExpectedTerrainCount);
        }
    }

    [Serializable]
    public struct TerrainStats
    {
        [Header("Movement Costs (0 = impassable)")]
        public int moveCostFoot;
        public int moveCostMounted;
        public int moveCostArmoured;
        public int moveCostFlying;
        public int moveCostPirate;
        public int moveCostThief;

        [Header("Combat Bonuses")]
        public int defenceBonus;
        public int avoidBonus;

        [Header("Special")]
        public int healPerTurn;
        public bool isInteractable;

        public static TerrainStats Default => new TerrainStats
        {
            moveCostFoot = 1,
            moveCostMounted = 1,
            moveCostArmoured = 1,
            moveCostFlying = 1,
            moveCostPirate = 1,
            moveCostThief = 1,
            defenceBonus = 0,
            avoidBonus = 0,
            healPerTurn = 0,
            isInteractable = false
        };
    }
}
