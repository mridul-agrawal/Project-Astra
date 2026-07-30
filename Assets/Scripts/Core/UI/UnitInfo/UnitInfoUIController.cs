using UnityEngine;
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
        [Header("Screen")]
        [SerializeField] private GameObject panelRoot;

        [Header("Views")]
        [SerializeField] private UnitSummaryView summaryView;
        [SerializeField] private UnitStatsView statsView;
        [SerializeField] private UnitGearView gearView;
        [SerializeField] private UnitInfoTabBarView tabBarView;
        [SerializeField] private UnitInfoFooterView footerView;

        [Header("Data")]
        [SerializeField] private StatInfoTable statInfo;

        private UnitSummaryController summaryController;
        private UnitStatsController statsController;
        private UnitGearController gearController;

        private TestUnit unit;
        private UnitInfoTab currentTab;
        private int selectedIndex;
        private bool active;

        private void Awake()
        {
            summaryController = new UnitSummaryController(summaryView);
            statsController = new UnitStatsController(statsView, statInfo);
            gearController = new UnitGearController(gearView);
            if (panelRoot != null) panelRoot.SetActive(false);
            EventService.Instance?.SubscribeGameStateChanged(OnStateChanged);
        }

        private void OnDestroy() => EventService.Instance?.UnsubscribeGameStateChanged(OnStateChanged);

        // Entry point — GridCursor calls this when the player presses I on a unit.
        public void Open(TestUnit unit)
        {
            if (unit == null) return;
            this.unit = unit;
            GameStateManager.Instance?.RequestTransition(GameState.UnitInfoScreen, nameof(UnitInfoUIController));
        }

        private void OnStateChanged(StateChangeArgs args)
        {
            if (args.NewState == GameState.UnitInfoScreen) Activate();
            else if (args.PreviousState == GameState.UnitInfoScreen) Deactivate();
        }

        private void Activate()
        {
            if (unit == null) return;
            currentTab = UnitInfoTab.Stats;
            selectedIndex = 0;

            summaryController.Render(unit);
            statsController.Render(unit);
            gearController.Render(unit);

            if (panelRoot != null) panelRoot.SetActive(true);
            ApplyTab();
            ApplySelection();

            SubscribeInput();
            active = true;
            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        private void Deactivate()
        {
            if (!active) return;
            active = false;
            UnsubscribeInput();
            if (panelRoot != null) panelRoot.SetActive(false);
            AudioManager.Instance?.Play(SoundId.UiPanelClose);
        }

        // --- Input ---

        private void SubscribeInput()
        {
            var im = InputManager.Instance;
            if (im == null) return;
            im.OnCursorMove += HandleCursorMove;
            im.OnNextUnit += ToggleTab;
            im.OnPrevUnit += ToggleTab;
            im.OnCancel += HandleCancel;
        }

        private void UnsubscribeInput()
        {
            var im = InputManager.Instance;
            if (im == null) return;
            im.OnCursorMove -= HandleCursorMove;
            im.OnNextUnit -= ToggleTab;
            im.OnPrevUnit -= ToggleTab;
            im.OnCancel -= HandleCancel;
        }

        private void HandleCursorMove(Vector2Int dir)
        {
            if (dir.x != 0) { ToggleTab(); return; }   // Left/Right also flips tabs
            if (dir.y == 0) return;
            int step = dir.y > 0 ? -1 : 1;              // Unity +y is up; rows flow downward
            int count = RowCount();
            if (count <= 0) return;
            selectedIndex = Mathf.Clamp(selectedIndex + step, 0, count - 1);
            ApplySelection();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        private void ToggleTab()
        {
            currentTab = currentTab == UnitInfoTab.Stats ? UnitInfoTab.Gear : UnitInfoTab.Stats;
            selectedIndex = 0;
            ApplyTab();
            ApplySelection();
            AudioManager.Instance?.Play(SoundId.UiTab);
        }

        private void HandleCancel() =>
            GameStateManager.Instance?.RequestTransition(GameState.BattleMap, nameof(UnitInfoUIController));

        // --- Presentation ---

        private void ApplyTab()
        {
            tabBarView?.Render(currentTab);
            statsView?.SetTabActive(currentTab == UnitInfoTab.Stats);
            gearView?.SetTabActive(currentTab == UnitInfoTab.Gear);
        }

        private void ApplySelection()
        {
            if (currentTab == UnitInfoTab.Stats) statsView?.SetSelected(selectedIndex);
            else gearView?.SetSelected(selectedIndex);
            footerView?.Render(BuildFooter());
        }

        private int RowCount() => currentTab == UnitInfoTab.Stats ? statsController.RowCount : gearController.SlotCount;

        private UnitInfoFooterModel BuildFooter() =>
            currentTab == UnitInfoTab.Stats ? statsController.FooterFor(selectedIndex) : gearController.FooterFor(selectedIndex);
    }
}
