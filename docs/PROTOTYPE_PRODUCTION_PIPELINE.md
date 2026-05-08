# Prototype Production Pipeline

## Critical path (the spine)

```
Art Direction Lock ──┬──► Asset Production ──┐
                     │                       │
World Bible Outline ─┴──► Unit/Map Design ───┼──► Integration ──► Playtest Iteration ──► Marketing Cut
                                             │           ▲
                Codebase Refactor (parallel) ┘           │
                                                  Audio Pass ┘
```

Three things gate everything else: **art direction lock**, **map + tutorial design doc**, and **codebase refactor**. The first two unblock contractors; the third unblocks integration. Run them in parallel.

---

## Phase 0 — Foundation Lock (Weeks 1–3)

**Goal:** make the prototype contractable. Every downstream piece breaks if this is fuzzy.

| Task | Owner | Dependency | Time |
|---|---|---|---|
| Mood board + reference pack (Fire Emblem GBA / Tactics Ogre / Triangle Strategy / Indian miniature painting + Amar Chitra Katha refs) | You | — | 3–5 days |
| Concept artist brief + hire | You | mood board | 3–5 days |
| **Art Direction Bible** (2–3 style explorations → pick one → refined style guide: palette, line-weight, portrait style, tile language, UI motifs, 1 hero key-art) | Concept Artist | brief | 2–3 weeks |
| World-bible outline (factions, mythology framing, themes — bullets only) | You / Narrative Writer | — | 1 week |
| Unit roster design doc (4–6 PCs + 5 enemies + 1 boss: class, role, personality one-liner, visual hook) | You | world bible | 1 week |
| Map design doc + tutorial flow on paper (defensive + offensive sessions, beat-by-beat) | You | unit roster | 1 week |

**How to lock art direction (your open question):** don't try to do it in pixel art directly. Hire one concept artist for 2–3 weeks to deliver a full-color **style bible** (key-art + character lineup + tile palette + UI motif). Pixel artists translate *from* that bible. Without this step, every contractor drifts and you'll re-do work twice.

---

## Phase 1 — Parallel Production (Weeks 4–13)

Three independent tracks running simultaneously.

### Track A — Art Production (the long pole)

Order matters: portraits unblock the narrative team, map sprites unblock playtesting, battle sprites are last because they're animation-heavy.

