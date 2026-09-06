using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.UI.BattleMap.HUD;
using ProjectAstra.Core.UI.Hub;

namespace ProjectAstra.Core.Editor
{
    // Copies the battle map's objective panel into the hub scene, exactly as it stands.
    //
    // Both places want the same interface, and there are only two of them — so the panel is copied
    // once and lives in each scene rather than becoming a prefab neither scene owns. Run again to
    // take a fresh copy after restyling the battle one.
    public static class HubObjectivePanelTransplant
    {
        private const string HubScene = "Assets/Scenes/HubExploration.unity";
        private const string BattleScene = "Assets/Scenes/BattleMap.unity";
        private const string PanelName = "ObjectivePanel";

        [MenuItem("Project Astra/Gurukul/Copy Objective Panel From Battle Map")]
        private static void Copy()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("[ObjectivePanel] Save the open scene first — this opens two others.");
                return;
            }

            Scene hub = EditorSceneManager.OpenScene(HubScene, OpenSceneMode.Single);
            Scene battle = EditorSceneManager.OpenScene(BattleScene, OpenSceneMode.Additive);

            ObjectiveView source = Find<ObjectiveView>(battle);
            HubHUDController hud = Find<HubHUDController>(hub);

            if (source == null || hud == null)
            {
                Debug.LogError($"[ObjectivePanel] Missing {(source == null ? "the battle panel" : "the hub HUD")}.");
                EditorSceneManager.CloseScene(battle, true);
                return;
            }

            DropWhateverIsThere(hud.transform);
            ObjectiveView copied = PlaceCopyUnder(source, hud.transform, hub);
            Wire(hud, copied);

            EditorSceneManager.CloseScene(battle, true);
            EditorSceneManager.MarkSceneDirty(hub);
            EditorSceneManager.SaveScene(hub);

            Selection.activeGameObject = copied.gameObject;
            Debug.Log("[ObjectivePanel] Copied into the hub and wired to HubHUDController.");
        }

        // Whatever the panel used to be — the generated one-line label, or an older copy of this.
        private static void DropWhateverIsThere(Transform canvas)
        {
            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);
                if (child.name == PanelName || child.GetComponent<ObjectiveView>() != null)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        // Parented without keeping world position, so every anchor, size and offset the battle panel
        // was authored with arrives unchanged. Both canvases are 1920x1080 overlay at match 0.5.
        private static ObjectiveView PlaceCopyUnder(ObjectiveView source, Transform canvas, Scene hub)
        {
            var clone = Object.Instantiate(source.gameObject);
            clone.name = source.gameObject.name;

            SceneManager.MoveGameObjectToScene(clone, hub);
            clone.transform.SetParent(canvas, false);
            clone.transform.SetSiblingIndex(0);

            return clone.GetComponent<ObjectiveView>();
        }

        private static void Wire(HubHUDController hud, ObjectiveView panel)
        {
            var editable = new SerializedObject(hud);
            editable.FindProperty("objectiveView").objectReferenceValue = panel;
            editable.ApplyModifiedProperties();
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
