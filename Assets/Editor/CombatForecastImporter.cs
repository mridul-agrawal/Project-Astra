using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // Applies UI-sprite import settings per docs/UI_WORKFLOW.md §4.4 to every PNG under
    // Assets/UI/CombatForecast/ so Figma exports drop in ready-to-use.
    public class CombatForecastImporter : AssetPostprocessor
    {
        const string RootPath = "Assets/UI/CombatForecast/";

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
