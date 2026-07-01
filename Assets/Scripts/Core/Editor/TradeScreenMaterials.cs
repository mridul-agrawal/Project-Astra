using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Generates the TMP Glow/Underlay materials used by the Trade Screen (Indigo Codex).
    // CSS source of truth: docs/mockups/Project Astra Trade Screen.unpacked/assets/e7379e7e-*.js
    // (JSX inline styles — the `boxShadow`, `text-shadow`-equivalent values).
    //
    // Why full-shader Distance Field, not Mobile: Mobile lacks the Glow feature. See
    // UI_WORKFLOW §4.5 gotcha 1.
    //
    // Why copy SDF floats manually: switching shaders loses per-font SDF scale params and
    // atlas texture — glyphs render blank otherwise. See UI_WORKFLOW §4.5 gotcha 2.
    // ==========================================================================================
    public static class TradeScreenMaterials
    {
        const string FontDir     = "Assets/UI/TradeScreen/Fonts/";
        const string MatDir      = "Assets/UI/TradeScreen/Materials/";
        const string CinzelSdf   = FontDir + "Cinzel SDF.asset";
        const string CormorantItalicSdf = FontDir + "CormorantGaramondItalic SDF.asset";

        public const string UnitNameGlow    = MatDir + "CormorantItalicUnitNameGlow.mat";
        public const string BannerGlow      = MatDir + "CinzelBannerGlow.mat";
        public const string ButtonHoverGlow = MatDir + "CormorantButtonHoverGlow.mat";
        public const string HoldingGlow     = MatDir + "CormorantItalicHoldingGlow.mat";

        [MenuItem("Project Astra/Generate TradeScreen Glow Materials")]
        public static void Generate()
        {
            var cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelSdf);
            var cormItal = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CormorantItalicSdf);
            if (cinzel == null || cormItal == null)
            {
                Debug.LogError("TMP font assets missing. Run 'Project Astra/Generate TradeScreen Fonts' first.");
                return;
            }
            if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);

            // Unit name (Cormorant italic 56px): soft indigo underlay for readability, light brass glow
            // CSS peer: unit name has no explicit text-shadow in JSX but sits on indigo bg;
            // adding a subtle black underlay + brass glow preserves legibility on busy chrome.
            Create(UnitNameGlow, cormItal.material,
                glow: new Color(0xe8 / 255f, 0xc6 / 255f, 0x6a / 255f, 0.15f),
                glowOuter: 0.25f,
                underlay: new Color(0, 0, 0, 0.50f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0.12f);

            // Banner label (Cinzel 13px uppercase, brass-lite): soft brass glow only
            // CSS peer: letterSpacing 10 + brass color; glow sells the "parchment-on-page" feel.
            Create(BannerGlow, cinzel.material,
                glow: new Color(0xe8 / 255f, 0xc6 / 255f, 0x6a / 255f, 0.40f),
                glowOuter: 0.22f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            // Button hover (Cormorant 20px, #fff5d8): warm highlight + drop shadow
            // Peer CSS: bg tint 15% brassLite + border brassLite → text picks up sibling glow cue.
            Create(ButtonHoverGlow, cormItal.material,
                glow: new Color(0xf5 / 255f, 0xe0 / 255f, 0xa0 / 255f, 0.30f),
                glowOuter: 0.20f,
                underlay: new Color(0, 0, 0, 0.40f),
                underlayOffset: new Vector2(0, -0.03f),
                underlaySoftness: 0.05f);

            // Holding value (Cormorant italic 24px, parchment): moderate drop shadow for lift
            Create(HoldingGlow, cormItal.material,
                glow: default,
                glowOuter: 0f,
                underlay: new Color(0, 0, 0, 0.55f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0.08f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TradeScreen glow materials generated.");
        }

        static void Create(string path, Material baseMat, Color glow, float glowOuter,
            Color underlay, Vector2 underlayOffset, float underlaySoftness)
        {
            Material mat;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { mat = existing; }
            else                  { mat = new Material(baseMat); AssetDatabase.CreateAsset(mat, path); }

            mat.shader = Shader.Find("TextMeshPro/Distance Field");

            CopyFloat(baseMat, mat, "_GradientScale");
            CopyFloat(baseMat, mat, "_TextureWidth");
            CopyFloat(baseMat, mat, "_TextureHeight");
            CopyFloat(baseMat, mat, "_WeightNormal");
            CopyFloat(baseMat, mat, "_WeightBold");
            CopyFloat(baseMat, mat, "_ScaleRatioA");
            CopyFloat(baseMat, mat, "_ScaleRatioB");
            CopyFloat(baseMat, mat, "_ScaleRatioC");
            mat.SetTexture("_MainTex", baseMat.GetTexture("_MainTex"));

            bool hasGlow = glow.a > 0.001f || glowOuter > 0.001f;
            if (hasGlow)
            {
                mat.EnableKeyword("GLOW_ON");
                mat.SetColor("_GlowColor", glow);
                mat.SetFloat("_GlowOffset", 0f);
                mat.SetFloat("_GlowInner", 0f);
                mat.SetFloat("_GlowOuter", glowOuter);
                mat.SetFloat("_GlowPower", 1f);
            }
            else
            {
                mat.DisableKeyword("GLOW_ON");
            }

            bool hasUnderlay = underlay.a > 0.001f;
            if (hasUnderlay)
            {
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor", underlay);
                mat.SetFloat("_UnderlayOffsetX", underlayOffset.x);
                mat.SetFloat("_UnderlayOffsetY", underlayOffset.y);
                mat.SetFloat("_UnderlayDilate", 0f);
                mat.SetFloat("_UnderlaySoftness", underlaySoftness);
            }
            else
            {
                mat.DisableKeyword("UNDERLAY_ON");
            }

            EditorUtility.SetDirty(mat);
        }

        static void CopyFloat(Material src, Material dst, string name)
        {
            if (src.HasProperty(name)) dst.SetFloat(name, src.GetFloat(name));
        }
    }
}
