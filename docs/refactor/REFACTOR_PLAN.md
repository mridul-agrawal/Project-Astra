# Project Astra — Manual Refactor Plan

Grounded in the actual layout: 170 .cs / 22.8 K LOC, single assembly (`ProjectAstra.Core` + `Core.Editor` + `Core.Tests`), with `Core/Map/` (49 files) and `Core/UI/` (36 files) acting as god-folders. Top offenders: `GridCursor.cs` (1,171 LOC), `UnitInfoPanelUI.cs` (1,018), `TradeScreenUI.cs` (566), `InventoryMenuUI.cs` (541), `ConvoyUI.cs` (529), `CombatForecastUI.cs` (466).

The plan is sequenced so each phase is **independently shippable**, leaves the game runnable, and respects the layered dependency rule from `CLAUDE.md` (Layer 0 → Layer 1 → mechanics → UI).

---

## Guiding rules (apply in every phase)

- **One mechanic per branch / PR.** Never co-mingle a folder move with a logic change.
- **Behavior preservation first.** Phases 1–2 must be pure reorg + namespace shuffles. Run all EditMode + PlayMode tests before merge.
- **Dependency direction is one-way.** Foundation → Spatial → Mechanics → Presentation. Any upward edge is a bug.
- **Asmdef as the enforcer.** Once a layer is carved out, its `.asmdef` references the layers below — the compiler refuses upward references for free.
- **Tests move with code.** Don't separate a class from its test in the same PR.
- **Each god-class split is its own PR.** `GridCursor` alone is two weeks of careful surgery.

---

## Phase 0 — Safety Net & Baseline (before touching anything)

**Goal:** know what "still works" means, lock in a reference point.

1. Tag the current `master` as `pre-refactor-2026-05-02` so every phase has a comparison point.
2. Run all EditMode + PlayMode tests in batchmode; record pass count and runtime.
3. Capture a "smoke test" checklist of in-Editor verifications: title screen → battle map → move a unit → combat forecast → execute combat → EXP overlay → end phase → save → load. This is the manual gate for every merge.
4. Generate a **dependency snapshot**: for each script, who calls it (grep `: ClassName`, `new ClassName`, `FindObjectOfType<ClassName>`). Stash as `docs/refactor/dep-baseline.md`. Used in Phase 2 to design assembly boundaries.
5. Add a `docs/refactor/PHASES.md` checklist to track progress across phases.

**Exit criteria:** every test currently passing today is recorded as passing; baseline file committed.

---

## Phase 1 — Folder Reorg by Mechanic (zero logic change)

**Goal:** stop `Core/Map/` and `Core/UI/` from being grab-bags. Pure file moves + namespace renames.

The current `Core/Map/` mixes seven distinct concerns. Split it into mechanic-aligned folders. Each rename = one commit.

### 1.1 — Carve `Core/Map/` into sub-mechanics

| New folder | Files (from `Core/Map/`) |
|---|---|
| `Core/Grid/` | `MapData`, `MapLayer`, `MapRenderer`, `MapBootstrapper`, `TerrainType`, `TerrainStatTable`, `TilesetDefinition`, `SyncedAnimatedTile`, `OverlaySpriteFactory` |
| `Core/Pathfinding/` | `Pathfinder`, `PathfindingService`, `PathArrowRenderer`, `MovementType` |
| `Core/Camera/` | `CameraController`, `MapCamera` |
| `Core/Cursor/` | `GridCursor`, `CursorAnimator`, `CursorMode`, `RangeHighlighter` |
| `Core/Units/` | `UnitInstance`, `UnitDefinition`, `UnitMover`, `UnitRegistry` (move up from Core/), `ClassDefinition`, `ClassType`, `Faction` (move from Core/), `Personality`, `NiyatiSymbol`, `PanchaBhuta`, `BondStage`, `TestUnit`, `FlyingHoverAnimator` |
| `Core/Stats/` | `StatArray`, `StatIndex`, `StatUtils`, `HPThreshold` |
| `Core/Combat/` (already exists) | Move into it: `CombatEngine`, `CombatForecast`, `CombatRound`, `WeaponData`, `WeaponType`, `WeaponTier`, `WeaponRank`, `WeaponRankTracker`, `WeaponTriangle`, `MagicSchool`, `DamageType`, `StaffEffect`, `StaffRangeResolver`, `IRng` |
| `Core/Terrain/` | `HealingTileSystem` (currently mis-filed in Map) |
| `Core/Support/` | `ISupportProvider` (and any future affinity/bond mechanics) |

### 1.2 — Carve `Core/UI/` by screen

