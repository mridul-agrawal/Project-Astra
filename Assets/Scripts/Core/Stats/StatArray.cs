using System;
using UnityEngine;

namespace ProjectAstra.Core.Stats
{
    // Bundles a unit's nine stats into one passable, copyable, serializable lump.
    [Serializable]
    public struct StatArray
    {
        public const int Length = 9;

        [SerializeField] private int[] _values;

        public static StatArray Create() => new StatArray { _values = new int[Length] };

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
            get { EnsureInitialized(); return _values[(int)index]; }
            set { EnsureInitialized(); _values[(int)index] = value; }
        }

        // Auto-allocates the backing array so default(StatArray) returns zeros instead of throwing on first index access.
        private void EnsureInitialized()
        {
            if (_values == null || _values.Length != Length)
                _values = new int[Length];
        }
    }
}
