# Map 1 Flow — Scripts Actually Used (in order)

The runtime scripts (your own code under `Assets/Scripts/Core`) that actually run in the
current game flow, traced **BootScene → Splash → Title → opening cutscene → BattleMap
(Map 1) → victory → ending cutscene → Title**, in the order each one first enters the flow.

**How to read this**
- Ordered by **first appearance**. Inside BattleMap, dozens of scripts run *concurrently*,
  so that phase is sub-grouped by logical sub-flow.
- Each script is listed once, at its first appearance. **Boot singletons stay live the whole time.**
- Under each MonoBehaviour, *"uses:"* lists the non-MonoBehaviour helpers / ScriptableObject
  classes it pulls in.
- **Core** = always exercised. **Conditional** = present in the scene but only fires if a
  specific action/condition occurs (and Map 1's simple teaching flow often doesn't trigger it).
- The MonoBehaviour lists are exact (read from the scene/prefab files). The helper/SO
  attributions come from the call-graph and are solid for the traced systems; the conditional
  group is where "used" is fuzziest (initialized vs. actually triggered).

Excluded by scope: external packages, editor tooling (`*Builder.cs`, `Core/Editor`), and tests (`Core.Tests`).

---

## Phase 0 — Boot (BootScene): persistent singletons, alive for the whole session
1. **`GameStateManager`** — top-level state machine; validates every transition.
   *uses:* `GameState`, `GameStateTransitionTable` (SO), `GameStateEventChannel` (SO).
2. **`SceneLoader`** — maps each `GameState` to a scene, loads through a fade; auto-creates `ScreenFader`.
3. **`ScreenFader`** — full-screen fade for every scene swap *(runtime-instantiated)*.
4. **`GameFlow`** — campaign director (`Begin`/`NotifyCutsceneFinished`/`NotifyBattleFinished`); holds Opening→Map1→Ending.
   *uses:* `CutsceneId`, `MapId`, `CutsceneCatalog` (SO), `MapCatalog` (SO), `MapData` (SO), `DialogueScript` (SO).
5. **`InputManager`** — raw input → logical actions, state-gated, DAS.
   *uses:* `InputContext`, `DelayedAutoShift`.
6. **`AudioManager`** — music / SFX / mixer buses.
   *uses:* `SoundId`, `AudioLibrary` (SO), `SoundSO`, `AudioBus`.
7. **`DialogueService`** — shared dialogue runner/queue; owns the persistent dialogue view.
   *uses:* `DialogueRunner`, `IDialogueView` + `DialogueView` (Resources/UI prefab), `DialogueScript`/`DialogueSegment`/`DialogueLine`/`DialogueNode`, `DialogueSpeaker`/`DialogueSpeakerRegistry` (SO), `DialogueSettings` (SO), `DialogueExpression`/`PortraitPosition`/`PortraitFacing`/`DialogueTriggeringContext`/`SpeakerIdAttribute`.
8. **`OverlayManager`** — on state change, instantiates `Resources/Overlays/{State}` prefabs.
   *uses:* `GameStateEventChannel`.

*Shared event-channel SOs created at boot and used throughout:* `GameStateEventChannel`, `TurnEventChannel`, `UnitDeathEventChannel`, `BattleDialogueEventChannel`.

## Phase 1 — Splash (Splash scene)
9. **`SplashScreenController`** — plays the splash video, then → TitleScreen.

## Phase 2 — Title (TitleScreen scene)
10. **`TitleScreenUI`** — title music; on Confirm calls `GameFlow.Begin()` (→ opening cutscene).

## Phase 3 — Opening cutscene (Cutscene scene)
11. **`CutsceneUI`** — asks `GameFlow` for the current script (`OPENING_CH01`), plays it via `DialogueService`, reports back when done.
    *Rendered by the boot `DialogueService` → `DialogueRunner` → `DialogueView` (typewriter, portraits, name plates).*

## Phase 4 — Map 1 load & spawn (BattleMap scene init)
12. **`MapBootstrapper`** — orchestrates map setup on scene load.
13. **`MapRenderer`** — paints `MapData` onto tilemap layers; terrain queries.
    *uses:* `MapData`/`MapCatalog`/`MapId` (SO), `TilesetDefinition` (SO), `TerrainType`, `MapLayer`, `SyncedAnimatedTile`, `TerrainBackgroundDatabase`.
