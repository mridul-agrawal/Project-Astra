using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Creates the TextMeshPro font assets for the battle-map HUD from the Noto Sans TTFs.
    //
    // The tile info spec asks for one neutral proportional sans at weights 500 / 600 / 700. Those
    // are three real font files rather than one face with synthetic bold, so each gets its own
    // asset and the weights are the designer's, not Unity's approximation.
    //
    // Atlases are dynamic so nothing ever renders as tofu, but the character set the panel
    // actually uses is baked in here at author time to keep runtime atlas churn out of git.
    //
    // Run via 'Project Astra/Build HUD Font Assets'.
    // ==========================================================================================
    public static class BuildHudFontAssets
    {
        const string FontDir = "Assets/UI/BattleMapHUD/Fonts";

        static readonly string[] Weights = { "Medium", "SemiBold", "Bold" };

        // Atlas settings, matching the project's other UI font assets.
        const int SamplingPointSize = 90;
        const int AtlasPadding = 9;
        const int AtlasSize = 1024;

        [MenuItem("Project Astra/Build HUD Font Assets")]
        public static void Build()
        {
            var report = new StringBuilder();

            foreach (string weight in Weights)
            {
                string ttf = $"{FontDir}/NotoSans-{weight}.ttf";
                var source = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                if (source == null)
                {
                    Debug.LogError("[BuildHudFontAssets] Missing " + ttf);
                    continue;
                }

                string assetPath = $"{FontDir}/NotoSans-{weight} SDF.asset";
                TMP_FontAsset font = CreateOrReplace(source, assetPath);
                string missing = Populate(font);

                EditorUtility.SetDirty(font);
                report.AppendLine($"  NotoSans-{weight} SDF  ->  {assetPath}" +
                                  (string.IsNullOrEmpty(missing) ? "   (full coverage)" : $"   MISSING: {missing}"));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildHudFontAssets] Built HUD font assets:\n" + report);
        }

        static TMP_FontAsset CreateOrReplace(Font source, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
                source, SamplingPointSize, AtlasPadding, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);

            AssetDatabase.CreateAsset(font, assetPath);

            // The atlas texture and material have to live inside the asset or the import loses them.
            if (font.atlasTexture != null)
            {
                font.atlasTexture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            }
            if (font.material != null)
            {
                font.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
            return font;
        }

        // Everything the panel can render: terrain names, chip labels and values, the signs, and
        // the two symbols the spec calls for by codepoint.
        static string CharacterSet()
        {
            var set = new StringBuilder();
            for (char c = ' '; c <= '~'; c++) set.Append(c);   // printable ASCII
            set.Append('−');                              // − minus sign, §6
            set.Append('➔');                              // ➔ heavy wide-headed arrow, §6 chevron
            set.Append('…');                              // … ellipsis for the truncated name
            set.Append('·');                              // · middle dot
            return set.ToString();
        }

        static string Populate(TMP_FontAsset font)
        {
            font.TryAddCharacters(CharacterSet(), out string missing);
            return Describe(missing);
        }

        // Codepoints read better than raw glyphs in a console report.
        static string Describe(string missing)
        {
            if (string.IsNullOrEmpty(missing)) return "";

            var parts = new List<string>();
            foreach (char c in missing) parts.Add($"U+{((int)c):X4}");
            return string.Join(" ", parts);
        }
    }
}
