# On-Map Unit Sprite Pipeline (FE-GBA chibi, 32px)

How to turn a character's **concept-art portrait** into a 32px Fire-Emblem-GBA-style
chibi on-map sprite that reads as the *same* character. Worked example: **Indravati**
(`Assets/Art/Portraits/Indravati.png`).

> Prompting principles distilled from pixellab.ai/docs live in memory
> `reference_pixellab_prompting.md`. This doc applies them to on-map units.

---

## What "match the concept art exactly" actually means

A 32px chibi is **not** a downscale of a detailed portrait — it is a re-draw into
super-deformed proportions (~2–2.5 heads tall). Fidelity = **palette + silhouette**,
not face. The character is recognized by:

- skin tone + hair color/shape (long braid)
- the two or three dominant outfit colors
- one signature silhouette feature (here: quiver of arrows + bow)

The model gets ~80%; the final palette/silhouette match is finished **by hand in
Pixelorama**, sampling colors directly from the portrait PNG.

---

## Tool choice (read this before generating)

- **Chibi only survives in `standard` mode.** `pro` and `v3` both *ignore* the
  `proportions` parameter, so they discard the chibi shape. Do **not** use pro/v3 here.
- **MCP `create_character` has no image input** — it matches the text description, not
  the concept-art pixels. Good for a fast, recognizable result; not for pixel fidelity.
- **For maximum fidelity to the portrait**, use the **PixelLab web-app Create Character**
  (or experimental **"Create walking character"** for a 4-dir walk in one shot) with the
  portrait dropped in as an **init / style reference**, kept in a chibi-capable mode.
- **Do NOT use `image-to-pixel-art`** on the portrait — it preserves realistic
  proportions and yields a tiny pixel *portrait*, not a chibi map unit.

---

## Recommended workflow

1. **Extract a feature spec from the concept art.** List, in plain functional terms:
   skin tone, hair color/style, the 2–3 dominant outfit colors + 1 accent, weapon, and
   the one silhouette feature that makes them readable. (No mood/lighting words.)
2. **Pull the palette from the portrait.** Eyedrop the real hex values for skin, hair,
   primary cloth, accent cloth, metal/wood. This is the source of truth for "match."
3. **Generate the base sprite** with the params below (standard mode, chibi, low
   top-down, 32px). Iterate cheap (1 gen each) until the silhouette + colors are right.
4. **(Higher fidelity option)** In the web app, add the portrait as an **init/style
   reference at low strength** so it informs colors/features while letting the model
   re-proportion to chibi. Web app also has a **Negative Description** field (MCP has none).
5. **Clean up in Pixelorama:** correct the palette to the eyedropped portrait colors,
   tidy the silhouette, remove stray pixels, ensure a 1px readable outline.
6. **Export & place:** crop to the character, drop into `Assets/Art/Units/`
   (or `Assets/Art/Map1/Units/`, replacing the placeholder icon), size to the 32px tile.

> **Sizing gotcha:** `size: 32` = character *height*; PixelLab pads the canvas ~40–50%
> (≈45px file) for animation headroom. Crop to the character before placing on the tile.

---

## Parameters (Create Character — standard mode)

| Param | Value | Why |
|-------|-------|-----|
| `mode` | `standard` | Only mode that respects `proportions` (chibi) |
| `proportions` | `{"type":"preset","name":"chibi"}` | The FE-GBA super-deformed look |
| `view` | `low top-down` | ~20° — FE map units face mostly forward |
| `size` | `32` | Character height; expect ~45px padded canvas |
| `n_directions` | `4` | S/W/E/N (south first; same cost as 8, faster) |
| `outline` | `single color black outline` | Crisp GBA-style read |
| `shading` | `basic shading` | GBA sprites are simple; medium at most |
| `detail` | `low detail` | 32px chibi cannot hold fine detail |
| `text_guidance_scale` | `9`–`11` | Push feature adherence; >12 risks artifacts |

Reuse this exact param block across the whole roster for visual consistency.

---

## Prompt template

**Description (functional only — role + build + color blocks + 1 silhouette feature):**

```
<build> <class>, <skin tone> skin, <hair color + style>,
<primary outfit color> <garment> with <accent color> <trim/detail>,
<legwear/footwear>, <signature equipment on body>, holding <weapon>
```

**Web-app Negative Description (optional; MCP has no negative field):**

```
no background, no realistic proportions, no helmet, no heavy plate armor,
no cape, no text, no watermark
```

---

## Worked example — Indravati (Cinder-Born Hunter, foot archer)

**Description:**

```
lean barefoot female archer, coppery-brown skin, very long black single braid down her back,
sleeveless charcoal-grey tunic with rust-orange trim, brown leather trousers,
quiver of black arrows on her back, thin gold chain at throat, holding a plain wooden recurve bow
```

**Negative (web app):**

```
no background, no realistic proportions, no helmet, no armor, no cape, no shoes, no text
```

**Palette to lock in Pixelorama** (eyedrop exact hex from `Indravati.png`):
coppery-brown skin · black hair · charcoal-grey tunic · rust-orange trim ·
brown leather · pale wood bow · gold chain.

**Generation note:** run a few `standard`-mode passes (1 gen each), pick the best
silhouette, then finish the palette by hand against the portrait.