| New folder | Files |
|---|---|
| `Core/UI/Overlays/` | `CombatAnimationOverlayUI`, `DialogueOverlayUI`, `SaveMenuOverlayUI`, `SettingsMenuOverlayUI`, `BattleMapPausedOverlayUI`, `ExpGainOverlayUI`, `PhaseBannerUI`, `ToastNotificationUI`, `ConfirmDialogUI`, `InventoryFullPromptUI` |
| `Core/UI/MainMenu/` | `TitleScreenUI`, `MainMenuUI` |
| `Core/UI/BattleMap/` | `BattleMapUI`, `UnitActionMenuUI` |
| `Core/UI/Forecast/` | `CombatForecastUI`, `CombatForecastRefs`, `SupportCombatBonus` |
| `Core/UI/Inventory/` | `InventoryMenuUI`, `InventoryPopupRefs`, `UnitInfoItemDetailUI` |
| `Core/UI/Trade/` | `TradeScreenUI`, `TradeScreenRowVisuals` |
| `Core/UI/Convoy/` | `ConvoyUI`, `SupplyConvoyRefs` |
| `Core/UI/UnitInfo/` | `UnitInfoPanelUI`, `UnitInfoContext`, `UnitInfoSupportDetailUI` |
| `Core/UI/Cutscene/` | `CutsceneUI`, `DialogueSequencePlayer` |
| `Core/UI/WarLedger/` | `WarLedgerUI`, `WarLedgerRefs`, `WarLedgerServices` |
| `Core/UI/Progression/` | `ChapterClearUI`, `LevelUpScreenUI`, `PreBattlePrepUI`, `GameOverUI`, `HealFloatSpawner` |
| `Core/UI/Interfaces/` | `IFogOfWarProvider`, `ISupportBonusProvider`, `ITemporaryModifierProvider` |

Move the orphaned `Assets/Scripts/UI/BattleMapHUDController.cs` into `Core/UI/BattleMap/`.

### 1.3 — Namespace alignment

Rename namespaces to match folders: `ProjectAstra.Core.Map.X` → `ProjectAstra.Core.Grid.X` (or whichever sub-folder it landed in). One namespace rename per commit, keep diffs reviewable.

### 1.4 — Mirror `Core.Tests/` to match

For every Phase 1.1/1.2 move, mirror in `Core.Tests/`. Keeps test discoverability tight.

**Exit criteria:** project compiles; all tests still pass; manual smoke test passes; no logic changed.

---

## Phase 2 — Assembly Definition Split (enforce layering)

Right now everything is in one `Core.asmdef`. The compiler can't catch a UI script reaching into `MapBootstrapper` because they're the same DLL. Carve assemblies along the dependency lines from `CLAUDE.md`:

```
ProjectAstra.Foundation       (Layer 0: GameState, Input, Faction, SceneLoader)
   ↑
ProjectAstra.Stats            (StatArray, StatIndex, StatUtils, HPThreshold)
   ↑
ProjectAstra.Grid             (MapData, Pathfinder, TerrainStatTable, Camera)
   ↑
ProjectAstra.Units            (UnitInstance, UnitMover, UnitDefinition, UnitRegistry)
   ↑
ProjectAstra.Combat           (CombatEngine, Forecast, Weapons, Triangle)
   ↑
ProjectAstra.Inventory        (Items, Convoy, Trade, Equip)
   ↑
ProjectAstra.Progression      (Exp, Death, Commitments, Chapters)
   ↑
ProjectAstra.UI               (everything in Core/UI)
   ↑
ProjectAstra.Game             (top-level glue: TurnManager, BattlePhaseManager,
                                 entry-point bootstrappers)
```

Each `.asmdef` references **only the layers below it**. Tests get one `.asmdef` per layer (`ProjectAstra.<Layer>.Tests`) so a UI test can't accidentally pull in editor-only code.

This phase is mostly mechanical but exposes hidden cross-layer leaks — when a build breaks, the leak gets fixed (introduce an interface in the lower layer + DI from above) before moving on.

**Risk:** cyclic references are likely between `Combat` ↔ `Units` ↔ `Inventory` (e.g., `CombatEngine` reads inventory; equipping changes combat stats). Break cycles by introducing **read-only interfaces** in the lower layer (`ICombatant`, `IInventoryView`) that the upper layer implements.

**Exit criteria:** project builds with strict asmdef references; no `autoReferenced: true`; tests still green.

---

## Phase 3 — God-Class Decomposition

Now that the geography is sane, attack the file-level monsters. **One PR per class.** Each split must be covered by tests before and after.

### 3.1 — `GridCursor.cs` (1,171 LOC)

This is the worst offender and the most-coupled file in the codebase (everything routes through cursor state). Split into:

- **`GridCursor`** — pure state machine + input dispatch (~250 LOC)
- **`CursorMovementController`** — DAS, edge-of-map handling, snap-to-tile
- **`CursorTargetingController`** — handles `Targeting` mode (the combat/staff range cursor)
- **`CursorActionDispatcher`** — `CompleteAction` + canto seam (called out as a known integration point in memory)
- **`CursorSelectionState`** — what unit/tile is currently selected
- **`CursorOverlayCoordinator`** — owns range/path-arrow visualization

Tests: `GridCursorTests` already exists (226 LOC) — extend with one test class per new component.

### 3.2 — `UnitInfoPanelUI.cs` (1,018 LOC)

Split into MVP-style panels — one widget per tab (Stats / Inventory / Supports / Skills) with a parent shell coordinator. Each detail view (`UnitInfoItemDetailUI`, `UnitInfoSupportDetailUI`) already exists; finish the pattern.

