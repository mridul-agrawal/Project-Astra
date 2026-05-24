# Title Screen — Design Brief & Decisions

*Project Astra · the first screen the player ever sees. This doc captures the thinking and the locked decisions behind it. It is the "why"; Doc 02 is the "what" (asset list); Doc 03 is the "how" (production playbook).*

---

## What this screen is for

The title screen is the single highest-leverage moment for **Pillar 1 (undeniable Indian cultural identity)** — it is one still image the player *stares at*, so it can concentrate identity harder than any stretch of gameplay. Its whole job: in ~2 seconds, read as **unmistakably ancient-India epic**, and set an emotional tone that makes the player lean in.

It carries **no game-title text / wordmark** for now (the project name may change, and we don't want to commission lettering art yet). The only text is the functional *"press any key"* prompt from the design doc's Scene 01. The image has to hold the screen on its own — which, for a title-less screen, is actually a strength: pure atmosphere.

---

## The core concept

**The ancient relic bow, resting on a worshipped altar inside the temple sanctum.**

Why this, and not a character or a landscape:

- **Less noise.** The whole frame points at one mysterious object — and that object *is* the central thread of the prototype (the hidden weapon the Rakshasas come to steal, the relic the priest finally places in the protagonist's hands).
- **Cultural specificity for free.** A simple temple interior says "this world" without a word, and a bow is the signature weapon of India's epics — the hero's weapon. Object + setting both serve Pillar 1.
- **A built-in bookend.** The relic the player sees *first* is the relic they *earn last*.

## The emotional engine: a duality

The screen runs on a deliberate tension, and every art/lighting choice serves it:

- **The temple carries the warmth** — serene, safe, hopeful, the strange ache of beauty a real mandir gives you. *Not* cold, not scary.
- **The bow carries the mystery** — alluring, charged, *calling* to be claimed, like forbidden fruit.

**Guiding rule:** the *setting* stays warm and grounded; the *bow* is the one thing that feels slightly **other** — a touch more luminous, more alive, than everything around it. Mystery comes from the object, never from making the room cold.

---

## Locked decisions

| Decision | Choice |
|---|---|
| **Focus** | The relic bow on a garlanded altar / weapon-stand in the temple sanctum. |
| **Title text** | None. Only a faint, pulsing *"press any key"* prompt. |
| **Deity in frame** | No murti/idol — just the relic and the room (keeps focus undivided; no premature lore commitment). |
| **Composition** | Symmetrical, **frame-in-frame**: two foreground pillars close to camera framing a brighter center. Adds depth (no parallax needed) and a dark frame around the glowing relic. |
| **Camera** | Symmetrical, **near eye-level with a slight downward tilt** — reveals the warm spread of lamps/offerings on the altar and floor, while the bow still stands proud and central. |
| **Feeling** | The warm/safe/serene temple + one alluring, mysterious, *calling* relic — the duality above. |
| **Breathing elements** | Diyas flicker; incense smoke drifts (our "mist"); motes/embers rise; the bow's glow slowly *breathes*. (Diyas = life; bow = power/awareness.) |
| **Art register** | **HD-2D detailed pixel art** (the title may be richer than the battle map — that's normal and fine). |
| **Cohesion rule** | Cohesion is shared *visual language* (palette, world, how things are drawn/lit), **not** matching pixel density. Title can out-detail gameplay. |
| **Pipeline** | Generate + assemble in **PixelLab Pixelorama** (the in-browser Pixelorama editor with PixelLab's AI tools) → light & animate in **Unity** (URP 2D lights + bloom). Lighting/atmosphere is added in-engine, not hand-painted per asset. |
| **Virtual resolution** | Build at **480×270**, integer-scale **×4** to 1920×1080. (Alt: 640×360 ×3 for more detail.) |

## Deferred / still open

- **Bow levitation** — a nice-to-have animation experiment, not a priority. Parked.
- **Exact palette** — to be locked from the first PixelLab style-anchor generation.
- **The bow's final visual design** — recurved dhanush, ornate golden limbs, leather grip; specifics settle during generation.
- **Divine-glow treatment** — handled live in Unity (a breathing halo + edge-light + a glow hue slightly *purer* than the warm lamps so it reads divine). Tuned by eye, not decided on paper.
- **Virtual-res final pick** (480×270 vs 640×360) — confirm when we see the first asset at scale.

## Reference touchstones

- **Character / portrait personality:** Fire Emblem GBA (expression, character, even at small canvas). *(Relevant later, not for this screen.)*
- **Environment / objects / lighting / atmosphere:** Octopath Traveler, Sea of Stars, Chained Echoes, Blasphemous.

Note on what makes those references *sing*: it's mostly **lighting** (real-time lights + bloom + fog over fairly clean sprites), which is exactly why our pipeline does the atmosphere in Unity rather than baking it into each pixel asset.
