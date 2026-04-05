using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class CameraControllerTests
    {
        private GameObject _cameraGO;
        private CameraController _controller;
        private MapRenderer _mapRenderer;
        private MapData _mapData;

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
            _mapRenderer = mapGO.AddComponent<MapRenderer>();
            _mapData = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(_mapData);
            so.FindProperty("_width").intValue = MapW;
            so.FindProperty("_height").intValue = MapH;
            so.ApplyModifiedPropertiesWithoutUndo();

            var field = typeof(MapRenderer).GetField("_currentMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_mapRenderer, _mapData);

            // Create CameraController
            _cameraGO = new GameObject("Camera");
            var cam = _cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            _controller = _cameraGO.AddComponent<CameraController>();
            _controller.Initialize(null, _mapRenderer, Margin, ViewW, ViewH);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraGO);
            Object.DestroyImmediate(_mapRenderer.gameObject);
            Object.DestroyImmediate(_mapData);
        }

        // --- Deadzone tests ---

        [Test]
        public void CursorInsideDeadzone_CameraDoesNotMove()
        {
            // Camera at (0,0), viewport 10x8, margin 2
            // Deadzone: x=[2,7], y=[2,5]
            _controller.SetCameraGridPos(Vector2Int.zero);

            _controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 3));
            Assert.AreEqual(Vector2Int.zero, _controller.CameraGridPos);

            _controller.ScrollIfOutsideDeadzone(new Vector2Int(5, 4));
            Assert.AreEqual(Vector2Int.zero, _controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneRight_CameraScrollsRight()
        {
            _controller.SetCameraGridPos(Vector2Int.zero);

            // Deadzone right edge = 10 - 1 - 2 = 7. Cursor at x=8 exceeds it.
            _controller.ScrollIfOutsideDeadzone(new Vector2Int(8, 3));
            Assert.AreEqual(new Vector2Int(1, 0), _controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneLeft_CameraScrollsLeft()
        {
            _controller.SetCameraGridPos(new Vector2Int(5, 5));

            // Deadzone left edge = 5 + 2 = 7. Cursor at x=6 (local 1) is below margin.
            _controller.ScrollIfOutsideDeadzone(new Vector2Int(6, 7));
            Assert.AreEqual(new Vector2Int(4, 5), _controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneUp_CameraScrollsUp()
        {
            _controller.SetCameraGridPos(Vector2Int.zero);

            // Deadzone top edge = 8 - 1 - 2 = 5. Cursor at y=6 exceeds it.
            _controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 6));
            Assert.AreEqual(new Vector2Int(0, 1), _controller.CameraGridPos);
        }

        [Test]
        public void CursorExitsDeadzoneDown_CameraScrollsDown()
        {
            _controller.SetCameraGridPos(new Vector2Int(0, 5));

            // Deadzone bottom edge = 5 + 2 = 7. Cursor at y=6 (local 1) is below margin.
            _controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 6));
            Assert.AreEqual(new Vector2Int(0, 4), _controller.CameraGridPos);
        }

        // --- Map boundary clamping ---

        [Test]
        public void CameraClampedAtRightEdge_DoesNotScrollFurther()
        {
            // Max camera x = 20 - 10 = 10
            _controller.SetCameraGridPos(new Vector2Int(10, 0));

            _controller.ScrollIfOutsideDeadzone(new Vector2Int(18, 3));
            Assert.AreEqual(10, _controller.CameraGridPos.x);
        }

        [Test]
        public void CameraClampedAtLeftEdge_DoesNotScrollFurther()
        {
            _controller.SetCameraGridPos(Vector2Int.zero);

            // Cursor at x=0 would push camera left, but it's already at 0
            _controller.ScrollIfOutsideDeadzone(new Vector2Int(0, 3));
            Assert.AreEqual(0, _controller.CameraGridPos.x);
        }

        [Test]
        public void CameraClampedAtBottom_DoesNotScrollFurther()
        {
            _controller.SetCameraGridPos(Vector2Int.zero);

            _controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 0));
            Assert.AreEqual(0, _controller.CameraGridPos.y);
        }

        [Test]
        public void CameraClampedAtTop_DoesNotScrollFurther()
        {
            // Max camera y = 15 - 8 = 7
            _controller.SetCameraGridPos(new Vector2Int(0, 7));

            _controller.ScrollIfOutsideDeadzone(new Vector2Int(3, 14));
            Assert.AreEqual(7, _controller.CameraGridPos.y);
        }

        // --- Small map (map < viewport) ---

        [Test]
        public void SmallMap_CameraStaysAtOrigin()
        {
            // Create a small 5x5 map (smaller than 10x8 viewport)
            var smallMap = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(smallMap);
            so.FindProperty("_width").intValue = 5;
            so.FindProperty("_height").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();

            var field = typeof(MapRenderer).GetField("_currentMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_mapRenderer, smallMap);

            _controller.SetCameraGridPos(Vector2Int.zero);
            _controller.ScrollIfOutsideDeadzone(new Vector2Int(4, 4));

            Assert.AreEqual(Vector2Int.zero, _controller.CameraGridPos);

            Object.DestroyImmediate(smallMap);
        }

        // --- CenterOnTile ---

        [Test]
        public void CenterOnTile_CentersViewportOnTile()
        {
            _controller.CenterOnTile(new Vector2Int(10, 7));

            // Expected: camera at (10 - 5, 7 - 4) = (5, 3)
            Assert.AreEqual(new Vector2Int(5, 3), _controller.CameraGridPos);
        }

        [Test]
        public void CenterOnTile_NearEdge_ClampsToMapBounds()
        {
            _controller.CenterOnTile(new Vector2Int(1, 1));

            // Would compute (-4, -3) but clamps to (0, 0)
            Assert.AreEqual(Vector2Int.zero, _controller.CameraGridPos);
        }

        [Test]
        public void CenterOnTile_NearBottomRight_ClampsToMax()
        {
            _controller.CenterOnTile(new Vector2Int(19, 14));

            // Would compute (14, 10) but max is (10, 7)
            Assert.AreEqual(new Vector2Int(10, 7), _controller.CameraGridPos);
        }

        // --- Edge-break behavior ---

        [Test]
        public void EdgeBreak_CursorReachesEdgeTile_WhileCameraClamped()
        {
            // Camera at max x=10, viewport shows tiles 10-19
            // Cursor at tile 19 (rightmost) — camera stays clamped at 10
            _controller.SetCameraGridPos(new Vector2Int(10, 0));

            _controller.ScrollIfOutsideDeadzone(new Vector2Int(19, 3));
            Assert.AreEqual(10, _controller.CameraGridPos.x);
            // Cursor at 19 is valid even though it's outside normal deadzone
        }
    }
}
