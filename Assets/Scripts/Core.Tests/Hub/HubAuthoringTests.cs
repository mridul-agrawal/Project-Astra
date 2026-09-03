using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Tests.Hub
{
    // Making a prop interactive is one click, so what that click produces has to be exactly what the
    // game would have spawned.
    [TestFixture]
    public class HubAuthoringTests
    {
        private readonly List<GameObject> spawned = new();
        private readonly List<Object> textures = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in spawned)
                if (made != null) Object.DestroyImmediate(made, true);
            spawned.Clear();

            foreach (Object made in textures)
                if (made != null) Object.DestroyImmediate(made, true);
            textures.Clear();

            // Placing and configuring register undo steps. The test runner replays the undo
            // stack when it finishes, and would be replaying it against objects that are gone.
            Undo.ClearAll();
        }

        [Test]
        public void APropStartsOutNotInteractive()
        {
            Assert.IsFalse(HubAuthoring.IsInteractive(Prop("Stone Well")));
        }

        [Test]
        public void MakingItLookableGivesItSomethingToBeReachedBy()
        {
            GameObject prop = Prop("Stone Well");
            HubAuthoring.MakeInspectable(prop);

            var reach = prop.GetComponent<CircleCollider2D>();

            Assert.IsNotNull(reach);
            Assert.IsTrue(reach.isTrigger, "a reach region she could walk into would be a wall");
            Assert.AreEqual(InteractionReachRules.DefaultReachTiles, reach.radius);
        }

        [Test]
        public void MakingItLookableNamesItAfterItself()
        {
            GameObject prop = Prop("Stone Well");
            InspectableInteractable made = HubAuthoring.MakeInspectable(prop);

            Assert.AreEqual("stone_well", made.InteractableId);
        }

        // Art is pivoted inconsistently, so the point she walks up to is taken from the art rather
        // than from the transform.
        [Test]
        public void SheWalksUpToWhereTheThingStands()
        {
            GameObject prop = Prop("Stone Well");
            prop.AddComponent<SpriteRenderer>().sprite = ArtPivotedBottomLeft();

            InspectableInteractable made = HubAuthoring.MakeInspectable(prop);

            Assert.AreEqual(0.125f, made.InteractionPoint.x, 0.0001f);
            Assert.AreEqual(0f, made.InteractionPoint.y, 0.0001f);
            Assert.AreEqual(made.InteractionPoint, prop.GetComponent<CircleCollider2D>().offset);
        }

        [Test]
        public void SomethingWithNoArtIsReachedAtItself()
        {
            InspectableInteractable made = HubAuthoring.MakeInspectable(Prop("Marker"));

            Assert.AreEqual(Vector2.zero, (Vector2)made.InteractionPoint);
        }

        [Test]
        public void SomethingAlreadyInteractiveIsLeftAlone()
        {
            GameObject prop = Prop("Stone Well");
            HubAuthoring.MakeInspectable(prop);

            Assert.IsNull(HubAuthoring.MakeInspectable(prop));
            Assert.AreEqual(1, prop.GetComponents<InspectableInteractable>().Length);
        }

        [Test]
        public void RevertingTakesBackWhatItAdded()
        {
            GameObject prop = Prop("Stone Well");
            HubAuthoring.MakeInspectable(prop);
            HubAuthoring.Revert(prop);

            Assert.IsFalse(HubAuthoring.IsInteractive(prop));
            Assert.IsNull(prop.GetComponent<CircleCollider2D>());
        }

        // A collider that blocks was drawn for some other reason and is not this tool's to remove.
        [Test]
        public void RevertingLeavesACollidersThatIsNotAReachRegion()
        {
            GameObject prop = Prop("Stone Well");
            prop.AddComponent<BoxCollider2D>();
            HubAuthoring.MakeInspectable(prop);

            HubAuthoring.Revert(prop);

            Assert.IsNotNull(prop.GetComponent<BoxCollider2D>());
        }

        [Test]
        public void RevertingSomethingThatWasNeverInteractiveDoesNothing()
        {
            GameObject prop = Prop("Stone Well");
            Assert.DoesNotThrow(() => HubAuthoring.Revert(prop));
        }

        [TestCase("Stone Well", "stone_well")]
        [TestCase("Noticeboard", "noticeboard")]
        [TestCase("Report Card (1)", "report_card_1")]
        [TestCase("  spaced  out  ", "spaced_out")]
        [TestCase("", "")]
        public void ANameBecomesAnIdInTheShapeTheOthersAreIn(string name, string expected)
        {
            Assert.AreEqual(expected, HubAuthoring.IdFrom(name));
        }

        [Test]
        public void AnIdAlreadyTakenGetsANumber()
        {
            Assert.AreEqual("tree_2", HubAuthoring.Unused("tree", new[] { "tree" }));
            Assert.AreEqual("tree_3", HubAuthoring.Unused("tree", new[] { "tree", "tree_2" }));
        }

        [Test]
        public void AnIdNobodyHasIsLeftAlone()
        {
            Assert.AreEqual("tree", HubAuthoring.Unused("tree", new[] { "well" }));
        }

        [Test]
        public void SomethingWithNoUsableNameStillGetsOne()
        {
            Assert.AreEqual("thing", HubAuthoring.Unused(HubAuthoring.IdFrom("!!!"), new string[0]));
        }

        // Eight pixels at 32 to the tile is a quarter of a tile, held by its bottom-left corner.
        private Sprite ArtPivotedBottomLeft()
        {
            var texture = new Texture2D(8, 8);
            textures.Add(texture);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.zero, 32f);
            textures.Add(sprite);
            return sprite;
        }

        private GameObject Prop(string name)
        {
            var made = new GameObject(name);
            spawned.Add(made);
            return made;
        }
    }
}
