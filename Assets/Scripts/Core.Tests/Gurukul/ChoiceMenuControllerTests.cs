using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Dialogue.Conversation;
using ProjectAstra.Core.UI.Dialogue.Choice;

namespace ProjectAstra.Core.Tests.Gurukul
{
    [TestFixture]
    public class ChoiceMenuControllerTests
    {
        private GameObject host;
        private ChoiceMenuController menu;
        private int chosen;
        private bool cancelled;

        [SetUp]
        public void SetUp()
        {
            // The view is a MonoBehaviour with no widgets wired, which renders as a no-op — enough
            // for the navigation rules, which is all this controller owns.
            host = new GameObject("ChoiceMenuView");
            menu = new ChoiceMenuController(host.AddComponent<ChoiceMenuView>());
            chosen = -1;
            cancelled = false;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        private void Show(bool allowCancel, params (string label, bool enabled)[] options)
        {
            var choices = new List<ConversationChoice>();
            foreach ((string label, bool enabled) in options)
                choices.Add(new ConversationChoice(label, enabled));

            menu.Show(choices, allowCancel, index => chosen = index, () => cancelled = true);
        }

        [Test]
        public void Showing_StartsOnTheFirstOption()
        {
            Show(true, ("A", true), ("B", true));

            Assert.IsTrue(menu.IsOpen);
            Assert.AreEqual(0, menu.HighlightedIndex);
        }

        // A topic she has already raised must never be where the cursor lands.
        [Test]
        public void Showing_SkipsPastALeadingUsedTopic()
        {
            Show(true, ("Asked already", false), ("Fresh", true));

            Assert.AreEqual(1, menu.HighlightedIndex);
        }

        [Test]
        public void MovingDown_StepsToTheNextOption()
        {
            Show(true, ("A", true), ("B", true));
            menu.Move(1);

            Assert.AreEqual(1, menu.HighlightedIndex);
        }

        [Test]
        public void MovingPastTheEnd_WrapsAround()
        {
            Show(true, ("A", true), ("B", true));
            menu.Move(1);
            menu.Move(1);

            Assert.AreEqual(0, menu.HighlightedIndex);
        }

        [Test]
        public void MovingUpFromTheTop_WrapsToTheBottom()
        {
            Show(true, ("A", true), ("B", true), ("C", true));
            menu.Move(-1);

            Assert.AreEqual(2, menu.HighlightedIndex);
        }

        [Test]
        public void Moving_StepsOverUsedTopics()
        {
            Show(true, ("A", true), ("Used", false), ("C", true));
            menu.Move(1);

            Assert.AreEqual(2, menu.HighlightedIndex);
        }

        [Test]
        public void WithOnlyOneOptionLeft_MovingStaysPut()
        {
            Show(true, ("Used", false), ("Only", true));
            menu.Move(1);

            Assert.AreEqual(1, menu.HighlightedIndex);
        }

        [Test]
        public void Confirming_ReportsTheHighlightedOptionAndCloses()
        {
            Show(true, ("A", true), ("B", true));
            menu.Move(1);
            menu.Confirm();

            Assert.AreEqual(1, chosen);
            Assert.IsFalse(menu.IsOpen);
        }

        [Test]
        public void Cancelling_ReportsItAndCloses()
        {
            Show(true, ("A", true));
            menu.Cancel();

            Assert.IsTrue(cancelled);
            Assert.IsFalse(menu.IsOpen);
        }

        // A knowledge check can't be backed out of, so cancel does nothing and the menu stays up
        // rather than closing with the conversation left nowhere to go.
        [Test]
        public void CancellingWhenItIsNotAllowed_LeavesTheMenuOpen()
        {
            Show(false, ("Right", true), ("Wrong", true));
            menu.Cancel();

            Assert.IsFalse(cancelled);
            Assert.IsTrue(menu.IsOpen);
        }

        [Test]
        public void ConfirmingWhileClosed_DoesNothing()
        {
            Show(true, ("A", true));
            menu.Hide();
            menu.Confirm();

            Assert.AreEqual(-1, chosen);
        }

        [Test]
        public void AMenuWithNothingPickable_CannotBeConfirmed()
        {
            Show(true, ("Used", false));
            menu.Confirm();

            Assert.AreEqual(-1, chosen, "Every option being used up must not resolve to a pick.");
        }
    }
}
