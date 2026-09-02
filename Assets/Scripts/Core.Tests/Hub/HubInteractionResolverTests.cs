using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // The spec's rule is that one press activates exactly one thing, so most of this is about which
    // candidate wins when several are in reach.
    [TestFixture]
    public class HubInteractionResolverTests
    {
        private static readonly Vector2 Player = new(4f, 4f);

        private HubCollisionMap map;

        [SetUp]
        public void SetUp() => map = new HubCollisionMap(20, 20);   // 10x10 tiles, all clear

        private static HubInteractionCandidate Candidate(string id, HubTargetKind kind, Vector2 at,
            HubVerb verb = HubVerb.Talk) => new(id, kind, at, verb);

        private bool Resolve(Facing facing, out HubInteractionCandidate chosen,
            params HubInteractionCandidate[] candidates) =>
            HubInteractionResolver.TryResolve(Player, facing, new List<HubInteractionCandidate>(candidates),
                map, out chosen);

        // Reach and facing

        [Test]
        public void SomethingDirectlyAhead_IsInReach()
        {
            Assert.IsTrue(Resolve(Facing.North, out var chosen,
                Candidate("kaal", HubTargetKind.Character, new Vector2(4f, 4.9f))));
            Assert.AreEqual("kaal", chosen.Id);
        }

        [Test]
        public void SomethingBehindHer_IsNotInReach()
        {
            Assert.IsFalse(Resolve(Facing.North, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(4f, 3.2f))));
        }

        [Test]
        public void SomethingTooFarAhead_IsNotInReach()
        {
            Assert.IsFalse(Resolve(Facing.North, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(4f, 5.4f))));
        }

        // Standing slightly off to one side still counts — the spec is explicit that pixel-perfect
        // alignment must not be required.
        [Test]
        public void SlightlyOffToTheSide_StillCounts()
        {
            Assert.IsTrue(Resolve(Facing.North, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(4.45f, 4.8f))));
        }

        [Test]
        public void WellOffToTheSide_DoesNot()
        {
            Assert.IsFalse(Resolve(Facing.North, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(5.1f, 4.8f))));
        }

        [TestCase(Facing.North, 4f, 4.8f)]
        [TestCase(Facing.South, 4f, 3.2f)]
        [TestCase(Facing.East, 4.8f, 4f)]
        [TestCase(Facing.West, 3.2f, 4f)]
        public void EachSide_CanBeInteractedFrom(Facing facing, float x, float y)
        {
            Assert.IsTrue(Resolve(facing, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(x, y))));
        }

        // Priority ladder

        [Test]
        public void ACharacter_BeatsEverythingElse()
        {
            Resolve(Facing.North, out var chosen,
                Candidate("shelf", HubTargetKind.Inspectable, new Vector2(4f, 4.7f)),
                Candidate("door", HubTargetKind.Door, new Vector2(4.1f, 4.7f)),
                Candidate("card", HubTargetKind.ObjectiveObject, new Vector2(3.9f, 4.7f)),
                Candidate("kaal", HubTargetKind.Character, new Vector2(4.2f, 4.7f)));

            Assert.AreEqual("kaal", chosen.Id);
        }

        [Test]
        public void AnObjectiveObject_BeatsADoorAndAPlainInspectable()
        {
            Resolve(Facing.North, out var chosen,
                Candidate("shelf", HubTargetKind.Inspectable, new Vector2(4f, 4.7f)),
                Candidate("door", HubTargetKind.Door, new Vector2(4.1f, 4.7f)),
                Candidate("card", HubTargetKind.ObjectiveObject, new Vector2(4.2f, 4.7f)));

            Assert.AreEqual("card", chosen.Id);
        }

        [Test]
        public void ADoor_BeatsAPlainInspectable()
        {
            Resolve(Facing.North, out var chosen,
                Candidate("shelf", HubTargetKind.Inspectable, new Vector2(4f, 4.7f)),
                Candidate("door", HubTargetKind.Door, new Vector2(4.2f, 4.7f)));

            Assert.AreEqual("door", chosen.Id);
        }

        // Tie-breaks within one rung

        [Test]
        public void AtTheSameRung_TheOneMostDirectlyInFrontWins()
        {
            Resolve(Facing.North, out var chosen,
                Candidate("offToTheSide", HubTargetKind.Character, new Vector2(4.5f, 4.6f)),
                Candidate("straightAhead", HubTargetKind.Character, new Vector2(4.02f, 4.9f)));

            Assert.AreEqual("straightAhead", chosen.Id,
                "Being lined up should beat being closer.");
        }

        [Test]
        public void EquallyInFront_TheNearerOneWins()
        {
            Resolve(Facing.North, out var chosen,
                Candidate("far", HubTargetKind.Character, new Vector2(4f, 4.9f)),
                Candidate("near", HubTargetKind.Character, new Vector2(4f, 4.4f)));

            Assert.AreEqual("near", chosen.Id);
        }

        // Line of sight

        [Test]
        public void AWallBetweenThem_BlocksTheInteraction()
        {
            // Cell (8,9) covers world x 4.0-4.5, y 4.5-5.0 — right between her and the target.
            map.SetGroundBlocked(8, 9, true);

            Assert.IsFalse(Resolve(Facing.North, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(4f, 5.0f))));
        }

        // A character standing right in front of her is solid, and must not count as blocking the
        // view of itself.
        [Test]
        public void TheTargetsOwnBody_DoesNotBlockIt()
        {
            map.Stamp(new Rect(3.9f, 4.7f, 0.5f, 0.5f));

            Assert.IsTrue(Resolve(Facing.North, out _,
                Candidate("kaal", HubTargetKind.Character, new Vector2(4f, 4.8f))));
        }

        [Test]
        public void NothingInReach_ResolvesToNothing()
        {
            Assert.IsFalse(Resolve(Facing.North, out var chosen));
            Assert.IsFalse(chosen.IsValid);
        }

        [Test]
        public void TheVerbTravelsWithTheChosenTarget()
        {
            Resolve(Facing.North, out var chosen,
                Candidate("door", HubTargetKind.Door, new Vector2(4f, 4.8f), HubVerb.Enter));

            Assert.AreEqual(HubVerb.Enter, chosen.Verb);
        }
    }
}
