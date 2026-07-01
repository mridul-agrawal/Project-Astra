using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjectAstra.EditorTools
{
    // =============================================================================
    // Generates the Noto Serif Devanagari TMP SDF font asset used by War's Ledger
    // column headers (जो गिरे / क्या निभाया, क्या नहीं / जो बचे). Pre-populates the
    // atlas with the Devanagari Unicode range + the literal header characters so
    // no glyph-miss on first play-mode entry.
    //
    // Source TTF: Assets/UI/WarLedger/Fonts/NotoSerifDevanagari.ttf
    // Follows UI_WORKFLOW §4.3: CreateFontAsset(..., SDFAA, 1024, 1024, Dynamic) +
    // AssetDatabase.AddObjectToAsset for atlas texture + material sub-assets.
    // =============================================================================
    public static class WarLedgerFonts
    {
        const string FontDir  = "Assets/UI/WarLedger/Fonts/";
        const string TtfPath  = FontDir + "NotoSerifDevanagari.ttf";
        const string SdfPath  = FontDir + "NotoSerifDevanagari SDF.asset";

        // Cover the Devanagari block (U+0900 ... U+097F) + the specific literal
        // characters the Ledger's column headers use. Duplication in the literal
        // string is harmless — TryAddCharacters is idempotent.
        const string DevanagariHeaders = "जो गिरे क्या निभाया, क्या नहीं जो बचे";

        [MenuItem("Project Astra/Generate WarLedger Fonts")]
        public static void Generate()
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (ttf == null)
            {
                Debug.LogError("WarLedgerFonts: NotoSerifDevanagari.ttf missing at " + TtfPath);
                return;
            }

            Directory.CreateDirectory(FontDir);

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
            if (existing != null)
            {
                Debug.Log("WarLedgerFonts: SDF asset already exists; top-up Devanagari glyphs only.");
                TopUpGlyphs(existing);
                return;
            }

            // Sampling 90pt with 9 padding — matches TradeScreen's shipped fonts so
            // the visual weight reads consistently across the suite.
            var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic);
            fa.name = "NotoSerifDevanagari SDF";

            AssetDatabase.CreateAsset(fa, SdfPath);

            // Pre-populate ASCII + the Devanagari range so glyphs render on first entry.
            var ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            fa.TryAddCharacters(ascii);

            // Unicode Devanagari block 0x0900–0x097F (128 codepoints).
            var sb = new System.Text.StringBuilder();
            for (int cp = 0x0900; cp <= 0x097F; cp++) sb.Append(char.ConvertFromUtf32(cp));
            fa.TryAddCharacters(sb.ToString());

            // Belt-and-braces: the literal header glyphs.
            fa.TryAddCharacters(DevanagariHeaders);

            // Persist atlas + material as sub-assets so they survive domain reload.
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            AssetDatabase.AddObjectToAsset(fa.material, fa);

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("WarLedgerFonts: NotoSerifDevanagari SDF created at " + SdfPath);
        }

        static void TopUpGlyphs(TMP_FontAsset fa)
        {
            var sb = new System.Text.StringBuilder();
            for (int cp = 0x0900; cp <= 0x097F; cp++) sb.Append(char.ConvertFromUtf32(cp));
            fa.TryAddCharacters(sb.ToString());
            fa.TryAddCharacters(DevanagariHeaders);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
        }
    }
}
