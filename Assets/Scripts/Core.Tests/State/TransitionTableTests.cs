using System;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Tests.State
{
    [TestFixture]
    public class TransitionTableTests
    {
        private GameStateTransitionTable table;

        [SetUp]
        public void SetUp()
        {
            table = ScriptableObject.CreateInstance<GameStateTransitionTable>();
            var field = typeof(GameStateTransitionTable).GetField("validTransitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(table, GameStateTransitionTable.CreateDefaultTransitions());
            table.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void TransitionCount_MatchesDefaults()
        {
            var defaults = GameStateTransitionTable.CreateDefaultTransitions();
            Assert.AreEqual(defaults.Length, table.TransitionCount);
        }

        [TestCase(GameState.TitleScreen, GameState.MainMenu)]
        [TestCase(GameState.MainMenu, GameState.Cutscene)]
        [TestCase(GameState.MainMenu, GameState.PreBattlePrep)]
        [TestCase(GameState.MainMenu, GameState.BattleMap)]
        [TestCase(GameState.Cutscene, GameState.PreBattlePrep)]
        [TestCase(GameState.Cutscene, GameState.BattleMap)]
        [TestCase(GameState.PreBattlePrep, GameState.BattleMap)]
        [TestCase(GameState.BattleMap, GameState.Cutscene)]
        [TestCase(GameState.BattleMap, GameState.CombatAnimation)]
        [TestCase(GameState.BattleMap, GameState.Dialogue)]
        [TestCase(GameState.BattleMap, GameState.BattleMapPaused)]
        [TestCase(GameState.BattleMap, GameState.ChapterClear)]
        [TestCase(GameState.BattleMap, GameState.WarLedger)]
        [TestCase(GameState.BattleMap, GameState.GameOver)]
        [TestCase(GameState.BattleMap, GameState.LevelUpScreen)]
        [TestCase(GameState.WarLedger, GameState.ChapterClear)]
        [TestCase(GameState.BattleMapPaused, GameState.BattleMap)]
        [TestCase(GameState.BattleMapPaused, GameState.SaveMenu)]
        [TestCase(GameState.BattleMapPaused, GameState.SettingsMenu)]
        [TestCase(GameState.CombatAnimation, GameState.BattleMap)]
        [TestCase(GameState.Dialogue, GameState.BattleMap)]
        [TestCase(GameState.Dialogue, GameState.GameOver)]
        [TestCase(GameState.ChapterClear, GameState.Cutscene)]
        [TestCase(GameState.ChapterClear, GameState.SaveMenu)]
        [TestCase(GameState.GameOver, GameState.MainMenu)]
        [TestCase(GameState.GameOver, GameState.SaveMenu)]
        [TestCase(GameState.SaveMenu, GameState.BattleMapPaused)]
        [TestCase(GameState.SaveMenu, GameState.ChapterClear)]
        [TestCase(GameState.SaveMenu, GameState.GameOver)]
        [TestCase(GameState.SaveMenu, GameState.MainMenu)]
        [TestCase(GameState.SettingsMenu, GameState.BattleMapPaused)]
        [TestCase(GameState.SettingsMenu, GameState.MainMenu)]
        [TestCase(GameState.LevelUpScreen, GameState.BattleMap)]
        public void ValidTransition_ReturnsTrue(GameState from, GameState to)
        {
            Assert.IsTrue(table.IsValid(from, to),
                $"Expected transition {from} -> {to} to be valid");
        }

        [Test]
        public void SelfTransitions_AreAllInvalid()
        {
            foreach (GameState state in Enum.GetValues(typeof(GameState)))
            {
                Assert.IsFalse(table.IsValid(state, state),
                    $"Self-transition {state} -> {state} should be invalid");
            }
        }

        [TestCase(GameState.TitleScreen, GameState.BattleMap)]
        [TestCase(GameState.TitleScreen, GameState.GameOver)]
        [TestCase(GameState.CombatAnimation, GameState.ChapterClear)]
        [TestCase(GameState.SaveMenu, GameState.Dialogue)]
        [TestCase(GameState.GameOver, GameState.BattleMap)]
        [TestCase(GameState.ChapterClear, GameState.BattleMap)]
        [TestCase(GameState.PreBattlePrep, GameState.MainMenu)]
        public void IllegalTransition_ReturnsFalse(GameState from, GameState to)
        {
            Assert.IsFalse(table.IsValid(from, to),
                $"Expected transition {from} -> {to} to be invalid");
        }
    }
}
