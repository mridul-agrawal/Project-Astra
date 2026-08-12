using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Composition root for the full-screen Unit Info screen. Owns the Views (serialized),
    // news the plain-C# per-panel controllers, drives the STATS/GEAR tab + row-selection
    // navigation, and manages the GameState.UnitInfoScreen lifecycle. It is the single
    // InputManager subscription point while the screen is up.
    //
    // Lives on an always-woken Canvas child (like the Battle HUD / Combat Forecast roots) so
    // Awake runs and it hears the state transition; panelRoot is the visible content it toggles.
    // The footer is the inspector — it always shows the selected row's details, so there is no
    // separate inspect action.
    public class UnitInfoUIController : MonoBehaviour
    {
        // An image that carries the selection accent, and how strongly. §2 swaps the accent for
        // the danger red on enemy sheets and changes nothing else.
        [Serializable]
        public struct AccentTarget
        {
            public Image Target;
            public float Alpha;
        }

        [Header("Screen")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private UnitInfoScreenTransition transition;

        [Header("Views")]
        [SerializeField] private UnitSummaryView summaryView;
        [SerializeField] private UnitStatsView statsView;
        [SerializeField] private UnitGearView gearView;
        [SerializeField] private UnitInfoTabBarView tabBarView;
        [SerializeField] private UnitInfoFooterView footerView;

        [Header("§8 focus")]
        [SerializeField] private UnitInfoFocusMarker focusMarker;
        [SerializeField] private Image focusMarkerRing;
        [SerializeField] private Image focusMarkerGlow;

        [Header("§2 accent")]
        [SerializeField] private AccentTarget[] accentTargets;
        [SerializeField] private Color allyAccent = new Color32(0x4f, 0x9d, 0xff, 0xff);
        [SerializeField] private Color enemyAccent = new Color32(0xe0, 0x52, 0x4f, 0xff);

        [Header("§8 label emphasis")]
        [SerializeField] private Color focusedLabel = Color.white;
        [SerializeField] private Color restingLabel = new Color32(0xae, 0xb6, 0xc4, 0xff);

        [Header("Data")]
        [SerializeField] private StatInfoTable statInfo;

        private UnitSummaryController summaryController;
        private UnitStatsController statsController;
        private UnitGearController gearController;

        private TestUnit unit;
        private UnitInfoTab currentTab;
        private int statsFocus, gearFocus;              // §8 keeps a focus per tab
        private float lastMoveTime;

        // The weapon card is stop zero on the STATS tab and the nine stat rows follow it.
        private const int WeaponStop = 0;
        private const int FirstStatRowStop = 1;

        // A move that lands within this of the previous one is a held repeat, so the marker snaps.
        private const float HeldRepeatWindow = 0.12f;

        private void Awake()
        {
            CreateControllers();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            EventService.Instance?.SubscribeGameStateChanged(OnStateChanged);
        }

        private void CreateControllers()
        {
            summaryController = new UnitSummaryController(summaryView);
            statsController = new UnitStatsController(statsView, statInfo);
            gearController = new UnitGearController(gearView);
        }

        private void OnDestroy() => EventService.Instance?.UnsubscribeGameStateChanged(OnStateChanged);

        // Entry point — GridCursor calls this when the player opens a unit's sheet.
        public void Open(TestUnit unit)
        {
            if (unit == null)
                return;

            this.unit = unit;
            GameStateManager.Instance?.RequestTransition(GameState.UnitInfoScreen, nameof(UnitInfoUIController));
        }

        private void OnStateChanged(StateChangeArgs args)
        {
            if (args.NewState == GameState.UnitInfoScreen)
                Activate();
            else if (args.PreviousState == GameState.UnitInfoScreen)
                Deactivate();
        }

        private void Activate()
        {
            if (unit == null)
                return;

            InitializeUnitInfoPanel();
            ApplyAccent(unit.faction == Faction.Enemy);

            summaryController.Render(unit);
            statsController.Render(unit);
            gearController.Render(unit);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            ApplyTab();
            ApplySelection(true);
            transition?.PlayEntry();
            SubscribeInput();
            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        private void Deactivate()
        {
            UnsubscribeInput();
            focusMarker?.Hide();
            HidePanelAfterFade();
            AudioManager.Instance?.Play(SoundId.UiPanelClose);
        }

        // §10 fades out before the panel disappears, so the hide waits on the transition.
        private void HidePanelAfterFade()
        {
            if (panelRoot == null) return;

            if (transition == null)
            {
                panelRoot.SetActive(false);
                return;
            }
            transition.PlayExit(() => panelRoot.SetActive(false));
        }

        private void InitializeUnitInfoPanel()
        {
            currentTab = UnitInfoTab.Stats;
            statsFocus = FirstStatRowStop;      // §8 opens on the first stat row, not the weapon
            gearFocus = FirstFilledSlot();
        }

        // --- §2 accent -------------------------------------------------------------------------

        private void ApplyAccent(bool enemy)
        {
            Color accent = enemy ? enemyAccent : allyAccent;

            if (accentTargets != null)
                foreach (var target in accentTargets)
                    if (target.Target != null)
                        target.Target.color = WithAlpha(accent, target.Alpha);

            if (focusMarkerRing != null) focusMarkerRing.color = accent;
            if (focusMarkerGlow != null) focusMarkerGlow.color = accent;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha <= 0f ? 1f : alpha);

        // --- Input -----------------------------------------------------------------------------

        private void SubscribeInput()
        {
            InputManager.Instance.OnCursorMove += HandleCursorMove;
            InputManager.Instance.OnNextUnit += ToggleTab;
            InputManager.Instance.OnPrevUnit += ToggleTab;
            InputManager.Instance.OnCancel += HandleCancel;
        }

        private void UnsubscribeInput()
        {
            InputManager.Instance.OnCursorMove -= HandleCursorMove;
            InputManager.Instance.OnNextUnit -= ToggleTab;
            InputManager.Instance.OnPrevUnit -= ToggleTab;
            InputManager.Instance.OnCancel -= HandleCancel;
        }

        private void HandleCursorMove(Vector2Int dir)
        {
            if (dir.x != 0)
            {
                ToggleTab();
                return;
            }

            if (dir.y == 0)
                return;

            bool held = Time.unscaledTime - lastMoveTime < HeldRepeatWindow;
            lastMoveTime = Time.unscaledTime;

            int step = dir.y > 0 ? -1 : 1;              // Unity +y is up; rows flow downward
            MoveFocus(step);
            ApplySelection(held);
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        // §8 wraps top to bottom, and skips gear slots that hold nothing.
        private void MoveFocus(int step)
        {
            int count = StopCount();
            if (count <= 0) return;

            int next = Focus;
            for (int guard = 0; guard < count; guard++)
            {
                next = (next + step + count) % count;
                if (!IsSkipped(next)) break;
            }
            Focus = next;
        }

        private bool IsSkipped(int stop) =>
            currentTab == UnitInfoTab.Gear && gearView != null && gearView.IsSlotEmpty(stop);

        private int Focus
        {
            get => currentTab == UnitInfoTab.Stats ? statsFocus : gearFocus;
            set
            {
                if (currentTab == UnitInfoTab.Stats) statsFocus = value;
                else gearFocus = value;
            }
        }

        private int StopCount() => currentTab == UnitInfoTab.Stats
            ? statsController.RowCount + 1                  // the weapon card sits ahead of the rows
            : gearController.SlotCount;

        private int FirstFilledSlot()
        {
            int count = gearController != null ? gearController.SlotCount : 0;
            for (int i = 0; i < count; i++)
                if (gearView == null || !gearView.IsSlotEmpty(i)) return i;
            return 0;
        }

        private void ToggleTab()
        {
            currentTab = currentTab == UnitInfoTab.Stats ? UnitInfoTab.Gear : UnitInfoTab.Stats;
            ApplyTab();
            ApplySelection(true);
            AudioManager.Instance?.Play(SoundId.UiTab);
        }

        private void HandleCancel() =>
            GameStateManager.Instance?.RequestTransition(GameState.BattleMap, nameof(UnitInfoUIController));

        // --- Presentation ----------------------------------------------------------------------

        private void ApplyTab()
        {
            tabBarView?.Render(currentTab);
            bool stats = currentTab == UnitInfoTab.Stats;
            statsView?.SetTabActive(stats);
            gearView?.SetTabActive(!stats);
            PlayTabSwap(stats);
        }

        private void PlayTabSwap(bool stats)
        {
            var content = stats
                ? statsView?.Root?.GetComponent<RectTransform>()
                : gearView?.Root?.GetComponent<RectTransform>();
            transition?.PlayTabSwap(content);
        }

        private void ApplySelection(bool instant)
        {
            if (currentTab == UnitInfoTab.Stats) ApplyStatsSelection();
            else ApplyGearSelection(instant);

            focusMarker?.MoveTo(FocusedRect(), instant);
            footerView?.Render(BuildFooter());
        }

        private void ApplyStatsSelection()
        {
            int row = statsFocus - FirstStatRowStop;
            statsView?.SetWeaponSelected(statsFocus == WeaponStop);
            statsView?.SetSelected(-1);                       // the moving marker replaces per-row art
            statsView?.ApplyLabelEmphasis(row, focusedLabel, restingLabel);
        }

        private void ApplyGearSelection(bool instant)
        {
            gearView?.SetSelected(gearFocus, instant);
        }

        private RectTransform FocusedRect()
        {
            if (currentTab == UnitInfoTab.Gear) return gearView?.FocusRectFor(gearFocus);
            if (statsFocus == WeaponStop) return statsView?.WeaponFocusRect;
            return statsView?.FocusRectFor(statsFocus - FirstStatRowStop);
        }

        private UnitInfoFooterModel BuildFooter()
        {
            if (currentTab == UnitInfoTab.Gear) return gearController.FooterFor(gearFocus);
            return statsFocus == WeaponStop
                ? statsController.WeaponFooter()
                : statsController.FooterFor(statsFocus - FirstStatRowStop);
        }
    }
}
