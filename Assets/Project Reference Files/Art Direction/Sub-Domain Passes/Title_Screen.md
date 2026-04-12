# Title Screen — Direction Spec

> **Status:** ✅ Pass 2 direction locked. State 7 validated via concept render v2 (2026-04-12).
> **Inherits from:** `Art_Direction_Foundation.md` — the WITNESS direction.
> **Purpose:** Concrete art-spec for the Project Astra title screen. Used by whoever builds the title screen mockup, the artist who paints the keyframes, and the audio designer who scores the loop.

---

## Validation Status — State 7 (the held moment)

**Locked reference:** `Tools/output/title_screen_state7_v2.png`
**Generation prompt:** `Tools/output/title_screen_state7_prompt_v2.txt`
**Generation method:** Gemini (gemini.google.com web interface) via manual paste loop
**Final score:** Weighted 8.55 / Resonance 8 — strong candidate, polish-only zone
**Iteration count:** 2 (v1 missed the buried-blade detail, v2 landed it)

### The Gurukul-Memorial Discovery (load-bearing)

The v2 render produced an **unanticipated thematic resonance** that the team should now treat as canonical: the vertical sword in an ash mound on an eroded fresco fragment reads as a **memorial / cairn** — *"a sword left standing in the ashes of something that burned down a long time ago."*

