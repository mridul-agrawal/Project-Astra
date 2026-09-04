using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Editor;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Tests.Hub
{
    // The drawer that lets a designer say what a stage does at either end. It offers the kinds it
    // finds and builds the one that is picked, so both halves are worth pinning.
    [TestFixture]
    public class QuestEventDrawerTests
    {
        [Test]
        public void EveryKindOfEffectIsOffered()
        {
            Type[] offered = QuestEventDrawer.Kinds().ToArray();

            CollectionAssert.Contains(offered, typeof(PlayAuthoredEventEvent));
            CollectionAssert.Contains(offered, typeof(PlayDialogEvent));
            CollectionAssert.Contains(offered, typeof(SetFlagEvent));
        }

        [Test]
        public void NothingAbstractIsOffered()
        {
            Assert.IsFalse(QuestEventDrawer.Kinds().Any(kind => kind.IsAbstract));
        }

        // The chooser builds what was picked with Activator, which needs a constructor taking nothing.
        [Test]
        public void EveryOfferedKindCanBeBuilt()
        {
            foreach (Type kind in QuestEventDrawer.Kinds())
                Assert.IsInstanceOf<QuestEvent>(Activator.CreateInstance(kind), kind.Name);
        }

        [Test]
        public void KindsAreNamedTheWayADesignerWouldSayThem()
        {
            Assert.AreEqual("Play authored event", QuestEventDrawer.Readable("PlayAuthoredEventEvent"));
            Assert.AreEqual("Set flag", QuestEventDrawer.Readable("SetFlagEvent"));
            Assert.AreEqual("Play dialog", QuestEventDrawer.Readable("PlayDialogEvent"));
        }

        // What the chooser does when a kind is picked: an entry that held nothing now holds one.
        [Test]
        public void ChoosingAKindFillsTheEmptyEntry()
        {
            QuestData quest = QuestData.Create("under_test", Array.Empty<QuestObjective>());
            var editable = new SerializedObject(quest);

            SerializedProperty effects = editable.FindProperty("onComplete");
            effects.arraySize = 1;
            editable.ApplyModifiedProperties();

            SerializedProperty entry = editable.FindProperty("onComplete").GetArrayElementAtIndex(0);
            Assert.IsEmpty(entry.managedReferenceFullTypename, "a new entry starts with no kind");

            entry.managedReferenceValue = new SetFlagEvent();
            editable.ApplyModifiedProperties();

            Assert.IsInstanceOf<SetFlagEvent>(quest.OnComplete[0]);
            UnityEngine.Object.DestroyImmediate(quest);
        }
    }
}
