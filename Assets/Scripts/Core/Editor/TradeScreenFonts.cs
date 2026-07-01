using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Generates TMP SDF font assets for the Trade Screen from the TTFs in
    // Assets/UI/TradeScreen/Fonts/. Pre-populates the ASCII atlas and saves the atlas texture
    // + material as sub-assets — otherwise glyphs render blank on first play-mode entry
    // (see UI_WORKFLOW §4.3).
    //
    // Run once via 'Project Astra/Generate TradeScreen Fonts'. Idempotent — if an SDF asset
    // already exists it is left untouched.
    // ==========================================================================================
    public static class TradeScreenFonts
    {
        const string FontDir = "Assets/UI/TradeScreen/Fonts/";

        // Standard printable ASCII — keep in sync with UnitInfoPanel's range. The mandala
        // bullet "◆" and middle-dot "·" are loaded explicitly below.
        const string AsciiRange =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~";
        const string ExtraChars = "◆·—";

        [MenuItem("Project Astra/Generate TradeScreen Fonts")]
        public static void Generate()
        {
            GenerateOne("Cinzel-VF.ttf",                   "Cinzel SDF.asset");
            GenerateOne("CormorantGaramond-VF.ttf",        "CormorantGaramond SDF.asset");
            GenerateOne("CormorantGaramond-Italic-VF.ttf", "CormorantGaramondItalic SDF.asset");
            GenerateOne("EBGaramond-VF.ttf",               "EBGaramond SDF.asset");
            GenerateOne("JetBrainsMono-VF.ttf",            "JetBrainsMono SDF.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TradeScreen TMP font assets generated.");
        }

        static void GenerateOne(string ttfFile, string outAsset)
        {
            string ttfPath = FontDir + ttfFile;
            string outPath = FontDir + outAsset;

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null)
            {
                Debug.Log($"[TradeScreenFonts] {outAsset} already exists — skipping.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[TradeScreenFonts] TTF missing at {ttfPath}");
                return;
            }

            var fa = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            fa.TryAddCharacters(AsciiRange + ExtraChars);

            AssetDatabase.CreateAsset(fa, outPath);
            // Save atlas textures + material as sub-assets so they persist with the font asset.
            if (fa.atlasTextures != null)
            {
                foreach (var tex in fa.atlasTextures)
                {
                    if (tex != null && !AssetDatabase.IsSubAsset(tex))
                        AssetDatabase.AddObjectToAsset(tex, fa);
                }
            }
            if (fa.material != null && !AssetDatabase.IsSubAsset(fa.material))
                AssetDatabase.AddObjectToAsset(fa.material, fa);

            EditorUtility.SetDirty(fa);
        }
    }
}
