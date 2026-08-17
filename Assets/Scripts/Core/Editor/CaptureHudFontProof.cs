using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Renders type specimens for the tile info spec at the exact sizes, weights and colours it
    // asks for, so the typography can be approved before any panel is built.
    //
    // Specimens only - no panel structure, no scene changes. Output is a throwaway PNG.
    //
    // Run via 'Project Astra/Capture HUD Font Proof'.
    // ==========================================================================================
    public static class CaptureHudFontProof
    {
        const string FontDir = "Assets/UI/BattleMapHUD/Fonts";
        const string OutputDir = "Assets/Screenshots";
        const int Width = 1280, Height = 720;
        const float Scale = 4f;                 // 480x270 spec -> 1920x1080 canvas

        static float Sc(float logical) => logical * Scale;

        static readonly Color NamePlateTop = new Color32(0x09, 0x0D, 0x1A, 0xEB);
        static readonly Color StripTop     = new Color32(0x09, 0x0D, 0x1A, 0xC7);
        static readonly Color White         = Color.white;
        static readonly Color Cyan          = new Color32(0x4F, 0xD6, 0xF7, 0xFF);
        static readonly Color Red           = new Color32(0xFF, 0x5A, 0x56, 0xFF);
        static readonly Color ChevronWhite  = new Color32(0xFF, 0xFF, 0xFF, 0xB3);

        [MenuItem("Project Astra/Capture HUD Font Proof")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutputDir);

            var camHolder = new GameObject("__ProofCam");
            var cam = camHolder.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1E, 0x24, 0x2E, 0xFF);
            cam.orthographic = true;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var canvasHolder = new GameObject("__ProofCanvas");
            var canvas = canvasHolder.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            var scaler = canvasHolder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);

            BuildSpecimens(canvasHolder.transform);

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            shot.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"{OutputDir}/hud_font_proof.png", shot.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(shot);
            Object.DestroyImmediate(canvasHolder);
            Object.DestroyImmediate(camHolder);
            rt.Release();
            Object.DestroyImmediate(rt);

            AssetDatabase.Refresh();
            Debug.Log("[CaptureHudFontProof] Wrote " + OutputDir + "/hud_font_proof.png");
        }

        static void BuildSpecimens(Transform parent)
        {
            float y = -30f;

            Caption(parent, ref y, "NAME  -  Noto Sans SemiBold 600, 12px spec (48 canvas), tracking +0.02em");
            Band(parent, ref y, NamePlateTop, Sc(24f));
            Label(parent, y + Sc(24f) * 0.5f, Sc(10f), "Protection Tile", "SemiBold", Sc(12f), White, 2f);
            y -= 18f;

            Caption(parent, ref y, "NAME  -  composite + ellipsis behaviour");
            Band(parent, ref y, NamePlateTop, Sc(24f));
            Label(parent, y + Sc(24f) * 0.5f, Sc(10f), "Ground + Flames", "SemiBold", Sc(12f), White, 2f);
            y -= 18f;

            Caption(parent, ref y, "STRIP  -  label Medium 500, value Bold 700, 8px spec (32 canvas)");
            Band(parent, ref y, StripTop, Sc(13f));
            Chips(parent, y + Sc(13f) * 0.5f, false);
            y -= 18f;

            Caption(parent, ref y, "STRIP  -  hazard: U+2212 minus, red value (chevron shown red)");
            Band(parent, ref y, StripTop, Sc(13f));
            Hazard(parent, y + Sc(13f) * 0.5f);
            y -= 18f;

            Caption(parent, ref y, "CHEVRON  -  U+2794 is ABSENT from Noto Sans; left = font glyph (tofu), right = sprite stand-in");
            Band(parent, ref y, StripTop, Sc(13f));
            ChevronTest(parent, y + Sc(13f) * 0.5f);
        }

        static void Caption(Transform parent, ref float y, string text)
        {
            var t = NewText(parent, "caption", text, "Medium", 15f, new Color32(0x9A, 0xA2, 0xAE, 0xFF));
            Place(t.rectTransform, 24f, y, 1220f, 20f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            y -= 24f;
        }

        static void Band(Transform parent, ref float y, Color color, float height)
        {
            var go = new GameObject("band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            Place(go.GetComponent<RectTransform>(), 24f, y, 760f, height);
            y -= height;
        }

        static void Chips(Transform parent, float centreY, bool unused)
        {
            float x = 24f + Sc(8f);
            x = Chevron(parent, centreY, x, ChevronWhite);
            x = Chip(parent, centreY, x, "Avo", "+30", Cyan);
            x = Chip(parent, centreY, x, "Heal/Turn", "+10", Cyan);
            Flag(parent, centreY, x, "Unbreakable");
        }

        static void Hazard(Transform parent, float centreY)
        {
            float x = 24f + Sc(8f);
            x = Chevron(parent, centreY, x, Red);
            Chip(parent, centreY, x, "Damage/Turn", "−10", Red);
        }

        static void ChevronTest(Transform parent, float centreY)
        {
            float x = 24f + Sc(8f);
            var glyph = NewText(parent, "tofu", "➔", "Medium", Sc(8f), ChevronWhite);
            Place(glyph.rectTransform, x, centreY + Sc(6f), Sc(12f), Sc(12f));
            glyph.alignment = TextAlignmentOptions.Midline;

            x += Sc(20f);
            var box = new GameObject("chevronSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(parent, false);
            var img = box.GetComponent<Image>();
            img.color = ChevronWhite;
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI/UnitInfoScreen/Generated/glyph_triangle_right.png");
            Place(box.GetComponent<RectTransform>(), x, centreY + Sc(4f), Sc(8f), Sc(8f));
        }

        static float Chevron(Transform parent, float centreY, float x, Color color)
        {
            var box = new GameObject("chev", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(parent, false);
            var img = box.GetComponent<Image>();
            img.color = color;
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI/UnitInfoScreen/Generated/glyph_triangle_right.png");
            Place(box.GetComponent<RectTransform>(), x, centreY + Sc(4f), Sc(8f), Sc(8f));
            return x + Sc(8f) + Sc(8f);
        }

        static float Chip(Transform parent, float centreY, float x, string label, string value, Color valueColor)
        {
            var l = NewText(parent, "label", label, "Medium", Sc(8f), White);
            Place(l.rectTransform, x, centreY + Sc(6f), Sc(70f), Sc(12f));
            l.alignment = TextAlignmentOptions.MidlineLeft;
            float w = l.GetPreferredValues().x;

            var v = NewText(parent, "value", value, "Bold", Sc(8f), valueColor);
            Place(v.rectTransform, x + w + Sc(3f), centreY + Sc(6f), Sc(40f), Sc(12f));
            v.alignment = TextAlignmentOptions.MidlineLeft;
            float vw = v.GetPreferredValues().x;

            return x + w + Sc(3f) + vw + Sc(8f);
        }

        // A single line of text on a band, at the spec's left padding.
        static void Label(Transform parent, float centreY, float pad, string text,
                          string weight, float size, Color color, float tracking)
        {
            var t = NewText(parent, "name", text, weight, size, color);
            t.characterSpacing = tracking;
            Place(t.rectTransform, 24f + pad, centreY + size * 0.75f, Sc(160f), size * 1.5f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
        }

        static void Flag(Transform parent, float centreY, float x, string text)
        {
            var f = NewText(parent, "flag", text, "SemiBold", Sc(8f), White);
            Place(f.rectTransform, x, centreY + Sc(6f), Sc(90f), Sc(12f));
            f.alignment = TextAlignmentOptions.MidlineLeft;
        }

        static TextMeshProUGUI NewText(Transform parent, string name, string text,
                                       string weight, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/NotoSans-{weight} SDF.asset");
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Top-left origin placement, y measured downward as a negative.
        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }
    }
}
