using System.Collections.Generic;

namespace ProjectAstra.Core
{
    /// <summary>Defines which input actions are allowed per GameState, used by InputManager for context-based action filtering.</summary>
    public static class InputContext
    {
        public const string CursorUp = "CursorUp";
        public const string CursorDown = "CursorDown";
        public const string CursorLeft = "CursorLeft";
        public const string CursorRight = "CursorRight";
        public const string Confirm = "Confirm";
        public const string Cancel = "Cancel";
        public const string FastCursor = "FastCursor";
        public const string OpenMapMenu = "OpenMapMenu";
        public const string OpenUnitInfo = "OpenUnitInfo";
        public const string ToggleMapOverlay = "ToggleMapOverlay";
        public const string Pause = "Pause";
        public const string SkipAnimation = "SkipAnimation";
        public const string SkipDialogue = "SkipDialogue";
        public const string HoldAdvanceDialogue = "HoldAdvanceDialogue";
        public const string NextUnit = "NextUnit";
        public const string PrevUnit = "PrevUnit";

        private static readonly HashSet<string> CursorAndMenuActions = new()
        {
            CursorUp, CursorDown, CursorLeft, CursorRight, Confirm, Cancel
        };

        private static readonly HashSet<string> AllActions = new()
        {
            CursorUp, CursorDown, CursorLeft, CursorRight,
            Confirm, Cancel, FastCursor,
            OpenMapMenu, OpenUnitInfo, ToggleMapOverlay,
            Pause, SkipAnimation, SkipDialogue, HoldAdvanceDialogue,
            NextUnit, PrevUnit
        };

        private static readonly Dictionary<GameState, HashSet<string>> ContextMap = new()
        {
            { GameState.TitleScreen, new HashSet<string> { Confirm } },

            { GameState.MainMenu, CursorAndMenuActions },

            { GameState.Cutscene, new HashSet<string>
                { CursorUp, CursorDown, CursorLeft, CursorRight, Confirm, Cancel, SkipDialogue, HoldAdvanceDialogue } },

            { GameState.PreBattlePrep, new HashSet<string>
                { CursorUp, CursorDown, CursorLeft, CursorRight, Confirm, Cancel, Pause } },

            { GameState.BattleMap, AllActions },

            { GameState.BattleMapPaused, CursorAndMenuActions },

            { GameState.CombatAnimation, new HashSet<string>
                { CursorUp, CursorDown, CursorLeft, CursorRight, Confirm, Cancel, SkipAnimation } },

            { GameState.Dialogue, new HashSet<string>
                { CursorUp, CursorDown, CursorLeft, CursorRight, Confirm, Cancel, SkipDialogue, HoldAdvanceDialogue } },

            { GameState.ChapterClear, CursorAndMenuActions },

            // WarLedger is Confirm-only by spec — it is a document, not a menu.
            // Dismissal is the one allowed input; no Cancel/Back.
            { GameState.WarLedger, new HashSet<string> { Confirm } },

            { GameState.GameOver, CursorAndMenuActions },

            { GameState.SaveMenu, CursorAndMenuActions },

            { GameState.SettingsMenu, CursorAndMenuActions },
        };

        public static HashSet<string> GetAllowedActions(GameState state)
        {
            return ContextMap.TryGetValue(state, out var actions) ? actions : new HashSet<string>();
        }

        public static bool IsActionAllowed(GameState state, string actionName)
        {
            return ContextMap.TryGetValue(state, out var actions) && actions.Contains(actionName);
        }

        public static int TotalActionCount => AllActions.Count;

        public static IReadOnlyCollection<string> GetAllActionNames() => AllActions;
    }
}
