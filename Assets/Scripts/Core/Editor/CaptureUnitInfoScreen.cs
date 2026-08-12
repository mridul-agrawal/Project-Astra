using System.Collections.Generic;
using System.IO;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.UI.UnitInfo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Renders the Unit Info Screen prefab to PNGs so the restyle can actually be looked at,
    // without entering play mode.
    //
    // A screen-space-overlay canvas captures as an empty frame, so the prefab is dropped under a
    // throwaway ScreenSpaceCamera canvas pointed at a private camera and rendered from there.
    // The Views are driven with sample models rather than left on their placeholder strings, so
    // the shot shows real text lengths and bar fills.
    //
    // Run via 'Project Astra/Capture Unit Info Screen'. Output lands in Assets/Screenshots/.
    // ==========================================================================================
    public static class CaptureUnitInfoScreen
    {
        const string PrefabPath = "Assets/UI/UnitInfoScreen/UnitInfoScreen.prefab";
        const string OutputDir  = "Assets/Screenshots";
        const int Width = 1920, Height = 1080;

        [MenuItem("Project Astra/Capture Unit Info Screen")]
        public static void Capture()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[CaptureUnitInfoScreen] Missing " + PrefabPath);
                return;
            }

            Directory.CreateDirectory(OutputDir);
            var stage = BuildStage();
            var screen = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.canvas.transform);
            StretchToCanvas(screen);

            var views = new Views(screen);
            DriveWithSampleData(views);

            ShowTab(views, stats: true);
            Shoot(stage, "unit_info_stats");

            ShowTab(views, stats: false);
            Shoot(stage, "unit_info_gear");

            ExpandFirstSlot(views);
            Shoot(stage, "unit_info_gear_expanded");

            Object.DestroyImmediate(screen);
            TearDown(stage);
            AssetDatabase.Refresh();
            Debug.Log("[CaptureUnitInfoScreen] Wrote 3 captures to " + OutputDir);
        }

        // ---- staging -------------------------------------------------------------------------

        class Stage
        {
            public GameObject cameraHolder;
            public Camera camera;
            public Canvas canvas;
            public RenderTexture target;
        }

        static Stage BuildStage()
        {
            var stage = new Stage();

            stage.cameraHolder = new GameObject("__CaptureCamera");
            stage.camera = stage.cameraHolder.AddComponent<Camera>();
            stage.camera.clearFlags = CameraClearFlags.SolidColor;
            stage.camera.backgroundColor = new Color32(0x08, 0x0a, 0x0d, 0xff);
            stage.camera.orthographic = true;
            stage.camera.cullingMask = ~0;

            stage.target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            stage.camera.targetTexture = stage.target;

            var canvasHolder = new GameObject("__CaptureCanvas");
            stage.canvas = canvasHolder.AddComponent<Canvas>();
            stage.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            stage.canvas.worldCamera = stage.camera;
            stage.canvas.planeDistance = 10f;

            var scaler = canvasHolder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasHolder.AddComponent<GraphicRaycaster>();
            return stage;
        }

        static void TearDown(Stage stage)
        {
            stage.camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(stage.canvas.gameObject);
            Object.DestroyImmediate(stage.cameraHolder);
            stage.target.Release();
            Object.DestroyImmediate(stage.target);
        }

        static void StretchToCanvas(GameObject screen)
        {
            var rect = screen.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        static void Shoot(Stage stage, string name)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(stage.canvas.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            stage.camera.Render();

            RenderTexture.active = stage.target;
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            shot.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes($"{OutputDir}/{name}.png", shot.EncodeToPNG());
            Object.DestroyImmediate(shot);
        }

        // ---- the prefab's views --------------------------------------------------------------

        class Views
        {
            public UnitSummaryView summary;
            public UnitStatsView stats;
            public UnitGearView gear;
            public UnitInfoTabBarView tabs;
            public UnitInfoFooterView footer;
            public UnitInfoFocusMarker marker;

            public Views(GameObject root)
            {
                summary = root.GetComponentInChildren<UnitSummaryView>(true);
                stats = root.GetComponentInChildren<UnitStatsView>(true);
                gear = root.GetComponentInChildren<UnitGearView>(true);
                tabs = root.GetComponentInChildren<UnitInfoTabBarView>(true);
                footer = root.GetComponentInChildren<UnitInfoFooterView>(true);
                marker = root.GetComponentInChildren<UnitInfoFocusMarker>(true);
            }
        }

        // Placed through the marker's own MoveTo, so the capture exercises the real §8 code path
        // rather than a copy of its maths.
        static void ShowTab(Views views, bool stats)
        {
            views.tabs?.Render(stats ? UnitInfoTab.Stats : UnitInfoTab.Gear);
            views.stats?.SetTabActive(stats);
            views.gear?.SetTabActive(!stats);

            Canvas.ForceUpdateCanvases();
            RectTransform target = stats ? views.stats?.FocusRectFor(0) : views.gear?.FocusRectFor(0);
            views.marker?.MoveTo(target, true);
        }

        static void ExpandFirstSlot(Views views)
        {
            views.gear?.SetSelected(0, true);
            if (views.gear == null || views.gear.Slots == null) return;

            // Update() does not tick in edit mode, so the accordion height is applied by hand.
            var slot = views.gear.Slots[0];
            if (slot.Sizer != null) slot.Sizer.preferredHeight = views.gear.WeaponSelectedHeight;
            if (slot.ExpansionFader != null) slot.ExpansionFader.alpha = 1f;
        }

        // ---- sample data ---------------------------------------------------------------------

        static void DriveWithSampleData(Views views)
        {
            views.summary?.Render(SampleSummary());
            views.stats?.Render(SampleStats());
            views.gear?.Render(SampleGear());
            views.footer?.Render(new UnitInfoFooterModel
            {
                Title = "STRENGTH",
                Description = "Physical attack power.",
                Detail = "Raises damage dealt with swords, lances, axes, and bows.",
            });
            views.stats?.ApplyLabelEmphasis(0, Color.white, new Color32(0xae, 0xb6, 0xc4, 0xff));
        }

        static UnitSummaryModel SampleSummary() => new UnitSummaryModel
        {
            UnitName = "ARANYA",
            ClassName = "ARCHER",
            Level = 3,
            CurrentHP = 17,
            MaxHP = 21,
            ShowExp = true,
            ExpText = "42 / 100",
            ExpFraction = 0.42f,
            ShowStatus = true,
            StatusText = "STATUS",
            Statuses = new List<UnitStatusKind> { UnitStatusKind.Stressed },
        };

        static UnitStatsModel SampleStats()
        {
            var labels = new[] { "STRENGTH", "MAGIC", "SKILL", "SPEED", "DEFENSE", "RESIST", "CON", "LUCK", "MOVE" };
            var values = new[] { 9, 0, 12, 11, 6, 3, 7, 8, 5 };
            var caps   = new[] { 20, 20, 20, 20, 20, 20, 20, 20, 10 };
            var groups = new[]
            {
                StatGroup.Attack, StatGroup.Attack, StatGroup.Attack, StatGroup.Attack,
                StatGroup.Guard, StatGroup.Guard,
                StatGroup.Body, StatGroup.Body, StatGroup.Body,
            };

            var rows = new StatRowVM[9];
            for (int i = 0; i < 9; i++)
                rows[i] = new StatRowVM
                {
                    Label = labels[i], Value = values[i], Cap = caps[i], Group = groups[i],
                };

            return new UnitStatsModel
            {
                Rows = rows,
                Weapon = new EquippedWeaponVM
                {
                    HasWeapon = true, Name = "IRON BOW", WeaponType = WeaponType.Bow,
                    Mt = 6, Hit = 85, Crt = 0, RngMin = 2, RngMax = 2,
                },
            };
        }

        static UnitGearModel SampleGear()
        {
            var gold = new Color32(0xe8, 0xb3, 0x4b, 0xff);
            var rankD = new Color32(0x79, 0xb5, 0x79, 0xff);

            return new UnitGearModel
            {
                Slots = new[]
                {
                    new GearSlotVM
                    {
                        Index = 1, Name = "IRON BOW", IsWeapon = true, IsEquipped = true,
                        TypeBadge = "BOW", Mt = 6, Hit = 85, Crt = 0, RngMin = 2, RngMax = 2, Weight = 5,
                        Grade = "D", GradeColor = rankD,
                        ShowUses = true, CurrentUses = 38, MaxUses = 45,
                        Description = "Bow at range 2.", Detail = "Might 6 · Hit 85 · Crit 0 · Weight 5.",
                    },
                    new GearSlotVM
                    {
                        Index = 2, Name = "SHORT BOW", IsWeapon = true,
                        TypeBadge = "BOW", Mt = 4, Hit = 90, Crt = 5, RngMin = 2, RngMax = 2, Weight = 3,
                        Grade = "Prf", GradeColor = gold, GradeIsPersonal = true,
                        ShowUses = true, CurrentUses = 12, MaxUses = 30,
                        Description = "Bow at range 2.", Detail = "Might 4 · Hit 90 · Crit 5 · Weight 3.",
                    },
                    new GearSlotVM
                    {
                        Index = 3, Name = "VULNERARY", IsWeapon = false, TypeBadge = "VULNERARY",
                        EffectText = "HEAL 10",
                        ShowUses = true, CurrentUses = 2, MaxUses = 3,
                        Description = "Restores 10 HP to the user.", Detail = "2 of 3 uses left.",
                    },
                    new GearSlotVM { Index = 4, IsEmpty = true, Description = "An empty slot." },
                    new GearSlotVM { Index = 5, IsEmpty = true, Description = "An empty slot." },
                },
            };
        }
    }
}
