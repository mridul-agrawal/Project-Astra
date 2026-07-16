# Project Astra — Design Context Pack

*Paste this whole file into Claude Design's chat as project context, then upload the reference images listed at the end. It captures the game's identity, exact palette tokens, typography, and UI language so mockups come out **in Astra's world** — not as generic AI UI. Every hex value below is copied from the actual shipped builders/mockups, not approximated.*

---

## 1. What the game is

**Project Astra** is an **Indian-mythology tactical RPG** — Fire Emblem GBA-style grid combat with a dharma-driven, moral-consequence narrative and "vyuhas" (formations).

**Aesthetic thesis** (from the art-direction docs): *"Mythic, sun-bleached, grounded. Reverent toward Indian myth without being touristic. Weight and consequence over spectacle."* And, from the design pillars: **clarity and legibility over realism** — this is chess with stories; readability wins every tie.

The one guiding image: a **relic bow (dhanush) resting on a garlanded temple altar** — the setting warm and safe, the relic luminous and "other."

## 2. Two visual registers — keep them separate

- **World / on-map / title art = HD-2D pixel art.** Warm temple lighting; references *Octopath Traveler, Sea of Stars, Chained Echoes, Blasphemous, Fire Emblem GBA*. Portraits are **hand-illustrated HD busts** (1024×1024), not pixel. **This is not the register for UI mockups** — treat it as world/identity flavor.
- **In-game UI (what you're designing here) = modern, crisp HD.** Smooth (SDF) fonts, **never pixelated** (the only pixel exceptions are the Title/Splash wordmark and one bundled pixel widget kit). 16:9, **1920×1080** reference resolution. Two palette systems live here — the **Indigo Codex** (tactical + narrative screens) and the **Constellation** palette (Main Menu / Title). Use Indigo Codex unless you're specifically designing the main menu.

## 3. Color — exact tokens

### A. "Indigo Codex" — the primary UI system (use this for most mockups)
Warm parchment ink + brass gold + vermillion, all on deep indigo. Reads like an **illuminated manuscript lit by lamplight**.

| Token | Hex | Role |
|---|---|---|
| `parchment` | `#F2E6C4` | primary text / light surface |
| `parchmentSel` | `#FFF5D8` | bright / selected parchment |
| `parchDim` | `#C9B98A` | dim / secondary text |
| `ink` | `#1A140F` | warm near-black ink |
| `inkDeep` | `#3D2A1A` | deep brown ink |
| `brass` | `#C9993A` | ornament base (the universal gold) |
| `brassLite` | `#E8C66A` | bright gold |
| `brassGlow` | `#FDE49A` | gold glow / highlight |
| `vermillion` | `#B0382A` | primary red accent |
| `vermillionLit` | `#D14A34` | bright red |
| `wine` | `#6B1E2E` | deep red |
| `bloodDeep` | `#3A0F1A` | darkest red |
| `indigoHi` | `#1A1540` | panel indigo (raised) |
| `indigo` | `#0F0B2E` | panel indigo (mid) — the signature ground |
| `indigoLo` | `#08061C` | panel indigo (recessed) |
| page base | `#05040F` | app background (near-black indigo) |
| ambient glow tints | `#14103A` `#1A1036` | soft radial background washes |
| HP green / yellow / red | `#60C870` / `#C8B040` / `#C84040` | bar states |

**How they combine:** deep indigo ground → brass illuminates frames, dividers, labels, key numerals → parchment carries readable text → **vermillion is reserved for accent, danger, and consequence** (KO, loss, warnings). Gold is structure; red is stakes.

### B. "Constellation" — Main Menu / Title only (cooler, celestial)
| Token | Hex | Role |
|---|---|---|
| title text | `#E8EDFF` | blue-white |
| eyebrow | `#8A9CD0` | muted periwinkle |
| label | `#E6ECFF` | cool label text |
| footer hint | `#B4C8F0` (~55% α) | faint helper text |

Copy voice here: title **"PROJECT ASTRA"**, eyebrow **"A TACTICAL CHRONICLE"**, footer **"CHOOSE YOUR CONSTELLATION"**. Motif set: yantra, chakra disc, sun/moon sigils, starfield, constellation footer.

### C. "Astra Temple" — world / pixel-art master palette (for cohesion, not UI chrome)
- **Stone:** `#ECD9B0` `#D2AE7C` `#A37C57` `#6F5044` `#43303E`
- **Gold / brass:** `#FFEFAE` `#F4C95B` `#CE9530` `#94621F` `#593619`
- **Flame:** `#FFF7D2` `#FFD66B` `#FF9F3E` `#EA5C2B` `#9E3320`
- **Marigold:** `#FFD24A` `#F9A52C` `#DC7416` `#97470E`
- **Leaf (accents):** `#94A84E` `#4E6A2D`
- **Cool shadow (indigo-plum):** `#6E5E7E` `#4B3A5C` `#2D2140` `#181222`
- **Divine / warm light:** `#FFF9E6` `#FCF4D8`
- **Outline:** `#281A24`

*Palette logic:* warm scene; shadows converge on cool indigo-plum; highlights warm toward candle-white; the one "divine" glow (the relic) runs a touch purer/cooler than the lamps so it reads sacred.

## 4. Typography (fonts actually in the project)

The tactical + narrative UI uses a **serif + mono** group; the Title/Splash uses a **pixel** group.

| Font | Voice | Use for |
|---|---|---|
| **Cinzel / Cinzel Decorative** | Engraved, monumental serif | Screen titles, headings, ornamental labels — the "epic" voice (caps, wide letter-spacing) |
| **Cormorant Garamond** (+ Italic) | Elegant book serif | Character names, italic flavor text |
| **EB Garamond** | Book serif | Default body / reading text |
| **JetBrains Mono** | Monospace | Stats, numerals, tabular readouts (Combat Forecast, War Ledger) |
| **Mulish** | Clean humanist sans | Dialogue UI body text |
| **Noto Serif Devanagari** | Devanagari serif | Sanskrit / Devanagari (War Ledger) |
| **Press Start 2P · Silkscreen · Pixelify Sans** | Pixel | Title / Splash wordmark only |
| **bitcell · VT323** | Pixel | Bundled Combat-Forecast pixel widget kit only |

**Default UI pairing:** Cinzel (display) + EB / Cormorant Garamond (body) + JetBrains Mono (data). Reach for pixel fonts only on the title.

## 5. UI design thesis + component language

**The design direction is "The Second Breath"** (the winning concept of 14 explored). Dual-tempo: an **instant tactical readout** the player parses in a glance, wrapped in a **slower, ornamented "second breath"** they savor. Load-bearing line: *"the readout and the ritual are the same act."* So: the numbers must be legible at speed **and** the frame must feel like a sacred object. Never trade one for the other.

**Component language:**
- **Panels:** deep-indigo grounds inside **brass cartouche frames** — a double rule (bright `#E8C66A` outer, deep `#C9993A` inner), corner **filigree**, occasional circular **seals / medallions**.
- **Naming:** items carry **Sanskrit names** with an English gloss (e.g., *Loha Khadga* — "Iron Sword"); Devanagari script appears on the War Ledger.
- **Feel:** ornate but legible — restrained warm-gold accents on indigo, generous negative space. Ornament frames content; it never crowds it.
- **Copy tone:** understated, plain, concrete. Trust the player. Never purple, forceful, or salesy.
- **Format:** 16:9, 1920×1080, HD, crisp.

## 6. Motif & iconography vocabulary (Indian-mythology, drawn from real UI assets)

Reach into this vocabulary; don't invent generic fantasy ornament.

- **Frames & selection:** brass filigree corners, temple-arch "bracket" framing, paisley bands, diamond / caret selection marks, lotus medallions.
- **Sacred objects:** lotus, chakra (disc), conch (shankha), **trishul** (trident — also the map cursor), **kirtimukha** ("face of glory" guardian), **yantra / mandala**, diya (oil lamp — used as a permadeath memorial), marigold garland.
- **Weapon-class sigils:** sword, lance, axe, bow, staff, consumable (a consistent icon set).
- **Pancha-bhuta five-element affinities:** agni (fire), jal (water), prithvi (earth), vayu (air), akasha (ether).
- **Tone map:** **gold** (`#C9993A`/`#E8C66A`/`#FDE49A`) = universal ornament; **vermillion** (`#B0382A`) = accent / danger / consequence; **deep indigo** (`#0F0B2E`/`#08061C`) = ground. Celestial/constellation motifs are reserved for the Main Menu.

## 7. Do / Don't

**Do:** deep indigo + brass gold + vermillion accents; Indian ornament (filigree, cartouche, trishul, chakra, lotus, kirtimukha, yantra, diya, marigold); elegant serif hierarchy with mono numerals; Sanskrit names; the "instant readout + slow ritual" duality; crisp HD.

**Don't:** pixelate the UI (outside the title); generic sci-fi / neon / Material / "AI-startup" looks; cold greys; emoji as icons; heavy drop-shadows; western-fantasy tropes; clutter; touristic or kitschy "exotic" pastiche — reverent and grounded, always.

---

## 8. Reference images to upload

*Priority order. The first two groups define the UI; the rest anchor world identity. The mockups are self-contained HTML — open in a browser and screenshot, or upload the files directly.*

**Core UI aesthetic (upload these first — they ARE the design system):**
- `docs/mockups/Combat Forecast Mockups.html` — the fullest labeled token set + glow/shadow recipes.
- `docs/mockups/Indigo Codex Inventory.html`
- `docs/mockups/Project Astra Trade Screen.html`
- `docs/mockups/Supply Convoy Mockup.html`
- `docs/mockups/Wars Ledger.html` — parchment-on-ink ledger + Devanagari.

**Direction explorations (optional, for range):**
- `docs/mockups/` also holds named variants: `battle_hud_01_temple_gold.html`, `unit_info_03_sacred_manuscript.html`, `action_menu_01_ornate_scroll.html`, `phase_banner_concepts.html`, `grid_cursor_concepts.html`, etc.
- `UI_Concepts/main_menu/variant_a_sanskrit_codex.html` — Main Menu / wordmark direction (Constellation palette).

**Palette:**
- `docs/title-screen/astra_temple_palette.png` — the master world swatch.
- `docs/title-screen/palettes/` — per-object palettes (stone, bow, diya, garland, thali, incense).

**World / title identity (pixel register — mood, not UI style):**
- `docs/title-screen/Final Assets/altar final.png`, `bow final.png`, `good overall placeholder.png`
- `docs/title-screen/title screen mock - gpt.png`, `altar mockup reference.png`

**Real character / world art:**
- Portraits (HD hand-illustrated busts): `Assets/Art/Portraits/` — `Indravati.png`, `Rakshas.png`, `Village Child.png`
- Map sprites (on-grid tokens): `Assets/Art/Map1/Units/` — `unit_aranya.png`, `unit_miniboss.png`, `unit_raider.png`, `unit_villager.png`
- Backdrops: `Assets/Art/BG Art/` — `Forrest.png`, `Village Attacked.png`
- UI ornament sprites: `Assets/UI/TradeScreen/Icons/`, `Assets/UI/UnitInfoPanel/Sprites/`, `Assets/UI/InventoryPopup/Icons/` (the lotus/kirtimukha/sigil/affinity sets).
- Cursor: `Assets/Art/Cursor/` (TempleBracket + trishul cursor).

> The shipped UI screens (Combat Forecast, Unit Info, War Ledger, etc.) also exist as **live Unity prefabs**. If you'd rather feed Claude Design **composed screenshots of the real running UI** than the HTML mockups, say so and I'll capture them from the editor and add them here.
