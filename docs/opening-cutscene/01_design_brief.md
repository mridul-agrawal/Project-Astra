# Opening Cutscene — Design Brief (Scene 02)

*The post-title narrative that bridges the title screen and Map 1. This is a **living brief** — the direction below is proposed (and the designer is currently leaning toward it), but the decisions in §4 are **not locked yet**. Annotate freely.*

*Sources: `docs/project_astra_prototype_design 001.md` → "Scene 02 — Opening Narrative Setup", "Narrative Presentation Conventions", "Design Intent: Early Segment". Pipeline conventions follow `docs/title-screen/`.*

---

## What this segment is

- Plays **right after "press any key"** on the title screen; ends by **cutting into the Map 1 battle map**. It's the connective tissue between them.
- Runs on **Option A** — the design doc's leading option. (Option B, an Elden-Ring-style lore cinematic, is **deferred** until the full lore exists and funding/writers are in place.)
- Uses the two global **Narrative Presentation Conventions**: **portrait-to-portrait dialogue** (normal, lower-intensity) and **bespoke full-screen pixel-art stills** (high-intensity, narration or dialogue laid over them).
- Lives inside the tight **≤15-minute** title→end-of-Map-1 budget. Model: Fire Emblem Blazing Sword's opening — **~4–5 beats, then drop straight into play.** Keep it short and flowing.

---

## 1. Narrative — what has to happen

Beat-by-beat (Option A):

1. **Calm open** — the protagonist alone in the forest at dusk, foraging/hunting for dinner, unaware anything is wrong. Establishes who she is: a *local forest-warrior*, not royalty.
2. **The alarm** — distant screams; she bolts back toward the village.
3. **The reveal** — she arrives to the village under attack: smoke, fallen villagers, two Rakshasas advancing on the homes. The emotional gut-punch.
4. **The child** — she finds a fleeing child who tells her what's happening, *and that the temple is under attack too*.
5. **The priority call** — the temple matters, but the villagers here are in immediate danger; she resolves to save them first, then head to the temple. (Seeds the Map 1 → Map 2 order.)
6. **The gatekeeper thread** — she asks where the gatekeeper (elephant-mounted, by name) is; the child says he's fighting at the temple but losing. (Plants Map 2's stakes and a key character.)
7. **Hard cut into Map 1** — the battle map: 2–3 Rakshasas attacking the village.

**What the segment must accomplish:**
- Introduce the protagonist and her nature/origin.
- Establish the Rakshasa threat and make the stakes *personal* (her home, her people).
- Seed the two-map objective chain (save villagers → reach temple).
- Plant the threads: the child, the gatekeeper, the temple/priest, the relic (implicitly).
- Set ancient-India identity with a cinematic, "a mission is running" tone — not a tutorial, not a book.
- Stay short and flow straight into Map 1.

**Deferred (per the design doc):** exact dialogue lines, which beats become stills, audio direction. **Option A itself is unconfirmed.**

---

## 2. Art assets needed

Two pipelines, same split as elsewhere: **pixel-art** (PixelLab → Pixelorama → Unity) for portraits, stills, and backdrops; **uGUI/Figma** for the dialogue chrome.

**A. Character portraits** — FE-GBA style, with expression sets:
- **Protagonist** — ~4 expressions: calm, alarmed, anguished, determined.
- **The Child** — ~2 expressions: terrified/crying, urgent/pleading.
- *Not needed here:* gatekeeper, priest, Rakshasa portraits (the gatekeeper is only spoken about).

**B. High-intensity full-screen stills** — bespoke, one-off pixel-art. Proposed **2–3**:
- **The village under attack** — the centerpiece reveal (fire, smoke, fallen villagers, Rakshasas advancing). *Required.*
- *(Optional)* Protagonist foraging in the dusk woods — peaceful establishing shot.
- *(Optional)* The temple burning in the distance / a Rakshasa silhouette — to plant Map 2.

**C. Dialogue backdrops** — for portrait-mode beats:
- A **village-street-amid-attack** backdrop behind the protagonist↔child talk (can be a dimmed/blurred derivative of the attack still).
- A forest backdrop *only if* the intro is portrait dialogue rather than a still.

**D. Dialogue UI chrome** (uGUI/Figma): text box (culturally styled), speaker name plate, left/right portrait frames, blinking continue indicator, letterbox bars.

**E. Transitions / VFX** (Unity): black fades & cross-fades, fire/smoke/ember particles over the attack still, subtle pan/zoom (Ken Burns) on stills, optional flash/shake on impact.

**F. Audio** — *DEFERRED (polish pass):* forest ambience, distant screams, a tense/sorrowful music cue.

**Proposed presentation split** (answers §4.2, still open):
- **Beat 1 (woods):** establishing still + her inner-voice/narration.
- **Beat 2 (alarm/run):** a transition (sound + fade), not its own still.
- **Beat 3 (reveal):** the **high-intensity full-screen still** + reaction/narration.
- **Beats 4–6 (child, decision, gatekeeper):** **portrait-to-portrait dialogue** over the village backdrop.
- **Beat 7:** cut to Map 1.

---

## 3. UI experience — how it should feel

- **Cinematic and flowing** — never a paused wall of text. The opening of a *film*, layering story and identity (the doc's teach-through-narrative principle applied to tone).
- **Two registers, switched filmically:** full-screen stills with narration/dialogue *overlaid* for the high-intensity beats (the attack); the portrait-to-portrait box for conversation (the child). Fades between them.
- **Text reveals line-by-line** (typewriter); **portraits swap expression** per emotional beat (FE GBA reference).
- **An emotional arc the UI must carry:** warm/calm (woods) → sharp alarm (screams) → dread (the attack still) → steel/resolve (the decision). The shift *to* a full-screen still for the gut-punch, plus pacing (and later a music swell), sells each step.
- **Player-paced** — CONFIRM advances; authored beats, but she's *moving through* a story, not watching a locked cutscene. Offer a **skip/auto** for replays.
- **Elegant, unobtrusive, culturally-styled chrome** so the art and words carry it — consistent with the HD-2D warm identity.
- **Tight, and seamless into Map 1** — the cut to the battle map should feel like the story *continuing* (the objective is already seeded), so Map 1's first moments feel motivated rather than abrupt.

---

## 4. Open decisions to lock (before any assets)

1. **Confirm Option A** (vs the deferred Option B).
2. **The still-vs-portrait split** — which beats are full-screen stills vs portrait dialogue (drives the asset count). *Proposed split in §2.*
3. **How many stills** — lean: 2–3.
4. **Working names** — the protagonist (not "Indravati") and the gatekeeper (the child names him).
5. **The woods intro** — narrated/still, or a tiny *playable* moment?
6. **Narration voice over stills** — omniscient narration, or only her inner voice / spoken dialogue?

---

## Status & notes

- **Status:** brief only; nothing built; decisions in §4 pending.
- **Tech reuse:** there is very likely an existing reusable **`DialogueSequencePlayer`** (built for the Lord-permadeath death sequence). This opening should **reuse/extend** it rather than build a dialogue system from scratch — *verify it's still in the project before relying on it.*
- **Next:** once §4 is decided, this can split into an asset list + production guide (mirroring `docs/title-screen/`).
