namespace ProjectAstra.Core.Grid
{
    // The five render layers, bottom-to-top: Ground, Overlay, Object, Units, UI.
    // Don't reorder — add new layers only at the end. Unity writes these as plain
    // integers into every map asset, so reordering silently points existing maps
    // at the wrong layer (no error, just broken data).
    public enum MapLayer
    {
        Ground = 0,
        Overlay = 1,
        Object = 2,
        Units = 3,
        UI = 4
    }
}
