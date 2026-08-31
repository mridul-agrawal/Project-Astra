using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.State
{
    // The list of legal GameState→GameState moves. Stored as a ScriptableObject so designers
    // can review and edit the table in the Inspector without code changes.
    [CreateAssetMenu(fileName = "TransitionTable", menuName = "Project Astra/Core/Transition Table")]
    public class GameStateTransitionTable : ScriptableObject
    {
        [System.Serializable]
        public struct TransitionEntry
        {
            public GameState From;
            public GameState To;

            public TransitionEntry(GameState from, GameState to)
            {
                From = from;
                To = to;
            }
        }

        [SerializeField] private TransitionEntry[] validTransitions;

        private HashSet<(GameState, GameState)> lookupSet;

        public int TransitionCount => validTransitions != null ? validTransitions.Length : 0;

        public bool IsValid(GameState from, GameState to)
        {
            if (lookupSet == null) Initialize();
            return lookupSet.Contains((from, to));
        }

        public void Initialize()
        {
            int capacity = validTransitions != null ? validTransitions.Length : 0;
            lookupSet = new HashSet<(GameState, GameState)>(capacity);

            if (validTransitions == null) return;

            foreach (var entry in validTransitions)
                lookupSet.Add((entry.From, entry.To));
        }


        // Test helper methods:

#if UNITY_EDITOR
        [ContextMenu("Populate Default Transitions")]
        private void PopulateDefaults()
        {
            validTransitions = CreateDefaultTransitions();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        // Seed data for the SO asset. Used by the inspector context-menu and by tests.
        public static TransitionEntry[] CreateDefaultTransitions()
        {
            return new[]
            {
                new TransitionEntry(GameState.Splash, GameState.TitleScreen),

                new TransitionEntry(GameState.TitleScreen, GameState.MainMenu),
                new TransitionEntry(GameState.TitleScreen, GameState.Cutscene),

                new TransitionEntry(GameState.MainMenu, GameState.Cutscene),
                new TransitionEntry(GameState.MainMenu, GameState.PreBattlePrep),
                new TransitionEntry(GameState.MainMenu, GameState.BattleMap),

                new TransitionEntry(GameState.Cutscene, GameState.PreBattlePrep),
                new TransitionEntry(GameState.Cutscene, GameState.BattleMap),
                // Campaign complete: the ending cutscene hands back to the title.
                new TransitionEntry(GameState.Cutscene, GameState.TitleScreen),

                new TransitionEntry(GameState.PreBattlePrep, GameState.BattleMap),

                new TransitionEntry(GameState.BattleMap, GameState.Cutscene),
                new TransitionEntry(GameState.BattleMap, GameState.CombatAnimation),
                new TransitionEntry(GameState.BattleMap, GameState.Dialogue),
                new TransitionEntry(GameState.BattleMap, GameState.BattleMapPaused),
                new TransitionEntry(GameState.BattleMap, GameState.ChapterClear),
                new TransitionEntry(GameState.BattleMap, GameState.WarLedger),
                new TransitionEntry(GameState.BattleMap, GameState.GameOver),
                new TransitionEntry(GameState.BattleMap, GameState.LevelUpScreen),

                new TransitionEntry(GameState.WarLedger, GameState.ChapterClear),

                new TransitionEntry(GameState.BattleMapPaused, GameState.BattleMap),
                new TransitionEntry(GameState.BattleMapPaused, GameState.SaveMenu),
                new TransitionEntry(GameState.BattleMapPaused, GameState.SettingsMenu),
                new TransitionEntry(GameState.BattleMapPaused, GameState.TitleScreen),

                new TransitionEntry(GameState.CombatAnimation, GameState.BattleMap),

                new TransitionEntry(GameState.Dialogue, GameState.BattleMap),
                new TransitionEntry(GameState.Dialogue, GameState.GameOver),

                new TransitionEntry(GameState.ChapterClear, GameState.Cutscene),
                new TransitionEntry(GameState.ChapterClear, GameState.SaveMenu),

                new TransitionEntry(GameState.GameOver, GameState.MainMenu),
                new TransitionEntry(GameState.GameOver, GameState.SaveMenu),
                new TransitionEntry(GameState.GameOver, GameState.TitleScreen),

                new TransitionEntry(GameState.SaveMenu, GameState.BattleMapPaused),
                new TransitionEntry(GameState.SaveMenu, GameState.ChapterClear),
                new TransitionEntry(GameState.SaveMenu, GameState.GameOver),
                new TransitionEntry(GameState.SaveMenu, GameState.MainMenu),

                new TransitionEntry(GameState.SettingsMenu, GameState.BattleMapPaused),
                new TransitionEntry(GameState.SettingsMenu, GameState.MainMenu),

                new TransitionEntry(GameState.LevelUpScreen, GameState.BattleMap),

                new TransitionEntry(GameState.BattleMap, GameState.UnitInfoScreen),
                new TransitionEntry(GameState.UnitInfoScreen, GameState.BattleMap),

                // The Gurukul hub sits between battles, so the campaign reaches it from wherever
                // the previous step ended and leaves it for whatever the next step is. A
                // conversation there is GameState.Dialogue, the same as anywhere else.
                new TransitionEntry(GameState.TitleScreen, GameState.HubExploration),
                new TransitionEntry(GameState.MainMenu, GameState.HubExploration),
                new TransitionEntry(GameState.Cutscene, GameState.HubExploration),
                new TransitionEntry(GameState.ChapterClear, GameState.HubExploration),
                new TransitionEntry(GameState.HubExploration, GameState.Dialogue),
                new TransitionEntry(GameState.Dialogue, GameState.HubExploration),
                new TransitionEntry(GameState.HubExploration, GameState.BattleMap),
                new TransitionEntry(GameState.HubExploration, GameState.Cutscene),
                new TransitionEntry(GameState.HubExploration, GameState.TitleScreen),

                // An authored sequence plays in whatever world is already loaded, so unlike a
                // cutscene it never brings a scene of its own. It can talk, and it can run
                // straight into the battle without handing control back first.
                new TransitionEntry(GameState.HubExploration, GameState.ScriptedSequence),
                new TransitionEntry(GameState.ScriptedSequence, GameState.HubExploration),
                new TransitionEntry(GameState.Dialogue, GameState.ScriptedSequence),
                new TransitionEntry(GameState.ScriptedSequence, GameState.Dialogue),
                new TransitionEntry(GameState.ScriptedSequence, GameState.BattleMap),
            };
        }
    }
}