| Task | Dependency | Time |
|---|---|---|
| Title screen + logo (anchor piece #1) | art bible | 1.5 weeks |
| 1 hero portrait at near-final quality (anchor piece #2 — the one for the pitch deck cover) | art bible | 1 week |
| Remaining portraits (4 PCs + 5 enemies + 1 boss = 10) at consistent-but-rougher quality | hero portrait | 4–5 weeks |
| Map sprites (13 units) — readable silhouettes, consistent palette | art bible | 3–4 weeks |
| Battle sprites + idle/attack/hit animations (anchor piece #3 — *one* hero gets a fully animated combat anim, others get a basic 2-frame loop) | map sprites | 4–6 weeks |
| 1 styled tileset for the hero map biome | art bible | 2–3 weeks |
| 1–2 battle scene backgrounds | tileset | 1 week |
| Weapon + item icons (~10) | art bible | 3–5 days |

### Track B — Codebase Refactor (Layer 1)

| Task | Time |
|---|---|
| Audit current architecture, list debt items blocking content iteration | 3–5 days |
| Refactor only the systems content authors will hit (map data pipeline, unit data pipeline, dialogue pipeline) | 2–3 weeks |
| Build pipeline: one-click `.exe` + itch upload, debug UI off by default | 1 week |
| Settings menu (resolution, audio, exit) | 3–5 days |

### Track C — Narrative + Design

| Task | Dependency | Time |
|---|---|---|
| 4th-wall-breaking script for tutorial + 2 sessions (~800–1500 words total, very light) | unit roster, hero portrait done | 1.5–2 weeks |
| Map iteration #1 in greybox (using current placeholder art + your refactored data pipeline) | refactored map pipeline | 1 week |
| Tutorial scripted into the engine | script + map iter 1 | 1 week |

---

## Phase 2 — Integration & Audio (Weeks 14–17)

Everything converges. This is where the prototype stops being parts and becomes a build.

| Task | Owner | Time |
|---|---|---|
| Drop final art into engine, replace placeholders | You | 1.5–2 weeks |
| Audio pass: 2–3 AI-generated music tracks (menu, battle, tense moment) curated + mastered lightly | You + light SFX contractor | 1 week |
| SFX kit (movement, attack hit/miss, UI confirm/cancel, level-up, victory) — use a paid library (GameDev Market / Humble) + AI-generated where it fits | You / SFX contractor | 1 week |
| Recording setup (clean capture pipeline: OBS profile, hidden debug, stable framerate) | You | 2–3 days |

---

## Phase 3 — Playtest & Iteration (Weeks 18–20)

Non-skippable. Tutorials and "fun" only emerge through iteration.

| Task | Time |
|---|---|
| Internal playtests (5–10 sessions with friends/devs who haven't seen it) | 1 week |
| Tutorial clarity pass + map balance pass | 1.5–2 weeks |
| Bug bash, crash hunt, build hardening | 3–5 days |

---

## Phase 4 — Grant Deliverables (Weeks 21–23)

Only after the build is stable. Recording an unstable build wastes everyone's time.

| Task | Owner | Time |
|---|---|---|
| 2–4 min gameplay capture + edit (anchor encounter, anchor art on screen, narration optional) | Video editor + you | 1.5 weeks |
| 30-sec social teaser (cut from same footage) | Video editor | 2–3 days |
| Pitch deck (12–15 slides) — your hand on copy, designer for layout | You + deck designer | 1.5 weeks |
| Elevator pitch (1 sentence + 1 paragraph), one-page GDD, mood board PDF | You | 3–5 days |

---

## Total timeline

**~5.5 months** (23 weeks) part-time solo lead with contractors layered in. Compress to **~4 months** if you go full-time and run art tracks aggressively in parallel. Add ~1 month buffer realistically — pixel art always slips.

---

## Roles + compensation

Rates assume India-based contractors with international portfolios (typical for mid-tier indie work). Western/EU rates are 2–3× higher.

| Role | Scope on prototype | Engagement | Estimated cost (USD) |
|---|---|---|---|
| **Concept Artist / Art Director** | Phase 0 style bible, 1 hero key-art, ongoing art-lead consultation | 3 weeks contract + light retainer | **$1,800 – $4,000** |
| **Pixel Artist — Characters** | 11 portraits, 13 map sprites, 13 battle sprites + anims for hero (anchor) + simple loops for rest | 8–10 weeks | **$3,500 – $7,500** |
| **Pixel Artist — Environments/Tiles** | 1 tileset, 1–2 battle backgrounds, weapon/item icons, title screen | 4–5 weeks | **$1,500 – $3,500** |
| **UI Artist** *(can overlap with environment artist)* | UI polish for the screens that show in the gameplay video; existing screens get a styling pass per the bible | 2–3 weeks | **$700 – $1,800** |
| **Narrative Writer** | 4th-wall tutorial script + character voice lines; very light scope | 2 weeks | **$400 – $1,200** |
| **Composer** *(optional — AI music covers most of this)* | 1 hero theme polished by a human if AI feels off; or skip entirely | 1 track or skip | **$0 – $600** |
| **SFX / Audio Designer** *(optional — libraries cover most)* | Mix pass + 5–10 custom hits | 1 week | **$0 – $500** |
| **Video Editor** | 2–4 min gameplay video + 30-sec teaser | 2 weeks | **$300 – $900** |
| **Pitch Deck Designer** *(optional)* | Layout + visual polish on the deck you write | 1 week | **$300 – $800** |
| **You (lead/PM/programmer/designer)** | Refactor, integration, design, project management, copy | Full timeline | — |

**Total contractor budget range: ~$8,500 – $20,800 USD** (≈ ₹7L – ₹17L). Realistic mid-target: **~$13,000 (₹11L)**. Trim aggressively by skipping composer + SFX designer, doing your own pitch deck layout, and finding a single artist who handles characters + environment.

---

## Risk callouts

- **Art direction drift** is the #1 prototype killer. If the bible isn't tight enough that two pixel artists produce visually consistent work without your intervention, redo the bible. This is cheaper than redoing assets.
- **The 60-min playtest is a content-design problem, not a code problem.** Budget the 2–3 weeks of iteration honestly — a "technically works" tutorial is not a passing grant prototype.
- **Don't start the gameplay video until the build is stable.** Re-recording is brutal; one stable capture session saves a week.
- **The anchor pieces are non-negotiable.** Resist the urge to spread polish thin across all assets — concentrate it in: title screen, hero portrait + battle anim, hero map opening shot. Those three carry the pitch.
