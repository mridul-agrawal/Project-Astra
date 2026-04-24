using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Modal level-up screen. Displays the unit's portrait, name, the level
    /// transition, and each of the 9 stats with its gain. Rows fade in one at
    /// a time; non-zero gains flash accent. Confirm dismisses.
    ///
    /// Driven by ExpGranter inside a BattleMap → LevelUpScreen state transition.
    /// </summary>
    public class LevelUpScreenUI : MonoBehaviour
    {
        [Serializable]
        public struct StatRow
        {
            public StatIndex stat;
            public CanvasGroup rowCanvasGroup;
            public TMP_Text labelText;
            public TMP_Text beforeValueText;
            public TMP_Text gainText;
            public TMP_Text afterValueText;
        }

        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TMP_Text _unitNameText;
        [SerializeField] private TMP_Text _levelTransitionText;
        [SerializeField] private TMP_Text _confirmHintText;
        [SerializeField] private StatRow[] _statRows;

        [Header("Timing")]
        [SerializeField] private float _rowStaggerSeconds = 0.1f;
        [SerializeField] private float _rowFadeSeconds = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color _gainAccentColor = new Color(0.486f, 0.827f, 0.416f, 1f);
        [SerializeField] private Color _noGainColor = new Color(0.541f, 0.478f, 0.345f, 1f);

        private bool _confirmReceived;
        private bool _confirmBound;

        public IEnumerator Play(TestUnit unit, StatArray preStats, StatArray gains, int preLevel, Sprite portrait)
        {
            if (_overlayRoot != null) _overlayRoot.SetActive(true);

            if (_unitNameText != null)
                _unitNameText.text = unit != null ? unit.name : "";
            if (_portraitImage != null)
            {
                _portraitImage.sprite = portrait;
                _portraitImage.enabled = portrait != null;
            }
            if (_levelTransitionText != null)
                _levelTransitionText.text = $"Level {preLevel}  →  {preLevel + 1}";

            if (_confirmHintText != null)
            {
                _confirmHintText.gameObject.SetActive(false);
                _confirmHintText.text = "▼  Press Confirm to continue";
            }

            if (_statRows != null)
            {
                foreach (var row in _statRows)
                    InitializeRow(row);
            }

            if (_statRows != null)
            {
                foreach (var row in _statRows)
                {
                    PopulateRow(row, preStats, gains);
                    yield return FadeRowIn(row);
                    yield return new WaitForSeconds(_rowStaggerSeconds);
                }
            }

            if (_confirmHintText != null) _confirmHintText.gameObject.SetActive(true);

            _confirmReceived = false;
            BindConfirm();
            while (!_confirmReceived) yield return null;
            UnbindConfirm();

            if (_overlayRoot != null) _overlayRoot.SetActive(false);
        }

        private void InitializeRow(StatRow row)
        {
            if (row.rowCanvasGroup != null) row.rowCanvasGroup.alpha = 0f;
        }

        private void PopulateRow(StatRow row, StatArray preStats, StatArray gains)
        {
            int pre = preStats[row.stat];
            int gain = gains[row.stat];
            int after = pre + gain;

            if (row.labelText != null) row.labelText.text = StatLabel(row.stat);
            if (row.beforeValueText != null) row.beforeValueText.text = pre.ToString();
            if (row.afterValueText != null) row.afterValueText.text = after.ToString();
            if (row.gainText != null)
            {
                row.gainText.text = gain > 0 ? $"+{gain}" : "—";
                row.gainText.color = gain > 0 ? _gainAccentColor : _noGainColor;
            }
        }

        private IEnumerator FadeRowIn(StatRow row)
        {
            if (row.rowCanvasGroup == null || _rowFadeSeconds <= 0f)
            {
                if (row.rowCanvasGroup != null) row.rowCanvasGroup.alpha = 1f;
                yield break;
            }
            float t = 0f;
            while (t < _rowFadeSeconds)
            {
                t += Time.deltaTime;
                row.rowCanvasGroup.alpha = Mathf.Clamp01(t / _rowFadeSeconds);
                yield return null;
            }
            row.rowCanvasGroup.alpha = 1f;
        }

        private static string StatLabel(StatIndex stat)
        {
            switch (stat)
            {
                case StatIndex.HP:     return "HP";
                case StatIndex.Str:    return "Str";
                case StatIndex.Mag:    return "Mag";
                case StatIndex.Skl:    return "Skl";
                case StatIndex.Spd:    return "Spd";
                case StatIndex.Def:    return "Def";
                case StatIndex.Res:    return "Res";
                case StatIndex.Con:    return "Con";
                case StatIndex.Niyati: return "Niyati";
                default:               return stat.ToString();
            }
        }

        private void Confirm() => _confirmReceived = true;

        private void BindConfirm()
        {
            if (_confirmBound || InputManager.Instance == null) return;
            InputManager.Instance.OnConfirm += Confirm;
            _confirmBound = true;
        }

        private void UnbindConfirm()
        {
            if (!_confirmBound) return;
            if (InputManager.Instance != null)
                InputManager.Instance.OnConfirm -= Confirm;
            _confirmBound = false;
        }

        private void OnDisable() => UnbindConfirm();
    }
}
