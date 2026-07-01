using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Generates the four TMP Glow/Underlay materials used by the Unit Info Panel.
    // CSS values are the source of truth (see docs/mockups/unit_info_06_nila_dharma.html).
    //
    // Why a full-shader Distance Field (not Mobile): Mobile variant lacks the Glow feature
    // — setting _GlowColor on it is a silent no-op. See UI_WORKFLOW.md §4.5 gotcha 1.
    //
    // Why copy SDF floats manually: switching shaders loses the per-font SDF scale params
    // and the atlas texture. Without these copied over the text renders blank.
    // See UI_WORKFLOW.md §4.5 gotcha 2.
    // ==========================================================================================
    public static class UnitInfoPanelMaterials
    {
        const string FontDir    = "Assets/UI/UnitInfoPanel/Fonts/";
        const string MatDir     = "Assets/UI/UnitInfoPanel/Materials/";
        const string CinzelFont = FontDir + "Cinzel SDF.asset";

        public const string CharNameGlow        = MatDir + "CinzelCharNameGlow.mat";
        public const string PageHeaderGlow      = MatDir + "CinzelPageHeaderGlow.mat";
        public const string WeaponRankAccessGlow = MatDir + "CinzelWeaponRankAccessibleGlow.mat";
        public const string WeaponRankDefaultUL  = MatDir + "CinzelWeaponRankDefaultUnderlay.mat";

        [MenuItem("Project Astra/Generate UnitInfo Glow Materials")]
        public static void Generate()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelFont);
            if (font == null) { Debug.LogError($"Cinzel SDF font not found at {CinzelFont}"); return; }
            if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);

            // char-name: soft outer glow in sapphire + drop-shadow underlay
            // CSS: text-shadow: 0 2px 4px rgba(0,0,0,0.6), 0 0 14px rgba(42,74,138,0.2)
            Create(CharNameGlow, font.material,
                glow: new Color(0x2a/255f, 0x4a/255f, 0x8a/255f, 0.20f),
                glowOuter: 0.35f,
                underlay: new Color(0, 0, 0, 0.60f),
                underlayOffset: new Vector2(0, -0.05f),
                underlaySoftness: 0.15f);

            // page-header: gentler glow + smaller drop
            // CSS: text-shadow: 0 1px 3px rgba(0,0,0,0.5), 0 0 12px rgba(42,74,138,0.15)
            Create(PageHeaderGlow, font.material,
                glow: new Color(0x2a/255f, 0x4a/255f, 0x8a/255f, 0.15f),
                glowOuter: 0.30f,
                underlay: new Color(0, 0, 0, 0.50f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0.10f);

            // weapon rank accessible: warm gold glow, no underlay
            // CSS: text-shadow: 0 0 6px rgba(200,160,64,0.4)
            Create(WeaponRankAccessGlow, font.material,
                glow: new Color(0xc8/255f, 0xa0/255f, 0x40/255f, 0.40f),
                glowOuter: 0.20f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            // weapon rank default: crisp drop-shadow, no glow
            // CSS: text-shadow: 1px 1px 0 rgba(0,0,0,0.3)
            Create(WeaponRankDefaultUL, font.material,
                glow: default,
                glowOuter: 0f,
                underlay: new Color(0, 0, 0, 0.30f),
                underlayOffset: new Vector2(0.02f, -0.02f),
                underlaySoftness: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("UnitInfo glow materials generated.");
        }

        static void Create(string path, Material baseMat, Color glow, float glowOuter,
            Color underlay, Vector2 underlayOffset, float underlaySoftness)
        {
            Material mat;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { mat = existing; }
            else                  { mat = new Material(baseMat); AssetDatabase.CreateAsset(mat, path); }

            mat.shader = Shader.Find("TextMeshPro/Distance Field");

            // Copy SDF-specific properties off the base (Mobile) material so glyphs still render.
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