14. **`PathfindingService`** + **`Pathfinder`** — reachability / attack-range graph (Dijkstra + Manhattan).
    *uses:* `TerrainStatTable` (SO), `TerrainType`, `MovementType`.
15. **`UnitSpawner`** — spawns units from data; creates **`TestUnit`** + **`UnitInventory`**.
    *uses:* `UnitDefinition`/`ClassDefinition`/`UnitDatabase` (SO), `UnitInstance`, `UnitRegistry`, `InventoryLoadout`/`InventoryItem`/`WeaponData`/`WeaponDefinition`, `StatArray`/`StatIndex`, `Faction`, `ClassType`, `MovementType`, `WeaponType`/`DamageType`/`MagicSchool`/`WeaponRank`/`WeaponTier`, `PanchaBhuta`, `Personality`.
16. **`CameraController`** + **`MapCamera`** — deadzone camera tracking / framing.
17. **`TurnManager`** — battle orchestrator; runs the prologue, then the phase loop.
    *uses:* `BattlePhaseManager`, `BattlePhase`, `TurnEventChannel`, `UnitRegistry`, `IBattlePrologue`, `IScriptedEnemyPhase`.

## Phase 5 — Beat 0 prologue (before turn 1)
18. **`Map1RaidPrologue`** (`IBattlePrologue`) — the scripted on-map raid cinematic.
    *uses:* `WorldMarker`, `HitFlashEffect`, `SpriteFader`, `CameraController`, `CursorMode`/`GridCursor`, `DialogueService.PlayRoutine` with `MAP1_PROLOGUE_ARANYA` (voiced via `UnitDefinition.Speaker` → `DialogueSpeaker`).

## Phase 6 — Player / turn loop (interactive core)
19. **`PhaseBannerUI`** — slides the "Player Phase" banner; raises `PhaseBannerFinished`.
20. **`DialogueTriggerDriver`** — after the banner, fires the tutorial line (`MAP1_T1_MOVE`).
    *uses:* `DialogueTrigger` (+ trigger-set), `BattleDialogueEventChannel`, `BattleDialogueEventType`.
21. **`Map1BattleDirector`** (`IScriptedEnemyPhase`) — scripted beats, intent telegraph, deterministic duels.
    *uses:* `Map1Beat` (beat enum + state machine), `Map1Tuning` (SO), `EnemyIntentTelegraph`, `ObjectiveBannerUI`, `ScriptedCombatRng`, `CombatScriptOverride`, `CombatAnimationSettingsRef`, `WorldMarker`, `UnitMover`, `CameraController`.
22. **`GridCursor`** — input hub + cursor-mode state machine.
    *composes:* `UnitSelectionFlow`, `ActionMenuFlow`, `TargetingFlow`, `CantoFlow`, `AdjacentAllyFinder`; *uses:* `CursorMode`, `CursorAnimator`, `PathfindingService`/`Pathfinder`.
23. **`RangeHighlighter`** — move/attack range tiles. **`PathArrowRenderer`** — planning arrow.
24. **`UnitMover`** — animates a unit along the committed path.
25. **`UnitActionMenuUI`** — the post-move Attack/Item/Wait menu.
26. **`CombatExecutor`** — resolves an attack and dispatches playback.
    *combat core:* `CombatEngine`, `CombatRound`, `CombatForecast`, `IRng`/`ScriptedCombatRng`, `CombatScriptOverride`, `CritContext`/`CritContextClassifier`, `WeaponTriangle`, `WeaponData`, `DamageType`, `StatUtils`/`StatArray`, `HPThreshold`, `NiyatiSymbol`.
    *playback:* `CombatPlaybackDispatcher`, `CombatPlan`, `CombatPlaybackContext`, `CombatResultApplicator`, `BraveFlowComposer`, `CombatTiming`, `CombatAnimationSettings`/`Ref`/`Speed`.
27. **`CombatForecastUI`** (+ `CombatForecastRefs`) — pre-combat odds panel.
28. **`SkipModePlaybackController`** — fast/skip combat resolution path.
29. *(combat-animation overlay, on the `CombatAnimation` state):* **`CombatPlaybackController`**, **`CombatFighterView`**, **`CombatSceneRefs`** → `HitFlashEffect`.

