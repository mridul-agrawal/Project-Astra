# Map Data Layer & Editor — Redesign

**Status:** ✅ Implemented 2026-07-12 on branch `map-data-redesign` (off `refactoring`). Built in 7 verified phases; 513 EditMode tests green; Map1 renders + spawns units live in play mode. Real hand-painted art drops in via the Map Editor's "Import PNG" (currently a generated 32px placeholder).
**Owner:** Mridul

## What shipped (vs. the target below)

- `MapData` is now `{ mapName, mapId (string), baseArt (Sprite), width/height, terrain[] (TerrainType per cell), unitStartPositions[], objects[] (MapObject scaffold) }`. `TerrainAt(x,y)` is the terrain source; `MapRenderer.GetTerrainType` reads it — pathfinding/combat/healing/HUD unchanged.
- `MapId` enum **deleted** → data-driven string id on `MapData`; `MapCatalog`/`CampaignStep` key by string; `CampaignStep` drawer shows a map-id dropdown. Adding a map needs **no code**.
- `MapRenderer` draws the base PNG on the Ground sorting layer + object sprites; `MapCamera` = 32 PPU / 480×270. `SwapTile`/`OnTileSwapped` → `OnCellChanged` (destructible scaffold).
- **Deleted:** `TilesetDefinition`/`TileEntry`, `SyncedAnimatedTile`, `MapLayer`, `MapId`, the tileset editor + placeholder/test-map generators, `TilesetDefinitionTests`, and orphaned tileset/tile/test-map assets. `Unity.2D.Tilemap.Extras` dropped from both asmdefs.
- **New:** `Assets/Scripts/Core/Editor/MapEditorWindow.cs` (menu: Project Astra/Map/Map Editor). Map1 migrated losslessly (terrain parity 165/165).
- **Object-layer behaviors** (tree→bridge passability, chest open/close, wall→rubble) are scaffolded only (data + render + `OnCellChanged`); wiring them up is the next increment.

---

## (Original design, retained for reference)
**Related code:** `Assets/Scripts/Core/Grid/`, `Assets/Scripts/Core/Units/UnitSpawner.cs`, `Assets/Scripts/Core/Editor/` (map tools), `Assets/Scripts/Core/Flow/` (campaign hookup)

---

## 1. TL;DR

The map data layer is currently built for a **reusable-tile-palette** workflow (stamp ~50 generic tiles across many maps). This game's art direction is the opposite: **bespoke, hand-painted seamless maps** (Sea of Stars style). For that direction the tile-ID / tileset / palette machinery is dead weight.

We are **simplifying** the data layer: art becomes one seamless painted PNG per map, decoupled from the gameplay grid, which becomes pure per-cell terrain data painted graphically in a new designer tool. Interactive/destructible objects that change appearance mid-game stay on separate object layers above the base art. The end state: a designer imports a PNG, paints terrain, drops units/objects, saves a `MapData`, references it from the Campaign SO, and the map just plays — with no knowledge of how the code works.

---

## 2. Current architecture (as-is, for reference)

### The end-to-end chain

```
Campaign (SO)  →  CampaignStep {Battle, MapId}
      │  GameFlow.CurrentMap
      ▼
MapCatalog (SO)  MapId → MapData (SO)
      │
      ▼
MapBootstrapper (in BattleMap scene, [DefaultExecutionOrder(-100)])
      ├──► MapRenderer.LoadMap(map)      stamps tile IDs → sprites onto 5 Unity Tilemaps
      └──► UnitSpawner.SpawnUnits(map)   resolves unitId → UnitDatabase → spawns units
```

### Data layer (ScriptableObjects + enums)

| Asset | Holds | Notes |
|---|---|---|
| `MapData` | name, `MapId`, width×height, `tilesets[]`, `layers[]`, `unitStartPositions[]`, `eventTriggers[]` | Pure data. Layers store a flat `int[] tileIds`, row-major `index = y*width + x`, `-1` = empty. |
| `TilesetDefinition` | name, source texture, `TileEntry[]` | **Tile ID = array index.** A reusable palette. Maps can hold several; a layer picks one via `tilesetIndex`. |
| `TileEntry` (struct) | `tileAsset` (TileBase) + `terrainType` | Terrain rides on the tile — swapping tilesets changes visuals AND gameplay in lockstep. |
| `TerrainType` (enum) | Plain, Forest, Wall, Fort, Throne… (19) | Drives movement/combat/heal. Append-only. |
| `MapLayer` (enum) | Ground(0), Overlay(1), Object(2), Units(3), UI(4) | Render order bottom→top. Units draw ABOVE all map layers (effectively top-down; no unit-behind-object occlusion). Append-only. |
| `MapId` (enum) | `None`, `Map1_BridgeAtSuvarnapur` | Stable per-map handle. **Adding a map needs a code edit + recompile.** |
| `MapCatalog` | `List<MapData>` → `MapId→MapData` dict | The registry GameFlow queries. |
| `TerrainStatTable` | `TerrainType` → move cost / defense / avoid / heal | Read by pathfinding & combat, separate from rendering. |

