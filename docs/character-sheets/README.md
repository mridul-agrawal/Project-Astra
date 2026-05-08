# Character Sheets

Structured, visually-laid-out design sheets for every character in Project Astra. Source of truth lives in markdown; the renderer turns it into a printable 3-page card suitable for hand-off to artists / designers.

## Structure

```
docs/character-sheets/
  TEMPLATE.md              the empty sheet — copy this for each new character
  index.html               browser viewer
  characters/              one .md per character (filled-in copies of TEMPLATE.md)
    manifest.json          list of characters the viewer should show in its sidebar
  portraits/               1024x1024 PNGs, named to match the character's id
  _renderer/               viewer code (CSS + JS + CDN deps)
```

## Viewing the sheets

**For the empty TEMPLATE preview**: just double-click `index.html`. It works directly via `file://` because the template content is embedded in the page.

**For filled-in character sheets** (anything under `characters/`): browsers block `fetch()` on `file://`, so run a one-line local server from this folder:

```bash
cd docs/character-sheets
python -m http.server 8000
```

Then open <http://localhost:8000/>. The sidebar lists `TEMPLATE` plus every entry in `characters/manifest.json`.

> No Python? Any static file server works. `npx serve`, `php -S localhost:8000`, etc.

> If you edit `TEMPLATE.md` and want the file:// preview to reflect the change, update the embedded copy inside `index.html` (search for `embedded-TEMPLATE.md`). Or just run the server.

## Adding a character

1. Copy `TEMPLATE.md` to `characters/<id>.md` (e.g., `characters/arjuna.md`).
2. Fill in the YAML frontmatter (numbers + identity) and the prose sections.
3. Drop a portrait into `portraits/<id>.png`. Set `portrait_path: "portraits/<id>.png"` in frontmatter.
4. Add an entry to `characters/manifest.json`:
   ```json
   { "id": "arjuna", "title": "Arjuna", "file": "characters/arjuna.md" }
   ```
5. Reload the viewer.

## Exporting a sheet for an artist

With the character open in the viewer, use the browser's **Print → Save as PDF**. The print stylesheet emits three clean letter-size pages:

1. **Front card** — portrait, identity at a glance, FE-style stat grid, palette swatches
2. **Visual brief** — everything an artist needs (silhouette, features, garments, sigil, weapon, mood refs, do's & don'ts)
3. **Lore + dialogue + meta** — combat kit, personality, dialogue library, design intent

Send the PDF + the portrait reference image to the artist. They have everything they need.

## Schema notes

- **Stats are FE-canonical**: HP / STR / MAG / SKL / SPD / LCK / DEF / RES / CON / MOV.
- **`growths`** are percent per level (0–100).
- **`cap_modifiers`** are signed deltas on top of the class's stat caps; default 0.
- **`pancha_bhuta_affinity`** matches the `PanchaBhuta` enum in `UnitDefinition.cs` (None / Akasha / Vayu / Agni / Jala / Prithvi).
- **`personality_enum`** matches the `Personality` enum (CC-01).
- **`status`**: `draft` (work-in-progress), `review` (ready for feedback), `locked` (committed; mechanical fields can be migrated into a `UnitDefinition` ScriptableObject).

The sheet is a *human design doc*. A future tool can lift the mechanical subset (identity + bases + growths + portraits + affinity + personality + last-words + one-line-identity) into a `UnitDefinition.asset`. Until then, the sheet is the source of truth for design conversations and artist briefs.
