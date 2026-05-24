# Title Screen — Asset List

*Every still and animation needed to build the title screen, with core details. This is the inventory; Doc 03 gives the step-by-step recipe for each. Sizes are in the **480×270 virtual canvas** (everything integer-scales ×4 to 1920×1080 in Unity).*

**Tool names are the PixelLab web-app names** (verified — see Doc 03 §1), used inside **PixelLab Pixelorama** (the in-browser Pixelorama editor with PixelLab's AI tools). The consistency rule that drives the "Source tool" column:

> **≤140px → "Create small-medium image with style support (bitforge)"** (style-matched to the anchor). **>140px → "Create medium-extra large image (pixflux)"** + shared palette (bitforge is capped at 140×140 on Tier 3).

**Composition reminder:** symmetrical, slight downward tilt, frame-in-frame. Two foreground pillars frame a center where the relic bow stands on a garlanded altar; diyas, a pooja thali and incense sit on/around it; a carved sanctum wall sits behind.

---

## Layer order (back → front)

1. Sanctum back wall + carved niche/arch *(S01)*
2. Temple floor *(S03)*
3. Altar / weapon-stand *(S05)*
4. Offerings on/around the altar — pooja thali *(S09)*, incense holder *(S10)*, petals *(S11)*
5. The relic bow *(S06)* + garland *(S07)*
6. Diyas *(S08)* (some in front of the altar)
7. Foreground pillars, left + right *(S04)* — closest to camera, in shadow
8. *(Unity overlays: glow, light shaft, smoke, motes, vignette, "press any key")*

---

## Stills (generated art)

| ID | Asset | Role | Approx size (in 480×270) | Web-app tool + view | Notes |
|----|-------|------|--------------------------|---------------------|-------|
| **S05** | Altar / weapon-stand | The bow rests on this — **make FIRST (the STYLE anchor)** | ~150×95 | Create M-XL image (pixflux), **side** | Carved stone, ancient-Indian motifs, top surface visible. It's the *style* anchor (rendering hand); the colors come from the separate master palette (`astra_temple_palette.gpl`), not from this asset. |
| **S06** | **The relic bow (HERO)** | The subject of the whole screen | ~130×140 | Create S-M image w/ style (**bitforge**), **side**, altar as style ref | Ornate recurved dhanush, golden limbs, leather grip, strung, upright. If you want it >140px, use pixflux + palette. |
| **S04** | Foreground pillar | Frame-in-frame, left + right | ~60×270 (one asset, **mirror** for the other side) | Create M-XL image (pixflux), **side** | >140 → pixflux. Sits in shadow close to camera. |
| **S01** | Sanctum back wall + niche/arch | Backdrop behind the altar | ~480×270 (generate to 400, then **Extend Map**) | Create map (pixflux), **sidescroller** + Extend Map | Warm torchlit stone; a niche/arch centered behind the bow. bg-removal OFF. |
| **S02** | Upper arch / ceiling trim (optional) | Top of the frame-in-frame | ~480×60 | Create map / pixflux, side | Can be folded into S01. |
| **S03** | Temple floor | Ground plane (seen due to down-tilt) | ~480×90, tileable | Create tiles (Pro), square + **high top-down** (or pixflux) | Weathered flagstones; viewed slightly from above. |
| **S07** | Marigold garland | Draped on altar/bow (devotional warmth) | ~120×40 | bitforge (style), **side** | Genda-phool mala, orange/yellow. |
| **S08** | Diya oil lamp (with flame) | Warm light source; **animated** | ~20×16 each | bitforge (style), **side** | Generate once; place 3–5 copies. See A01. |
| **S09** | Pooja thali (offerings plate) | Lived-in worship detail | ~34×22 | bitforge (style), **high top-down** (it lies flat) | Brass plate: kumkum, rice, petals, tiny lamp. |
| **S10** | Incense holder (agarbatti) | Source of the smoke | ~16×20 | bitforge (style), **side** | Smoke itself is a Unity effect (A02), not baked in. |
| **S11** | Scattered petals / offerings (optional) | Floor/altar dressing | tiny | bitforge or pixflux, **high top-down** | Optional polish. |

> **View-matching rule:** upright things (bow, pillars, altar face, diya, incense) → `side` view. Things lying flat (thali, petals, floor) → `high top-down`, so the slight downward camera tilt reads correctly. Turn **Background removal ON** for every standalone prop, **OFF** for the background scene.

---

## Animations & live effects

| ID | Effect | Built where | How | Notes |
|----|--------|-------------|-----|-------|
| **A01** | Diya flame flicker | **Unity** (recommended) | Flicker the flame's `Light2D` intensity + tiny scale jitter; optional short sprite loop | PixelLab option is limited: *Animate with text* = 64×64 / 4 frames. For real frames use the Pro animation tool, then loop in Unity. |
| **A02** | Incense smoke drift | **Unity** particle system | Soft pixel puff, low alpha, slow upward drift + fade | More organic than a baked loop. |
| **A03** | **Bow divine glow "breathing"** | **Unity** | `Light2D` over the bow + additive glow sprite, intensity pulses (sine) via a small script | Give it a *slightly purer/cooler* gold than the lamps → reads "divine/other". |
| **A04** | Rising motes / embers | **Unity** particle system | Tiny additive dots drifting up, gentle flicker | The indoor-appropriate "fireflies" (spiritual motes). |
| **A05** | Light shaft / god-ray shimmer | **Unity** | Soft additive sprite or `Light2D` cookie, slow opacity/position drift | Falls onto the bow; reinforces focus. |
| **A06** | Garland gentle sway (optional) | PixelLab Pro animation or skip | Very subtle | Low-priority polish. |

---

## Built in Unity, not generated (atmosphere layer)

- **Bloom** (URP Volume post-processing) — makes the bow + flames bleed light. The core of the HD-2D look.
- **Vignette** (URP Volume) — darkens edges, focuses the relic.
- **2D ambient + key lights** (`Light2D`) — warm global tint + a warm pool over the altar + a cooler-gold light on the bow.
- **"press any key" prompt** — `TextMeshProUGUI`, faint, slow pulse (the one bit that follows `UI_WORKFLOW.md`).

---

## Generation budget sanity check

~10–12 generated stills + optional flame frames. pixflux props are cheap; bitforge/tile-pro tools ≈ 20–25 generations each. Total is a small fraction of your ~9,788 remaining — re-roll for quality without worrying. Tier-3 size caps: **400×400** (pixflux/map), **140×140** (bitforge), **64×64** (Animate with text).
