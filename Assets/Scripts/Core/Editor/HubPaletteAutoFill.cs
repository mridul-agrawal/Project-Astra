using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Art that has just appeared is placeable straight away: the folder the artist filed it in
    // becomes its palette category, and nobody has to register anything.
    public sealed class HubPaletteAutoFill : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            HubPalette palette = null;

            foreach (string path in imported)
            {
                if (!IsHubArt(path)) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                palette ??= HubPalette.Load();
                palette.Adopt(sprite, CategoryOf(path), KindOf(path));
            }

            palette?.Save();
        }

        private static bool IsHubArt(string path) =>
            path.StartsWith(HubArtImporter.ArtFolder) &&
            (path.EndsWith(".png") || path.EndsWith(".aseprite") || path.EndsWith(".psd"));

        // The folder under the hub art root, so "Nature/tree.png" files itself under Nature.
        private static string CategoryOf(string path)
        {
            string[] parts = path.Substring(HubArtImporter.ArtFolder.Length).Split('/');
            return parts.Length > 1 ? parts[0] : "Props";
        }

        private static HubPalette.Kind KindOf(string path) =>
            CategoryOf(path).ToLowerInvariant() is "ground" or "grounds" or "floors"
                ? HubPalette.Kind.Ground
                : HubPalette.Kind.Object;
    }
}
