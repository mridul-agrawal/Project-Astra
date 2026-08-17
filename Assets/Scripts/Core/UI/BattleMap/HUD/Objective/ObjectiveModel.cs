using System.Collections.Generic;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // One row of the OBJECTIVES checklist. A runtime copy of the map's authored objective, so
    // ticking one off during a battle never writes back into the MapData asset.
    public sealed class ObjectiveRowVM
    {
        public string Text;
        public bool Complete;
        public int Current;
        public int Max;

        // §B4 pins a counter to the row's right end only when there is progress to show.
        public bool HasCounter => Max > 0;
        public string CounterText => Current + "/" + Max;
    }

    // Display data for the Objective panel: the mission conditions, the side objectives, and which
    // corner the panel docks to.
    //
    // Turn and EnemiesRemaining are still tracked because the battle events that feed them are
    // still wired, but the spec's banner does not show them - they are kept so that reversing that
    // decision needs no controller work.
    public sealed class ObjectiveModel
    {
        public string WinText;
        public string LoseText;
        public int Turn;
        public int EnemiesRemaining;
        public HudCorner Corner;

        // Empty on most maps, and that is the normal case: the banner then renders the win and
        // lose pairs alone.
        public List<ObjectiveRowVM> Objectives = new();

        public bool HasObjectives => Objectives != null && Objectives.Count > 0;
    }
}
