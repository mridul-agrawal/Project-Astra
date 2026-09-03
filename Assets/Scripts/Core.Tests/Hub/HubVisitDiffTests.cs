using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // Flipping between two visits shows the difference. This says it, and a designer trusts it to
    // be complete, so what it stays quiet about matters as much as what it reports.
    [TestFixture]
    public class HubVisitDiffTests
    {
        private readonly List<Object> made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object one in made) Object.DestroyImmediate(one);
            made.Clear();
        }

        [Test]
        public void TwoIdenticalVisitsDifferInNothing()
        {
            HubVisitData first = Visit("one", Standing("arjun", 2, 2));
            HubVisitData second = Visit("two", Standing("arjun", 2, 2));

            CollectionAssert.Contains(HubVisitDiff.Describe(first, second),
                "Nothing differs from the visit before.");
        }

        [Test]
        public void SomeoneNewIsReported()
        {
            HubVisitData first = Visit("one", Standing("arjun", 2, 2));
            HubVisitData second = Visit("two", Standing("arjun", 2, 2), Standing("kaal", 5, 5));

            Assert.IsTrue(Says(first, second, "kaal is here now"));
        }

        [Test]
        public void SomeoneMissingIsReported()
        {
            HubVisitData first = Visit("one", Standing("arjun", 2, 2), Standing("kaal", 5, 5));
            HubVisitData second = Visit("two", Standing("arjun", 2, 2));

            Assert.IsTrue(Says(first, second, "kaal has gone"));
        }

        [Test]
        public void SomeoneStandingElsewhereIsReported()
        {
            HubVisitData first = Visit("one", Standing("arjun", 2, 2));
            HubVisitData second = Visit("two", Standing("arjun", 9, 9));

            Assert.IsTrue(Says(first, second, "arjun stands somewhere else"));
        }

        // Moving rooms is a bigger change than moving a few tiles, and is said as such.
        [Test]
        public void SomeoneInAnotherRoomIsReportedAsThat()
        {
            HubVisitData first = Visit("one", Standing("arjun", 2, 2));
            HubVisitData second = Visit("two", Standing("arjun", 2, 2, room: "the_hall"));

            Assert.IsTrue(Says(first, second, "arjun has moved to the_hall"));
            Assert.IsFalse(Says(first, second, "arjun stands somewhere else"));
        }

        [Test]
        public void SomeoneWithNewLinesIsReported()
        {
            HubVisitData first = Visit("one", Standing("arjun", 2, 2, says: "hello"));
            HubVisitData second = Visit("two", Standing("arjun", 2, 2, says: "goodbye"));

            Assert.IsTrue(Says(first, second, "arjun says something else"));
        }

        [Test]
        public void AGateThatNowStartsOpenIsReported()
        {
            HubVisitData first = Visit("one");
            HubVisitData second = Visit("two", gates: new[] { "north_road" });

            Assert.IsTrue(Says(first, second, "'north_road' starts open"));
        }

        [Test]
        public void AGateThatHasShutAgainIsReported()
        {
            HubVisitData first = Visit("one", gates: new[] { "north_road" });
            HubVisitData second = Visit("two");

            Assert.IsTrue(Says(first, second, "'north_road' starts shut again"));
        }

        [Test]
        public void AnObjectInANewStateIsReported()
        {
            HubVisitData first = Visit("one");
            HubVisitData second = Visit("two", changes: Changed("noticeboard", HubInteractableState.Gated));

            Assert.IsTrue(Says(first, second, "noticeboard is Gated"));
        }

        // An override both visits share is not a change, and saying so every time would bury the
        // ones that are.
        [Test]
        public void AnObjectLeftAsItWasIsNotReported()
        {
            HubVisitData first = Visit("one", changes: Changed("noticeboard", HubInteractableState.Gated));
            HubVisitData second = Visit("two", changes: Changed("noticeboard", HubInteractableState.Gated));

            CollectionAssert.Contains(HubVisitDiff.Describe(first, second),
                "Nothing differs from the visit before.");
        }

        [Test]
        public void AVisitThatOpensSomewhereElseIsReported()
        {
            HubVisitData first = Visit("one");
            HubVisitData second = Visit("two", opensIn: "the_hall");

            Assert.IsTrue(Says(first, second, "opens in the_hall"));
        }

        [Test]
        public void NothingIsSaidAboutAVisitThatIsNotThere()
        {
            Assert.IsEmpty(HubVisitDiff.Describe(null, Visit("two")));
            Assert.IsEmpty(HubVisitDiff.Describe(Visit("one"), null));
        }

        private static bool Says(HubVisitData before, HubVisitData after, string expected) =>
            HubVisitDiff.Describe(before, after).Contains(expected);

        private static HubCharacterPlacement Standing(string who, float x, float y,
            string room = "courtyard", string says = null) =>
            new()
            {
                characterId = who,
                locationId = room,
                position = new Vector2(x, y),
                facing = Facing.South,
                conversationId = says
            };

        private static HubInteractableOverride[] Changed(string what, HubInteractableState state) =>
            new[] { new HubInteractableOverride { interactableId = what, state = state } };

        private HubVisitData Visit(string id, params HubCharacterPlacement[] cast) =>
            Visit(id, cast, null, null, "courtyard");

        private HubVisitData Visit(string id, string[] gates = null,
            HubInteractableOverride[] changes = null, string opensIn = "courtyard") =>
            Visit(id, System.Array.Empty<HubCharacterPlacement>(), gates, changes, opensIn);

        private HubVisitData Visit(string id, HubCharacterPlacement[] cast, string[] gates,
            HubInteractableOverride[] changes, string opensIn)
        {
            var visit = ScriptableObject.CreateInstance<HubVisitData>();
            made.Add(visit);

            var editable = new SerializedObject(visit);
            editable.FindProperty("visitId").stringValue = id;
            editable.FindProperty("startLocationId").stringValue = opensIn;
            Fill(editable.FindProperty("characterPlacements"), cast, WritePlacement);
            Fill(editable.FindProperty("openGates"), gates, (p, gate) => p.stringValue = gate);
            Fill(editable.FindProperty("interactableOverrides"), changes, WriteOverride);
            editable.ApplyModifiedPropertiesWithoutUndo();

            return visit;
        }

        private static void Fill<T>(SerializedProperty list, T[] items,
            System.Action<SerializedProperty, T> write)
        {
            list.arraySize = items?.Length ?? 0;
            for (int i = 0; i < list.arraySize; i++) write(list.GetArrayElementAtIndex(i), items[i]);
        }

        private static void WritePlacement(SerializedProperty entry, HubCharacterPlacement placement)
        {
            entry.FindPropertyRelative("characterId").stringValue = placement.characterId;
            entry.FindPropertyRelative("locationId").stringValue = placement.locationId;
            entry.FindPropertyRelative("position").vector2Value = placement.position;
            entry.FindPropertyRelative("conversationId").stringValue = placement.conversationId;
        }

        private static void WriteOverride(SerializedProperty entry, HubInteractableOverride change)
        {
            entry.FindPropertyRelative("interactableId").stringValue = change.interactableId;
            entry.FindPropertyRelative("state").enumValueIndex = (int)change.state;
        }
    }
}
