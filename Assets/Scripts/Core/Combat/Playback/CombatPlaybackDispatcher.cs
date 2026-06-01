using UnityEngine;
using ProjectAstra.Core.State;
using ProjectAstra.Core.UI.CombatAnimation;

namespace ProjectAstra.Core.Combat.Playback
{
    // Routes a CombatPlaybackContext to the correct playback controller
    // based on the effective speed setting:
    //   Skip            → SkipModePlaybackController (on the persistent
    //                     CombatRuntime GO in BattleMap).
    //   Normal / Fast   → set CombatPlaybackController.PendingContext,
    //                     transition to GameState.CombatAnimation. The
    //                     OverlayManager instantiates the prefab, whose
    //                     CombatPlaybackController picks up the context in
    //                     its Start() and runs the overlay playback.
    //
    // Fallbacks (no controller wired, state transition rejected, etc.) call
    // CombatResultApplicator.Finalize + OnComplete so combat never hangs.
    public class CombatPlaybackDispatcher
    {
        private readonly SkipModePlaybackController _skipController;

        public CombatPlaybackDispatcher(SkipModePlaybackController skipController)
        {
            _skipController = skipController;
        }

        public void Dispatch(CombatPlaybackContext ctx)
        {
            if (ctx == null) return;

            // Per-combat speed override (GridCursor sets it when SkipAnimation
            // was held during Confirm). Wrap OnComplete to clear it at the end
            // so the next combat reverts to the persisted preference.
            var settings = CombatAnimationSettingsRef.Current;
            if (settings != null)
            {
                var inner = ctx.OnComplete;
                ctx.OnComplete = () =>
                {
                    settings.ClearOneShotOverride();
                    inner?.Invoke();
                };
            }

            var speed = CombatTiming.EffectiveSpeed;

            // Compose the brave-fused plan up front. Skip mode ignores it
            // (brave fusion is invisible at that cadence), Normal/Fast walk it.
            if (ctx.Plan == null)
                ctx.Plan = BraveFlowComposer.Compose(ctx.Result, ctx.Attacker, ctx.Defender);

            if (speed == CombatAnimationSpeed.Skip)
            {
                if (_skipController != null)
                {
                    _skipController.StartCoroutine(_skipController.Play(ctx));
                    return;
                }
                FallbackInstant(ctx, "Skip controller not wired");
                return;
            }

            // Normal or Fast — overlay path.
            CombatPlaybackController.PendingContext = ctx;

            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                CombatPlaybackController.PendingContext = null;
                FallbackInstant(ctx, "GameStateManager.Instance null");
                return;
            }

            bool ok = gsm.RequestTransition(GameState.CombatAnimation, "CombatPlaybackDispatcher");
            if (!ok)
            {
                CombatPlaybackController.PendingContext = null;
                FallbackInstant(ctx, "Transition to CombatAnimation rejected");
                return;
            }
            // Success — OverlayManager will instantiate the prefab and its
            // CombatPlaybackController.Start() will pick up the context.
        }

        private static void FallbackInstant(CombatPlaybackContext ctx, string reason)
        {
            Debug.LogWarning($"[CombatPlaybackDispatcher] Fallback instant playback — {reason}.");
            CombatResultApplicator.Finalize(ctx);
            ctx.OnComplete?.Invoke();
        }
    }
}
