using NUnit.Framework;
using UnityEditor;
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
            Author(door, new HubDoor { doorId = "exit", position = Vector2.zero, verb = HubVerb.Leave });
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

        // A door in the room is always worth walking up to; a shut one owes her an answer.
        [Test]
        public void ADoorIsAlwaysAvailable()
        {
            Assert.IsTrue(door.IsAvailable);
        }

        // A door is authored in its room, so the test stands in for the inspector.
        private static void Author(DoorInteractable target, HubDoor authored)
        {
            var serialized = new UnityEditor.SerializedObject(target);
            SerializedProperty field = serialized.FindProperty("door");
            field.FindPropertyRelative("doorId").stringValue = authored.doorId;
            field.FindPropertyRelative("position").vector2Value = authored.position;
            field.FindPropertyRelative("verb").enumValueIndex = (int)authored.verb;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
