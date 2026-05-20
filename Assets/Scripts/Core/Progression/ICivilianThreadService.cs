using System;
using System.Collections.Generic;

namespace ProjectAstra.Core.Progression
{
    // Survival outcome for a single named civilian. Stored on save data as
    // the integer value; don't reorder.
    public enum CivilianStatus { Safe, Displaced, Lost }

    [Serializable]
    public struct CivilianStatusEntry
    {
        public string civilianName;
        public CivilianStatus status;
        public string statusNote;   // free-text, e.g. "fate currently unknown"
    }

    // The Named Civilian Thread system isn't shipped yet. UM-01 ships a stub
    // service that returns no entries so the Ledger's right column renders
    // its header but has nothing beneath it. When the Civilian Thread ticket
    // lands, a real implementation replaces NullCivilianThreadService.
    public interface ICivilianThreadService
    {
        IReadOnlyList<CivilianStatusEntry> ForCurrentChapter();
        bool AnyOnMapThisChapter();
    }

    public sealed class NullCivilianThreadService : ICivilianThreadService
    {
        public static readonly NullCivilianThreadService Instance = new NullCivilianThreadService();
        private static readonly CivilianStatusEntry[] Empty = new CivilianStatusEntry[0];

        public IReadOnlyList<CivilianStatusEntry> ForCurrentChapter() => Empty;
        public bool AnyOnMapThisChapter() => false;
    }
}
