using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Turn;

namespace ProjectAstra.Core.UI.Overlays
{
    // Slides a "Player Phase / Enemy Phase / Allied Phase" banner across the
    // screen at phase transitions, holds briefly, then slides back out. Phase
    // accent + glow material flip per side.
    public class PhaseBannerUI : MonoBehaviour
    {
        static readonly Color PlayerAccent = new(0.831f, 0.635f, 0.298f, 1f);
        static readonly Color EnemyAccent  = new(0.753f, 0.271f, 0.220f, 1f);
        static readonly Color AlliedAccent = new(0.165f, 0.541f, 0.290f, 1f);

        static readonly Color PlayerTextColor = new(0.961f, 0.824f, 0.478f, 1f);
        static readonly Color EnemyTextColor  = new(0.961f, 0.627f, 0.541f, 1f);
        static readonly Color AlliedTextColor = new(0.627f, 0.910f, 0.667f, 1f);

        static readonly Color TurnTextColor = new(0.541f, 0.478f, 0.345f, 1f);

        [Header("UI References")]
        [SerializeField] private RectTransform bannerRoot;
        [SerializeField] private Image borderTop;
        [SerializeField] private Image borderBottom;
        [SerializeField] private Image innerBorderTop;
        [SerializeField] private Image innerBorderBottom;
        [SerializeField] private Image[] orbImages;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private Image dimOverlay;

        [Header("Phase Materials")]
        [SerializeField] private Material playerGlowMat;
        [SerializeField] private Material enemyGlowMat;
        [SerializeField] private Material alliedGlowMat;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private float holdDuration = 1.2f;
        [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.55f;
        static readonly Color DimColor = new(0.0196f, 0.0118f, 0.0196f, 0f);

        private Coroutine bannerRoutine;

        private void Awake()
        {
            if (bannerRoot != null)
                bannerRoot.gameObject.SetActive(false);
            if (dimOverlay != null)
            {
                dimOverlay.color = DimColor;
                dimOverlay.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventService.Instance.SubscribePhaseStarted(OnPhaseStarted);
        }

        private void OnDisable()
        {
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribePhaseStarted(OnPhaseStarted);

            // A banner killed by a disable or a scene unload would otherwise leave the intro
            // flag stuck on, and with it the cursor permanently dead.
            bannerRoutine = null;
            PhaseIntro.End();
        }

        private void OnPhaseStarted(BattlePhase phase, int turnNumber)
        {
            PlayPhaseCue(phase);

            // Stop only our own banner. StopAllCoroutines would also kill the slide/dim
            // helpers mid-frame, and a superseded banner never reaches its finish, so the flag
            // has to be owned here rather than by the coroutine that got cut short.
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);

            PhaseIntro.Begin();
            bannerRoutine = StartCoroutine(ShowBanner(phase, turnNumber));
        }

        private static void PlayPhaseCue(BattlePhase phase)
        {
            var id = phase switch
            {
                BattlePhase.PlayerPhase => SoundId.PhasePlayer,
                BattlePhase.EnemyPhase  => SoundId.PhaseEnemy,
                BattlePhase.AlliedPhase => SoundId.PhaseAllied,
                _                       => SoundId.PhasePlayer,
            };
            AudioManager.Instance?.Play(id);
        }

        private IEnumerator ShowBanner(BattlePhase phase, int turnNumber)
        {
            ApplyPhaseVisuals(phase, turnNumber);
            if (bannerRoot == null) { RaiseBannerFinished(phase, turnNumber); yield break; }

            bannerRoot.gameObject.SetActive(true);
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(true);

            yield return AnimateSlideAndDim(-Screen.width, 0, 0f, dimAlpha, slideDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return AnimateSlideAndDim(0, Screen.width, dimAlpha, 0f, slideDuration);

            bannerRoot.gameObject.SetActive(false);
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);

            bannerRoutine = null;
            RaiseBannerFinished(phase, turnNumber);
        }

        // The gate everything else waits on: phase-start dialogue, the enemy AI's first move,
        // and the cursor and HUD coming back.
        private void RaiseBannerFinished(BattlePhase phase, int turnNumber)
        {
            PhaseIntro.End();
            if (EventService.Instance != null) EventService.Instance.RaisePhaseBannerFinished(phase, turnNumber);
        }

        private IEnumerator AnimateSlideAndDim(float fromX, float toX,
            float fromDim, float toDim, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                bannerRoot.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), 0f);
                SetDimAlpha(Mathf.Lerp(fromDim, toDim, t));
                yield return null;
            }
            bannerRoot.anchoredPosition = new Vector2(toX, 0f);
            SetDimAlpha(toDim);
        }

        private void SetDimAlpha(float alpha)
        {
            if (dimOverlay == null) return;
            var c = dimOverlay.color;
            c.a = alpha;
            dimOverlay.color = c;
        }

        private void ApplyPhaseVisuals(BattlePhase phase, int turnNumber)
        {
            var accent = GetAccentColor(phase);
            var textColor = GetTextColor(phase);
            var glowMat = GetGlowMaterial(phase);

            if (phaseText == null) return;

            phaseText.text = GetPhaseLabel(phase);
            phaseText.color = textColor;
            if (glowMat != null) phaseText.fontMaterial = glowMat;

            if (turnText != null)
                turnText.text = phase == BattlePhase.PlayerPhase ? $"Turn {turnNumber}" : "";

            if (borderTop != null) borderTop.color = WithAlpha(accent, 0.55f);
            if (borderBottom != null) borderBottom.color = WithAlpha(accent, 0.55f);
            if (innerBorderTop != null) innerBorderTop.color = WithAlpha(accent, 0.2f);
            if (innerBorderBottom != null) innerBorderBottom.color = WithAlpha(accent, 0.2f);

            if (orbImages != null)
                foreach (var orb in orbImages)
                    if (orb != null) orb.color = accent;
        }

        private static string GetPhaseLabel(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => "Player Phase",
                BattlePhase.EnemyPhase => "Enemy Phase",
                BattlePhase.AlliedPhase => "Allied Phase",
                _ => ""
            };
        }

        private static Color GetAccentColor(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => PlayerAccent,
                BattlePhase.EnemyPhase => EnemyAccent,
                BattlePhase.AlliedPhase => AlliedAccent,
                _ => PlayerAccent
            };
        }

        private static Color GetTextColor(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => PlayerTextColor,
                BattlePhase.EnemyPhase => EnemyTextColor,
                BattlePhase.AlliedPhase => AlliedTextColor,
                _ => Color.white
            };
        }

        private Material GetGlowMaterial(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => playerGlowMat,
                BattlePhase.EnemyPhase => enemyGlowMat,
                BattlePhase.AlliedPhase => alliedGlowMat,
                _ => playerGlowMat
            };
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
