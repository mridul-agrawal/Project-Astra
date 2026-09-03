using System.Linq;
using NUnit.Framework;
using ProjectAstra.Core;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // "Where is this used?" is what makes renaming safe. An answer that misses one reference is
    // worse than no answer, because it is trusted.
    [TestFixture]
    public class HubUsagesTests
    {
        [SetUp]
        public void Forget() => HubUsages.Forget();

        [Test]
        public void NothingIsUsedByNothing()
        {
            Assert.IsEmpty(HubUsages.Of(null));
            Assert.IsEmpty(HubUsages.Of(""));
        }

        [Test]
        public void ANameNothingMentionsIsUsedNowhere()
        {
            Assert.AreEqual(0, HubUsages.CountOf("a_name_nothing_in_this_project_uses"));
        }

        // Every conversation the hub offers is offered because something names it, so a
        // conversation used nowhere means the search is not looking everywhere it should.
        [Test]
        public void TheConversationsTheHubUsesAreFoundToBeUsed()
        {
            string[] used = HubIds.Of(HubIdKind.Conversation)
                .Where(id => HubUsages.CountOf(id) > 0).ToArray();

            Assert.IsNotEmpty(used, "no conversation in the project is used by anything, which cannot be true");
        }

        // A script names itself, so it is at least used once by its own asset.
        [Test]
        public void AConversationIsAtLeastUsedByItself()
        {
            string any = HubIds.Of(HubIdKind.Conversation).First();

            Assert.GreaterOrEqual(HubUsages.CountOf(any), 1);
        }

        [Test]
        public void EveryUseSaysWhereItIs()
        {
            foreach (HubUsage usage in HubUsages.Of(HubIds.Of(HubIdKind.Location).First()))
            {
                Assert.IsNotNull(usage.In);
                Assert.IsNotEmpty(usage.Field);
                Assert.IsNotEmpty(usage.Path);
            }
        }

        [Test]
        public void AskingTwiceGivesTheSameAnswer()
        {
            string any = HubIds.Of(HubIdKind.Location).First();

            Assert.AreSame(HubUsages.Of(any), HubUsages.Of(any));
        }

        [Test]
        public void RenamingToNothingIsRefused()
        {
            Assert.IsFalse(HubRename.CanRename("something", ""));
            Assert.IsFalse(HubRename.CanRename("", "something"));
            Assert.IsFalse(HubRename.CanRename("same", "same"));
        }

        [Test]
        public void RenamingToSomethingElseIsAllowed()
        {
            Assert.IsTrue(HubRename.CanRename("before", "after"));
        }

        [Test]
        public void RenamingNothingChangesNothing()
        {
            Assert.AreEqual(0, HubRename.Everywhere("", "after").Places);
        }
    }
}
