using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectAstra.Core.Editor
{
    // Installs the CRT filter on the project's 2D renderer using URP's own
    // FullScreenPassRendererFeature, pointed at Assets/Rendering/CRT/CRT.mat.
    //
    // A hand-written renderer feature was tried first and abandoned: getting a render-graph pass
    // to actually composite is fiddly in ways that are invisible from the code — the pass records
    // and reports healthy while drawing nothing, or takes over the camera colour and blanks the
    // screen. Unity's feature already solves all of that, and the shader is identical either way,
    // so the only thing we gave up was our own plumbing bugs.
    //
    // Tunable values live on the material. Nothing here needs to know what they are.
    public static class CrtFeatureInstaller
    {
        private const string RendererPath = "Assets/Settings/Renderer2D.asset";
        private const string MaterialPath = "Assets/Rendering/CRT/CRT.mat";
        private const string FeatureName = "CRT";

        [MenuItem("Project Astra/Rendering/Install CRT Renderer Feature")]
        public static void Install()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (rendererData == null)
            {
                Debug.LogError($"[CrtFeatureInstaller] No renderer data at {RendererPath}.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"[CrtFeatureInstaller] No material at {MaterialPath}. "
                    + "Create one from Assets/Rendering/CRT/CRT.shader first.");
                return;
            }

            if (FindExisting(rendererData) != null)
            {
                Debug.Log("[CrtFeatureInstaller] CRT feature is already installed.");
                return;
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.SaveAssets();

            Configure(feature, material);
            AppendToRenderer(rendererData, feature);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CrtFeatureInstaller] CRT feature installed on {RendererPath}.");
        }

        [MenuItem("Project Astra/Rendering/Remove CRT Renderer Feature")]
        public static void Remove()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (rendererData == null) return;

            var feature = FindExisting(rendererData);
            if (feature == null)
            {
                Debug.Log("[CrtFeatureInstaller] No CRT feature to remove.");
                return;
            }

            RemoveFromRenderer(rendererData, feature);
            Object.DestroyImmediate(feature, allowDestroyingAssets: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrtFeatureInstaller] CRT feature removed.");
        }

        private static ScriptableRendererFeature FindExisting(ScriptableRendererData rendererData)
        {
            foreach (var feature in rendererData.rendererFeatures)
                if (feature != null && feature.name == FeatureName) return feature;
            return null;
        }

        // "Fetch Color Buffer" is what copies the screen into _BlitTexture for the shader to
        // read. Without it the shader samples nothing and the map disappears.
        private static void Configure(FullScreenPassRendererFeature feature, Material material)
        {
            var so = new SerializedObject(feature);
            so.FindProperty("passMaterial").objectReferenceValue = material;
            so.FindProperty("injectionPoint").enumValueIndex = 2;      // AfterRenderingPostProcessing
            so.FindProperty("fetchColorBuffer").boolValue = true;
            so.FindProperty("requirements").intValue = 0;              // no depth/normal/motion
            so.FindProperty("passIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The feature list and the id map have to stay in step — URP uses the map to re-link
        // sub-assets after a domain reload, and a mismatch silently drops the feature.
        private static void AppendToRenderer(ScriptableRendererData rendererData, ScriptableRendererFeature feature)
        {
            var so = new SerializedObject(rendererData);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");

            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }

        private static void RemoveFromRenderer(ScriptableRendererData rendererData, ScriptableRendererFeature feature)
        {
            var so = new SerializedObject(rendererData);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");

            for (int i = features.arraySize - 1; i >= 0; i--)
            {
                if (features.GetArrayElementAtIndex(i).objectReferenceValue != feature) continue;

                features.DeleteArrayElementAtIndex(i);
                if (i < map.arraySize) map.DeleteArrayElementAtIndex(i);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }
    }
}
