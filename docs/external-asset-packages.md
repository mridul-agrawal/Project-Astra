# External Asset Packages — Catalog & Usage Guide

Seven third-party Asset Store packages were imported on 2026-05-28 and live under
`Assets/External Assets/` (a local staging folder). They are **gitignored** (see
`.gitignore` → "Third-party Asset Store packages"). We do **not** commit the packages
wholesale. Instead, when we decide to use a specific asset, we copy it into one of our
own working sub-folders (e.g. `Assets/UI/...`, `Assets/Audio/...`, `Assets/VFX/...`) and
commit only that copy. This keeps the repo lean and our shipped assets clearly separated
from vendor dumps.

This doc records what each package contains and how it can serve Project Astra (a
Fire-Emblem-style tactical RPG, HD-2D pixel art, URP 2D, uGUI for gameplay UI). Consult
it before building a feature that needs icons, UI, audio, or VFX — there may already be a
usable asset here.

> Project context that shapes "is this usable?": shipped UI is **uGUI only**; the visual
> style is **HD-2D pixel / Indian mythology**; the render pipeline is **URP 2D**.

---

## 1. CelerisLab — The Complete UI Sound Effects Library
**Folder:** `Assets/External Assets/CelerisLab/CompleteUISFX/`
**Type:** Audio (WAV, 48 kHz / 24-bit, game-ready)
**Aesthetic fit:** Neutral — pure UI sounds, no visual style to clash.

~370 UI sound effects grouped into 8 categories:

| Category | Count | Examples / use |
| --- | --- | --- |
| basic_interactions_and_navigation | 118 | button clicks, click-rejection (invalid action) — cursor move, confirm, cancel |
| crafting_and_upgrading | 33 | forge / upgrade / smith feedback |
| inventory_and_item_management | 63 | pick up, equip, drop, sort — inventory/trade/convoy screens |
| positive_feedback_and_success | 50 | success chimes, level-up fanfare, unlock |
| miscellaneous_and_unique_ui | 35 | unique flourishes |
| social_and_communication | 31 | message / notification pings |
| negative_feedback_and_warnings | 22 | error, warning, denied |
| shop_and_economy | 19 | purchase, coins, transaction |

**How it helps Astra:** the single most broadly useful package here. Every UI surface we
already have or plan (title menu, dialogue advance, inventory popup, trade screen, supply
convoy, shop, combat forecast, EXP/level-up screen, cursor movement on the grid) needs
audio feedback. Map logical input actions (CURSOR_MOVE → soft click, CONFIRM → click,
CANCEL → back, invalid → rejection, level-up → success fanfare). Harvest the chosen WAVs
into `Assets/Audio/UI/`.

---

## 2. Heat — Complete Modern UI (Michsky)
**Folder:** `Assets/External Assets/Heat - Complete Modern UI/`
**Type:** Full uGUI UI framework (scripts + prefabs + animations + fonts + localization).
**Aesthetic fit:** Visuals clash (sleek modern console-game look, not pixel mythology) —
but the **code/patterns are uGUI and directly reusable**. No asmdef, so individual scripts
can be lifted (watch for internal cross-references between Heat scripts).

Script families under `Scripts/UI Elements/`:
- **ButtonManager / BoxButtonManager / PanelButton / ShopButtonManager** — rich button
  state, hover/click animation, sound hookup, icon+text layouts.
