using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(fileName = "TransitionTable", menuName = "Project Astra/Core/Transition Table")]
    /// <summary>Data-driven whitelist of legal GameState transitions, stored as a ScriptableObject and queried via HashSet lookup.</summary>
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

        [SerializeField] private TransitionEntry[] _validTransitions;

        private HashSet<(GameState, GameState)> _lookupSet;

        public int TransitionCount => _validTransitions != null ? _validTransitions.Length : 0;

        public void Initialize()
        {
            int capacity = _validTransitions != null ? _validTransitions.Length : 0;
            _lookupSet = new HashSet<(GameState, GameState)>(capacity);

            if (_validTransitions == null) return;

            foreach (var entry in _validTransitions)
                _lookupSet.Add((entry.From, entry.To));
        }

        public bool IsValid(GameState from, GameState to)
        {
            if (_lookupSet == null) Initialize();
            return _lookupSet.Contains((from, to));
        }

        public static TransitionEntry[] CreateDefaultTransitions()
        {
            return new[]
            {
                new TransitionEntry(GameState.TitleScreen, GameState.MainMenu),

                new TransitionEntry(GameState.MainMenu, GameState.Cutscene),
                new TransitionEntry(GameState.MainMenu, GameState.PreBattlePrep),
                new TransitionEntry(GameState.MainMenu, GameState.BattleMap),

                new TransitionEntry(GameState.Cutscene, GameState.PreBattlePrep),
                new TransitionEntry(GameState.Cutscene, GameState.BattleMap),

                new TransitionEntry(GameState.PreBattlePrep, GameState.BattleMap),

                new TransitionEntry(GameState.BattleMap, GameState.Cutscene),
                new TransitionEntry(GameState.BattleMap, GameState.CombatAnimation),
                new TransitionEntry(GameState.BattleMap, GameState.Dialogue),
                new TransitionEntry(GameState.BattleMap, GameState.BattleMapPaused),
                new TransitionEntry(GameState.BattleMap, GameState.ChapterClear),
                new TransitionEntry(GameState.BattleMap, GameState.WarLedger),
                new TransitionEntry(GameState.BattleMap, GameState.GameOver),

                new TransitionEntry(GameState.WarLedger, GameState.ChapterClear),

                new TransitionEntry(GameState.BattleMapPaused, GameState.BattleMap),
                new TransitionEntry(GameState.BattleMapPaused, GameState.SaveMenu),
                new TransitionEntry(GameState.BattleMapPaused, GameState.SettingsMenu),

                new TransitionEntry(GameState.CombatAnimation, GameState.BattleMap),

                new TransitionEntry(GameState.Dialogue, GameState.BattleMap),

                new TransitionEntry(GameState.ChapterClear, GameState.Cutscene),
                new TransitionEntry(GameState.ChapterClear, GameState.SaveMenu),

                new TransitionEntry(GameState.GameOver, GameState.MainMenu),
                new TransitionEntry(GameState.GameOver, GameState.SaveMenu),

                new TransitionEntry(GameState.SaveMenu, GameState.BattleMapPaused),
                new TransitionEntry(GameState.SaveMenu, GameState.ChapterClear),
                new TransitionEntry(GameState.SaveMenu, GameState.GameOver),
                new TransitionEntry(GameState.SaveMenu, GameState.MainMenu),

                new TransitionEntry(GameState.SettingsMenu, GameState.BattleMapPaused),
                new TransitionEntry(GameState.SettingsMenu, GameState.MainMenu),
            };
        }

#if UNITY_EDITOR
        [ContextMenu("Populate Default Transitions")]
        private void PopulateDefaults()
        {
            _validTransitions = CreateDefaultTransitions();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
