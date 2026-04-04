using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    [CustomEditor(typeof(TilesetDefinition))]
    public class TilesetDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            var tileset = (TilesetDefinition)target;

            if (tileset.SourceTexture != null)
            {
                if (GUILayout.Button("Auto-Populate from Sprite Sheet"))
                    AutoPopulate(tileset);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Source Texture to enable auto-population.", MessageType.Info);
            }
        }

        private void AutoPopulate(TilesetDefinition tileset)
        {
            string texPath = AssetDatabase.GetAssetPath(tileset.SourceTexture);
            if (string.IsNullOrEmpty(texPath)) return;

            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                EditorUtility.DisplayDialog("Error",
                    "Source texture must be imported as Sprite with Multiple mode and sliced into tiles.",
                    "OK");
                return;
            }

            // Validate import settings
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texPath);
            var sprites = new List<Sprite>();
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            }

            if (sprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No sprites found in the texture.", "OK");
                return;
            }

            // Sort sprites by name to maintain consistent ordering
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            // Create or update Tile assets
            string tilesetPath = AssetDatabase.GetAssetPath(tileset);
            string tileFolder = System.IO.Path.GetDirectoryName(tilesetPath).Replace('\\', '/') + "/Tiles";
            if (!AssetDatabase.IsValidFolder(tileFolder))
            {
                string parent = System.IO.Path.GetDirectoryName(tileFolder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, "Tiles");
            }

            var entries = new TileEntry[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                string tilePath = $"{tileFolder}/{sprites[i].name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprites[i];
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);

                entries[i] = new TileEntry
                {
                    tileAsset = tile,
                    terrainType = TerrainType.Plain
                };
            }

            tileset.SetTiles(entries);
            EditorUtility.SetDirty(tileset);
            AssetDatabase.SaveAssets();

            Debug.Log($"TilesetDefinition: Auto-populated {sprites.Count} tiles from '{tileset.SourceTexture.name}'.");
        }
    }
}