### Art path (today)

`PNG sprite sheet → import Sprite/Multiple, Point, PPU=tilepx → slice → Unity Tile assets → TilesetDefinition.tiles[] (Tile + TerrainType)`. Tile ID = index.

### Runtime

- `MapRenderer.LoadMap`: clears tilemaps, per layer per cell reads ID → resolves tileset by `layer.tilesetIndex` → `tilemap.SetTile`. `-1` skips; bad/missing ID → magenta error tile + log; warns on ground holes.
- `MapRenderer.GetTerrainType(x,y)`: reads the **Ground** tile's ID → tileset → `TerrainType`. The single door pathfinding/combat use.
- `MapRenderer.SwapTile`: runtime tile swap for destructibles (wall→rubble); updates tilemap + MapData, fires `OnTileSwapped` so pathfinding rebuilds.
- `UnitSpawner.SpawnUnits`: per `unitStartPosition`, resolve `unitId` (string) via `UnitDatabase` → build unit, faction from `team` (0 Player / 1 Enemy / 2 Allied), seed inventory, attach map sprite, snap to grid.

### Existing files (inventory for migration)

**Runtime/data:** `MapData.cs`, `TilesetDefinition.cs`, `MapRenderer.cs`, `MapLayer.cs`, `TerrainType.cs`, `MapId.cs`, `MapCatalog.cs`, `TerrainStatTable.cs`, `MapBootstrapper.cs`, `OverlaySpriteFactory.cs`, `SyncedAnimatedTile.cs`, `Units/UnitSpawner.cs`.
**Editor tools:** `TestMapGenerator.cs`, `TilesetDefinitionEditor.cs`, `PlaceholderTilesetGenerator.cs`, `MapGridSetup.cs`, `TerrainStatTableGenerator.cs`.
**Tests:** `Core.Tests/Grid/MapDataTests.cs`, `TilesetDefinitionTests.cs`, `TerrainStatTableTests.cs`.
**Assets:** `Assets/ScriptableObjects/Map/` (MapData assets, `Tilesets/`, `MapCatalog.asset`, `TerrainStatTable.asset`).
**Flow:** `Flow/GameFlow.cs`, `Campaign.cs`, `CampaignStep.cs`.

### How a designer adds a map today (the pain)

1. Import + slice a sprite sheet by hand.
2. Create a `TilesetDefinition`, auto-populate, then **hand-set the terrain on every tile** (auto-populate defaults all to Plain).
3. Create a `MapData` — and **add a new `MapId` enum member in code + recompile**.
4. **Author the layout as a flat `int[]`** — in practice a throwaway C# script of hand-typed integer arrays (`TestMapGenerator` is exactly this). A programmer task.
5. Type `unitStartPositions` as raw `Vector2Int` + magic `unitId` strings.
6. Register the map in `MapCatalog`; add a `CampaignStep`.

Nothing is visual; nothing is validated until Play.

---

## 3. Why we're changing it

**Paradigm mismatch.** The current model optimizes for reusing a small tile vocabulary across many maps. This game wants unique, seamless, painted battlefields. Under bespoke art, every cell is a one-off — the palette collapses into ~400 IDs each used once, and all the tile-ID/tileset indirection becomes noise that the designer must wade through. The instinct that it is "over-engineered" is correct **for this art direction.**

---

## 4. Target design

### 4.1 Decouple art from the grid

- **Art** = one seamless painted PNG per map (the base/ground layer). The artist paints freely; 32×32 is only the *gameplay grid resolution*, not an art constraint.
- **Grid** = pure gameplay data: a `TerrainType` per cell, authored graphically. No tile IDs, no tilesets, no palettes.

### 4.2 Simplified `MapData` (target shape)

