using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Support
{
    // Returns the support bonds a unit currently has. Implemented by whichever subsystem owns the bond data.
    public interface ISupportProvider
    {
        IReadOnlyList<SupportBond> GetBonds(UnitInstance unit);
    }

    // A single bond between two units. Tracks level, oath/promise state, and whether they can converse this chapter.
    public struct SupportBond
    {
        public UnitDefinition Partner;
        public int BondLevel;               // Stored as int 0..3; Stage exposes the BondStage view.
        public bool ConversationAvailable;
        public bool IsDeceased;
        public string PromiseText;          // Bandhan promise; null/empty when not sworn.
        public bool ShapathWitnessed;       // True once the oath scene has played.

        public BondStage Stage => (BondStage)Mathf.Clamp(BondLevel, (int)BondStage.Encounter, (int)BondStage.Bandhan);
    }
}
