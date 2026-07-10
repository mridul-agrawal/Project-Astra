# Map 1 — "The Boar and the Farm" — Tutorial Flow Spec

**Doc type:** Scripted tutorial level flow / beat sheet
**Level ID:** MAP_01 (First Playable / opening combat tutorial)
**Status:** Draft — reframed from raw flow notes
**Owner:** Mridul
**Contributors referenced:** prince dewangan

> **How to read this doc.** This is a *scripted* first mission — the enemy behaves on rails and the player is gently funneled toward the one correct line of play. Every beat below lists the on-screen moment, the single thing we're asking the player to do, the tutorial callout that asks for it, the system being taught, the success/fail branch, the narrative payload, and the feedback ("juice") that sells the moment. Placeholder names (`[PROTAGONIST — TBD]`) and open tuning questions are collected at the bottom rather than left inline.

---

## 1. Logline

The player's very first battle is a small, personal emergency: a lone wild boar has wandered into the protagonist's farm and is smashing her fence. Over three short turns the game teaches selection, movement, positioning-to-attack, weapon choice, and the combat forecast — then closes not on a kill but on an act of restraint that reveals the protagonist's hidden talent to a watching Guru.

## 2. Design goals & pillars

- **Teach the core loop safely.** Select → view movement → move → position for attack → choose weapon → confirm forecast → resolve combat. One new idea per beat, no punishment for exploration.
- **Make the grid feel diegetic, not abstract.** The tutorial is framed as "stop the boar wrecking my farm," so movement and range read as *urgency*, not as a lesson.
- **Establish the emotional pillar early — dharma over dominance.** The mission's climax is a *choice to spare*, seeding the game's moral-consequence identity in the very first fight.
- **Set the character hook.** The closing beats reveal the protagonist's breadth of skill (craft, engineering, farming, animal bond, courage, Vedic knowledge) through the Guru's eyes, motivating the journey to Gurukul.

## 3. Teaching objectives (systems introduced)

| # | System taught | Introduced in beat |
|---|---------------|--------------------|
| 1 | Unit selection (cursor + Confirm) | P1.1 |
| 2 | Movement range display ("blue area") | P1.2 |
| 3 | Grid movement + arrow-guided path preview | P1.3 |
| 4 | Committing a move / "Hold Position" (wait) | P1.4 |
| 5 | Positioning into attack range (adjacency) | P2.1 |
| 6 | Attack range highlight | P2.2 |
| 7 | Weapon selection + character stats screen | P2.3 |
| 8 | Target cursor / enemy cycling | P2.4 |
| 9 | Combat forecast (Player vs Enemy stats) | P2.4 |
| 10 | 1v1 combat scene + damage feedback text | P2.5 |
| 11 | Critical hit / cinematic finisher | P3.2 |
| 12 | Roam-mode interaction prompt + healing (Vedic Herbs) | R.2 |

## 4. Cast & setting

- **[PROTAGONIST — TBD]** — the player unit. A young farmer; archer (starting weapon: **Simple Bow**). Placeholder `XYZ`/`???` in the raw notes refer to her.
- **Wild Boar** — the tutorial enemy. Scripted, single unit. Attacks the fence, then the player; ultimately spared and flees.
- **The Guru** — silent observer for the mission, delivers the closing reveal. No player interaction this map.
- **Setting** — the protagonist's farm at harvest. Fence is the boar's target and the source of the opening threat.

## 5. Combat tuning (reference values)

| Unit | Max HP | Notes |
|------|--------|-------|
| Wild Boar | **10** *(see Open Question O-2)* | Takes **7** damage on the player's first attack |
| [Protagonist] | **20** | Takes **15** damage from the boar's counterattack → **5/20** remaining |

> ⚠️ The raw notes give conflicting boar values (max HP `10`, but the later 1v1 readout shows `Boar HP 7/20`). Preserved as-is and flagged in **Open Questions O-2**.

---

## 6. Sequence / beat sheet

### Cold Open — pre-combat

**C.1 — Harvest & intrusion**
- **Camera / Scene:** Roam view. The protagonist is harvesting the farm — calm, establishing shot.
- **Event:** A single wild boar appears at the field's edge.
- **Feedback (juice):** Music and rhythm shift — the beat changes to signal *combat is starting*. This audio cue is the transition from roam to battle state.
- **State change:** Roam → Battle Map.

---

### Enemy Phase 1 — the map opens on the enemy's turn

> **Design intent:** Starting on Enemy Phase lets the player *watch* the threat act before they're asked to do anything. It establishes stakes (the boar is actively damaging the farm) with zero input pressure.

