using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace ProjectAstra.Core
{
    public class UIDebugDiagnostics : MonoBehaviour
    {
        private void Update()
        {
            // Use New Input System API
            var mouse = Mouse.current;
            if (mouse == null)
            {
                Debug.LogError("[Diagnostics] No mouse device found!");
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            // 1. Check EventSystem
            var es = EventSystem.current;
            if (es == null)
            {
                Debug.LogError("[Diagnostics] No EventSystem.current found!");
                return;
            }

            // 2. Check input module
            var inputModule = es.currentInputModule;
            if (inputModule == null)
            {
                Debug.LogError($"[Diagnostics] EventSystem exists but currentInputModule is NULL. " +
                    $"Components: {ListComponents(es.gameObject)}");
                return;
            }

            Debug.Log($"[Diagnostics] InputModule: {inputModule.GetType().Name}");

            // 3. Check InputSystemUIInputModule configuration
            if (inputModule is InputSystemUIInputModule isim)
            {
                Debug.Log($"[Diagnostics] actionsAsset: {(isim.actionsAsset != null ? isim.actionsAsset.name : "NULL")}");
                Debug.Log($"[Diagnostics] point action: {isim.point?.action?.name ?? "NULL"}");
                Debug.Log($"[Diagnostics] leftClick action: {isim.leftClick?.action?.name ?? "NULL"}");
            }

            // 4. Raycast from pointer
            Vector2 pointerPos = mouse.position.ReadValue();
            var pointerData = new PointerEventData(es) { position = pointerPos };
            var results = new List<RaycastResult>();
            es.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                Debug.LogWarning($"[Diagnostics] Click at {pointerPos} — raycast hit NOTHING");
            }
            else
            {
                Debug.Log($"[Diagnostics] Click at {pointerPos} — hit {results.Count} objects:");
                foreach (var r in results)
                    Debug.Log($"  -> {r.gameObject.name} (depth={r.depth}, sortOrder={r.sortingOrder})");
            }
        }

        private static string ListComponents(GameObject go)
        {
            var components = go.GetComponents<Component>();
            var names = new string[components.Length];
            for (int i = 0; i < components.Length; i++)
                names[i] = components[i]?.GetType().Name ?? "null";
            return string.Join(", ", names);
        }
    }
}
