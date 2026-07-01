using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Generates TMP Glow/Underlay materials for the Supply Convoy (Indigo Codex · Variant A).
    // CSS source of truth: docs/mockups/Supply Convoy Mockup.html
    // Fonts are reused from Assets/UI/TradeScreen/Fonts/ — no duplication.
    // ==========================================================================================
    public static class SupplyConvoyMaterials
    {
        const string FontDir   = "Assets/UI/TradeScreen/Fonts/";
        const string MatDir    = "Assets/UI/SupplyConvoy/Materials/";
        const string CinzelSdf = FontDir + "Cinzel SDF.asset";
        const string CormSdf   = FontDir + "CormorantGaramond SDF.asset";

        public const string CartoucheTitleGlow = MatDir + "CinzelCartoucheTitleGlow.mat";
        public const string StockNumGlow       = MatDir + "JetBrainsStockNumGlow.mat";
        public const string FocusedNameGlow    = MatDir + "CormorantFocusedNameGlow.mat";
        public const string BrassLabelGlow     = MatDir + "CinzelBrassLabelGlow.mat";

        [MenuItem("Project Astra/Generate SupplyConvoy Glow Materials")]
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
            if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);

            // Cartouche title (Cinzel 22px, brassGlow):
            //   --sh-label: 0 0 6px rgba(253,228,154,0.35), 0 1px 0 rgba(0,0,0,0.65)
            Create(CartoucheTitleGlow, cinzel.material,
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.35f),
                glowOuter: 0.25f,
                underlay: new Color(0, 0, 0, 0.65f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0f);

            // Stock counter (JetBrains Mono 28px, parchmentSel): soft brassGlow halo
            //   text-shadow: 0 0 10px rgba(253,228,154,0.25)
            Create(StockNumGlow, corm.material,   // sharing Corm atlas; glow settings dominate
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.25f),
                glowOuter: 0.30f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            // Focused convoy row name (Cormorant 24px, parchmentSel):
            //   text-shadow: 0 0 10px rgba(253,228,154,0.35), 0 1px 0 rgba(0,0,0,0.6)
            Create(FocusedNameGlow, corm.material,
                glow: new Color(0xfd/255f, 0xe4/255f, 0x9a/255f, 0.35f),
                glowOuter: 0.30f,
                underlay: new Color(0, 0, 0, 0.6f),
                underlayOffset: new Vector2(0, -0.035f),
                underlaySoftness: 0f);

            // Brass labels (Cinzel 11–13px, brassLite): gilded engraving halo
            Create(BrassLabelGlow, cinzel.material,
                glow: new Color(0xe8/255f, 0xc6/255f, 0x6a/255f, 0.35f),
                glowOuter: 0.22f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SupplyConvoy glow materials generated.");
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
            else mat.DisableKeyword("GLOW_ON");

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
            else mat.DisableKeyword("UNDERLAY_ON");

            EditorUtility.SetDirty(mat);
        }

        static void CopyFloat(Material src, Material dst, string name)
        {
            if (src.HasProperty(name)) dst.SetFloat(name, src.GetFloat(name));
        }
    }
}