**E1.1 — The boar strikes the fence**
- **Camera / Scene:** Battle map. Boar takes its scripted action.
- **Event:** Boar rams the farm fence.
- **Feedback (juice):** **Small screen shake.**
- **End of phase:** Enemy Phase ends.

**E1.2 — Protagonist's internal beat**
- **Presentation:** Thought bubble / "clouds" pop-up over the protagonist.
- **Line:** *"I quickly need to stop this boar from destroying my farm."*
- **Purpose:** Converts the mechanical objective ("engage the enemy") into a personal one, then hands control to the player.

---

### Player Phase 1 — selection & movement

> **Design intent:** First hands-on beat. Teach selection and movement in isolation; the player *cannot yet reach* the boar, so this phase ends on a "wait," not an attack.

**P1.1 — Select the unit**
- **Player objective:** Select the protagonist.
- **Tutorial callout (1/2):** "Place the cursor over `[PROTAGONIST]` and press **A / LMB** to select her."
- **Tutorial callout (2/2):** "You have selected `[PROTAGONIST]`. When a character is selected, you can see where she can move."
- **System taught:** Cursor placement + Confirm = select unit.

**P1.2 — Read the movement range**
- **Tutorial callout:** "You can move the selected character anywhere in the **blue area**."
- **System taught:** Movement-range overlay ("blue area").

**P1.3 — Move toward the boar**
- **Tutorial callout (framing):** "For this situation, you need to move closer to the boar to deliver the attack."
- **Tutorial callout (action):** *(target tile flashing / highlighted)* "Move your cursor over the highlighted position and press **A / LMB**."
- **System taught:** Grid movement with **arrow-guided cursor mapping** (path preview) across the movable grid.
- **Branch — CORRECT input:**
  - Basic **run** animation; the unit moves to the selected tile.
  - The **"Hold Position"** button becomes visible, with a small pointer prompting the click.
- **Branch — INCORRECT input:**
  - Re-prompt: "You need to move closer to the boar to deliver an attack. Select the highlighted position and press **A / LMB**."
  - *(Loop until correct.)*

**P1.4 — Commit the move ("Hold Position")**
- **Player objective:** Click **Hold Position** to end the unit's action.
- **System taught:** Committing a move / the "wait" action.
- **Narrative payload (on commit):** *"This boar looks strong; I'd better keep my bow ready!"*
- **End of phase:** Player Phase ends. **The protagonist is now near the boar, but not yet in attack range** — this is intentional; it sets up a second approach beat.

---

### Enemy Phase 2

