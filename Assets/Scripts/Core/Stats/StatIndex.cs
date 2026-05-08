namespace ProjectAstra.Core.Stats
{
    // Ordinals are contracted with StatArray._values[] indices — reordering this enum breaks every serialized StatArray on disk.
    public enum StatIndex
    {
        HP = 0,
        Str = 1,
        Mag = 2,
        Skl = 3,
        Spd = 4,
        Def = 5,
        Res = 6,
        Con = 7,
        Niyati = 8
    }
}
