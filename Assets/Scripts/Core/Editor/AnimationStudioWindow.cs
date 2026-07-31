using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Editor
{
    // One front door for turning sprite sheets into animators. Slice a sheet into
    // named frames, then build either a unit's 6-state override controller or a
    // looping decoration/river controller — with a live preview so the designer
    // confirms the loop before leaving. A thin GUI over the existing builders.
    public sealed class AnimationStudioWindow : EditorWindow
    {
        private enum Mode { Slice, UnitController, DecoController }

        private static readonly string[] UnitStateRows = { "idle", "run_n", "run_e", "run_s", "run_w", "selected" };
        private const string DecoOutputRoot = "Assets/Animation/Environment";

        private Mode mode = Mode.Slice;

        // Slice
        private Texture2D sheet;
        private bool byCell;
        private int columns = 4, rows = 1, cellWidth = 32, cellHeight = 32;
        private string sliceBaseName = "";
        private bool unitRows;
        private bool tileable;
        private int alignmentIndex;   // 0 = Center, 1 = BottomCenter

        // Build
        private DefaultAsset framesFolder;
        private string controllerName = "";
        private int fps = 8;

        // Preview
        private Sprite[] previewFrames;

        [MenuItem("Project Astra/Animation/Animation Studio")]
        public static void Open()
        {
            var window = GetWindow<AnimationStudioWindow>("Animation Studio");
            window.minSize = new Vector2(420, 520);
        }

        private void OnGUI()
        {
            mode = (Mode)GUILayout.Toolbar((int)mode, new[] { "Slice", "Unit Controller", "Deco Controller" });
            EditorGUILayout.Space();

            switch (mode)
            {
                case Mode.Slice: DrawSlice(); break;
                case Mode.UnitController: DrawBuild(unit: true); break;
                case Mode.DecoController: DrawBuild(unit: false); break;
            }

            DrawPreview();
        }

        // --- Slice ---

        private void DrawSlice()
        {
            EditorGUILayout.LabelField("Slice a sprite sheet into named frames", EditorStyles.boldLabel);
            sheet = (Texture2D)EditorGUILayout.ObjectField("Sheet", sheet, typeof(Texture2D), false);
            sliceBaseName = EditorGUILayout.TextField("Base Name", sliceBaseName);

            unitRows = EditorGUILayout.ToggleLeft(
                "Unit sheet (rows = idle / run_n / run_e / run_s / run_w / selected)", unitRows);

            if (unitRows)
            {
                columns = Mathf.Max(1, EditorGUILayout.IntField("Frames per row", columns));
                EditorGUILayout.LabelField("Rows", $"{UnitStateRows.Length} (fixed by the unit states)");
            }
            else
            {
                byCell = EditorGUILayout.Toggle("Size by cell pixels", byCell);
                if (byCell)
                {
                    cellWidth = Mathf.Max(1, EditorGUILayout.IntField("Cell Width (px)", cellWidth));
                    cellHeight = Mathf.Max(1, EditorGUILayout.IntField("Cell Height (px)", cellHeight));
                }
                else
                {
                    columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
                    rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", rows));
                }
            }

            tileable = EditorGUILayout.ToggleLeft("Tileable (FullRect — for a river base-layer)", tileable);
            alignmentIndex = EditorGUILayout.Popup("Pivot", alignmentIndex, new[] { "Center", "Bottom-Center" });

            using (new EditorGUI.DisabledScope(sheet == null))
                if (GUILayout.Button("Slice"))
                    DoSlice();
        }

        private void DoSlice()
        {
            string path = AssetDatabase.GetAssetPath(sheet);
            string baseName = string.IsNullOrWhiteSpace(sliceBaseName)
                ? Path.GetFileNameWithoutExtension(path)
                : sliceBaseName;

            SpriteSlice[] slices = BuildSlices(baseName);
            if (slices.Length == 0) { Debug.LogWarning("[Animation] Slice produced no frames — check the grid."); return; }

            SpriteAlignment alignment = alignmentIndex == 1 ? SpriteAlignment.BottomCenter : SpriteAlignment.Center;
            int count = SpriteSheetSlicer.Slice(path, slices, alignment, tileable);
            AssetDatabase.Refresh();
            LoadPreview(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            Debug.Log($"[Animation] Sliced {count} frames in {path}");
        }

        private SpriteSlice[] BuildSlices(string baseName)
        {
            if (unitRows)
                return SpriteSheetGrid.SliceByGrid(sheet.width, sheet.height, columns, UnitStateRows.Length, baseName, UnitStateRows);
            return byCell
                ? SpriteSheetGrid.SliceByCell(sheet.width, sheet.height, cellWidth, cellHeight, baseName)
                : SpriteSheetGrid.SliceByGrid(sheet.width, sheet.height, columns, rows, baseName);
        }

        // --- Build (unit AOC or deco loop controller) ---

        private void DrawBuild(bool unit)
        {
            EditorGUILayout.LabelField(unit ? "Build a unit override controller" : "Build a looping decoration controller",
                EditorStyles.boldLabel);
            controllerName = EditorGUILayout.TextField(unit ? "Character Name" : "Deco Name", controllerName);
            framesFolder = (DefaultAsset)EditorGUILayout.ObjectField("Frames Folder", framesFolder, typeof(DefaultAsset), false);
            fps = Mathf.Max(1, EditorGUILayout.IntField("Frame Rate (fps)", fps));

            EditorGUILayout.HelpBox(unit
                ? "Folder must hold frames named idle_*, run_n_*, run_e_*, run_s_*, run_w_*, selected_*."
                : "Folder must hold frames named <name>_0, <name>_1, … (one looping animation).",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!CanBuild()))
                if (GUILayout.Button("Build"))
                    Build(unit);

            if (GUILayout.Button("Load folder into preview") && framesFolder != null)
                LoadPreviewFromFolder();
        }

        private bool CanBuild() => !string.IsNullOrWhiteSpace(controllerName) && framesFolder != null;

        private void Build(bool unit)
        {
            string folder = AssetDatabase.GetAssetPath(framesFolder);
            if (unit)
            {
                UnitAnimatorSetupTool.BuildFrom(controllerName, folder, fps);
            }
            else
            {
                string output = $"{DecoOutputRoot}/{controllerName}";
                LoopingAnimatorBuilder.BuildFromFolder(controllerName, folder, fps, output);
            }
            LoadPreviewFromFolder();
        }

        private void LoadPreviewFromFolder()
        {
            string folder = AssetDatabase.GetAssetPath(framesFolder);
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .SelectMany(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<Sprite>());
            LoadPreview(sprites);
        }

        // --- Preview ---

        private void LoadPreview(IEnumerable<Sprite> sprites) =>
            previewFrames = sprites.OrderBy(s => TrailingNumber(s.name)).ToArray();

        private void DrawPreview()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Preview ({(previewFrames?.Length ?? 0)} frames @ {fps} fps)", EditorStyles.boldLabel);
            Rect box = GUILayoutUtility.GetRect(160, 160, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(box, new Color(0.14f, 0.14f, 0.14f));

            if (previewFrames == null || previewFrames.Length == 0) return;

            int frame = (int)(EditorApplication.timeSinceStartup * fps) % previewFrames.Length;
            AnimationPreviewUtil.DrawSprite(box, previewFrames[frame]);
            Repaint();   // keep the loop animating while the window is open
        }

        private static int TrailingNumber(string name)
        {
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(name[(underscore + 1)..], out int n) ? n : 0;
        }
    }
}
