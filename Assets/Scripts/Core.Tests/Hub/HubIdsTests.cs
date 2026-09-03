using System;
using System.Linq;
using NUnit.Framework;
using ProjectAstra.Core;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // Every id dropdown in the hub is built from this, so an empty or duplicated answer is a
    // designer picking a name that does not work.
    [TestFixture]
    public class HubIdsTests
    {
        [SetUp]
        public void SetUp() => HubIds.Forget();

        [Test]
        public void NoKindAnswersWithNothingAtAll()
        {
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind)))
                Assert.IsNotNull(HubIds.Of(kind), kind.ToString());
        }

        [Test]
        public void NoAnswerContainsABlank()
        {
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind)))
                CollectionAssert.DoesNotContain(HubIds.Of(kind), string.Empty, kind.ToString());
        }

        [Test]
        public void NoAnswerRepeatsItself()
        {
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind)))
            {
                string[] ids = HubIds.Of(kind);
                Assert.AreEqual(ids.Length, ids.Distinct().Count(), $"{kind} lists something twice");
            }
        }

        [Test]
        public void EveryAnswerIsInOrderSoItCanBeScanned()
        {
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind)))
            {
                string[] ids = HubIds.Of(kind);
                CollectionAssert.AreEqual(
                    ids.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(), ids, kind.ToString());
            }
        }

        // Gates and signals are named as they are needed rather than declared anywhere, which is
        // why those two dropdowns offer a way to name another one.
        [Test]
        public void OnlyGatesAndSignalsAreNamedInPlace()
        {
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind)))
            {
                bool expected = kind is HubIdKind.Gate or HubIdKind.Signal;
                Assert.AreEqual(expected, HubIds.IsNamedInPlace(kind), kind.ToString());
            }
        }

        // What the hub is actually made of right now. A conversation nobody can pick is a hub
        // nobody can author.
        [Test]
        public void TheAuthoredHubOffersSomethingToPick()
        {
            Assert.IsNotEmpty(HubIds.Of(HubIdKind.Conversation));
            Assert.IsNotEmpty(HubIds.Of(HubIdKind.Location));
            Assert.IsNotEmpty(HubIds.Of(HubIdKind.Quest));
            Assert.IsNotEmpty(HubIds.Of(HubIdKind.Visit));
        }

        [Test]
        public void AskingTwiceGivesTheSameAnswer()
        {
            Assert.AreSame(HubIds.Of(HubIdKind.Location), HubIds.Of(HubIdKind.Location));
        }

        [Test]
        public void ForgettingMakesItLookAgain()
        {
            string[] first = HubIds.Of(HubIdKind.Location);
            HubIds.Forget();

            Assert.AreNotSame(first, HubIds.Of(HubIdKind.Location));
        }
    }
}
