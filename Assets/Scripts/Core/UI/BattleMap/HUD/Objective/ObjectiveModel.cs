namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Display data for the Objective panel: the mission conditions, the current turn,
    // how many enemies are left, and which corner the panel docks to.
    public sealed class ObjectiveModel
    {
        public string WinText;
        public string LoseText;
        public int Turn;
        public int EnemiesRemaining;
        public HudCorner Corner;
    }
}
