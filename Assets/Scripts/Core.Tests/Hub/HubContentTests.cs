using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // Runs the content checker over the hub assets that are actually in the project.
    //
    // The spec treats a missing marker target, a dead conversation branch or a door to nowhere as
    // blocking errors, and expects them found before play. Checking them here means a content edit
    // that breaks a reference fails the build rather than surfacing as a confused player.
    [TestFixture]
    public class HubContentTests
    {
        [Test]
        public void AuthoredHubContent_HasNoProblems()
        {
            List<HubProblem> problems = HubProblems.Collect();
            Assert.AreEqual(0, problems.Count, Describe(problems));
        }

        private static string Describe(List<HubProblem> problems)
        {
            var report = new StringBuilder("Hub content problems:");
            foreach (HubProblem problem in problems) report.Append("\n  ").Append(problem.Message);
            return report.ToString();
        }
    }
}
