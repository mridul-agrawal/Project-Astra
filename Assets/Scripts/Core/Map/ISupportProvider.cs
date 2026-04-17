using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    public interface ISupportProvider
    {
        IReadOnlyList<SupportBond> GetBonds(UnitInstance unit);
    }

    public struct SupportBond
    {
        public UnitDefinition Partner;
        public int BondLevel;               // 0..3 — round-trips with BondStage cast
        public bool ConversationAvailable;
        public bool IsDeceased;
        public string PromiseText;          // SP-01 bandhan promise; null/empty when not sworn
        public bool ShapathWitnessed;       // SP-04 oath scene completed

        public BondStage Stage => (BondStage)Mathf.Clamp(BondLevel, 0, 3);
    }
}
