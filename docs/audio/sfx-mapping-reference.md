# Project Astra — SFX Mapping Reference

A working reference to turn the raw SFX library in `Assets/External Assets/SFX ASSETS/` into wired game sounds. For **every** place in the game a sound could naturally play, this lists the recommended pack + candidate clips so you can **audition and decide**. Nothing here is wired yet — this is the menu to order from.

> **Why this is a reference, not a finished wiring:** I can't hear the clips, and almost every sound has 5–10 variations you'll want to pick by ear. So I've narrowed each slot to a pack + a short candidate list; you audition and tell me the winners, and I wire them.

---

## 0. Decisions to make first

A few upfront choices shape everything below:

1. **Pick ONE primary UI palette** (for consistency — don't mix click styles across menus). Candidates:
   | Palette | Vibe | Fits our UI direction? | Note |
   |---|---|---|---|
   | **CelerisLab Complete UI** ★ | Modern, clean, 48kHz/24-bit, context-labelled (100+ actions) | **Best fit** — our UI is Modern HD | Premium, most complete; has open/close/confirm/error/buy/level-up/buff already named |
   | UI Soundpack → *Modern* / *Minimalist* | Clean, neutral | Good fit | Free-ish; **needs attribution** (CC-BY, Nathan Gibson) |
   | JDSherbert *Ultimate UI* | Polished sci-fi | OK | Free |
   | JDSherbert *Pixel UI* | Chiptune (Saw/Sine/Square) | Off — our UI is HD, not pixel | Save for a retro option |

   Our locked UI direction is **Modern HD** (pixel only for on-map sprites + combat animations), so **CelerisLab is the natural primary UI palette**, with UI Soundpack *Modern* as a free fallback.

2. **Unit voice barks — yes/no?** The **Super Dialogue Pack** (5 actors × 10 categories incl. *Damage/Death/Grunting/Shouting*) can give Fire-Emblem-style vocal feedback on select/attack/hurt/die. Big personality boost, but it's a stylistic call (and you'd assign voices per character). Decide if you want this layer.

3. **Music is an audition-by-vibe task,** not a 1:1 map. None of the packs are explicitly Indian-instrument; treat **Helton Yan Tranquility** (calm/menu) and **JDSherbert Ambiences** (10 location loops) as *placeholders* until you source on-theme tracks (or generate them — see `docs/research/ai-sfx-tools.md`).

---

## 1. How wiring works (so the tables make sense)

- **The system is ready.** `AudioManager.Instance.Play(SoundId.X)` (one-shot), `.PlayMusic(id, fade)`, `.PlayAmbient(id)`. Each `SoundId` → a `SoundSO` (clip list + bus + volume + pitch range) → wired in `AudioLibrary.asset`. Today only **UiConfirm, HitCrit, RakshasaRoar, VillagerScream** are wired.
- **One SoundSO can hold ALL variations of a sound.** Drop all 6 sword-slash clips into one `SoundSO`; it picks one at random per play. Add a small **pitch range** (e.g. 0.95–1.05) and repeated sounds (footsteps, cursor, hits) stop feeling robotic. **Use this everywhere** — it's the single biggest "feel" win.
- **Format:** these packs duplicate every sound across Mono/Stereo × wav/mp3/ogg × HD/SD. **Use `Stereo / wav (HD)`** and ignore the rest.
- **Gitignore:** `External Assets/` is gitignored. To ship a sound I'll **copy** the chosen clip into `Assets/Audio/<category>/` (tracked), then make the SoundSO point there. The big packs stay un-committed.
- **Licensing flags** (verify before launch): UI Soundpack = **CC-BY, must credit Nathan Gibson**; Super Dialogue = **credit the voice actors** per its license; JDSherbert FREE packs + CelerisLab + Helton Yan + Shapeforms = commercial-OK (Shapeforms previews are free samplers of paid libraries). Most Asset-Store "FREE" packs allow commercial use *inside a game* — just don't redistribute the raw files.

---

## 2. The aesthetic split

Mirror the established look: **Modern HD UI, pixel combat/on-map.**

