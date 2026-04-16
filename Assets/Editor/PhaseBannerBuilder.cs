using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.Editor
{
    public static class PhaseBannerBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = false;
        const float BannerHeight = 180f;

        const string HudSpriteDir = "Assets/UI/BattleMapHUD/Sprites/";
        const string FontDir      = "Assets/UI/UnitInfoPanel/Fonts/";
        const string MatDir       = "Assets/UI/PhaseBanner/Materials/";

        static TMP_FontAsset cinzelDecor;
        static TMP_FontAsset cormorantItalic;

        [MenuItem("Project Astra/Build Phase Banner (temp)")]
        public static void Build()
        {
            LoadFonts();
            EnsureMaterials();

            var existing = Object.FindAnyObjectByType<UI.PhaseBannerUI>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                Debug.Log("PhaseBannerBuilder: Destroyed existing PhaseBannerUI.");
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("PhaseBannerBuilder: No Canvas found in scene. Build the HUD first.");
                return;
            }

            var wrapperGO = new GameObject("PhaseBanner", typeof(RectTransform));
            wrapperGO.transform.SetParent(canvas.transform, false);
            wrapperGO.transform.SetAsLastSibling();
            SetStretch(wrapperGO.GetComponent<RectTransform>(), 0f);

            var dimOverlay = BuildDimOverlay(wrapperGO.transform);
            var bannerRoot = BuildBannerRoot(wrapperGO.transform);
            var bg = BuildBackground(bannerRoot);
            var borderTop = BuildBorder(bannerRoot, "BorderTop", true);
            var borderBottom = BuildBorder(bannerRoot, "BorderBottom", false);
            var innerBorderTop = BuildInnerBorder(bannerRoot, "InnerBorderTop", true);
            var innerBorderBottom = BuildInnerBorder(bannerRoot, "InnerBorderBottom", false);
            var orbs = BuildOrbs(bannerRoot);
            var phaseText = BuildPhaseText(bannerRoot);
            var turnText = BuildTurnText(bannerRoot);

            var cg = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var bannerUI = wrapperGO.AddComponent<UI.PhaseBannerUI>();
            WireReferences(bannerUI, bannerRoot, dimOverlay, bg, borderTop, borderBottom,
                innerBorderTop, innerBorderBottom, orbs, phaseText, turnText);

            Undo.RegisterCreatedObjectUndo(wrapperGO, "Build Phase Banner");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("PhaseBannerBuilder: Phase banner built successfully.");
        }

        static void LoadFonts()
        {
            cinzelDecor = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CinzelDecorative SDF.asset");
            cormorantItalic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramondItalic SDF.asset");
        }

        static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI/PhaseBanner"))
                AssetDatabase.CreateFolder("Assets/UI", "PhaseBanner");
            if (!AssetDatabase.IsValidFolder("Assets/UI/PhaseBanner/Materials"))
                AssetDatabase.CreateFolder("Assets/UI/PhaseBanner", "Materials");

            EnsureGlow(MatDir + "PhaseTextGoldGlow.mat",
                new Color(0.831f, 0.635f, 0.298f, 0.55f), 0.55f);
            EnsureGlow(MatDir + "PhaseTextCrimsonGlow.mat",
                new Color(0.753f, 0.271f, 0.220f, 0.55f), 0.55f);
            EnsureGlow(MatDir + "PhaseTextEmeraldGlow.mat",
                new Color(0.165f, 0.541f, 0.290f, 0.55f), 0.55f);
        }

        static void EnsureGlow(string path, Color glowColor, float glowOuter)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            if (cinzelDecor == null) return;

            var baseMat = cinzelDecor.material;
            var mat = new Material(baseMat);

            var fullShader = Shader.Find("TextMeshPro/Distance Field");
            if (fullShader != null) mat.shader = fullShader;

            string[] props = { "_GradientScale", "_TextureWidth", "_TextureHeight",
                               "_WeightNormal", "_WeightBold",
                               "_ScaleRatioA", "_ScaleRatioB", "_ScaleRatioC" };
            foreach (var p in props)
                if (baseMat.HasProperty(p)) mat.SetFloat(p, baseMat.GetFloat(p));
            if (baseMat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", baseMat.GetTexture("_MainTex"));

            mat.EnableKeyword("GLOW_ON");
            mat.SetColor("_GlowColor", glowColor);
            mat.SetFloat("_GlowOuter", glowOuter);
            mat.SetFloat("_GlowInner", 0f);
            mat.SetFloat("_GlowPower", 1f);

            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", -0.06f);
            mat.SetFloat("_UnderlaySoftness", 0.18f);

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }

        static Image BuildDimOverlay(Transform parent)
        {
            var go = NewImage("DimOverlay", parent, new Color(0.0196f, 0.0118f, 0.0196f, 0.55f));
            var rt = go.GetComponent<RectTransform>();
            SetStretch(rt, 0f);
            go.SetActive(false);
            return go.GetComponent<Image>();
        }

        static RectTransform BuildBannerRoot(Transform parent)
        {
            var go = new GameObject("PhaseBannerRoot", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            float halfBanner = BannerHeight / CanvasHeight / 2f;
            rt.anchorMin = new Vector2(0f, 0.5f - halfBanner);
            rt.anchorMax = new Vector2(1f, 0.5f + halfBanner);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image BuildBackground(RectTransform parent)
        {
            var go = NewImage("Background", parent, Color.white);
            var rt = go.GetComponent<RectTransform>();
            SetStretch(rt, 0f);
            var img = go.GetComponent<Image>();
            img.sprite = LoadHudSprite("panel_bg.png");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = new Color(1f, 1f, 1f, 0.92f);
            return img;
        }

        static Image BuildBorder(RectTransform parent, string name, bool isTop)
        {
            var go = NewImage(name, parent, new Color(0.831f, 0.635f, 0.298f, 0.55f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            return go.GetComponent<Image>();
        }

        static Image BuildInnerBorder(RectTransform parent, string name, bool isTop)
        {
            var go = NewImage(name, parent, new Color(0.831f, 0.635f, 0.298f, 0.2f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
            float insetY = isTop ? -8f : 8f;
            rt.sizeDelta = new Vector2(-120f, 1f);
            rt.anchoredPosition = new Vector2(0f, insetY);
            return go.GetComponent<Image>();
        }

        static Image[] BuildOrbs(RectTransform parent)
        {
            var sprite = LoadHudSprite("corner_boss.png");
            var orbs = new Image[4];

            orbs[0] = BuildOrb(parent, "OrbTL", sprite, new Vector2(0f, 1f), new Vector2(56f, -8f));
            orbs[1] = BuildOrb(parent, "OrbTR", sprite, new Vector2(1f, 1f), new Vector2(-56f, -8f));
            orbs[2] = BuildOrb(parent, "OrbBL", sprite, new Vector2(0f, 0f), new Vector2(56f, 8f));
            orbs[3] = BuildOrb(parent, "OrbBR", sprite, new Vector2(1f, 0f), new Vector2(-56f, 8f));

            return orbs;
        }

        static Image BuildOrb(RectTransform parent, string name, Sprite sprite,
            Vector2 anchor, Vector2 offset)
        {
            var go = NewImage(name, parent, Color.white);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(18f, 18f);
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            img.color = new Color(0.831f, 0.635f, 0.298f, 1f);
            return img;
        }

        static TextMeshProUGUI BuildPhaseText(RectTransform parent)
        {
            var go = new GameObject("PhaseText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.35f);
            rt.anchorMax = new Vector2(0.9f, 0.75f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = "Player Phase";
            if (cinzelDecor != null) t.font = cinzelDecor;
            t.fontSize = 78;
            t.color = new Color(0.961f, 0.824f, 0.478f, 1f);
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Normal;
            t.characterSpacing = 22f;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;

            var goldMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "PhaseTextGoldGlow.mat");
            if (goldMat != null) t.fontMaterial = goldMat;

            return t;
        }

        static TextMeshProUGUI BuildTurnText(RectTransform parent)
        {
            var go = new GameObject("TurnText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0.12f);
            rt.anchorMax = new Vector2(0.7f, 0.38f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = "Turn 1";
            if (cormorantItalic != null) t.font = cormorantItalic;
            t.fontSize = 24;
            t.fontStyle = FontStyles.Italic;
            t.color = new Color(0.541f, 0.478f, 0.345f, 1f);
            t.alignment = TextAlignmentOptions.Center;
            t.characterSpacing = 18f;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;

            return t;
        }

        static void WireReferences(UI.PhaseBannerUI bannerUI, RectTransform root,
            Image dimOverlay, Image bg, Image borderTop, Image borderBottom,
            Image innerBorderTop, Image innerBorderBottom,
            Image[] orbs, TextMeshProUGUI phaseText, TextMeshProUGUI turnText)
        {
            var so = new SerializedObject(bannerUI);

            var turnChannel = AssetDatabase.LoadAssetAtPath<TurnEventChannel>(
                "Assets/ScriptableObjects/Core/TurnEventChannel.asset");
            so.FindProperty("_turnEventChannel").objectReferenceValue = turnChannel;

            so.FindProperty("_bannerRoot").objectReferenceValue = root;
            so.FindProperty("_dimOverlay").objectReferenceValue = dimOverlay;
            so.FindProperty("_borderTop").objectReferenceValue = borderTop;
            so.FindProperty("_borderBottom").objectReferenceValue = borderBottom;
            so.FindProperty("_innerBorderTop").objectReferenceValue = innerBorderTop;
            so.FindProperty("_innerBorderBottom").objectReferenceValue = innerBorderBottom;
            so.FindProperty("_phaseText").objectReferenceValue = phaseText;
            so.FindProperty("_turnText").objectReferenceValue = turnText;

            var orbProp = so.FindProperty("_orbImages");
            orbProp.arraySize = orbs.Length;
            for (int i = 0; i < orbs.Length; i++)
                orbProp.GetArrayElementAtIndex(i).objectReferenceValue = orbs[i];

            var goldMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "PhaseTextGoldGlow.mat");
            var crimsonMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "PhaseTextCrimsonGlow.mat");
            var emeraldMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "PhaseTextEmeraldGlow.mat");

            so.FindProperty("_playerGlowMat").objectReferenceValue = goldMat;
            so.FindProperty("_enemyGlowMat").objectReferenceValue = crimsonMat;
            so.FindProperty("_alliedGlowMat").objectReferenceValue = emeraldMat;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite LoadHudSprite(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(HudSpriteDir + filename);

        static GameObject NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static void SetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
