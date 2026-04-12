# Project Astra — Art Direction Foundation

> **Status:** Living document. Pass 1 (Foundation) — established 2026-04-12.
> **Purpose:** The canonical art direction constitution for Project Astra. All asset creation (UI/UX, characters, environments, VFX, key art, marketing) must reference this document. When in doubt, return here.

---

## The North Star

> ### *"Project Astra looks like a fresco being painted from memory by someone who isn't sure he saw it clearly."*

**One-word handle: WITNESS.**

This sentence is the Simplicity-as-Violence test. It implies the palette (aged, warm-neutral, restrained), the composition (negative space, asymmetric, withholding), the mood (retrospective, mournful, uncertain), the pacing (meditative, even in combat), and the UX (information appears as it is *witnessed*, not as it is given).

If a proposed asset cannot be defended in one sentence as "this is a fragment of the fresco, painted from memory," it does not belong in the game.

---

## The Insight This Direction Serves

> *Players who love narrative tactical RPGs want to feel the crushing moral weight of leadership, but the games in this genre — and especially the ones drawing from Indian mythology — keep dressing that weight in the visual language of celebration, ornament, and spectacle, because the industry has confused 'culturally rich' with 'visually loud.'*

The Indian aesthetic that global audiences have been shown is *the festival aesthetic*. The Indian aesthetic the source literature actually demands is *the funeral aesthetic*. WITNESS is built in the second register.

---

## The WITNESS Direction — Full Specification

### Compositional Principle: Withholding as Craft

- Backgrounds are 60-70% **negative space** — warm neutral "plaster" — with hand-rendered "fresco fragments" anchored asymmetrically.
- The contrast between *lavishly detailed fragments* and *bare wall* is what sells the intent. Fragments must be richly rendered. The wall must be confidently empty.
- Every frame contains a **still center** — a deliberate empty area the eye rests on. (Yantra-influenced compositional discipline.)
- Information density is *earned* over time, not delivered upfront. The fog of war is unpainted plaster, not black void.

### Diegetic Justification

The "unpainted" areas are not stylistic — they are **diegetic**. They represent areas the protagonist has not yet witnessed, understood, or come to terms with. As the war unfolds and his bubble shatters, more of the fresco appears. The art direction *is* the protagonist's growing partial knowledge of the world.

This is the art direction's load-bearing thesis: **the player experiences "partial knowledge" as a visual fact, not as exposition.**

### Master Palette (5 base + 3 mood states)

**Base palette (always present):**
- Bone white / aged plaster (ground)
- Terracotta / burnt sienna
- Indigo (deep, mineral)
- Charcoal / iron-gall ink
- Oxidized gold (NOT chrome gold)

**Forbidden:** vermilion festival red, electric peacock blue, chrome gold, neon any-color, pure white, pure black.

**Logic:** every hue feels mineral, vegetal, weathered. Natural-dye rules — nothing pure or saturated. Reference: handloom textile (khadi, indigo, madder, turmeric, lac), faded fresco, aged manuscript.

