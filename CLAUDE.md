# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Project Astra** is a tactical RPG inspired by Indian mythology (Fire Emblem-style grid-based combat with dharma-driven narrative). Players command myth-inspired heroes in turn-based battles on 2D grid maps, with emphasis on strategic positioning via "vyuhas" (formations) and moral consequence mechanics.

- **Engine**: Unity 6 (6000.4.1f1)
- **Render Pipeline**: Universal Render Pipeline (2D)
- **Target Platforms**: PC (primary), Nintendo Switch (secondary)
- **Input**: New Input System (keyboard, gamepad, mouse with hot-swapping)

## Build & Test Commands

```bash
# Build from command line (adjust platform as needed)
Unity -batchmode -projectPath . -buildTarget Win64 -quit

# Run EditMode tests
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml

# Run PlayMode tests
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml

# Run a specific test class
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "ClassName"
```

Note: `Unity` refers to the Unity editor executable. On Windows this is typically at `"C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Unity.exe"`.

## Architecture

The project follows a **layered, state-machine-driven architecture** with strict dependency ordering. No ECS — traditional OOP with explicit state machines and data-driven design.

### System Layers (dependency flows downward only)

- **Layer 0 — Engine Foundation** (zero cross-dependencies): Game State Manager, Input Mapping, Tile Rendering, Pathfinding
- **Layer 1 — Grid & Spatial Core** (depends on Layer 0): Grid Cursor, Camera Tracking, Grid Movement, Terrain Bonuses, Terrain Passability

### Core Architectural Patterns

- **Top-level Game State Machine**: Only one state active at a time (TITLE_SCREEN, MAIN_MENU, BATTLE_MAP, COMBAT_ANIMATION, DIALOGUE, etc.). All transitions validated against an explicit transition table; illegal transitions are rejected and logged.
- **Battle Phase Sub-States**: PLAYER_PHASE -> ENEMY_PHASE -> ALLIED_PHASE (looping). Input is filtered per phase.
- **Data-Driven Mechanics**: Movement costs, terrain bonuses, and passability are defined in external data files (keyed by class/movement type x terrain type), not hardcoded.
- **Spatial Graph Pathfinding**: Dijkstra's algorithm on a static graph (rebuilt only on structural map changes like wall destruction). Manhattan distance for attack range. Tile cost applied at destination, not origin. Must complete within 1 frame for 32x32 maps with 50 units.
- **Input Abstraction**: Raw device inputs mapped to logical actions (CURSOR_UP, CONFIRM, CANCEL, etc.) with context-based suppression and DAS (Delayed Auto-Shift) for smooth cursor movement.
- **Pixel-Perfect 2D Rendering**: Orthographic camera with integer-snapped coordinates. 5 render layers: Ground -> Overlay -> Object -> Units -> UI/Cursor. 1-tile buffer for culling to prevent pop-in.
- **Camera**: Deadzone-based scrolling that tracks the cursor, with edge-break behavior at map boundaries.

### Key Packages

- 2D Tilemap + Extras (map construction)
- 2D Animation + Sprite + Aseprite/PSD importers (art pipeline)
- Timeline (cutscenes/sequencing)
- Test Framework (unit/integration testing)

## Design Reference

Detailed system specifications live in `Assets/Project Reference Files/`:
- `2 Page Game Description.pdf` — genre, core verbs, emotional design pillars
- `Ch1-Engine-Grid-Core.pdf` — Layer 0-1 system specs with edge cases
- `Production_Roadmap.pdf` — milestones and timeline

Always consult these documents before implementing a system to ensure alignment with the specification.

## UI Pipeline

> **`docs/UI_WORKFLOW.md` is outdated and must not be followed.** Its concept-phase → Figma → export → builder pipeline no longer reflects how UI is made here. The doc is kept only for the Unity-side gotchas it happens to record; treat its process as retired.

**Build UI that meets the requirements functionally, then stop.** Visual design and polish are the designer's, done by hand in Unity afterwards. So:

- **Do not** run a concept phase, produce mockups for a pick, invoke `frontend-design` for game UI, or touch Figma.
- **Do** make sure every surface a spec calls for actually exists and works. A missing surface is a real gap; an unstyled one is not.
- Wire real logic underneath — a View/Controller/Model trio per panel, per the Battle HUD pattern — so restyling only ever touches sprites, fonts and colours.
- Ask the user before inventing a screen that isn't in the spec.

Rules that still hold regardless:
- In-game UI is **uGUI** (Canvas + RectTransform + Image + TextMeshProUGUI). Never UI Toolkit for gameplay UI.
- Canvas reference resolution is **1920×1080**; gameplay renders at **480×270**, so sizes authored in gameplay pixels are multiplied by 4. `GurukulScreenSpace` holds that constant — use it rather than restating the factor per file.
- Dynamic text stays live as TMP; never bake it into a background PNG.
- A UI builder script regenerates its hierarchy on every run, so hand-styling belongs on a prefab or the saved scene, never anywhere a rebuild would overwrite it.

## Implementation Notes

- The input actions asset (`Assets/InputSystem_Actions.inputactions`) currently has default Unity template bindings — these need to be replaced with tactical RPG-specific actions (CURSOR_MOVE, CONFIRM, CANCEL, INFO, MENU, etc.) per the design spec.
- All gameplay values (movement costs, terrain bonuses, stat formulas) must go in ScriptableObjects or data files, never hardcoded in MonoBehaviours.
- State transitions must be validated — add guard checks that reject and log invalid transitions rather than silently allowing them.
- Comment code only where genuinely ambiguous or misinterpretable. Comments must be short (1 line max). Comment the "why" not the "what". Never comment obvious code.
