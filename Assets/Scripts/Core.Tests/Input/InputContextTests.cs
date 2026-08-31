using System;
using NUnit.Framework;
using UnityEditor;
using ProjectAstra.Core;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Tests.Input
{
    // Validates the authored InputContextTable asset — the real data a designer edits — so an
    // accidental toggle that would (say) strip Confirm from a screen fails the build, not the game.
    [TestFixture]
    public class InputContextTests
    {
        private const string AssetPath = "Assets/ScriptableObjects/Core/InputContextTable.asset";

        private InputContextTable table;

        [OneTimeSetUp]
        public void LoadTable()
        {
            table = AssetDatabase.LoadAssetAtPath<InputContextTable>(AssetPath);
            Assert.IsNotNull(table, $"InputContextTable asset not found at {AssetPath}");
        }

        [Test]
        public void EveryGameState_HasAtLeastOneAllowedAction()
        {
            foreach (GameState state in Enum.GetValues(typeof(GameState)))
                Assert.IsTrue(table.GetAllowedActionNames(state).Count > 0,
                    $"State {state} has no allowed input — it could soft-lock.");
        }

        [Test]
        public void ActionEnum_Defines17Actions()
        {
            Assert.AreEqual(17, Enum.GetValues(typeof(GameInputAction)).Length - 1); // minus None
        }

        [Test]
        public void BattleMap_AllowsAll17Actions()
        {
            Assert.AreEqual(17, table.GetAllowedActionNames(GameState.BattleMap).Count);
        }

        [Test]
        public void CombatAnimation_AllowsSkipAnimationAndBlocksGameplay()
        {
            Assert.IsTrue(table.IsActionAllowed(GameState.CombatAnimation, GameInputAction.SkipAnimation));
            Assert.IsFalse(table.IsActionAllowed(GameState.CombatAnimation, GameInputAction.OpenMapMenu));
            Assert.IsFalse(table.IsActionAllowed(GameState.CombatAnimation, GameInputAction.NextUnit));
            Assert.IsFalse(table.IsActionAllowed(GameState.CombatAnimation, GameInputAction.FastCursor));
        }

        [Test]
        public void Cutscene_AllowsDialogueActionsAndBlocksGameplay()
        {
            Assert.IsTrue(table.IsActionAllowed(GameState.Cutscene, GameInputAction.SkipDialogue));
            Assert.IsTrue(table.IsActionAllowed(GameState.Cutscene, GameInputAction.HoldAdvanceDialogue));
            Assert.IsFalse(table.IsActionAllowed(GameState.Cutscene, GameInputAction.OpenMapMenu));
            Assert.IsFalse(table.IsActionAllowed(GameState.Cutscene, GameInputAction.NextUnit));
        }

        [Test]
        public void Dialogue_AllowsDialogueActionsAndBlocksGameplay()
        {
            Assert.IsTrue(table.IsActionAllowed(GameState.Dialogue, GameInputAction.SkipDialogue));
            Assert.IsTrue(table.IsActionAllowed(GameState.Dialogue, GameInputAction.HoldAdvanceDialogue));
            Assert.IsFalse(table.IsActionAllowed(GameState.Dialogue, GameInputAction.OpenMapMenu));
            Assert.IsFalse(table.IsActionAllowed(GameState.Dialogue, GameInputAction.NextUnit));
        }

        [Test]
        public void TitleScreen_OnlyAllowsConfirm()
        {
            Assert.AreEqual(1, table.GetAllowedActionNames(GameState.TitleScreen).Count);
            Assert.IsTrue(table.IsActionAllowed(GameState.TitleScreen, GameInputAction.Confirm));
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
                Assert.IsTrue(table.IsActionAllowed(state, GameInputAction.CursorUp), $"{state} missing CursorUp");
                Assert.IsTrue(table.IsActionAllowed(state, GameInputAction.CursorDown), $"{state} missing CursorDown");
                Assert.IsTrue(table.IsActionAllowed(state, GameInputAction.CursorLeft), $"{state} missing CursorLeft");
                Assert.IsTrue(table.IsActionAllowed(state, GameInputAction.CursorRight), $"{state} missing CursorRight");
                Assert.IsTrue(table.IsActionAllowed(state, GameInputAction.Confirm), $"{state} missing Confirm");
                Assert.IsTrue(table.IsActionAllowed(state, GameInputAction.Cancel), $"{state} missing Cancel");
            }
        }

        // The hub walks on the cursor directions and interacts with Confirm. Battle-only actions
        // must stay out, or a stray press would reach systems that aren't loaded in a hub scene.
        [Test]
        public void Gurukul_AllowsWalkingAndInteractingOnly()
        {
            Assert.IsTrue(table.IsActionAllowed(GameState.HubExploration, GameInputAction.CursorUp));
            Assert.IsTrue(table.IsActionAllowed(GameState.HubExploration, GameInputAction.Confirm));
            Assert.IsTrue(table.IsActionAllowed(GameState.HubExploration, GameInputAction.Cancel));
            Assert.IsFalse(table.IsActionAllowed(GameState.HubExploration, GameInputAction.OpenMapMenu));
            Assert.IsFalse(table.IsActionAllowed(GameState.HubExploration, GameInputAction.NextUnit));
            Assert.IsFalse(table.IsActionAllowed(GameState.HubExploration, GameInputAction.SkipAnimation));
        }

        [Test]
        public void PreBattlePrep_IncludesPause()
        {
            Assert.IsTrue(table.IsActionAllowed(GameState.PreBattlePrep, GameInputAction.Pause));
            Assert.IsTrue(table.IsActionAllowed(GameState.PreBattlePrep, GameInputAction.Confirm));
            Assert.IsTrue(table.IsActionAllowed(GameState.PreBattlePrep, GameInputAction.Cancel));
        }

        [TestCase(GameInputAction.CursorUp)]
        [TestCase(GameInputAction.Confirm)]
        [TestCase(GameInputAction.Pause)]
        [TestCase(GameInputAction.NextUnit)]
        [TestCase(GameInputAction.SkipAnimation)]
        public void BattleMap_ContainsAction(GameInputAction action)
        {
            Assert.IsTrue(table.IsActionAllowed(GameState.BattleMap, action));
        }

        [TestCase(GameInputAction.OpenMapMenu)]
        [TestCase(GameInputAction.NextUnit)]
        [TestCase(GameInputAction.FastCursor)]
        public void CombatAnimation_DoesNotAllowGameplayActions(GameInputAction action)
        {
            Assert.IsFalse(table.IsActionAllowed(GameState.CombatAnimation, action));
        }
    }
}
