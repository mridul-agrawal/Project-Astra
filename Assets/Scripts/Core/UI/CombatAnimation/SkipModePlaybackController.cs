using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.CombatAnimation
{
    // Skip-mode playback — no scene transition; combat resolves on the map
    // view in ~0.6–1.0 seconds. Per-hit cadence:
    //   1. Apply HP at the strike instant.
    //   2. Spawn damage / miss / crit float over the receiver.
    //   3. Brief white hit-flash on the receiver's sprite.
    //   4. Drain the receiver's MapHpBar (lazy-spawned per unit).
    //   5. If the hit kills: PrepareDeath → SpriteFader.FadeOut → HideVictim
    //      → RaiseDeath → break (subsequent hits don't play).
    //   6. Inter-hit gap, repeat.
    //
    // Terminal: hide HP bars after a brief hold, call Finalize (durability +
    // EXP), invoke ctx.OnComplete.
    public class SkipModePlaybackController : MonoBehaviour
    {
        [Header("Visual helpers (assign in scene)")]
        [SerializeField] private MapDamageFloat _damageFloat;

        [Header("Audio")]
        [SerializeField] private WeaponAudioMap _audioMap;

        [Header("Timing (Skip mode)")]
        [SerializeField] private float _interHitGap = 0.15f;
        [SerializeField] private float _hpDrainDuration = 0.10f;
        [SerializeField] private float _deathFadeDuration = 0.30f;
        [SerializeField] private float _hpBarHideDelay = 0.30f;
        [SerializeField] private float _hitFlashDuration = 0.10f;
        [SerializeField] private Color _hitFlashColor = Color.white;

        public IEnumerator Play(CombatPlaybackContext ctx)
        {
            if (ctx == null) yield break;
            if (ctx.Result.Hits == null || ctx.Attacker == null || ctx.Defender == null)
            {
                CombatResultApplicator.Finalize(ctx);
                ctx.OnComplete?.Invoke();
                yield break;
            }

            // Pre-show HP bars for both combatants.
            var atkBar = MapHpBar.GetOrCreate(ctx.Attacker);
            atkBar?.Show(CurrentHP(ctx.Attacker), MaxHP(ctx.Attacker));
            var defBar = MapHpBar.GetOrCreate(ctx.Defender);
            defBar?.Show(CurrentHP(ctx.Defender), MaxHP(ctx.Defender));

            foreach (var hit in ctx.Result.Hits)
            {
                bool fromAttacker = hit.Attacker == "Attacker";
                var receiver = fromAttacker ? ctx.Defender : ctx.Attacker;
                var attacker = fromAttacker ? ctx.Attacker : ctx.Defender;
                if (receiver == null) continue;

                var receiverSprite = GetSprite(receiver);
                var receiverBar = receiver == ctx.Attacker ? atkBar : defBar;

                int curHp = CurrentHP(receiver);
                int damage = hit.Hit ? hit.Damage : 0;
                int newHp = Mathf.Max(0, curHp - damage);

                CombatResultApplicator.ApplyHitDamage(receiver, newHp);
                PlayHitSfx(attacker, hit.Hit, hit.Crit);

                if (_damageFloat != null)
                {
                    var pos = receiver.transform.position;
                    if (!hit.Hit) _damageFloat.Show(pos, 0, MapDamageFloat.Kind.Miss);
                    else if (hit.Crit) _damageFloat.Show(pos, damage, MapDamageFloat.Kind.Crit);
                    else _damageFloat.Show(pos, damage, MapDamageFloat.Kind.Damage);
                }

                if (hit.Hit)
                {
                    if (receiverSprite != null)
                        StartCoroutine(HitFlashEffect.Flash(receiverSprite, _hitFlashDuration, _hitFlashColor));
                    receiverBar?.DrainTo(newHp, _hpDrainDuration);
                }

                bool died = hit.Hit && newHp <= 0;
                if (died)
                {
                    var args = UnitDeathHook.PrepareDeath(receiver, attacker);
                    yield return SpriteFader.FadeOut(receiverSprite, _deathFadeDuration);
                    UnitDeathHook.HideVictim(receiver);
                    UnitDeathHook.RaiseDeath(args, ctx.DeathChannel);
                    break;
                }

                yield return CombatTiming.WaitSeconds(_interHitGap);
            }

            yield return CombatTiming.WaitSeconds(_hpBarHideDelay);
            atkBar?.Hide();
            defBar?.Hide();

            CombatResultApplicator.Finalize(ctx);
            ctx.OnComplete?.Invoke();
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

        private static SpriteRenderer GetSprite(TestUnit unit) =>
            unit != null ? unit.GetComponentInChildren<SpriteRenderer>() : null;

        private static int CurrentHP(TestUnit unit) =>
            unit?.UnitInstance != null ? unit.UnitInstance.CurrentHP : unit != null ? unit.currentHP : 0;

        private static int MaxHP(TestUnit unit) =>
            unit?.UnitInstance != null ? unit.UnitInstance.MaxHP : unit != null ? unit.maxHP : 1;
    }
}