**Three hidden mood-state variants (palette swaps tied to protagonist's arc):**
- **Dawn (Brahma — Creation):** cool lavender shift, optimistic, gurukul chapters
- **Noon (Vishnu — Preservation):** stark white shift, oppressive, mid-game certainty chapters
- **Dusk (Shiva — Destruction):** amber + deep indigo shift, mournful, late-game shattering chapters

The palette swap technique is pixel-art native (16-bit era craft signature) and indie-achievable. Players are never told why the world is recoloring. They feel it.

### Signature Element: The Bubble Trail

Every unit on the battlefield trails a faint **translucent doubled silhouette** — slightly larger, slightly off-color, lagging by a frame. This is their "bubble" — their interpretation of themselves.

- In moments of moral certainty, the bubble snaps tight to the body.
- In moments of doubt or growth, it drifts further away.
- In death, it separates fully.

This is the Vedantic bubble made literally visible — but as a craft signature, not exposition. Players will not be told what it means. They will learn it over hours.

### Lighting & VFX

- **Soft, single-direction, low-contrast.** No harsh rim lights.
- **No anime explosive VFX.** No screen-shake spectacle. No glowing weapon trails.
- **Astras are quiet and terrible** — closer to the silence after a thunderclap than to the thunder itself. Their visual language is *withdrawal of color*, not addition of effects.
- Healers cast warm light at low intensity. Death is marked by absence (the bubble separating), not gore.

### UI / UX Grammar

- Thin painted-frame boundaries with vast "paper" space inside.
- Devanagari-influenced (but **invented**, not literal) script as decorative ink.
- Everything looks hand-touched, manuscript-adjacent, never machine-clean.
- Information appears progressively, like marginalia being added to a page.
- No glow effects. No drop shadows on text. No skeuomorphic depth.
- Cursor / selection feedback uses **ink-bleed** as the visual metaphor — a tile is "stained" when selected, not "highlighted."

### Surface Texture Principle

Every surface should feel **woven, not printed**. Borrowed from handloom textile tradition: a faint visible "weave" granularity in surfaces, color from natural-dye logic, deliberate irregularity. Conveys "made by human hands" — exactly what an indie pixel art game is.

---

## Anti-Cliché Manifesto

Apply the Specificity Test ruthlessly: replace "Project Astra" with "Generic Indian Mythology RPG #47." If the visual choice still works for that generic competitor, **it does not belong in Astra.**

### Project Astra IS NOT:

1. **Bollywood-saturated.** No vermilion, peacock blue, or chrome gold. Vibrant celebration is the *opposite* emotion.
2. **Temple-iconographic.** No literal devotional imagery, no recognizable deity portraits, no rendered yantras outside compositional reference, no real Sanskrit text in UI (use invented script). The game must NOT feel like a religious product.
3. **Maximalist-ornate.** No baroque borders. No filigree. The default is restraint. Ornament is reserved and earned.
4. **Power-fantasy bright.** No anime VFX, screen-shake, or glowing trails. Astras are quiet.
5. **Three Houses-derivative.** No deep-blue + chrome-gold heraldic palette. We respect the comp; we are not it.
6. **Mobile-gacha exotic.** No portrait waifu/husbando art. No glittery edges. No "premium pull" language. Every character is rendered with the same restraint as the world.
7. **Stardew-cozy.** No squat chibi, bright pastels, or farm-sim warmth.
8. **Octopath HD-2D imitation.** That look belongs to Triangle Strategy. Astra is flatter, more painterly, more handmade.
9. **Generic ethnographic decoration.** Madhubani / Warli / Pattachitra are inspirations for the *spirit* (handmade, natural pigment, ritual craft), not for *surface reference*.
10. **"Painterly" 3D.** Pixel art only. Every pixel placed by a human (or by a tool used by a human with intent).

---

## Production Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Withholding reads as "unfinished" instead of "intentional" | Every fresco fragment must be lavishly detailed. Discipline must come from the top and be enforced ruthlessly. |
| Palette shifts look like a bug | Lock the three palette states early. Build all assets to swap correctly. Do not equivocate during production. |
| Bubble trail reads as graphical noise instead of craft signature | Prototype on a single unit first. Iterate until it reads. Cut if it doesn't. |
| Steam capsule looks empty in 1.5-second judgment | Validate via title screen + key art prototype before committing the full direction. |
| Team / collaborators drift toward filling empty space | Re-read this document at the start of every art review. The discipline is the product. |

---

## Discarded Directions (and Why)

- **Mughal Miniature, Madhubani, Temple Stone & Saffron** — Conventional warmups. All reduced to "ethnographic decoration" under the Specificity Test.
- **Smoke and Ash Above Stone** — Near-monochrome destroys unit readability at 32x32 with 50+ units. Concept absorbed into WITNESS as "restraint of color."
- **Monsoon Watercolor** — Beautiful, but pixel art does not natively support wet color-bleed without expensive shaders.
- **Monsoon Theatre / Kathakali characters** — Locks future character design too narrowly.
- **The Pothi Manuscript (standalone)** — Strong but Pentiment owns the diegetic-manuscript space. **Reserved as the menu/codex layer of WITNESS, not as the master direction.**
- **The Grain of the World (standalone)** — Quieter cultural impact than WITNESS. **Absorbed as the surface texture principle.**

---

## Validation Plan (Before Full Production Commit)

Three artifacts must be prototyped in WITNESS direction and reviewed before scaling:

1. **Title screen mockup** — bone-white wall, a single fresco fragment, lower-third logo. (See Pass 2 deliverable.)
2. **One battlefield screen** — demonstrate withholding with 8-12 units on a 16x16 grid section.
3. **One key art piece** — protagonist as fresco fragment, much of the plaster bare.

If all three sell the direction, full production proceeds. If they don't, fall back to "The Withheld Fresco" standalone (without palette shifts and bubble trail) — lower ambition, lower risk, still distinctive.

---

## Sub-Domain Pass Index

This document establishes the spine. Sub-domain passes drill into specific areas with WITNESS as the inherited foundation.

- **Pass 2 — Title Screen:** _(in progress)_
- **Pass 3 — UI/UX Grammar:** _pending_
- **Pass 4 — Character Silhouette System:** _pending_
- **Pass 5 — Environment / Biome Variety:** _pending_
- **Pass 6 — VFX / Game Feel Language:** _pending_
- **Pass 7 — Cutscene & Key Art Framing:** _pending_

---

## Process Notes (for future re-reading)

- This direction was generated through the `creative-director` skill, recalibrated to game art direction benchmarks (Hades, Disco Elysium, Sea of Stars, Pentiment, Unicorn Overlord) rather than advertising awards.
- Final score on the WITNESS hybrid: **Weighted 8.95 / Resonance 9.**
- The single highest-leverage early action is **prototyping the title screen**. If it sells the direction in 1.5 seconds, the rest of production has cover.
- The discipline is the product. Withholding is cheap (which is the indie team's structural advantage), but only if the team has the courage not to fill it.
