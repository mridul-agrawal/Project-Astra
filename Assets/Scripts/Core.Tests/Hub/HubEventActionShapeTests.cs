using System;
using NUnit.Framework;
using ProjectAstra.Core;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Tests.Hub
{
    // The shape table decides which of an action's fields a designer is shown. If it disagrees with
    // HubEventRunner, the inspector offers a field the game never reads — so it is pinned here.
    [TestFixture]
    public class HubEventActionShapeTests
    {
        [Test]
        public void EveryKindSaysWhatItDoes()
        {
            foreach (HubEventActionKind kind in Enum.GetValues(typeof(HubEventActionKind)))
                Assert.IsNotEmpty(HubEventActionShape.Of(kind).Summary, kind.ToString());
        }

        [Test]
        public void EveryKindShowsSomethingOrIsDeliberatelyBare()
        {
            foreach (HubEventActionKind kind in Enum.GetValues(typeof(HubEventActionKind)))
            {
                HubEventActionShape shape = HubEventActionShape.Of(kind);
                bool showsSomething = shape.UsesTarget || shape.UsesValue || shape.UsesPosition ||
                                      shape.UsesFacing || shape.UsesRoute || shape.UsesSeconds ||
                                      shape.UsesFlag || shape.UsesState;

                Assert.IsTrue(showsSomething, $"{kind} shows no fields at all");
            }
        }

        // The runner reads action.targetId for a gate, not valueId. Getting this backwards would
        // silently author gates nothing ever opens.
        [Test]
        public void AGateIsNamedInTheTargetField()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.SetGate);

            Assert.AreEqual(HubIdKind.Gate, shape.TargetKind);
            Assert.IsFalse(shape.UsesValue);
            Assert.IsTrue(shape.UsesFlag);
        }

        // A signal, by contrast, is read from valueId.
        [Test]
        public void ASignalIsNamedInTheValueField()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.RaiseFlag);

            Assert.AreEqual(HubIdKind.Signal, shape.ValueKind);
            Assert.IsFalse(shape.UsesTarget);
        }

        [Test]
        public void RelocatingSomeoneNeedsBothWhoAndWhere()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.RelocateCharacter);

            Assert.AreEqual(HubIdKind.Character, shape.TargetKind);
            Assert.AreEqual(HubIdKind.Location, shape.ValueKind);
            Assert.IsTrue(shape.UsesPosition);
            Assert.IsTrue(shape.UsesFacing);
        }

        [Test]
        public void WalkingNeedsARouteAndASpeed()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.WalkCharacter);

            Assert.IsTrue(shape.UsesRoute);
            Assert.IsTrue(shape.UsesSeconds);
            Assert.AreEqual(HubIdKind.Character, shape.TargetKind);
        }

        // Pointing the camera reads only who to point it at; offering a position or a duration
        // would be offering a setting that does nothing.
        [Test]
        public void PointingTheCameraOnlyAsksWho()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.FocusCamera);

            Assert.AreEqual(HubIdKind.Character, shape.TargetKind);
            Assert.IsFalse(shape.UsesPosition);
            Assert.IsFalse(shape.UsesSeconds);
        }

        [Test]
        public void WaitingOnlyAsksHowLong()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.Wait);

            Assert.IsTrue(shape.UsesSeconds);
            Assert.IsFalse(shape.UsesTarget);
            Assert.IsFalse(shape.UsesValue);
        }

        [Test]
        public void DepartingNamesTheBattle()
        {
            Assert.AreEqual(HubIdKind.Map, HubEventActionShape.Of(HubEventActionKind.Depart).ValueKind);
        }

        [Test]
        public void ChangingAnObjectNamesTheObject()
        {
            HubEventActionShape shape = HubEventActionShape.Of(HubEventActionKind.SetInteractableState);

            Assert.AreEqual(HubIdKind.Interactable, shape.TargetKind);
            Assert.IsTrue(shape.UsesState);
        }
    }
}
