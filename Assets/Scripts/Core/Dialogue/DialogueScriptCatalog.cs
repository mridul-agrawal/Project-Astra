using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Lookup from a script's own ScriptId to its asset, so a character placement, an
    // interactable or an event can name a conversation without holding a direct reference.
    //
    // Adding a script means dropping it in the list — the key comes from the asset itself
    // rather than being typed twice.
    [CreateAssetMenu(fileName = "DialogueScriptCatalog", menuName = "Project Astra/Dialogue/Script Catalog")]
    public class DialogueScriptCatalog : ScriptableObject
    {
        [SerializeField] private List<DialogueScript> scripts = new();

        private Dictionary<string, DialogueScript> byId;

        public DialogueScript Get(string scriptId)
        {
            if (string.IsNullOrEmpty(scriptId)) return null;

            EnsureIndexBuilt();
            return byId.TryGetValue(scriptId, out DialogueScript script) ? script : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;

            byId = new Dictionary<string, DialogueScript>();
            foreach (DialogueScript script in scripts)
            {
                if (script == null || string.IsNullOrEmpty(script.ScriptId)) continue;

                if (byId.ContainsKey(script.ScriptId))
                {
                    Debug.LogError($"[DialogueScriptCatalog] Two scripts both claim id '{script.ScriptId}'. " +
                                   "Ids must be unique — the second one is unreachable.");
                    continue;
                }
                byId[script.ScriptId] = script;
            }
        }

        // Play-mode edits to the list should take effect without a domain reload.
        private void OnValidate() => byId = null;
    }
}
