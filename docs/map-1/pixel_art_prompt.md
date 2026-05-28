# Map 1 — FE GBA Pixel-Art Prompt (for ChatGPT image generation)

*Goal: see the v2 "Bridge at Suvarnapur" layout rendered as a Fire Emblem GBA–style battle map. This is an exploratory look, not a production asset.*

## How to use
1. Open ChatGPT (a model with image generation).
2. **Attach the reference image:** `docs/map-1/map1_layout.png` (the schematic export of the v2 layout).
3. Paste the prompt block below.
4. Iterate — see the tips at the bottom.

> The reference PNG is a flat **schematic** (color-coded tiles + unit dots), *not* finished art. ChatGPT's job is to re-render that exact spatial layout as authentic FE-GBA pixel art.

---

## The prompt (copy everything in this block)

```
You are a pixel-art tile artist. Using the attached image as the exact spatial
reference, create a SINGLE top-down battle-map illustration in the authentic
pixel-art style of the Game Boy Advance Fire Emblem games (FE7 Blazing Sword /
FE8 Sacred Stones). Re-interpret the schematic — do not copy its flat colors;
turn each tile type into a proper FE-GBA tile, while keeping the same layout.

SETTING / THEME
An ancient Indian jungle village in the mythic past, at warm dusk. The village
is under a monster (rakshasa) raid, so a faint sense of smoke/threat is welcome,
but the map tiles must stay clean and readable. Warm, slightly mysterious mood.

READING THE REFERENCE (color -> FE-GBA tile):
- Beige/tan tiles        -> open GROUND: FE-GBA grassland / packed-earth plains,
                            warm green with earthen patches, walkable.
- Blue band (middle row) -> a RIVER/STREAM running straight across the whole
                            width: FE-GBA water tiles with subtle ripple lines.
                            Impassable water.
- Wood-striped tile in   -> a single wooden FOOTBRIDGE crossing the river at the
  the river center          horizontal center (planks).
- Dark green tiles       -> dense JUNGLE FOREST: rounded tropical tree canopies,
                            FE-GBA forest style. Frames the top and the left/right
                            edges, leaving an open clearing at top-center.
- Brown tiles w/ roof    -> village HUTS: ancient Indian thatched-roof huts (mud
                            walls, peaked straw roofs), FE-GBA "house/village"
                            tile style.
- Circular dots          -> these are UNIT START POSITIONS, not terrain. Render
                            the terrain normally underneath them. (See UNITS.)

EXACT LAYOUT TO PRESERVE (square map, 11 columns x 11 rows of tiles):
- Top edge, upper corners, and the left/right side columns: dense forest treeline
  framing the playable area, opening into a clearing at top-center.
- A river crosses the FULL width at the vertical middle (the 6th row from the
  top), split by ONE central wooden footbridge.
- North half (above the river): an open clearing/commons rising toward the forest
  mouth at the top.
- South half (below the river): the village — scattered thatched huts with open
  dirt lanes between them; the bottom rows are the residential area.
- Just south of the bridge, TWO huts flank a single open lane, so the bridge
  funnels into the village through that one gap.

ART STYLE (must match):
- Authentic GBA-era pixel art: ~16px logical tiles, limited/soft GBA palette,
  clean 1px outlines, light dithering for shading.
- Top-down map view with the classic FE slight-3/4 on trees and rooftops (you see
  canopies and peaked roofs from a gentle angle), ground read flat from above.
- Cohesive tileset where neighboring tiles connect (grass edges, riverbanks,
  forest borders) like a real FE map. Crisp pixels; nearest-neighbor upscale is
  fine; NO blur/anti-aliasing-heavy smoothing.

UNITS (optional but encouraged, FE-GBA sprite scale):
- Blue dot (south bank, east side): the heroine — a young female ARCHER in
  green/teal forest garb, bow in hand.
- Red dot (on the bridge / north of it): a RAKSHASA raider — a hulking horned
  demon-warrior, reddish skin.
- Gold-ringed dark-red dot (top-center, forest mouth): the rakshasa MINI-BOSS —
  larger, fiercer, a chieftain.
- Yellow dots (south, among the huts): frightened VILLAGERS — small civilian
  sprites.
If placing units is unreliable, render the clean terrain map alone.

OUTPUT — do NOT include:
- No grid lines, no row/column numbers, no legend, no labels, no text anywhere.
- No UI/HUD, health bars, or cursor.
- No modern HD-2D smooth lighting, no photorealism, no isometric/3D perspective.

Deliver one single image: the whole map, top-down, FE-GBA pixel art.
```

---

## Tips for iterating
- If it drifts toward modern/HD or 3D, re-emphasize **"Game Boy Advance Fire Emblem, 16-bit GBA pixel art, ~16px tiles, limited palette."**
- If the layout warps, tell it to **match the reference tile-for-tile**: river across the exact middle row, one central bridge, forest on top + sides, huts only in the lower half.
- If units clutter it, ask for a **terrain-only** version first, then a second pass that adds the four unit types.
- For a true low-res look, ask it to **render small (≈176×176) then upscale 4–6× with nearest-neighbor**, no smoothing.
- Want the dusk/raid mood stronger? Add: *"warm orange dusk light, long shadows, faint smoke rising from one hut."*

---

## Files
- Reference image: `docs/map-1/map1_layout.png`
- Source layout (interactive): `docs/map-1/layout_v2.html`
- Export helper (regenerates the PNG): `docs/map-1/_export_map.html`

*Note: the project's actual pixel-art production pipeline is PixelLab → Pixelorama → Unity. This ChatGPT route is just a quick visualization to feel the layout — not the asset we'd ship.*
