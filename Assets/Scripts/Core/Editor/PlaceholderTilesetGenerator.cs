using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class PlaceholderTilesetGenerator
    {
        private const int TileSize = 16;
        private const int GridSize = 4;
        private const string OutputFolder = "Assets/Art/Tilesets/Placeholder";
        private const string TileAssetFolder = "Assets/Art/Tilesets/Placeholder/Tiles";
        private const string SOFolder = "Assets/ScriptableObjects/Map/Tilesets";

        private static readonly Color32[] TileColors = new Color32[]
        {
            new Color32(34, 139, 34, 255),    // 0  Plain (green)
            new Color32(0, 100, 0, 255),      // 1  Forest (dark green)
            new Color32(139, 137, 137, 255),  // 2  Mountain (grey)
            new Color32(105, 105, 105, 255),  // 3  Peak (dark grey)
            new Color32(30, 144, 255, 255),   // 4  Water (blue)
            new Color32(0, 0, 139, 255),      // 5  Sea (dark blue)
            new Color32(100, 149, 237, 255),  // 6  River (cornflower)
            new Color32(210, 180, 140, 255),  // 7  Road (tan)
            new Color32(255, 165, 0, 255),    // 8  Village (orange)
            new Color32(178, 34, 34, 255),    // 9  Fort (firebrick)
            new Color32(139, 69, 19, 255),    // 10 Gate (brown)
            new Color32(255, 215, 0, 255),    // 11 Chest (gold)
            new Color32(160, 82, 45, 255),    // 12 Door (sienna)
            new Color32(80, 80, 80, 255),     // 13 Wall (dark)
            new Color32(120, 120, 120, 255),  // 14 DestructibleWall
            new Color32(169, 169, 169, 255),  // 15 Rubble (light grey)
        };

        private static readonly TerrainType[] TileTerrains = new TerrainType[]
        {
            TerrainType.Plain, TerrainType.Forest, TerrainType.Mountain, TerrainType.Peak,
            TerrainType.Water, TerrainType.Sea, TerrainType.River, TerrainType.Road,
            TerrainType.Village, TerrainType.Fort, TerrainType.Gate, TerrainType.Chest,
            TerrainType.Door, TerrainType.Wall, TerrainType.DestructibleWall, TerrainType.Rubble
        };

        [MenuItem("Project Astra/Map/Generate Placeholder Tileset")]
        public static void Generate()
        {
            EnsureDirectories();
            string texturePath = GenerateSpriteSheet();
            Sprite[] sprites = SliceSpriteSheet(texturePath);
            TileBase[] tiles = CreateTileAssets(sprites);
            CreateErrorTile();
            CreateTilesetDefinition(tiles);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Placeholder tileset generated successfully.");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { OutputFolder, TileAssetFolder, SOFolder };
            foreach (string dir in dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                    string folderName = Path.GetFileName(dir);
                    // Recursively ensure parent exists
                    if (!AssetDatabase.IsValidFolder(parent))
                    {
                        string grandParent = Path.GetDirectoryName(parent).Replace('\\', '/');
                        string parentName = Path.GetFileName(parent);
                        if (!AssetDatabase.IsValidFolder(grandParent))
                        {
                            string greatGrandParent = Path.GetDirectoryName(grandParent).Replace('\\', '/');
                            string grandParentName = Path.GetFileName(grandParent);
                            AssetDatabase.CreateFolder(greatGrandParent, grandParentName);
                        }
                        AssetDatabase.CreateFolder(grandParent, parentName);
                    }
                    AssetDatabase.CreateFolder(parent, folderName);
                }
            }
        }

        private static string GenerateSpriteSheet()
        {
            int texSize = TileSize * GridSize;
            var texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color32[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            for (int tileIndex = 0; tileIndex < GridSize * GridSize; tileIndex++)
            {
                int col = tileIndex % GridSize;
                int row = tileIndex / GridSize;
                Color32 color = tileIndex < TileColors.Length
                    ? TileColors[tileIndex]
                    : new Color32(0, 0, 0, 255);

                // Unity textures have origin at bottom-left; row 0 in our grid = top row visually
                int baseY = (GridSize - 1 - row) * TileSize;
                int baseX = col * TileSize;

                for (int py = 0; py < TileSize; py++)
                {
                    for (int px = 0; px < TileSize; px++)
                    {
                        pixels[(baseY + py) * texSize + (baseX + px)] = color;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string path = $"{OutputFolder}/PlaceholderTileset.png";
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();

            // Configure texture import settings
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TileSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            int texWidth = TileSize * GridSize;
            var spriteSheet = new SpriteMetaData[GridSize * GridSize];
            for (int i = 0; i < spriteSheet.Length; i++)
            {
                int col = i % GridSize;
                int row = i / GridSize;
                spriteSheet[i] = new SpriteMetaData
                {
                    name = $"tile_{i:D2}",
                    rect = new Rect(col * TileSize, (GridSize - 1 - row) * TileSize, TileSize, TileSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }
            importer.spritesheet = spriteSheet;
            importer.SaveAndReimport();

            return path;
        }

        private static Sprite[] SliceSpriteSheet(string texturePath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            var sprites = new Sprite[GridSize * GridSize];

            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name.StartsWith("tile_"))
                {
                    string indexStr = sprite.name.Substring(5);
                    if (int.TryParse(indexStr, out int index) && index < sprites.Length)
                        sprites[index] = sprite;
                }
            }
            return sprites;
        }

        private static TileBase[] CreateTileAssets(Sprite[] sprites)
        {
            var tiles = new TileBase[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                string path = $"{TileAssetFolder}/Tile_{i:D2}.asset";

                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.sprite = sprites[i];
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles[i] = tile;
            }
            return tiles;
        }

        private static void CreateErrorTile()
        {
            string errorTexPath = $"{OutputFolder}/ErrorTile.png";
            var errorTex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            errorTex.filterMode = FilterMode.Point;
            var magenta = new Color32(255, 0, 255, 255);
            var pixels = new Color32[TileSize * TileSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = magenta;
            errorTex.SetPixels32(pixels);
            errorTex.Apply();

            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", errorTexPath), errorTex.EncodeToPNG());
            Object.DestroyImmediate(errorTex);
            AssetDatabase.Refresh();

            var importer = (TextureImporter)AssetImporter.GetAtPath(errorTexPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TileSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            Sprite errorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(errorTexPath);
            string errorTilePath = $"{TileAssetFolder}/ErrorTile.asset";
            var errorTile = AssetDatabase.LoadAssetAtPath<Tile>(errorTilePath);
            if (errorTile == null)
            {
                errorTile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(errorTile, errorTilePath);
            }
            errorTile.sprite = errorSprite;
            errorTile.colliderType = Tile.ColliderType.None;
            errorTile.color = Color.white;
            EditorUtility.SetDirty(errorTile);
        }

        private static void CreateTilesetDefinition(TileBase[] tiles)
        {
            string path = $"{SOFolder}/PlaceholderTileset.asset";
            var tileset = AssetDatabase.LoadAssetAtPath<TilesetDefinition>(path);
            if (tileset == null)
            {
                tileset = ScriptableObject.CreateInstance<TilesetDefinition>();
                AssetDatabase.CreateAsset(tileset, path);
            }

            var entries = new TileEntry[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                entries[i] = new TileEntry
                {
                    tileAsset = tiles[i],
                    terrainType = i < TileTerrains.Length ? TileTerrains[i] : TerrainType.Void
                };
            }

            tileset.SetTiles(entries);
            tileset.SetTilesetName("Placeholder");
            EditorUtility.SetDirty(tileset);
        }
    }
}
