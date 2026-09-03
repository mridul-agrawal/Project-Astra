using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Tests.Hub
{
    // "She is standing right there and the button does nothing" is the question this answers, so a
    // wrong answer sends a designer looking in the wrong place.
    [TestFixture]
    public class HubReachReportTests
    {
        private readonly List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in spawned)
                if (made != null) Object.DestroyImmediate(made, true);
            spawned.Clear();
            Undo.ClearAll();
        }

        [Test]
        public void SomethingWithNothingToSayIsSaidToHaveNothing()
        {
            Stub thing = Thing(available: false, reachable: true);

            Assert.AreEqual(HubReachReport.NothingToOffer, HubReachReport.Why(thing, LookingAt(thing), thing));
        }

        [Test]
        public void SomethingBehindHerIsSaidToBeBehindHer()
        {
            Stub thing = Thing(available: true, reachable: false);
            // Past it and still looking north, so it is behind her.
            var lookingAway = new InteractorPose(new Vector2(0f, 7f), Facing.North);

            Assert.AreEqual(HubReachReport.NotFacing, HubReachReport.Why(thing, lookingAway, null));
        }

        // Facing it and still out of reach means a wall, which is a different fix entirely.
        [Test]
        public void SomethingFacedButUnreachableIsSaidToBeBlocked()
        {
            Stub thing = Thing(available: true, reachable: false);

            Assert.AreEqual(HubReachReport.InTheWay, HubReachReport.Why(thing, LookingAt(thing), null));
        }

        [Test]
        public void TheOneThatWonIsSaidToBeTheOneThatWon()
        {
            Stub thing = Thing(available: true, reachable: true);

            Assert.AreEqual(HubReachReport.Offered, HubReachReport.Why(thing, LookingAt(thing), thing));
        }

        [Test]
        public void SomethingOutrankedNamesWhatBeatIt()
        {
            Stub thing = Thing(available: true, reachable: true);
            Stub winner = Thing(available: true, reachable: true, named: "Door");

            Assert.IsTrue(HubReachReport.Why(thing, LookingAt(thing), winner).Contains("Door"));
        }

        [Test]
        public void NothingAtAllIsSaidPlainly()
        {
            Assert.AreEqual("there is nothing there",
                HubReachReport.Why(null, new InteractorPose(Vector2.zero, Facing.South), null));
        }

        private static InteractorPose LookingAt(Stub thing) =>
            new(thing.InteractionPoint + Vector2.down, Facing.North);

        private Stub Thing(bool available, bool reachable, string named = "Thing")
        {
            var host = new GameObject(named);
            spawned.Add(host);
            host.transform.position = new Vector2(0f, 6f);

            Stub stub = host.AddComponent<Stub>();
            stub.Available = available;
            stub.Reachable = reachable;
            return stub;
        }

        // Stands in for a real interactable so the report can be asked about each answer in turn.
        private sealed class Stub : MonoBehaviour, IInteractable
        {
            public bool Available;
            public bool Reachable;

            public HubVerb Verb => HubVerb.Inspect;
            public bool IsAvailable => Available;
            public InteractionPriority Priority => InteractionPriority.Inspectable;
            public Vector2 InteractionPoint => transform.position;

            public bool CanReach(InteractorPose player) => Reachable;
            public void Interact(InteractorPose player) { }
        }
    }
}
