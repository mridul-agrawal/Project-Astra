using System;

namespace ProjectAstra.Core.Input
{
    // Every logical input action, as a [Flags] enum so a set of them fits in one value and the
    // inspector renders it as a tick-box dropdown. Each name MUST match its action name in the
    // InputActionAsset — InputManager bridges to Unity via the enum's ToString(), the same
    // name-matching the old string constants relied on.
    [Flags]
    public enum GameInputAction
    {
        None                = 0,
        CursorUp            = 1 << 0,
        CursorDown          = 1 << 1,
        CursorLeft          = 1 << 2,
        CursorRight         = 1 << 3,
        Confirm             = 1 << 4,
        Cancel              = 1 << 5,
        FastCursor          = 1 << 6,
        OpenMapMenu         = 1 << 7,
        OpenUnitInfo        = 1 << 8,
        ToggleMapOverlay    = 1 << 9,
        Pause               = 1 << 10,
        SkipAnimation       = 1 << 11,
        SkipDialogue        = 1 << 12,
        HoldAdvanceDialogue = 1 << 13,
        NextUnit            = 1 << 14,
        PrevUnit            = 1 << 15,
        PeekObjective       = 1 << 16,
        HoldInspect         = 1 << 17,
    }
}
