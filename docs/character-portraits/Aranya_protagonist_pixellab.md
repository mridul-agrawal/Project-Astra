# Aranya — Protagonist Portrait (PixelLab)

## DESCRIPTION (paste into the tool's description field)

Portrait bust, head and shoulders, of a young Indian forest huntress — brown skin, dark eyes, long dark braided hair with a few loose strands, ragged earth-toned cloth and leather, a quiver strap over one shoulder, calm and determined expression, facing slightly to one side, soft warm light.

> Keep it concise and purely visual. PixelLab is a pixel-art model, not a world-knowledge LLM — drop lore/era names ("Mahabharata/Ramayana"), meta ("tactical-RPG dialogue portrait"), and anything that's a setting ("highly detailed", view angle). Those don't help the output and can add noise.

## SETTINGS (set these in the web app)

- **Tool:** Create medium-extra large image (pixflux) — the image generator, NOT the "Characters" tool.
- **Canvas size:** portrait aspect, large for detail — about **320 × 400** (up to the Tier-3 cap). *Size is the main "HD/detail" lever — bigger = more detail, less GBA-chunky.*
- **Camera view:** side (front-facing character; not top-down).
- **Outline:** selective outline (cleaner / more modern than a heavy black GBA outline).
- **Shading:** detailed shading.
- **Detail:** high detail.
- **Background removal:** ON (clean cutout for the dialogue UI).
- **Target palette:** optional — a warm earth-tone / Indian palette for consistency, or leave default and unify later.
- **Init image:** optional but powerful — see tip below.
- **Seed:** set a fixed value once you find a result you want to iterate on.

## TIPS

- **Bridge from the GPT version:** generate the portrait in ChatGPT first (the `Aranya_protagonist.md` prompt), then feed that image — or your old Indravati mock-up — into PixelLab as the **Init image**, so PixelLab pixel-izes *that exact design* instead of inventing a new face. (For the tightest style-match PixelLab has the bitforge style tool, but it caps at 140×140; for a larger detailed portrait use pixflux + init image.)
- **Expression set:** once you have a base you like, reuse it as the init image and change only the expression word (alarmed / anguished / determined) to build the matching set on the same face.
- **Reality check:** this is *pixel art*. If you want the smooth illustrated look, that's the ChatGPT route; if you want a portrait that matches the pixel world, this is the one. You can keep both and decide which reads better in-game.
