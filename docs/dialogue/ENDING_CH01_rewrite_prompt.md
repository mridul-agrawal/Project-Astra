# Prompt — Rewrite the Map 1 ending dialogue (ENDING_CH01)

Hand everything below the line to another LLM. It is self-contained (the model needs no
prior knowledge of the project). The goal: a complete, restrained post-battle dialogue
sequence to replace the current over-written one.

---

You are a senior game-narrative writer. Your trademark is **restraint** — you trust the
player and never let a line *perform* emotion. You are writing one short dialogue sequence
for a story-driven tactical RPG.

## The game
**Project Astra** is a Fire Emblem-style tactical RPG rooted in the world of India's ancient
epics (the Mahabharata / Ramayana era). Grounded myth, not high fantasy. The mood is
earthy and human: villages, jungle, mountains, dharma, real stakes.

## Where we are (Map 1: "The Bridge at Suvarnapur")
A small jungle-and-mountain village. **Rakshasas** — non-human raiders — attacked it.
**Aranya**, a local forest-warrior and archer (ragged, self-taught, not royally trained —
she hunts these woods), arrived and fought them off, holding the village's single footbridge.

What the player carries into this scene:
- In the opening, a villager was cut down *before* Aranya could reach them — in front of a
  child whose mother told them to run. Aranya's vow then was plain: **"They take no one else."**
- **Gajen**, the village gatekeeper (a lance-fighter who rides a small mountain elephant), is
  still fighting Rakshasas up at the **temple**, which hides a powerful relic. Most of the
  Rakshasa force was never after the village — it's headed for that temple (the next map).

## The scene to write
The battle is **just over**. It's **dusk**. The village is saved — for now. This is the quiet
beat after the fight. Its job: let the win land *quietly*, acknowledge the cost without
announcing it, and turn gently toward what's next (north / the temple). A soft hook — **not**
a shouted cliffhanger.

## Who can speak (use these speaker IDs exactly)
- **NARRATOR** — sparse, neutral narration. No portrait, no name shown. Use rarely.
- **PROTAGONIST** — Aranya. Practical, quiet, plainspoken. A hunter, not a poet. Do not
  give her grand or formal speech.
- **CHILD** — the survivor she comforted. Optional; use only if it earns a real human beat.

Do **not** invent other speakers.

## VOICE & TONE — read this twice; it matters more than anything else
Write the way real, tired people actually speak and think after something hard. Plain words.
Short lines. One thought per line. Concrete over abstract — show a small real detail instead
of stating a feeling. **Never name the emotion. Never spell out the callback. Trust the player.**
Fewer lines is better than more. If a line isn't doing real work, cut it.

**Calibrate against these examples:**

❌ Too forceful / pretentious (this is the current draft — do NOT write like this):
- "The bridge held. By dusk, the smoke over Suvarnapur was from hearth-fires again."
- "One raid turned aside. But they came from the northern dark, and the dark is deep."
- "Rest tonight, Suvarnapur. Tomorrow, I follow their trail north."
- "— To be continued —"

✅ The register to aim for (plain, true, understated):
- "The bridge held. No one else was taken."
- "By dark, the children were back outside."
- "Gajen's still at the temple. I should go."

## Hard rules (do not break)
- No similes or metaphors that announce themselves; no "the dark is deep"-style portent.
- No image inversions (e.g. raid-smoke → hearth-smoke). No loaded adverbs ("only", "again").
- No em-dashes used for drama. No addressing the village by name as a flourish
  ("Rest tonight, Suvarnapur").
- No grand abstractions, no restating the objective heroically, no generic
  "— To be continued —".
- Every line must read naturally if spoken aloud by a tired person. If it sounds "written," redo it.
- Each line must fit a two-line on-screen box: aim for **under ~80 characters**, ideally less.

## Length & shape
- Target **4–6 lines total** (fewer is fine). Open on the quiet aftermath, hold one honest
  human beat, and end with a soft, understated turn toward the north/temple.
- It is acceptable — even good — to end on a small concrete image rather than a line of speech.

## Output format
Return the sequence as a table with these columns, in play order:

| # | Speaker | Expression | Portrait | Line |
|---|---------|------------|----------|------|

- **Expression** — choose from: Neutral, Happy, Sad, Angry, Surprised, Determined, Afraid.
- **Portrait** — position + facing. Portraits face *inward* toward the text box:
  Left position → faces Right; Right position → faces Left; Center → faces Left.
  For NARRATOR use **None** (no portrait).
  Format the cell like `Left / Right` (position / facing), or `None` for narration.
- **Line** — the dialogue text only.

After the table, list just the bare lines (speaker: line) so they're easy to read on their own.

Finally, add **2 alternates for the closing line only**, in the same restrained register, so a
final choice can be made.