This evokes the **gurukul destruction** (the inciting incident of the protagonist's arc) without naming it. The title screen now silently teaches the player, in 1.5 seconds, that the game begins in the aftermath of grief — not in heroic adventure.

**Implication for downstream art direction:** the "ash + buried object on eroded fragment" motif is now part of WITNESS's visual vocabulary. It can recur in:
- Chapter title cards
- Cutscene framing for major losses
- The post-completion title screen variant (which should now reference the *opposite* — something rebuilt from the same ash)
- Key art for Steam capsule and trailer

**This was a happy accident.** Subsequent passes must protect it. Do not iterate State 7 in a way that risks losing the memorial read.

### Accepted Minor Variances from Original Spec

| Spec said | v2 delivered | Accepted? |
|---|---|---|
| Sword diagonal, half-buried (50% blade hidden) | Sword vertical, ~30-35% buried | ✅ Vertical reads stronger as memorial; depth is balanced |
| Ash as "fine particle scatter at base" | Ash as a defined mound the sword stands in | ✅ The mound enables the memorial reading |
| Hilt sigil as "carved gurukul mark" | Hilt sigil as round pommel medallion | ⚠️ Acceptable for concept render; production version should sharpen the sigil to a stylized intentional shape |
| Clean fragment edges | Slightly architectural fragment edges + faint indigo side strokes | ⚠️ Acceptable for concept render; production version should make the eroded edges more organic and remove any decorative side elements |

### What Was Validated

- ✅ Withholding-as-craft works visually — the ~70% empty wall reads as intentional, not unfinished
- ✅ Restrained natural-dye palette communicates "aged, mineral, faded" without instruction reading "drab"
- ✅ The painterly fresco-aged style is reproducible from prompt
- ✅ The composition carries genuine emotional weight ("held breath" mood lands)
- ✅ The Specificity Test passes — replace Astra with Generic Indian Mythology RPG #47 and the image makes no sense
- ✅ The "buried, not broken" symbol reads correctly when emphasized in the prompt

### Production Notes for the Pixel Art Version

When a real artist (or pixellab) converts this to native pixel art:
- **Keep:** vertical sword, ash mound, eroded fresco fragment, restrained palette, vast empty wall, mournful mood, the memorial read
- **Refine:** sharpen the hilt sigil to an intentional symbol (stylized sun/leaf/geometric mark — design TBD), remove any decorative indigo side elements, make the fragment edges more organically eroded
- **Add (per Foundation doc):** the very faint woven plaster wall texture, the "Grain of the World" surface principle
- **Test on:** Switch handheld dimensions (1280×720) for fragment legibility before locking final size

---

---

## Concept Name

**"The Sword That Is Remembered"**

## One-Sentence Pitch

> *A title screen that paints itself into existence, holds for a moment, and then forgets — the entire Vedantic cycle of Project Astra enacted in 30 seconds, on loop, before a single button is pressed.*

## Why This is the Right Title Screen for WITNESS

- It teaches the player **in 30 seconds, without a single line of text**, that this is a game about memory, suspension, weight, and the cycle of creation and destruction.
- It enacts the Vedantic Brahma/Vishnu/Shiva inner cycle *as a visual loop the player witnesses before they ever press a button.* The art direction's central thesis is delivered before the game begins.
- It is **indie-achievable**: 8 keyframes + transition pixel-passes is roughly a single skilled artist's week, not a month. The animation is essentially a slideshow with care.
- It passes the Specificity Test: replace "Project Astra" with any other game and the loop makes no sense. Only WITNESS earns it.
- It rewards both player types: linger and you see the cycle; click past and you still see at least one held moment.
- The half-buried sword is a stronger symbol than the Falchion (Awakening), the Sothis throne (Three Houses), or the Skyrim emblem — because it *implies a question rather than declaring an identity*. "Why is it buried? Who buried it? Will it be drawn?" The player is hooked before the menu loads.

---

## Composition Spec — The Held Moment (State 7)

### Canvas
- Standard 16:9, designed at native pixel art resolution. **Recommended: 384×216 or 480×270**, then integer-scaled. Confirm with engine setup.

### Background
- Bone-white plaster wall.
- The faintest weave-texture noise across the entire surface (the "Grain of the World" surface principle from Foundation doc).
- Slightly **warmer at the lower-right corner**, slightly **cooler upper-left** — very subtle directional ambient. Almost imperceptible. Just enough to give the wall depth.

### The Fresco Fragment (the focal element)
- **Position:** lower-center, occupying roughly **30% of the canvas width**.
- **Subject:** a sword pushed half into ash/dust at a slight angle.
- **Hilt:** detailed — wrapped grip, simple cross-guard, a small carved sigil that the player will eventually recognize as the gurukul's mark.
- **Blade:** disappears into the ash about ⅔ down its visible length.
- **Critical distinction:** the blade is **NOT broken — it is buried.** Broken = past tense ("the hero failed"). Buried = suspended ("the hero has not yet drawn it"). The hilt must look well-cared-for, not weathered. The sigil must be intact.
- **Ash:** fine charcoal-gray pixel scatter at the base of the sword, fading outward into the plaster over ~20 pixels.

### Negative Space
- Upper two-thirds of canvas: bare wall.
- Right third of canvas: bare wall.
- The eye is *forced* to rest in the empty area, then drawn to the fragment.
- This is non-negotiable. Do not fill it.

### Logo
- **Position:** lower-third, slightly off-center toward the right.
- **Typeface:** custom serif with mineral-gold (NOT chrome gold) fill. Drawn at small scale — no more than 1/8 of canvas height.
- **Below the logo, in the same gold but smaller:** *"PRESS ANY KEY"* → translated visually as a single line of invented Devanagari-influenced script that the player learns to read as "begin."
- **First-time fallback:** for the first 8 seconds of the cycle, show the invented script alongside a small English subtitle. After 8 seconds, fade out the English.

### Legal Text
- Lowest-left corner.
- Charcoal color, smallest legible pixel font.
- Minimum required only — no padding, no copyright symbols beyond the necessary.

---

## Animation — The Loop

**Total cycle: ~30 seconds.** Eight states. Held with subtle pixel-level transitions. Loops seamlessly.

| State | Duration | What's Visible |
|---|---|---|
| 1. Bare wall | 2s | Just plaster. Weave texture. Logo and prompt visible. *The wall before memory.* |
| 2. First ink lines | 2s | Faint charcoal contour appears — the silhouette of the sword and ash, undrawn. |
| 3. Underdrawing | 2s | Contours sharpen. Suggestion of form. Grayscale only. |
| 4. First color block | 2s | The hilt grip color appears — a single muted color block. |
| 5. Second color block | 2s | Ash color fills in. |
| 6. Detail pass | 3s | Hilt sigil appears. Cross-guard detail. The blade's surface — a single highlight. |
| 7. **The held moment** | **10s** | **Full image. This is the screen the player sees if they linger.** |
| 8. Slow fade to plaster | 5s | The image desaturates and softens back into the wall. Last to disappear: the ash, then the hilt sigil. |
| **Loop back to State 1.** | | |

**Implementation note:** transitions between keyframes do not need to be true frame-by-frame animation. A short cross-fade (4-6 frames) between each held state is sufficient. The aesthetic effect is "memory resolving and fading," not "smooth motion."

---

## Audio — Title Screen Soundtrack

- **First 3 seconds (State 1):** silence. Only ambient room tone — wind, distant temple bell faint enough to be unsure.
- **States 2–6:** a single tanpura drone fades in. **No melody. Just the drone.**
- **State 7 (the held moment):** a single voice — solo, female, no harmony — sings the first phrase of the game's main theme. **Eight notes maximum.** Sanskrit-influenced melisma but **invented language**, not real Sanskrit (consistent with the invented-script rule in the Foundation doc).
- **State 8 (fade):** the voice and drone fade together. Silence at the loop point.

**CRITICAL:** the voice must NOT use real Sanskrit and must NOT use a devotional melodic mode (avoid bhajan-style ragas like Bhairavi). Use an **invented mode in a melancholic register** — closer to a dirge than a hymn. If it sounds like temple devotional music, the brief has failed.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| The painting animation feels gimmicky after 5 cycles | The cycle is 30 seconds. Most players will see it 1–2 times before pressing start. The risk is for the press-Steam-page-watching-trailer crowd, not menu users. |
| The fresco fragment looks too small / loses presence on Switch handheld | Size the fragment at minimum 25% of the smallest target screen's height. Test on Switch handheld dimensions early (1280×720 docked and handheld). |
| Players interpret "buried sword" as "broken hero" | The blade is *intact* — only buried. Hilt looks well-cared-for, sigil intact. Reinforce in the second color block stage. |
| The voice / tanpura combo reads as "spiritual product" or "yoga app intro" | Invented language only. Melancholic mode, not devotional. Closer to a Norwegian funeral hymn than a bhajan. |
| The "PRESS ANY KEY" prompt in invented script confuses new players | First 8 seconds of cycle: show invented script alongside English subtitle. After 8 seconds, fade out the English. |
| The bare wall reads as "unfinished placeholder" in Steam capsule | Validate via static mockup BEFORE animation work. If State 7 alone doesn't sell the direction, the animation won't save it. |

---

## Dynamic Title Screen — Post-Completion Unlock

Per the Foundation doc's emphasis on evolving title screens (Three Houses, Portal 2, Hollow Knight): **the title screen changes after game completion.**

After the player finishes the game once, a new variant unlocks:

- **State 7 (the held moment)** in the new variant shows the sword *removed from the ash* — the hilt sigil now visible in full, blade above the dust, holding faint dawn light.
- The voice in the held moment sings a *second* phrase, four notes longer than before.
- Nothing else changes.

The player will not be told. They will notice. This is the WITNESS reward for the player who witnessed the game to its end.

**Possible second variant** (after a hard difficulty / no-deaths run, TBD): the sword fully drawn, held by a partial fresco fragment of the protagonist's hand. To be locked in a later pass.

---

## Concrete Next Steps

1. **Mock up State 7 as a static concept render** (use Gemini / Imagen / ChatGPT image gen — see prompt at bottom of this file). This is the single highest-leverage artifact. If it doesn't sell the direction, nothing else will.
2. **Validate the concept render** by pasting it back into a `creative-director` Phase 4 Evaluate-Only pass against the WITNESS constitution.
3. **If validated:** lock the master palette in a Unity color swatch file (`Assets/_Art/Palettes/Master_Witness.asset`). Stop debating it.
4. **Storyboard the 8 keyframes** as low-fi sketches. Validate timing on a 30-second click-through.
5. **Test the held moment on Switch handheld dimensions** (1280×720) for legibility before committing fragment size.
6. **Commission/draft the audio loop** (tanpura drone + 8-note vocal phrase + ambient).

---

## Concept Render Prompt (for Gemini / Imagen / ChatGPT image gen)

Use this prompt to generate the State 7 mockup. Iterate by tweaking color words and composition cues, not by adding detail.

> A 16:9 painterly concept art frame for a tactical RPG title screen. The composition is deliberately empty: 70% of the frame is a bone-white aged plaster wall with a very faint woven texture, slightly warmer in the lower-right corner and slightly cooler in the upper-left. In the lower-center of the frame, occupying about 30% of the width, is a single fresco-style painted fragment depicting a sword pushed halfway into a bed of fine grey ash. The sword's hilt is fully visible — a wrapped leather grip, simple iron cross-guard, and a small carved sigil on the pommel — but the blade disappears into the ash. The blade is intact, NOT broken, only buried. The fresco fragment is rendered in a restrained natural-dye palette: terracotta, indigo, charcoal, oxidized weathered gold (NOT chrome gold), bone white. Faint ash particles scatter outward from the sword's base into the plaster. The painting style is aged, hand-touched, like a 12th-century Indian or Mediterranean fresco where the surrounding wall has eroded to nothing. No ornament, no borders, no decoration. The mood is mournful, contemplative, suspended — the moment before something hard is decided. NOT festive. NOT epic. NOT bright. NO temple iconography. NO religious symbols. NO chrome or shine. NO glow effects. The image should feel like a held breath. Lower-right corner: empty space reserved for a small game logo (do not draw the logo). Lower-left corner: empty space reserved for legal text (do not draw text). Cinematic, restrained, painterly. Inspired by aged fresco wall paintings, monsoon-faded murals, and the visual language of grief.

**Iteration tips:**
- If the result looks too "epic" or sword-glamorous → emphasize "buried, suspended, mournful" and de-emphasize "sword."
- If the result looks too festive / colorful → reinforce "natural dye, weathered, oxidized, bone-white plaster, restrained."
- If the wall feels empty in a bad way → ask for "deliberate negative space, like a meditation diagram, where the void is the point."
- If the sword looks broken → reinforce "the blade is intact and buried, not broken or shattered. The hilt is well-cared-for."

---

## Process Notes

- **Generated via:** `creative-director` skill, Pass 2, Execution level. Methods: SCAMPER (structural) + Synectics (analogy from manuscript/fresco/cycle traditions) + Oblique Strategies (perturbation).
- **Final score:** Weighted 8.95 / Resonance 9. Plateau reached, exit per stopping criterion.
- **Inherited constraints from:** `Art_Direction_Foundation.md` — WITNESS direction, master palette, withholding principle, no-glow rule, anti-cliché manifesto.
- **Subject to revision after:** State 7 concept render is generated, evaluated, and either validated or sent back for iteration.
