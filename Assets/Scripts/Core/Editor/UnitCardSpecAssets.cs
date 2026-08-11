using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Builds the two assets the unit card spec needs but that cannot be expressed as component
    // values: the acted-unit grayscale material (§9) and the placeholder bust silhouette (§5).
    //
    // Run via 'Project Astra/Build Unit Card Spec Assets'. Idempotent.
    // ==========================================================================================
    public static class UnitCardSpecAssets
    {
        const string ShaderPath   = "Assets/UI/Shared/Shaders/UIGrayscale.shader";
        const string MaterialPath = "Assets/UI/Shared/Materials/UnitCardActedPortrait.mat";
        const string BustPath     = "Assets/UI/Shared/Sprites/unit_bust_placeholder.png";

        // Spec §9: grayscale(100%) brightness(72%).
        const float ActedBrightness = 0.72f;

        // Spec §5 geometry, in the source 24x24 viewBox.
        const int   ViewBox     = 24;
        const int   BustPixels  = 256;
        const int   Supersample = 4;
        static readonly Vector2 HeadCentre = new Vector2(12f, 9.5f);
        const float HeadRadius = 5f;
        static readonly Color32 BustFill = new Color32(0x7d, 0x85, 0x93, 0xff);

        [MenuItem("Project Astra/Build Unit Card Spec Assets")]
        public static void Build()
        {
            CreateActedMaterial();
            CreateBustSprite();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UnitCardSpecAssets] Acted material and bust silhouette ready.");
        }

        static void CreateActedMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"[UnitCardSpecAssets] Shader missing at {ShaderPath}");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetFloat("_Brightness", ActedBrightness);
            material.SetFloat("_Saturation", 0f);
            EditorUtility.SetDirty(material);
        }

        // ---- bust silhouette -------------------------------------------------------------

        static void CreateBustSprite()
        {
            var shoulders = BuildShoulderOutline();
            var texture = new Texture2D(BustPixels, BustPixels, TextureFormat.RGBA32, false);
            var pixels = new Color32[BustPixels * BustPixels];

            for (int y = 0; y < BustPixels; y++)
                for (int x = 0; x < BustPixels; x++)
                    pixels[y * BustPixels + x] = SamplePixel(x, y, shoulders);

            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(BustPath));
            File.WriteAllBytes(BustPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(BustPath, ImportAssetOptions.ForceUpdate);
            ApplySpriteImportSettings();
        }

        // Supersampled so the silhouette edge stays clean at HD sizes.
        static Color32 SamplePixel(int x, int y, IReadOnlyList<Vector2> shoulders)
        {
            int hits = 0;
            for (int sy = 0; sy < Supersample; sy++)
            {
                for (int sx = 0; sx < Supersample; sx++)
                {
                    Vector2 point = ToViewBox(x + (sx + 0.5f) / Supersample,
                                              y + (sy + 0.5f) / Supersample);
                    if (IsInsideBust(point, shoulders)) hits++;
                }
            }

            if (hits == 0) return new Color32(0, 0, 0, 0);
            byte alpha = (byte)Mathf.RoundToInt(255f * hits / (Supersample * Supersample));
            return new Color32(BustFill.r, BustFill.g, BustFill.b, alpha);
        }

        // Texture space is y-up, the spec's viewBox is y-down, so the row is flipped.
        static Vector2 ToViewBox(float x, float y)
        {
            float scale = (float)ViewBox / BustPixels;
            return new Vector2(x * scale, ViewBox - y * scale);
        }

        static bool IsInsideBust(Vector2 point, IReadOnlyList<Vector2> shoulders) =>
            Vector2.Distance(point, HeadCentre) <= HeadRadius || IsInsidePolygon(point, shoulders);

        static List<Vector2> BuildShoulderOutline()
        {
            var outline = new List<Vector2> { new Vector2(2f, 25f) };
            AppendCubic(outline, new Vector2(2f, 25f),   new Vector2(2f, 16.5f),
                                 new Vector2(7.5f, 14.5f), new Vector2(12f, 14.5f));
            AppendCubic(outline, new Vector2(12f, 14.5f), new Vector2(16.5f, 14.5f),
                                 new Vector2(22f, 16.5f),  new Vector2(22f, 25f));
            return outline;
        }

        static void AppendCubic(List<Vector2> into, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            const int Segments = 32;
            for (int i = 1; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                float u = 1f - t;
                into.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
            }
        }

        static bool IsInsidePolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                bool straddles = (polygon[i].y > point.y) != (polygon[j].y > point.y);
                if (!straddles) continue;

                float crossingX = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                                / (polygon[j].y - polygon[i].y) + polygon[i].x;
                if (point.x < crossingX) inside = !inside;
            }
            return inside;
        }

        static void ApplySpriteImportSettings()
        {
            var importer = AssetImporter.GetAtPath(BustPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
