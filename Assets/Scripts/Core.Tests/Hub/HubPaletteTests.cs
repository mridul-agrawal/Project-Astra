using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // The palette fills itself from the art folder, so what it accepts and rejects decides whether a
    // designer ever has to register anything by hand.
    [TestFixture]
    public class HubPaletteTests
    {
        private HubPalette palette;
        private Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            palette = ScriptableObject.CreateInstance<HubPalette>();
            texture = new Texture2D(8, 8);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AdoptingArtNamesItReadably()
        {
            palette.Adopt(Art("stone_well"), "Props", HubPalette.Kind.Object);

            Assert.AreEqual("Stone well", palette.Entries[0].label);
        }

        [Test]
        public void TheSameArtIsOnlyAdoptedOnce()
        {
            Sprite sprite = Art("tree");

            Assert.IsTrue(palette.Adopt(sprite, "Nature", HubPalette.Kind.Object));
            Assert.IsFalse(palette.Adopt(sprite, "Nature", HubPalette.Kind.Object));
            Assert.AreEqual(1, palette.Entries.Count);
        }

        [Test]
        public void NothingIsAdoptedFromNothing()
        {
            Assert.IsFalse(palette.Adopt((Sprite)null, "Props", HubPalette.Kind.Object));
            Assert.IsEmpty(palette.Entries);
        }

        // Ground is walked over, so offering it as something that stops her would be wrong.
        [Test]
        public void GroundArtDoesNotBlockByDefault()
        {
            palette.Adopt(Art("grass"), "Ground", HubPalette.Kind.Ground);

            Assert.IsFalse(palette.Entries[0].blocks);
        }

        [Test]
        public void ObjectArtBlocksByDefault()
        {
            palette.Adopt(Art("hut"), "Buildings", HubPalette.Kind.Object);

            Assert.IsTrue(palette.Entries[0].blocks);
        }

        [Test]
        public void CategoriesAreListedOnceAndInOrder()
        {
            palette.Adopt(Art("hut"), "Buildings", HubPalette.Kind.Object);
            palette.Adopt(Art("tree"), "Nature", HubPalette.Kind.Object);
            palette.Adopt(Art("shrub"), "Nature", HubPalette.Kind.Object);

            CollectionAssert.AreEqual(new[] { "Buildings", "Nature" }, palette.Categories.ToArray());
        }

        [Test]
        public void ACategoryShowsOnlyItsOwn()
        {
            palette.Adopt(Art("hut"), "Buildings", HubPalette.Kind.Object);
            palette.Adopt(Art("tree"), "Nature", HubPalette.Kind.Object);

            Assert.AreEqual(1, palette.InCategory("Nature").Count());
        }

        private Sprite Art(string name)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0f), 32f);
            sprite.name = name;
            return sprite;
        }
    }
}
