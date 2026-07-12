using System.Collections.Generic;
using NUnit.Framework;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Tests.Dialogue
{
    [TestFixture]
    public class DialogueTriggerTests
    {
        private DialogueScript scriptA;
        private DialogueScript scriptB;

        [SetUp]
        public void SetUp()
        {
            scriptA = DialogueScript.CreateForTest("A");
            scriptB = DialogueScript.CreateForTest("B");
        }

        private static DialogueTriggerSet Set(params DialogueTrigger[] triggers)
            => new DialogueTriggerSet(new List<DialogueTrigger>(triggers));

        [Test]
        public void Resolve_ReturnsScriptForMatchingEvent()
        {
            var set = Set(DialogueTrigger.CreateForTest(BattleDialogueEventType.UnitSelected, scriptA));
            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.UnitSelected, 1));
        }

        [Test]
        public void Resolve_ReturnsNullForUnmatchedEvent()
        {
            var set = Set(DialogueTrigger.CreateForTest(BattleDialogueEventType.UnitSelected, scriptA));
            Assert.IsNull(set.Resolve(BattleDialogueEventType.PreCombat, 1));
        }

        [Test]
        public void FireOnce_DoesNotFireTwice()
        {
            var set = Set(DialogueTrigger.CreateForTest(BattleDialogueEventType.PreCombat, scriptA, fireOnce: true));

            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.PreCombat, 1));
            Assert.IsNull(set.Resolve(BattleDialogueEventType.PreCombat, 1), "a fire-once trigger retires after firing");
        }

        [Test]
        public void Repeating_FiresEveryTime()
        {
            var set = Set(DialogueTrigger.CreateForTest(BattleDialogueEventType.MoveConfirmed, scriptA, fireOnce: false));

            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.MoveConfirmed, 1));
            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.MoveConfirmed, 2));
        }

        [Test]
        public void TurnFilter_OnlyFiresOnTheNamedPlayerPhaseTurn()
        {
            var set = Set(DialogueTrigger.CreateForTest(
                BattleDialogueEventType.PlayerPhaseStarted, scriptA, fireOnce: true, turnFilter: 2));

            Assert.IsNull(set.Resolve(BattleDialogueEventType.PlayerPhaseStarted, 1), "turn 1 doesn't match a turn-2 filter");
            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.PlayerPhaseStarted, 2));
        }

        [Test]
        public void TurnFilterZero_FiresOnAnyTurn()
        {
            var set = Set(DialogueTrigger.CreateForTest(
                BattleDialogueEventType.PlayerPhaseStarted, scriptA, fireOnce: false, turnFilter: 0));

            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.PlayerPhaseStarted, 5));
        }

        [Test]
        public void Resolve_FirstUnspentMatchWins()
        {
            var first = DialogueTrigger.CreateForTest(BattleDialogueEventType.UnitSelected, scriptA, fireOnce: true);
            var second = DialogueTrigger.CreateForTest(BattleDialogueEventType.UnitSelected, scriptB, fireOnce: true);
            var set = Set(first, second);

            Assert.AreSame(scriptA, set.Resolve(BattleDialogueEventType.UnitSelected, 1), "first match fires first");
            Assert.AreSame(scriptB, set.Resolve(BattleDialogueEventType.UnitSelected, 1), "then the next on a later event");
        }

        [Test]
        public void NullScriptTrigger_IsIgnored()
        {
            var set = Set(DialogueTrigger.CreateForTest(BattleDialogueEventType.UnitSelected, null));
            Assert.IsNull(set.Resolve(BattleDialogueEventType.UnitSelected, 1));
        }
    }
}
