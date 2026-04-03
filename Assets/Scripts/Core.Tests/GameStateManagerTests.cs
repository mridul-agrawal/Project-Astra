using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class GameStateManagerTests
    {
        private GameObject _go;
        private GameStateManager _manager;
        private GameStateTransitionTable _table;
        private GameStateEventChannel _channel;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestGameStateManager");
            _manager = _go.AddComponent<GameStateManager>();
            _table = ScriptableObject.CreateInstance<GameStateTransitionTable>();
            _channel = ScriptableObject.CreateInstance<GameStateEventChannel>();

            var field = typeof(GameStateTransitionTable).GetField("_validTransitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_table, GameStateTransitionTable.CreateDefaultTransitions());

            _manager.Initialize(_table, _channel, GameState.TitleScreen);
        }

        [TearDown]
        public void TearDown()
        {
            if (GameStateManager.Instance == _manager)
            {
                var instanceProp = typeof(GameStateManager).GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                instanceProp.SetValue(null, null);
            }
            UnityEngine.Object.DestroyImmediate(_go);
            UnityEngine.Object.DestroyImmediate(_table);
            UnityEngine.Object.DestroyImmediate(_channel);
        }

        // --- Initial state ---

        [Test]
        public void InitialState_IsTitleScreen()
        {
            Assert.AreEqual(GameState.TitleScreen, _manager.CurrentState);
        }

        // --- All 27 valid transitions (parameterized) ---

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
        [TestCase(GameState.BattleMap, GameState.GameOver)]
        [TestCase(GameState.BattleMapPaused, GameState.BattleMap)]
        [TestCase(GameState.BattleMapPaused, GameState.SaveMenu)]
        [TestCase(GameState.BattleMapPaused, GameState.SettingsMenu)]
        [TestCase(GameState.CombatAnimation, GameState.BattleMap)]
        [TestCase(GameState.Dialogue, GameState.BattleMap)]
        [TestCase(GameState.ChapterClear, GameState.Cutscene)]
        [TestCase(GameState.ChapterClear, GameState.SaveMenu)]
        [TestCase(GameState.GameOver, GameState.MainMenu)]
        [TestCase(GameState.GameOver, GameState.SaveMenu)]
        [TestCase(GameState.SaveMenu, GameState.BattleMapPaused)]
        [TestCase(GameState.SaveMenu, GameState.ChapterClear)]
        [TestCase(GameState.SaveMenu, GameState.MainMenu)]
        [TestCase(GameState.SettingsMenu, GameState.BattleMapPaused)]
        [TestCase(GameState.SettingsMenu, GameState.MainMenu)]
        public void ValidTransition_Succeeds(GameState startState, GameState target)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            _manager.ForceState(startState, "test setup");
            _manager.ResetFrameGate();

            bool result = _manager.RequestTransition(target, "test");

            Assert.IsTrue(result, $"Transition {startState} -> {target} should succeed");
            Assert.AreEqual(target, _manager.CurrentState);
        }

        // --- Illegal transitions ---

        [TestCase(GameState.TitleScreen, GameState.BattleMap)]
        [TestCase(GameState.Dialogue, GameState.GameOver)]
        [TestCase(GameState.CombatAnimation, GameState.ChapterClear)]
        [TestCase(GameState.GameOver, GameState.BattleMap)]
        public void IllegalTransition_Rejected_StateUnchanged(GameState startState, GameState target)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            _manager.ForceState(startState, "test setup");
            _manager.ResetFrameGate();

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] ILLEGAL transition"));
            bool result = _manager.RequestTransition(target, "test");

            Assert.IsFalse(result, $"Transition {startState} -> {target} should be rejected");
            Assert.AreEqual(startState, _manager.CurrentState, "State should remain unchanged");
        }

        // --- Per-frame gate ---

        [Test]
        public void SecondTransitionSameFrame_IsDiscarded()
        {
            bool first = _manager.RequestTransition(GameState.MainMenu, "first");
            Assert.IsTrue(first);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[GameStateManager\] Transition to .+ discarded"));
            bool second = _manager.RequestTransition(GameState.Cutscene, "second");
            Assert.IsFalse(second);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }

        [Test]
        public void AfterFrameGateReset_TransitionSucceeds()
        {
            _manager.RequestTransition(GameState.MainMenu, "first");
            _manager.ResetFrameGate();

            bool result = _manager.RequestTransition(GameState.Cutscene, "second");
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Cutscene, _manager.CurrentState);
        }

        // --- Entry/Exit hooks ---

        [Test]
        public void EntryHook_FiredOnTransition()
        {
            bool hookFired = false;
            _manager.RegisterEntryHook(GameState.MainMenu, () => hookFired = true);

            _manager.RequestTransition(GameState.MainMenu, "test");

            Assert.IsTrue(hookFired);
        }

        [Test]
        public void ExitHook_FiredOnTransition()
        {
            bool hookFired = false;
            _manager.RegisterExitHook(GameState.TitleScreen, () => hookFired = true);

            _manager.RequestTransition(GameState.MainMenu, "test");

            Assert.IsTrue(hookFired);
        }

        [Test]
        public void ExitHook_FiresBeforeEntryHook()
        {
            int order = 0;
            int exitOrder = -1;
            int entryOrder = -1;

            _manager.RegisterExitHook(GameState.TitleScreen, () => exitOrder = order++);
            _manager.RegisterEntryHook(GameState.MainMenu, () => entryOrder = order++);

            _manager.RequestTransition(GameState.MainMenu, "test");

            Assert.AreEqual(0, exitOrder, "Exit hook should fire first");
            Assert.AreEqual(1, entryOrder, "Entry hook should fire second");
        }

        [Test]
        public void UnregisteredHook_DoesNotFire()
        {
            int fireCount = 0;
            Action hook = () => fireCount++;
            _manager.RegisterEntryHook(GameState.MainMenu, hook);
            _manager.UnregisterEntryHook(GameState.MainMenu, hook);

            _manager.RequestTransition(GameState.MainMenu, "test");

            Assert.AreEqual(0, fireCount);
        }

        // --- Event channel ---

        [Test]
        public void StateChangedEvent_ContainsCorrectPreviousAndNewState()
        {
            GameStateEventChannel.StateChangeArgs? received = null;
            _channel.Register(args => received = args);

            _manager.RequestTransition(GameState.MainMenu, "test");

            Assert.IsNotNull(received);
            Assert.AreEqual(GameState.TitleScreen, received.Value.PreviousState);
            Assert.AreEqual(GameState.MainMenu, received.Value.NewState);
        }

        // --- Context tracking ---

        [Test]
        public void SaveMenu_StoresReturnContext()
        {
            _manager.RequestTransition(GameState.MainMenu, "test");
            _manager.ResetFrameGate();
            _manager.RequestTransition(GameState.BattleMap, "test");
            _manager.ResetFrameGate();
            _manager.RequestTransition(GameState.BattleMapPaused, "test");
            _manager.ResetFrameGate();

            _manager.RequestTransition(GameState.SaveMenu, "test");

            Assert.AreEqual(GameState.BattleMapPaused, _manager.ReturnContext);
        }

        [Test]
        public void SettingsMenu_StoresReturnContext()
        {
            _manager.RequestTransition(GameState.MainMenu, "test");
            _manager.ResetFrameGate();
            _manager.RequestTransition(GameState.BattleMap, "test");
            _manager.ResetFrameGate();
            _manager.RequestTransition(GameState.BattleMapPaused, "test");
            _manager.ResetFrameGate();

            _manager.RequestTransition(GameState.SettingsMenu, "test");

            Assert.AreEqual(GameState.BattleMapPaused, _manager.ReturnContext);
        }

        [Test]
        public void ReturnFromContextMenu_TransitionsToStoredContext()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            _manager.ForceState(GameState.BattleMapPaused, "test setup");
            _manager.ResetFrameGate();
            _manager.RequestTransition(GameState.SaveMenu, "test");
            _manager.ResetFrameGate();

            bool result = _manager.ReturnFromContextMenu("test");

            Assert.IsTrue(result);
            Assert.AreEqual(GameState.BattleMapPaused, _manager.CurrentState);
        }

        [Test]
        public void ReturnFromContextMenu_RejectsIfNotInContextMenuState()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] ReturnFromContextMenu called from invalid state"));
            bool result = _manager.ReturnFromContextMenu("test");
            Assert.IsFalse(result);
        }

        // --- ForceState ---

        [Test]
        public void ForceState_BypassesTransitionTable()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            _manager.ForceState(GameState.BattleMap, "test recovery");
            Assert.AreEqual(GameState.BattleMap, _manager.CurrentState);
        }

        [Test]
        public void ForceState_FiresHooks()
        {
            bool exitFired = false;
            bool entryFired = false;
            _manager.RegisterExitHook(GameState.TitleScreen, () => exitFired = true);
            _manager.RegisterEntryHook(GameState.BattleMap, () => entryFired = true);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            _manager.ForceState(GameState.BattleMap, "test");

            Assert.IsTrue(exitFired);
            Assert.IsTrue(entryFired);
        }

        [Test]
        public void ForceState_RaisesEvent()
        {
            GameStateEventChannel.StateChangeArgs? received = null;
            _channel.Register(args => received = args);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameStateManager\] FORCED state change"));
            _manager.ForceState(GameState.MainMenu, "test");

            Assert.IsNotNull(received);
            Assert.AreEqual(GameState.TitleScreen, received.Value.PreviousState);
            Assert.AreEqual(GameState.MainMenu, received.Value.NewState);
        }
    }
}
