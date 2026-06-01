# Rakshasa Raider — Dialogue Portrait (Stage 2, GPT image-gen prompt)

Stage 2 of the Rakshasa pipeline: turn the **locked Stage-1 concept art** into an
FE-style dialogue portrait (a head-and-shoulders bust). **Attach the finalized concept
art image as a reference when you paste this prompt** — the whole point is to re-render
*that exact character* as a portrait, not to invent a new one.

This is the neutral BASE expression; alarmed / enraged / wounded variants get made later
off this same face. Matches the protagonist's portrait convention (`Aranya_protagonist.md`)
so the cast sits together in the dialogue UI.

Pipeline: Stage 1 concept (locked) → **Stage 2 portrait (GPT, this file)** → Stage 3
pixelized portrait (GPT) → Stage 4 real pixel asset (PixelLab, init-image from Stage 3).

---

## PROMPT (paste into ChatGPT, WITH the locked concept art attached as reference)

Using the attached concept art as the exact, locked character design, create a character
DIALOGUE PORTRAIT (a mugshot / bust) of this same rakshasa raider. **Preserve his design
faithfully** — the same face, the same ember coppery-red skin, the same tusks, fangs,
clawed brow and blazing copper-red eyes, the same wild matted hair and beard, the same
vermilion-and-ash war-paint pattern, the same crimson/leather/bone-trophy attire and crude
ornament, and the same color palette. Do NOT redesign him or change his species read — only
re-render this established character as an FE-style portrait bust.

GAME: Project Astra — a tactical RPG (Fire Emblem–style grid combat) set in the era of
ancient India's epics. The game pairs pixel-art gameplay with detailed, hand-illustrated
character portraits in the modern Fire Emblem tradition.

USE: his neutral dialogue portrait, shown beside his lines in cutscenes and conversation.
Head-and-shoulders, expressive, on a plain dark background so it can be cleanly cut out and
placed in the dialogue UI.

EXPRESSION (neutral base for an antagonist): a cold, menacing baseline — a hard hostile
glare, jaw set, a faint contemptuous snarl baring the fangs. Threatening but composed, not
mid-roar. (Other expressions — enraged/roaring, sneering, wounded — will be made later as
variants of this same face.)

STYLE: a detailed, HD, hand-illustrated character portrait in the modern Fire Emblem portrait
tradition (think Fire Emblem Awakening / Fates / Three Houses key art) — clean lineart with
soft painterly shading, high detail, warm cinematic lighting. Explicitly NOT low-resolution
GBA-style pixel art — smooth and HD. Keep the cohesive ancient-India palette from the concept:
ember coppery-red skin, deep crimson and dark leather, battle-worn bronze, hot ember rim light.

FRAMING / POSE (match the Fire Emblem GBA mug convention, same as the rest of the cast): a
head-and-shoulders bust in a THREE-QUARTER view — body and head turned about 30–45° to one
side (toward the viewer's RIGHT, as in FE GBA portraits), shoulders angled with the turn, and
his eyes looking toward that same side (toward an off-frame conversation partner). NOT
front-on, NOT staring straight at the camera, and NOT a full profile — we still see most of
his face. Keep the facing consistent with the cast; the engine mirrors portraits so two
speakers face each other. Centered bust, plain dark background for clean compositing.

AVOID: GBA pixelation, low resolution, full-body, busy background, changing the character's
design or colors from the reference, green ogre skin, European-devil horns, comedic look,
modern elements, text, logos, watermark.

---

## ITERATION LEVERS

- **Expression** — the big one. Base is `cold menacing glare`. Alternatives to try:
  `enraged / roaring` (mouth open, full fangs — good for a combat/death line),
  `cruel sneer`, `wounded / snarling in pain`. Lock the calm base FIRST, then build the set
  by reusing the locked portrait as the reference and changing only the expression word.
- **Facing** — keep `toward viewer's right` to match the cast (engine mirrors). Only flip if
  we change the whole cast convention.
- **Crop tightness** — `head-and-shoulders` (default) vs a slightly wider `chest-up` if the
  ornament/trophies are important to read in dialogue.

## NOTES

- The reference attachment is doing the heavy lifting — keep restating "preserve the design /
  do not redesign," because image models drift the face and colors otherwise.
- Once the base portrait is locked, it becomes the reference/init for **Stage 3** (pixelize
  in ChatGPT) and the expression-variant set.
- This is the **smooth illustrated** route. The matching pixel-asset route is Stage 4 in
  PixelLab — same split as the Aranya `_protagonist` / `_protagonist_pixellab` pair.
