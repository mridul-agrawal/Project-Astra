using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjectAstra.EditorTools
{
    // One-off baker that turns the three title-screen pixel TTFs into TMP SDF font assets.
    public static class TitleScreenFontBaker
    {
        private const string FontDir = "Assets/UI/TitleScreen/Fonts";

        [MenuItem("Tools/Astra/Bake Title Screen Fonts")]
        public static void BakeAll()
        {
            var log = new StringBuilder();
            log.AppendLine(Bake("Silkscreen-Regular.ttf", "Silkscreen SDF.asset"));
            log.AppendLine(Bake("PressStart2P-Regular.ttf", "Press Start 2P SDF.asset"));
            log.AppendLine(Bake("PixelifySans-VariableFont_wght.ttf", "Pixelify Sans SDF.asset"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TitleScreenFontBaker]\n" + log);
        }

        private static string Bake(string ttfName, string assetName)
        {
            string ttfPath = $"{FontDir}/{ttfName}";
            string outPath = $"{FontDir}/{assetName}";
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null) return $"MISSING source font: {ttfPath}";

            const int pointSize = 90;
            const int padding = 9;
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font, pointSize, padding, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            if (fontAsset == null) return $"BAKE FAILED: {ttfPath}";

            fontAsset.name = Path.GetFileNameWithoutExtension(assetName);
            fontAsset.TryAddCharacters(PrintableAscii());

            AssetDatabase.CreateAsset(fontAsset, outPath);
            foreach (var atlas in fontAsset.atlasTextures)
            {
                atlas.name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            EditorUtility.SetDirty(fontAsset);
            return $"OK: {outPath} (glyphs={fontAsset.characterTable.Count}, atlases={fontAsset.atlasTextures.Length})";
        }

        private static string PrintableAscii()
        {
            var sb = new StringBuilder();
            for (int c = 32; c <= 126; c++) sb.Append((char)c);
            return sb.ToString();
        }

        // Gives every title font a thin dark outline and a soft drop shadow so the
        // prompt stays legible and polished over the busy art, whichever font is active.
        [MenuItem("Tools/Astra/Polish Title Screen Font Materials")]
        public static void PolishMaterials()
        {
            var log = new StringBuilder();
            foreach (var assetName in new[] { "Silkscreen SDF.asset", "Press Start 2P SDF.asset", "Pixelify Sans SDF.asset" })
                log.AppendLine(Polish($"{FontDir}/{assetName}"));
            AssetDatabase.SaveAssets();
            Debug.Log("[TitleScreenFontBaker.Polish]\n" + log);
        }

        private static string Polish(string assetPath)
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null || fontAsset.material == null) return $"MISSING font/material: {assetPath}";

            var mat = fontAsset.material;
            var outline = new Color(0.10f, 0.05f, 0.02f, 1f);
            var shadow = new Color(0f, 0f, 0f, 0.55f);

            mat.SetColor(ShaderUtilities.ID_OutlineColor, outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);

            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, shadow);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.75f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.75f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);

            EditorUtility.SetDirty(mat);
            return $"Polished: {assetPath}";
        }
    }
}
