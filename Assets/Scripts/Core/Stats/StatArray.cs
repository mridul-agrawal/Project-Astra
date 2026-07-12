using System;
using UnityEngine;

namespace ProjectAstra.Core.Stats
{
    // Bundles a unit's nine stats into one passable, copyable, serializable lump.
    [Serializable]
    public struct StatArray
    {
        public const int Length = 9;

        [SerializeField] private int[] values;

        public static StatArray Create() => new StatArray { values = new int[Length] };

        public static StatArray From(int hp, int str, int mag, int skl, int spd, int def, int res, int con, int niyati)
        {
            var array = Create();
            array[StatIndex.HP]     = hp;
            array[StatIndex.Str]    = str;
            array[StatIndex.Mag]    = mag;
            array[StatIndex.Skl]    = skl;
            array[StatIndex.Spd]    = spd;
            array[StatIndex.Def]    = def;
            array[StatIndex.Res]    = res;
            array[StatIndex.Con]    = con;
            array[StatIndex.Niyati] = niyati;
            return array;
        }

        public int this[StatIndex index]
        {
            get { EnsureInitialized(); return values[(int)index]; }
            set { EnsureInitialized(); values[(int)index] = value; }
        }

        // Allocates _values on first access; without this, default(StatArray) would null-ref on read.
        private void EnsureInitialized()
        {
            if (values == null || values.Length != Length)
                values = new int[Length];
        }
    }
}
