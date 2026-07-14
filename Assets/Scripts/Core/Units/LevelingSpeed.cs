namespace ProjectAstra.Core.Units
{
    // Designer-facing leveling pace for a class. Maps to the EXP formula's divisor under the hood
    // (see ClassDefinition): Fast levels quickest (few kills per level), Very Slow is grindy.
    public enum LevelingSpeed
    {
        Fast,
        Normal,
        Slow,
        VerySlow,
    }
}
