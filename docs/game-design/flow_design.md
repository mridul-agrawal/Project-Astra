# 60-Minute Prototype — Player Flow Design

*Beat-by-beat design for the 60-minute single-map prototype. Companion brief: `flow_plan.md`. Primary source: `SRPG Prototype Design Playbook.md` — every beat carries a `§N #M` reference back to it.*

| Field | Value |
|-------|-------|
| Doc owner | Mridul (design lead) |
| Status | v1 — pre-playtest |
| Mechanics referenced as already shipped | Canto, Healing Tiles, Flying Terrain Immunity, Lord Permadeath (UM-02), EXP Scaling, Throne `TerrainType`, `DialogueSequencePlayer` |
| Mechanics newly introduced by this design | *Marked Shot* (lord ability), *Spare* (action-menu entry), reinforcement-spawn telegraph indicator |

---

## Purpose

This document describes **what the player does, feels, and chooses, minute-by-minute, across one 60-minute first-time playthrough** of the prototype's single map. It is the bridge between the playbook (theory) and the implementation tickets (map authoring, AI scripting, dialogue copy, art).

It does **not** specify exact tile coordinates, dialogue prose, balance numbers, or art direction — those are downstream tickets. It *does* specify every meaningful decision the player is forced to make and the design rationale behind each.

## Glossary

- **Turn** — one full Player Phase + Enemy Phase cycle (~2–3 min real-time at first-time-player pace).
- **Phase** — a Player Phase or Enemy Phase. Distinct from "phase" in the encounter sense (Phase 1 / Phase 2).
- **Act** — a 5-act narrative segment (Act 1 = 0:00–10:00, etc.). 5 acts × ~12 min = 60 min.
- **Phase 1 / Phase 2** — encounter-level objective regimes. Phase 1 = defensive (Acts 1–2 + first half Act 3). Phase 2 = offensive (second half Act 3 + Acts 4–5). Transition at 25:00 (start of Act 3).
- **Chokepoint** — a tile-width constraint forcing single-file engagement. The map has two: the 1-tile Temple Gate (mid-map) and the 2-tile Mountain Pass (north).
- **Telegraph** — visible-from-the-board indicator of impending enemy action or reinforcement spawn. Mandatory for fairness (§6 reinforcement design).
- **Decision forced** — a per-beat count of player choices that have ≥2 valid options with no obvious right answer. Each decision counted is described inline so a reviewer can audit the claim.

## Decision-density curve (target)

| Act | Window | Decisions / 5-min target | Total decisions in act |
|-----|--------|--------------------------|------------------------|
| 1   | 0:00–10:00 | 3–4 | **5–7** |
| 2   | 10:00–25:00 | 3 | **8–10** |
| 3   | 25:00–45:00 | 3–4 | **12–15** |
| 4   | 45:00–55:00 | 3 | **5–7** |
| 5   | 55:00–60:00 | 0 | **0–2** |
| **Total** | | | **~30–41** |

Within Section 4's 30–45-decision band. Closer to ITB density than FE sprawl, per §1 #9 (60-min prototype targets ITB model — high stakes per turn over fewer total turns).

---

## Map architecture

### Size and orientation

