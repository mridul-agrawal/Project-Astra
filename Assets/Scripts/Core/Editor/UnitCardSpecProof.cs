using System.Collections.Generic;
using System.IO;
using ProjectAstra.Core.UI.BattleMap.HUD;
using ProjectAstra.Core.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Drives the real UnitCardView.Render with the spec's §11 test data and captures the three
    // §9 states, so the card is verified by what it actually draws rather than by its
    // serialized fields. Edit-mode only.
    //
    // Run via 'Project Astra/Proof Sheet - Unit Card'. Throwaway; delete once reviewed.
    // ==========================================================================================
    public static class UnitCardSpecProof
    {
        const string OutPath = "Assets/Screenshots/unit_card_spec_proof.png";

        const int CanvasWidth  = 1920;
        const int CanvasHeight = 1080;

        // The card docks top-left at a 16px inset; grab a generous crop around it.
        const int CropWidth  = 540;
        const int CropHeight = 260;
        const int Gutter     = 12;

        [MenuItem("Project Astra/Proof Sheet - Unit Card")]
        public static void Capture()
        {
            var view = Object.FindAnyObjectByType<UnitCardView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogError("[UnitCardSpecProof] No UnitCardView in the open scene.");
                return;
            }

            var canvas = view.GetComponentInParent<Canvas>(true);
            var restore = new SceneRestore(canvas, view);
            var camera = CreateCamera();

            try
            {
                restore.PrepareForCapture(camera);
                WriteSheet(CaptureStates(view, camera));
            }
            finally
            {
                restore.Undo();
                Object.DestroyImmediate(camera.gameObject);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UnitCardSpecProof] Wrote {OutPath}");
        }

        static List<Texture2D> CaptureStates(UnitCardView view, Camera camera)
        {
            var shots = new List<Texture2D>();
            foreach (var model in SpecTestData())
            {
                view.Render(model);
                Canvas.ForceUpdateCanvases();
                shots.Add(CaptureCrop(camera));
            }
            return shots;
        }

        // Spec §11 test data, one row per §9 state treatment.
        static IEnumerable<UnitCardModel> SpecTestData()
        {
            yield return new UnitCardModel
            {
                UnitName = "Arjun", CurrentHP = 85, MaxHP = 100,
                UnitFaction = Faction.Player, HasActed = false, Corner = HudCorner.TopLeft
            };
            yield return new UnitCardModel
            {
                UnitName = "Ghatotkach", CurrentHP = 7, MaxHP = 22,
                UnitFaction = Faction.Enemy, HasActed = false, Corner = HudCorner.TopLeft
            };
            yield return new UnitCardModel
            {
                UnitName = "Arjun", CurrentHP = 100, MaxHP = 100,
                UnitFaction = Faction.Player, HasActed = true, Corner = HudCorner.TopLeft
            };
        }

        static Texture2D CaptureCrop(Camera camera)
        {
            var target = new RenderTexture(CanvasWidth, CanvasHeight, 24);
            camera.targetTexture = target;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var crop = new Texture2D(CropWidth, CropHeight, TextureFormat.RGB24, false);
            crop.ReadPixels(new Rect(0f, CanvasHeight - CropHeight, CropWidth, CropHeight), 0, 0);
            crop.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            return crop;
        }

        static void WriteSheet(List<Texture2D> shots)
        {
            int height = shots.Count * CropHeight + (shots.Count - 1) * Gutter;
            var sheet = new Texture2D(CropWidth, height, TextureFormat.RGB24, false);
            FillGutters(sheet);

            for (int i = 0; i < shots.Count; i++)
            {
                int y = height - (i + 1) * CropHeight - i * Gutter;
                sheet.SetPixels(0, y, CropWidth, CropHeight, shots[i].GetPixels());
                Object.DestroyImmediate(shots[i]);
            }
            sheet.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllBytes(OutPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        static void FillGutters(Texture2D sheet)
        {
            var background = new Color[sheet.width * sheet.height];
            for (int i = 0; i < background.Length; i++) background[i] = new Color(0.5f, 0.1f, 0.5f);
            sheet.SetPixels(background);
        }

        static Camera CreateCamera()
        {
            var go = new GameObject("~UnitCardProofCamera");
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Neutral, because the card is only 94% opaque and the backdrop tints it. The
            // magenta gutters between rows are what flag a row that failed to draw.
            camera.backgroundColor = new Color(0.22f, 0.22f, 0.24f);
            camera.orthographic = true;
            return camera;
        }

        // Puts the live HUD into a capturable state and puts it back exactly as it was.
        sealed class SceneRestore
        {
            readonly Canvas canvas;
            readonly UnitCardView view;
            readonly RenderMode mode;
            readonly Camera worldCamera;
            readonly bool canvasEnabled;
            readonly List<(GameObject go, bool active)> siblings = new List<(GameObject, bool)>();

            public SceneRestore(Canvas canvas, UnitCardView view)
            {
                this.canvas = canvas;
                this.view = view;
                mode = canvas.renderMode;
                worldCamera = canvas.worldCamera;
                canvasEnabled = canvas.gameObject.activeSelf;
            }

            public void PrepareForCapture(Camera camera)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;

                HideEverythingExceptTheCard();
                WakeAncestorChain();
            }

            // Battle-map UI ships disabled by default and is woken at battle start, so every
            // link from the canvas down to the card has to be switched on by hand here.
            void WakeAncestorChain()
            {
                for (Transform t = view.transform; t != null; t = t.parent)
                {
                    siblings.Add((t.gameObject, t.gameObject.activeSelf));
                    t.gameObject.SetActive(true);
                    if (t == canvas.transform) break;
                }
            }

            // Other HUD panels dock to the same corner region; they would sit in the crop.
            void HideEverythingExceptTheCard()
            {
                Transform hud = view.transform.parent;
                if (hud == null) return;

                foreach (Transform child in hud)
                {
                    if (child == view.transform) continue;
                    siblings.Add((child.gameObject, child.gameObject.activeSelf));
                    child.gameObject.SetActive(false);
                }
            }

            // Reversed, so ancestors go back off after their children do.
            public void Undo()
            {
                for (int i = siblings.Count - 1; i >= 0; i--)
                    siblings[i].go.SetActive(siblings[i].active);

                canvas.renderMode = mode;
                canvas.worldCamera = worldCamera;
                canvas.gameObject.SetActive(canvasEnabled);
            }
        }
    }
}