| Layer | Source packs |
|---|---|
| **UI / menus (HD)** | CelerisLab Complete UI (primary) · UI Soundpack *Modern/Minimalist* · JDSherbert Ultimate UI |
| **Combat & on-map (stylized/pixel)** | Helton Yan Pixel Combat (2,100 designed impacts/magic/whoosh) · Shapeforms *Hack & Slash* / *Hit & Punch* (organic slashes & flesh hits) |
| **Human voice** | Super Dialogue Pack (barks, screams, death cries) |
| **Footsteps** | JDSherbert Footstep Foley (16 surfaces) — incl. Grass/Sand/Stone/Water for our map terrains |
| **Music / ambience** | Helton Yan Tranquility 1 & 2 (menu/cinematic) · JDSherbert Ambiences (location loops) |

---

## 3. Master mapping (by system)

Legend: **[WIRED]** already done · **[+SoundId]** needs a new SoundId · candidates are *starting points to audition in-folder*.

### A. UI / Menus  → palette = CelerisLab (HD)
| Moment | Proposed SoundId | Candidate clips | Notes |
|---|---|---|---|
| Confirm / select (all menus) | `UiConfirm` [WIRED] | CelerisLab `basic.../button click` or `standard_button_click_in` | Re-point the existing UiConfirm to the chosen HD click |
| Cancel / back | `UiCancel` [+] | CelerisLab `button click_out`, or `dropdown` close | |
| Navigate / cursor move in menus | `UiMove` [+] | CelerisLab soft button hover; UI Soundpack `Minimalist` tick | Keep it *quiet* — it fires a lot |
| Invalid / denied | `UiError` [+] | CelerisLab `negative.../access_denied`, `action_failed` | |
| Panel / menu **open** | `UiOpen` [+] | CelerisLab `inventory_open`, `dramatic_button` | Forecast, action menu, info panel, pause |
| Panel / menu **close** | `UiClose` [+] | CelerisLab `inventory_close` | |
| Tab / category switch | `UiTab` [+] | CelerisLab `shop_category_select`; JDSherbert *SciFi UI* `Swipe` | Convoy tabs, info-panel pages |
| Hover (mouse) / slider tick | `UiHover` [+] | CelerisLab button `hover`; UI Soundpack `Minimalist` | Optional polish |
| Settings dropdown change | reuse `UiConfirm` | — | SettingsMenuOverlayUI `OnSpeedChanged` |

Hooks: TitleScreenUI (wired), MainMenuUI, GameOverUI, ChapterClearUI, BattleMapPausedOverlayUI, SettingsMenuOverlayUI, ConfirmDialogUI, UnitActionMenuUI, CombatForecastUI, InventoryMenuUI, TradeScreenUI, ConvoyUI, UnitInfoPanel.

### B. Cursor & grid (on-map)
| Moment | SoundId | Candidates | Notes |
|---|---|---|---|
| Cursor move (free) | reuse `UiMove` [+] | A *very* soft tick — UI Soundpack `Minimalist`, or Helton Yan `UIMisc` | Fires constantly w/ DAS — keep subtle + pitch-vary |
| Cursor blocked / invalid tile | `UiError` [+] | CelerisLab `access_denied` | GridCursor:198 |
| Unit selected | `UnitSelect` [+] | CelerisLab `standard_button click`; or Super Dialogue *Greeting* bark (if voices) | GridCursor:219 |
| Unit deselected / cancel | `UiCancel` [+] | — | GridCursor:250 |
| Move committed | `MoveCommit` [+] | Helton Yan `WHSH_MOVEMENT-Simple Whoosh` | GridCursor:224 |
| Footstep per tile | `Footstep*` [+] | JDSherbert Footstep Foley — **Grass/Sand/Stone/Water** by terrain | UnitMover:56 `onTileEntered`. Could be one generic step to start |

### C. Combat (pixel animation)
| Moment | SoundId | Candidates | Notes |
|---|---|---|---|
| Attack initiate / swing | `AttackSwing` [+] | Shapeforms `Blade Swing Noise Slice`, `Dagger Slash`; Helton Yan `SWSH_*` | |
| **Hit (physical)** | `HitPhysical` [+] *(enum exists, unwired)* | Shapeforms `HIT_SLAP`, `PUNCH_*HEAVY`; Helton Yan `FGHTImpt_HIT-Strong Smack` | **High priority — combat has no hit sound today** |
| **Crit** | `HitCrit` *(wired only in prologue)* | Helton Yan `FGHTImpt_MELEE-Kick Critical`; Shapeforms `Gore Splat Stab` | Wire it into real combat too |
| **Miss / dodge** | `Miss` [+] *(enum exists, unwired)* | Shapeforms `WHOOSH_ARM_SWING`, `Whoosh Short Light`; Helton Yan `WHSH_*` | |
| Magic / skill cast | `MagicCast` [+] | Helton Yan `MAGSpel_CAST-*` (85 options), `DSGNTonl_SKILL IMPACT-*` | For mage units |
| Unit death (in combat) | `UnitDeath` [+] | Super Dialogue *Death* (`death_*`); + Helton Yan impact thud | The fall/defeat beat in CombatPlaybackController |
| Combat start transition | `CombatStart` [+] | Helton Yan percussive `DSGNImpt_*` sting | Optional — into the combat animation |

