# Title Screen — Production Guide (PixelLab → Unity)

*The end-to-end playbook, written for the **PixelLab web app you use in your browser** (not the Claude/MCP interface). Follow it top to bottom: generate the pieces in PixelLab Pixelorama, assemble & clean them there too, then light and animate the scene in Unity. Every asset ID (S01, A01…) refers to Doc 02.*

> Read Doc 01 (why) and Doc 02 (what) first. Locked decisions: relic bow on a garlanded altar, symmetrical frame-in-frame, slight downward tilt, warm temple + "other" glowing bow, HD-2D pixel, **480×270 virtual canvas scaled ×4** to 1920×1080.
>
> **Note on tool names:** an earlier draft used PixelLab's *MCP* tool names (`create_map_object`, etc.). Those are the programmatic API names — **not** what you see in the browser. This version uses the **web-app names** (verified against pixellab.ai/docs), with the MCP equivalents only in the mapping table in §1 for reference.

---

## 0. Where you actually work

PixelLab ships several surfaces ([ways to use](https://www.pixellab.ai/docs/ways-to-use-pixellab)). You'll mainly use **one**:

- **PixelLab Pixelorama** — an in-browser editor (desktop browsers only) that is the open-source **Pixelorama** pixel-art editor **with PixelLab's AI generation tools built in**. This means you **generate the assets *and* assemble/clean them in the same app**. This is your home base.
- **Unity** — separate; where lighting, bloom, particles, and animation bring the scene to life.

So the pipeline is really just two stops: **PixelLab Pixelorama → Unity.** (Standalone Pixelorama or Aseprite would also work for the assembly step, but PixelLab Pixelorama keeps generation + editing together.)

> **PixelLab ≠ Pixelorama.** Pixelorama is the editor (layers, palette, drawing — no AI). PixelLab is the AI generator. "PixelLab Pixelorama" is PixelLab's build of Pixelorama that fuses the two. When this doc says "in PixelLab," it means: use the PixelLab AI panel inside that editor.

---

## 0.5 Principles that make this work

1. **One virtual resolution, one pixel size.** Compose the whole scene at **480×270**, then scale **×4** to 1920×1080 in Unity with **point (no) filtering**. Generate each asset at roughly the pixel footprint it occupies in that 480×270 layout (Doc 02 sizes), so when everything scales ×4 together, every pixel is the same size. **Uniform pixel size is the #1 thing that makes assembled assets look like one image, not a collage.**
2. **Light in Unity, not in the sprites.** Generate assets fairly evenly lit; add the warm pools, the bow's divine glow, bloom, and the shaft in-engine. This is how the reference games get their look, and it means you don't hand-paint lighting.
3. **Anything that moves is its own layer/file.** The static scene exports as one "background plate"; each animated element stays separate.
4. **Re-roll freely.** You have ~9,788 of 10,000 generations. Generate 3–4 candidates of anything important and keep the best.

---

## 1. The PixelLab web tools you'll use

All of these live in the AI panel of PixelLab Pixelorama (and in the simple web creator). Names and links are the real documentation names.

| What you want to make | Web-app tool (exact name) | Docs | Key limits |
|---|---|---|---|
| A single prop from text (the workhorse) | **Create medium-extra large image (pixflux)** | [link](https://www.pixellab.ai/docs/tools/create-image-flux) | up to **400×400** (Tier 2+); great text understanding |
| A prop **matched to an existing style** | **Create small-medium image with style support (bitforge)** | [link](https://www.pixellab.ai/docs/tools/style) | **max 140×140** (Tier 2+) — small assets only |
| A full background **scene** | **Create map (pixflux)** | [link](https://www.pixellab.ai/docs/tools/create-map) | up to 400×400; top-down or sidescroller |
| **Widen** a scene past 400px | **Extend Map** | [link](https://www.pixellab.ai/docs/tools/map-tiles) | outpaints a selected area |
| Floor / wall tiles | **Create tiles (Pro)** | [link](https://www.pixellab.ai/docs/tools/create-tiles-pro) | tile size 16–128; ~20–25 generations |
| Repeating terrain w/ transitions | **Create tileset** | [link](https://www.pixellab.ai/docs/tools/create-tileset) | Wang / dual-grid / 3×3 |
| Animate an object (frames) | **Animate with text** | [link](https://www.pixellab.ai/docs/tools/animation) | **exactly 64×64, 4 frames** — limited |
| 8 rotations of an object | **Rotate** / **Create 8-directional sprite (Pro)** | [link](https://www.pixellab.ai/docs/tools/rotate) | not needed for this static screen |

**Mapping back to the MCP names** (only matters if you ever ask me to drive it from Claude Code):

- Create medium-extra large image (pixflux) ≈ `create_map_object` (basic) / `create_1_direction_object`
- Create small-medium image with style support (bitforge) ≈ `create_1_direction_object` with `style_images`
- Create map (pixflux) ≈ `create_map_object`
- Create tiles (Pro) ≈ `create_tiles_pro`; Create tileset ≈ `create_topdown_tileset` / `create_sidescroller_tileset`
- Animate with text ≈ `animate_object`; Create 8-directional sprite ≈ `create_8_direction_object`

So: **"create_map_object" was real, not hallucinated — its browser name is "Create medium-extra large image (pixflux)" (for props) / "Create map (pixflux)" (for scenes).**

### The settings every generation tool shows (set these the same across all assets)

Both pixflux and bitforge expose the same controls. These are your per-asset configuration:

- **Description** — the prompt (see §2 for each).
- **Negative description** — what to avoid (e.g. "blurry, modern, photorealistic, text").
- **Camera view** — `side` for upright objects (bow, pillar, altar, diya, incense); `high top-down` for things lying flat (floor, thali, petals). Match the scene's slight downward tilt.
- **Outline** — pick **selective outline** and use it on every asset (consistency).
- **Shading** — **detailed** for hero/large pieces, **medium** for small props.
- **Detail** — **high** for the bow/altar, **medium** for small props.
- **Target palette** — your master palette (`astra_temple_palette.gpl`), applied via **Limit colors used → Color palette**. The single most important *color*-consistency lever; select it on every generation (see §1.5).
- **Init image** — a guidance image. In **bitforge** this doubles as the **style reference**.
- **Background removal toggle** — **ON** for every standalone prop (clean transparent cutout); **OFF** for the background scene.
- **Seed** — set a fixed number when you want to reproduce/iterate a result.
- **Canvas size** — set per Doc 02 (remember the 400 / 140 / 64 caps above).

---

## 1.5 Keeping a consistent style across separate generations

There are **two separate levers here, and they're easy to confuse.** Keep them apart:

**A) Color consistency = one *designed* master palette (NOT the altar's colors).**
The scene shares **one deliberately-designed master palette** that covers every material and the warm mood — *not* a palette scraped from whatever the AI gave the altar. A "shared palette" does **not** mean every object is the same color; it means **every color on every object is drawn from the same harmonious set.** Each asset uses the *ramp* that fits it: bow → gold ramp, walls/floor/pillar → stone ramp, flames → fire ramp, garland → marigold ramp, dark framing → shadow ramp.

- Use the ready-made palette in **`docs/title-screen/astra_temple_palette.gpl`** — import it into Pixelorama. **Design/lock the palette FIRST**, then make every asset (the altar included) conform to it.
- Enforce it on generation: set **Limit colors used → Color palette** (or **Force colors**) with that master set as the **Target Palette**, on every pixflux and bitforge call ([color options](https://www.pixellab.ai/docs/options/color)).
- Safety net: run **Reduce colors** (or a Pixelorama remap, §4.3) to snap any drifting asset to the master palette afterward. You can also generate a little freer and unify after the fact.

**B) Style consistency = the rendering *hand* (detail, outline, shading).**
This is what the **altar (S05)** anchors and what **bitforge** carries via its **Init image** — *not* the palette. For anything **≤140×140** (bow, garland, diya, thali, incense, petals) use bitforge with the altar as the Init image so the outline/shading/detail match. For anything **>140px** (altar, pillar, background, floor) bitforge can't fit it — use **pixflux** (the master palette still keeps its colors aligned).

> **Rule of thumb: ≤140px → bitforge (style-matched). >140px → pixflux. The master palette goes on *everything*.**

**Per-object generation palettes.** To stop the AI embedding foreign colors (gold in the stone, stone in the bow), `docs/title-screen/palettes/` holds a focused palette per object — each is its own material ramp(s) plus the 3 shared neutrals (warm-white highlight, deep-indigo shadow, outline). **Load the object's palette before you generate it** (File ▸ Open the `_palette.png` → Import as → New palette → select it → Limit colors used → Color palette), then switch when you move to the next object:

| Palette (`palettes/…`) | Load when generating | Contains |
|---|---|---|
| `stone_palette.png` | altar (S05), pillar (S04), wall/niche (S01), floor (S03) | stone ramp + neutrals |
| `bow_palette.png` | relic bow (S06) | gold ramp + divine highlight + neutrals |
| `diya_palette.png` | diya lamp + flame (S08) | brass + fire + neutrals |
| `garland_palette.png` | garland (S07), petals (S11) | marigold + leaf + neutrals |
| `thali_palette.png` | pooja thali (S09) | brass + marigold + fire + rice-white + neutrals |
| `incense_palette.png` | incense holder (S10) | brass + ember + neutrals |

Use the **full master palette** (`astra_temple_palette.gpl`) only for the final **Pixelorama unify** step (§4.3), where the assembled scene wants every color available.

---

## 2. Per-asset recipes

For each: the web tool, the settings, an example prompt, size, and notes. Prompts are starting points — re-roll freely.

### S05 — Altar / weapon-stand *(make this FIRST = the STYLE anchor)*
- **Tool:** Create medium-extra large image (pixflux). **View** side · **outline** selective · **shading** detailed · **detail** high · **bg removal** ON · **size** ~150×95 · **Target palette** = `astra_temple_palette.gpl` (already imported).
- **Prompt:** `carved stone temple altar and weapon stand, weathered sandstone, ancient Indian motifs, flat top surface, evenly lit`
- **Notes:** Generate 3–4; keep the cleanest silhouette with a readable top surface. This is your **style** anchor — the rendering hand bitforge will copy onto the small assets — **not** the palette source (the palette is the separate master set). If its stone tones come out beautifully, you may refine the palette's *stone ramp* from them, but the gold/fire/marigold/shadow ramps come from the master palette, not the altar.

### S06 — The relic bow (HERO)
- **Tool:** Create small-medium image with style support (bitforge). **Init image** = the altar (S05) for style transfer · **view** side · **outline** selective · **shading** detailed · **detail** high · **Target palette** = anchor · **bg removal** ON · **size** ~130×140 (within the 140 cap).
- **Prompt:** `ornate ancient Indian recurved war bow (dhanush), polished golden limbs with carved engraving, wrapped leather grip, taut bowstring, standing upright, evenly lit`
- **Notes:** This is the star — spend re-rolls here. Keep it neutral/even; the divine glow is added in Unity (A03). If you want it grander than 140px, use **pixflux** at a larger size with the shared palette instead (style match is then via palette + Pixelorama cleanup).

### S04 — Foreground pillar
- **Tool:** Create medium-extra large image (pixflux) *(>140px → pixflux)*. View side · outline selective · shading detailed · detail high · palette = anchor · bg removal ON · size ~60×270.
- **Prompt:** `tall carved stone temple pillar, ancient Indian Nagara architecture, weathered sandstone, ornamental capital, evenly lit`
- **Notes:** Generate one; in Pixelorama duplicate and **flip horizontally** for the right side. Sits close to camera and gets darkened in Unity, so silhouette > fine detail.

### S01 — Sanctum back wall + niche/arch
- **Tool:** Create map (pixflux), **view sidescroller**, size up to 400 wide, then **Extend Map** to reach the full ~480 width. bg removal OFF (it's the backdrop).
- **Prompt:** `ancient Indian temple inner sanctum, weathered carved sandstone wall with a pointed-arch niche in the center, warm torchlit, sidescroller view`
- **Notes:** Generate the central section first (with the niche where the bow will sit), then Extend Map left and right for width. Alternatively make a repeating wall with Create tiles (Pro) and place the niche as a separate pixflux object.

### S03 — Temple floor
- **Tool:** Create tiles (Pro) — **tile type** square (top-down), **view angle** high top-down, **tile size** 32. *(Or a pixflux piece, view high top-down.)*
- **Prompt:** `weathered sandstone temple floor flagstones`
- **Notes:** Tile across the bottom of the canvas; high top-down so it reads as receding under the tilt.

### S07 — Marigold garland
- **Tool:** bitforge (≤140) with the anchor as Init image. View side · detail medium · palette = anchor · bg removal ON · size ~120×40.
- **Prompt:** `marigold flower garland (genda phool mala), orange and yellow blossoms on a string, draped, ancient Indian, evenly lit`

### S08 — Diya oil lamp (with flame)
- **Tool:** bitforge (≤140) with anchor Init image. View side · detail medium · palette = anchor · bg removal ON · size ~20×16.
- **Prompt:** `small brass oil lamp (diya) with a lit flame, ancient Indian, evenly lit`
- **Notes:** Make one good lamp; place 3–5 copies. This is the asset you may animate (A01).

### S09 — Pooja thali
- **Tool:** bitforge (≤140) with anchor Init image. **View high top-down** (it lies flat) · detail medium · palette = anchor · bg removal ON · size ~34×22.
- **Prompt:** `brass pooja thali plate of offerings seen from above, kumkum, rice, flower petals, a tiny lit lamp, ancient Indian`

### S10 — Incense holder
- **Tool:** bitforge (≤140) with anchor Init image. View side · detail medium · palette = anchor · bg removal ON · size ~16×20.
- **Prompt:** `brass incense holder with thin incense sticks (agarbatti), ancient Indian, evenly lit`
- **Notes:** No smoke baked in — smoke is a Unity particle (A02).

### S11 — Petals / small offerings (optional)
- **Tool:** bitforge or pixflux. View high top-down · tiny.
- **Prompt:** `scattered marigold flower petals, ancient Indian temple offering, seen from above`

---

## 3. Animation in PixelLab (and what to do in Unity instead)

Only **A01 (diya flame)** is a candidate for PixelLab animation; everything else (A02–A05) is done in Unity (§5), which is easier and more organic.

### A01 — Diya flame flicker
- **The constraint:** **Animate with text** ([docs](https://www.pixellab.ai/docs/tools/animation)) outputs **4 frames** on a **fixed 64×64** canvas, conditioned on your lamp image. 4 frames is choppy for fire.
- **Recommended:** do the flame flicker in **Unity** instead — a small additive flame glow whose intensity/scale flickers via a `Light2D` + a short script (and optionally a 2–3 frame sprite swap). Avoids the 64×64/4-frame limit entirely and looks better with bloom.
- **If you want true sprite frames:** use the **Pro** animation tool (Create animated object/character (Pro) / Animate with text (Pro)) for more frames, then export the spritesheet and loop it in Unity. Generate the flame as its own tiny object so only the flame moves (the tool animates the whole object).

> One flicker source can drive every diya — just offset each copy's playback start in Unity so they don't pulse in unison.

---

## 4. Assemble & clean in Pixelorama

Use the Pixelorama editing toolset (it's right there in PixelLab Pixelorama; standalone Pixelorama works too). This is where loose generations become one coherent 480×270 image.

### 4.1 Project setup
- **New project, 480×270**, transparent background.
- Optional guide layer: sketch the composition boxes (pillars at the edges, altar center-bottom, bow center, wall behind) so placement stays consistent.

### 4.2 Import & arrange
- Bring each generated asset onto **its own layer** (if you generated it in this app it's already a layer; otherwise `File → Import → as new layer`). Name layers by asset ID.
- Arrange per Doc 02's **layer order** (wall → floor → altar → offerings → bow+garland → diyas → pillars on top).
- **Scale only by whole-number factors** (×1, ×2…) so pixels stay square — never fractional. If a generation came out the wrong size, regenerate at the right size rather than fractionally scaling.
- Mirror the pillar (S04): duplicate the layer → flip horizontally → move to the right edge.

### 4.3 Palette unification (the cohesion step — don't skip)
- Open the saved **anchor palette**. For each asset layer, remap stray/off-palette colors to it (select-by-color → replace, or reduce the layer's colors and hand-map).
- Keep the **bow's** golds a touch brighter/cleaner than the room — it's the "other" object.

### 4.4 Edge & artifact cleanup
- Erase orphan/stray pixels and any leftover background fringe around cutouts.
- Harden soft/semi-transparent edge pixels so cutouts read crisp; keep the selective outline consistent across assets.
- Tidy where assets meet (bow on altar, garland on altar) so they sit *in* the scene, not pasted on.

### 4.5 Export
- **Background plate:** hide the layers that will move/animate (diyas/flames, anything driven in Unity), then export the merged static scene as `titlescreen_bg.png` (480×270).
- **Animated elements:** export each as its own transparent PNG / spritesheet, at the same pixel scale.
- Everything stays at ×1 (480×270 space). Unity does the ×4.

---

## 5. Bring it to life in Unity (lighting + animation)

This is where the warmth and the duality actually appear.

### 5.1 Import settings (every PNG)
- **Texture Type:** Sprite (2D and UI) · **Filter Mode:** **Point (no filter)** ← critical · **Compression:** None · **Pixels Per Unit:** one consistent value for all · **Sprite Mode:** Single (Multiple for any flame spritesheet → slice frames).

### 5.2 Scene & camera (pixel-perfect)
- 2D URP scene with a **2D Renderer** assigned in the URP settings (required for `Light2D` + 2D post-processing).
- Orthographic camera + **Pixel Perfect Camera** component: Reference Resolution **480×270**, PPU matching your sprites, Upscale Render Texture on — guarantees clean ×4 scaling to 1080p.
- Place the background plate at origin; place animated/lit elements on sprite layers in front, matched to the plate.
- **Sorting Layers:** Background → Mid → Bow → Foreground/Pillars → FX.

### 5.3 The atmosphere (the part that sells it)
- **Material:** set lit sprites to **Sprite-Lit-Default** so they receive 2D light (bow, altar, floor). Keep the dark foreground pillars unlit/darkened.
- **2D lights** (`GameObject → Light → 2D`):
  - **Global Light 2D** — low warm ambient (amber) so nothing is pure black.
  - Warm **Spot/Point Light 2D** over the altar — the pooled lamp light (orange-gold) = the "warm, safe temple" feeling.
  - A separate light on the **bow**, a *slightly purer/cooler gold* than the lamps. That subtle hue gap is what makes it read **divine / other** (Doc 01's rule).
- **Post-processing (Global Volume; camera Post Processing on):**
  - **Bloom** — tuned so the bow's golds and the flames *bleed* light. The heart of the HD-2D look.
  - **Vignette** — darken the edges, push the eye to the relic.
  - Optional **Color Adjustments** — nudge the whole frame warm.

### 5.4 The breathing & moving elements
- **A03 — Bow glow pulse:** put the bow's `Light2D` (and/or an additive glow sprite) on a tiny script driving intensity/alpha with a slow sine wave (period ~3–4s, ±15%). Diyas *flicker*; the bow *breathes*.
- **A01 — Diya flames:** in Unity, flicker the flame's `Light2D` intensity + tiny scale jitter (and/or a short sprite loop). Stagger per lamp.
- **A02 — Incense smoke:** a **Particle System** — soft pixel puff, low alpha, slow upward drift, scale-up + fade over ~3–5s, very low emission. Far more natural than a baked loop.
- **A04 — Rising motes/embers:** a **Particle System** — tiny additive dots, slight flicker, slow upward drift with horizontal noise. The indoor "fireflies."
- **A05 — Light shaft:** a soft additive sprite (pale wedge) angled onto the bow, or a `Light2D` cookie; animate opacity/position a few percent for a living shimmer. Motes drifting through it sell it.

### 5.5 Custom shaders — usually unnecessary
URP 2D Lights + Bloom cover everything above. Only reach for a shader for a cheap additive/emissive glow without a real light, or a scrolling-noise smoke instead of particles. Start without them.

### 5.6 The "press any key" prompt
The one element that follows `UI_WORKFLOW.md` (HD UI text, not pixel art): a `TextMeshProUGUI` on a Canvas, faint, wide letter-spacing, slow opacity pulse. Screen Space – Camera if you want bloom to touch it.

---

## 6. Cleaning checklist (per asset, before it joins the plate)

- [ ] Correct camera view (upright = side, flat = high top-down)
- [ ] Background removal was ON; clean transparent cutout, no fringe
- [ ] Scaled by a whole-number factor only; pixel size matches the scene
- [ ] Remapped to the shared anchor palette
- [ ] Consistent selective outline with the set
- [ ] No orphan pixels
- [ ] Contact with neighbors tidied (sits *in* the scene)
- [ ] Animated elements kept on separate layers/files

---

## 7. Start-to-finish order of operations

1. Open **PixelLab Pixelorama** in your browser.
2. **Import the master palette** (`astra_temple_palette.gpl`) into Pixelorama and set it as the Target Palette (Limit colors used → Color palette). Then generate the **altar S05** with *Create medium-extra large image (pixflux)*, palette on. Re-roll until you love it — this is your **style** anchor.
3. Generate the **hero bow S06** with *Create small-medium image with style support (bitforge)*, altar as Init image, anchor palette. Review candidates; keep the best.
4. Generate the rest with the **≤140 → bitforge / >140 → pixflux** rule, anchor palette on every one: **S04 pillar, S01 background (+Extend Map), S03 floor (tiles), S07 garland, S08 diya, S09 thali, S10 incense** (S02/S11 optional).
5. *(Optional)* flame frames via the Pro animation tool — or skip and do flame flicker in Unity.
6. In **Pixelorama**: assemble at 480×270, scale to footprints, **unify palette**, clean edges, export `titlescreen_bg.png` + separate animated/element PNGs.
7. In **Unity**: import (Point filter), set up the **Pixel Perfect Camera** at 480×270, place the plate + element layers.
8. Add **2D lights** (warm ambient + altar pool + cooler-gold bow light), **Bloom**, **Vignette**.
9. Wire the **bow glow pulse**, **diya flicker**, and the **smoke / motes / shaft** particles.
10. Add the **"press any key"** TMP prompt.
11. Tune by eye until the room feels warm and safe and the bow feels *other* — then capture a frame and check it against the duality goal in Doc 01.

---

## Sources (PixelLab docs)

- [Ways to use PixelLab](https://www.pixellab.ai/docs/ways-to-use-pixellab) · [Docs home](https://www.pixellab.ai/docs)
- [Create medium-extra large image (pixflux)](https://www.pixellab.ai/docs/tools/create-image-flux) · [Create small-medium image with style support (bitforge)](https://www.pixellab.ai/docs/tools/style)
- [Create map (pixflux)](https://www.pixellab.ai/docs/tools/create-map) · [Extend Map](https://www.pixellab.ai/docs/tools/map-tiles)
- [Create tiles (Pro)](https://www.pixellab.ai/docs/tools/create-tiles-pro) · [Create tileset](https://www.pixellab.ai/docs/tools/create-tileset)
- [Animate with text](https://www.pixellab.ai/docs/tools/animation) · [Rotate](https://www.pixellab.ai/docs/tools/rotate)

*Tier note: you're Tier 3 — max canvas 400×400 (pixflux/map), 140×140 (bitforge style), 64×64 (Animate with text). ~9,788 generations remaining; props ≈ cheap, Pro/tile tools ≈ 20–25 each.*
