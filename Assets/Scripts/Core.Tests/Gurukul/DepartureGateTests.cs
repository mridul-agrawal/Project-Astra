using NUnit.Framework;
using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.Core.Tests.Gurukul
{
    [TestFixture]
    public class DepartureGateTests
    {
        private const string Map = "map_1";

        [Test]
        public void EverythingDoneAndAgreed_Departs()
        {
            Assert.IsTrue(DepartureGate.CanDepart(true, Map, Map, out string problem), problem);
        }

        // The one hard rule about leaving a visit.
        [Test]
        public void UnfinishedObjectives_BlockDeparture()
        {
            Assert.IsFalse(DepartureGate.CanDepart(false, Map, Map, out string problem));
            StringAssert.Contains("objective", problem);
        }

        [Test]
        public void AVisitWithNoDestination_IsAContentError()
        {
            Assert.IsFalse(DepartureGate.CanDepart(true, "", Map, out string problem));
            StringAssert.Contains("does not name a battle", problem);
        }

        [Test]
        public void NoBattleAfterThisVisit_IsAContentError()
        {
            Assert.IsFalse(DepartureGate.CanDepart(true, Map, null, out string problem));
            StringAssert.Contains("no battle after", problem);
        }

        // The destination is authored on the visit and must never be inferred, so the campaign
        // disagreeing with it is reported rather than quietly resolved one way or the other.
        [Test]
        public void CampaignAndVisitDisagreeing_IsAContentError()
        {
            Assert.IsFalse(DepartureGate.CanDepart(true, "map_2", "map_1", out string problem));
            StringAssert.Contains("map_2", problem);
            StringAssert.Contains("map_1", problem);
        }

        [Test]
        public void UnfinishedObjectivesAreReportedFirst_EvenWhenTheContentIsAlsoWrong()
        {
            Assert.IsFalse(DepartureGate.CanDepart(false, "", null, out string problem));
            StringAssert.Contains("objective", problem);
        }
    }
}
