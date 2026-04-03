using System;
using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class InputContextTests
    {
        [Test]
        public void EveryGameState_HasDefinedContext()
        {
            foreach (GameState state in Enum.GetValues(typeof(GameState)))
            {
                var actions = InputContext.GetAllowedActions(state);
                Assert.IsNotNull(actions, $"State {state} has no input context defined");
                Assert.IsTrue(actions.Count > 0, $"State {state} has empty input context");
            }
        }

        [Test]
        public void TotalActionCount_Is16()
        {
            Assert.AreEqual(16, InputContext.TotalActionCount);
        }

        [Test]
        public void BattleMap_AllowsAll16Actions()
        {
            var actions = InputContext.GetAllowedActions(GameState.BattleMap);
            Assert.AreEqual(16, actions.Count);
        }

        [Test]
        public void CombatAnimation_OnlyAllowsSkipAnimation()
        {
            var actions = InputContext.GetAllowedActions(GameState.CombatAnimation);
            Assert.AreEqual(1, actions.Count);
            Assert.IsTrue(actions.Contains(InputContext.SkipAnimation));
        }

        [Test]
        public void Cutscene_OnlyAllowsDialogueActions()
        {
            var actions = InputContext.GetAllowedActions(GameState.Cutscene);
            Assert.AreEqual(2, actions.Count);
            Assert.IsTrue(actions.Contains(InputContext.SkipDialogue));
            Assert.IsTrue(actions.Contains(InputContext.HoldAdvanceDialogue));
        }

        [Test]
        public void Dialogue_OnlyAllowsDialogueActions()
        {
            var actions = InputContext.GetAllowedActions(GameState.Dialogue);
            Assert.AreEqual(2, actions.Count);
            Assert.IsTrue(actions.Contains(InputContext.SkipDialogue));
            Assert.IsTrue(actions.Contains(InputContext.HoldAdvanceDialogue));
        }

        [Test]
        public void TitleScreen_OnlyAllowsConfirm()
        {
            var actions = InputContext.GetAllowedActions(GameState.TitleScreen);
            Assert.AreEqual(1, actions.Count);
            Assert.IsTrue(actions.Contains(InputContext.Confirm));
        }

        [Test]
        public void MenuStates_AllowCursorAndMenuActions()
        {
            var menuStates = new[]
            {
                GameState.MainMenu, GameState.BattleMapPaused,
                GameState.ChapterClear, GameState.GameOver,
                GameState.SaveMenu, GameState.SettingsMenu
            };

            foreach (var state in menuStates)
            {
                var actions = InputContext.GetAllowedActions(state);
                Assert.IsTrue(actions.Contains(InputContext.CursorUp), $"{state} missing CursorUp");
                Assert.IsTrue(actions.Contains(InputContext.CursorDown), $"{state} missing CursorDown");
                Assert.IsTrue(actions.Contains(InputContext.CursorLeft), $"{state} missing CursorLeft");
                Assert.IsTrue(actions.Contains(InputContext.CursorRight), $"{state} missing CursorRight");
                Assert.IsTrue(actions.Contains(InputContext.Confirm), $"{state} missing Confirm");
                Assert.IsTrue(actions.Contains(InputContext.Cancel), $"{state} missing Cancel");
            }
        }

        [Test]
        public void PreBattlePrep_IncludesPause()
        {
            var actions = InputContext.GetAllowedActions(GameState.PreBattlePrep);
            Assert.IsTrue(actions.Contains(InputContext.Pause));
            Assert.IsTrue(actions.Contains(InputContext.Confirm));
            Assert.IsTrue(actions.Contains(InputContext.Cancel));
        }

        [TestCase(InputContext.CursorUp)]
        [TestCase(InputContext.Confirm)]
        [TestCase(InputContext.Pause)]
        [TestCase(InputContext.NextUnit)]
        [TestCase(InputContext.SkipAnimation)]
        public void BattleMap_ContainsAction(string actionName)
        {
            Assert.IsTrue(InputContext.IsActionAllowed(GameState.BattleMap, actionName));
        }

        [TestCase(InputContext.Confirm)]
        [TestCase(InputContext.OpenMapMenu)]
        [TestCase(InputContext.NextUnit)]
        public void CombatAnimation_DoesNotAllowGameplayActions(string actionName)
        {
            Assert.IsFalse(InputContext.IsActionAllowed(GameState.CombatAnimation, actionName));
        }
    }
}
