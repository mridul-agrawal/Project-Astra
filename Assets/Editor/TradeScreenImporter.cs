using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // Applies UI-sprite import settings per docs/UI_WORKFLOW.md §4.4 to any PNG under
    // Assets/UI/TradeScreen/ so freshly-downloaded Figma exports don't need to be
    // configured by hand.
    public class TradeScreenImporter : AssetPostprocessor
    {
        const string RootPath = "Assets/UI/TradeScreen/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(RootPath)) return;
            if (!assetPath.EndsWith(".png")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.filterMode          = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 200f;
            importer.maxTextureSize      = 4096;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
        }
    }
}
