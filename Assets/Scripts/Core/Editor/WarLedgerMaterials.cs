using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // =============================================================================
    // TMP Glow/Underlay materials for the War's Ledger. Deliberately RESTRAINED —
    // the Ledger is a parchment document, not a chrome panel. Two materials:
    //   * DevaHeaderInk — the Devanagari column headers with a subtle ink underlay
    //     for legibility on the warm parchment, no glow.
    //   * InkBodyUnderlay — used sparingly on dense paragraphs; near-invisible
    //     underlay that keeps body text from dissolving into the foxing texture.
    // That's it. Per spec: no warning glows, no vermillion auras — the Ledger
    // has no warnings.
    // =============================================================================
    public static class WarLedgerMaterials
    {
        const string MatDir          = "Assets/UI/WarLedger/Materials/";
        const string DevaFontPath    = "Assets/UI/WarLedger/Fonts/NotoSerifDevanagari SDF.asset";
        const string CormFontPath    = "Assets/UI/TradeScreen/Fonts/CormorantGaramond SDF.asset";

        public const string DevaHeaderInk     = MatDir + "NotoSerifDevaHeaderInk.mat";
        public const string InkBodyUnderlay   = MatDir + "CormorantInkBodyUnderlay.mat";

        [MenuItem("Project Astra/Generate WarLedger Glow Materials")]
        public static void Generate()
        {
            var deva = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DevaFontPath);
            if (deva == null)
            {
                Debug.LogError("WarLedgerMaterials: Devanagari SDF missing — run 'Project Astra/Generate WarLedger Fonts' first.");
                return;
            }
            var corm = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CormFontPath);
            if (corm == null)
            {
                Debug.LogError("WarLedgerMaterials: Cormorant SDF missing under " + CormFontPath);
                return;
            }
            Directory.CreateDirectory(MatDir);

            // Deva header — very faint ink underlay (drives legibility on cream),
            // no glow. Ink colour is 3d2a1a at 60% alpha.
            Create(DevaHeaderInk, deva.material,
                glow: default, glowOuter: 0f,
                underlay: new Color(0x3d/255f, 0x2a/255f, 0x1a/255f, 0.60f),
                underlayOffset: new Vector2(0, -0.025f),
                underlaySoftness: 0f);

            // Body underlay — near-invisible, pulls ink off the foxing texture just
            // enough that large italic passages don't dissolve.
            Create(InkBodyUnderlay, corm.material,
                glow: default, glowOuter: 0f,
                underlay: new Color(0x3d/255f, 0x2a/255f, 0x1a/255f, 0.30f),
                underlayOffset: new Vector2(0, -0.02f),
                underlaySoftness: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("WarLedger glow materials generated.");
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
