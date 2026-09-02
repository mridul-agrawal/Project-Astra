using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // The state that has to survive a doorway: which house she is in, and the way back out of it.
    // Six student houses share one interior, so both of those are the difference between exiting
    // into her own street and exiting into someone else's.
    [TestFixture]
    public class HubRuntimeStateTests
    {
        private HubRuntimeState state;

        [SetUp]
        public void SetUp() => state = new HubRuntimeState("hub1");

        [Test]
        public void EnteringARoom_RecordsWhereSheIs()
        {
            state.EnterLocation("library", null);

            Assert.AreEqual("library", state.CurrentLocationId);
        }

        [Test]
        public void EnteringAHouse_RecordsWhoseItIs()
        {
            state.EnterLocation("student_house", "house_madhavi");

            Assert.AreEqual("house_madhavi", state.HouseIdentity);
        }

        // Walking between rooms that aren't houses must not wipe whose house she is in, or the exit
        // would forget where to put her.
        [Test]
        public void MovingToARoomWithNoIdentity_KeepsTheLastOne()
        {
            state.EnterLocation("student_house", "house_madhavi");
            state.EnterLocation("courtyard", null);

            Assert.AreEqual("house_madhavi", state.HouseIdentity);
        }

        [Test]
        public void EnteringADifferentHouse_ReplacesTheIdentity()
        {
            state.EnterLocation("student_house", "house_madhavi");
            state.EnterLocation("student_house", "house_rudrani");

            Assert.AreEqual("house_rudrani", state.HouseIdentity);
        }

        [Test]
        public void WithNothingRecorded_ThereIsNoWayBack()
        {
            Assert.IsFalse(state.TryGetReturn(out _, out _, out _));
        }

        [Test]
        public void RememberingTheWayBack_ReturnsHerToWhereSheStood()
        {
            state.RememberReturn("courtyard", new Vector2(12f, 3f), Facing.South);

            Assert.IsTrue(state.TryGetReturn(out string locationId, out Vector2 spawn, out Facing facing));
            Assert.AreEqual("courtyard", locationId);
            Assert.AreEqual(new Vector2(12f, 3f), spawn);
            Assert.AreEqual(Facing.South, facing);
        }

        // Going into a second house overwrites the first way back, so leaving lands her outside the
        // house she actually just left.
        [Test]
        public void EnteringASecondHouse_ReplacesTheWayBack()
        {
            state.RememberReturn("courtyard", new Vector2(4f, 3f), Facing.South);
            state.RememberReturn("courtyard", new Vector2(20f, 3f), Facing.South);

            state.TryGetReturn(out _, out Vector2 spawn, out _);
            Assert.AreEqual(new Vector2(20f, 3f), spawn);
        }

        // Gates and interactable states are what an objective's effects write into, and what a door
        // reads to decide whether it opens.
        [Test]
        public void AGateStartsShutAndCanBeOpened()
        {
            Assert.IsFalse(state.IsGateOpen("guru_door"));

            state.SetGate("guru_door", true);
            Assert.IsTrue(state.IsGateOpen("guru_door"));

            state.SetGate("guru_door", false);
            Assert.IsFalse(state.IsGateOpen("guru_door"));
        }

        [Test]
        public void AnUnmentionedInteractable_FallsBackToItsAuthoredState()
        {
            Assert.AreEqual(HubInteractableState.Available,
                state.GetInteractableState("blackboard", HubInteractableState.Available));

            state.SetInteractableState("blackboard", HubInteractableState.Exhausted);
            Assert.AreEqual(HubInteractableState.Exhausted,
                state.GetInteractableState("blackboard", HubInteractableState.Available));
        }

        [Test]
        public void SettingAnInteractableTwice_ReplacesRatherThanStacks()
        {
            state.SetInteractableState("door", HubInteractableState.Gated);
            state.SetInteractableState("door", HubInteractableState.Available);

            Assert.AreEqual(HubInteractableState.Available,
                state.GetInteractableState("door", HubInteractableState.Inactive));
        }
    }
}
