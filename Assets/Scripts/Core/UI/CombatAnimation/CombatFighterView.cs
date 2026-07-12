using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.CombatAnimation
{
    // One side of the overlay combat scene. Renders the unit's portrait (or
    // placeholder enlarged MapSprite), tracks an HP bar that drains live, and
    // exposes phase-aligned animation coroutines the playback controller
    // sequences. Placeholder for Phase B — translate tweens on a single
    // sprite. Swap for an Animator-driven implementation when real combat
    // sprite art lands; method signatures stay the same.
    public class CombatFighterView : MonoBehaviour
    {
        [Header("Slot / sprite")]
        [SerializeField] private RectTransform slotRoot;          // Translate tweens run on this
        [SerializeField] private Image spriteImage;               // Fighter sprite; set at runtime
        [SerializeField] private CanvasGroup canvasGroup;         // Optional — used for death fade

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI unitNameLabel;
        [SerializeField] private TextMeshProUGUI weaponNameLabel;
        [SerializeField] private TextMeshProUGUI damageLabel;     // Pop-up: "12" / "MISS" / "12!" for crit

        [Header("HP bar")]
        [SerializeField] private Image hpBarFill;                 // Fill driven via rectTransform.anchorMax.x
        [SerializeField] private RectTransform hpBarRoot;         // Optional — hidden until first attack

        [Header("Tween distances (canvas pixels)")]
        [SerializeField] private float attackLungeDistance = 80f;
        [SerializeField] private float windupPullBack       = 12f;
        [SerializeField] private float hitRecoilDistance    = 40f;
        [SerializeField] private float dodgeDistance        = 70f;

        [Header("Hit flash")]
        [SerializeField] private Color hitFlashColor    = Color.white;
        [SerializeField] private float hitFlashSeconds  = 0.08f;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(0.96f, 0.88f, 0.85f, 1f);
        [SerializeField] private Color critColor   = new Color(0.98f, 0.94f, 0.28f, 1f);
        [SerializeField] private Color missColor   = new Color(0.86f, 0.86f, 0.86f, 1f);

        // The attacker side's lunge is positive (toward center); the defender
        // side's lunge is negative. Set on Show() based on `facingRight`.
        private float lungeSign = 1f;

        private Vector2 idleAnchoredPos;
        private Color idleSpriteColor;
        private int currentHp;
        private int maxHp;

        private void Awake()
        {
            if (slotRoot != null) idleAnchoredPos = slotRoot.anchoredPosition;
            if (spriteImage != null) idleSpriteColor = spriteImage.color;
        }

        // Configure the slot for a specific unit at the start of combat.
        // `facingRight` = which way the fighter faces (and lunges):
        // attacker (left side) faces right (true), defender (right side) faces left (false).
        public void Show(TestUnit unit, bool facingRight)
        {
            if (slotRoot != null) slotRoot.anchoredPosition = idleAnchoredPos;
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            lungeSign = facingRight ? 1f : -1f;

            if (spriteImage != null)
            {
                spriteImage.sprite = unit?.UnitDefinition?.MapSprite;
                spriteImage.color = idleSpriteColor;
                // Mirror placeholder sprite for the side that doesn't match its default facing.
                var localScale = spriteImage.rectTransform.localScale;
                float magX = Mathf.Abs(localScale.x);
                localScale.x = facingRight ? magX : -magX;
                spriteImage.rectTransform.localScale = localScale;
            }

            if (unitNameLabel != null)
                unitNameLabel.text = unit?.UnitDefinition?.UnitName ?? unit?.name ?? "—";

            var weapon = unit?.Inventory != null ? unit.Inventory.GetEquippedWeapon() : default;
            if (weaponNameLabel != null)
                weaponNameLabel.text = weapon.IsEmpty ? "—" : weapon.name;

            if (damageLabel != null) damageLabel.text = "";

            maxHp = unit?.UnitInstance != null ? unit.UnitInstance.MaxHP : unit != null ? unit.maxHP : 1;
            currentHp = unit?.UnitInstance != null ? unit.UnitInstance.CurrentHP : unit != null ? unit.currentHP : 0;
            ApplyHpFill();
            if (hpBarRoot != null) hpBarRoot.gameObject.SetActive(true);
        }

        // --- Phase-aligned coroutines (durations chosen by the controller per the speed mode) ---

        public IEnumerator PlayWindup(float duration)
        {
            float pullBackDur = duration * 0.35f;
            float windupDur   = duration - pullBackDur;
            yield return TweenSlotX(0f, -windupPullBack * lungeSign, pullBackDur);
            yield return TweenSlotX(-windupPullBack * lungeSign, attackLungeDistance * 0.45f * lungeSign, windupDur);
        }

        public IEnumerator PlayStrike(float duration)
        {
            yield return TweenSlotX(attackLungeDistance * 0.45f * lungeSign, attackLungeDistance * lungeSign, duration);
        }

        public IEnumerator PlayHitReact(int damage, bool crit, float duration)
        {
            ShowDamageLabel(crit ? damage + "!" : damage.ToString(), crit ? critColor : damageColor);
            StartCoroutine(FlashSprite());
            yield return TweenSlotX(0f, -hitRecoilDistance * lungeSign, duration);
        }

        public IEnumerator PlayMissReact(float duration)
        {
            ShowDamageLabel("MISS", missColor);
            float half = duration * 0.5f;
            yield return TweenSlotX(0f, -dodgeDistance * lungeSign, half);
            yield return TweenSlotX(-dodgeDistance * lungeSign, 0f, half);
        }

        public IEnumerator PlayFollowThrough(float duration)
        {
            float fromX = slotRoot != null ? slotRoot.anchoredPosition.x - idleAnchoredPos.x : 0f;
            yield return TweenSlotX(fromX, 0f, duration);
            if (damageLabel != null) damageLabel.text = "";
        }

        public IEnumerator PlayDeath(float duration)
        {
            if (spriteImage == null) yield break;
            var startColor = spriteImage.color;
            var startAnchor = slotRoot != null ? slotRoot.anchoredPosition : Vector2.zero;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                var c = startColor; c.a = Mathf.Lerp(startColor.a, 0f, p);
                spriteImage.color = c;
                if (slotRoot != null)
                    slotRoot.anchoredPosition = startAnchor + new Vector2(0f, -50f * p);
                yield return null;
            }
            if (hpBarRoot != null) hpBarRoot.gameObject.SetActive(false);
        }

        public void DrainTo(int newHp, float duration) =>
            StartCoroutine(DrainCoroutine(newHp, Mathf.Max(0f, duration)));

        public void Hide()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            else if (spriteImage != null) { var c = spriteImage.color; c.a = 0f; spriteImage.color = c; }
        }

        // --- internals ---

        private void ShowDamageLabel(string text, Color color)
        {
            if (damageLabel == null) return;
            damageLabel.text = text;
            damageLabel.color = color;
        }

        private IEnumerator FlashSprite()
        {
            if (spriteImage == null) yield break;
            var original = spriteImage.color;
            spriteImage.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashSeconds);
            if (spriteImage != null) spriteImage.color = original;
        }

        private IEnumerator TweenSlotX(float fromOffset, float toOffset, float duration)
        {
            if (slotRoot == null || duration <= 0f)
            {
                if (slotRoot != null)
                    slotRoot.anchoredPosition = idleAnchoredPos + new Vector2(toOffset, 0f);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float x = Mathf.Lerp(fromOffset, toOffset, p);
                slotRoot.anchoredPosition = idleAnchoredPos + new Vector2(x, 0f);
                yield return null;
            }
            slotRoot.anchoredPosition = idleAnchoredPos + new Vector2(toOffset, 0f);
        }

        private IEnumerator DrainCoroutine(int target, float duration)
        {
            int startHp = currentHp;
            if (duration <= 0f)
            {
                currentHp = target;
                ApplyHpFill();
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                currentHp = Mathf.RoundToInt(Mathf.Lerp(startHp, target, p));
                ApplyHpFill();
                yield return null;
            }
            currentHp = target;
            ApplyHpFill();
        }

        private void ApplyHpFill()
        {
            if (hpBarFill == null) return;
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            var anchorMax = hpBarFill.rectTransform.anchorMax;
            anchorMax.x = ratio;
            hpBarFill.rectTransform.anchorMax = anchorMax;
            hpBarFill.color = ratio > 0.5f
                ? new Color(0.376f, 0.784f, 0.439f)
                : ratio > 0.25f
                    ? new Color(0.901f, 0.776f, 0.207f)
                    : new Color(0.901f, 0.282f, 0.282f);
        }
    }
}
