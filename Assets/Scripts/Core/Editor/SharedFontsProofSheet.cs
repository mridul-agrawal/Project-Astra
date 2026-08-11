using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Throwaway verification sheet: renders every generated JetBrains Mono weight at the unit
    // info card's spec values so we can confirm glyphs actually rasterise instead of trusting
    // the serialized atlas. Edit-mode only — no play mode needed (see UI_WORKFLOW §4.3).
    //
    // Run via 'Project Astra/Proof Sheet - Shared Fonts'. Delete this file once reviewed.
    // ==========================================================================================
    public static class SharedFontsProofSheet
    {
        const string FontDir = "Assets/UI/Shared/Fonts/";
        const string OutPath = "Assets/Screenshots/shared_fonts_proof.png";

        const int SheetWidth  = 1000;
        const int SheetHeight = 820;

        // The card's own palette, straight from the spec's §8 table.
        static readonly Color CardBackground = new Color32(22, 26, 33, 255);
        static readonly Color Emphasis       = new Color32(255, 255, 255, 255);
        static readonly Color Muted          = new Color32(154, 162, 174, 255);
        static readonly Color AllyAccent     = new Color32(79, 157, 255, 255);

        static readonly string[] Weights = { "Regular", "Medium", "Bold", "ExtraBold" };

        [MenuItem("Project Astra/Proof Sheet - Shared Fonts")]
        public static void Capture()
        {
            var root = new GameObject("~ProofSheet");
            var camera = CreateCamera(root);
            var canvas = CreateCanvas(root, camera);

            BuildRows(canvas.transform);

            Canvas.ForceUpdateCanvases();
            WritePng(camera);

            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[SharedFontsProofSheet] Wrote {OutPath}");
        }

        static void BuildRows(Transform parent)
        {
            float y = -40f;
            foreach (string weight in Weights)
            {
                var font = LoadFont(weight);
                if (font == null) continue;

                AddLabel(parent, $"JetBrainsMono-{weight}", 20f, Muted, new Vector2(40f, y), font);
                AddLabel(parent, "Arjun    HP 85/100    Ghatotkach    7/22", 24f, Emphasis,
                         new Vector2(40f, y - 30f), font);
                AddLabel(parent, "the quick brown fox 0123456789 /:-", 18f, AllyAccent,
                         new Vector2(40f, y - 68f), font);
                y -= 150f;
            }

            AddSpecComposition(parent, new Vector2(40f, y - 10f));
        }

        // The literal §6 typography run: 8px/700 name, 6px/400 label, 8px/700 + 8px/400 numerals,
        // drawn at 3x so the integer-scaled result is readable on screen.
        static void AddSpecComposition(Transform parent, Vector2 origin)
        {
            var bold = LoadFont("Bold");
            var regular = LoadFont("Regular");
            if (bold == null || regular == null) return;

            AddLabel(parent, "spec composition @3x", 20f, Muted, origin, regular);
            AddLabel(parent, "Ghatotkach", 24f, Emphasis, origin + new Vector2(0f, -30f), bold);
            AddLabel(parent, "HP", 18f, Muted, origin + new Vector2(0f, -64f), regular);
            AddLabel(parent, "85", 24f, Emphasis, origin + new Vector2(40f, -62f), bold);
            AddLabel(parent, "/100", 24f, Muted, origin + new Vector2(75f, -62f), regular);
        }

        static void AddLabel(Transform parent, string text, float size, Color color,
                             Vector2 position, TMP_FontAsset font)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(900f, size * 1.6f);
            rect.anchoredPosition = position;
        }

        static Camera CreateCamera(GameObject root)
        {
            var go = new GameObject("~Camera");
            go.transform.SetParent(root.transform, false);

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CardBackground;
            camera.orthographic = true;
            return camera;
        }

        static Canvas CreateCanvas(GameObject root, Camera camera)
        {
            var go = new GameObject("~Canvas", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(SheetWidth, SheetHeight);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void WritePng(Camera camera)
        {
            var target = new RenderTexture(SheetWidth, SheetHeight, 24);
            camera.targetTexture = target;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(SheetWidth, SheetHeight, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, SheetWidth, SheetHeight), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllBytes(OutPath, image.EncodeToPNG());

            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        static TMP_FontAsset LoadFont(string weight) =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}JetBrainsMono-{weight} SDF.asset");
    }
}