### 3.3 — `TradeScreenUI` / `ConvoyUI` / `InventoryMenuUI` (~500–566 LOC each)

These three UIs share a common pattern (two-column item list + detail pane + give/take action). Extract:

- `ItemListPanel` (reusable) — list + selection + sort
- `ItemDetailPanel` (reusable) — item stats display
- `InventoryActionDispatcher` (reusable) — give/take/equip/discard

Trade, Convoy, and Inventory screens become thin compositions of these. This is the first phase that **introduces shared UI infrastructure**.

### 3.4 — `CombatForecastUI.cs` (466 LOC)

Split into `ForecastDataBinder` (pulls live forecast from `CombatForecast`) + `ForecastView` (pure rendering). Already has interfaces for fog-of-war and support — finish the seam.

**Exit criteria per sub-phase:** new file count higher, max file LOC under ~300, tests still pass, manual smoke test passes for the affected screen.

---

## Phase 4 — Data Layer Consolidation

After god-classes are split, the data definitions are still a mess: `WeaponData`, `ConsumableData`, `UnitDefinition`, `ClassDefinition`, `TilesetDefinition`, `TerrainStatTable` all live as ScriptableObjects but their generators (`Core/Editor/*Generator.cs`) are scattered.

1. Establish `Core/Data/` containing every `*Definition.cs` and `*Data.cs` (move from current sub-folders).
2. Establish `Core/Editor/Generators/` and group all `*Generator.cs` and `*Editor.cs` there.
3. Audit hardcoded values in MonoBehaviours (per `CLAUDE.md` rule: *all gameplay values must be data-driven*). Anything found becomes a SO field.
4. Add a `DataValidation` editor menu that walks every SO and asserts schema (no nulls in required refs, weapon ranks within enum range, etc.).

**Exit criteria:** every gameplay constant lives in a SO; one menu item validates them all.

---

## Phase 5 — Cross-Cutting Cleanup

By this point folders, assemblies, god-classes, and data are clean. Final pass for the systemic stuff that touched everything:

1. **Event channels audit** — `GameStateEventChannel`, `TurnEventChannel`, `UnitDeathEventChannel` are inconsistent (some are static, some ScriptableObject). Pick one pattern and migrate the rest.
2. **Singleton sweep** — anything using `FindObjectOfType` or `Instance` gets a constructor-injection or interface-locator alternative. The `IFogOfWarProvider` / `ISupportBonusProvider` / `ITemporaryModifierProvider` files show the team already favors interfaces — finish that conversion.
3. **State machine guard normalization** — `CLAUDE.md` mandates explicit transition tables with logged rejection. Audit `GameStateTransitionTable` consumers: every `GameStateManager.Transition` call must go through it; no ad-hoc state mutations.
4. **Comment cleanup** — per project rule (1-line max, "why" not "what"). Pass over all files; bulk-delete `// what` comments.
5. **Test reconciliation** — some test classes reference moved/renamed types; rename test files and test class names to match (`*EngineTests` → `Combat/*EngineTests`, etc.).

---

## Phase 6 — Documentation & Final Lock-In

1. Generate an architecture diagram (`docs/refactor/architecture.md`) showing the 9 assemblies and their reference graph. This becomes the new source of truth and supersedes the Layer 0/1 description in `CLAUDE.md` (update `CLAUDE.md` to match).
2. Add a `docs/refactor/CONTRIBUTING.md` snippet: "to add a new mechanic, here's where it goes."
3. Tag the result `post-refactor-1.0`.

---

## Phase ordering & dependencies

```
Phase 0 (baseline)
  → Phase 1 (folder reorg)            [pure moves; safe]
    → Phase 2 (asmdef split)          [exposes hidden coupling]
      → Phase 3 (god-class split)     [needs clean folders + asmdefs]
        → Phase 4 (data layer)        [easier once classes are small]
          → Phase 5 (cross-cutting)   [final sweep]
            → Phase 6 (docs + tag)
```

Phase 1 can ship in days; Phase 2 in a week; Phase 3 is the longest (each god-class is its own multi-day exercise); Phases 4–6 a week each. Total realistic budget: **6–8 weeks of focused work**, but each phase is independently valuable — you can stop after any phase and the codebase is strictly better than today.

---

## Highest-risk integration points to watch

These are gameplay seams where a refactor mistake will break things silently:

- **`GridCursor.CompleteAction`** — canto integration lives here (per memory), and most action dispatch funnels through it. Decomposing in Phase 3.1 is the single riskiest change.
- **`CombatEngine` ↔ `UnitInventory`** — equipping mid-combat (durability ticks, weapon breaks) creates cyclic dependency surfaced in Phase 2.
- **`SceneLoader`** — overlay states (EXP, Level Up) bypass scene loads (per memory: "SceneLoader gotcha for in-scene overlay states"). Don't "fix" this without re-reading that memory.
- **`HealingTileSystem` + `TurnEventChannel.PhaseStarted`** — moves between folders in Phase 1.1; verify subscription survives.
- **TextMeshPro asset modifications** — the current git status shows 11 modified TMP `.asset` files. Decide before Phase 1 whether to commit or revert these so they don't muddle every refactor diff.