Hooks: CombatExecutor, CombatPlaybackController, CombatPlaybackDispatcher, CombatForecastUI confirm.

### D. Units · progression · items
| Moment | SoundId | Candidates | Notes |
|---|---|---|---|
| EXP gain — counter ticks | `ExpTick` [+] | UI Soundpack `Minimalist`; CelerisLab `currency_gained` | ExpGainOverlayUI:70 (per increment, subtle) |
| **Level up** | `LevelUp` [+] | CelerisLab `positive.../level_up` (7 takes) or `upgrade_applied`; Helton Yan `MAGAngl_BUFF-Buff Pickup` | LevelUpScreenUI |
| Stat-up row reveal | `StatPip` [+] | UI Soundpack `Minimalist` blip | Optional per-row tick |
| Heal (staff / healing tile) | `Heal` [+] | Helton Yan `MAGAngl_BUFF-Simple Heal`, `Healing Gusts` | HealingTileSystem, staff combat |
| Buff / debuff applied | `BuffApplied`/`DebuffApplied` [+] | CelerisLab `buff_applied` / `debuff_applied_deep` | |
| Item equip / use / discard | `ItemEquip` etc. [+] | CelerisLab `item_equip` / `item_pick_up` / `item_drop` | Inventory submenu |
| Item / trade / convoy transfer | `ItemMove` [+] | CelerisLab `item_drag` / `item_stack` | Trade + Convoy give/take |
| Gold / reward gained | `GoldGain` [+] | Shapeforms `Coin Flung`, `Coin Dropped`; Helton Yan `DSGNTonl_USABLE-Coin Toss` | |

### E. Turn / phase
| Moment | SoundId | Candidates | Notes |
|---|---|---|---|
| Player phase banner | `PhasePlayer` [+] | Helton Yan bright `DSGNTonl_*` / Tranquility `Achievement`; CelerisLab `objective_reached` | PhaseBannerUI:72 |
| Enemy phase banner | `PhaseEnemy` [+] | Helton Yan minor/tense sting; Tranquility Pt2 `Deep Lurker` | |
| Allied phase banner | `PhaseAllied` [+] | A third, neutral cue | |
| Turn counter advance | reuse phase cues | — | |
| **Victory** | `MusicVictory` / `Fanfare` [+] | CelerisLab `mission_success`; Helton Yan Tranquility `MUSCMisc_Major-Achievement` | ChapterClearUI |
| **Defeat / game over** | `MusicGameOver` [+] | Helton Yan Tranquility *Minor* drone; Shapeforms somber ambience | GameOverUI, LordDeathWatcher |

### F. Dialogue
| Moment | SoundId | Candidates | Notes |
|---|---|---|---|
| Typewriter char blip | `DialogueBlip` [+] | UI Soundpack `Minimalist`/`Wood Block` soft tick; **or** Super Dialogue *Grunting* (per-char voice, FE/Undertale style) | DialogueView:37 — fires per character, must be tiny + pitch-varied |
| Line advance (Confirm) | reuse `UiConfirm` | — | DialogueSequencePlayer:62 |
| Nameplate appears | `NameplatePop` [+] | CelerisLab subtle `notification` | Optional |

