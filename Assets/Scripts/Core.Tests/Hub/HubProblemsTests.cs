using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // The validator is what a designer trusts instead of pressing Play, so the shape of what it
    // reports matters as much as whether it finds anything.
    [TestFixture]
    public class HubProblemsTests
    {
        // The thorough sweep stands every room up in a scene of its own, so it is run once for the
        // whole fixture rather than once per test.
        private static List<HubProblem> thorough;
        private static List<HubProblem> quick;

        [OneTimeSetUp]
        public void LookOnce()
        {
            thorough = HubProblems.Collect();
            quick = HubProblems.CollectQuick();
        }

        [Test]
        public void TheAuthoredHubHasNothingWrongWithIt()
        {
            Assert.IsEmpty(quick, Describe(quick));
            Assert.IsEmpty(thorough, Describe(thorough));
        }

        // The thorough pass adds checks; it never drops one, or the live count would say a hub was
        // fine when the full sweep disagrees.
        [Test]
        public void TheThoroughPassNeverFindsFewerThanTheQuickOne()
        {
            Assert.GreaterOrEqual(thorough.Count, quick.Count);
        }

        [Test]
        public void EveryProblemSaysSomething()
        {
            foreach (HubProblem problem in thorough)
                Assert.IsNotEmpty(problem.Message);
        }

        // A badge needs both to be drawn: a position with no room would be drawn in every room.
        [Test]
        public void AProblemWithAPlaceAlsoSaysWhichRoom()
        {
            foreach (HubProblem problem in thorough)
                if (problem.Where.HasValue)
                    Assert.IsNotEmpty(problem.LocationId, problem.Message);
        }

        [Test]
        public void EveryFixSaysWhatItWouldDo()
        {
            foreach (HubProblem problem in thorough)
                if (problem.Fix != null)
                    Assert.IsNotEmpty(problem.Fix.Label, problem.Message);
        }

        [Test]
        public void TheWatchStartsFromWhatIsActuallyThere()
        {
            Assert.AreEqual(HubWatch.Count, HubWatch.Problems.Count);
        }

        [Test]
        public void AskingForOneRoomGivesOnlyThatRoom()
        {
            foreach (HubProblem problem in HubWatch.In("greybox_courtyard"))
                Assert.AreEqual("greybox_courtyard", problem.LocationId);
        }

        // Nothing without a place can be drawn, so the room filter must not offer one.
        [Test]
        public void AskingForOneRoomGivesOnlyThingsWithAPlace()
        {
            foreach (HubProblem problem in HubWatch.In("greybox_courtyard"))
                Assert.IsTrue(problem.Where.HasValue, problem.Message);
        }

        private static string Describe(IEnumerable<HubProblem> problems) =>
            string.Join("\n  ", problems.Select(problem => problem.Message));
    }
}
