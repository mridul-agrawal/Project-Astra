using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Camera;

namespace ProjectAstra.Core.Tests.Camera
{
    [TestFixture]
    public class CameraControllerTests
    {
        private GameObject cameraGO;
        private CameraController controller;
        private MapRenderer mapRenderer;
        private MapData mapData;

        // Default test setup: 20x15 map, 10x8 viewport, deadzone margin 2
        private const int MapW = 20;
        private const int MapH = 15;
        private const int ViewW = 10;
        private const int ViewH = 8;
        private const int Margin = 2;

        [SetUp]
        public void SetUp()
        {
            // Create MapRenderer with map data
            var mapGO = new GameObject("MapRenderer");
            mapRenderer = mapGO.AddComponent<MapRenderer>();
            mapData = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(mapData);
            so.FindProperty("width").intValue = MapW;
            so.FindProperty("height").intValue = MapH;
            so.ApplyModifiedPropertiesWithoutUndo();

            var field = typeof(MapRenderer).GetField("currentMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(mapRenderer, mapData);

            // Create CameraController
            cameraGO = new GameObject("Camera");
            var cam = cameraGO.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            controller = cameraGO.AddComponent<CameraController>();
            controller.Initialize(null, mapRenderer, Margin, ViewW, ViewH);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraGO);
            Object.DestroyImmediate(mapRenderer.gameObject);
            Object.DestroyImmediate(mapData);
        }

        // --- Deadzone tests ---

        [Test]
        public void CursorInsideDeadzone_CameraDoesNotMove()
        {
            // Camera at (0,0), viewport 10x8, margin 2
            // Deadzone: x=[2,7], y=[2,5]
            controller.SetCameraGridPos(Vector2Int.zero);

            controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 3));
            Assert.AreEqual(Vector2Int.zero, controller.CameraGridPos);

            controller.ScrollIfOutsideDeadzone(new Vector2Int(5, 4));
            Assert.AreEqual(Vector2Int.zero, controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneRight_CameraScrollsRight()
        {
            controller.SetCameraGridPos(Vector2Int.zero);

            // Deadzone right edge = 10 - 1 - 2 = 7. Cursor at x=8 exceeds it.
            controller.ScrollIfOutsideDeadzone(new Vector2Int(8, 3));
            Assert.AreEqual(new Vector2Int(1, 0), controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneLeft_CameraScrollsLeft()
        {
            controller.SetCameraGridPos(new Vector2Int(5, 5));

            // Deadzone left edge = 5 + 2 = 7. Cursor at x=6 (local 1) is below margin.
            controller.ScrollIfOutsideDeadzone(new Vector2Int(6, 7));
            Assert.AreEqual(new Vector2Int(4, 5), controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneUp_CameraScrollsUp()
        {
            controller.SetCameraGridPos(Vector2Int.zero);

            // Deadzone top edge = 8 - 1 - 2 = 5. Cursor at y=6 exceeds it.
            controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 6));
            Assert.AreEqual(new Vector2Int(0, 1), controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneDown_CameraScrollsDown()
        {
            controller.SetCameraGridPos(new Vector2Int(0, 5));

            // Deadzone bottom edge = 5 + 2 = 7. Cursor at y=6 (local 1) is below margin.
            controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 6));
            Assert.AreEqual(new Vector2Int(0, 4), controller.CameraGridPos);
        }

        // --- Map boundary clamping ---

        [Test]
        public void CameraClampedAtRightEdge_DoesNotScrollFurther()
        {
            // Max camera x = 20 - 10 = 10
            controller.SetCameraGridPos(new Vector2Int(10, 0));

            controller.ScrollIfOutsideDeadzone(new Vector2Int(18, 3));
            Assert.AreEqual(10, controller.CameraGridPos.x);
        }

        [Test]
        public void CameraClampedAtLeftEdge_DoesNotScrollFurther()
        {
            controller.SetCameraGridPos(Vector2Int.zero);

            // Cursor at x=0 would push camera left, but it's already at 0
            controller.ScrollIfOutsideDeadzone(new Vector2Int(0, 3));
            Assert.AreEqual(0, controller.CameraGridPos.x);
        }

        [Test]
        public void CameraClampedAtBottom_DoesNotScrollFurther()
        {
            controller.SetCameraGridPos(Vector2Int.zero);

            controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 0));
            Assert.AreEqual(0, controller.CameraGridPos.y);
        }

        [Test]
        public void CameraClampedAtTop_DoesNotScrollFurther()
        {
            // Max camera y = 15 - 8 = 7
            controller.SetCameraGridPos(new Vector2Int(0, 7));

            controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 14));
            Assert.AreEqual(7, controller.CameraGridPos.y);
        }

        // --- Small map (map < viewport) ---

        [Test]
        public void SmallMap_CameraStaysAtOrigin()
        {
            // Create a small 5x5 map (smaller than 10x8 viewport)
            var smallMap = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(smallMap);
            so.FindProperty("width").intValue = 5;
            so.FindProperty("height").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();

            var field = typeof(MapRenderer).GetField("currentMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(mapRenderer, smallMap);

            controller.SetCameraGridPos(Vector2Int.zero);
            controller.ScrollIfOutsideDeadzone(new Vector2Int(4, 4));

            Assert.AreEqual(Vector2Int.zero, controller.CameraGridPos);

            Object.DestroyImmediate(smallMap);
        }

        // --- CenterOnTile ---

        [Test]
        public void CenterOnTile_CentersViewportOnTile()
        {
            controller.CenterOnTile(new Vector2Int(10, 7));

            // Expected: camera at (10 - 5, 7 - 4) = (5, 3)
            Assert.AreEqual(new Vector2Int(5, 3), controller.CameraGridPos);
        }

        [Test]
        public void CenterOnTile_NearEdge_ClampsToMapBounds()
        {
            controller.CenterOnTile(new Vector2Int(1, 1));

            // Would compute (-4, -3) but clamps to (0, 0)
            Assert.AreEqual(Vector2Int.zero, controller.CameraGridPos);
        }

        [Test]
        public void CenterOnTile_NearBottomRight_ClampsToMax()
        {
            controller.CenterOnTile(new Vector2Int(19, 14));

            // Would compute (14, 10) but max is (10, 7)
            Assert.AreEqual(new Vector2Int(10, 7), controller.CameraGridPos);
        }

        // --- Edge-break behavior ---

        [Test]
        public void EdgeBreak_CursorReachesEdgeTile_WhileCameraClamped()
        {
            // Camera at max x=10, viewport shows tiles 10-19
            // Cursor at tile 19 (rightmost) — camera stays clamped at 10
            controller.SetCameraGridPos(new Vector2Int(10, 0));

            controller.ScrollIfOutsideDeadzone(new Vector2Int(19, 3));
            Assert.AreEqual(10, controller.CameraGridPos.x);
            // Cursor at 19 is valid even though it's outside normal deadzone
        }
    }
}