**18 × 20 tiles** (within §3's 14×16–18×20 prototype band). North up. Player deploys at the south edge.

### Layout (north-to-south)

```
N
│   ┌──────────────────────────────────────┐
│   │  [Reinforcement spawn ◇]   [◇]       │  rows 0–1   (off-map until telegraphed)
│   │  ════════════════════════              │  row  2     MOUNTAIN PASS  (2-tile chokepoint)
│   │  ▓▓▓ ░░░░░░░░░░░░░░░░ ░░░ ▓▓▓        │  rows 3–5   River + Forest belt (impassable / cost 2)
│   │  ░░░░ [Elevated Ruin ⛩] ░░░░          │  rows 6–8   Outer field
│   │  ░░░░░░░░░░░░░░░░░░░░░░░░             │  rows 9–11  Approach plain
│   │  [Fort 🏰] [== TEMPLE GATE ==] [Fort] │  row  12    GATE  (1-tile chokepoint)
│   │  ░░░░░░░░░░░░░░░░░░░░░░░░             │  rows 13–15 Courtyard
│   │  ░░░░░░░ [Throne 👑] ░░░░░░░          │  rows 16–18 Inner sanctum
│   │  [Player deploy 1–5]                  │  row  19
│   └──────────────────────────────────────┘
S
```

### Terrain types in play (and their decision-relevance)

Each terrain type exists because it changes ≥1 player decision. If you can imagine the same map with a tile replaced by Plains and the strategy unchanged, that tile is noise (§3).

| Terrain | Move cost (foot/mount/fly) | Def / Avoid bonus | Phase-start heal | Decision changes |
|---------|----------------------------|-------------------|------------------|------------------|
| Plains | 1 / 1 / 1 | 0 / 0 | – | baseline |
| Forest | 2 / 3 / 1 | +1 / +20 | – | Devika tanks here; Bhanu (chariot) cannot enter |
| Mountain | – / – / 1 | +2 / +30 | – | only Vidya crosses; archers gain perch line |
| River | – / – / 1 | – / – | – | only Vidya crosses; bisects map E–W |
| Fort 🏰 | 1 / 1 / 1 | +2 / +20 | +5 HP | who occupies the gate-flank fort? |
| Throne 👑 | 1 / 1 / 1 | +3 / +30 | +5 HP | Vikram's deployment seat → defensive anchor in Phase 1 |
| Elevated Ruin ⛩ | 2 / – / 1 | +1 / +10 + crit-bonus | – | enemy archer's perch in Acts 1–2; player's perch in Act 3 |

Seven terrain types — the playbook caps complexity by use, not count, so this is fine *if* every type does mechanical work. The table above is the proof.

### Two chokepoints, two regimes

- **Temple Gate** (row 12, 1-tile wide, flanked by two Fort tiles). The defensive fulcrum in Phase 1. Gajen's body fills it. In Phase 2 the player exits through it; it becomes the egress, not the wall.
- **Mountain Pass** (row 2, 2-tile wide). The offensive fulcrum in Phase 2. Veerasen retreats here. Bhanu's chariot cannot enter; the pass forces the player to commit Vikram + Gajen on foot OR fly Vidya past.

### Two telegraphed reinforcement spawn points

Tiles (row 0, col 4) and (row 0, col 14) — visible at map start as red-glow indicators. Reinforcements arrive there on telegraphed turns (Act 2 turn 7 and Act 3 turn 13, both with one-turn warning). Players who scout the map's edges at deployment see them. Players who don't are warned by the indicator; nobody is ambushed (§6 §8 #5 ambush-spawn guard).

---

## Pre-deployment (00:00 — before clock starts)

- Title screen → "Continue" → loading veil.
- 6-second establishing shot: temple silhouette at sunrise, Surya-mantra chant fading in, Vikram's quiver in foreground.
- Tactician 4th-wall opener (one beat — §2 4th-wall framing): *"Tactician. The Suryavansh court trusts your hand. The board is yours."*
- No deployment screen for the prototype — the 5 starting units are auto-placed. **Three units active at the start: Vikram (on Throne), Gajen (south of gate), Devika (south of gate).** Vidya and Bhanu join later (§4 Act 1: ≤4 units in opening, FE7 Lyn-mode pacing).

Failure mode this section guards against: §8 #1 "Demo Fatigue by Unit Count" — the player does not see all five units on first deployment.

---

# Act 1 — Cold Open + First Impression  (0:00 – 10:00)

## Map state at act start

- **Player units:** Vikram (Throne, row 17 col 9), Gajen (row 16 col 8), Devika (row 16 col 10).
- **Enemy units:** 4 raiders visible **north of the gate** at row 9–10.
  - Raider-Sword-A (col 8) — vanguard, will charge.
  - Raider-Sword-B (col 10) — vanguard.
  - Raider-Archer (col 12, on elevated ruin perch) — holds.
  - Raider-Lance (col 6) — patrols left flank.
- **Ritual HP gauge:** 100/100 — visible top-of-screen UI bar. Loses 1 HP per enemy adjacent to the gate per turn.
- **Fog:** none in Act 1 (small force, full-board legibility).
- **Visible telegraphs:** every enemy shows their next-turn target tile in red — including Raider-Archer's 3-tile crit arc south of the elevated ruin (§1 #1).

## Pacing target

3–4 decisions per 5 min → **5–7 decisions across Act 1.**

## Failure mode this act guards against

§8 #3 "Narrative-Combat Disconnect" + §4 Act 1 "Opening with 2–3 minutes of dialogue before first combat." Mitigation: player makes their first move by 1:30. All dialogue is ≤30 sec, in-fiction, and tied to the immediate tactical state.

## Beats

### Beat 1.1 — The Tactician's Address  (0:00 – 1:00)

- **Player action:** none. Watches and reads.
- **Intended emotion:** *gravity.*
- **Mechanic(s):** none yet — the establishing shot ends with the cursor handed to the player.
- **Enemy AI:** raiders frozen in start positions (no AI yet — game in `Dialogue` state).
- **Scripted event:** opening dialogue plays via `DialogueSequencePlayer`. Three lines:
  1. Tactician (4th-wall): *"Tactician. The court has six turns of ritual left. Hold the gate that long."*
  2. Acharya Devika (in-fiction): *"The Surya-mantra is fragile. Every blow against the gate is a blow against the chant."*
  3. Vikram (in-fiction): *"They came for the relic. Let them remember why we keep it."*
- **Dialogue trigger:** `GameState.Dialogue` on scene-load, auto-resolves on confirm (UM-02-style flow).
- **Decisions forced:** 0.
- **Playbook ref:** §2 (4th-wall narrator with in-world role) + §7 (pre-battle dialogue ≤90 sec).

### Beat 1.2 — First Move, First Read  (1:00 – 3:00)

- **Player action:** moves Vikram off the Throne toward the gate, OR positions Gajen to plug the gate, OR sends Devika to a Fort.
- **Intended emotion:** *focused calm — the board is small, the threats are visible.*
- **Mechanic(s):** movement; terrain-bonus tooltip on hovering Throne/Fort/Plains; enemy-intent telegraphs (§1 #1).
- **Enemy AI:**
  - *Raider-Sword-A & -B:* "Vanguard charge — advance toward the nearest player unit each turn; attack if in range; never retreat."
  - *Raider-Archer:* "Perch — does not move from elevated ruin. Attacks any player unit entering the 3-tile crit arc south of the ruin."
  - *Raider-Lance:* "Flanker — advances along the western forest edge toward the gate, prioritizes flanking the gate over engaging the nearest unit."
- **Scripted event:** none in this beat — it's player-initiated.
- **Dialogue trigger:** none.
- **Decisions forced:** **2.**
  - (a) *Plug the gate now or hold Gajen one tile back?* — plugging means the elephant absorbs the first blow and triggers Devika's heal economy; holding back means the gate takes 1 ritual-HP point from the lance flanker but lets Vikram fire from Throne range. Both viable; trade-off is *unit-HP risk vs ritual-HP risk*.
  - (b) *Devika to west Fort or east Fort?* — west Fort positions her to heal Gajen but exposes her to the lance flanker. East Fort is safer but a turn slower to respond. Asymmetric reads; no obvious right answer.
- **Playbook ref:** §1 #1 (telegraph) + §3 (chokepoint pattern) + §6 (scripted enemy archetypes).

### Beat 1.3 — The Marked Shot  (3:00 – 5:00)

- **Player action:** uses Vikram's *Marked Shot* — one guaranteed crit per map. Cursor highlights the ability when the lord has line-of-sight to any enemy.
- **Intended emotion:** *thrilled vindication — the lord IS different.*
- **Mechanic(s):** *Marked Shot* (lord-distinct ability — §8 #2 lord identity guard). Guaranteed crit; no RNG miss; the playbook explicitly warns against RNG-dependent showcase moments in Act 1 (§4 Act 1 "common failure modes").
- **Enemy AI:** if the marked target is Raider-Sword-A (likely), it dies; the remaining two ground raiders' AI does not change priorities — they continue charging.
- **Scripted event:** the first time Vikram fires Marked Shot, the tactician interjects with one line: *"That's the shot. He's not just an archer — he's the archer of this house."* (§4 Act 1 — "make the first combat encounter feel like it says something about the character.")
- **Dialogue trigger:** event-bound to the Marked Shot animation finishing.
- **Decisions forced:** **1.**
  - (a) *Mark the archer on the ruin OR the lance flanker OR a vanguard?* The archer is the highest threat (perch, crit arc) but reaching him in range needs movement; the lance threatens the gate-flank fort; the vanguards are easy kills. Three valid targets, three different downstream maps.
- **Playbook ref:** §1 #10 (signature mechanic surfaces in first 3 turns) + §4 Act 1 (script the Act-1 ability to succeed first try, no RNG).

### Beat 1.4 — Counterpressure  (5:00 – 7:00)

- **Player action:** absorbs the first enemy phase. Devika heals Gajen if needed; Vikram either re-positions or chips the next-priority threat.
- **Intended emotion:** *tested confidence — the system holds, but only if I work it.*
- **Mechanic(s):** healing (Devika staff, FE GBA EXP grant fires — §EXP-shipped-mechanic), counter-attack (the engaged raider triggers a counter on Gajen).
- **Enemy AI:** vanguards engage — first into Gajen if he plugs the gate, otherwise into Vikram (the nearest unit). Lance flanker arrives at western Fort tile and attacks adjacent player unit.
- **Scripted event:** Ritual HP gauge ticks down 1 if any enemy is adjacent to the gate at end of Enemy Phase. First visible deduction creates urgency.
- **Dialogue trigger:** Devika line on her first heal: *"Hold steady — the chant cannot falter while you bleed for it."*
- **Decisions forced:** **2.**
  - (a) *Heal now (uses Devika's action) or save the heal for next turn (frees her to chip-attack)?* Healing is the safe play; chipping is faster damage but risks Gajen falling to a crit.
  - (b) *Engage the lance flanker in the forest or let him come to the Fort?* Engaging in forest gives him +20 avoid but pulls him away from the ritual; letting him come means he attacks from Fort defense bonus.
- **Playbook ref:** §6 (encounter design via scripted beats) + §1 #4 (defensive win condition produces denser decisions than rout).

### Beat 1.5 — The Scout Party Falls; The Reveal  (7:00 – 10:00)

- **Player action:** finishes the last 1–2 raiders. End of Player Phase auto-scrolls camera north.
- **Intended emotion:** *premature triumph turning to dread.*
- **Mechanic(s):** combat resolution; first level-up screen for Vikram (FE-style 9-stat-row animation — `LevelUpScreenUI` already shipped).
- **Enemy AI:** survivors fight to the death; no morale-flee.
- **Scripted event:** when the last Act-1 enemy dies, camera scrolls north over the river/forest belt and reveals **8 raider silhouettes plus a banner-bearer at the mountain pass.** Static — they are not yet active, but the player sees what's coming.
- **Dialogue trigger:** Vikram (in-fiction): *"Six raiders. That was the scouts."* Tactician (4th-wall, low-key): *"The board's bigger than the gate, tactician. Look north."*
- **Decisions forced:** **1.**
  - (a) *Pull units back to refresh on Fort/Throne or push them up to row 9 for Act-2 lead time?* Fort/Throne refresh costs 1–2 turns; pushing forward saves time but enters the elevated-ruin's crit arc. The decision sets the player's Act-2 starting position.
- **Playbook ref:** §4 Act 1 ("end Act 1 with a narrative beat that changes the objective for Act 2") + §6 (positional telegraph for upcoming reinforcements).

## Act 1 decision count: **6** (within target 5–7).

---

# Act 2 — Mechanic Introduction  (10:00 – 25:00)

## Map state at act start

- **Player units:** 3 starting units in their Act-1-end positions. **Vidya joins on turn 5 (~12:00) from west forest edge.** Bhanu does not join in Act 2.
- **Enemy units:** 4 fresh raiders advance from the north — 2 Raider-Sword (mountain pass), 1 Raider-Archer (relocates to the elevated ruin if it was vacated), 1 new **Raider-Healer** (rear, holds back).
- **Ritual HP gauge:** wherever Act 1 left it (typically 95–100/100).
- **Fog of war:** introduced this act on the north 1/3 of the map (rows 0–6) — represents distance + dust. Cleared by Vidya's *Scout's Eye*.
- **Reinforcement telegraph:** at the start of turn 6, the (row 0, col 4) spawn tile glows red; reinforcements arrive turn 7.

## Pacing target

3 decisions per 5 min → **8–10 decisions across Act 2.**

## Failure mode this act guards against

§8 #1 "Demo Fatigue by Unit Count" + §4 Act 2 "Introducing signature mechanic AND a new enemy type in the same 5-minute window." Mitigation: each new mechanic gets a dedicated 4–5-minute window. Vidya joins minute 12, terrain bonus tested minute 14, healing tile triggered minute 17, canto demonstrated minute 20, reinforcement at minute 22 — strict 4–5 min spacing, no overlaps.

## Beats

### Beat 2.1 — Vidya Arrives; Flying Introduced  (10:00 – 14:00)

- **Player action:** Vidya appears on the western forest edge (a tile no ground unit could enter without 2 turns of cost). Player moves her — discovers cost-1 movement over forest. Tactician confirms.
- **Intended emotion:** *expansion — the board just got bigger.*
- **Mechanic(s):** **Flying terrain immunity** (already shipped — `FlyingHoverAnimator` + flying cost table). *Scout's Eye* reveals fog around her.
- **Enemy AI:** Raider-Archer relocates to the elevated ruin if it was vacated in Act 1; Raider-Sword pair advances toward the gate (vanguard charge); Raider-Healer holds at row 4 (rear) and casts heal on adjacent allies if any are below 50% HP.
- **Scripted event:** Vidya arrival cinematic — 2-second wing-flap animation; Tactician: *"The Vidyadhari rides the wind. She does not pay the river its toll."*
- **Dialogue trigger:** Vidya's first move triggers her self-intro: *"I came as fast as the wind allows. What I saw in the pass is worse than the gate knows."*
- **Decisions forced:** **2.**
  - (a) *Send Vidya north to scout (clears fog, reveals reinforcement spawn early) OR east to flank the elevated archer (kills the perch threat)?* Scouting is information; flanking is damage. Real trade.
  - (b) *Have Vidya enter the river-tiles to bisect the enemy line OR stay on the forest edge for a Fort retreat?* River entry is a flier-only move that puts her at low HP risk if the archer survives.
- **Playbook ref:** §1 #10 (first 3 turns surface signature mechanic — flying is the prototype's signature movement-class differentiator) + §3 ("if cavalry is in the prototype, there must be at least one engagement where high movement range is the win condition" — applies equally to fliers).

### Beat 2.2 — Terrain Bonus Tested  (14:00 – 18:00)

- **Player action:** an enemy unit (Raider-Archer) attacks from the elevated ruin and the damage spike is *visible*. Player chooses how to dislodge.
- **Intended emotion:** *tactical respect — the terrain has teeth.*
- **Mechanic(s):** **Elevated terrain bonus + crit modifier** (`TerrainStats` already supports per-terrain crit/avoid; the elevated ruin authoring is new for this map but uses the existing pipeline).
- **Enemy AI:**
  - *Raider-Archer:* "Perch — re-fires from elevated ruin every turn. If a player unit reaches the ruin's tile, the archer disengages 1 tile north and re-fires."
  - *Raider-Sword pair:* continues vanguard charge.
  - *Raider-Healer:* heals the archer if HP < 50%.
- **Scripted event:** the first time the archer crits a player unit, a damage-popup pulse + on-screen tutorial card appears: *"Elevated terrain: +1 def, +20 avoid, +crit. Climb it, or kill what's on it."* (§2 popup-tutorial — fires *during* a player-action context, not as gating screen.)
- **Dialogue trigger:** Devika (if she's the one hit): *"The high ground is not just dharma — it is geometry."*
- **Decisions forced:** **2.**
  - (a) *Charge the ruin uphill (Mahisha/Vikram, takes the avoid penalty) OR send Vidya over the river to flank from the rear (no terrain cost but exposes her to the archer's counter)?* Both are valid solutions; one trades HP, the other trades positioning.
  - (b) *Kill the archer first OR break the vanguard line first?* The archer is the consistent damage source; the vanguard threatens the gate sooner.
- **Playbook ref:** §3 (terrain variety vs noise — every type does work) + §4 Act 2 ("introduce by placing 1 elevated archer that deals demonstrably more damage when the player charges uphill").

### Beat 2.3 — Healing Tile Reveal  (18:00 – 20:00)

- **Player action:** at the start of the next Player Phase, a player unit standing on a Fort or the Throne visibly heals (+5 HP, green float). The first time it triggers, the tactician calls it out.
- **Intended emotion:** *resourcefulness — the map itself is on my side.*
- **Mechanic(s):** **Healing Tiles** (already shipped — `HealingTileSystem.PhaseStarted` hook).
- **Enemy AI:** unchanged from beat 2.2.
- **Scripted event:** first phase-start where any player unit is on Fort/Throne, the heal-float animation plays, and the tactician interjects: *"Your forts heal between turns. Position to bleed in the right places."*
- **Dialogue trigger:** event-bound to first heal-float spawn.
- **Decisions forced:** **1.**
  - (a) *Cycle a damaged unit to the Fort for free heal (costs movement) OR keep them on the line and have Devika heal (costs Devika's action)?* The Fort heal is "free" but requires a unit move; Devika's heal is targeted but burns her offensive turn.
- **Playbook ref:** §3 (terrain doing mechanical work) + memory `Healing Tiles shipped` (existing infrastructure exposed cleanly to player here).

### Beat 2.4 — Canto Reveal  (20:00 – 22:00)

- **Player action:** Mahisha (cavalry) attacks then re-positions back toward the gate using leftover movement. The first time the canto-loop fires, the unit's residual move-grid re-appears in cyan and the tactician explains.
- **Intended emotion:** *empowerment — the rules just leveled up.*
- **Mechanic(s):** **Canto** (already shipped — `GridCursor.TryEnterCanto`, gated on `ClassDefinition.HasCanto`).
- **Enemy AI:** unchanged.
- **Scripted event:** first time Mahisha (or any cavalry/flying unit) takes a non-Wait action, the cantered movement-grid renders with a one-time tutorial badge: *"Canto: mounted units keep unused movement after acting."*
- **Dialogue trigger:** tactician (one beat): *"Strike, then ride. The elephant doesn't waste a single furlong."*
- **Decisions forced:** **1.**
  - (a) *Canto Mahisha back to the Fort (heal next phase) OR forward to the elevated ruin (lock down the perch)?* Defense vs aggression. Both valid.
- **Playbook ref:** §1 #2 (one mechanic per 3–5 min — canto is the 4th in Act 2's strict cadence) + memory `Canto shipped`.

### Beat 2.5 — Reinforcement Wave + Optional Escort  (22:00 – 25:00)

- **Player action:** turn 6 starts — the (row 0, col 4) spawn tile glows red. Player has one full Player Phase before reinforcements arrive on turn 7. Player redeploys.
- **Intended emotion:** *productive dread — I see it coming, and I have one turn.*
- **Mechanic(s):** **Reinforcement telegraph** (this map's authoring; mechanically it's just a turn-N visual indicator + scripted spawn on N+1, no new system).
- **Enemy AI:**
  - *Reinforcement wave (turn 7):* 2 Raider-Sword spawn. Vanguard-charge AI applies.
  - *Existing raiders:* unchanged.
  - *Optional ally NPCs:* 2 villagers appear at row 12 col 8 (gate interior) on turn 6 as a scripted dialogue cue. They walk south along a fixed scripted path toward the deploy row (evac tile at row 19 col 9), 1 tile per turn. The path is *fixed* — no escort-AI to break — but they take 4 turns to reach safety, during which any enemy that reaches them does 5 ritual-HP-equivalent damage.
- **Scripted event:** villagers' arrival triggers a dialogue: village-elder (in-fiction): *"Tactician — let them through. The court's children, the cooks, the carrier who burned his hands for the lamps. They cannot fight."*
- **Dialogue trigger:** villager arrival on turn 6.
- **Decisions forced:** **2** (3 if escort included).
  - (a) *Move a unit to intercept the reinforcement spawn point OR fall back to absorb at the gate?* Intercepting kills them at low cost; falling back means the gate gets pressured for 2 more turns.
  - (b) *Devika heals (saves a unit) or chip-magics the reinforcements (kills one before it reaches gate)?*
  - (c) *(if escort active)* *Detour Mahisha or Vidya south to body-block enemies away from the villagers' scripted path OR accept the ritual-HP damage?*
- **Playbook ref:** §6 (1-turn telegraph rule for fair reinforcements; positional telegraph at logical entry — mountain pass) + §6 ("Escort NPC to tile … NPC AI failures destroy player experience" — mitigated by fixed scripted path, no AI).

## Act 2 decision count: **8** (or 9 with escort active — within target 8–10).

---

# Act 3 — Mastery + Escalation  (25:00 – 45:00)

## Phase shift trigger (25:00 — start of Act 3)

At minute 25 (turn ~10), the **Surya-mantra ritual completes.** A scripted cinematic (~5 sec) plays: gold light radiates from the throne, the gate UI HP-bar dissolves, and a new objective banner appears: **"Pursue Veerasen — 12 turns."** Bhanu (chariot) joins from the south road on turn 11, having delivered the villagers. Tactician (4th-wall — second of three audience-aware breaks): *"That's Phase 1. The board flips now — we're the ones moving."* (§1 #5 multi-phase via objective shift.)

## Map state at act start

- **Player units:** 4 active units in Act-2-end positions + Bhanu joining on turn 11 from row 19 col 5.
- **Enemy units:** **Veerasen visible at the mountain pass (row 2, col 9).** He has NOT moved during Phase 1 (zoning AI — §6 zoning behavior). 4 fresh enemies form a defensive wedge across rows 5–8: 2 Raider-Sword, 1 Raider-Lance, 1 Raider-Mage (new archetype — first appearance). Surviving Act-2 enemies retreat north to join Veerasen's line.
- **Objective:** reach Veerasen OR reduce his HP below 30% within 12 turns. (The 30% threshold unlocks the Spare action — see Act 4.)
- **Fog:** still present rows 0–4 until Vidya scouts.
- **Reinforcement telegraph:** turn 13 — the (row 0, col 14) spawn tile glows; Veerasen's bodyguard pair arrives on turn 14.

## Pacing target

3–4 decisions per 5 min → **12–15 decisions across Act 3.**

## Failure mode this act guards against

§4 Act 3 ("Flat difficulty — Act 3 plays exactly like Act 2, just with more enemies"). Mitigation: every Act 3 beat reuses an Act 1–2 enemy type *in a new tactical configuration* (§6 Tier 2 "familiar enemy in new configuration") — no new enemy types except the Mage, whose introduction is a single beat with explicit context.

## Beats

### Beat 3.1 — Phase Shift; Bhanu Arrives  (25:00 – 28:00)

- **Player action:** absorbs the cinematic, sees the objective banner, moves Bhanu (chariot) onto the field.
- **Intended emotion:** *surge — the leash comes off.*
- **Mechanic(s):** chariot range (1–2 tile lance), high move (7), restricted from forest/mountain/1-tile gate. Re-introduces canto (Bhanu also has it).
- **Enemy AI:** Veerasen and his line do not move yet — they zone-lock.
  - *Veerasen:* "Zone-lock. Holds at the mountain pass. Does NOT advance until any player unit crosses row 6 OR a player unit's HP drops below 25%. When unlocked, advances toward the nearest non-civilian player unit, prefers Vikram (the lord)."
  - *Defensive wedge:* "Zoning — advance only when a player unit enters their attack range" (§6 zoning AI).
- **Scripted event:** Bhanu arrival animation; Tactician (4th-wall — moment 2 of 3): *"The relic's running. So are we. Take the pass before he gets to it."*
- **Dialogue trigger:** Bhanu's first move: *"My horses have not run for the dharma in a long time. Let them, today."*
- **Decisions forced:** **2.**
  - (a) *Advance up the center plain (fast, exposed) OR sweep west via the river (Vidya leads) OR east via the elevated ruin (Vikram-Bhanu)?* Three viable approach lanes — the playbook's chokepoint-and-arena logic compressed.
  - (b) *Commit Bhanu to the lead (his range punishes the wedge) OR keep him as a counterpunch (waits to clean reinforcements turn 14)?*
- **Playbook ref:** §1 #5 (multi-phase shift) + §6 (zoning AI) + §1 #10 (signature mechanic — chariot range — surfaces in first 3 turns of Phase 2).

### Beat 3.2 — Familiar Enemies in New Configuration  (28:00 – 32:00)

- **Player action:** engages the defensive wedge. Discovers that Raider-Archer + Raider-Mage on the elevated ruin (now repositioned) is a different problem than Raider-Archer alone in Act 2.
- **Intended emotion:** *recognition + adaptation — I know this enemy, but the situation just changed.*
- **Mechanic(s):** all of Act 2's mechanics now stacked. Magic damage (new — but the *unit* is just a re-skin of the archer's behaviour pattern, only with magic damage type).
- **Enemy AI:**
  - *Raider-Archer (back from Act 2 if he survived, else fresh):* perch on elevated ruin.
  - *Raider-Mage:* "Caster — stays adjacent to Raider-Archer if possible. Targets the player unit with highest current HP (ignores armour, magic-typed). Does not move from Mountain tile."
  - *Raider-Sword pair:* vanguard charge.
  - *Raider-Lance:* flanker (now on the eastern edge).
- **Scripted event:** first time the Mage hits a player unit, a one-line on-screen card: *"Magic damage ignores defence. Track the caster like an archer with longer reach."*
- **Dialogue trigger:** Acharya Devika: *"That mantra is a country chant — illegal to recite without sanction. He learned it for this."*
- **Decisions forced:** **3.**
  - (a) *Counter-cast with Devika (magic vs magic) OR rush the Mage with Vidya (1 turn to reach, dies if she's countered)?* Devika trades safely; Vidya is a faster solution but high-risk.
  - (b) *Pull Vikram up to the elevated ruin perch (Marked Shot already used — but his base archery still benefits from crit terrain) OR use Bhanu's range to chip from below?*
  - (c) *Heal-cycle through the gate forts now (one last turn it's safe) OR push past row 8 and abandon the heal economy?*
- **Playbook ref:** §6 Tier 2 ("familiar enemy in new configuration") + §4 Act 3 ("the player uses 2–3 Act 2 mechanics simultaneously for the first time").

### Beat 3.3 — Chokepoint #2 — The Mountain Pass  (32:00 – 36:00)

- **Player action:** approaches the mountain pass — 2-tile chokepoint guarded by 2 Raider-Sword + Veerasen. Bhanu cannot enter (chariot restricted). Player must dismount or fly past.
- **Intended emotion:** *strategic identification — this is the moment that asks the most of me.*
- **Mechanic(s):** chokepoint reversal (§3 — "fortress siege, attack/defence reversal"). Player is now the attacker, terrain favours the defender.
- **Enemy AI:**
  - *Pass-Sword pair:* "Phalanx — both stand on the pass tiles, never both leave them. If one falls, the other advances 1 tile to engage; if both fall, Veerasen's zone-lock breaks."
  - *Veerasen:* still zone-locked — UNLESS a player unit's HP drops below 25%, in which case he advances 1 tile (creates pressure to keep HP healthy through the choke).
- **Scripted event:** none here — pure tactical engagement.
- **Dialogue trigger:** Veerasen's first taunt: *"You came to the pass for me. You should have gone for the throne when you had it."* (§7 minimum-viable conditional dialogue — boss taunt on first sight.)
- **Decisions forced:** **3.**
  - (a) *Send Vidya past the pass to harass Veerasen directly (low-HP risk; can she survive his counter?)?*
  - (b) *Have Vikram dismount-equivalent — climb the ruin and snipe through the pass (his archery is the only ranged option that doesn't rely on Bhanu)?*
  - (c) *Have Mahisha plug one pass tile to break the phalanx pair, taking the +30 mountain avoid against him?* Mahisha's HP is the buffer.
- **Playbook ref:** §3 (chokepoint reversal) + §3 ("DON'T design the map so that cavalry units are useless — if cavalry is in the prototype, there must be at least one engagement where high movement range is the win condition" — Bhanu's role here is the negative-space example: when the chariot's strength becomes a liability, the player must improvise).

### Beat 3.4 — Mid-Battle Dialogue  (36:00 – 40:00)

- **Player action:** continues engaging. When Veerasen's HP drops below 50%, he speaks for the second time.
- **Intended emotion:** *moral complication — the boss is not a monster.*
- **Mechanic(s):** HP-threshold conditional dialogue (§7).
- **Enemy AI:**
  - *Veerasen:* on his HP < 50%, his zone-lock breaks (independently of player position) and he advances toward Vikram, prioritizing him. *Refuses to attack civilians* — explicitly skips them in target selection. This is visible to the player as he steps past a villager NPC (if any survived).
  - *Bodyguard pair (turn 14 reinforcements):* 2 Raider-Sword spawn at (row 0, col 14). Vanguard charge toward the closest player unit.
- **Scripted event:** Veerasen HP < 50% triggers a 6-second mid-battle vignette: he stops, looks at the temple, says *"I knelt at this gate as a boy. Then the famine came. Then the court's word came."*
- **Dialogue trigger:** HP threshold.
- **Decisions forced:** **2.**
  - (a) *Push Vikram into Veerasen's range to finish him fast (high damage, also high risk — Vikram dying = game over per UM-02)?*
  - (b) *Hold Vikram at long range and let Bhanu/Devika tag in (slower but safer; risks bodyguard pair reaching Vikram from the rear)?*
- **Playbook ref:** §1 #7 (anchor moment setup — characterization concentrated late, since prototype budget is 25 min not 6 maps) + §7 (HP-threshold dialogue + boss-with-conditional-behaviour, §8 #6 "passive boss" guard).

### Beat 3.5 — Wave 2; Final Push  (40:00 – 45:00)

- **Player action:** absorbs bodyguard reinforcements while closing on Veerasen. Manages HP economy without the gate's Fort tiles (now too far south).
- **Intended emotion:** *attrition discipline — every action matters, no slack.*
- **Mechanic(s):** all stacked. Devika's *Mantra of Revival* (1 charge) becomes active in spirit — players are now thinking about whether to spend it.
- **Enemy AI:**
  - *Bodyguards:* vanguard charge.
  - *Veerasen:* advances toward Vikram if he's within 4 tiles; otherwise hunts the next-priority lord-class threat (Bhanu, the chariot warrior).
  - *Surviving wedge units:* fight to the death; Raider-Healer (if still alive) heals Veerasen as priority over self.
- **Scripted event:** none in this beat.
- **Dialogue trigger:** Acharya Devika line at start of any turn where she has the *Mantra of Revival* charge unspent and an ally is at < 25% HP: *"Vikram. The mantra is mine to spend. Tell me when."* (Player uses the action via the unit menu — not a popup.)
- **Decisions forced:** **3.**
  - (a) *Spend Mantra of Revival now (saves a unit) or hold for the boss fight (saves Vikram if he falls in Act 4)?*
  - (b) *Engage the bodyguards with Bhanu (clears them in 1–2 turns; pulls him away from the boss line) or wall them with Mahisha (slow attrition)?*
  - (c) *Position to bring Veerasen below 30% with the right unit (so the Spare action is reachable from the right adjacency)?* — sets up the anchor moment.
- **Playbook ref:** §6 (3–4 scripted encounter beats — wave 2 is the planned escalation peak before the anchor) + §4 Act 3 ("Encounter difficulty increases — A new enemy configuration forces creative application of known tools").

## Act 3 decision count: **13** (within target 12–15).

---

# Act 4 — Anchor Moment + Payoff  (45:00 – 55:00)

## Map state at act start

- **Player units:** all surviving units; positions vary based on player choices through Act 3. Vikram's position is the load-bearing variable for the anchor.
- **Enemy units:** **only Veerasen remains** (or 1–2 stragglers, finished in beat 4.1). Veerasen is at low HP, advancing.
- **Objective:** reduce Veerasen to < 30% HP — at which point the Spare action unlocks. The encounter becomes about *which* unit lands the threshold blow, not just whether.

## Pacing target

3 decisions per 5 min → **5–7 decisions across Act 4.**

## Failure mode this act guards against

§4 Act 4 ("anchor moment is purely narrative" OR "purely mechanical" OR "the choice has a wrong answer"). Mitigation: anchor is mechanical+narrative same event; both Spare and Strike are equally winnable; the prototype's victory state is the same either way (the difference is the closing image, not the outcome).

## Beats

### Beat 4.1 — Final Clearance  (45:00 – 48:00)

- **Player action:** kills the last 1–2 Act-3 stragglers (bodyguards or Raider-Lance) to clear the field for the boss fight.
- **Intended emotion:** *clinical control — the cleanup before the real moment.*
- **Mechanic(s):** all stacked.
- **Enemy AI:** stragglers fight to the death; Veerasen advances 1 tile per turn toward Vikram.
- **Scripted event:** none in this beat (intentional — the next beat is heavy, this one is air).
- **Dialogue trigger:** if Bhanu lands the killing blow on a straggler, his support line: *"That's the last of them. The pass is ours."*
- **Decisions forced:** **2.**
  - (a) *Who lands the threshold blow on Veerasen?* The Spare action has a positional requirement (next beat). The player must engineer the right unit's adjacency.
  - (b) *Heal-pre-spend or save?* If Devika heals now, she can position to be adjacent for the Spare; if she saves, she is held in reserve for revival.
- **Playbook ref:** §4 Act 4 (set up the anchor's mechanical preconditions in the 30 sec before the trigger).

### Beat 4.2 — Boss Combat  (48:00 – 50:00)

- **Player action:** engages Veerasen. He has the unique conditional AI from Act 3 (only attacks Vikram, refuses to attack civilians). Combat patter is unique.
- **Intended emotion:** *focused intensity — the duel.*
- **Mechanic(s):** combat resolution; the ritual-side of combat (no Marked Shot — already spent in Act 1).
- **Enemy AI:** *Veerasen:* "Final-stand — attacks only Vikram if any player unit is adjacent. If Vikram is not adjacent, attacks the nearest non-civilian. On HP < 30%, kneels — see Anchor Moment expansion."
- **Scripted event:** combat animations resolve normally; HP gauge ticks visibly.
- **Dialogue trigger:** Veerasen's third line at HP < 50% (or first-time-attacked-by-Vikram, whichever first): *"You are not the boy I trained beside. You are the king he became."*
- **Decisions forced:** **1.**
  - (a) *Use Vikram to deliver the threshold blow (sets up Vikram-spare imagery, but Vikram is also the unit Veerasen is hunting — risk)?* OR *Use Devika/Bhanu (safer, but the imagery of someone other than the lord granting mercy is weaker)?*
- **Playbook ref:** §8 #6 (boss with conditional behaviour, not passive) + §7 (mid-battle dialogue at HP threshold).

### Beat 4.3 — The Threshold; Anchor Setup  (50:00 – 52:00)

- **Player action:** the threshold blow lands. Veerasen drops to a knee. Camera lingers (3 sec). A new action menu entry — **"Spare"** — appears for the *adjacent* player unit on the next Player Phase.
- **Intended emotion:** *moral hesitation — the game is asking me a question.*
- **Mechanic(s):** **Spare** action (new — see implementation hook in Anchor Moment expansion below).
- **Enemy AI:** Veerasen does NOT attack on his next phase. He drops his weapon. (Coded: AI returns "no action" for him for the rest of the encounter regardless of player choice.)
- **Scripted event:** kneel cinematic (3 sec): blade lowers, sun crests over the temple silhouette behind him. (This is the 30-second-video-test shot.)
- **Dialogue trigger:** Acharya Devika delivers the *trigger line*: *"Vikram. He is broken, not lost. The dharma allows mercy."* Tactician (4th-wall — moment 3 of 3): *"Your call, tactician. Both paths are written."*
- **Decisions forced:** **0.**  (The next beat is the decision; this beat is the *setup* per the playbook's 30-second prior rule.)
- **Playbook ref:** §1 #7 + §5 ("the anchor moment lands when mechanical and narrative peaks are the same event"; 30-second setup prior).

### Beat 4.4 — The Choice  (52:00 – 54:00)

- **Player action:** **selects Spare or Strike** from the action menu of the adjacent unit.
- **Intended emotion:** *commitment — this one is mine.*
- **Mechanic(s):**
  - **Strike:** standard combat resolution; Veerasen dies; +EXP via `ExpGranter`; +1 unique relic added to convoy (his blade, named).
  - **Spare:** combat ends; Veerasen's unit is despawned from the enemy roster; a new player roster slot opens (unused in the prototype — flagged as "joins next chapter"); +0 EXP; -0 relic.
  - Both paths transition to Act 5's victory state. *Same victory state* — different framing, same load (§5 TO Balmamusa principle).
- **Enemy AI:** N/A — Veerasen is locked.
- **Scripted event:**
  - **Strike path:** 4-sec cinematic — drawn arrow, release, hit, fall. Veerasen's last line: *"I curse the throne that made us fight this, brother."*
  - **Spare path:** 4-sec cinematic — Veerasen lifts the hilt of his sword, hands it grip-first to Vikram, accepts a hand to his feet. Veerasen's surrender line: *"I will fight under your sun, then. Until the throne answers for what it did."*
- **Dialogue trigger:** the action selection.
- **Decisions forced:** **1.**  (The defining choice. ≥2 valid options by definition — both are winnable, both produce the same victory state, no obvious right answer.)
- **Playbook ref:** §1 #7 + §5 ("design the anchor as a player choice that has immediate tactical consequences AND permanent narrative consequences" + "for a grant evaluation prototype specifically: the evaluator needs to *do* something during the anchor moment, not just watch it").

### Beat 4.5 — Settling  (54:00 – 55:00)

- **Player action:** none — watches the cinematic conclude, the camera pull back, and the victory-state UI hand off to Act 5.
- **Intended emotion:** *exhalation — the choice is made.*
- **Mechanic(s):** state transition to a brief in-fiction tableau.
- **Enemy AI:** N/A.
- **Scripted event:** camera sweeps from Veerasen's body / kneeling figure back to the temple, dawn breaking, ritual-fire visible through the gate.
- **Dialogue trigger:** none (silence held intentionally, ~5 sec).
- **Decisions forced:** **0.**
- **Playbook ref:** §4 Act 4 (the payoff is *given*, not earned again — the player has spent their decision-budget).

## Act 4 decision count: **4** (within target 5–7 — adequate; the anchor is the load-bearing decision and is intentionally surrounded by lower-density beats so it lands).

## Anchor moment — explicit expansion

| Component | Specification |
|-----------|---------------|
| **Setup (30 sec prior)** | Beat 4.3. Kneel cinematic; sun crests; Devika's trigger line; tactician's audience-aware acknowledgement. Visual: blade lowered, archer's drawn arrow held, sacred fire visible behind. |
| **Trigger (mechanical)** | Veerasen's HP transitions below 30% AND a player unit is adjacent AND it is the start of that player unit's selectable turn. Action menu gains "Spare" entry, only available to the adjacent unit. Implementation: action-menu condition `target.IsBoss && target.HP < target.MaxHP * 0.3 && target.AnchorEligible`. |
| **Trigger (narrative)** | Devika's "the dharma allows mercy" line + tactician's "your call" beat. Both fire on the same turn, before the player selects. Player has unlimited time to choose — no clock pressure on the anchor. |
| **Player payoff (immediate)** | Strike → arrow-release cinematic + curse-line + EXP/relic gain. Spare → hilt-handover cinematic + surrender-line + roster slot. Both are 4 seconds, both are wholly cinematic (player has no input during them). |
| **Re-contextualization (Acts 4.5–5)** | Strike path → Devika's closing voiceover questions the cost of decisive dharma; final shot is Vikram alone with the relic. Spare path → Veerasen walks beside Vikram in the closing tableau; final shot has both silhouettes against the temple. The *same victory text* prints over both endings; the *image and one closing line* differ. |
| **30-second-video-test** | The kneel-and-arrow shot from beat 4.3 + the 4-second resolution cinematic from beat 4.4 form a self-contained 30-sec clip. A viewer with no context sees: warrior on a knee at a temple gate, sun rising, archer's arrow drawn, then either lowered or fired. The Indian-mythology iconography (sacred fire, gate, dawn) carries the stakes without text. ✅ |

---

# Act 5 — Resolution + Hook  (55:00 – 60:00)

## Map state at act start

- Combat is over. All units gather at the inner sanctum. The throne is visible. Devika stands at the ritual fire.

## Pacing target

0 decisions per 5 min → **0–2 decisions across Act 5** (intentionally a denouement).

## Failure mode this act guards against

§4 Act 5 ("ending on a boss death with no narrative aftermath", "delivering exposition that answers questions rather than raising new ones", "victory screen without emotional residue"). Mitigation: ritual-closure dialogue + tactician's final 4th-wall break + an antagonist line that *raises* a question.

## Beats

### Beat 5.1 — Ritual Closure  (55:00 – 58:00)

- **Player action:** none — narrative beat. (If user has *Continue* button, they can pace through.)
- **Intended emotion:** *grave warmth — what was held was real.*
- **Mechanic(s):** state transition to in-fiction dialogue overlay; chapter-clear UI in the wings.
- **Scripted event:** Acharya Devika walks to the ritual fire, raises both hands. Camera holds on the fire. She delivers the closure: *"What we held was not the gate. It was the order behind it. And the order remembers, even when the world does not."*
- **Dialogue trigger:** auto-advance after Veerasen cinematic.
- **Decisions forced:** **0.**
- **Playbook ref:** §7 (post-battle dialogue 60–90 sec; total narrative time within the 4–5-min budget).

### Beat 5.2 — The Hook  (58:00 – 60:00)

- **Player action:** none.
- **Intended emotion:** *unease — the door was held; the corridor behind it is dark.*
- **Mechanic(s):** state transition to credits/title.
- **Scripted event:**
  1. Vikram looks north (toward the mountain pass).
  2. Off-screen voiceover (a new antagonist, never seen in the prototype): *"You think you've protected this place. You don't yet know what it protects against."*
  3. Title card: **PROJECT ASTRA**, fade to black. End.
- **Dialogue trigger:** auto-advance.
- **Decisions forced:** **0.**
- **Playbook ref:** §4 Act 5 ("end with one line of dialogue from the antagonist or a surviving enemy commander that reframes everything" + "end on a question, not an answer").

## Act 5 decision count: **0** (within target 0–2).

---

## Total decision count audit

| Act | Beat-by-beat sum | Target band | Within band? |
|-----|------------------|-------------|--------------|
| 1   | 0 + 2 + 1 + 2 + 1 = **6** | 5–7 | ✅ |
| 2   | 2 + 2 + 1 + 1 + 2 = **8** (or 9 with escort) | 8–10 | ✅ |
| 3   | 2 + 3 + 3 + 2 + 3 = **13** | 12–15 | ✅ |
| 4   | 2 + 1 + 0 + 1 + 0 = **4** | 5–7 | borderline (intentional — anchor isolation) |
| 5   | 0 | 0–2 | ✅ |
| **Total** | **31 (32 with escort)** | 30–45 | ✅ |

Act 4 sits 1 below the band intentionally. The anchor moment is the densest *single* decision in the prototype — surrounding it with low-density beats is the design move that makes it land (§5 — TO Balmamusa is one decision wrapped in a chapter of setup, not a sequence of decisions).

---

## 4th-wall narration framing

The narrator is **the tactician** — a diegetic role borrowed from FE7's Lyn-mode (§2 4th-wall section). The tactician is *in-fiction* a court adviser; *out-of-fiction* the player. The two are blurred deliberately. The character of Vikram *believes* the tactician is real. So does the player.

There are **three audience-aware moments** where the tactician explicitly addresses the grant board:

1. **00:30 — Setup.** *"Tactician. The Suryavansh court trusts your hand. The board is yours."*
2. **25:00 — Phase shift.** *"That's Phase 1. The board flips now — we're the ones moving."*
3. **52:00 — Anchor.** *"Your call, tactician. Both paths are written."*

These are the *only* moments where the fourth wall is touched. Everything else — every other dialogue trigger, every tutorial card, every level-up — stays in-fiction. The fourth-wall budget is small on purpose; over-using it makes the prototype read as quirky rather than confident (§2 — "what makes it cringe: direct address to the player without an in-universe intermediary").

The tactician never names the player and never names the grant board literally. The phrasing ("tactician", "the board", "your hand") is calibrated so that an in-fiction reading is fully coherent (it's an adviser briefing) AND an out-of-fiction reading is fully coherent (it's the game's narrator addressing the grant evaluator). Both are true at once. This is the FE7 Lyn-mode trick.

---

## Failure-mode guard table

Cross-walk between §8's seven indie SRPG prototype failure modes and the design moves that counter each:

| § | Failure mode | Where it would hit | Design move that counters it |
|---|--------------|--------------------|------------------------------|
| §8 #1 | Demo Fatigue by Unit Count | Pre-deployment + Act 1 | Only 3 starting units; Vidya joins minute 12, Bhanu minute 25. Staged disclosure mirrors FE7 Lyn-mode (§2). |
| §8 #2 | No Mechanical Identity for the Lord | Act 1 | *Marked Shot* (lord-only ability), guaranteed-crit first use, telegraphed, fires by minute 4. Vikram also has Permadeath = game-over (UM-02), unique to him. |
| §8 #3 | Narrative-Combat Disconnect | All acts | Every dialogue trigger is bound to a tactical event (HP threshold, action choice, turn count, unit-vs-unit pairing). No floating cutscenes between fights. Total dialogue time ≤5 min of the 60. |
| §8 #4 | Grid Confusion | All acts | 7 terrain types in play, each documented as changing ≥1 player decision (see Map architecture table). The first time Forest/Fort/Mountain/Ruin is engaged, a one-line tutorial card fires *during* the player's action context (§2 popup-tutorial rule). |
| §8 #5 | Ambush Spawns Without Telegraph | Act 2 turn 7, Act 3 turn 14 | Both spawn tiles visible at map start as red-glow indicators. Both spawns telegraphed 1 turn ahead. Both spawns occur during Enemy Phase, giving player a full action economy before reinforcements attack. |
| §8 #6 | The Passive Boss | Act 3 + Act 4 | Veerasen has 3 conditional behaviours: zone-locked → activates on HP/position trigger → final-stand AI → kneels at HP < 30%. Never a stationary throne-sitter. |
| §8 #7 | The 40-Hour Vertical Slice | Scope as a whole | No class change, no support conversations, no base management, no world map, no recruitment system (Spare path *foreshadows* recruitment but does not implement it). Single map, single encounter, 5 player units, 5 enemy types + boss. |

---

## Implementation hook list (handoff to engineering)

The following are NOT design decisions, they are the *minimum* engineering surface this design requires beyond what is already shipped:

1. **Map authoring** — re-author `BattleMap.unity` (or new scene) at 18×20 with the terrain layout above. Use existing `TerrainStatTable` and `MapRenderer`. Throne tile already authored (`TerrainType.Throne` index 18).
2. **Marked Shot ability** — new `UnitAbility` ScriptableObject + `GridCursor` action-menu entry. Guaranteed-crit override flag in `CombatRound`. Lord-only gate via `UnitDefinition._isLord` (already exists).
3. **Spare action** — new action-menu entry. Conditional on `target.IsBoss && target.HP < target.MaxHP * 0.3 && unit.IsAdjacent(target)`. Triggers boss-despawn + roster-slot-add (slot stays unused in prototype) + cinematic.
4. **Reinforcement spawn telegraph** — render a red-glow indicator on map-start tiles (rows 0, cols 4 + 14). On scripted turn N, a brighter pulse fires; on N+1, units spawn. Indicator system can be a single overlay-tile prefab.
5. **Five scripted enemy AI behaviours** — see Acts 1–3 for specs. Each is a small finite-state machine on the unit's MonoBehaviour, evaluated during Enemy Phase. Generalist pathfinding + custom priority table.
6. **Veerasen boss AI** — three states (zone-lock, advance, final-stand) + the kneel/spare resolution. Trigger: HP threshold + position.
7. **Two cinematic sequences** — kneel-cinematic + Strike/Spare resolution. Use `DialogueSequencePlayer` + camera-pan helper. Each ≤4 sec.
8. **Ritual HP gauge** — top-of-screen UI bar tied to a per-turn enemy-adjacency check. Goes away at minute 25.
9. **Optional escort NPCs** — 2 villager units with a fixed scripted-path mover (1 tile/turn south along col 8). No AI. Their presence on a tile contributes to Phase-1 ritual-HP loss if an enemy is adjacent. Cut first if scope demands.

Estimated engineering scope: ~3 weeks (within the Phase 1 production-pipeline budget for content work).

---

## Open questions for next iteration

1. *Does the tactician have a name?* If yes, the 4th-wall framing tightens (the tactician is a real character the player inhabits). If no, the framing is more abstract. Recommend deciding before Phase 1 narrative writer engages.
2. *Does the player see Veerasen in Act 1 or Act 2 at all?* Currently he is first visible at minute 25. Earlier visibility would deepen the anchor's emotional weight (FFT Algus-Teta principle); but it would steal screen time from mechanic introduction. Tradeoff to playtest.
3. *Should Marked Shot be once-per-map or once-per-encounter (recharges between Phase 1 and Phase 2)?* Current spec is once-per-map for tactical scarcity. Playtest will tell us if Phase 2 feels under-equipped.
