using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Editor
{
    // Slices a sprite-sheet PNG into named Multiple sub-sprites. The grid and the
    // names come from the pure SpriteSheetGrid; this just writes them to the
    // importer via Unity's sprite data provider. The animation builders read the
    // sub-sprites straight out, so a sliced sheet feeds them with no extra steps.
    public static class SpriteSheetSlicer
    {
        public static int Slice(string assetPath, SpriteSlice[] slices,
            SpriteAlignment alignment = SpriteAlignment.Center, bool tileable = false)
        {
            if (slices == null || slices.Length == 0) return 0;
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) return 0;

            importer.spriteImportMode = SpriteImportMode.Multiple;
            if (tileable) SetFullRect(importer);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] rects = BuildRects(slices, alignment);
            provider.SetSpriteRects(rects);
            RegisterNameFileIds(provider, rects);

            provider.Apply();
            importer.SaveAndReimport();
            return rects.Length;
        }

        private static SpriteRect[] BuildRects(SpriteSlice[] slices, SpriteAlignment alignment)
        {
            Vector2 pivot = PivotFor(alignment);
            var rects = new SpriteRect[slices.Length];
            for (int i = 0; i < slices.Length; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = slices[i].Name,
                    spriteID = GUID.Generate(),
                    rect = slices[i].Rect,
                    alignment = alignment,
                    pivot = pivot
                };
            }
            return rects;
        }

        // Keeps the sliced sub-sprites' file ids stable across reimports.
        private static void RegisterNameFileIds(ISpriteEditorDataProvider provider, SpriteRect[] rects)
        {
            var idProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (idProvider == null) return;

            var pairs = new List<SpriteNameFileIdPair>();
            foreach (SpriteRect r in rects)
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            idProvider.SetNameFileIdPairs(pairs);
        }

        // Tiled-drawMode river patches need FullRect meshes or Unity stretches them.
        private static void SetFullRect(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        private static Vector2 PivotFor(SpriteAlignment alignment) => alignment switch
        {
            SpriteAlignment.BottomCenter => new Vector2(0.5f, 0f),
            SpriteAlignment.BottomLeft => new Vector2(0f, 0f),
            _ => new Vector2(0.5f, 0.5f)
        };
    }
}