- **ModalWindowManager** — pop-up dialog with show/hide animation (confirm dialogs, "are
  you sure?" prompts).
- **NotificationManager** — queued toast/notification system.
- **ProgressBar**, **SliderManager / SliderInput**, **SwitchManager** (toggle),
  **Dropdown**, **HorizontalSelector / ModeSelector** (cycle-through option picker — great
  for settings and difficulty selection), **SmoothScrollbar**.
- **SettingsElement / SettingsSubElement** — settings-menu row scaffolding.
- **UIElementSound** — central place to attach UI SFX (pairs well with CelerisLab).
- **CanvasManager**, **UI Manager/** (theme + global color management).
- **Input/** — controller/gamepad navigation support (relevant: our input is New Input
  System with gamepad hot-swap).

Prefabs: HUD (Health Bar, Minimap, Quest Item), Panels (Chapter, Credits, Load Slot,
Profile, Panel Manager), plus a full set of UI-element prefabs.

**How it helps Astra:** reference implementation for things we'll build — modal confirm
dialogs, notification toasts, settings menu, options selectors, controller-navigable menus.
Likely reskin the visuals to our pixel aesthetic or harvest just the controller-nav /
modal / notification logic rather than shipping Heat's look.

---

## 3. Honeti — PixelArtGUI
**Folder:** `Assets/External Assets/Honeti/PixelArtGUI/`
**Type:** Pixel-art styled uGUI kit (prefabs + textures + fonts + demo scenes).
**Aesthetic fit:** **On-aesthetic** — pixel-art GUI matches our HD-2D look. Strongest
visual match of the UI packages.

Prefab groups:
- **Bars/** — many HP/progress bar styles (Big, Outlined, Transparent-bg) in blue/green/
  orange/purple/red/yellow. Candidates for unit HP bars, EXP bars, cast/charge meters.
- **Buttons/** — ActionBarSlot, Checkbox, Dropdown, Gold/currency buttons, LevelButton,
  Quest, RadioButton, ShopItem, green/gold variants (+ outlined/small sizes).
- **Panels/** — Achievement, ChatMessage, CurrencyHolder, Menu/MenuItem, Message,
  RankingScore, SavedGame, WindowTitle, TopTitle, InputField (TMP), tag holders.
- **Texts/** — TMP text presets (Display, Body big/small, with shadow).

**How it helps Astra:** the go-to source for in-game UI that must read as pixel art —
window frames/9-slice panels, HP/EXP bars, currency displays, save-slot rows, menu items,
TMP text styling. Use as direct building blocks or as style reference for our own pixel UI.

---

## 4. Hovl Studio — Fullscreen Effects
**Folder:** `Assets/External Assets/Hovl Studio/` (`Fullscreen effects/` + shared `HSFiles/`)
**Type:** Fullscreen overlay VFX (prefabs + URP Shader Graph shaders + `HS_ScreenEffect.cs`).
**Aesthetic fit:** Effect-dependent; these are screen-space overlays, not pixel art —
use sparingly / tune so they read with the art style. **URP-based** (Shader Graph) — verify
they render correctly under our **URP 2D Renderer** (fullscreen shaders sometimes assume
the 3D/universal renderer; may need a blit/render-feature check).

24 screen-overlay prefabs: blood, buff, debuff, healing, fire, ice, acid, dirt, sand,
smoke, water (+ twirl), wind (+ straight), hex, spider web, liana, love, magic flow,
microcircuit, space, gradient, pink.

**How it helps Astra:** combat/status feedback as fullscreen tints — e.g. red vignette
on the lord taking heavy damage, healing glow when a Fort/healing tile or staff heals,
buff/debuff screen flash when a status is applied, fire/ice flourish during an astra
(divine-weapon) cast in the combat-animation state. Drive via `HS_ScreenEffect`.

---

## 5. MPUIKit (Scrollbie)
**Folder:** `Assets/External Assets/MPUIKit/`
**Type:** Procedural SDF-shape UI components (runtime scripts + shaders). Has its own
**`MPUIKit.asmdef`** (`…/MPUIKit/Runtime/MPUIKit.asmdef`) — keep the asmdef if harvesting,
or reference it.
**Aesthetic fit:** Clean vector/geometric (resolution-independent) — best for HD UI
layers; not pixel art. Use where crisp geometry beats a sprite.

`MPImage` / `MPImageBasic` replace Unity's `Image` and render procedural shapes via SDF
shaders — crisp at any resolution, with gradients, outlines, rounded/independent corners.
Shapes: Rectangle (rounded/chamfer), Circle, Hexagon, Pentagon, Triangle, Parallelogram,
N-Star Polygon, ChamferBox. Plus `GradientEffect`.

**How it helps Astra:** sprite-free, always-sharp UI primitives — rounded panel
backgrounds, circular icon/skill buttons, **hexagonal portrait frames**, star ratings,
gradient fills for forecast/HUD panels. No texture import or atlas glyph worries; scales
with any canvas resolution. Good for the HD UI layer that sits above pixel content.

---

## 6. MagicArsenal (Archanor VFX)
**Folder:** `Assets/External Assets/MagicArsenal/`
**Type:** 3D particle/VFX spell library (prefabs + materials + models + scripts + sound).
**Aesthetic fit:** 3D particles in a 2D project — usable in combat-animation sequences on
a world/overlay camera, but won't look pixel-native; tune or use as motion reference.
**Requires URP upgrade:** import `MagicArsenal/Upgrades/` packages **in order**
(1. URP Upgrade, 2. URP 2022.3.21 Fix); enable Depth Texture or disable Soft Particles.

Effect prefabs (`Effects/Prefabs/`): AreaDamage, Aura, Beam / Beam Blast, Charge, Cleave,
Curse, DoT, Enchant, Flames, Missiles & Explosions, Muzzleflash, Nova, Orbital, Pillar
Blast, Rain, Shields, Slash / Slash Hit, SphereBlast, Sprays, Walls. Helper scripts:
`MagicBeamStatic`, `MagicLightFade`, `MagicLightFlicker`, `MagicRotation`.

**How it helps Astra:** source for **combat-animation VFX** — slash/impact on a melee
hit, projectile + explosion for ranged/magic, charge + nova/pillar for an astra cast,
aura/shield for buffs/guard, curse/DoT for debuff status. Pair with Hovl fullscreen tints.
Treat as a VFX harvest library; expect to scale/retune particles, lights, trails per use.

---

## 7. RPG Icons Pixel Art
**Folder:** `Assets/External Assets/RPG Icons Pixel Art/`
**Type:** Pixel-art icon library — **~8,266 PNGs across 92 categories**.
**Aesthetic fit:** **On-aesthetic** (pixel art). Style is generic Western fantasy, not
Indian mythology — many items (potions, gems, scrolls, buffs) are culturally neutral and
shippable; class/weapon flavor may need our own art for final, but all work as placeholders.

Categories (grouped):
- **Weapons:** Swords, Axes, Bows, Crossbowman, Spears, Daggers, Maces, Staffs, Arrow,
  Exotic weapons pack.
- **Armor / equip:** Helmets, Cuirass, Shields, Belts, Sabatons, Trousers, Rings_jewellery.
- **Items / loot:** Potions, Potions 2, Scrolls, Books, Runes, Sigils, Gems1/2, Artefacts,
  Chests_keys_treasure, Drop1/2, Paints, Brasers.
- **Consumables / resources:** Food_icons, Fruits_vegetables, Berries_nuts, Mushrooms,
  Meat_skins, Minerals, Craft_materials1/2, Farming, Fishing, Mining, Blacksmith.
- **Status icons:** Buffs, Anti-buffs, Curse (buff/debuff/status-effect indicators).
- **Class / skill icon sets:** Paladin, Priest, Necromancer, Warlock, Druid, Summoner,
  Pyromanser, Cryomancer, Aeromancer, Lightning mage, Blood mage, Barbarian, Swordsman,
  Spearsman, Archer, Thief, Engineering — and faction packs (Demon, Undead, Goblin, Dwarf,
  Fairy, Elf, Dark Elf, Pirate, Chaos monsters).
- **Avatars / portraits (32×32):** Civilian, Demon, Undead, Pirate, Dwarf, Goblin, Fairy,
  Halfling.

**How it helps Astra:** the icon source for inventory/weapon/item icons, ability/skill
icons, and **buff/debuff/status-effect icons** (the Buffs / Anti-buffs / Curse sets map
cleanly onto our status system). Also shop items and consumables. Harvest individual PNGs
into `Assets/UI/Icons/` (or feature-specific folders) as we wire up inventory, items, and
status effects.

---

## Quick "what do I need?" index

| I'm building… | Look in |
| --- | --- |
| UI sound feedback (click, error, success, level-up) | **CelerisLab** |
| Modal dialog / notification toast / settings menu / controller-nav menu | **Heat** (harvest scripts) |
| Pixel-art panels, HP/EXP bars, menu rows, currency display | **Honeti** |
| Fullscreen combat tint (hurt, heal, buff, elemental cast) | **Hovl Studio** |
| Crisp resolution-independent shapes (rounded panels, hex frames, circular buttons) | **MPUIKit** |
| Combat-animation spell/impact VFX (slash, projectile, explosion, aura) | **MagicArsenal** |
| Item / weapon / skill / buff-debuff icons | **RPG Icons Pixel Art** |