### G. Scene · music · ambience
| Moment | SoundId | Candidates | Notes |
|---|---|---|---|
| Title music (loop) | `MusicTitle` [+] | Helton Yan Tranquility `MUSCMisc_Major-*`; JDSherbert Ambiences `Cosmic Star` | TitleScreenUI |
| **Map / exploration music** | `MusicMap` *(enum exists, unwired)* | JDSherbert Ambiences `Tiger Temple Trial` / `Dark Dark Woods` | Wire on BattleMap entry |
| **Battle music** | `MusicBattle` *(enum exists, unwired)* | JDSherbert Ambiences (intense loop); Helton Yan Tranquility Pt2 drone layered | Wire on combat/phase |
| **Ambient wind / map bed** | `AmbientWind` *(enum exists, unwired)* | Shapeforms `AMBIENCE_*_LOOP`; JDSherbert Ambiences | `PlayAmbient` on map load |
| Scene fade transition | `TransitionWhoosh` [+] | Helton Yan `WHSH_*`; Shapeforms `Fly By` whoosh | ScreenFader:43/55 |
| Splash logo sting | `SplashSting` [+] | Helton Yan Tranquility `Breathy Startup`; CelerisLab `achievement_unlocked` | SplashScreenController:53 |

### H. Prologue / Beat 0 — upgrade the 3 placeholders
| Beat 0 slot | Current | Real candidate | Notes |
|---|---|---|---|
| Kill impact `HitCrit` | LanceImpact (placeholder) | Shapeforms `Gore Splat Stab Poke` / `PUNCH_SQUELCH_HEAVY`; Helton Yan `FGHTImpt_MELEE-Crunch`, `DSGNMisc_HIT-Gore Pierce` | A wet, brutal stab |
| Villager scream `VillagerScream` | MissWhoosh (placeholder) | **Super Dialogue** `shouting_*` or `death_*` — **Karen/Meghan (female)** | Real human voice — pick a take by ear |
| Rakshasa roar `RakshasaRoar` | LanceImpact (placeholder) | ⚠ **weakest** — no good organic monster roar in these packs. Closest: Shapeforms `CREAMnstr_Beast Vocalisation`, Helton Yan `MAGSpel_CAST-Critter Transformation` / a deep `DSGNImpt_EXPLOSION-Bass Hit` | **Likely the one to AI-generate** (ElevenLabs/Firefly — see `docs/research/ai-sfx-tools.md`) or grab a monster-roar pack |

---

## 4. Proposed `SoundId` additions (consolidated)

Current enum has 12. To cover the surface, append (don't reorder):

```
// UI
UiOpen, UiClose, UiError, UiTab, UiHover
// Grid / units
UnitSelect, MoveCommit, Footstep
// Combat (HitPhysical/HitCrit/Miss already exist — wire them)
AttackSwing, MagicCast, UnitDeath, Heal
// Progression / items
ExpTick, LevelUp, BuffApplied, DebuffApplied, ItemEquip, ItemMove, GoldGain
// Turn / phase
PhasePlayer, PhaseEnemy, PhaseAllied, Fanfare
// Dialogue
DialogueBlip
// Scene / music
MusicTitle, MusicVictory, MusicGameOver, TransitionWhoosh, SplashSting
```

(`UiMove`, `UiCancel`, `MusicMap`, `MusicBattle`, `AmbientWind` already exist — they just need wiring + call sites.)

---

## 5. Suggested priority (so we don't wire all 95 at once)

- **P1 — core game feel (biggest bang):** `UiConfirm`/`UiCancel`/`UiMove` across all menus · `UnitSelect` · `MoveCommit`/`Footstep` · **combat `HitPhysical`/`HitCrit`/`Miss`** · `MusicBattle`/`MusicMap` on the map · upgrade Beat 0's 3 sounds.
- **P2 — feedback & moments:** `UnitDeath` · `LevelUp` + `ExpTick` · phase banners + `Fanfare`/`MusicGameOver` · `AmbientWind`.
- **P3 — depth & polish:** item/trade/convoy/inventory sounds · `DialogueBlip` · hovers/toasts/transitions · settings/slider ticks.

---

## 6. How I'd like to proceed

You audition and pick winners; I wire. Concretely, the fastest loop:
1. You go through a P1 slot (say "UI confirm/cancel/move") in your audio editor, pick the file(s) — or just tell me *"CelerisLab standard_button, takes 1 & 3"*.
2. I copy them into `Assets/Audio/...`, build the SoundSO (with all your chosen variations + a pitch range), wire the SoundId, and add the call site.
3. Repeat down the priority list.

For anything where you want options first, point me at the slot and I'll pull the exact candidate filenames from the pack so you can A/B them.

> Reminder on verification: I can wire and confirm sounds *fire* structurally, but I can't judge how they *sound* — that ear is yours.
