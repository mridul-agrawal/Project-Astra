using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Tests.Hub.Interaction
{
    [TestFixture]
    public class DoorInteractableTests
    {
        private GameObject go;
        private DoorInteractable door;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Door");
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            door = go.AddComponent<DoorInteractable>();
            door.Configure(new HubDoor { doorId = "exit", position = Vector2.zero, verb = HubVerb.Leave });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        private static InteractorPose StandingAt(float x, float y, Facing facing) =>
            new(new Vector2(x, y), facing);

        // The wall a door is set into sits between her and it, so a door must not ask for sight.

        [Test]
        public void StandingAtTheDoorFacingIt_CanReach()
        {
            Assert.IsTrue(door.CanReach(StandingAt(0f, 0.7f, Facing.South)));
        }

        [Test]
        public void StandingCloserThanTheSightCheckWouldAllow_StillCanReach()
        {
            Assert.IsTrue(door.CanReach(StandingAt(0f, 0.3f, Facing.South)));
        }

        [Test]
        public void FacingAwayFromIt_CannotReach()
        {
            Assert.IsFalse(door.CanReach(StandingAt(0f, 0.7f, Facing.North)));
        }

        [Test]
        public void StandingWellOffToOneSide_CannotReach()
        {
            Assert.IsFalse(door.CanReach(StandingAt(1.5f, 0.7f, Facing.South)));
        }

        // A shut door is still worth walking up to — it owes her an answer rather than silence.

        [Test]
        public void AnUnconfiguredDoor_IsNotAvailable()
        {
            var bare = new GameObject("Bare");
            bare.AddComponent<CircleCollider2D>().isTrigger = true;
            var unconfigured = bare.AddComponent<DoorInteractable>();

            Assert.IsFalse(unconfigured.IsAvailable);
            Object.DestroyImmediate(bare);
        }

        [Test]
        public void AConfiguredDoor_IsAvailable()
        {
            Assert.IsTrue(door.IsAvailable);
        }

        [Test]
        public void ItsVerbAndIdComeFromTheAuthoredDoor()
        {
            Assert.AreEqual(HubVerb.Leave, door.Verb);
            Assert.AreEqual("exit", door.DoorId);
        }

        [Test]
        public void ItSitsOnTheDoorRungOfTheLadder()
        {
            Assert.AreEqual(InteractionPriority.Door, door.Priority);
        }
    }
}
