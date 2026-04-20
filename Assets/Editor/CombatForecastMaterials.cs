using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // TMP Glow/Underlay materials for the Combat Forecast panel (Variant A · Indigo Codex).
    // CSS source of truth: docs/mockups/Combat Forecast Mockups.html (the V2 token readout).
    // Fonts are reused from Assets/UI/TradeScreen/Fonts/ — no duplication.
    // Per UI_WORKFLOW §4.5: shader switched to full TextMeshPro/Distance Field + SDF floats
    // copied from base; glyphs render blank otherwise.
    // ==========================================================================================
    public static class CombatForecastMaterials
    {
        const string FontDir      = "Assets/UI/TradeScreen/Fonts/";
        const string MatDir       = "Assets/UI/CombatForecast/Materials/";
        const string CinzelSdf    = FontDir + "Cinzel SDF.asset";
        const string CormSdf      = FontDir + "CormorantGaramond SDF.asset";

        public const string DmgHeroGlow     = MatDir + "CinzelDmgHeroGlow.mat";
        public const string KoBadgeGlow     = MatDir + "CinzelKoBadgeGlow.mat";
        public const string EffectiveGlow   = MatDir + "CinzelEffectiveGlow.mat";
        public const string StatValueGlow   = MatDir + "JetBrainsStatValueGlow.mat";
        public const string UnitNameGlow    = MatDir + "CormorantUnitNameGlow.mat";
        public const string BrassLabelGlow  = MatDir + "CinzelBrassLabelGlow.mat";
        public const string VsTagGlow       = MatDir + "CinzelVsTagGlow.mat";

        [MenuItem("Project Astra/Generate CombatForecast Glow Materials")]
        public static void Generate()
        {
            var cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelSdf);
            var corm   = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CormSdf);
            if (cinzel == null || corm == null)
            {
                Debug.LogError("TMP font assets missing under " + FontDir +
                    ". Run 'Project Astra/Generate TradeScreen Fonts' first (fonts are shared).");
                return;
            }
            Directory.CreateDirectory(MatDir);

            // DMG hero (Cinzel 900 72px, brassGlow):
            //   text-shadow: 0 0 20px rgba(253,228,154,0.45), 0 0 2px rgba(253,228,154,0.9), 0 3px 0 rgba(0,0,0,0.85)
            Create(DmgHeroGlow, cinzel.material,
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.50f),
                glowOuter: 0.45f,
                underlay: new Color(0, 0, 0, 0.85f),
                underlayOffset: new Vector2(0, -0.045f),
                underlaySoftness: 0.05f);

            // KO badge (Cinzel 900 18px, brassGlow):
            //   text-shadow: 0 0 16px rgba(176,56,42,0.95), 0 0 4px rgba(253,228,154,0.95), 0 2px 0 rgba(0,0,0,0.85)
            Create(KoBadgeGlow, cinzel.material,
                glow: new Color(0xb0/255f, 0x38/255f, 0x2a/255f, 0.95f),
                glowOuter: 0.38f,
                underlay: new Color(0, 0, 0, 0.85f),
                underlayOffset: new Vector2(0, -0.03f),
                underlaySoftness: 0.04f);

            // EFFECTIVE (Cinzel 800 13px, brassGlow):
            //   text-shadow: 0 0 10px rgba(253,228,154,0.55), 0 1px 0 rgba(0,0,0,0.85)
            Create(EffectiveGlow, cinzel.material,
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.55f),
                glowOuter: 0.28f,
                underlay: new Color(0, 0, 0, 0.85f),
                underlayOffset: new Vector2(0, -0.03f),
                underlaySoftness: 0f);

            // Stat value (JetBrains Mono 600 28px, parchmentSel):
            //   text-shadow: 0 0 6px rgba(253,228,154,0.3), 0 2px 0 rgba(0,0,0,0.9)
            Create(StatValueGlow, corm.material, // share SDF atlas; mat props dominate
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.30f),
                glowOuter: 0.22f,
                underlay: new Color(0, 0, 0, 0.9f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0f);

            // Unit name (Cormorant 700 42px, parchmentSel):
            //   text-shadow: 0 0 14px rgba(253,228,154,0.25), 0 2px 0 rgba(0,0,0,0.8)
            Create(UnitNameGlow, corm.material,
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.25f),
                glowOuter: 0.35f,
                underlay: new Color(0, 0, 0, 0.8f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0f);

            // Brass small-caps label (Cinzel 10-13px, brassLite):
            //   text-shadow: 0 0 6px rgba(253,228,154,0.35), 0 1px 0 rgba(0,0,0,0.7)
            Create(BrassLabelGlow, cinzel.material,
                glow: new Color(0xe8/255f, 0xc6/255f, 0x6a/255f, 0.35f),
                glowOuter: 0.22f,
                underlay: new Color(0, 0, 0, 0.7f),
                underlayOffset: new Vector2(0, -0.035f),
                underlaySoftness: 0f);

            // VS tag (Cinzel 800 14px, brassGlow):
            Create(VsTagGlow, cinzel.material,
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.50f),
                glowOuter: 0.28f,
                underlay: new Color(0, 0, 0, 0.85f),
                underlayOffset: new Vector2(0, -0.03f),
                underlaySoftness: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatForecast glow materials generated.");
        }

        static void Create(string path, Material baseMat, Color glow, float glowOuter,
            Color underlay, Vector2 underlayOffset, float underlaySoftness)
        {
            Material mat;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) mat = existing;
            else { mat = new Material(baseMat); AssetDatabase.CreateAsset(mat, path); }

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

            if (glow.a > 0.001f || glowOuter > 0.001f)
            {
                mat.EnableKeyword("GLOW_ON");
                mat.SetColor("_GlowColor", glow);
                mat.SetFloat("_GlowOffset", 0f);
                mat.SetFloat("_GlowInner", 0f);
                mat.SetFloat("_GlowOuter", glowOuter);
                mat.SetFloat("_GlowPower", 1f);
            }
            else mat.DisableKeyword("GLOW_ON");

            if (underlay.a > 0.001f)
            {
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor", underlay);
                mat.SetFloat("_UnderlayOffsetX", underlayOffset.x);
                mat.SetFloat("_UnderlayOffsetY", underlayOffset.y);
                mat.SetFloat("_UnderlayDilate", 0f);
                mat.SetFloat("_UnderlaySoftness", underlaySoftness);
            }
            else mat.DisableKeyword("UNDERLAY_ON");

            EditorUtility.SetDirty(mat);
        }

        static void CopyFloat(Material src, Material dst, string name)
        {
            if (src.HasProperty(name)) dst.SetFloat(name, src.GetFloat(name));
        }
    }
}