**Battle HUD / feedback (live during the whole battle):**
30. **`BattleMapHUDController`**, **`BattleMapUI`** — HUD + pause entry.
31. **`UnitInfoPanelUI`** (+ `UnitInfoItemDetailUI`, `UnitInfoSupportDetailUI`, `UnitInfoContext`) — unit inspector.
32. **`MapHpBar`**, **`MapDamageFloat`** — on-map HP bars + damage numbers.
33. **`ToastNotificationUI`**, **`ConfirmDialogUI`** — toasts + confirm prompts.

## Phase 7 — Enemy phase / scripted beats
Driven by **`TurnManager`** → **`Map1BattleDirector.TryBuildPhaseScript`** (raider advance/hold, objective flip → `ObjectiveBannerUI`, boss taunt via `DialogueService` + `MAP1_BOSS_TAUNT`). Combat determinism via `ScriptedCombatRng`/`CombatScriptOverride`. Death events through `UnitDeathHook` → `UnitDeathEventChannel`.

## Phase 8 — Victory & post-victory wrap
34. **`ExpGranter`** — grants EXP after kills, triggers level-up. *uses:* `UnitInstance`, `ExpMath`, `StatArray`/`StatUtils`.
35. **`ExpGainOverlayUI`** — EXP count-up. **`LevelUpScreenUI`** — level-up screen.
36. **`BattleVictoryWatcher`** — detects the win, drives the transition into ChapterClear / back into `GameFlow`.
37. *(passive trackers running through the battle, read at the end):* **`CommitmentTracker`** + **`DeterministicCommitmentEvaluator`** (→ `Commitment` SO e.g. *ProtectSuvarnapurVillage*, `ChapterContext`), **`DeathRegistry`**, **`NamedCommanderScanner`**, and **`WarLedgerUI`** (+ `WarLedgerRefs`, `WarLedgerServices`) for named-enemy deaths (the boss).
38. **`ChapterClearUI`** (ChapterClear scene) — chapter-clear screen → on to the ending cutscene.

## Phase 9 — Ending cutscene → Title
39. **`CutsceneUI`** again — plays `ENDING_CH01` via `DialogueService`; on finish `GameFlow` reports campaign-complete → back to **`TitleScreenUI`**.

---

## Present in BattleMap but conditional (only fire if triggered — Map 1's flow usually doesn't)
- **Inventory UI:** `InventoryMenuUI`, `InventoryFullPromptUI`, `InventoryPopupRefs`, `ItemBreakToaster`, `ItemSortComparer`, `InventoryAcquisition`, `EquipResolver`, `ConsumableEffects` — only when a unit's items are opened. (`UnitInventory`/`InventoryItem`/`WeaponData` themselves *are* core — they back every unit.)
- **Trade:** `TradeScreenUI`, `TradeSession`, `TradeScreenRowVisuals` — only if trade is opened.
- **Convoy:** `ConvoyBootstrap` (its Awake *does* initialize the convoy), `ConvoyUI`, `SupplyConvoy`, `SupplyConvoyRefs`, `IConvoy` — UI only if opened.
- **Staff / healing:** `StaffExecutor`, `StaffEffects`, `StaffRangeResolver`, `HealingTileSystem`, `HealFloatSpawner`, `ConsumableEffects` — Map 1 has no healer / heal-tiles in the core flow.
- **Pause / menus (overlay prefabs):** `BattleMapPausedOverlayUI`, `SaveMenuOverlayUI`, `SettingsMenuOverlayUI` — only if the player pauses.
- **Misc visual:** `FlyingHoverAnimator` — only with a flying unit.

## Defined but not part of this win flow
- **Loss branch:** `LordDeathWatcher`, `GameOverUI`, `UnitDeathHook` (loss path) — fire only on defeat.
- **Bypassed / unused screens:** `MainMenuUI` (removed from the flow), `PreBattlePrepUI` (not in the campaign), `DialogueOverlayUI` (old `Dialogue.prefab`; `DialogueService` owns the real view).
- **Future-stub interfaces (declared, not exercised in Map 1):** `IFogOfWarProvider`, `ICivilianThreadService`, `IDeathEpitaphProvider`, `ISupportProvider`/`ISupportBonusProvider`/`SupportCombatBonus`/`BondStage`, `ITemporaryModifierProvider`, `IPersistable`, `WeaponRankTracker`.
