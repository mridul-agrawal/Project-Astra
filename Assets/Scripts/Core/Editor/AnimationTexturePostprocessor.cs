using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Forces crisp pixel-art import settings on everything under the animation
    // art folder, so a designer can't accidentally ship a blurry or compressed
    // frame. Mirrors the crispness rules the Map Editor applies to map PNGs.
    // Pixels-per-unit is pinned to 32 so a frame's pixel size maps straight to
    // tiles (1 tile = 32px), the same world scale the camera renders at.
    public sealed class AnimationTexturePostprocessor : AssetPostprocessor
    {
        private const string AnimationArtFolder = "Assets/Art/Animation/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AnimationArtFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            if (importer.importSettingsMissing)   // first import only: one sprite per file; sheets opt into Multiple later
                importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 32;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
