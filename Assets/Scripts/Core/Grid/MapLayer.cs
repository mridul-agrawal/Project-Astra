namespace ProjectAstra.Core.Grid
{
    // The five render layers stacked bottom-to-top. Save data references these by integer
    // index, so don't reorder; append new layers at the end only.
    public enum MapLayer
    {
        Ground = 0,
        Overlay = 1,
        Object = 2,
        Units = 3,
        UI = 4
    }
}
