using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // A request to start in the middle of a visit has to be honoured exactly once. One that
    // survived would mean the game quietly opening part-way through for whoever ran it next.
    [TestFixture]
    public class HubLaunchRequestTests
    {
        [SetUp]
        [TearDown]
        public void Forget() => HubLaunchRequest.Clear();

        [Test]
        public void WithNothingAskedThereIsNothingToDo()
        {
            Assert.IsFalse(HubLaunchRequest.Take().IsSomething);
        }

        [Test]
        public void WhatWasAskedForComesBack()
        {
            HubLaunchRequest.Set("hub1", 2, new Vector2(4f, 7f));
            HubLaunchRequest.Request asked = HubLaunchRequest.Take();

            Assert.AreEqual("hub1", asked.VisitId);
            Assert.AreEqual(2, asked.Stage);
            Assert.IsTrue(asked.HasSpawn);
            Assert.AreEqual(new Vector2(4f, 7f), asked.Spawn);
        }

        [Test]
        public void AskingIsHonouredOnceAndThenForgotten()
        {
            HubLaunchRequest.Set("hub1", 2, new Vector2(4f, 7f));

            Assert.IsTrue(HubLaunchRequest.Take().IsSomething);
            Assert.IsFalse(HubLaunchRequest.Take().IsSomething);
        }

        [Test]
        public void NotSayingWhereMeansTheVisitDecides()
        {
            HubLaunchRequest.Set("hub1");

            HubLaunchRequest.Request asked = HubLaunchRequest.Take();
            Assert.IsFalse(asked.HasSpawn);
            Assert.AreEqual(0, asked.Stage);
        }

        [Test]
        public void AStageBeforeTheFirstIsTheFirst()
        {
            HubLaunchRequest.Set("hub1", -3);

            Assert.AreEqual(0, HubLaunchRequest.Take().Stage);
        }

        [Test]
        public void ClearingLeavesNothingBehind()
        {
            HubLaunchRequest.Set("hub1", 2, Vector2.one);
            HubLaunchRequest.Clear();

            Assert.IsFalse(HubLaunchRequest.Take().IsSomething);
        }
    }
}
