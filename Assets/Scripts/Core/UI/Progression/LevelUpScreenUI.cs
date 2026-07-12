using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Progression
{
    // Modal level-up screen. Shows the unit's portrait, name, the level
    // transition, and each of the 9 stats with its gain. Rows fade in one at
    // a time; non-zero gains flash accent. Confirm dismisses.
    //
    // Driven by ExpGranter inside a BattleMap → LevelUpScreen state transition.
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

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TMP_Text levelTransitionText;
        [SerializeField] private TMP_Text confirmHintText;
        [SerializeField] private StatRow[] statRows;

        [Header("Timing")]
        [SerializeField] private float rowStaggerSeconds = 0.1f;
        [SerializeField] private float rowFadeSeconds = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color gainAccentColor = new Color(0.486f, 0.827f, 0.416f, 1f);
        [SerializeField] private Color noGainColor = new Color(0.541f, 0.478f, 0.345f, 1f);

        private bool confirmReceived;
        private bool confirmBound;

        public IEnumerator Play(TestUnit unit, StatArray preStats, StatArray gains, int preLevel, Sprite portrait)
        {
            if (overlayRoot != null) overlayRoot.SetActive(true);
            AudioManager.Instance?.Play(SoundId.LevelUp);

            if (unitNameText != null)
                unitNameText.text = unit != null ? unit.name : "";
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
            }
            if (levelTransitionText != null)
                levelTransitionText.text = $"Level {preLevel}  →  {preLevel + 1}";

            if (confirmHintText != null)
            {
                confirmHintText.gameObject.SetActive(false);
                confirmHintText.text = "▼  Press Confirm to continue";
            }

            if (statRows != null)
            {
                foreach (var row in statRows)
                    InitializeRow(row);
            }

            if (statRows != null)
            {
                foreach (var row in statRows)
                {
                    PopulateRow(row, preStats, gains);
                    yield return FadeRowIn(row);
                    yield return new WaitForSeconds(rowStaggerSeconds);
                }
            }

            if (confirmHintText != null) confirmHintText.gameObject.SetActive(true);

            confirmReceived = false;
            BindConfirm();
            while (!confirmReceived) yield return null;
            UnbindConfirm();

            if (overlayRoot != null) overlayRoot.SetActive(false);
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
                row.gainText.color = gain > 0 ? gainAccentColor : noGainColor;
            }
        }

        private IEnumerator FadeRowIn(StatRow row)
        {
            if (row.rowCanvasGroup == null || rowFadeSeconds <= 0f)
            {
                if (row.rowCanvasGroup != null) row.rowCanvasGroup.alpha = 1f;
                yield break;
            }
            float t = 0f;
            while (t < rowFadeSeconds)
            {
                t += Time.deltaTime;
                row.rowCanvasGroup.alpha = Mathf.Clamp01(t / rowFadeSeconds);
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

        private void Confirm()
        {
            AudioManager.Instance?.Play(SoundId.UiConfirm);
            confirmReceived = true;
        }

        private void BindConfirm()
        {
            if (confirmBound || InputManager.Instance == null) return;
            InputManager.Instance.OnConfirm += Confirm;
            confirmBound = true;
        }

        private void UnbindConfirm()
        {
            if (!confirmBound) return;
            if (InputManager.Instance != null)
                InputManager.Instance.OnConfirm -= Confirm;
            confirmBound = false;
        }

        private void OnDisable() => UnbindConfirm();
    }
}
