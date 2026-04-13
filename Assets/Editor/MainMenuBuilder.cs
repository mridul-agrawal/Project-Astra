using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ===========================================================================================
    // Main Menu — full-screen 1920×1080 title screen (Variant B · "Astra Forge").
    //
    // Target resolution: 1920×1080 (matches the Canvas reference resolution).
    // Screen kind:       FULL_SCREEN (fills the whole canvas, no dim backdrop needed).
    //
    // The Figma source is authored natively at 1920×1080, so Scale = 1.
    // Design nodes live on the "Main Menu (Variant B)" page of the existing UI Concepts file.
    //
    // See `docs/UI_WORKFLOW.md` for the full Figma-to-Unity pipeline.
    // ===========================================================================================
    public static class MainMenuBuilder
    {
        // ---- target resolution & screen kind ----
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = true;
        const float Scale        = 1f;

        // ---- paths ----
        const string SpriteDir = "Assets/UI/MainMenu/Sprites/";
        const string FontDir   = "Assets/UI/UnitInfoPanel/Fonts/"; // reuse TMP fonts from Unit Info Panel

        // ---- colours (for text tints / transparent placeholders) ----
        static readonly Color ColTitle       = new Color32(0xe8, 0xed, 0xff, 0xff);
        static readonly Color ColEyebrow     = new Color32(0x8a, 0x9c, 0xd0, 0xff);
        static readonly Color ColLabel       = new Color32(0xe6, 0xec, 0xff, 0xff);
        static readonly Color ColFooterHint  = new Color32(0xb4, 0xc8, 0xf0, 0x8c);

        // ---- fonts (loaded in Build) ----
        static TMP_FontAsset cinzel;
        static TMP_FontAsset jost;  // we don't have Jost in the project; fall back to Cinzel

        // ==================================================================
        // entry point
        // ==================================================================

        [MenuItem("Project Astra/Build Main Menu")]
        public static void Build()
        {
            if (IsFullScreen && (Mathf.Abs(Sc(1920) - CanvasWidth) > 1f || Mathf.Abs(Sc(1080) - CanvasHeight) > 1f))
            {
                Debug.LogError($"MainMenuBuilder: panel dimensions ({Sc(1920)}×{Sc(1080)}) " +
                               $"do not match canvas reference ({CanvasWidth}×{CanvasHeight}). " +
                               "Fix Scale or IsFullScreen.");
                return;
            }

            // TMP fonts — reuse the assets already generated for the Unit Info Panel.
            // No Jost TMP asset yet, so labels/eyebrow fall back to Cinzel (plain, not
            // Decorative) for legibility at small sizes. Swap to Jost SDF when available.
            cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Cinzel SDF.asset");
            jost   = cinzel;

            var canvas = EnsureCanvas();
            var existing = canvas.transform.Find("MainMenu");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var root = BuildRoot(canvas.transform);

            // Attach runtime controller. MainMenuUI discovers its buttons from
            // ButtonsContainer at OnEnable, so no serialized refs to patch.
            if (root.GetComponent<MainMenuUI>() == null)
                root.AddComponent<MainMenuUI>();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("MainMenu built.");
        }

        // ==================================================================
        // canvas
        // ==================================================================

        static Canvas EnsureCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
                return existing;

            var go = new GameObject("UICanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return c;
        }

        // ==================================================================
        // root hierarchy
        // ==================================================================

        static GameObject BuildRoot(Transform parent)
        {
            // Root — full-stretch transparent RectTransform filling the canvas.
            var root = NewRect("MainMenu", parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildBackdrop(root);
            BuildTopOrnaments(root);
            BuildTitle(root);
            BuildButtons(root);
            BuildFooter(root);

            return root.gameObject;
        }

        // ==================================================================
        // backdrop stack — 5 stacked full-bleed Images + the centered yantra
        // ==================================================================

        static void BuildBackdrop(RectTransform parent)
        {
            var backdrop = NewRect("Backdrop", parent);
            SetStretch(backdrop, 0);

            AddFullBleedImage(backdrop, "SkyBackground", "sky_background.png");
            AddFullBleedImage(backdrop, "Starfield",     "starfield.png");
            AddFullBleedImage(backdrop, "HazeTop",       "haze_top.png");
            AddFullBleedImage(backdrop, "HazeBottom",    "haze_bottom.png");

            // Yantra — centered, fixed size (~880×880 in the Figma design)
            var yantra = NewImage("Yantra", backdrop, new Color(1, 1, 1, 0.55f));
            var yRt = yantra.GetComponent<RectTransform>();
            yRt.anchorMin = new Vector2(0.5f, 0.5f);
            yRt.anchorMax = new Vector2(0.5f, 0.5f);
            yRt.pivot = new Vector2(0.5f, 0.5f);
            yRt.anchoredPosition = Vector2.zero;
            yRt.sizeDelta = V2(880, 880);
            yantra.GetComponent<Image>().sprite = LoadSprite("yantra.png");
            yantra.GetComponent<Image>().preserveAspect = true;
            yantra.GetComponent<Image>().raycastTarget = false;
        }

        static void AddFullBleedImage(RectTransform parent, string name, string sprite)
        {
            var go = NewImage(name, parent, Color.white);
            SetStretch(go, 0);
            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite(sprite);
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }

        // ==================================================================
        // top ornaments — sun (left) and moon (right) sigils
        // ==================================================================

        static void BuildTopOrnaments(RectTransform parent)
        {
            var ornaments = NewRect("TopOrnaments", parent);
            ornaments.anchorMin = new Vector2(0, 1);
            ornaments.anchorMax = new Vector2(1, 1);
            ornaments.pivot = new Vector2(0.5f, 1);
            ornaments.offsetMin = new Vector2(0, -260);
            ornaments.offsetMax = new Vector2(0, 0);

            AddSigil(ornaments, "SunSigil",  "sun_sigil.png",  new Vector2(0, 1), V2(220 + 65,  -80 - 65));
            AddSigil(ornaments, "MoonSigil", "moon_sigil.png", new Vector2(1, 1), V2(-220 - 65, -80 - 65));
        }

        static void AddSigil(RectTransform parent, string name, string sprite, Vector2 anchor, Vector2 pos)
        {
            var go = NewImage(name, parent, Color.white);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = V2(130, 130);
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().sprite = LoadSprite(sprite);
            go.GetComponent<Image>().preserveAspect = true;
            go.GetComponent<Image>().raycastTarget = false;
        }

        // ==================================================================
        // title — eyebrow + wordmark + glowing rule
        // ==================================================================

        static void BuildTitle(RectTransform parent)
        {
            var title = NewRect("Title", parent);
            title.anchorMin = new Vector2(0.5f, 1);
            title.anchorMax = new Vector2(0.5f, 1);
            title.pivot = new Vector2(0.5f, 1);
            title.sizeDelta = V2(1200, 220);
            title.anchoredPosition = V2(0, -180);

            // Eyebrow line
            var eyebrow = NewText("Eyebrow", title, "A TACTICAL CHRONICLE", jost,
                Sc(22), ColEyebrow, TextAlignmentOptions.Top, FontStyles.Normal);
            eyebrow.characterSpacing = 14f;
            var eRt = eyebrow.rectTransform;
            eRt.anchorMin = new Vector2(0, 1);
            eRt.anchorMax = new Vector2(1, 1);
            eRt.pivot = new Vector2(0.5f, 1);
            eRt.sizeDelta = new Vector2(0, Sc(34));
            eRt.anchoredPosition = Vector2.zero;

            // Main wordmark — plain sharp TMP text. Glow is deferred to post-production.
            var main = NewText("TitleMain", title, "PROJECT ASTRA", cinzel,
                Sc(110), ColTitle, TextAlignmentOptions.Top, FontStyles.Bold);
            main.characterSpacing = 10f;
            var mRt = main.rectTransform;
            mRt.anchorMin = new Vector2(0, 1);
            mRt.anchorMax = new Vector2(1, 1);
            mRt.pivot = new Vector2(0.5f, 1);
            mRt.sizeDelta = new Vector2(0, Sc(140));
            mRt.anchoredPosition = V2(0, -50);

            // Glowing rule
            var rule = NewImage("TitleRule", title, Color.white);
            var rRt = rule.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 1);
            rRt.anchorMax = new Vector2(0.5f, 1);
            rRt.pivot = new Vector2(0.5f, 0.5f);
            rRt.sizeDelta = V2(376, 18);
            rRt.anchoredPosition = V2(0, -200);
            rule.GetComponent<Image>().sprite = LoadSprite("title_rule.png");
            rule.GetComponent<Image>().preserveAspect = false;
            rule.GetComponent<Image>().raycastTarget = false;
        }

        // ==================================================================
        // buttons
        // ==================================================================

        // Order matches MainMenuUI's runtime wiring: 0 → Cutscene, 1 → PreBattlePrep, 2 → BattleMap.
        static readonly string[] ButtonLabels =
        {
            "Cutscene", "Pre Battle Prep", "Battle Map"
        };

        static void BuildButtons(RectTransform parent)
        {
            var container = NewRect("ButtonsContainer", parent);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            int n = ButtonLabels.Length;
            container.sizeDelta = V2(900, n * 78 + (n - 1) * 24);
            container.anchoredPosition = V2(0, -150);

            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = Sc(24);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < ButtonLabels.Length; i++)
                BuildButton(container, i, ButtonLabels[i]);
        }

        static void BuildButton(RectTransform parent, int index, string label)
        {
            // Wrapper — pure layout, sized to the visible button (720×78)
            var wrapper = NewRect($"Button_{index + 1:00}_{label.Replace(" ", "")}", parent);
            wrapper.sizeDelta = V2(720, 78);
            var we = wrapper.gameObject.AddComponent<LayoutElement>();
            we.preferredWidth = Sc(720);
            we.preferredHeight = Sc(78);

            // Button component on the wrapper
            var btn = wrapper.gameObject.AddComponent<Button>();

            // Background plaque — exported 768×126 due to baked glow extending beyond bounds,
            // so the visual stretches 48px beyond the wrapper's logical 720×78 on every side.
            var bg = NewImage("Background", wrapper, Color.white);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = V2(768, 126);
            bgRt.anchoredPosition = Vector2.zero;
            bg.GetComponent<Image>().sprite = LoadSprite("button_plaque.png");
            bg.GetComponent<Image>().type = Image.Type.Simple;
            bg.GetComponent<Image>().preserveAspect = false;

            btn.targetGraphic = bg.GetComponent<Image>();

            // Chakra ornaments — left and right, each 76×76
            AddChakra(wrapper, "ChakraLeft",  new Vector2(0, 0.5f), V2(-26, 0),  false);
            AddChakra(wrapper, "ChakraRight", new Vector2(1, 0.5f), V2( 26, 0),  true);

            // Label — plain sharp TMP text, no glow. UpperCase is a display modifier:
            // the stored string stays mixed-case ("Pre Battle Prep") but renders uppercase.
            var labelText = NewText("Label", wrapper, label, jost,
                Sc(32), ColLabel, TextAlignmentOptions.Center, FontStyles.UpperCase);
            labelText.characterSpacing = 12f;
            var lRt = labelText.rectTransform;
            lRt.anchorMin = new Vector2(0, 0);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
        }

        static void AddChakra(RectTransform parent, string name, Vector2 anchor, Vector2 offset, bool mirror)
        {
            var go = NewImage(name, parent, Color.white);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = V2(76, 76);
            rt.anchoredPosition = offset;
            if (mirror) rt.localScale = new Vector3(-1, 1, 1);
            go.GetComponent<Image>().sprite = LoadSprite("chakra_disc.png");
            go.GetComponent<Image>().preserveAspect = true;
            go.GetComponent<Image>().raycastTarget = false;
        }

        // ==================================================================
        // footer
        // ==================================================================

        static void BuildFooter(RectTransform parent)
        {
            var footer = NewRect("Footer", parent);
            footer.anchorMin = new Vector2(0, 0);
            footer.anchorMax = new Vector2(1, 0);
            footer.pivot = new Vector2(0.5f, 0);
            footer.sizeDelta = new Vector2(0, Sc(140));
            footer.anchoredPosition = Vector2.zero;

            // Constellation flourish
            var cons = NewImage("FooterConstellation", footer, new Color(1, 1, 1, 0.7f));
            var cRt = cons.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0);
            cRt.anchorMax = new Vector2(0.5f, 0);
            cRt.pivot = new Vector2(0.5f, 0);
            cRt.sizeDelta = V2(320, 30);
            cRt.anchoredPosition = V2(0, 80);
            cons.GetComponent<Image>().sprite = LoadSprite("footer_constellation.png");
            cons.GetComponent<Image>().preserveAspect = true;
            cons.GetComponent<Image>().raycastTarget = false;

            // Hint text
            var hint = NewText("FooterHint", footer, "CHOOSE YOUR CONSTELLATION", jost,
                Sc(16), ColFooterHint, TextAlignmentOptions.Center, FontStyles.Normal);
            hint.characterSpacing = 8f;
            var hRt = hint.rectTransform;
            hRt.anchorMin = new Vector2(0, 0);
            hRt.anchorMax = new Vector2(1, 0);
            hRt.pivot = new Vector2(0.5f, 0);
            hRt.sizeDelta = new Vector2(0, Sc(24));
            hRt.anchoredPosition = V2(0, 40);
        }

        // ==================================================================
        // scale helpers
        // ==================================================================

        static float Sc(float v) => v * Scale;
        static Vector2 V2(float x, float y) => new Vector2(x * Scale, y * Scale);

        // ==================================================================
        // low-level element helpers
        // ==================================================================

        static Sprite LoadSprite(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + filename);

        static GameObject NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static GameObject NewImage(string name, RectTransform parent, Color color)
            => NewImage(name, (Transform)parent, color);

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static RectTransform NewRect(string name, RectTransform parent)
            => NewRect(name, (Transform)parent);

        static TextMeshProUGUI NewText(string name, Transform parent, string text,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
            => NewText(name, (Transform)parent, text, font, size, color, align, style);

        static void SetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void SetStretch(GameObject go, float inset)
            => SetStretch(go.GetComponent<RectTransform>(), inset);
    }
}
