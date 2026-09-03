using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // The authoring requirements set budgets in seconds from the designer's hands. Most of each
    // budget is thinking and aiming; what is measured here is the part the tooling owns, which has
    // to be small enough not to be felt.
    //
    // Generous limits on purpose. These catch something becoming slow by an order of magnitude,
    // which is what actually happens, rather than policing milliseconds on a busy machine.
    [TestFixture]
    public class HubSpeedTests
    {
        private const int Unnoticeable = 100;
        private const int WhileTyping = 400;

        private readonly List<UnityEngine.Object> spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object made in spawned)
                if (made != null) UnityEngine.Object.DestroyImmediate(made, true);
            spawned.Clear();
            Undo.ClearAll();
        }

        // Every dropdown in every inspector is built from this, so it is asked on every repaint.
        [Test]
        public void OfferingTheNamesIsInstantOnceAsked()
        {
            HubIds.Forget();
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind))) HubIds.Of(kind);

            long taken = Time(() =>
            {
                for (int i = 0; i < 100; i++)
                    foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind))) HubIds.Of(kind);
            });

            Assert.Less(taken, Unnoticeable, $"reading the cached names 100 times took {taken}ms");
        }

        // The first check after a change has to read the content, which is the expensive part.
        [Test]
        public void TheFirstCheckAfterAChangeIsQuickEnough()
        {
            HubProblems.Forget();
            long taken = Time(() => HubProblems.CollectQuick());

            Assert.Less(taken, WhileTyping, $"the first check took {taken}ms");
        }

        // Every check after that is what runs continuously, and has to stay cheap as content grows.
        //
        // Thirty milliseconds each, not because that is fast but because checking only happens
        // after a change has settled for three quarters of a second. This is here to catch the
        // difference between that and half a second, which is what a lost cache looks like.
        [Test]
        public void CheckingAgainIsQuickEnoughToRunContinuously()
        {
            HubProblems.CollectQuick();
            long taken = Time(() => { for (int i = 0; i < 10; i++) HubProblems.CollectQuick(); });

            Assert.Less(taken, 300, $"ten more checks took {taken}ms");
        }

        // "Where is this used?" is required to stay instant.
        [Test]
        public void FindingWhereANameIsUsedIsQuick()
        {
            HubUsages.Forget();
            string any = HubIds.Of(HubIdKind.Conversation).First();

            long taken = Time(() => HubUsages.Of(any));

            Assert.Less(taken, WhileTyping, $"finding every use took {taken}ms");
        }

        [Test]
        public void AskingAgainIsFree()
        {
            string any = HubIds.Of(HubIdKind.Conversation).First();
            HubUsages.Of(any);

            long taken = Time(() => { for (int i = 0; i < 1000; i++) HubUsages.Of(any); });

            Assert.Less(taken, Unnoticeable, $"asking 1000 more times took {taken}ms");
        }

        // The budget is twenty props in thirty seconds. Almost all of that is aiming; placing must
        // not be what makes it slow.
        [Test]
        public void PlacingTwentyPropsIsNotWhatTakesTheTime()
        {
            HubRoom room = Room();
            HubPalette.Entry entry = Prop();

            long taken = Time(() =>
            {
                for (int i = 0; i < 20; i++)
                    spawned.Add(HubPlacement.Place(entry, room, new Vector2(i, i)));
            });

            Assert.Less(taken, Unnoticeable, $"placing twenty took {taken}ms");
        }

        [Test]
        public void ReadingAConversationIsQuickEnoughToDrawEveryFrame()
        {
            var lines = new List<string>();
            for (int i = 0; i < 30; i++) lines.Add($"line {i}");

            ProjectAstra.Core.Dialogue.DialogueScript script =
                ProjectAstra.Core.Dialogue.DialogueScript.CreateRuntime("under_test", null, lines);
            spawned.Add(script);

            long taken = Time(() => { for (int i = 0; i < 200; i++) HubConversationFlow.Read(script); });

            Assert.Less(taken, Unnoticeable, $"reading a conversation 200 times took {taken}ms");
        }

        private static long Time(Action work)
        {
            var clock = Stopwatch.StartNew();
            work();
            return clock.ElapsedMilliseconds;
        }

        private HubRoom Room()
        {
            var host = new GameObject("Room");
            spawned.Add(host);
            return host.AddComponent<HubRoom>();
        }

        private HubPalette.Entry Prop()
        {
            var texture = new Texture2D(8, 8);
            spawned.Add(texture);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0f), 32f);
            spawned.Add(sprite);

            return new HubPalette.Entry { label = "Prop", sprite = sprite, kind = HubPalette.Kind.Object };
        }
    }
}
