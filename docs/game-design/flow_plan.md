# 60-Minute Prototype — Flow Plan (Brief)

*Half-page brief. Companion to `flow_design.md` (full beat-by-beat). Every claim traces to `SRPG Prototype Design Playbook.md`.*

---

## Encounter concept

**The Defense of the Sun-Temple.** A single 18×20 map. One physical location, two objective regimes:

- **Phase 1 — Defensive (0:00–25:00).** A small force of the Suryavansh court holds the temple gate while Acharya Devika completes the *Surya-mantra* ritual. Win = Survive 6 turns AND ritual HP gauge > 0.
- **Phase 2 — Offensive (25:00–55:00).** Ritual completes. The rebel commander is fleeing through the mountain pass with a stolen relic. Win = Reach the commander OR force surrender within 12 turns.

Same geography, two regimes, transition at minute 25 — the Three Houses Ch.1/7/17 multi-phase reuse pattern compressed to 60 minutes (§1 #5).

## Anchor moment (Act 4, 52:00)

**Spare-or-Strike Veerasen** — the rebel commander reaches one knee at the temple gate, surrounded. The action menu offers a new entry: **"Spare"**. Mechanical click *is* the moral choice (§1 #7).

- **30-second-video test:** kshatriya warrior on one knee at the temple gate, blade lowered, sun rising; Vikram's drawn arrow either lowers or fires. Visually legible without context.
- **Re-contextualization:** chosen path colours Acts 4–5 framing, but the same victory state is reached either way. No "wrong" choice (§5 — TO Balmamusa principle: both paths fully realized).

## Roster (5 player + 5 enemy + 1 boss + optional 2 ally)

| # | Name | Class | Move | Signature mechanic |
|---|------|-------|------|--------------------|
| 1 | **Vikram** | Archer-prince (Lord) | 5 | *Marked Shot* — 1 guaranteed crit/map (telegraphed). Permadeath = game over (UM-02 already shipped). |
| 2 | **Gajen** | Gajaaroha (elephant) | 6 | High HP, blocks 1-tile gates as living chokepoint; cannot enter forest/mountain. |
| 3 | **Acharya Devika** | Brahmin priestess (Mage + Healer hybrid) | 4 | Ranged magic 1–2 + heal staff; *Mantra of Revival* (1 charge, revives at 1 HP). |
| 4 | **Vidya** | Vidyadhari (flier) | 7 | Terrain immunity (already shipped); *Scout's Eye* reveals fog. Low HP. |
| 5 | **Bhanu** | Rathi (chariot) | 7 | Lance attack 1–2; cannot enter 1-tile-wide chokepoints. |

**Enemies (5):** 3 Raider-Sword, 1 Raider-Archer, 1 Raider-Lance — each with one named scripted behaviour (§6).
**Boss:** **Veerasen** — fallen kshatriya. Zone-locks until temple HP < 50%, then advances. Refuses to attack civilians.
**Optional allies:** 2 villager NPCs (escort to evac tile, scripted path) — first to cut if scope demands, per spec.

## Top 5 playbook insights applied (with traceability)

1. **§1 #5 — Single map, multi-phase via objective shift.** Drives the entire structural choice: one map, two objective regimes, transition at minute 25. *Why*: a 60-minute prototype cannot afford a second map's authoring + onboarding cost; objective-shift produces a fresh tactical experience for free.

2. **§1 #4 — Defensive win conditions produce more interesting decisions than "kill all".** Phase 1 is *Survive 6 turns + protect ritual HP*, not *rout*. *Why*: a "kill all" first phase produces dead turns chasing the last raider; *survive + protect* keeps every turn under pressure regardless of player skill.

3. **§1 #2 — One mechanic per 3–5 minutes (FE7 Lyn-mode pace).** Act 2 introduces flying / terrain bonus / healing tiles / canto on a strict 5-minute cadence. Never two new mechanics in the same window. *Why*: the playbook's empirical upper bound; violating it produces choice-paralysis on first deployment (§8 #1, "Demo Fatigue by Unit Count").

4. **§1 #7 + §5 — Anchor lands when mechanical and narrative peaks are the same event.** *Spare* is one click that *is* the moral choice. Not a cutscene wrapped around an unrelated boss kill. *Why*: pure-narrative anchors are forgettable (§5); pure-mechanical anchors require extended play (§5 ITB push-pull). The 60-minute prototype must combine both for a grant evaluator who plays once.

5. **§1 #6 + §6 — Scripted enemy behaviour outperforms generalist AI for narrative encounters.** Each of the 5 raiders + Veerasen gets 1–2 named behaviours (vanguard charges, archer holds elevated ruin, healer retreats below 40% HP, boss zone-locks until temple HP < 50%, etc.). *Why*: predictable scripted AI is *legible* — first-time players read it within 2–3 turns and start exploiting it, which is exactly the on-ramp a one-shot prototype needs.

---

*See `flow_design.md` for the full 5-act beat-by-beat with map architecture, per-beat decisions, AI scripts, dialogue triggers, and the failure-mode guard table.*
