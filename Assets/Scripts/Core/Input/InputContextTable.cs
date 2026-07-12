using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Input
{
    // One row of the table: the actions allowed while in a given GameState. Both fields render as
    // dropdowns (state = enum, allowed = a [Flags] multi-select), so a designer never types names.
    [Serializable]
    public class StateInputRule
    {
        [SerializeField] private GameState state;
        [SerializeField] private GameInputAction allowed;

        public StateInputRule() { }
        public StateInputRule(GameState state, GameInputAction allowed)
        {
            this.state = state;
            this.allowed = allowed;
        }

        public GameState State => state;
        public GameInputAction Allowed => allowed;
    }

    // Designer-editable table of which inputs are live in which GameState. InputManager reads it on
    // every state change and disables any action not allowed here (so e.g. cursor movement can't
    // fire during a cutscene). Seeded with sensible defaults; edit rows in the inspector to retune.
    [CreateAssetMenu(fileName = "InputContextTable", menuName = "Project Astra/Core/Input Context Table")]
    public class InputContextTable : ScriptableObject
    {
        // Readability helpers for the defaults below — kept out of the enum so they don't clutter
        // the designer's tick-box dropdown.
        private const GameInputAction Cursor =
            GameInputAction.CursorUp | GameInputAction.CursorDown | GameInputAction.CursorLeft | GameInputAction.CursorRight;
        private const GameInputAction CursorAndMenu = Cursor | GameInputAction.Confirm | GameInputAction.Cancel;
        private const GameInputAction Dialogue =
            Cursor | GameInputAction.Confirm | GameInputAction.Cancel | GameInputAction.SkipDialogue | GameInputAction.HoldAdvanceDialogue;
        private const GameInputAction All = (GameInputAction)0xFFFF;   // bits 0..15 = every defined action

        [SerializeField] private List<StateInputRule> rules = new()
        {
            new(GameState.Splash, GameInputAction.Confirm),
            new(GameState.TitleScreen, GameInputAction.Confirm),
            new(GameState.MainMenu, CursorAndMenu),
            new(GameState.Cutscene, Dialogue),
            new(GameState.PreBattlePrep, CursorAndMenu | GameInputAction.Pause),
            new(GameState.BattleMap, All),
            new(GameState.BattleMapPaused, CursorAndMenu),
            new(GameState.CombatAnimation, Cursor | GameInputAction.Confirm | GameInputAction.Cancel | GameInputAction.SkipAnimation),
            new(GameState.Dialogue, Dialogue),
            new(GameState.ChapterClear, CursorAndMenu),
            new(GameState.WarLedger, GameInputAction.Confirm),
            new(GameState.GameOver, CursorAndMenu),
            new(GameState.SaveMenu, CursorAndMenu),
            new(GameState.SettingsMenu, CursorAndMenu),
            new(GameState.LevelUpScreen, GameInputAction.Confirm),
        };

        // The allowed-action mask for a state — first matching rule wins; None if the state is
        // unlisted (nothing allowed). Uncached so a designer's play-mode edits take effect at once.
        public GameInputAction AllowedFor(GameState state)
        {
            foreach (var rule in rules)
                if (rule.State == state) return rule.Allowed;
            return GameInputAction.None;
        }

        public bool IsActionAllowed(GameState state, GameInputAction action) =>
            (AllowedFor(state) & action) != 0;

        // The allowed actions as their InputActionAsset names, for InputManager to enable/disable.
        public HashSet<string> GetAllowedActionNames(GameState state)
        {
            var mask = AllowedFor(state);
            var names = new HashSet<string>();
            foreach (GameInputAction action in Enum.GetValues(typeof(GameInputAction)))
                if (action != GameInputAction.None && (mask & action) != 0)
                    names.Add(action.ToString());
            return names;
        }
    }
}
