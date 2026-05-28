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

        [SerializeField] private List<Entry> _cutscenes = new();

        private Dictionary<CutsceneId, DialogueScript> _byId;

        public DialogueScript Get(CutsceneId id)
        {
            EnsureIndexBuilt();
            return _byId.TryGetValue(id, out DialogueScript script) ? script : null;
        }

        private void EnsureIndexBuilt()
        {
            if (_byId != null) return;
            _byId = new Dictionary<CutsceneId, DialogueScript>();
            foreach (Entry e in _cutscenes)
                if (e.script != null && e.id != CutsceneId.None)
                    _byId[e.id] = e.script;
        }
    }
}
