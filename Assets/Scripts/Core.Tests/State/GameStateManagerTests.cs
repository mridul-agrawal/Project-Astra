using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAstra.Core;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Events;

namespace ProjectAstra.Core.Tests.State
{
    [TestFixture]
    public class GameStateManagerTests
    {
        private GameObject go;
        private GameObject eventServiceGo;
        private GameStateManager manager;
        private GameStateTransitionTable table;
        private GameStateEventChannel channel;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("TestGameStateManager");
            manager = go.AddComponent<GameStateManager>();
            table = ScriptableObject.CreateInstance<GameStateTransitionTable>();
            channel = ScriptableObject.CreateInstance<GameStateEventChannel>();

            eventServiceGo = new GameObject("TestEventService");
            eventServiceGo.AddComponent<EventService>().InitializeForTest(channel, null, null, null);

            var field = typeof(GameStateTransitionTable).GetField("validTransitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(table, GameStateTransitionTable.CreateDefaultTransitions());

            manager.Initialize(table, GameState.TitleScreen);
        }

        [TearDown]
        public void TearDown()
        {
            if (GameStateManager.Instance == manager)
            {
                var instanceProp = typeof(GameStateManager).GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                instanceProp.SetValue(null, null);
            }
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(eventServiceGo);
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(channel);
        }

        [Test]
        public void InitialState_IsTitleScreen()
        {
            Assert.AreEqual(GameState.TitleScreen, manager.CurrentState);
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
        public void ValidTransition_Succeeds(GameState startState, GameState target)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            manager.ForceState(startState, "test setup");
            manager.ResetFrameGate();

            bool result = manager.RequestTransition(target, "test");

            Assert.IsTrue(result, $"Transition {startState} -> {target} should succeed");
            Assert.AreEqual(target, manager.CurrentState);
        }

        [TestCase(GameState.TitleScreen, GameState.BattleMap)]
        [TestCase(GameState.CombatAnimation, GameState.ChapterClear)]
        [TestCase(GameState.GameOver, GameState.BattleMap)]
        public void IllegalTransition_Rejected_StateUnchanged(GameState startState, GameState target)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            manager.ForceState(startState, "test setup");
            manager.ResetFrameGate();

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] ILLEGAL transition"));
            bool result = manager.RequestTransition(target, "test");

            Assert.IsFalse(result, $"Transition {startState} -> {target} should be rejected");
            Assert.AreEqual(startState, manager.CurrentState, "State should remain unchanged");
        }

        [Test]
        public void SecondTransitionSameFrame_IsDiscarded()
        {
            bool first = manager.RequestTransition(GameState.MainMenu, "first");
            Assert.IsTrue(first);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[GameStateManager\] Transition to .+ discarded"));
            bool second = manager.RequestTransition(GameState.Cutscene, "second");
            Assert.IsFalse(second);
            Assert.AreEqual(GameState.MainMenu, manager.CurrentState);
        }

        [Test]
        public void AfterFrameGateReset_TransitionSucceeds()
        {
            manager.RequestTransition(GameState.MainMenu, "first");
            manager.ResetFrameGate();

            bool result = manager.RequestTransition(GameState.Cutscene, "second");
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Cutscene, manager.CurrentState);
        }

        [Test]
        public void StateChangedEvent_ContainsCorrectPreviousAndNewState()
        {
            StateChangeArgs? received = null;
            channel.Register(args => received = args);

            manager.RequestTransition(GameState.MainMenu, "test");

            Assert.IsNotNull(received);
            Assert.AreEqual(GameState.TitleScreen, received.Value.PreviousState);
            Assert.AreEqual(GameState.MainMenu, received.Value.NewState);
        }

        [Test]
        public void StateChangedEvent_DoesNotFireOnIllegalTransition()
        {
            StateChangeArgs? received = null;
            channel.Register(args => received = args);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] ILLEGAL transition"));
            manager.RequestTransition(GameState.BattleMap, "test");

            Assert.IsNull(received);
        }

        [Test]
        public void SaveMenu_StoresMenuReturnState()
        {
            manager.RequestTransition(GameState.MainMenu, "test");
            manager.ResetFrameGate();
            manager.RequestTransition(GameState.BattleMap, "test");
            manager.ResetFrameGate();
            manager.RequestTransition(GameState.BattleMapPaused, "test");
            manager.ResetFrameGate();

            manager.RequestTransition(GameState.SaveMenu, "test");

            Assert.AreEqual(GameState.BattleMapPaused, manager.MenuReturnState);
        }

        [Test]
        public void SettingsMenu_StoresMenuReturnState()
        {
            manager.RequestTransition(GameState.MainMenu, "test");
            manager.ResetFrameGate();
            manager.RequestTransition(GameState.BattleMap, "test");
            manager.ResetFrameGate();
            manager.RequestTransition(GameState.BattleMapPaused, "test");
            manager.ResetFrameGate();

            manager.RequestTransition(GameState.SettingsMenu, "test");

            Assert.AreEqual(GameState.BattleMapPaused, manager.MenuReturnState);
        }

        [Test]
        public void ReturnFromContextMenu_TransitionsToStoredContext()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            manager.ForceState(GameState.BattleMapPaused, "test setup");
            manager.ResetFrameGate();
            manager.RequestTransition(GameState.SaveMenu, "test");
            manager.ResetFrameGate();

            bool result = manager.ReturnFromContextMenu("test");

            Assert.IsTrue(result);
            Assert.AreEqual(GameState.BattleMapPaused, manager.CurrentState);
        }

        [Test]
        public void ReturnFromContextMenu_RejectsIfNotInContextMenuState()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] ReturnFromContextMenu called from invalid state"));
            bool result = manager.ReturnFromContextMenu("test");
            Assert.IsFalse(result);
        }

        [Test]
        public void ForceState_BypassesTransitionTable()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            manager.ForceState(GameState.BattleMap, "test recovery");
            Assert.AreEqual(GameState.BattleMap, manager.CurrentState);
        }

        [Test]
        public void ForceState_RaisesEvent()
        {
            StateChangeArgs? received = null;
            channel.Register(args => received = args);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            manager.ForceState(GameState.MainMenu, "test");

            Assert.IsNotNull(received);
            Assert.AreEqual(GameState.TitleScreen, received.Value.PreviousState);
            Assert.AreEqual(GameState.MainMenu, received.Value.NewState);
        }
    }
}
