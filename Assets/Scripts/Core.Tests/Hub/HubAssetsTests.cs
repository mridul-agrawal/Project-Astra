using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // Making a thing from the field that wants it is done from a table of field names. A typo there
    // is invisible until a designer clicks the button, so the table is checked against the types.
    [TestFixture]
    public class HubAssetsTests
    {
        [Test]
        public void EveryKindItCanMakeHasATypeToMake()
        {
            foreach (HubIdKind kind in Creatable())
                Assert.IsNotNull(HubAssets.TypeOf(kind), kind.ToString());
        }

        [Test]
        public void AKindItCannotMakeOffersNoType()
        {
            foreach (HubIdKind kind in Enum.GetValues(typeof(HubIdKind)).Cast<HubIdKind>())
                if (!HubAssets.CanCreate(kind))
                    Assert.IsNull(HubAssets.TypeOf(kind), kind.ToString());
        }

        // The id it writes has to be a field that actually exists, or a new thing is made nameless.
        [Test]
        public void EveryKindWritesItsIdIntoAFieldThatExists()
        {
            foreach (HubIdKind kind in Creatable())
            {
                var made = ScriptableObject.CreateInstance(HubAssets.TypeOf(kind));
                SerializedProperty id = new SerializedObject(made).FindProperty(IdFieldOf(kind));

                Assert.IsNotNull(id, $"{kind}: no such field on {HubAssets.TypeOf(kind).Name}");
                Assert.AreEqual(SerializedPropertyType.String, id.propertyType, kind.ToString());

                UnityEngine.Object.DestroyImmediate(made);
            }
        }

        // And the catalog it appends to has to have the list it names.
        [Test]
        public void EveryIndexedKindAppendsToAListThatExists()
        {
            foreach (HubIdKind kind in Creatable())
            {
                Type catalog = CatalogOf(kind);
                if (catalog == null) continue;

                var made = ScriptableObject.CreateInstance(catalog);
                SerializedProperty list = new SerializedObject(made).FindProperty(ListFieldOf(kind));

                Assert.IsNotNull(list, $"{kind}: no such list on {catalog.Name}");
                Assert.IsTrue(list.isArray, $"{kind}: {catalog.Name}.{ListFieldOf(kind)} is not a list");

                UnityEngine.Object.DestroyImmediate(made);
            }
        }

        // Gates and signals are only ever names, so nothing should try to make an asset for them.
        [Test]
        public void NothingIsMadeForAKindThatIsOnlyAName()
        {
            Assert.IsFalse(HubAssets.CanCreate(HubIdKind.Gate));
            Assert.IsFalse(HubAssets.CanCreate(HubIdKind.Signal));
            Assert.IsNull(HubAssets.Create(HubIdKind.Gate, "whatever"));
        }

        // The scene owns doors and objects, so they cannot be made from a dropdown either.
        [Test]
        public void NothingIsMadeForAKindTheSceneOwns()
        {
            Assert.IsFalse(HubAssets.CanCreate(HubIdKind.Door));
            Assert.IsFalse(HubAssets.CanCreate(HubIdKind.Interactable));
        }

        private static HubIdKind[] Creatable() =>
            Enum.GetValues(typeof(HubIdKind)).Cast<HubIdKind>().Where(HubAssets.CanCreate).ToArray();

        private static string IdFieldOf(HubIdKind kind) => Recipe(kind, "IdField");
        private static string ListFieldOf(HubIdKind kind) => Recipe(kind, "ListField");

        private static Type CatalogOf(HubIdKind kind)
        {
            object recipe = RecipeFor(kind);
            return (Type)recipe.GetType().GetField("Catalog").GetValue(recipe);
        }

        private static string Recipe(HubIdKind kind, string field)
        {
            object recipe = RecipeFor(kind);
            return (string)recipe.GetType().GetField(field).GetValue(recipe);
        }

        // The table is private because nothing but HubAssets should be reading it; this test is the
        // exception, and reads it rather than restating it.
        private static object RecipeFor(HubIdKind kind)
        {
            FieldInfo table = typeof(HubAssets).GetField("Recipes",
                BindingFlags.NonPublic | BindingFlags.Static);
            var recipes = (System.Collections.IDictionary)table.GetValue(null);

            return recipes[kind];
        }
    }
}
