using System;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat.Playback
{
    // Carrier handed from CombatExecutor through the dispatcher into a
    // playback controller's coroutine. Shared between Skip and Overlay paths.
    public class CombatPlaybackContext
    {
        public TestUnit Attacker;
        public TestUnit Defender;
        public CombatResult Result;
        public CombatPlan Plan;  // Brave-fused step list; null = controller walks Result.Hits directly (Skip mode).
        public TerrainType DefenderTerrain;  // Drives the overlay background art via TerrainBackgroundDatabase.
        public UnitDeathEventChannel DeathChannel;
        public ToastNotificationUI ToastUI;
        public Action OnComplete;
    }
}
