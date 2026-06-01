using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.CombatAnimation
{
    // Normal / Fast overlay playback. Lives on the rebuilt CombatAnimation
    // prefab root (instantiated by OverlayManager when the game enters the
    // CombatAnimation GameState). Picks up the pending context handed off
    // by CombatPlaybackDispatcher and walks the precomposed plan:
    //   - SingleHitStep → 5-phase per-hit timeline (windup → strike → resolve
    //                     → follow-through → inter-hit gap).
    //   - BraveHitStep  → windup → strike1 → short-windup (no inter-hit gap)
    //                     → strike2 → resolve → follow-through → gap.
    // Crit context drives flash color and pre-death pacing.
    public class CombatPlaybackController : MonoBehaviour
    {
        // Static handoff slot — set by CombatPlaybackDispatcher before
        // requesting the state transition that instantiates this prefab.
        // This controller picks it up in Start(), clears the slot.
        public static CombatPlaybackContext PendingContext;

        [SerializeField] private CombatSceneRefs _refs;

        [Header("Audio")]
        [SerializeField] private WeaponAudioMap _audioMap;

        [Header("Terrain background")]
        [SerializeField] private TerrainBackgroundDatabase _terrainDb;

        [Header("Crit flash (full-screen)")]
        [SerializeField] private float _critFlashIntensity = 0.85f;
        [SerializeField] private float _critFlashSeconds  = 0.25f;
        [SerializeField] private Color _righteousFlashColor = Color.white;
        [SerializeField] private Color _tragicFlashColor    = new(0.45f, 0.40f, 0.55f, 1f);  // dim purple-grey

        [Header("Crit pacing")]
        [Tooltip("Extra pre-death hold added before the fall when a Tragic crit kills.")]
        [SerializeField] private float _tragicPreDeathHold = 0.30f;
        [Tooltip("Death-fall speed multiplier when a Tragic crit kills (1.0 = normal, 0.7 = slower fall).")]
        [SerializeField, Range(0.1f, 1.0f)] private float _tragicFallSpeed = 0.7f;

        [Header("Brave fusion")]
        [Tooltip("Wind-up duration between the two strikes of a brave-fused pair (Normal-mode seconds).")]
        [SerializeField] private float _braveShortWindup = 0.15f;

        [Header("Open / close holds (Normal-mode seconds)")]
        [SerializeField] private float _openingHoldSeconds = 0.40f;
        [SerializeField] private float _closingHoldSeconds = 0.50f;
        [SerializeField] private float _deathHoldSeconds   = 0.50f;

        private void Start()
        {
            var ctx = PendingContext;
            PendingContext = null;
            if (ctx == null)
            {
                Debug.LogError("[CombatPlaybackController] No PendingContext on Start — returning to BattleMap.");
                ReturnToBattle();
                return;
            }
            StartCoroutine(Play(ctx));
        }

        public IEnumerator Play(CombatPlaybackContext ctx)
        {
            if (_refs == null)
            {
                Debug.LogError("[CombatPlaybackController] CombatSceneRefs not wired.");
                FinalizeAndReturn(ctx);
                yield break;
            }
            if (ctx == null || ctx.Plan == null || ctx.Plan.Steps == null
                || ctx.Attacker == null || ctx.Defender == null)
            {
                FinalizeAndReturn(ctx);
                yield break;
            }

            ApplyTerrainBackground(ctx.DefenderTerrain);

            if (_refs.LeftFighter  != null) _refs.LeftFighter.Show(ctx.Attacker, facingRight: true);
            if (_refs.RightFighter != null) _refs.RightFighter.Show(ctx.Defender, facingRight: false);

            yield return CombatTiming.WaitSeconds(_openingHoldSeconds);

            bool combatEnded = false;
            foreach (var step in ctx.Plan.Steps)
            {
                if (step is BraveHitStep brave)
                    yield return PlayBraveHit(ctx, brave, ended => combatEnded = ended);
                else if (step is SingleHitStep single)
                    yield return PlaySingleHit(ctx, single, ended => combatEnded = ended);

                if (combatEnded) break;
                yield return CombatTiming.WaitPhase(CombatTiming.Phase.InterHitGap);
            }

            yield return CombatTiming.WaitSeconds(_closingHoldSeconds);
            FinalizeAndReturn(ctx);
        }

        // --- Steps ---

        private IEnumerator PlaySingleHit(CombatPlaybackContext ctx, SingleHitStep step, System.Action<bool> setEnded)
        {
            var attackerUnit = step.AttackerIsStriker ? ctx.Attacker : ctx.Defender;
            var receiverUnit = step.AttackerIsStriker ? ctx.Defender : ctx.Attacker;
            var attackerView = step.AttackerIsStriker ? _refs.LeftFighter  : _refs.RightFighter;
            var defenderView = step.AttackerIsStriker ? _refs.RightFighter : _refs.LeftFighter;

            int newHp = Mathf.Max(0, CurrentHP(receiverUnit) - (step.Hit.Hit ? step.Hit.Damage : 0));

            PlayAttackBark(attackerUnit);
            if (attackerView != null)
                yield return attackerView.PlayWindup(CombatTiming.PhaseDuration(CombatTiming.Phase.Windup));

            CombatResultApplicator.ApplyHitDamage(receiverUnit, newHp);
            PlayHitSfx(attackerUnit, step.Hit.Hit, step.Hit.Crit);

            float strikeDur  = CombatTiming.PhaseDuration(CombatTiming.Phase.Strike);
            float resolveDur = CombatTiming.PhaseDuration(CombatTiming.Phase.Resolve);
            float followDur  = CombatTiming.PhaseDuration(CombatTiming.Phase.FollowThrough);

            if (attackerView != null) StartCoroutine(attackerView.PlayStrike(strikeDur));
            if (defenderView != null)
            {
                if (step.Hit.Hit) StartCoroutine(defenderView.PlayHitReact(step.Hit.Damage, step.Hit.Crit, strikeDur + followDur));
                else              StartCoroutine(defenderView.PlayMissReact(strikeDur + resolveDur));
            }
            if (step.Hit.Crit) StartCoroutine(CritFlash(step.CritContext));

            yield return CombatTiming.WaitPhase(CombatTiming.Phase.Strike);

            if (step.Hit.Hit && defenderView != null)
                defenderView.DrainTo(newHp, resolveDur + followDur);

            yield return CombatTiming.WaitPhase(CombatTiming.Phase.Resolve);
            if (attackerView != null)
                yield return attackerView.PlayFollowThrough(followDur);

            if (step.FatalHit)
            {
                yield return RunDeath(ctx, receiverUnit, attackerUnit, defenderView, step.CritContext);
                setEnded(true);
            }
        }

        private IEnumerator PlayBraveHit(CombatPlaybackContext ctx, BraveHitStep step, System.Action<bool> setEnded)
        {
            // Brave is always attacker-striker (composer guarantees this).
            var attackerUnit = ctx.Attacker;
            var receiverUnit = ctx.Defender;
            var attackerView = _refs.LeftFighter;
            var defenderView = _refs.RightFighter;

            int hpAfterHit1 = Mathf.Max(0, CurrentHP(receiverUnit) - (step.Hit1.Hit ? step.Hit1.Damage : 0));
            int hpAfterHit2 = Mathf.Max(0, hpAfterHit1            - (step.Hit2.Hit ? step.Hit2.Damage : 0));

            float strikeDur  = CombatTiming.PhaseDuration(CombatTiming.Phase.Strike);
            float resolveDur = CombatTiming.PhaseDuration(CombatTiming.Phase.Resolve);
            float followDur  = CombatTiming.PhaseDuration(CombatTiming.Phase.FollowThrough);
            float shortWindup = _braveShortWindup * SpeedScale();

            // --- Hit 1 ---
            PlayAttackBark(attackerUnit);
            if (attackerView != null)
                yield return attackerView.PlayWindup(CombatTiming.PhaseDuration(CombatTiming.Phase.Windup));

            CombatResultApplicator.ApplyHitDamage(receiverUnit, hpAfterHit1);
            PlayHitSfx(attackerUnit, step.Hit1.Hit, step.Hit1.Crit);

            if (attackerView != null) StartCoroutine(attackerView.PlayStrike(strikeDur));
            // Defender's recoil tween is sized to span BOTH strikes; the controller
            // does not reset it between hits — that's the brave-fusion visual.
            if (defenderView != null)
            {
                float combinedReactDur = strikeDur * 2 + shortWindup + followDur;
                if (step.Hit1.Hit) StartCoroutine(defenderView.PlayHitReact(step.Hit1.Damage, step.Hit1.Crit, combinedReactDur));
                else              StartCoroutine(defenderView.PlayMissReact(strikeDur + resolveDur));
            }
            if (step.Hit1.Crit) StartCoroutine(CritFlash(step.Hit1CritContext));

            yield return CombatTiming.WaitPhase(CombatTiming.Phase.Strike);

            // Single continuous HP drain across both hits (down to hit-2 result).
            if ((step.Hit1.Hit || step.Hit2.Hit) && defenderView != null)
                defenderView.DrainTo(hpAfterHit2, shortWindup + strikeDur + resolveDur + followDur);

            // If hit 1 killed, fall through to death. CombatRound usually truncates
            // hit 2 in that case (composer falls back to SingleHitStep), but be defensive.
            if (step.Hit1Fatal)
            {
                yield return RunDeath(ctx, receiverUnit, attackerUnit, defenderView, step.Hit1CritContext);
                setEnded(true);
                yield break;
            }

            // Short wind-up between strikes (no full pull-back, no inter-hit gap).
            yield return CombatTiming.WaitSeconds(shortWindup);

            // --- Hit 2 ---
            CombatResultApplicator.ApplyHitDamage(receiverUnit, hpAfterHit2);
            PlayHitSfx(attackerUnit, step.Hit2.Hit, step.Hit2.Crit);

            if (attackerView != null) StartCoroutine(attackerView.PlayStrike(strikeDur));
            if (step.Hit2.Crit) StartCoroutine(CritFlash(step.Hit2CritContext));

            yield return CombatTiming.WaitPhase(CombatTiming.Phase.Strike);
            yield return CombatTiming.WaitPhase(CombatTiming.Phase.Resolve);

            if (attackerView != null)
                yield return attackerView.PlayFollowThrough(followDur);

            if (step.Hit2Fatal)
            {
                yield return RunDeath(ctx, receiverUnit, attackerUnit, defenderView, step.Hit2CritContext);
                setEnded(true);
            }
        }

        // --- Death sequence with crit-context-aware pacing ---

        private IEnumerator RunDeath(CombatPlaybackContext ctx, TestUnit victim, TestUnit killer, CombatFighterView victimView, CritContext context)
        {
            var args = UnitDeathHook.PrepareDeath(victim, killer);
            PlayDeathBark(victim);

            if (context == CritContext.Tragic)
                yield return CombatTiming.WaitSeconds(_tragicPreDeathHold);

            if (victimView != null)
            {
                float hold = _deathHoldSeconds;
                if (context == CritContext.Tragic)
                    hold = _deathHoldSeconds / Mathf.Max(0.1f, _tragicFallSpeed);  // slower fall = longer hold
                yield return victimView.PlayDeath(hold);
            }

            UnitDeathHook.HideVictim(victim);
            UnitDeathHook.RaiseDeath(args, ctx.DeathChannel);
        }

        // --- Helpers ---

        private void FinalizeAndReturn(CombatPlaybackContext ctx)
        {
            CombatResultApplicator.Finalize(ctx);
            ctx?.OnComplete?.Invoke();
            ReturnToBattle();
        }

        private void ReturnToBattle()
        {
            var gsm = GameStateManager.Instance;
            if (gsm != null)
                gsm.RequestTransition(GameState.BattleMap, nameof(CombatPlaybackController));
        }

        private IEnumerator CritFlash(CritContext context)
        {
            var img = _refs.FullScreenFlash;
            if (img == null) yield break;
            var baseColor = context == CritContext.Tragic ? _tragicFlashColor : _righteousFlashColor;
            var color = baseColor;
            color.a = _critFlashIntensity;
            img.color = color;
            float t = 0f;
            while (t < _critFlashSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _critFlashSeconds);
                color.a = Mathf.Lerp(_critFlashIntensity, 0f, p);
                img.color = color;
                yield return null;
            }
            color.a = 0f;
            img.color = color;
        }

        private void ApplyTerrainBackground(ProjectAstra.Core.Grid.TerrainType terrain)
        {
            if (_refs.TerrainBackground == null || _terrainDb == null) return;
            var sprite = _terrainDb.GetBackground(terrain);
            if (sprite != null) _refs.TerrainBackground.sprite = sprite;
        }

        private static float SpeedScale()
        {
            switch (CombatTiming.EffectiveSpeed)
            {
                case CombatAnimationSpeed.Fast: return 0.5f;
                case CombatAnimationSpeed.Skip: return 0f;
                default:                        return 1f;
            }
        }

        private static void PlayAttackBark(TestUnit attacker)
        {
            var def = attacker != null ? attacker.UnitDefinition : null;
            if (def == null || AudioService.Instance == null) return;
            AudioService.Instance.PlayVoiceBark(def.PickAttackBark());
        }

        private static void PlayDeathBark(TestUnit victim)
        {
            var def = victim != null ? victim.UnitDefinition : null;
            if (def == null || AudioService.Instance == null) return;
            AudioService.Instance.PlayVoiceBark(def.DeathBark);
        }

        private void PlayHitSfx(TestUnit attacker, bool hit, bool crit)
        {
            if (AudioService.Instance == null) return;
            if (!hit)
            {
                if (_audioMap != null) AudioService.Instance.PlaySfx(_audioMap.GetMiss());
                return;
            }
            if (_audioMap == null || attacker?.Inventory == null) return;
            var weapon = attacker.Inventory.GetEquippedWeapon();
            if (weapon.IsEmpty) return;
            AudioService.Instance.PlaySfx(_audioMap.GetImpact(weapon.weaponType, crit));
        }

        private static int CurrentHP(TestUnit unit) =>
            unit?.UnitInstance != null ? unit.UnitInstance.CurrentHP : unit != null ? unit.currentHP : 0;
    }
}
