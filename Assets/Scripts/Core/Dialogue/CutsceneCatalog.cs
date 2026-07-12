using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Lookup from a CutsceneId to its DialogueScript asset. One shared asset; the GameFlow
    // resolves which script the (single, reused) Cutscene scene should play through it.
    // Mirrors MapCatalog / DialogueSpeakerRegistry.
    [CreateAssetMenu(menuName = "Project Astra/Dialogue/Cutscene Catalog")]
    public class CutsceneCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public CutsceneId id;
            public DialogueScript script;
        }

        [SerializeField] private List<Entry> cutscenes = new();

        private Dictionary<CutsceneId, DialogueScript> byId;

        public DialogueScript Get(CutsceneId id)
        {
            EnsureIndexBuilt();
            return byId.TryGetValue(id, out DialogueScript script) ? script : null;
        }

        private void EnsureIndexBuilt()
        {
            if (byId != null) return;
            byId = new Dictionary<CutsceneId, DialogueScript>();
            foreach (Entry e in cutscenes)
                if (e.script != null && e.id != CutsceneId.None)
                    byId[e.id] = e.script;
        }
    }
}