```
MapData
  ├─ mapName
  ├─ id                     (see §6 open question on MapId)
  ├─ mapArtSprite / PNG     ← the seamless base image
  ├─ width, height          ← auto-derived from PNG ÷ 32 (validated)
  ├─ terrain[]              ← TerrainType per cell, flat row-major
  ├─ unitStartPositions[]   ← position, unitId, team, optional loadout
  └─ objects[] / events[]   ← interactive/destructible object placements (see 4.3)
```

`TilesetDefinition` and `TileEntry` are **deleted**.

### 4.3 Layered interactive/destructible objects (kept)

The 5-layer idea is retained ONLY for things that **change appearance mid-game**:

- Base layer = the seamless painted PNG.
- Object layer(s) above it hold discrete, placed sprites for things that mutate at runtime — e.g. a **tree that gets cut into a bridge** (and becomes passable), a **chest** with closed/open art, a **wall → rubble**.
- These are individual sprites/objects on a cell, not baked into the base PNG, precisely because they need to swap sprite + change passability/terrain at runtime. This preserves the useful part of `SwapTile`/`OnTileSwapped` without the tile-ID machinery.

### 4.4 Rendering

- Base PNG renders below units (matches current sorting: units already draw above all map layers, so a flat background fits with no occlusion problem).
- Object layer sprites render above the base, below units (like the current Object layer).
- `MapRenderer` keeps `LoadMap` / `GetTerrainType` signatures. `GetTerrainType` reads `terrain[y*w+x]` directly instead of deriving via a tile. **Pathfinding & combat are unaffected.**

### 4.5 Designer tool (the deliverable)

ONE editor tool that:
1. Imports a PNG → configures import (PPU 32, Point, Multiple/or single, no compression) → auto-derives map size, rejects non-multiple-of-32 dimensions.
2. Shows the art with a **grid overlay**; designer paints `TerrainType` per cell with a terrain-color overlay drawn semi-transparent on top of the art (so art↔terrain mismatches are obvious).
3. Lets the designer place units (dropdown of real `UnitDatabase` ids, colored by team — no magic strings) and interactive objects.
4. Validates (holes, off-map/overlapping units, unresolved ids, size mismatch) live.
5. Saves a `MapData` asset and one-click registers it into `MapCatalog` (+ optionally appends a `CampaignStep`).

Goal: designer never sees an index or writes code; the produced `MapData` plugs into the Campaign SO and plays.

---

## 5. Decisions resolved this session

1. Pivot to bespoke seamless painted-PNG maps; retire the reusable-tile-palette model.
2. Decouple art (PNG) from grid (terrain data).
3. Auto-derive map size from PNG.
4. Terrain painted graphically per cell; designer sees no tile IDs.
5. Keep layers ONLY for interactive/destructible objects that change appearance mid-game.
6. `TilesetDefinition` + `TileEntry` deleted; `MapData` slimmed.
7. Contained runtime migration — keep `MapRenderer` public surface; pathfinding/combat untouched.
8. Tool must overlay terrain on art to prevent art↔terrain desync.

---

## 6. Open questions (resolve during planning)

- **Destructible/interactive terrain scope** — confirmed WANTED (tree→bridge, chest open/closed, wall→rubble). Object layer is in scope.
- **`MapId`: enum vs data.** Keep the enum (auto-append via tool) or replace with a string/GUID id on `MapData` (cleaner, but touches `MapCatalog`, `CampaignStep`, any save-data). To decide in the plan.
- **Render the base as one big sprite vs auto-sliced onto the tilemap.** One sprite is simplest; slicing keeps the Tilemap path. Decide in the plan (leaning one-sprite for the base).
- **Fate of `TerrainStatTable`** — unaffected in principle (still `TerrainType → stats`); confirm it stays as-is.
- **Existing `Map1_BridgeAtSuvarnapur`** — must be migrated to the new format (or re-authored) without losing the map. Non-negotiable: no data loss.

---

## 7. Migration risks & guardrails (do not lose anything)

This is a large change that can break the running game in many ways. The execution plan MUST:

- Interleave **validation/testing checkpoints throughout**, not only at the end (compile checks, EditMode tests, in-editor deserialization checks, play-mode smoke test of Map1).
- Preserve or migrate **all existing map data** — especially `Map1_BridgeAtSuvarnapur` and its unit placements — before deleting the old format.
- Keep the game **playable at every checkpoint** (or on a branch that can be reverted), per the per-folder branch + per-file refactor workflow.
- Track every script slated for **deletion** and confirm no live references remain first.
- End with a full verification pass: Map1 loads, renders, spawns units, is playable, and existing tests pass.