**E2.1 — The boar strikes again**
- **Event:** Boar rams the fence a second time.
- **Feedback (juice):** **Medium screen shake** *(escalated from E1.1's small shake — rising tension).*
- **End of phase:** Enemy Phase ends.

---

### Player Phase 2 — close in & attack (→ first combat)

> **Design intent:** The payoff phase. Teach adjacency/positioning, weapon selection, the forecast, and then drop into the 1v1 combat scene.

**P2.1 — Select & approach**
- **Tutorial callout (select):** "Select `[PROTAGONIST]`."
- **Tutorial callout (framing):** "Let's close in and attack!"
- **Tutorial callout (action):** "Move `[PROTAGONIST]` to one of the adjacent positions near the boar."
- **System taught:** Positioning into attack range (adjacency).
- **Mechanic:** **3 valid attack-position tiles** are highlighted around the boar.
- **Branch — CORRECT input:** Run animation; unit reaches the chosen tile.

**P2.2 — Read the attack range**
- **Mechanic:** Highlight **all 4 tiles** that fall within the player's attack range.
- **Tutorial callout:** "Here is our chance → now deliver the attack!"
- **UI:** The **"Attack!"** button becomes visible, with a small pointer prompting the click.

**P2.3 — Choose a weapon**
- **Trigger:** Player clicks **Attack!**
- **UI:** Weapon-list screen opens **alongside character stats**. A small pointer highlights **"Simple Bow."**
- **System taught:** Weapon selection + stats readout.

**P2.4 — Target & confirm the forecast**
- **After weapon select:** The **attack/target cursor** appears around the boar, and the **Player vs Enemy stats** (combat forecast) screen is shown.
- **Behavior:** The target cursor normally **rotates through all enemies in range**; here, with only the boar present, it is **locked** to the single target.
- **Confirm:** Press **A / LMB** again → **transition into the 1v1 combat scene.**

**P2.5 — First combat resolves (1v1)**
- **Scene:** 1v1 combat presentation.
- **Result:** Boar takes **7** damage.
- **Feature — damage feedback text:** Show floating/text damage feedback on the hit.
  > **Design note (raw, preserved):** *"I want text feedback after damage in our game. I think that was missing in FE, and widely seen in modern games — @Mridul @prince dewangan."*
- **Tuning:** Boar Max HP = **10**, Damage taken = **7**.
- **End of phase:** Player Phase ends.

---

### Enemy Phase 3 — the boar counterattacks

**E3.1 — Boar attacks the protagonist**
- **Event:** The boar turns to face the player and delivers an attack.
- **Scene:** 1v1 screen with appropriate combat UI.
- **Tuning:** Protagonist Max HP = **20**, Damage taken = **15** → **5/20 remaining.**
- **End of phase:** Enemy Phase ends.

---

### Player Phase 3 — the finishing blow (and the choice to spare)

**P3.1 — Player resolves to end it**
- **Narrative payload:** *"This boar is strong and I am deeply wounded. I need to get rid of this quickly."*
- **Player actions:** Select protagonist → Select **Attack** → Deliver attack.
- **1v1 readout:** Boar `7/20`, Player `5/20`. *(See Open Question O-2 on the boar's HP.)*

**P3.2 — Critical strike → cinematic finisher (mercy)**
- **Feature:** **Critical strike effect** triggers a cinematic / storyboard cut.
- **Cinematic beats:**
  - The arrow passes *through* the boar…
  - …pierces a **falling leaf**…
  - …and lodges in the **tree behind** the boar.
- **Read:** The shot deliberately *misses lethality* — it frightens rather than kills.
- **Payoff animation:** **Boar flees** (run-away animation).

**P3.3 — The Guru's reaction (reveal beat 1)**
- **Cinematic:** Cut to the Guru, **surprised and amazed.**
- **Line (Guru, internal):** *"She could have killed the boar, but she chose only to frighten him."*
- **Purpose:** First externalization of the dharma/restraint pillar — the game's moral identity, stated by a witness.

---

### Resolution — roam & heal

**R.1 — Threat cleared, wound remains**
- **State change:** Return to Roam view.
- **UI pop-up:** "The farm is out of danger — but you are deeply wounded."

**R.2 — Guided heal (interaction tutorial)**
- **UI pop-up:** "Select the **Vedic Herbs** to heal your wounds."
- **Mechanic:** The Vedic Herbs are highlighted in the roam scene. When the player walks near them, a small prompt — **"E to interact"** — appears (contextual, only while in range).
- **System taught:** Roam-mode contextual interaction + healing.
- **Feedback (juice):** **Bandaging** sound; health bar animates back to full.

**R.3 — The Guru's reaction (reveal beat 2 — the full picture)**
- **Cinematic:** The Guru, amazed, takes stock of everything she's just seen in this one girl:
  - a **handmade bow**, crafted from simple wood and bark;
  - **water engineering;**
  - **farming;**
  - a **bell for communicating with her animals;**
  - **bravery** in defending the farm;
  - and **knowledge of Vedic herbs.**
- **Line (Guru):** *"What am I seeing? How could I have missed this talent, so near to the Gurukul all along?"*

**R.4 — Close**
- **Blocking:** The Guru approaches the protagonist as she patches her wounds.
- **Transition:** **Fade out.**

---

## 7. Open questions & production notes

- **O-1 — Placeholder names.** `XYZ` / `???` throughout the raw notes are the protagonist. All callout text above uses `[PROTAGONIST — TBD]`; lock the name before UI string freeze.
- **O-2 — Boar HP inconsistency (needs a decision).** Combat tuning states **Boar Max HP = 10** and **first attack = 7 damage** (which would leave **3/10**). The later 1v1 readout, however, shows **"Boar HP 7/20."** The max (10 vs 20) and the remaining value (3 vs 7) don't reconcile. Pick one: either the boar is a 10-HP enemy (adjust the readout) or a 20-HP enemy (adjust the first-hit tuning). Player values are internally consistent (20 max, −15 → 5/20).
- **O-3 — Damage feedback text.** Confirmed feature request (see P2.5 note). Decide style (floating combat text vs. fixed callout), and whether it appears in both the 1v1 scene and on the map.
- **O-4 — Input parity.** Callouts show **A / LMB** together (gamepad + mouse). Confirm the tutorial string system swaps glyphs based on the active device per the input-abstraction spec, rather than hardcoding "A / LMB."
- **O-5 — Fail-branch coverage.** P1.3 has an explicit incorrect-input re-prompt. P2.1 (approach) currently only specifies the correct branch — decide whether an equivalent re-prompt loop is needed, or whether non-highlighted tiles are simply non-selectable.
- **O-6 — Screen-shake escalation.** Small (E1.1) → Medium (E2.1) is a deliberate tension ramp. Note for the audio/VFX pass so the two shakes are tuned as a pair, not independently.
