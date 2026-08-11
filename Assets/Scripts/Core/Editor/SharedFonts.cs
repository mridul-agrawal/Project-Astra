using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Generates TMP SDF font assets for the shared JetBrains Mono family from the TTFs in
    // Assets/UI/Shared/Fonts/. Pre-populates the ASCII atlas and saves the atlas texture
    // + material as sub-assets — otherwise glyphs render blank on first play-mode entry
    // (see UI_WORKFLOW §4.3).
    //
    // We bake a real Bold rather than leaning on TMP's <b>, which fakes weight by smearing the
    // SDF and falls apart at the 6-8px sizes the unit info card asks for.
    //
    // Run via 'Project Astra/Generate Shared Fonts'. Idempotent — existing assets are skipped.
    // ==========================================================================================
    public static class SharedFonts
    {
        const string FontDir = "Assets/UI/Shared/Fonts/";

        static readonly string[] Weights = { "Regular", "Medium", "Bold", "ExtraBold" };

        // Standard printable ASCII — kept in sync with TradeScreenFonts. The mandala bullet "◆",
        // middle-dot "·" and em-dash "—" are loaded explicitly below.
        const string AsciiRange =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~";
        const string ExtraChars = "◆·—";

        [MenuItem("Project Astra/Generate Shared Fonts")]
        public static void Generate()
        {
            foreach (string weight in Weights)
                GenerateOne($"JetBrainsMono-{weight}.ttf", $"JetBrainsMono-{weight} SDF.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SharedFonts] JetBrains Mono TMP font assets generated.");
        }

        static void GenerateOne(string ttfFile, string outAsset)
        {
            string ttfPath = FontDir + ttfFile;
            string outPath = FontDir + outAsset;

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null)
            {
                Debug.Log($"[SharedFonts] {outAsset} already exists — skipping.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[SharedFonts] TTF missing at {ttfPath}");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            fontAsset.TryAddCharacters(AsciiRange + ExtraChars);

            AssetDatabase.CreateAsset(fontAsset, outPath);
            SaveAtlasAsSubAssets(fontAsset);
            EditorUtility.SetDirty(fontAsset);
        }

        static void SaveAtlasAsSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture != null && !AssetDatabase.IsSubAsset(texture))
                        AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }

            if (fontAsset.material != null && !AssetDatabase.IsSubAsset(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
    }
}
