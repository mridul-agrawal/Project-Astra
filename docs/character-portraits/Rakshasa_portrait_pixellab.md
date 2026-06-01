# Rakshasa Raider — Pixel Portrait (PixelLab, from HD portrait)

Final stage: pixelize the **locked HD FE portrait** (Stage 2) into a real pixel-art asset
in PixelLab, feeding that portrait as the reference/init image. We skip the ChatGPT
pixelization pass and let PixelLab do the pixel render directly off the HD portrait.

Two prompts below, kept separate as requested: the **main description prompt** (the text
field) and the **reference-image prompt** (how to attach and weight the HD portrait).
Mirrors `Aranya_protagonist_pixellab.md`.

> Keep the description concise and PURELY VISUAL. PixelLab is a pixel-art model, not a
> world-knowledge LLM — drop lore/era names ("Ramayana", "rakshasa"), meta ("FE dialogue
> portrait"), and view-angle prose. Functional descriptors only; those are what the model
> actually uses. The HD reference image carries the identity; the text just steers the pixels.

---

## 1. MAIN PROMPT (paste into the description field)

Portrait bust, head and shoulders, of a fierce demon-warrior — ember coppery-red skin,
glowing red eyes, protruding tusks and sharp fangs, heavy ridged brow, wild matted dark
hair and beard, vermilion and white war-paint streaked across the face, crude gold armlets
and bone-and-bead trophy necklaces, crimson and dark-leather wrap, cold menacing glare,
facing slightly to one side, warm ember rim light, dark background.

---

## 2. REFERENCE-IMAGE PROMPT (how to use the HD portrait)

Attach the locked Stage-2 HD portrait as the **init image** (pixflux generator). Intent:
keep this exact face, pose, expression, and color palette — only re-render it as pixel art.
Do not let PixelLab invent a new face.

- **Init image strength:** start fairly HIGH so it stays faithful to the reference's
  composition and colors — around **70–80%**. Too low and it drifts into a different face;
  too high (near 100%) and it just down-samples the photo without a clean pixel read. Tune in
  ±10% steps: lower it a little if the result looks muddy/over-detailed, raise it if the face
  or colors wander from the reference.
- **Keep the text prompt aligned with the reference** (section 1 already describes the same
  character) so the description reinforces the init image instead of fighting it.
- If PixelLab also exposes a separate **style/color reference** slot, you can additionally
  feed the same portrait there to pin the ember palette.

---

## 3. SETTINGS (set these in the web app)

- **Tool:** Generate medium–extra large image (**pixflux**) — the image generator, NOT the
  "Characters" tool.
- **Canvas size:** portrait aspect, large for detail — about **320 × 400** (up to the Tier-3
  cap), matching the Aranya pixel portrait. *Size is the main HD/detail lever — bigger = more
  detail, less GBA-chunky.* Drop toward GBA-native (~96 × 80) only if you want a true
  retro-chunky read.
- **Camera view:** side (front-facing character; not top-down).
- **Outline:** selective outline (cleaner / more modern than a heavy black GBA outline).
- **Shading:** detailed shading.
- **Detail:** high detail.
- **Background removal:** ON (clean cutout for the dialogue UI) — or keep the dark bg if you
  prefer to mask later.
- **Target palette:** optional — a warm ember / coppery-red + crimson + bronze palette for
  consistency, or leave default and unify later.
- **Seed:** once you get a result worth iterating, fix the seed and tune only one knob at a
  time (init strength, then palette, then detail).

## NOTES

- **Reality check:** this is the pixel route — it matches the in-game pixel world. The smooth
  HD route is the Stage-2 portrait itself. You can keep both and decide which reads better in
  the dialogue UI.
- **Expression set:** once a base pixel portrait is locked, reuse it as the init image and
  change only the expression word (enraged / sneer / wounded) to build the matching set on the
  same face.
- For the tightest style-match PixelLab has the bitforge style tool, but it caps at 140×140;
  for a larger detailed portrait, pixflux + init image (this doc) is the route.
