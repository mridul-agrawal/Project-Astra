# Project Astra — UI/UX Direction Candidates (Reference)

> **Status:** Reference document. Not a locked spec.
> **Generated:** 2026-04-14, during a focused UI/UX pass through the `creative-director` skill.
> **Purpose:** Captures the full detailed design philosophy of every candidate generated in the UI/UX brainstorming session — including the 3 warmups, the 3 shortlisted directions, and the 8 directions that were cut at various stages. Prior to this document, only the shortlisted candidates were presented in full detail; the cut candidates had only brief one-line summaries. This doc expands every candidate to the same level of detail so the full search space can be referenced, reconsidered, or revived in future sessions.

---

## What this document is

Project Astra's UI/UX direction was explored through a structured creative-director process (Phase 1 intake → Phase 2 insight → Phase 3 ideation → Phase 4 evaluation + refinement → Phase 5 articulation). Phase 3 generated 14 candidate directions. Of those, 3 were skill-mandated warmups, 5 reached Phase 4 scoring, 2 were merged into one final recommendation, and 6 were cut at the ideation phase for various reasons.

The skill's final presentation in Phase 5 only showed the top 3. This reference document preserves **all 14** at comparable depth, so no candidate is invisible for future reconsideration.

---

## The load-bearing insight (Phase 2)

Before ideation began, three tensions were identified:

1. **Cultural tension.** Modern game UI has been trained by Apple and mobile design into *frictionless* as a first-order value. Snappy response, satisfying pop feedback, "it feels good" as the test. But Project Astra's spine is about the weight of decisions and the impossibility of perfect knowledge. A UI that gives candy on every click actively contradicts the game's thesis. Yet a slow UI is unusable. **Tension: deliberate without broken.**

2. **Category (tactical RPG) tension.** The genre's core contract is information density — a single combat forecast shows 12+ numbers. Players want this data; they will revolt if you hide it. But the game's emotional promise is that seeing clearly is always partial — the Astras destroy generations because their wielders thought they saw enough. **Tension: ruthless clarity AND epistemic weight.**

3. **Human tension.** Players who love TRPGs want the UI to disappear so they can "read the battlefield like a language." That requires transparency and speed. But they also want the UI to be something worth looking at — something that rewards their attention, makes them linger. These wants are usually at war: transparent UIs are austere, beautiful UIs are slow. **Tension: transparent when used, beautiful when looked at.**

### Insight (one sentence, load-bearing)

> *Tactics players want a UI that is transparent-when-used and beautiful-when-looked-at, clear-with-information and heavy-with-meaning, fast-to-read and slow-to-accept — but the genre has always forced them to pick one side, because nobody designed a UI where receiving the information IS bearing its weight. Project Astra's UI must be the first one that refuses the pick: the readout and the ritual are the same act.*

Every candidate below is a different attempt to operationalize this insight into interaction grammar.

---

## Scoring summary

For candidates that reached Phase 4 evaluation:

| # | Candidate | Status | Weighted | HumanKind | Grey |
|---|---|---|---|---|---|
| 1 | Glass Sutra | Warmup — cut at ideation | — | — | — |
| 2 | Scroll & Banner | Warmup — cut at ideation | — | — | — |
| 3 | Painted Mural | Warmup — cut at ideation | — | — | — |
| 4 | The Two Breaths | Presented (merged into Second Breath) | 8.85 → 9.05 | 9 | 9 |
| 5 | The Council Table | Presented | 8.65 | 9 | 8 |
| 6 | The War Registry | Cut at ideation | — | — | — |
| 7 | The Astronomer's Instrument | Cut at scoring | 7.30 | 6 | 7 |
| 8 | The Mudra Menu | Cut at scoring | 7.50 | 7 | 7 |
| 9 | The Oracle Bones | Cut at ideation | — | — | — |
| 10 | The Letter from the Field | Cut at scoring | 7.50 | 8 | 7 |
| 11 | The Temple Bell | Merged into Second Breath | 8.60 | 8 | 8 |
| 12 | The Council Whispers | Presented | 8.35 | 9 | 8 |
| 13 | The Constellation Map | Cut at ideation | — | — | — |
| 14 | The Court Audience | Cut at ideation | — | — | — |

