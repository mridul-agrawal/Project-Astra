using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Tests.Hub.Interaction
{
    // The GDD's contextual-button rules, section 4.5, pinned against the controller.
    [TestFixture]
    public class PlayerInteractionControllerTests
    {
        private PlayerInteractionController controller;
        private InteractorPose facingNorth;

        [SetUp]
        public void SetUp()
        {
            controller = new PlayerInteractionController();
            facingNorth = new InteractorPose(new Vector2(5f, 5f), Facing.North);
        }

        private FakeInteractable Add(string id, InteractionPriority priority = InteractionPriority.Inspectable,
            float x = 5f, float y = 6f, bool available = true, bool reachable = true)
        {
            var fake = new FakeInteractable(id, priority, new Vector2(x, y))
            {
                IsAvailable = available,
                Reachable = reachable
            };
            controller.Add(fake);
            return fake;
        }


        // One press, one target.

        [Test]
        public void WithNothingInRange_NothingIsTargeted()
        {
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.IsNull(controller.CurrentTarget);
        }

        [Test]
        public void PressingWithNoTarget_DoesNothingAtAll()
        {
            var behind = Add("behind", y: 4f, reachable: false);

            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(0, behind.InteractCount, "no target means no dialogue, no state change, no progress");
        }

        [Test]
        public void APressActsOnTheOneTarget()
        {
            var shelf = Add("shelf");

            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(1, shelf.InteractCount);
        }

        [Test]
        public void OnlyOneOfSeveralOverlappingTargetsIsActedOn()
        {
            var a = Add("a", InteractionPriority.Character);
            var b = Add("b", InteractionPriority.Door);
            var c = Add("c", InteractionPriority.Inspectable);

            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(1, a.InteractCount + b.InteractCount + c.InteractCount,
                "never activate every overlapping target from one press");
        }


        // The priority ladder.

        [Test]
        public void ACharacterBeatsEverythingElse()
        {
            Add("door", InteractionPriority.Door);
            var character = Add("character", InteractionPriority.Character);
            Add("shelf", InteractionPriority.Inspectable);

            Assert.AreSame(character, controller.ResolvePriority(facingNorth));
        }

        [Test]
        public void AnObjectiveObjectBeatsADoor()
        {
            Add("door", InteractionPriority.Door);
            var objective = Add("report", InteractionPriority.ObjectiveObject);

            Assert.AreSame(objective, controller.ResolvePriority(facingNorth));
        }

        [Test]
        public void ADoorBeatsAPlainInspectable()
        {
            Add("shelf", InteractionPriority.Inspectable);
            var door = Add("door", InteractionPriority.Door);

            Assert.AreSame(door, controller.ResolvePriority(facingNorth));
        }

        [Test]
        public void OnTheSameRung_MostDirectlyInFrontWins()
        {
            Add("offToTheSide", InteractionPriority.Inspectable, x: 5.5f, y: 6f);
            var ahead = Add("deadAhead", InteractionPriority.Inspectable, x: 5f, y: 6f);

            Assert.AreSame(ahead, controller.ResolvePriority(facingNorth));
        }

        [Test]
        public void EquallyInFront_TheNearerOneWins()
        {
            Add("far", InteractionPriority.Inspectable, x: 5f, y: 8f);
            var near = Add("near", InteractionPriority.Inspectable, x: 5f, y: 6f);

            Assert.AreSame(near, controller.ResolvePriority(facingNorth));
        }


        // Availability and reach.

        [Test]
        public void SomethingUnavailable_IsNotATargetAtAll()
        {
            Add("silentCharacter", InteractionPriority.Character, available: false);

            Assert.IsNull(controller.ResolvePriority(facingNorth),
                "a character with no conversation shows no prompt");
        }

        [Test]
        public void SomethingInRangeButUnreachable_IsNotATarget()
        {
            Add("throughAWall", reachable: false);

            Assert.IsNull(controller.ResolvePriority(facingNorth));
        }

        // A locked thing is still a target — it owes her an answer.
        [Test]
        public void ALockedTargetIsStillTargetedAndStillActedOn()
        {
            var lockedDoor = Add("lockedDoor", InteractionPriority.Door);

            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreSame(lockedDoor, controller.CurrentTarget);
            Assert.AreEqual(1, lockedDoor.InteractCount, "it answers with a denial line rather than failing silently");
        }

        [Test]
        public void ATargetThatGoesUnavailableBeforeThePress_IsNotActedOn()
        {
            var shelf = Add("shelf");
            controller.Tick(facingNorth, confirmHeld: false);
            Assert.AreSame(shelf, controller.CurrentTarget);

            shelf.IsAvailable = false;
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(0, shelf.InteractCount, "cancel cleanly rather than acting on a stale target");
            Assert.IsNull(controller.CurrentTarget);
        }


        // The button itself.

        [Test]
        public void HoldingTheButton_DoesNotRetrigger()
        {
            var shelf = Add("shelf");

            controller.Tick(facingNorth, confirmHeld: true);
            controller.Tick(facingNorth, confirmHeld: true);
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(1, shelf.InteractCount, "holding is one activation, not many");
        }

        [Test]
        public void ReleasingAndPressingAgain_ActsAgain()
        {
            var shelf = Add("shelf");

            controller.Tick(facingNorth, confirmHeld: true);
            controller.Tick(facingNorth, confirmHeld: false);
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(2, shelf.InteractCount);
        }

        // A press held through a door or a closing dialogue must not count on the other side.
        [Test]
        public void ASuppressedPress_StaysDeadUntilRelease()
        {
            var shelf = Add("shelf");
            controller.SuppressCurrentPress();

            controller.Tick(facingNorth, confirmHeld: true);
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(0, shelf.InteractCount);

            controller.Tick(facingNorth, confirmHeld: false);
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(1, shelf.InteractCount, "and works normally once she lets go");
        }


        // Being switched off — the spec's first priority rung, honoured by silence.

        [Test]
        public void WhileDisabled_NothingIsTargetedAndNothingFires()
        {
            var shelf = Add("shelf");
            controller.Enabled = false;

            controller.Tick(facingNorth, confirmHeld: true);

            Assert.IsNull(controller.CurrentTarget);
            Assert.AreEqual(0, shelf.InteractCount);
        }

        [Test]
        public void APressHeldWhileDisabled_DoesNotFireOnReenable()
        {
            var shelf = Add("shelf");
            controller.Enabled = false;
            controller.Tick(facingNorth, confirmHeld: true);

            controller.Enabled = true;
            controller.Tick(facingNorth, confirmHeld: true);

            Assert.AreEqual(0, shelf.InteractCount, "the latch was consumed while nothing was listening");
        }


        // The list, and what the prompt hears about.

        [Test]
        public void SomethingLeavingRange_IsNoLongerTargeted()
        {
            var shelf = Add("shelf");
            controller.Tick(facingNorth, confirmHeld: false);

            controller.Remove(shelf);

            Assert.IsNull(controller.CurrentTarget, "a prompt must never outlive its target");
        }

        [Test]
        public void AddingTheSameThingTwice_KeepsOneEntry()
        {
            var shelf = Add("shelf");
            controller.Add(shelf);

            Assert.AreEqual(1, controller.TargetsInRange.Count);
        }

        [Test]
        public void TargetChanged_FiresOnlyWhenItActuallyChanges()
        {
            var seen = new List<string>();
            controller.TargetChanged += t => seen.Add(t == null ? "none" : ((FakeInteractable)t).Id);
            var shelf = Add("shelf");

            controller.Tick(facingNorth, confirmHeld: false);
            controller.Tick(facingNorth, confirmHeld: false);
            controller.Remove(shelf);

            CollectionAssert.AreEqual(new[] { "shelf", "none" }, seen);
        }


        private sealed class FakeInteractable : IInteractable
        {
            public FakeInteractable(string id, InteractionPriority priority, Vector2 point)
            {
                Id = id;
                Priority = priority;
                InteractionPoint = point;
            }

            public string Id { get; }
            public bool Reachable { get; set; } = true;
            public int InteractCount { get; private set; }

            public HubVerb Verb => HubVerb.Inspect;
            public bool IsAvailable { get; set; } = true;
            public InteractionPriority Priority { get; }
            public Vector2 InteractionPoint { get; }

            public bool CanReach(InteractorPose player) => Reachable;
            public void Interact(InteractorPose player) => InteractCount++;
        }
    }
}
