using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Bakes the dialogue body font (Mulish) into a TMP SDF asset and swaps it into the
    // DialogueView prefab, replacing the old pixel font (Pixelify Sans) that read poorly for
    // long passages. Mirrors TradeScreenFonts: pre-populates the ASCII atlas and saves the atlas
    // texture + material as sub-assets so glyphs don't render blank on first play (UI_WORKFLOW 4.3).
    // ==========================================================================================
    public static class DialogueFonts
    {
        const string FontDir     = "Assets/UI/Dialogue/Fonts/";
        const string TtfPath     = FontDir + "Mulish-VF.ttf";
        const string SdfPath     = FontDir + "Mulish SDF.asset";
        const string PrefabPath  = "Assets/Resources/UI/DialogueView.prefab";
        const string OldFontName = "Pixelify Sans SDF";

        // Pixelify's cap-height ratio (CapLine 57 / PointSize 90). We rescale font sizes so Mulish
        // fills the box to the same cap height the prefab was hand-tuned to.
        const float OldCapRatio = 57f / 90f;

        const string AsciiRange =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~";
        // Curly quotes, ellipsis, en/em dash and mid-dot appear in the dialogue scripts.
        const string ExtraChars = "‘’“”…–—·";

        [MenuItem("Project Astra/Generate Dialogue Font (Mulish)")]
        public static void Generate()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath) != null)
            {
                Debug.Log("[DialogueFonts] Mulish SDF already exists — skipping bake.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError($"[DialogueFonts] TTF missing at {TtfPath}");
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

            AssetDatabase.CreateAsset(fa, SdfPath);
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueFonts] Baked {SdfPath} (capLine {fa.faceInfo.capLine}, pointSize {fa.faceInfo.pointSize}).");
        }

        [MenuItem("Project Astra/Apply Dialogue Font to Prefab")]
        public static void Apply()
        {
            var mulish = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
            if (mulish == null)
            {
                Debug.LogError("[DialogueFonts] Generate the Mulish SDF first.");
                return;
            }

            float newCapRatio = mulish.faceInfo.capLine / mulish.faceInfo.pointSize;
            float scale = OldCapRatio / newCapRatio;

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            int swapped = 0;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.font == null || t.font.name != OldFontName) continue;
                t.font = mulish;
                t.fontSize = Mathf.Round(t.fontSize * scale);
                t.lineSpacing = Mathf.Round(t.lineSpacing * scale);
                EditorUtility.SetDirty(t);
                swapped++;
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[DialogueFonts] Swapped {swapped} text objects to Mulish (scale {scale:0.###}).");
        }
    }
}
