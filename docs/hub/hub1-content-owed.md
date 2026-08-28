# Hub Interaction 1 — what design still owes

The hub systems are built and Hub Interaction 1 is wired end to end, but **every piece of content in
it is a placeholder**. The GDD is explicit that programmers do not invent dialogue, move a character,
remove a gate, choose a destination or alter progression to make a sequence work — so nothing below
was guessed at. This is the list of what has to arrive before Hub 1 is real.

Rebuild the placeholder version any time with **`Project Astra/Gurukul/Build Hub Interaction 1 (Placeholder)`**.
Check content at any point with **`Project Astra/Gurukul/Content Problems`**.

## What is already true

- The sequence matches the GDD: speak to the other five students (0/5 → 5/5), collect the report card
  from the Guru, then the training-ground beat that runs straight into Map 1 with no return to
  exploration.
- Departure is automatic, and refuses to fire until every objective is done and the visit's
  destination agrees with the campaign's next battle.
- The campaign now reads: opening cutscene → **Hub Interaction 1** → Map 1 → closing cutscene.
- Each student has separate first-time and repeat dialogue, and only the first counts toward the
  counter.

## 1. The Gurukul map — blocking

Everything is standing in a programmer-art courtyard. The GDD's locked placements can't be authored
until the map exists.

| Needed | For |
|---|---|
| Gurukul exterior art | the courtyard, Training Grounds, Banyan Tree |
| Student-house interior (one, reused) | Madhavi's and Rudrani's houses |
| Guru's Quarters interior | the report card scene |
| Library interior | Kaal |

Once the art exists, paint each room in **`Project Astra/Gurukul/Location Editor`** and move the
placements onto it. The locked Hub 1 arrangement is: Guru in the Guru's Quarters, Kaal in the
Library, Aryaman at the Training Grounds, Madhavi in her house, Rudrani in her house, Gajendra
beneath the Banyan Tree.

**Currently:** all five students stand in the greybox courtyard; the Guru stands in the greybox house.

## 2. The cast — blocking

Seven `UnitDefinition` assets now exist with the GDD's names: Kaal, Aryaman, Madhavi, Rudrani,
Gajendra, Guru, Merchant. They are shells.

- **Portraits.** None have one. They currently borrow the protagonist's map sprite so they are
  visible at all. The Data Hub's Problems tab will keep flagging these.
- **Map sprites and animations.** All seven share the placeholder animation set. The real order is
  seven characters × four directions × (idle + walk) — the largest single art item in the hub.
- **Stats, class, identity.** Left at defaults. Only needed if any of them ever fight.

Note there are also no **walk** clips anywhere in the project, only run. At 3.5 tiles/sec the run
clip reads as a jog, which is fine for greybox but is a real art decision to make.

## 3. The script — blocking

Every line currently begins with `PLACEHOLDER`. Design owns all of it:

- Five first-time student conversations, one per student, each ending the exchange.
- Five repeat lines — short, per the GDD's "keep repeat dialogue brief and contextual".
- The report-card scene with the Guru: the handover, that she can't join external missions until she
  relearns the basics, and his agreement to the practical retest.
- The training-ground beat: the move to the Training Grounds, the two shadow puppets, the others
  becoming the audience.

## 4. Decisions design still has to make

- **Does the counter want markers over all five students at once?** It currently does, and drops each
  as that student is spoken to. The GDD allows either that or a single grouped direction, and says
  the setting must be explicit rather than chosen per scene.
- **Portrait staging for the training-ground scene.** The dialogue view has three portrait slots. Any
  scene with four or more speakers on screen needs a decision about how they share those slots.
- **Whether the opening beat inside her house is a scripted event.** The GDD says Hub 1 opens with
  the protagonist inside her own house. That house isn't authored yet, so the visit currently opens
  in the courtyard with no opening event.

## 5. Known gaps outside this visit

- **Save/load is deferred by decision.** Progress lives for the session only. Revisit once two maps
  and two hubs are playable and replaying them starts to hurt.
- **Retrying a battle restarts the campaign.** `GameFlow`'s position is never persisted, so Game Over
  → Title → New Game rewinds to the top. Pre-existing, but the hub makes it visible: the GDD asks
  that retrying a battle not reset the visit that led into it.
- **The hub UI is programmer art.** Five surfaces — interaction prompt, objective line, choice menu,
  world marker, off-screen indicator — are plain boxes and text, pending one batched pass through the
  Figma pipeline.
