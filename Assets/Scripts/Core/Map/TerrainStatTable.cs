using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Map/Terrain Stat Table")]
    public class TerrainStatTable : ScriptableObject
    {
        [SerializeField] private TerrainStats[] _stats = new TerrainStats[18];

        public TerrainStats GetStats(TerrainType terrain)
        {
            int index = (int)terrain;
            if (index < 0 || index >= _stats.Length)
                return TerrainStats.Default;
            return _stats[index];
        }

        public int TerrainCount => _stats.Length;

        private void OnValidate()
        {
            if (_stats.Length != 18)
                Array.Resize(ref _stats, 18);
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
            defenceBonus = 0,
            avoidBonus = 0,
            healPerTurn = 0,
            isInteractable = false
        };
    }
}
