namespace ProjectAstra.Core.UI
{
    // UI-02. Returns the net temporary-modifier delta per stat for display in the
    // Unit Info Panel. Providers sum across all active sources (support, terrain,
    // status effect, weapon-triangle, etc.) — the panel shows only the net value.
    //
    // Stub implementation returns zeros. Real implementations live alongside the
    // subsystems that create modifiers.
    public interface ITemporaryModifierProvider
    {
        StatArray GetModifiers(UnitInstance unit);
    }
}
