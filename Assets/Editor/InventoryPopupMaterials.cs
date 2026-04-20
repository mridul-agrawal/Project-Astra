using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Generates TMP Glow/Underlay materials for the Inventory Popup (Indigo Codex).
    // CSS source of truth: docs/mockups/Indigo Codex Inventory.unpacked/assets/*.js
    //
    // Fonts are reused from Assets/UI/TradeScreen/Fonts/ — no duplication.
    // Shader must be the full TextMeshPro/Distance Field (not Mobile) so Glow works;
    // SDF floats + atlas are copied off the font's base material (UI_WORKFLOW §4.5 gotchas).
    // ==========================================================================================
    public static class InventoryPopupMaterials
    {
        const string FontDir      = "Assets/UI/TradeScreen/Fonts/";
        const string MatDir       = "Assets/UI/InventoryPopup/Materials/";
        const string CinzelSdf    = FontDir + "Cinzel SDF.asset";
        const string CormSdf      = FontDir + "CormorantGaramond SDF.asset";
        const string CormItalSdf  = FontDir + "CormorantGaramondItalic SDF.asset";

        public const string StatValueGlow     = MatDir + "CormorantStatValueGlow.mat";
        public const string ItemNameUnderlay  = MatDir + "CormorantItemNameUnderlay.mat";
        public const string BrassLabelGlow    = MatDir + "CinzelBrassLabelGlow.mat";
        public const string HeaderGlyphGlow   = MatDir + "CinzelHeaderGlyphGlow.mat";

        [MenuItem("Project Astra/Generate InventoryPopup Glow Materials")]
        public static void Generate()
        {
            var cinzel   = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelSdf);
            var corm     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CormSdf);
            var cormItal = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CormItalSdf);
            if (cinzel == null || corm == null || cormItal == null)
            {
                Debug.LogError("TMP font assets missing under " + FontDir +
                    ". Run 'Project Astra/Generate TradeScreen Fonts' first (fonts are shared).");
                return;
            }
            if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);

            // Stat pill value (Cormorant 600 64px): text-shadow: 0 0 16px rgba(253,228,154,0.35)
            // Half-em at 64px ≈ 32; 16/32 = 0.5 → _GlowOuter ≈ 0.5 (but clamped by atlas padding)
            Create(StatValueGlow, corm.material,
                glow: new Color(0xfd / 255f, 0xe4 / 255f, 0x9a / 255f, 0.35f),
                glowOuter: 0.45f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            // Item name in selected row (Cormorant 600 26px): no explicit shadow in JSX,
            // but row sits on vermillion gradient — black underlay boosts legibility.
            Create(ItemNameUnderlay, corm.material,
                glow: default,
                glowOuter: 0f,
                underlay: new Color(0, 0, 0, 0.55f),
                underlayOffset: new Vector2(0, -0.04f),
                underlaySoftness: 0.08f);

            // Brass labels (Cinzel 11px, letterSpacing 5px, #e8c66a): soft brass halo
            // Peer CSS: Cinzel text with brassLite fill; glow sells the "gilded engraving" vibe.
            Create(BrassLabelGlow, cinzel.material,
                glow: new Color(0xe8 / 255f, 0xc6 / 255f, 0x6a / 255f, 0.40f),
                glowOuter: 0.25f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            // Header cartouche glyphs (Cinzel 16px, #fde49a): brighter glow than labels
            Create(HeaderGlyphGlow, cinzel.material,
                glow: new Color(0xfd / 255f, 0xe4 / 255f, 0x9a / 255f, 0.50f),
                glowOuter: 0.32f,
                underlay: default,
                underlayOffset: Vector2.zero,
                underlaySoftness: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("InventoryPopup glow materials generated.");
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