**Final top 3 presented:** The Second Breath (merged #4+#11, scored 9.05), The Council Table (#5, 8.65), The Council Whispers (#12, 8.35).

---

## Family observation

Unlike prior broader-art-direction rounds, the UI/UX candidates here fall into three distinct grammar families that are genuinely different kinds of UI thinking:

- **Temporal grammar** — *when* the UI responds. (Two Breaths, Temple Bell, Second Breath)
- **Diegetic / material grammar** — *what the UI is made of*. (Scroll & Banner, Council Table, War Registry, Oracle Bones, Letter from the Field, Court Audience)
- **Content / voice grammar** — *what the UI says alongside the numbers*. (Council Whispers)
- **Abstract / non-diegetic grammar** — *the UI as pure thought or pure geometry*. (Glass Sutra, Astronomer's Instrument, Constellation Map, Mudra Menu)
- **Compositional grammar** — *the UI as a single continuous visual system*. (Painted Mural)

---

# The 14 candidates

## #1 — Glass Sutra

**Handle:** SUTRA
**Status:** Warmup (first of three conventional starting ideas). Cut at ideation.

### Core design philosophy

Glass Sutra is a UI built on **radical non-diegetic lightness**. The UI is deliberately NOT an object from the world's fiction — no scrolls, no letters, no carved tablets, no physical materials. Every panel, menu, and overlay floats as an abstract translucent plane above the game world, ungrounded in any material the world contains. The design commitment is **lightness as a form of gravity** — an argument that a UI can carry tactical and moral weight through extreme visual restraint rather than physical presence.

### How it feels

- The UI is visually weightless but functionally precise.
- Information is delivered by pure typography, geometric linework, and faint translucent fields — no backgrounds, no frames, no ornament.
- The game world is always visible through the UI; the UI is never opaque enough to block the world behind it.
- Every element feels dismissible with a single breath, and that dismissibility is itself the weight.
- Interaction grammar is instant, silent, meditative — no bouncy feedback, no candy satisfaction. Selecting a menu item feels like thinking, not touching.

### Tension explored

In a game whose narrative is philosophically weighty, can a UI that refuses physicality and ornament still communicate that weight? The thesis: yes — through a clarity so severe that the *absence* of everything becomes its own presence.

### Why it was cut at ideation

Flagged as warmup #1 per the skill's rule that the first three ideas are conventional starting points. Pattern-matches to mobile-gacha / glass-panel UI defaults, which risks reading as generic "modern UI" rather than project-specific. Fails the Specificity Test — replace "Project Astra" with any other game and the direction still works. The best version of Glass Sutra would need to find a way to make its emptiness feel like Project Astra's emptiness specifically, not just like any minimalist UI.

### Best showcase surface

Battle map HUD during active tactical play — where multiple floating overlay elements (movement range, attack range, threat zone, tile info, unit card, turn counter) must coexist on screen without grounding and remain legible as a composition.

---

## #2 — Scroll & Banner

**Handle:** UNFURLED
**Status:** Warmup (second of three). Cut at ideation.

### Core design philosophy

Scroll & Banner is a UI built on **physical manuscript objects**. Every major UI surface is a literal object the player sees unrolling, unfurling, or dropping into view: menus are scrolls that extend from their tied ends, dialogue boxes are banners that drop from above, notification prompts are small hanging cloths that fall into view. The UI is fully diegetic — made of tactile materials the world of the game actually contains: silk, parchment, cord, weights, seals, ink.

Every UI surface has: a visible edge (the border of the scroll or cloth), a visible binding element (cord, tied ends, hanging weights, a seal), and a felt moment of arriving and departing as a physical act.

### How it feels

- The UI *arrives* — it is not simply present.
- Every surface has a sense of physical rhythm of appearance and dismissal.
- Materials are legible as materials: silk, parchment, cord, ink.
- Text sits on its object as if physically inked onto the material.
- The game world is always partially visible around the UI objects because the objects are finite in size and shape, not screen-spanning rectangles.

### Tension explored

Most game UIs are abstract overlays that pretend the world holds the interface invisibly. Scroll & Banner argues the opposite — that making every UI surface an object the player watches arrive grounds the game in a felt reality and makes every menu a small ceremony. The weight comes from physicality; the majesty comes from the craft of the objects.

### Why it was cut at ideation

Dramatic scroll-unroll animation for every menu is slow in practice, hurting tactical readability where the player needs instant data access. Feels like a one-trick gimmick that exhausts after the first 2 hours. Core problem: tactical RPG UI must be fast, and physical objects are inherently slower than abstract overlays. Would work beautifully for narrative surfaces (dialogue, chapter intros) but fails at the high-frequency tactical surfaces (forecasts, unit info, action menus) where the player lives most of their session.

### Best showcase surface

A dialogue scene during a narrative beat — where slow arrival is appropriate and the physicality adds ceremony. Would not work for combat forecasts or rapid tactical panels.

---

## #3 — Painted Mural

**Handle:** MURAL
**Status:** Warmup (third of three). Cut at ideation.

### Core design philosophy

Painted Mural is a UI built on **continuous narrative painting**. Every UI screen in the game is a section of one enormous painted mural that extends horizontally across the game's lifespan. Transitions between screens — main menu to world map to chapter intro to battle — are camera pans across the mural, not cuts. The mural is the game. The UI is what the player sees of the mural when their attention is focused on a particular region of it.

This is **diegetic once removed**: the UI is not objects within the world (as in Scroll & Banner), but the world itself is a painting, and the UI is how the player navigates the painting. The mural is dense with figuration where narrative beats happen, sparser in connecting regions, but every inch is painted by the same hand with consistent style.

### How it feels

- Transitions are camera pans across a continuous painted surface, not cuts.
- Every "screen" is a section of a painted panel with visible continuity to adjacent sections.
- The painting style is the entire art direction of the UI — brushwork, pigment, figure posture, compositional rhythm all consistent.
- Chapter intros and chapter clears feel like literal chapters of a painted narrative — panels of the mural the player has arrived at.
- The player's progress through the game is legible as progress across the mural.

### Tension explored

Games usually treat screens as modular containers that get swapped when the player navigates. Painted Mural argues for continuity — a game about cyclical time, moral continuity, and the weight of consequence should have a UI that is spatially continuous. Moving from one screen to the next should feel like walking along a temple wall and stopping in front of a new panel, not like clicking to load a new page.

### Why it was cut at ideation

Too close to prior broader-art-direction rounds' "document + ornament" emotional family. Also: the warmup mark is structural — it's the kind of idea the skill reaches for first when asked "painterly Indian-inspired UI", so it fails novelty on the Specificity Test. The painted-mural aesthetic is also difficult to hold across every UI surface — tactical battle UIs resist being "painted" because they need clean geometric readability. The direction works beautifully for transition screens but struggles at the dense tactical surfaces.

### Best showcase surface

Chapter transition screens — the place where the mural's continuous-pan philosophy is most compelling. Also: main menu, world map, chapter intros, chapter clears.

---

## #4 — The Two Breaths

**Handle:** BREATH
**Status:** Shortlisted for Phase 4. **Merged with #11 (Temple Bell) into "The Second Breath"** — the final top-ranked recommendation scored 9.05.

### Core design philosophy

The Two Breaths is a UI built on **dual-tempo interaction**. Every UI surface has two visual states that occupy the same panel but appear at different moments:

- **First breath** — instant, clean, high-contrast, purely informational. All the numbers, all the stats, everything the player needs to act on, delivered with zero delay.
- **Second breath** — settles in ~400–600ms later if the player lingers. Adds visual weight, ornament, context, and meaning around the same data.

### How it feels

- Information is instant; weight is earned by lingering.
- The second-breath state is a *superset* of the first — no data is hidden in the first breath, only unadorned. The second breath adds ornament, context, and qualitative depth around the same numbers.
- Fast players live in the first breath and experience the UI as pure data delivery. Immersive players linger on the second breath and experience the UI as a slow ritual. Neither is penalized.
- The transition between the two states has a visible rhythm — a soft settle, a fade-in of additional elements, a typographic shift.

### Tension explored

Tactical RPGs face a contradiction — players want information fast (to act quickly) AND they want the weight of their decisions to register (to feel what they're doing). Most games pick one. Two Breaths picks both by **separating them in time**. The first breath is for acting; the second breath is for dwelling.

### Why it was merged

On its own, Two Breaths answers the speed-vs-weight tension but doesn't give the game a felt rhythm across interactions. When merged with Temple Bell during PASS 2 refinement, the second-breath timing locks to a shared world rhythm so every settle lands on the same heartbeat — the beat becomes the game's pulse and every interaction participates in it. The merged version scored 9.05 (from 8.85 for Two Breaths alone) and became the final recommendation.

### Best showcase surface

Combat forecast — where the dual-tempo structure is most stress-tested. The player needs numbers in under a second to decide if the attack is a good trade, AND the weight of a potential death hits hardest here.

---

## #5 — The Council Table

**Handle:** TABLE
**Status:** Shortlisted for Phase 4. **Presented as candidate #2** (weighted 8.65, HumanKind 9, Grey 8).

### Core design philosophy

The Council Table is a UI built on **physical objects arranged on a war council's table**. The entire battlefield UI is rendered as if the player is sitting at a war council and every menu, panel, and overlay is a physical object being consulted on that table. The cursor is a fingertip. The turn end is a gavel strike. The combat forecast is a small cloth map laid down between the player and the enemy. Items are pulled from a wooden chest at the edge of the table. Unit info is a scroll being laid down.

The player IS at the table. Every action has the felt-physicality of moving a real object. The UI is not a layer above the world — it's a set of objects *from* the world that the player is arranging to make decisions.

### How it feels

- Every action is an object-manipulation. Selection = placing a finger on a piece. Menu navigation = sliding the finger between objects. Confirm = pressing down. Cancel = pulling the finger back.
- The motion grammar is deliberate physical gesture, not abstract click.
- The player's emotional position aligns with the protagonist's felt experience: sitting at the council table, bearing the weight of command.

### Tension explored

The player's position and the protagonist's position become one. You are not a floating commander looking down at the battlefield — you are a young prince at a table, arranging figures and objects and making choices that feel, physically, like arranging weights.

### Execution discipline (PASS 2 refinement)

Use a **limited vocabulary of 6 recurring object types**, not a unique object per UI surface:
1. **The scroll** — unrolls for stat and information display
2. **The cloth map** — lays flat for range / combat forecast overlays
3. **The carved chest** — opens for inventory / item display
4. **The brass gavel** — strikes for turn end / confirm actions
5. **The ivory marker** — represents units on the battle map
6. **The brass bell** — signals state changes (alerts, phase transitions)

Every UI surface uses one or more of these 6 objects. The player learns the object vocabulary in chapter 1 and reads it fluently thereafter. This bounds the authoring cost to a fixed library.

### Honest weakness

The diegetic commitment requires every new UI feature in the future to fit the object vocabulary. If the team ships a feature that doesn't naturally fit as a scroll / cloth / chest / gavel / ivory / bell, there's a crisis — do we break the vocabulary or invent a 7th object type? This discipline has to be held across the project.

### Best showcase surface

A full battle map scene with multiple tabletop objects visible at once — scrolls, cloth maps, ivory markers, the brass bell — all coexisting on the council table.

---

## #6 — The War Registry

**Handle:** REGISTRY
**Status:** Cut at ideation.

### Core design philosophy

The War Registry is a UI rendered as a living scribe's manuscript where the scribe is **writing the UI in front of the player in response to their actions**. Open a unit info panel: a faint line of ink writes Tana's stats onto the page as you watch. Open a combat forecast: the scribe writes the forecast in two columns. Select an action: the scribe marks it with a small sigil. Every UI event is an act of ink appearing, as if an invisible hand is recording the war in real time.

This is NOT about the ledger being the game's metaphor. It's about the INTERACTION GRAMMAR being an act of **co-authorship** — the player decides, and the scribe records. The weight comes from watching your choices become permanent ink.

### How it feels

- Every menu opens with a sense of being witnessed and recorded.
- The player's choices are not ephemeral — they are being transcribed by an invisible witness who is making them permanent.
- The scribe's hand is implied but rarely shown; the player sees only the ink as it appears.
- Over time, the player starts to feel that the game is keeping a record of them, and that the record will outlive the decisions made in it.

### Tension explored

Tactical choices usually feel ephemeral in games — made, committed, forgotten. The War Registry makes them feel permanent by showing them being written into a record that will outlive the decision. This aligns with the game's Vedantic theme that every human's actions ripple across generations.

### Why it was cut at ideation

Too close to a prior broader-art-direction round's rejected "Ledger" direction. The ink-being-written metaphor and the "scribe as implied author" overlap significantly with the Ledger's core idea. Reusing this direction would trigger a "you've seen this before" response. Cut to preserve the discipline of "do not iterate prior direction concepts."

### Best showcase surface

Chapter clear / results screen — where the scribe writes the outcomes of the chapter into a permanent record that the player reads after the battle, with visible ink marks for each unit's fate.

---

## #7 — The Astronomer's Instrument

**Handle:** INSTRUMENT
**Status:** Reached Phase 4. Cut at scoring (7.30 / HK 6 / Grey 7).

### Core design philosophy

The Astronomer's Instrument is a UI designed as if it's a **Jantar-Mantar-scale astronomical reading device**. Menus are concentric dials. Combat forecasts are alignment readings between two celestial bodies — the attacker and defender as stars, the damage number as a conjunction. The cursor is a sight-line. You don't pick menu items — you align readings. Every interaction is a calibration between heavenly bodies that represent earthly events.

### How it feels

- Every interaction is a calibration. The player is not commanding armies; they are reading cosmic consequences.
- The weight comes from the sense that the game's events are being measured against something vast and indifferent — the same stars that have seen every previous yuga.
- There is a meditative, timeless quality to the interaction — you are not reacting to the battlefield, you are measuring it.

### Tension explored

The genre's default is "commander looking down at the battlefield." The Astronomer's Instrument reframes that position as "astronomer reading the stars" — a fundamentally different emotional position that distances the player from the action and makes them feel like they are measuring fate rather than imposing it.

### Why it was cut at scoring

**Cosmic detachment is the wrong emotion for this project.** Project Astra is about intimate moral weight — the protagonist must FEEL the consequences of his choices on the people below. An astronomer's position is distant, measuring, cool. It captures "the weight of time" but misses "the weight of the crown." The direction makes the player feel too far from the consequences.

Also: aligning dials is a slower, more ritualistic interaction than tactical RPGs can afford across hundreds of actions per battle. Every menu would take 2–3 times longer than a conventional click, and players would resent it.

### Best showcase surface

World map / route selection between chapters — where the "reading cosmic consequences" framing fits the slow strategy-layer pace and doesn't impact the frequent tactical surfaces.

---

## #8 — The Mudra Menu

**Handle:** MUDRA
**Status:** Reached Phase 4. Cut at scoring (7.50 / HK 7 / Grey 7).

### Core design philosophy

The Mudra Menu is a UI built on **hand-gesture-based interaction vocabulary**. Every menu option is a named mudra — a symbolic hand position. The action menu isn't "Attack / Item / Wait" — it's three small carved mudras the player selects. The cursor is a hand. Confirming is closing the hand on the choice. Cancelling is pulling the hand back.

Learning the menu is like learning a ritual vocabulary. Once learned, the mudras are READ in a glance — but each carries a weighted symbolic meaning the player has internalized.

### How it feels

- Every action is a small sacred gesture. The mudras are ritual positions the protagonist (and therefore the player) is invoking.
- Over time the player learns to read the mudras fluently; at that point each click becomes a small ceremonial act rather than a mechanical selection.
- The weight is in the sacred vocabulary itself. Every tactical choice is also a dharmic one.

### Tension explored

Tactical RPG menus are usually verbose (text labels) or iconic (flat pictograms). The Mudra Menu proposes that the menu itself is ritual — the player is not choosing between options but invoking between gestures. Every click is a small dharmic act.

### Why it was cut at scoring

**Doesn't scale to dense information display.** Combat forecasts, unit info panels, inventory menus all need to show a LOT of numerical data — you can't replace stats with mudras. The direction works for small action menus (3–6 options) but fails the scalability test across the full UI surface inventory.

Also: players need the first 3 hours of the game to learn the mudra vocabulary before the payoff lands, which is a significant cost for the early-game experience. New players would be confused by icons they don't yet understand.

### Salvage note

Could be absorbed into a winning direction as the **action-menu treatment specifically** — action menus use mudras, while other panels use whatever vocabulary the winning direction provides. Stands as a craft discipline, not a top-level direction.

### Best showcase surface

Action menu after unit movement — where the 4–6 option vocabulary fits the mudra format perfectly.

---

## #9 — The Oracle Bones

**Handle:** BONES
**Status:** Cut at ideation.

### Core design philosophy

The Oracle Bones is a UI where every menu is a **divination**. Options appear as if cast from oracle bones (or astragali, or dice of a celestial game). The player reads the positions of the cast bones, not labels. Combat forecast is a "cast" of bones showing the outcome ranges. Every choice is framed as a reading rather than a command.

### How it feels

- Every choice feels like a reading rather than a command.
- The player is interpreting what the game is showing them — reading bones, not selecting items.
- There's a faint sense that the outcomes are not fully under the player's control, that you're asking the cosmos to reveal what will happen rather than deciding what will happen.

### Tension explored

Tactical RPGs usually give the player total agency over their decisions. The Oracle Bones introduces a quiet uncertainty — the bones have fallen, you are reading them, and the reading feels like discovery more than decision. This subtly matches the Vedantic idea that the player's perception of their own agency is itself a bubble.

### Why it was cut at ideation

- **Tips into mystical hokum and breaks tactical readability.** The player needs clear numerical data (hit rate, damage, outcome) to make informed tactical decisions, not symbolic bones to interpret. The moment the player has to "read" a menu instead of scan it, the genre's speed contract is broken.
- **Philosophical contradiction.** The implied determinism ("the bones have already fallen") subtly contradicts the game's core message that the player's choices matter morally. It suggests the moral outcomes are pre-determined, which is the wrong philosophical tone for the project — the game wants the player to feel that they COULD have chosen differently.

### Best showcase surface

Combat forecast as cast bones — but cut for the above reasons.

---

## #10 — The Letter from the Field

**Handle:** DISPATCH
**Status:** Reached Phase 4. Cut at scoring (7.50 / HK 8 / Grey 7).

### Core design philosophy

The Letter from the Field is a UI where **every major surface is framed as correspondence** from someone in the war. A unit info panel is a letter someone wrote to the court about Tana: *"she has served with the Iron Lance since the third month, and her arm has not weakened though her earring was lost at the river crossing."* A combat forecast is a short military dispatch: *"enemy archer, twelve paces east, lance strike yields 31 damage at 140 hit."* A chapter intro is a proclamation from the king. A shop menu is an invoice from the quartermaster.

The player is reading the war through the eyes of the people who are in it, and the UI is the correspondence they receive.

### How it feels

- The game's information is always filtered through a human voice.
- You are not omnisciently observing the battlefield — you are receiving dispatches from the people who are actually there.
- The weight comes from the sense that someone wrote this to you, and that somewhere a hand is waiting for your response.
- Over hours, the player starts to feel the CHARACTER of the dispatcher behind each piece of information — a weary quartermaster, a precise scout, a worried priest.

### Tension explored

TRPG UI is usually abstract and impersonal — numbers, stats, grids. The Letter from the Field makes it personal — every piece of information is narrated by someone who cares about the outcome. The player never reads a stat; they read what someone wrote about a stat.

### Why it was cut at scoring

**Prose-parsing fights tactical readability.** Combat forecasts need to be SCANNED instantly — players look at a number like "140 hit" and accept or reject in half a second. Prose framing adds parse time. Even with a "quick-read row" plus "letter margin" fallback, the direction's core claim — that the UI is epistolary — fights the speed the genre requires.

The direction works beautifully for narrative surfaces (dialogue, chapter intros, between-chapter letters) but fails at the high-frequency tactical surfaces where TRPG players live.

### Salvage note

Could be a **sub-element under another direction** — between-chapter letters and narrative interludes could use epistolary framing while the tactical UI uses whichever main direction wins.

### Best showcase surface

A dialogue / correspondence scene between chapters — where prose parsing is appropriate and the epistolary framing adds emotional weight. Also: chapter intros, world map between chapters, strategic planning screens.

---

## #11 — The Temple Bell

**Handle:** BELL
**Status:** Shortlisted for Phase 4. **Merged with #4 (The Two Breaths) into "The Second Breath"** — the final top-ranked recommendation.

### Core design philosophy

The Temple Bell is a UI built around a **consistent shared rhythm**. Every interaction has a two-beat cadence: the action (click / select), then the settling (the ornament or feedback or readout completes on the next beat). The timing is precisely 300–500ms — fast enough to feel instant, slow enough to feel measured.

The game has a subtle ambient rhythm (not music, but a low mix-element — a distant temple bell at the edge of hearing, or a water clock, or a slow drum) and **every interaction in the game lands on the rhythm**. The player's clicks happen whenever they want, but the visual feedback completes on the next rhythm beat. The whole game is a room with a heartbeat.

### How it feels

- Every choice the player makes feels WITNESSED by the world itself — the world has a heartbeat, and the player's actions land on it.
- Over hours the player unconsciously synchronizes to the rhythm, and when they notice it for the first time, it feels like the world has been breathing with them all along.
- There is a quiet, ritual quality to every interaction — not slow, but measured.

### Tension explored

Game UI motion is usually ad-hoc — animations picked per element with no shared timing. The Temple Bell proposes a single world-clock that every interaction respects. The rhythm is not music; it is the game's pulse. Every click is a small act of participation in that pulse.

### Why it was merged

Scored 8.60 on its own. On its own, Temple Bell provides a consistent rhythm but does not directly resolve the "clarity vs weight" tension — it's a timing discipline, not a content architecture. When combined with The Two Breaths during PASS 2 refinement, the two directions became structurally complementary: Two Breaths provides the dual-tempo content architecture (instant first breath, slow second breath), and Temple Bell provides the shared world rhythm that binds every settle to the same cadence. The merged result scored 9.05 and became the final recommendation.

### Best showcase surface

Turn transition banner (Player Phase → Enemy Phase → Allied Phase) — where the rhythm is most audibly and visibly expressed as the world's heartbeat. Every phase change lands on a beat.

---

## #12 — The Council Whispers

**Handle:** WHISPERS
**Status:** Shortlisted for Phase 4. **Presented as candidate #3** (weighted 8.35, HumanKind 9, Grey 8).

### Core design philosophy

The Council Whispers is a UI where **every menu option carries an editorial annotation** — a one-line whisper about what the choice will cost beyond the mechanical cost, delivered at the same speed as the hit rate.

Every action in the game has a tiny cost-readout: *"Attack — 31 damage, 140 hit — her children will grow up without her."* *"End Turn — the Rakshasa general does not sleep tonight either."* *"Use Elixir — this was the brewer's last oath, still held."* The annotations are NOT commentary on whether the player should do the thing — they are *facts* about the outcome the player cannot calculate. The UI is literally whispering what the protagonist doesn't know to compute.

### How it feels

- The game's conscience is made visible inside the interaction.
- Every menu item carries a small shadow — a factual consequence the player can't reach through stats alone.
- Over hours, the player starts to notice the whispers more than the numbers. Hit rate becomes less important than what it will cost.
- The UI has a quiet, non-blocking dissent — it doesn't stop the player from any choice, but it shows the cost in the same moment they see the option.

### Tension explored

TRPG choices feel mechanical because the UI treats them as mechanical. The Council Whispers makes the moral consequence appear in the same reading glance as the mechanical outcome — not as commentary, but as FACT the player cannot calculate from the numbers.

### Execution discipline (PASS 2 refinement)

The annotations are NOT hand-authored per action — they are **generated from a template vocabulary** of ~30–40 short fragments combined with context data (unit relationships, unit backstory, outcome type, chapter state).

Example fragments: `"{unit}'s village will not hear this back"`, `"{unit} will not forget this choice"`, `"the river still remembers {unit}'s mother"`, `"{unit} has been silent since the gurukul"`, `"{unit}'s daughter is your age"`.

A few thousand words of authored fragments combine procedurally into thousands of unique annotations. This bounds the writing cost.

### Honest weakness

The template vocabulary has to be GOOD — if the fragments are clunky or cliché, the whole direction becomes preachy overnight. This is a writing risk, not an engineering risk, and the writer must be as disciplined as the art team.

Also: some players may find the annotations didactic and want to turn them off. **Mitigation: no off switch.** The annotations are structural, not optional. The game is sold on this being the UI.

### Best showcase surface

Action menu with annotated options — where every menu item carries its one-line whisper beside its mechanical label.

---

## #13 — The Constellation Map

**Handle:** STARS
**Status:** Cut at ideation.

### Core design philosophy

The Constellation Map is a UI where every surface is a **constellation of stars the player reads**. Units are stars. Their stats are the points of light around them. Combat forecast is the alignment of two stars and the line between them. The tactical grid is a sky map. The player doesn't command the battlefield — they read the heavens.

### How it feels

- Everything feels ancient and cold.
- The figures on the battlefield are reduced to points of light, their relationships visualized as lines between stars.
- There is a meditative quality to reading constellations instead of scanning panels — the brain shifts from "looking up data" to "seeing a pattern."
- Every menu feels like an astronomer's chart.

### Tension explored

Tactical RPGs usually show the world as an explicit battlefield with readable terrain. The Constellation Map abstracts the world upward — the player is reading the war as if it were a sky chart. Decisions feel less immediate and more timeless.

### Why it was cut at ideation

- **Too abstract for a game that needs the player to feel grounded in real places.** Project Astra's worldbuilding is deliberately specific — Ayodhya-scale cities, forests of divinity, riverbank dusks. The Constellation Map abstracts away that place-specificity in favor of a celestial metaphor. The game has said it wants the player to feel the world; constellations make the world disappear.
- **Tactical decisions become hard to read when units are reduced to points of light.** The player needs to see terrain, movement range, threat zones as concrete spaces, not as abstract relationships between stars.

### Best showcase surface

World map / campaign overview as star chart — the high-level strategy layer where abstraction is acceptable and the specific geography is less important.

---

## #14 — The Court Audience

**Handle:** AUDIENCE
**Status:** Cut at ideation.

### Core design philosophy

The Court Audience is a UI where **every menu interaction is framed as the player giving audience** to members of the court / army. When you select Tana, you're not opening her info panel — you're calling her forward to present herself. Her portrait moves, she presents her arms (items), she reports her status (HP, level). The UI is POSE-based — figures stand before you, present, retreat.

### How it feels

- The player is not above the battlefield — they are seated at a court, and the army comes before them one by one.
- Every unit interaction has the weight of a personal audience.
- There is a sense of ceremony and deference — the unit is presenting themselves to you, and you are receiving them.
- Over time the player starts to know each unit not as a stat-block but as a person who has stood before them many times.

### Tension explored

TRPG UI usually treats units as data points you select and query. The Court Audience makes the selection into a human encounter — the unit is SOMEONE who has been summoned before you, not a card you opened. Strong hold on both pillars (majesty of court ceremony, weight of command).

### Why it was cut at ideation

- **Animation infeasibility on indie budget.** Every unit would need a "presenting themselves" animation sequence, every action would need character-specific response animations. That's a character animation pipeline the indie team cannot afford across 30+ recruitable units.
- **Ceremonial pacing too slow for TRPG action frequency.** The player selects units dozens of times per battle. A 1.5-second ceremony per selection becomes exhausting within the first hour. The direction fights the genre's interaction rate.

### Salvage note

Could work for **recruitment scenes specifically** — when the player first recruits a unit, they "present themselves" before being added to the roster. Not the main UI, but a special-occasion surface used once per unit.

### Best showcase surface

Recruitment or party management screen — where the ceremony is appropriate and not repeated hundreds of times per session.

---

# Closing notes

**Family spread:** The 14 candidates deliberately span five different grammar families — temporal, diegetic/material, content/voice, abstract/non-diegetic, compositional. This was Phase 3's search-width discipline: force the ideation across categories rather than iterating one emotional family. Unlike some prior rounds, this pass produced real diversity in the kind of UI thinking at work.

**Rejected ≠ worthless.** Multiple cut candidates carry strong ideas that could surface as **sub-elements under a winning direction**. The Mudra Menu could become the action-menu treatment inside any winner. The Letter from the Field could become the between-chapter narrative framing. The Court Audience could become the recruitment-scene special case. This document preserves them at full detail so they can be revisited as salvage options later.

**The load-bearing discovery:** The insight that *"the readout and the ritual are the same act"* is the thread running through the top 3 directions. Any future round of UI/UX thinking on this project should carry this insight forward, or explicitly argue against it.

**The final recommendation from Phase 5:** The Second Breath (merged #4 + #11), with The Council Table as the second choice if a more diegetic register is wanted, and The Council Whispers as the third choice if the maximum emotional specificity is wanted. None of these has been formally adopted yet; mockups are being built to allow visual evaluation before commitment.
