# Aranya — On-Map Sprite (GPT image-gen prompt)

A 32×32 Fire Emblem–style overworld map sprite for the protagonist, Aranya.
Tuned for GPT-4o / DALL·E. Scope: single idle frame, facing down. Earth tones
with one saffron signature accent so she reads as the protagonist on a green map.

## PROMPT (paste into GPT image generation)

A single Fire Emblem–style overworld map sprite of a tactical-RPG hero unit, in clean retro pixel art (GBA Fire Emblem map-sprite tradition).

Subject: "Aranya," a young Indian forest huntress and archer. Super-deformed / chibi proportions (about 2.5 heads tall), seen from a high top-down 3/4 angle, standing in a calm idle pose and facing toward the viewer (facing "down").

Appearance: warm brown skin, dark eyes; long dark hair in a practical braid with a few loose strands; ragged, practical earth-toned attire — a worn forest wrap/tunic in browns and olive with leather straps; a quiver of arrows on her back and a longbow held at her side; one bright signature saffron/amber sash so she reads as the protagonist; small tribal beadwork. Lean and capable; calm, resolute expression. Not royal, not ornate.

Style: crisp limited-palette pixel art, bold readable shapes, clean selective dark outline, soft simple shading. Warm earth-tone palette — browns, olive green, leather tan, with the saffron accent. It must read clearly when scaled to 32×32 pixels: keep detail minimal and the silhouette strong.

Output: one centered single character sprite on a fully transparent background — no scene, no tile, no ground, no baked shadow, no border, no text or UI. Front/down-facing idle frame only.

Avoid: 3D render, smooth gradients, photorealism, painterly portrait, background scenery, multiple characters, drop shadow, signature/watermark.

## NOTES

- GPT won't output a true 32×32 grid — it gives a high-res "pixel-art-style" image. Generate large on the transparent background, then downscale to 32×32 and clean up in Aseprite/Pixelorama. The "reads at 32×32" line keeps it from over-detailing.
- For the other directions later: reuse the first good result as a reference image and swap only `facing "down"` → up / left / right, keeping every other word identical for consistency.
- Our usual map-sprite route is PixelLab (see memory `project_pixellab_unit_sprites`); this GPT prompt is the alternative illustrated-then-pixelized path, mirroring the portrait pair `Aranya_protagonist.md` (GPT) / `Aranya_protagonist_pixellab.md`.
