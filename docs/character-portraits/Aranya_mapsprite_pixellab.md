# Aranya — On-Map Sprite (PixelLab)

## PROMPT — copy this into the description field

```
A young woman archer with brown skin and a long dark braided ponytail. Worn earth-toned tunic and wrap in brown and olive, leather straps, and a saffron sash. A longbow held in one hand and a quiver of arrows on her back.
```

## SETTINGS — set these in the tool (not copy-paste)

Use the **Characters** tool (`create_character`), NOT the image generator.
Matched to the roster pilot Indravati so the cast reads as one set.

- Size: `32`  (exports as 48×48 PNG — PixelLab pads ~50% for animation headroom)
- View: `low top-down`
- Directions: `4` (south, east, north, west)
- Outline: single color black outline
- Shading: medium shading
- Detail: high detail
- Mode: standard (1 generation — not pro)
- Background removal: ON
- Init image: optional but strong — feed the approved Aranya portrait so it pixelizes that exact design

---

## NOTES (don't copy — context only)

- Why the prompt is so bare: PixelLab is a pixel-art model, not a lore LLM. It wants role + equipment + color blocking + one strong silhouette feature (quiver-and-bow archer outline, the braid, the saffron protagonist accent). Drop era/lore names, "map sprite" meta, mood/lighting, quality words, and view-angle words — the camera and render come from SETTINGS.
- Output goes in `Assets/Art/Units/` (e.g. `Aranya_South.png`), replacing the placeholder `_mapSprite` on `Aranya.asset`.
- A 4-dir standard job takes ~2–3 min. Poll with `get_character`.
- GPT alternative path: `Aranya_mapsprite_gpt.md`. Portrait counterpart: `Aranya_protagonist_pixellab.md`. Memory: `project-pixellab-unit-sprites`.
