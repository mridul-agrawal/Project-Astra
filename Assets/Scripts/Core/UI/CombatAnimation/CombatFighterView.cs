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
        [SerializeField] private RectTransform _slotRoot;          // Translate tweens run on this
        [SerializeField] private Image _spriteImage;               // Fighter sprite; set at runtime
        [SerializeField] private CanvasGroup _canvasGroup;         // Optional — used for death fade

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI _unitNameLabel;
        [SerializeField] private TextMeshProUGUI _weaponNameLabel;
        [SerializeField] private TextMeshProUGUI _damageLabel;     // Pop-up: "12" / "MISS" / "12!" for crit

        [Header("HP bar")]
        [SerializeField] private Image _hpBarFill;                 // Fill driven via rectTransform.anchorMax.x
        [SerializeField] private RectTransform _hpBarRoot;         // Optional — hidden until first attack

        [Header("Tween distances (canvas pixels)")]
        [SerializeField] private float _attackLungeDistance = 80f;
        [SerializeField] private float _windupPullBack       = 12f;
        [SerializeField] private float _hitRecoilDistance    = 40f;
        [SerializeField] private float _dodgeDistance        = 70f;

        [Header("Hit flash")]
        [SerializeField] private Color _hitFlashColor    = Color.white;
        [SerializeField] private float _hitFlashSeconds  = 0.08f;

        [Header("Colors")]
        [SerializeField] private Color _damageColor = new Color(0.96f, 0.88f, 0.85f, 1f);
        [SerializeField] private Color _critColor   = new Color(0.98f, 0.94f, 0.28f, 1f);
        [SerializeField] private Color _missColor   = new Color(0.86f, 0.86f, 0.86f, 1f);

        // The attacker side's lunge is positive (toward center); the defender
        // side's lunge is negative. Set on Show() based on `facingRight`.
        private float _lungeSign = 1f;

        private Vector2 _idleAnchoredPos;
        private Color _idleSpriteColor;
        private int _currentHp;
        private int _maxHp;

        private void Awake()
        {
            if (_slotRoot != null) _idleAnchoredPos = _slotRoot.anchoredPosition;
            if (_spriteImage != null) _idleSpriteColor = _spriteImage.color;
        }

        // Configure the slot for a specific unit at the start of combat.
        // `facingRight` = which way the fighter faces (and lunges):
        // attacker (left side) faces right (true), defender (right side) faces left (false).
        public void Show(TestUnit unit, bool facingRight)
        {
            if (_slotRoot != null) _slotRoot.anchoredPosition = _idleAnchoredPos;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            _lungeSign = facingRight ? 1f : -1f;

            if (_spriteImage != null)
            {
                _spriteImage.sprite = unit?.UnitDefinition?.MapSprite;
                _spriteImage.color = _idleSpriteColor;
                // Mirror placeholder sprite for the side that doesn't match its default facing.
                var localScale = _spriteImage.rectTransform.localScale;
                float magX = Mathf.Abs(localScale.x);
                localScale.x = facingRight ? magX : -magX;
                _spriteImage.rectTransform.localScale = localScale;
            }

            if (_unitNameLabel != null)
                _unitNameLabel.text = unit?.UnitDefinition?.UnitName ?? unit?.name ?? "—";

            var weapon = unit?.Inventory != null ? unit.Inventory.GetEquippedWeapon() : default;
            if (_weaponNameLabel != null)
                _weaponNameLabel.text = weapon.IsEmpty ? "—" : weapon.name;

            if (_damageLabel != null) _damageLabel.text = "";

            _maxHp = unit?.UnitInstance != null ? unit.UnitInstance.MaxHP : unit != null ? unit.maxHP : 1;
            _currentHp = unit?.UnitInstance != null ? unit.UnitInstance.CurrentHP : unit != null ? unit.currentHP : 0;
            ApplyHpFill();
            if (_hpBarRoot != null) _hpBarRoot.gameObject.SetActive(true);
        }

        // --- Phase-aligned coroutines (durations chosen by the controller per the speed mode) ---

        public IEnumerator PlayWindup(float duration)
        {
            float pullBackDur = duration * 0.35f;
            float windupDur   = duration - pullBackDur;
            yield return TweenSlotX(0f, -_windupPullBack * _lungeSign, pullBackDur);
            yield return TweenSlotX(-_windupPullBack * _lungeSign, _attackLungeDistance * 0.45f * _lungeSign, windupDur);
        }

        public IEnumerator PlayStrike(float duration)
        {
            yield return TweenSlotX(_attackLungeDistance * 0.45f * _lungeSign, _attackLungeDistance * _lungeSign, duration);
        }

        public IEnumerator PlayHitReact(int damage, bool crit, float duration)
        {
            ShowDamageLabel(crit ? damage + "!" : damage.ToString(), crit ? _critColor : _damageColor);
            StartCoroutine(FlashSprite());
            yield return TweenSlotX(0f, -_hitRecoilDistance * _lungeSign, duration);
        }

        public IEnumerator PlayMissReact(float duration)
        {
            ShowDamageLabel("MISS", _missColor);
            float half = duration * 0.5f;
            yield return TweenSlotX(0f, -_dodgeDistance * _lungeSign, half);
            yield return TweenSlotX(-_dodgeDistance * _lungeSign, 0f, half);
        }

        public IEnumerator PlayFollowThrough(float duration)
        {
            float fromX = _slotRoot != null ? _slotRoot.anchoredPosition.x - _idleAnchoredPos.x : 0f;
            yield return TweenSlotX(fromX, 0f, duration);
            if (_damageLabel != null) _damageLabel.text = "";
        }

        public IEnumerator PlayDeath(float duration)
        {
            if (_spriteImage == null) yield break;
            var startColor = _spriteImage.color;
            var startAnchor = _slotRoot != null ? _slotRoot.anchoredPosition : Vector2.zero;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                var c = startColor; c.a = Mathf.Lerp(startColor.a, 0f, p);
                _spriteImage.color = c;
                if (_slotRoot != null)
                    _slotRoot.anchoredPosition = startAnchor + new Vector2(0f, -50f * p);
                yield return null;
            }
            if (_hpBarRoot != null) _hpBarRoot.gameObject.SetActive(false);
        }

        public void DrainTo(int newHp, float duration) =>
            StartCoroutine(DrainCoroutine(newHp, Mathf.Max(0f, duration)));

        public void Hide()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            else if (_spriteImage != null) { var c = _spriteImage.color; c.a = 0f; _spriteImage.color = c; }
        }

        // --- internals ---

        private void ShowDamageLabel(string text, Color color)
        {
            if (_damageLabel == null) return;
            _damageLabel.text = text;
            _damageLabel.color = color;
        }

        private IEnumerator FlashSprite()
        {
            if (_spriteImage == null) yield break;
            var original = _spriteImage.color;
            _spriteImage.color = _hitFlashColor;
            yield return new WaitForSeconds(_hitFlashSeconds);
            if (_spriteImage != null) _spriteImage.color = original;
        }

        private IEnumerator TweenSlotX(float fromOffset, float toOffset, float duration)
        {
            if (_slotRoot == null || duration <= 0f)
            {
                if (_slotRoot != null)
                    _slotRoot.anchoredPosition = _idleAnchoredPos + new Vector2(toOffset, 0f);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float x = Mathf.Lerp(fromOffset, toOffset, p);
                _slotRoot.anchoredPosition = _idleAnchoredPos + new Vector2(x, 0f);
                yield return null;
            }
            _slotRoot.anchoredPosition = _idleAnchoredPos + new Vector2(toOffset, 0f);
        }

        private IEnumerator DrainCoroutine(int target, float duration)
        {
            int startHp = _currentHp;
            if (duration <= 0f)
            {
                _currentHp = target;
                ApplyHpFill();
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _currentHp = Mathf.RoundToInt(Mathf.Lerp(startHp, target, p));
                ApplyHpFill();
                yield return null;
            }
            _currentHp = target;
            ApplyHpFill();
        }

        private void ApplyHpFill()
        {
            if (_hpBarFill == null) return;
            float ratio = _maxHp > 0 ? Mathf.Clamp01((float)_currentHp / _maxHp) : 0f;
            var anchorMax = _hpBarFill.rectTransform.anchorMax;
            anchorMax.x = ratio;
            _hpBarFill.rectTransform.anchorMax = anchorMax;
            _hpBarFill.color = ratio > 0.5f
                ? new Color(0.376f, 0.784f, 0.439f)
                : ratio > 0.25f
                    ? new Color(0.901f, 0.776f, 0.207f)
                    : new Color(0.901f, 0.282f, 0.282f);
        }
    }
}
