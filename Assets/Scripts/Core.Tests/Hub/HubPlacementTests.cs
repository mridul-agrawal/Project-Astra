using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // Placing something from the palette has to leave it set up the way the game expects, because a
    // designer never opens the components afterwards to check.
    [TestFixture]
    public class HubPlacementTests
    {
        private static readonly Vector2 BottomCentre = new(0.5f, 0f);
        private static readonly Vector2 BottomLeft = new(0f, 0f);
        private static readonly Vector2 Centre = new(0.5f, 0.5f);

        private readonly List<Object> spawned = new();
        private HubRoom room;

        [SetUp]
        public void SetUp()
        {
            var host = new GameObject("Room");
            spawned.Add(host);
            room = host.AddComponent<HubRoom>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in spawned)
                if (made != null) Object.DestroyImmediate(made, true);
            spawned.Clear();
        }

        [Test]
        public void APlacementLandsOnAWholePixel()
        {
            Vector3 snapped = HubPlacement.SnapToPixel(new Vector2(1.004f, -2.6f));

            Assert.AreEqual(1f, snapped.x, 0.0001f);
            Assert.AreEqual(-2.59375f, snapped.y, 0.0001f);
        }

        [Test]
        public void APlacementSitsOnTheSpritePlane()
        {
            Assert.AreEqual(0f, HubPlacement.SnapToPixel(new Vector2(3f, 4f)).z);
        }

        [Test]
        public void GroundGoesUnderTheGroundGroupAndIsNotSorted()
        {
            GameObject placed = Place(Entry(HubPalette.Kind.Ground, blocks: false), new Vector2(2f, 2f));

            Assert.AreEqual("Ground", placed.transform.parent.name);
            Assert.AreEqual("Ground", placed.GetComponent<SpriteRenderer>().sortingLayerName);
            Assert.IsNull(placed.GetComponent<YSortRenderer>());
        }

        [Test]
        public void AnObjectGoesUnderPropsAndSortsByDepth()
        {
            GameObject placed = Place(Entry(HubPalette.Kind.Object, blocks: false), new Vector2(2f, 2f));

            Assert.AreEqual("Props", placed.transform.parent.name);
            Assert.AreEqual("Object", placed.GetComponent<SpriteRenderer>().sortingLayerName);
            Assert.IsNotNull(placed.GetComponent<YSortRenderer>());
        }

        [Test]
        public void SomethingThatDoesNotBlockHasNoCollider()
        {
            GameObject placed = Place(Entry(HubPalette.Kind.Object, blocks: false), Vector2.zero);

            Assert.IsNull(placed.GetComponent<Collider2D>());
            Assert.AreNotEqual(LayerMask.NameToLayer(PhysicsSolidSpace.SolidLayer), placed.layer);
        }

        [Test]
        public void SomethingThatBlocksStopsHerAtItsBase()
        {
            HubPalette.Entry entry = Entry(HubPalette.Kind.Object, blocks: true);
            entry.footprint = new Vector2(2f, 0.5f);

            GameObject placed = Place(entry, Vector2.zero);
            var box = placed.GetComponent<BoxCollider2D>();

            Assert.AreEqual(LayerMask.NameToLayer(PhysicsSolidSpace.SolidLayer), placed.layer);
            Assert.AreEqual(new Vector2(2f, 0.5f), box.size);
            Assert.AreEqual(new Vector2(0f, 0.25f), box.offset);
            Assert.IsFalse(box.isTrigger);
        }

        [Test]
        public void PlacingTwiceReusesTheSameGroup()
        {
            Place(Entry(HubPalette.Kind.Object, blocks: false), Vector2.zero);
            Place(Entry(HubPalette.Kind.Object, blocks: false), Vector2.one);

            Assert.AreEqual(1, room.transform.childCount);
            Assert.AreEqual(2, room.transform.Find("Props").childCount);
        }

        // Art in this project is pivoted inconsistently, and a designer should never have to know
        // which. The click marks the spot the thing stands on either way.
        [Test]
        public void AnObjectStandsWhereItWasClickedWhateverItsPivot()
        {
            GameObject fromLeft = Place(Entry(HubPalette.Kind.Object, false, BottomLeft), new Vector2(5f, 5f));
            GameObject fromCentre = Place(Entry(HubPalette.Kind.Object, false, BottomCentre), new Vector2(5f, 5f));

            Assert.AreEqual(5f, Foot(fromLeft).x, 0.0001f);
            Assert.AreEqual(5f, Foot(fromLeft).y, 0.0001f);
            Assert.AreEqual(Foot(fromCentre), Foot(fromLeft));
        }

        // Floors are held by their corner instead, so pieces laid side by side meet exactly.
        [Test]
        public void GroundIsHeldByItsCorner()
        {
            GameObject placed = Place(Entry(HubPalette.Kind.Ground, false, Centre), new Vector2(5f, 5f));
            Bounds art = placed.GetComponent<SpriteRenderer>().bounds;

            Assert.AreEqual(5f, art.min.x, 0.0001f);
            Assert.AreEqual(5f, art.min.y, 0.0001f);
        }

        [Test]
        public void WhatBlocksSitsUnderTheArtNotUnderThePivot()
        {
            HubPalette.Entry entry = Entry(HubPalette.Kind.Object, true, BottomLeft);
            entry.footprint = new Vector2(0.25f, 0.5f);

            GameObject placed = Place(entry, new Vector2(5f, 5f));
            Physics2D.SyncTransforms();
            Bounds blocked = placed.GetComponent<BoxCollider2D>().bounds;

            Assert.AreEqual(5f, blocked.center.x, 0.0001f);
            Assert.AreEqual(5f, blocked.min.y, 0.0001f);
        }

        [Test]
        public void DepthIsMeasuredFromTheFootOfTheArt()
        {
            GameObject placed = Place(Entry(HubPalette.Kind.Object, false, Centre), new Vector2(5f, 5f));
            var baseline = new SerializedObject(placed.GetComponent<YSortRenderer>())
                .FindProperty("baselineOffset");

            Assert.AreEqual(-0.125f, baseline.floatValue, 0.0001f);
        }

        private static Vector2 Foot(GameObject placed)
        {
            Bounds art = placed.GetComponent<SpriteRenderer>().bounds;
            return new Vector2(art.center.x, art.min.y);
        }

        [Test]
        public void NothingIsPlacedWithoutARoom()
        {
            Assert.IsNull(HubPlacement.Place(Entry(HubPalette.Kind.Object, blocks: false), null, Vector2.zero));
        }

        [Test]
        public void AnEntryWithNothingToShowIsNotPlaced()
        {
            Assert.IsNull(HubPlacement.Place(new HubPalette.Entry(), room, Vector2.zero));
        }

        private GameObject Place(HubPalette.Entry entry, Vector2 at)
        {
            GameObject placed = HubPlacement.Place(entry, room, at);
            spawned.Add(placed);
            return placed;
        }

        private HubPalette.Entry Entry(HubPalette.Kind kind, bool blocks) =>
            Entry(kind, blocks, BottomCentre);

        // An eight-pixel square at 32 to the tile is a quarter of a tile across.
        private HubPalette.Entry Entry(HubPalette.Kind kind, bool blocks, Vector2 pivot)
        {
            var texture = new Texture2D(8, 8);
            spawned.Add(texture);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), pivot, 32f);
            spawned.Add(sprite);

            return new HubPalette.Entry { label = "Thing", kind = kind, sprite = sprite, blocks = blocks };
        }
    }
}
