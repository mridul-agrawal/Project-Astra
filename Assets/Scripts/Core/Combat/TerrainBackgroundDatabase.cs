using System;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Combat
{
    // Maps TerrainType → background Sprite for the Normal/Fast overlay combat
    // scene. The defender's tile terrain wins (FE convention). Unfilled entries
    // fall through to _fallback, so adding terrains never crashes — they just
    // show the placeholder until art lands.
    [CreateAssetMenu(menuName = "Project Astra/Combat/Terrain Background Database", fileName = "TerrainBackgroundDatabase")]
    public class TerrainBackgroundDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TerrainType terrain;
            public Sprite background;
        }

        [Tooltip("Used when a terrain has no authored entry.")]
        [SerializeField] private Sprite fallback;
        [SerializeField] private Entry[] entries;

        public Sprite GetBackground(TerrainType terrain)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                    if (entries[i].terrain == terrain && entries[i].background != null)
                        return entries[i].background;
            }
            return fallback;
        }
    }
}
