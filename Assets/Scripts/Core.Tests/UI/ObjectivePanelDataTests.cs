using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.UI.BattleMap.HUD;

namespace ProjectAstra.Core.Tests.UI
{
    // Pins the objectives banner's content to the map's authored data.
    //
    // These drive the real ObjectiveController against a real MapData rather than asserting a
    // hand-built model. That distinction is the whole point: the tile panel round shipped a broken
    // Impassable flag precisely because its check asserted its own model and so proved nothing about
    // the derivation underneath.
    [TestFixture]
    public class ObjectivePanelDataTests
    {
        private MapData map;

        [SetUp]
        public void SetUp()
        {
            map = ScriptableObject.CreateInstance<MapData>();
        }

        [TearDown]
        public void TearDown()
        {
            if (map != null) Object.DestroyImmediate(map);
        }

        // ---- the conditions -------------------------------------------------------------------

        [Test]
        public void WinAndLoseLines_ComeFromTheMap()
        {
            ObjectiveModel model = Build("Seize the gate", "Arjun falls");

            Assert.AreEqual("Seize the gate", model.WinText);
            Assert.AreEqual("Arjun falls", model.LoseText);
        }

        // §B6's empty map: the banner renders the two pairs and nothing else.
        [Test]
        public void MapWithNoObjectives_ProducesNoRows()
        {
            ObjectiveModel model = Build("Rout the enemy", "Arjun falls");

            Assert.IsEmpty(model.Objectives);
            Assert.IsFalse(model.HasObjectives,
                "A map with no authored objectives must not raise the OBJECTIVES section.");
        }

        // ---- the checklist --------------------------------------------------------------------

        [Test]
        public void AuthoredObjectives_BecomeRowsInOrder()
        {
            ObjectiveModel model = Build("Rout the enemy", "Arjun falls",
                Objective("Open every chest"),
                Objective("Recruit the mercenary"),
                Objective("Hold the bridge"));

            Assert.IsTrue(model.HasObjectives);
            Assert.AreEqual(3, model.Objectives.Count);
            Assert.AreEqual("Open every chest", model.Objectives[0].Text);
            Assert.AreEqual("Recruit the mercenary", model.Objectives[1].Text);
            Assert.AreEqual("Hold the bridge", model.Objectives[2].Text);
        }

        [Test]
        public void BlankObjectiveText_IsSkipped()
        {
            ObjectiveModel model = Build("Rout the enemy", "Arjun falls",
                Objective("Open every chest"),
                Objective("   "),
                Objective(""));

            Assert.AreEqual(1, model.Objectives.Count,
                "An objective row with no text would render as an empty checkbox.");
        }

        [Test]
        public void AuthoredCompletion_CarriesThrough()
        {
            ObjectiveModel model = Build("Rout the enemy", "Arjun falls",
                Objective("Open every chest", complete: true));

            Assert.IsTrue(model.Objectives[0].Complete);
        }

        // §B4 pins a counter only when there is progress to show.
        [Test]
        public void MaxOfZero_MeansNoCounter()
        {
            ObjectiveModel model = Build("Rout the enemy", "Arjun falls",
                Objective("Hold the bridge"),
                Objective("Recruit the mercenary", current: 2, max: 5));

            Assert.IsFalse(model.Objectives[0].HasCounter);
            Assert.IsTrue(model.Objectives[1].HasCounter);
            Assert.AreEqual("2/5", model.Objectives[1].CounterText);
        }

        // ---- the runtime copy -----------------------------------------------------------------

        // The authored values are the map's starting state. Ticking a row off must not write back
        // into the ScriptableObject, or the change would persist across editor sessions.
        [Test]
        public void CompletingAnObjective_DoesNotWriteBackToTheAsset()
        {
            ObjectiveController controller = Controller("Rout the enemy", "Arjun falls",
                Objective("Open every chest"));

            controller.SetObjectiveComplete(0, true);

            Assert.IsTrue(controller.objectiveModel.Objectives[0].Complete,
                "The runtime row should be complete.");
            Assert.IsFalse(map.SecondaryObjectives[0].complete,
                "The authored map data must be untouched.");
        }

        [Test]
        public void Progress_ClampsAndCompletesAtTheMax()
        {
            ObjectiveController controller = Controller("Rout the enemy", "Arjun falls",
                Objective("Recruit the mercenary", current: 0, max: 5));

            controller.SetProgress(0, 3);
            Assert.AreEqual(3, controller.objectiveModel.Objectives[0].Current);
            Assert.IsFalse(controller.objectiveModel.Objectives[0].Complete);

            controller.SetProgress(0, 99);
            Assert.AreEqual(5, controller.objectiveModel.Objectives[0].Current, "Clamped to the max.");
            Assert.IsTrue(controller.objectiveModel.Objectives[0].Complete,
                "Reaching the max completes the objective.");
        }

        [Test]
        public void OutOfRangeIndex_IsIgnored()
        {
            ObjectiveController controller = Controller("Rout the enemy", "Arjun falls",
                Objective("Open every chest"));

            Assert.DoesNotThrow(() => controller.SetObjectiveComplete(7, true));
            Assert.DoesNotThrow(() => controller.SetProgress(-1, 3));
        }

        // ---- helpers --------------------------------------------------------------------------

        private static SecondaryObjective Objective(string text, bool complete = false,
                                                    int current = 0, int max = 0) =>
            new SecondaryObjective { text = text, complete = complete, current = current, max = max };

        private ObjectiveModel Build(string win, string lose, params SecondaryObjective[] objectives) =>
            Controller(win, lose, objectives).objectiveModel;

        // Authors the map, loads it into MapService, and lets the real controller read it.
        private ObjectiveController Controller(string win, string lose,
                                               params SecondaryObjective[] objectives)
        {
            var so = new UnityEditor.SerializedObject(map);
            so.FindProperty("width").intValue = 1;
            so.FindProperty("height").intValue = 1;
            so.FindProperty("winConditionText").stringValue = win;
            so.FindProperty("loseConditionText").stringValue = lose;

            var list = so.FindProperty("secondaryObjectives");
            list.arraySize = objectives != null ? objectives.Length : 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("text").stringValue = objectives[i].text;
                e.FindPropertyRelative("complete").boolValue = objectives[i].complete;
                e.FindPropertyRelative("current").intValue = objectives[i].current;
                e.FindPropertyRelative("max").intValue = objectives[i].max;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            MapService.Load(map, null);
            return new ObjectiveController(null);
        }
    }
}
