using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Gives every image dropped into the hub's art folder the settings this game's pixel art needs,
    // so a designer never opens an import inspector.
    //
    // Keep this class small and still: changing it makes Unity reimport every texture in the
    // project. The palette side of the same job lives in HubPaletteAutoFill, which does not.
    public class HubArtImporter : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/Gurukul/";

        // 32 px to the world unit, matching the camera. Wrong here and everything is the wrong size.
        private const int PixelsPerUnit = 32;

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;

            var importer = (TextureImporter)assetImporter;

            // Only on the very first import. After that the settings are the designer's, and a
            // re-import must never undo a change they made on purpose.
            if (!importer.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            PivotAtTheBase(importer);
        }

        // Standing on the ground is the common case, and it is what depth sorting measures from.
        private static void PivotAtTheBase(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
        }
    }
}
