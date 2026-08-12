using System.Collections.Generic;
using System.Linq;
using ProjectAstra.Core.UI.UnitInfo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // The BattleMap instance of the Unit Info screen carries property overrides left over from the
    // old layout - mostly TMP font colours, plus a few renames. They win over the prefab, so after
    // a restyle they would show through as patches of the previous palette.
    //
    // Only those are stripped. The root RectTransform and the active flag stay: the scene owns
    // where the screen sits and whether it starts awake, and reverting those would fight the
    // disabled-by-default rule for battle-map UI.
    //
    // Run via 'Project Astra/Clean Unit Info Scene Overrides'.
    // ==========================================================================================
    public static class CleanUnitInfoSceneOverrides
    {
        const string ScenePath = "Assets/Scenes/BattleMap.unity";

        // Overrides the restyle now owns. Everything else is the scene's business.
        static readonly HashSet<string> StaleProperties = new()
        {
            "m_fontColor32.rgba",
            "m_TextStyleHashCode",
            "m_Name",
        };

        [MenuItem("Project Astra/Clean Unit Info Scene Overrides")]
        public static void Clean()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[CleanUnitInfoSceneOverrides] Could not open " + ScenePath);
                return;
            }

            GameObject instance = FindScreenInstance();
            if (instance == null)
            {
                Debug.LogError("[CleanUnitInfoSceneOverrides] No UnitInfoScreen prefab instance in the scene.");
                return;
            }

            StripStaleOverrides(instance);
            ReportSceneWiring();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static GameObject FindScreenInstance()
        {
            var controller = Object.FindAnyObjectByType<UnitInfoUIController>(FindObjectsInactive.Include);
            if (controller == null) return null;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(controller.gameObject);
            return root != null ? root : controller.gameObject;
        }

        static void StripStaleOverrides(GameObject instance)
        {
            PropertyModification[] all = PrefabUtility.GetPropertyModifications(instance);
            if (all == null)
            {
                Debug.Log("[CleanUnitInfoSceneOverrides] Instance had no overrides.");
                return;
            }

            PropertyModification[] kept = all.Where(m => !StaleProperties.Contains(m.propertyPath)).ToArray();
            int dropped = all.Length - kept.Length;

            PrefabUtility.SetPropertyModifications(instance, kept);
            Debug.Log($"[CleanUnitInfoSceneOverrides] Dropped {dropped} stale override(s), kept {kept.Length} " +
                      "(root transform and active flag).");
        }

        // The whole point of restyling in place rather than rebuilding: these scene-side
        // references have to still resolve afterwards.
        static void ReportSceneWiring()
        {
            var cursor = Object.FindAnyObjectByType<ProjectAstra.Core.Cursor.GridCursor>(FindObjectsInactive.Include);
            if (cursor == null)
            {
                Debug.LogWarning("[CleanUnitInfoSceneOverrides] No GridCursor found to check.");
                return;
            }

            var so = new SerializedObject(cursor);
            var reference = so.FindProperty("unitInfoUIController");
            bool wired = reference != null && reference.objectReferenceValue != null;

            if (wired)
                Debug.Log("[CleanUnitInfoSceneOverrides] GridCursor.unitInfoUIController still resolves.");
            else
                Debug.LogError("[CleanUnitInfoSceneOverrides] GridCursor.unitInfoUIController is EMPTY.");
        }
    }
}
