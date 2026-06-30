using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.Battle.Map1
{
    // Every authored number behind Map 1's scripted beats, in one asset — the director
    // carries no literals. Coordinates are 15-wide-map tiles; north = high y.
    [CreateAssetMenu(fileName = "Map1Tuning", menuName = "Project Astra/Battle/Map 1 Tuning")]
    public class Map1Tuning : ScriptableObject
    {
        [Header("Actors (unit ids from the map's start positions)")]
        public string RaiderUnitId = "rakshasa_raider";
        public string BossUnitId = "rakshasa_miniboss";

        [Header("Telegraph — the raider's readable intent")]
        public Vector2Int[] IntentPath = { new(7, 7), new(7, 6), new(7, 5), new(7, 4), new(7, 3), new(7, 2) };
        public Vector2Int[] IntentPathAfterAdvance = { new(7, 5), new(7, 4), new(7, 3), new(7, 2) };
        public Vector2Int ThreatenedVillagerTile = new(7, 2);
        public Vector2Int CorkHintTile = new(7, 4);

        [Header("Turn 1 guided movement (connected path ending on the cork)")]
        public Vector2Int[] GuidedMovePath = { new(10, 2), new(9, 2), new(9, 3), new(8, 3), new(7, 3), new(7, 4) };

        [Header("Enemy phase — raider advance onto the bridge")]
        public Vector2Int[] RaiderAdvancePath = { new(7, 7), new(7, 6), new(7, 5) };
        public float RaiderFeintHoldSeconds = 0.5f;

        [Header("Scripted combat outcomes (per swing, in CombatRound order)")]
        public SwingOutcome[] RaiderDuelOutcomes = { SwingOutcome.Hit, SwingOutcome.Miss };
        public SwingOutcome[] BossDuelOutcomes = { SwingOutcome.Hit, SwingOutcome.Hit, SwingOutcome.Crit };

        [Header("Turn 4 — low-HP caption before the crit")]
        [TextArea] public string LowHpCaption = "Not yet. Suvarnapur still stands behind me.";
        public float CaptionHoldSeconds = 1.8f;

        [Header("Camera")]
        public float CameraPanSeconds = 0.8f;
        public float GameplayOrthoSize = 4.21875f;   // the map's pixel-perfect size
        public Vector2Int BossFrameTile = new(7, 9);

        [Header("Post-victory level-up — deterministic instead of growth dice")]
        [Tooltip("A stat gains +1 iff its growth rate is >= this. For Aranya at 40: HP, Str, Skl, Spd — a designed 'great first level'.")]
        public int LevelUpGrowthThreshold = 40;
    }
}
