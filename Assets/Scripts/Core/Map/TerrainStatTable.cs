using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Map/Terrain Stat Table")]
    public class TerrainStatTable : ScriptableObject
    {
        [SerializeField] private TerrainStats[] _stats = new TerrainStats[19];

        public TerrainStats GetStats(TerrainType terrain)
        {
            int index = (int)terrain;
            if (index < 0 || index >= _stats.Length)
                return TerrainStats.Default;
            return _stats[index];
        }

        /// <summary>
        /// Returns terrain DEF and AVO bonuses. Applied uniformly across movement types —
        /// flying units are immune to movement costs, not to terrain cover.
        /// </summary>
        public static (int def, int avo) GetTerrainBonuses(TerrainStats stats, MovementType moveType)
        {
            return (stats.defenceBonus, stats.avoidBonus);
        }

        /// <summary>Returns whether a tile is passable for a given movement type.</summary>
        public static bool IsPassable(TerrainStats stats, MovementType moveType)
        {
            return Pathfinder.GetMovementCost(stats, moveType) > 0;
        }

        public int TerrainCount => _stats.Length;

        private void OnValidate()
        {
            if (_stats.Length != 19)
                Array.Resize(ref _stats, 19);
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
