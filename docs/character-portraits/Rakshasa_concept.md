# Rakshasa — Concept Art (Stage 1, GPT image-gen prompt)

Stage 1 of the Rakshasa pipeline: **nail the look** with painterly concept art in
ChatGPT before committing to a portrait. This is exploratory concept art — NOT the
final dialogue portrait and NOT pixel art yet.

Goal: a culturally accurate Hindu-mythology rakshasa (a proud warrior-demon race),
explicitly **not** a Western fantasy ogre. The basic in-game enemy is the
"Rakshasa Raider"; this concept defines the shared visual DNA for the whole type
(Raider → Pati/chief → miniboss).

Pipeline: **Stage 1 concept (GPT, this file)** → Stage 2 locked portrait (GPT) →
Stage 3 pixelized portrait (GPT) → Stage 4 real pixel asset (PixelLab, init-image
from Stage 3). Mirrors the Aranya pair `Aranya_protagonist.md` / `_pixellab.md`.

---

## LOCKED DIRECTION (v1)

- **Color register:** coppery-red / ember
- **Archetype:** savage war-band raider (matches our basic "Rakshasa Raider" enemy)
- **Monstrousness:** overt (pronounced tusks, claws, frightening)

## PROMPT (paste into ChatGPT image generation)

A single full-body character concept-art illustration of a **rakshasa raider** — a
warrior-demon from ancient Indian (Hindu) mythology, in the tradition of the Ramayana and
the Puranas. He is a feral war-band raider of a proud, martial demon-race — savage and
frightening, but **NOT** a Western fantasy ogre, troll, or orc. Keep him unmistakably from
Indian myth: a brutal warrior, yet dignified in his ferocity.

Subject — a male rakshasa raider, anchored in authentic Indian iconography:
- **Build:** tall, broad, heavily muscled — a powerful, imposing, battle-scarred warrior
  physique. Lean and predatory, coiled with menace. No bloated belly, no dumb-brute slump.
- **Face (overtly demonic):** bestial and frightening — a heavy ridged brow, a snarling
  mouth with pronounced tusks jutting from the lower jaw and sharp fangs, blazing molten
  copper-red eyes, a broad flared nose, pointed ears. A wild matted mane of dark hair,
  partly bound in a rough top-knot, with a coarse beard.
- **Hands:** thick clawed talons instead of nails.
- **Skin:** deep coppery-red / ember tone — burnished like heated bronze, dark in the
  shadows and glowing at the highlights. NOT green, NOT a sickly ogre tone.
- **War-markings:** crude vermilion and sacred-ash (vibhuti) war-paint streaked across the
  face, chest, and arms — tribal but rooted in Indian ritual marking, not generic tattoos.
- **Dress & ornament (raider, not king):** sparse and savage — a rough warrior dhoti/loincloth
  and hide wrap in deep crimson and dark leather, thick crude gold armlets and looted
  ornaments, strands of bone, tusks, and rudraksha beads as trophies, heavy iron earrings.
  Worn, scarred, and battle-stained — the gear of a raider, not a gilded demon-king.
- **Weapon:** a brutal heavy curved blade (khadga / cleaver) or a spiked iron mace (gada),
  gripped in a clawed hand, ready for slaughter.

Style: detailed, HD, hand-illustrated concept art — semi-realistic painterly rendering with
clean confident drawing, rich textures, dramatic cinematic lighting. Cohesive ancient-India
palette: ember coppery-red skin, deep crimson and dark-leather cloth, dull battle-worn
bronze, with hot ember-orange rim light and deep shadow. Mood: savage, ominous, mythic.
Draw on classical Indian visual references — Kathakali / Yakshagana demon-warrior (kati)
costume and makeup, fierce temple dvarapala / asura guardian sculpture, Mahishasura
iconography, Raja Ravi Varma's demons.

Composition: a single centered full-body character on a plain dark neutral background (a
clean character-design / concept-sheet look), standing in a powerful, aggressive, grounded
warrior stance. Clear, readable, threatening silhouette.

Avoid: Western ogre/orc/troll, green skin, goofy or comedic look, potbelly, modern clothing,
European-devil horns, ornate king/royal costume (he is a raider), busy scene/landscape,
multiple characters, text, logos, watermark, pixel art (this stage is smooth painterly).

---

## ITERATION LEVERS (turn these between generations to refine)

Locked for v1 above. If you want to explore, these are the knobs:

1. **Skin / color register** — `coppery-red / ember` (LOCKED) · alternatives:
   `storm-cloud charcoal-indigo` (regal Ravana), `ash-grey` (ascetic), `Kathakali-green`
   (theatrical kati makeup — the one culturally-grounded green, not ogre-green).
2. **Archetype** — `savage raider` (LOCKED) · alternatives: `noble warlord` (for the
   Pati/chief or miniboss later), `sorcerer-ascetic`.
3. **Monstrousness dial** — `overt` (LOCKED) · `subtle` (handsome-fierce, fangs+eyes only).
4. **Extra arms** — optional: add `with a hinted second pair of muscular arms` for a more
   supernatural/boss read (probably save this for the Pati/miniboss, keep the basic Raider
   two-armed).
5. **Framing** — `full-body concept` (default) vs `bust/portrait crop` (face-only for Stage 2).

## NOTES

- ChatGPT outputs a high-res illustration, not a game asset — that's correct for concept.
- Once you lock a result you love, that image becomes the **reference/init for Stage 2**
  (the dialogue portrait), so posture, palette, and expression carry over.
- Keep the "NOT a Western ogre" line in every regeneration — it's the load-bearing
  instruction; models drift to orc-green without it.
