# Child (Village Girl) — Portrait (PixelLab)

## DESCRIPTION (paste into the tool's description field)

Portrait bust, head and shoulders, of a frightened young Indian village girl — brown skin, big dark teary eyes, dark hair in a messy side braid with loose strands, soot smudges and tear streaks on her face, small red tilak, ragged torn earth-toned cloth garb, beaded necklace, red thread armband, scared pleading expression, three-quarter view with her face turned to one side, not facing the viewer.

> Keep it concise and purely visual — PixelLab is a pixel-art model, not a world-knowledge LLM. No lore/era names, no "RPG dialogue portrait" meta, nothing that's a setting (detail/view/outline live in the controls below).

## SETTINGS (set these in the web app)

- **Tool:** Create medium-extra large image (pixflux) — the image generator, NOT the "Characters" tool.
- **Canvas size:** portrait aspect, large for detail — about **320 × 400**.
- **Camera view:** side (front/side-facing; not top-down).
- **Outline:** selective outline.
- **Shading:** detailed shading.
- **Detail:** high detail.
- **Background removal:** ON.
- **Target palette:** optional — warm earth tones, or unify later.
- **Init / reference image:** the approved ChatGPT child portrait (see prompt below).
- **Seed:** fix once you find a result to iterate on.

## REFERENCE / STYLE IMAGE PROMPTS (single-line)

- **Reference image (the ChatGPT portrait):**
  `Reproduce the girl in the reference image exactly — her face, braided hair, teary frightened expression, soot and tear streaks, tilak, ragged garb, beads, and armband — re-rendered as pixel art.`

- **Style images (only if you want the GBA look):**
  `Match the pixel-art style of the style images — their outline weight, shading, and palette — applied to this character.`
  ⚠️ The FE-Repo mugs are GBA-low-resolution — using them as style images pulls toward the chunky GBA look (and the style-matching pro tool caps at 140×140). For an HD-detailed result, **skip the style images** and let the reference + settings + a large canvas carry it.

## TIPS

- **Bridge from the GPT version:** feed the approved ChatGPT child portrait in as the **Init image** so PixelLab pixel-izes that exact face/pose instead of inventing a new one.
- **Keep the set cohesive:** use the *same* settings (size, outline, shading, detail) you used for Aranya's pixel portrait, so the two pixel portraits match.
- **Reality check:** this outputs pixel art (cohesive with the game world); relative size vs Aranya is handled by the bottom-anchored scale in Unity, not here.
