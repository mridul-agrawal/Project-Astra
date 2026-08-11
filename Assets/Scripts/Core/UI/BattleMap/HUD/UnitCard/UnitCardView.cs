using System.Collections;
using ProjectAstra.Core.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public sealed class UnitCardView : MonoBehaviour
    {
        [Header("Widgets")]
        public GameObject Root;
        public TextMeshProUGUI UnitName;
        public TextMeshProUGUI HpValue;
        // The spec drops the level row, but the reference is kept so the row can be
        // switched back on without re-wiring the scene.
        public TextMeshProUGUI LvlValue;
        public Image HpFill;
        public Image PortraitImage;

        [Header("Spec widgets")]
        public TextMeshProUGUI HpMax;
        public Image PortraitBackdrop;
        public CanvasGroup Fader;
        public Material ActedPortraitMaterial;
        public Sprite PlaceholderBust;

        // Palette, straight from spec §8.
        private static readonly Color Emphasis    = new Color32(0xff, 0xff, 0xff, 0xff);
        private static readonly Color Muted       = new Color32(0x9a, 0xa2, 0xae, 0xff);
        private static readonly Color AllyAccent  = new Color32(0x4f, 0x9d, 0xff, 0xff);
        private static readonly Color EnemyAccent = new Color32(0xe0, 0x52, 0x4f, 0xff);

        // Retro-handheld bar colours. These are their own pair rather than the §8 accents:
        // the accents drive the portrait backdrop, these drive the bar.
        private static readonly Color AllyHpFill  = new Color32(0x59, 0x9e, 0x66, 0xff);
        private static readonly Color EnemyHpFill = new Color32(0xb8, 0x3d, 0x3d, 0xff);

        // Spec values are logical px in a 480x270 space; the canvas is 1920x1080, so x4.
        private const float RiseDistance  = 20f;
        private const float EntranceTime  = 0.12f;
        private const float ExitLinger    = 0.15f;
        private const float BackdropAlpha = 0.15f;
        private const float ActedBarAlpha = 0.45f;

        // Matches TileInfoView and ObjectiveView so the three panels sit level with each other.
        private const float EdgePad = 56f;

        private RectTransform rect;
        private HudCorner corner;
        private bool cornerInit;
        private Vector2 dockedPosition;
        private bool shown;
        private Coroutine motion;

        // Resolved on demand rather than in Awake, so docking also works when an editor tool
        // renders the card outside play mode.
        private RectTransform Rect
        {
            get
            {
                if (rect == null)
                    rect = Root != null ? Root.GetComponent<RectTransform>()
                                        : GetComponent<RectTransform>();
                return rect;
            }
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) Root.SetActive(visible);
        }

        public void Render(UnitCardModel model)
        {
            if (model == null)
            {
                BeginExit();
                return;
            }

            ApplyCorner(model.Corner);
            RenderText(model);
            RenderPortrait(model);
            RenderHpBar(model);
            BeginEntrance();
        }

        // ---- content -------------------------------------------------------------------

        private void RenderText(UnitCardModel model)
        {
            Color nameColor = model.HasActed ? Muted : Emphasis;

            if (UnitName != null)
            {
                UnitName.text = model.UnitName;
                UnitName.color = nameColor;
            }
            if (HpValue != null)
            {
                HpValue.text = model.CurrentHP.ToString();
                HpValue.color = nameColor;
            }
            if (HpMax != null)
                HpMax.text = "/" + model.MaxHP;
        }

        private void RenderPortrait(UnitCardModel model)
        {
            if (PortraitImage != null)
            {
                PortraitImage.sprite = model.unitCardPortriat != null
                    ? model.unitCardPortriat
                    : PlaceholderBust;
                PortraitImage.material = model.HasActed ? ActedPortraitMaterial : null;
            }

            if (PortraitBackdrop != null)
                PortraitBackdrop.color = WithAlpha(AccentFor(model.UnitFaction), BackdropAlpha);
        }

        // Driven by rect width rather than Image.fillAmount: the bar is an MPUIKit procedural
        // image, which draws its own shape and ignores the built-in fill modes.
        private void RenderHpBar(UnitCardModel model)
        {
            if (HpFill == null) return;

            float fraction = Mathf.Clamp01(Mathf.Round(model.HpFraction * 100f) / 100f);
            RectTransform fill = HpFill.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(fraction, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            Color barColor = HpFillFor(model.UnitFaction);
            HpFill.color = model.HasActed ? WithAlpha(barColor, ActedBarAlpha) : barColor;
        }

        private static Color HpFillFor(Faction faction) =>
            faction == Faction.Enemy ? EnemyHpFill : AllyHpFill;

        private static Color AccentFor(Faction faction) =>
            faction == Faction.Enemy ? EnemyAccent : AllyAccent;

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);

        // ---- docking -------------------------------------------------------------------

        // The composition root hands the card its corner; docking is the shared corner layout
        // every HUD panel uses, so the three of them never cover the cursor or each other.
        private void ApplyCorner(HudCorner target)
        {
            if (Rect == null || (cornerInit && target == corner)) return;
            corner = target;
            cornerInit = true;
            HudCornerLayout.Apply(Rect, target, EdgePad);
            dockedPosition = Rect.anchoredPosition;
        }

        // ---- motion (spec §2) ----------------------------------------------------------

        // Content swaps instantly while the card is up; the rise-and-fade only plays when
        // it comes back from fully hidden.
        private void BeginEntrance()
        {
            StopMotion();
            if (Root != null) Root.SetActive(true);

            if (shown || !Application.isPlaying)
            {
                shown = true;
                SetFade(1f, 0f);
                return;
            }

            shown = true;
            motion = StartCoroutine(PlayEntrance());
        }

        private void BeginExit()
        {
            if (!shown) return;
            StopMotion();

            if (!Application.isPlaying)
            {
                shown = false;
                if (Root != null) Root.SetActive(false);
                return;
            }

            motion = StartCoroutine(PlayExit());
        }

        private IEnumerator PlayEntrance()
        {
            float elapsed = 0f;
            while (elapsed < EntranceTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(elapsed / EntranceTime));
                SetFade(t, Mathf.Lerp(RiseDistance, 0f, t));
                yield return null;
            }
            SetFade(1f, 0f);
            motion = null;
        }

        private IEnumerator PlayExit()
        {
            yield return new WaitForSecondsRealtime(ExitLinger);

            float elapsed = 0f;
            while (elapsed < EntranceTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(elapsed / EntranceTime));
                SetFade(1f - t, Mathf.Lerp(0f, RiseDistance, t));
                yield return null;
            }

            shown = false;
            motion = null;
            if (Root != null) Root.SetActive(false);
        }

        private void StopMotion()
        {
            if (motion == null) return;
            StopCoroutine(motion);
            motion = null;
        }

        // Rise is applied on top of the docked position, so docking stays the source of truth.
        // The card starts above its resting place and settles down, matching the spec's
        // translateY(-5px) to 0.
        private void SetFade(float alpha, float rise)
        {
            if (Fader != null) Fader.alpha = alpha;
            if (Rect == null) return;

            Rect.anchoredPosition = dockedPosition + new Vector2(0f, rise);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
