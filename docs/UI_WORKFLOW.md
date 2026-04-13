# Project Astra — UI Pipeline (Figma → Unity)

This document is the canonical workflow for designing and implementing UI in Project Astra.
**Every new UI screen or component must follow it.** It exists because early iterations wasted
hours solving the same Figma-to-Unity problems; reading this from turn 1 avoids re-learning them.

Scope: all in-game UI (HUD, menus, unit info, battle forecast, dialogue boxes, etc.). Does not
apply to editor tooling or gizmos.

---

## TL;DR

1. Designer (Claude via `frontend-design`) prototypes in HTML/CSS for fast aesthetic exploration.
2. Chosen direction is authored in Figma, **following the layer structure rules in §2**.
3. Unity implementation is built via an editor builder script (`Assets/Editor/*Builder.cs`)
   that creates a layered uGUI hierarchy, not UI Toolkit.
4. Only actual **decorative chrome** is exported from Figma as PNG (backgrounds, icons,
   ornaments). Text, layouts, and dynamic content are **live** `TextMeshProUGUI` / `Image`
   GameObjects, never baked into background PNGs.
5. Each frame that has visual properties (gradient, stroke, shadow) must contain a dedicated
   `Background` **Rectangle** child that owns those properties. The parent frame stays a pure
   layout container. This is the single most important rule.

---

## 0. Before you touch anything — ask these questions

**Every new UI screen must start with this checklist.** Do not assume Figma's existing frame
dimensions are correct — a prior session may have authored the file at the wrong size.

1. **Screen kind — full-screen or modal popup?**
    - *Full-screen*: fills the entire 1920×1080 canvas. Examples: Title Screen, Main Menu, Chapter Clear, Unit Info Panel (in this project), Game Over.
    - *Modal popup*: smaller than the canvas, centered, battle map visible behind it (usually dimmed via a backdrop). Examples: Battle Forecast, Confirm Dialog, Item Selection menu during a unit's turn.
2. **Target dimensions.** The Unity Canvas reference resolution is **1920×1080** for this project (see `CanvasScaler` in any existing builder). Full-screen UIs must match that exactly. Modal popups are smaller — confirm the specific size with the user (1197×673, 960×540, etc).
3. **If modal, does it need a dim backdrop?** A semi-transparent black `Image` as a sibling of the panel under the canvas. Ask the user unless the answer is obvious.
4. **Does an existing Figma file match those answers?** If not, restructure the Figma file **first** (via `mcp__figma__use_figma` — `rescale()`, `resizeWithoutConstraints()`) **before** exporting assets or writing any Unity code. The Figma source of truth must match the Unity target.
5. **Validate the answer in code.** Every builder script must declare `CanvasWidth`, `CanvasHeight`, `IsFullScreen` as constants at the top, and assert at build time that the outer panel dimensions match. Example from `UnitInfoPanelBuilder.cs`:

    ```csharp
    const float CanvasWidth  = 1920f;
    const float CanvasHeight = 1080f;
    const bool  IsFullScreen = true;
    const float Scale        = 1.6037086f;  // ← if rescaling from original Figma design values

    if (IsFullScreen && (Mathf.Abs(Sc(1197) - CanvasWidth) > 1f || Mathf.Abs(Sc(673) - CanvasHeight) > 1f))
        Debug.LogError("Panel dimensions don't match canvas reference. Fix Scale or IsFullScreen.");
    ```

    The check runs every time the builder executes, so if anyone ever changes Scale or the constants without meaning to, the builder fails loudly instead of silently shipping a half-size panel.

6. **Interactive states.** For every interactive element in the screen (buttons, selectable rows, toggles, sliders, draggable items, tabs, anything the player can hover / click / focus / activate), the concept must define each state explicitly. Minimum states: **default, hover, pressed, disabled, selected/focused** (for keyboard/gamepad). Skipping this is the single biggest cause of "it looks great in the mockup and dead in-game" — if the mockup can't show you a state, you can't build it. See §4.6 for the full rule and how states map from HTML → Figma → Unity.
7. **Visual effects fidelity — glows, shadows, blurs.** Mockups use CSS `text-shadow`, `box-shadow`, `filter: drop-shadow`, `filter: blur`. None of these translate automatically. Decide up front how each effect will be reproduced: baked into the sprite's PNG alpha, rendered by TMP's Glow/Underlay material features for text, or handled by URP post-processing (Bloom) for emissive sprites. See §4.5.
8. **Reference image handling.** If the user provides a reference image, treat it as an **inspiration anchor, not a blueprint**. Specific rules, all non-negotiable:
    - **Preserve from the reference:** layout, components present, textual content, color scheme.
    - **Do NOT preserve:** pixel-for-pixel fidelity, the reference's native resolution, pixelation artefacts, the reference's aspect ratio. Project Astra is not a pixel-art game — all shipped UI must be **HD vector/smooth raster at the project's canvas resolution** (1920×1080 by default), regardless of the reference's resolution or aspect.
    - **Aspect ratio:** always 16:9 (matching the 1920×1080 canvas), even when the reference is 4:3 or square. Re-flow the composition to fill 16:9 rather than letterboxing.
    - **Copyright:** the reference may be from a shipped game. Never reproduce it pixel-for-pixel or with trivial alterations. The assets we ship must be **original** — our own typography, our own ornamental shapes, our own textures. The reference tells you the vibe, not the bitmap.
    - **Having a reference does NOT skip the concept phase.** A reference is one data point. The concept phase in §6 step 2 exists specifically to explore 2–3 interpretations around a reference (color palette variants, typography pairings, ornament treatments, component proportions) so the user can pick the one that matches Project Astra's identity rather than the reference game's identity. See §6 step 2 for the requirement.

**Why this section exists:** Earlier sessions made two avoidable mistakes. (1) We built the Unit Info Panel using Figma's authored size of 1197×673 without asking if that was the intended target. Unity's canvas was 1920×1080, so the panel ended up centered with a large empty margin. Full rescale took ~20 minutes when the right answer up front would have taken 30 seconds. (2) For the Main Menu we started translating a reference image directly into Figma without first running the concept phase, because the doc's old "(optional)" wording on concept phase let us reason our way out of it. The user had to call it out and push back. Both failures had the same root cause: skipping §0 questions or reading them loosely. Never skip this checklist.

---

## 1. Why uGUI, not UI Toolkit

Project Astra uses **uGUI (Canvas + RectTransform + Image + TextMeshProUGUI)** for game UI.

Rationale:
- Game UI needs to change dynamically via C# (HP bars, inventory, stat tooltips, animations).
  uGUI GameObjects map directly to scripted behaviour; every element is inspectable and
  addressable by name/reference.
- UI Toolkit's UXML/USS is CSS-like but its runtime model treats elements as visual tree nodes,
  not first-class GameObjects. Per-element scripting is clunkier, animations require different
  tooling, and editor-time visualization in the Scene view is worse.
- Every designer hand-off in this project assumes "hierarchy of GameObjects with layout groups".

Use UI Toolkit only for: custom Editor windows, inspector extensions, or tooling — never in-game UI.

---

## 2. Figma layer structure rules

The biggest footgun: Figma's REST image export renders a frame **with all its children baked in**.
There is no "export background only" flag. Therefore frames must be structured so the background
is its own child node from the start.

### Rule 1 — Every visual frame has a `Background` rectangle child

If a frame has any visual property (fill, stroke, effect, corner radius), move those properties
**off the frame** and **onto a `Rectangle` child named `Background`**. The rectangle:
- Sits at index 0 (behind siblings)
- Has `constraints: STRETCH/STRETCH` so it follows the parent's size
- Owns the fill, stroke, strokeWeight, strokeAlign, effects, cornerRadius
- Has export settings set to `PNG @ 2x`

The parent frame then has empty `fills`, empty `strokes`, empty `effects`. It becomes a **pure
layout container**. When reading the file programmatically, it's obvious which nodes need
exporting (the `Background` rectangles) and which are just layout.

### Rule 2 — Layout-only frames need no export

Frames whose only job is to group children (rows, columns, sections, padding wrappers) should
have no fills, strokes, or effects. They map to transparent `RectTransform` GameObjects with
layout groups in Unity. **Do not export them.** If you catch yourself exporting a frame that's
just a grouping container, stop and re-think the hierarchy.

### Rule 3 — Text stays live, never baked

Any text that might change at runtime (unit names, stat values, counts, dialogue) must be a
separate `TextNode` child, **not** part of a `Background` rectangle. In Unity it becomes a
`TextMeshProUGUI` that scripts can write to. Never export a frame whose children include text
you want to drive dynamically — the text will render into the PNG and "update" at runtime will
show the old baked text underneath the new live text.

### Rule 4 — Icons and small decorative graphics export individually

Item icons, class icons, corner ornaments, dividers, etc. are authored as their own leaf image
layers in Figma (usually vector shapes flattened to raster). They export cleanly via `get_design_context`
because Figma treats them as raster assets. No special structure needed.

### Rule 5 — Gradients and multi-ring borders

Figma often fakes layered borders with stacked `DROP_SHADOW` effects using only `spread`
(e.g. brown spread 13 → gold spread 9 → brown spread 5 for a triple ring). This is a hack that
does not round-trip to Unity cleanly. When authoring new designs:
- Prefer **real strokes** (`strokeWeight` + `strokeAlign = INSIDE`) for single-ring borders.
- For multi-ring borders, create **nested `Rectangle` nodes** (one per ring), each with its own
  fill. Unity can then mirror them as nested `Image` GameObjects.
- Legacy frames using the drop-shadow trick still work, but when you port them to Unity, recreate
  the rings as nested Images instead of trying to bake them into a PNG.

---

## 3. The Figma Plugin API — what Claude can and can't do

**Read:** `mcp__figma__get_metadata`, `mcp__figma__get_design_context`, `mcp__figma__get_screenshot`,
`mcp__figma__get_variable_defs`. These go through the Figma REST API.

**Write:** `mcp__figma__use_figma` runs arbitrary JavaScript inside Figma's Plugin API. Claude has
**full write access** via this tool — creating nodes, editing fills, toggling visibility, setting
export settings, etc. Use this to enforce Rule 1 automatically when a designer delivers a file that
doesn't have `Background` rectangles.

**Image export:** use the REST API directly, not an MCP tool:
```bash
curl -H "X-Figma-Token: $FIGMA_TOKEN" \
  "https://api.figma.com/v1/images/{fileKey}?ids={id1,id2,...}&format=png&scale=2"
```
Returns a JSON map of `nodeId → S3 URL`. Download the URLs with `curl`.

**Token storage:** `.secrets/figma.env` (gitignored). Load with `source .secrets/figma.env` or
read with `grep FIGMA_TOKEN .secrets/figma.env | cut -d= -f2`.

---

## 4. Unity implementation pattern

Every UI screen ships as:
1. `Assets/UI/{ScreenName}/Sprites/` — Figma-exported PNGs (backgrounds, icons, ornaments)
2. `Assets/UI/{ScreenName}/Fonts/` — TMP font assets + source TTFs
3. `Assets/Editor/{ScreenName}Builder.cs` — editor script that constructs the hierarchy under a menu item like `Project Astra/Build {ScreenName}`
4. `Assets/Scripts/UI/{ScreenName}Controller.cs` — (later) runtime MonoBehaviour that exposes typed `SetData(...)` methods to drive the live elements

### 4.1 The builder script

An idempotent editor script that:
- Is called from a menu item (`[MenuItem("Project Astra/Build {ScreenName} (temp)")]`)
- Removes any existing instance of the screen before rebuilding
- Creates `Canvas` (Screen Space - Overlay, reference resolution 1920×1080, Scale With Screen Size, match 0.5)
- Builds the hierarchy one helper function at a time (`BuildPanel`, `BuildLeftSection`, `BuildItemRow`, etc.)
- Marks the scene dirty so changes persist

Why a builder, not a prefab: we iterate on layouts a lot during early dev, and a builder script
keeps the source of truth in code — diffable, version-controllable, re-runnable. Convert to a
prefab only when the layout is stable and you need instance-level overrides.

### 4.2 Hierarchy translation rules

| Figma                                 | Unity                                          |
|---------------------------------------|------------------------------------------------|
| Frame with `Background` rect child    | GameObject with `Image` sprite = the exported PNG |
| Pure layout frame (no visuals)        | GameObject with `RectTransform` only           |
| AutoLayout (HORIZONTAL)               | `HorizontalLayoutGroup`                        |
| AutoLayout (VERTICAL)                 | `VerticalLayoutGroup`                          |
| Frame with AutoLayout + Wrap          | Not supported in uGUI — use `GridLayoutGroup` or manual |
| Text node                             | `TextMeshProUGUI`                              |
| Rectangle (non-background decoration) | `Image` with the exported sprite               |
| Icon (leaf image)                     | `Image` with the exported sprite, `preserveAspect = true` |
| Constraints: Stretch                  | `anchorMin = 0,0`, `anchorMax = 1,1`           |
| Constraints: Left+Top                 | `anchorMin = anchorMax = 0,1`, pivot 0,1       |
| Constraints: Scale                    | `anchorMin = anchorMax` at center of parent    |
| Gradient fill                        | Exported as raster sprite (no runtime gradient) |
| Drop shadow / inner shadow            | Usually baked into sprite; or TMP outline/shadow for text |

### 4.3 TextMeshPro fonts

- Download font TTFs to `Assets/UI/{Screen}/Fonts/`. Prefer Google Fonts variable TTFs if the original is variable — Unity 6 imports them.
- Generate TMP font assets programmatically via `TMP_FontAsset.CreateFontAsset(ttf, 90, 9, SDFAA, 1024, 1024, Dynamic)`.
- **Critical:** after creating, pre-populate the atlas with `fa.TryAddCharacters(" ...ASCII...")` AND save the atlas textures as sub-assets via `AssetDatabase.AddObjectToAsset(tex, fa)` + `AssetDatabase.AddObjectToAsset(fa.material, fa)`. Without this, glyphs render blank on first play-mode entry.
- Common ASCII range: `" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_\`abcdefghijklmnopqrstuvwxyz{|}~"`

### 4.4 Sprite import settings

All exported PNGs:
```
TextureType: Sprite (2D and UI)
SpriteMode: Single
AlphaIsTransparency: true
MipmapEnabled: false
FilterMode: Bilinear
SpritePixelsPerUnit: 200      (for 2x exports — keeps visual size equivalent to 100 ppu at 1x)
MaxTextureSize: 4096
TextureCompression: Uncompressed (for UI chrome; compress later if memory matters)
```

For 9-sliced frame sprites (rare in practice — most UI uses fixed-size frames that don't stretch),
set sprite borders in the Sprite Editor to match the stroke/border width.

### 4.5 Visual effects fidelity — glows, shadows, blurs

The HTML mockup is the authoritative source of truth for every visual effect. Its CSS values (`text-shadow`, `box-shadow`, `filter: drop-shadow`, `filter: blur`) must reach Unity **exactly**, not "in spirit". None of these translate automatically. Decide up front which of the three routes each effect takes:

**Route A — Bake into the sprite's PNG alpha (for sprite effects: plaques, ornaments, sigils, rule bars).**

Author the effect in Figma as a `DROP_SHADOW` or `INNER_SHADOW` on the `Background` rectangle, then make sure the **exported frame is padded** by at least `2 × blurRadius` on every side. If the frame is the same size as the visible element, the glow will clip at the edge of the PNG. This is the #1 reason "the glow is in Figma but not in Unity" — the bounding box was too tight. When exporting via `curl` to the REST API, the `ids=` you pass are the `Background` rect IDs; make sure the frame that owns them was resized to include padding first.

Once padded and exported, the glow is inherent to the sprite and will render correctly in any uGUI Image. No runtime work.

**Route B — TMP material features (for text effects: `text-shadow`).**

TextMeshPro has two shader features that together cover every `text-shadow` CSS rule:

| CSS                                                | TMP feature    | Material properties                                                                       |
|----------------------------------------------------|----------------|-------------------------------------------------------------------------------------------|
| `text-shadow: 0 0 24px rgba(120,160,255,.6)`       | **Glow**       | `_GlowColor`, `_GlowInner`, `_GlowOuter`, `_GlowPower` (outer = blur radius ÷ half-em)    |
| `text-shadow: 0 4px 0 rgba(20,30,70,.8)` (no blur) | **Underlay**   | `_UnderlayColor`, `_UnderlayOffsetX/Y` (offset ÷ half-em), `_UnderlaySoftness = 0`        |
| `text-shadow: 2px 4px 8px rgba(0,0,0,.5)` (blurred)| **Underlay**   | offsets as above, `_UnderlaySoftness` ≈ blurRadius ÷ half-em                              |
| Inner glow (rare in CSS, Figma `INNER_SHADOW`)     | **Glow**       | set `_GlowInner > 0`, `_GlowOuter = 0`                                                    |

Stacking rules:
- You can have **one Underlay + one Glow** active on the same material simultaneously (keywords `UNDERLAY_ON` and `GLOW_ON`). Use Underlay for the hard drop shadow, Glow for the soft halo.
- CSS commonly stacks **two soft glows** (e.g. `0 0 24px` + `0 0 48px`). TMP can only represent one in a single pass — collapse them into a single Glow whose `_GlowOuter` matches the larger radius. Pixel-match is close enough; nobody can tell.

**Critical gotchas:**

1. **Wrong shader variant kills Glow silently.** TMP font assets default to `TextMeshPro/Mobile/Distance Field`, which **has only Underlay, not Glow**. Setting `_GlowColor`/`_GlowOuter` on a Mobile-shader material is a silent no-op — the properties don't exist and the setter returns without error. Always switch to `TextMeshPro/Distance Field` (the full shader) on any material that needs glow:

    ```csharp
    mat.shader = Shader.Find("TextMeshPro/Distance Field");
    // Then copy SDF properties from the font's base material (see gotcha 2).
    mat.EnableKeyword("GLOW_ON");
    mat.SetColor("_GlowColor", ...);
    ```

2. **Copy SDF properties when swapping shaders.** When you `new Material(font.material)` off a Mobile-shader base and switch `.shader = full Distance Field`, the SDF-specific floats and the atlas texture must be copied over by hand or glyphs render blank:

    ```csharp
    mat.SetTexture("_MainTex",      baseMat.GetTexture("_MainTex"));
    mat.SetFloat("_GradientScale",  baseMat.GetFloat("_GradientScale"));
    mat.SetFloat("_TextureWidth",   baseMat.GetFloat("_TextureWidth"));
    mat.SetFloat("_TextureHeight",  baseMat.GetFloat("_TextureHeight"));
    mat.SetFloat("_WeightNormal",   baseMat.GetFloat("_WeightNormal"));
    mat.SetFloat("_WeightBold",     baseMat.GetFloat("_WeightBold"));
    mat.SetFloat("_ScaleRatioA",    baseMat.GetFloat("_ScaleRatioA"));
    mat.SetFloat("_ScaleRatioB",    baseMat.GetFloat("_ScaleRatioB"));
    mat.SetFloat("_ScaleRatioC",    baseMat.GetFloat("_ScaleRatioC"));
    ```

3. **Atlas padding limits glow radius.** The font atlas is generated with a fixed SDF padding (default 9 samples in our `CreateFontAsset` call). Glow falloff computed by the shader cannot extend beyond that padding in SDF-space. On a 90pt atlas with padding 9, the maximum usable `_GlowOuter` is ~1.0 and it will start to clip before that. If a design calls for extreme glow, regenerate the font asset with larger padding (e.g. 16 or 20) — but this grows the atlas size. For most menus, padding 9 is fine.

4. **One material instance per style, saved as an asset.** Do not create materials at runtime. Create them once as `.mat` assets under `Assets/UI/{Screen}/Materials/`, one per visual style (e.g. `CinzelTitleGlow.mat`, `CinzelButtonGlow.mat`, `CinzelButtonGlowHover.mat`). The builder script loads them with `AssetDatabase.LoadAssetAtPath<Material>(...)` and assigns via `tmpText.fontMaterial = mat`. This keeps the material diffable in the Inspector, version-controllable, and inspectable at edit time.

5. **Material assets are generated by code, not hand-authored.** The Inspector is fine for tweaking, but the canonical way to create a glow material in this project is a throwaway `ExecuteCode` snippet (or a small `[MenuItem]` helper) that reads the CSS values from the HTML mockup and writes them into the material asset. The mockup's CSS remains the source of truth; the material asset is a compiled artefact.

**Route C — URP post-processing Bloom (for emissive sprite bleed across the whole scene).**

Bloom is a screen-space effect that affects every bright pixel in the scene, not a per-element knob. Reach for it when the design calls for a "neon city" / "everything glows" vibe that feels like camera bloom, not per-element glow. It requires the Canvas to render in **Screen Space – Camera** mode through a URP camera, because Overlay canvases bypass post-processing. Adding a Global Volume with a Bloom override is three lines of Unity setup, but the canvas-mode switch ripples through every existing builder. Treat Bloom as a scene-wide opt-in, not a default.

Default choice: Route A for sprite ornaments, Route B for text, Route C only when the art direction explicitly calls for it.

### 4.6 Interactive state specs — hover, pressed, disabled, focused

**The rule: every interactive element must have its states defined at the concept phase, carried through Figma, and ported to Unity. No exceptions.**

"Interactive element" is anything the player can point at, hover over, click, focus with keyboard/gamepad, or drag. Buttons are the obvious case, but the rule applies equally to:

- Selectable list rows (inventory items, unit roster entries, save slots)
- Toggles, checkboxes, radio groups
- Sliders, steppers, dropdowns
- Tabs and segment controls
- Draggable items (tile in a map editor, token in a formation screen)
- Grid cells that the cursor can rest on
- Links / text buttons

For each one, the concept must define the following states — treat each as a distinct visual:

| State          | When it applies                                                                              | Typical visual deltas                                                      |
|----------------|----------------------------------------------------------------------------------------------|----------------------------------------------------------------------------|
| **Default**    | Resting state, not hovered, not focused, not disabled.                                       | —                                                                          |
| **Hover**      | Mouse pointer is over the element.                                                           | Brightness +, glow +, optional 2–4px position nudge, optional scale 1.02.  |
| **Pressed**    | Mouse button held down on the element (between down and up).                                 | Brightness −, scale 0.98, inset shadow, "pushed in" feel.                  |
| **Focused**    | Keyboard / gamepad selection ring. For tactical RPGs this is the *primary* state on console. | Outline ring, glow, indicator sprite.                                      |
| **Selected**   | Persistent "I am currently chosen" state, distinct from momentary focus (e.g. active tab).   | Background tint, underline, chakra marker.                                 |
| **Disabled**   | Interaction blocked (e.g. "Continue" with no save, greyed-out skill).                        | Desaturated, 40–60% alpha, no glow, cursor changes.                        |

Keyboard/gamepad controllers conflate **hover** and **focused** — whatever the cursor is on is both. Mouse players get hover on pointer-over and focused is invisible. Design for focused first, treat hover as a cosmetic mouse-only bonus.

**How states travel through the pipeline:**

1. **HTML mockup (concept phase):** Every interactive element has CSS rules for `:hover`, `:active` (pressed), `:focus-visible`, `[disabled]`, and (where applicable) `.selected` or `.is-active`. Add a screenshot gallery to the mockup HTML that shows all states side-by-side, or a JS toggle. The user's sign-off on the concept = sign-off on every state.

2. **Figma:** Author the element as a **Component** with **Variants**, one variant per state (`State=Default/Hover/Pressed/Focused/Selected/Disabled`). Each variant is a separate design with its own `Background` rect and effects per §2. Export each variant as its own sprite (`button_plaque_default.png`, `button_plaque_hover.png`, `button_plaque_pressed.png`, `button_plaque_disabled.png`). This is the step that most often gets skipped — do not skip it.

3. **Unity builder:** The builder loads all state sprites and wires them in. Two patterns depending on the control type:

    **For standard `Button` components:** Unity's built-in `Button.spriteState` field has slots for Highlighted / Pressed / Selected / Disabled sprites. Assign the state PNGs and Unity swaps them for free on pointer events and navigation input — no custom code:

    ```csharp
    var btn = go.GetComponent<UnityEngine.UI.Button>();
    btn.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
    btn.targetGraphic = plaqueImage;
    var ss = new UnityEngine.UI.SpriteState {
        highlightedSprite = LoadSprite("button_plaque_hover.png"),
        pressedSprite     = LoadSprite("button_plaque_pressed.png"),
        selectedSprite    = LoadSprite("button_plaque_focused.png"),
        disabledSprite    = LoadSprite("button_plaque_disabled.png"),
    };
    btn.spriteState = ss;
    ```

    For text color changes across states, use the `ColorTint` transition on a separate Selectable, or (cleaner) a small MonoBehaviour that listens to `IPointerEnter/ExitHandler` and `ISelectHandler/IDeselectHandler` and swaps the TMP `fontMaterial` between the default and hover glow materials (§4.5 Route B).

    **For custom controls that aren't `Button`** (list rows, toggles, draggables): write a reusable `UIInteractiveState` MonoBehaviour that implements the pointer/selection event interfaces and swaps a set of registered graphics/materials per state. One class, one file, reused across every interactive element in the project.

4. **Motion on state change.** "Hover lifts by 3px" and "pressed settles by 2px" are motion, not sprite swaps. Implement motion separately: a small coroutine or DOTween tween on the wrapper's `anchoredPosition`, triggered by the same state-change events. The sprite swap handles the *look*, the tween handles the *feel*. Both come from the mockup spec.

**Why this section exists:** Early Main Menu and Unit Info Panel iterations hard-coded the default state and left hover/pressed/disabled as "we'll figure it out in scripting". When scripting time came there was no spec — no CSS, no Figma variant, no reference — so the implementer had to invent the states on the fly, which meant they drifted from the designer's intent and from each other. Defining states upstream is the only way to keep every interactive element in the project consistent.

---

## 5. End-to-end example: what this session produced

Files created by this workflow for the Unit Info Panel:

```
Assets/UI/UnitInfoPanel/
├── Sprites/
│   ├── panel_bg.png              ← Figma 11:2 (parchment gradient only)
│   ├── portrait_bg.png           ← Figma 11:3 (navy gradient + gold stroke)
│   ├── equipment_bg.png          ← Figma 11:4 (navy gradient + gold stroke)
│   └── portrait_placeholder.png  ← Figma 3:189 (character silhouette)
├── Icons/
│   ├── items_divider_left.png
│   ├── items_divider_right.png
│   ├── icon_iron_lance.png (etc — 5 item icons)
│   ├── class_wyvern_knight.png
│   ├── corner_ornament_a.png
│   └── corner_ornament_b.png
└── Fonts/
    ├── Cinzel-VF.ttf + Cinzel SDF.asset
    ├── CinzelDecorative-Bold.ttf + CinzelDecorative SDF.asset
    ├── CormorantGaramond-VF.ttf + CormorantGaramond SDF.asset
    └── CormorantGaramond-Italic-VF.ttf + CormorantGaramondItalic SDF.asset

Assets/Editor/UnitInfoPanelBuilder.cs   ← constructs the hierarchy under Project Astra/Build Unit Info Panel (temp)
```

Figma file (`Hm2UvlLCNZiq6Hh4uhFMaS`) was restructured via `use_figma` so that frames 3:3, 3:180,
3:106 each have a `Background` rectangle child (11:2, 11:3, 11:4) owning the visual props. The
original parent frames are now pure layout containers.

---

## 6. Claude workflow for a new UI screen

When a future Claude Code session is asked "create the X UI screen":

1. **Check this document.** Read it fully before touching Figma or Unity.
2. **Concept phase (REQUIRED for new screens; optional when restructuring an existing Figma file you're already happy with).** Invoke the `frontend-design` skill and produce **2–3 HTML/CSS mockups** that represent different aesthetic interpretations of the brief (color palette variants, typography pairings, ornament treatments, component proportions). Screenshot each. Present them to the user. **Do not proceed to Figma until the user picks a direction.** Having a reference image does NOT skip this step — see §0.8. The mockups are throwaway deliverables whose only job is to de-risk the aesthetic decision before committing it to Figma. Typical budget: 15–30 minutes.

    **Every mockup must include:**
    - All interactive states for every interactive element (§4.6 — default / hover / pressed / focused / selected / disabled). Shown either as a state-gallery at the bottom of the HTML or via a toggle. If an element doesn't show its states, the mockup is not finished.
    - Exact CSS values for every visual effect (§4.5 — `text-shadow`, `box-shadow`, `filter: drop-shadow`, `filter: blur`). These are the literal source of truth for the Unity implementation; Unity will read them back out of the HTML when generating TMP material presets and sprite-padding specs.
3. **Figma phase:**
    - If a Figma file doesn't exist yet, create one via `mcp__figma__create_new_file` + `use_figma` for authoring. Follow the layer rules in §2 from the start.
    - If a file exists but doesn't follow the rules, restructure it via `use_figma` (add `Background` rects, clear parent frame visuals). Never edit Figma files by hand.
    - Mark all `Background` rects and leaf icons with `exportSettings = [{ format: "PNG", constraint: { type: "SCALE", value: 2 } }]`.
4. **Asset export:** Use the REST API `curl` pattern in §3 to download PNGs into `Assets/UI/{Screen}/Sprites/` and `Assets/UI/{Screen}/Icons/`. Configure import settings per §4.4. **Export every interactive-element state variant** (§4.6), not just the default, and make sure every frame with soft effects is padded per §4.5 Route A before export.
5. **Fonts:** Download TTFs, generate TMP font assets per §4.3.
6. **Text glow/shadow materials:** For every `text-shadow` rule in the HTML concept, generate a TMP material preset under `Assets/UI/{Screen}/Materials/` per §4.5 Route B. Reuse the font's SDF texture, switch the shader to the full `TextMeshPro/Distance Field` variant, and write the CSS values into `_GlowColor`/`_GlowOuter`/`_UnderlayColor`/`_UnderlayOffsetY`. One material per text style per state (e.g. `CinzelTitleGlow.mat`, `CinzelButtonGlow.mat`, `CinzelButtonGlowHover.mat`).
7. **Builder script:** Write `Assets/Editor/{Screen}Builder.cs` under a `Project Astra/Build {Screen}` menu item. Reference the hierarchy translation table in §4.2. Assign state sprites to `Button.spriteState` (§4.6). Assign glow materials to TMP labels via `tmpText.fontMaterial = ...`.
8. **Verify:** Execute the builder, enter play mode, capture a screenshot via `manage_camera`, compare to Figma. Iterate on layout positions if needed. Also verify each interactive state visually (hover a button, capture; click-hold, capture; tab-focus, capture) and compare each to its mockup spec.
9. **Commit:** Include the sprites (all state variants), icons, fonts, TMP assets, material presets, and builder script in the PR. Do **not** commit the scene unless asked — builder re-runs regenerate the hierarchy.

### Non-negotiables

- **Never start building a UI screen without completing §0.** Ask "full-screen or modal?" and "target dimensions?" before touching Figma or Unity. Do not assume Figma's existing frame size is correct — resize the Figma file first if it doesn't match.
- **Never skip the concept phase for a new screen.** Even when the user provides a reference image, run the `frontend-design` skill and produce 2–3 HTML/CSS mockups first. A reference is one data point, not a locked direction. Only skip concept phase when restructuring an already-approved existing Figma file, or when the user explicitly says "use this exact reference, no exploration".
- **Never reproduce a reference image pixel-for-pixel in shipped assets.** References are style anchors. What we ship must be original typography, original ornament shapes, original textures, at HD quality (not pixelated), at the project's canvas aspect ratio (16:9 / 1920×1080 by default), regardless of the reference's native format. See §0.8.
- **Never ship an interactive element without all its states.** Every button, list row, toggle, tab, slider, draggable, or cursor-selectable cell must have default / hover / pressed / focused / selected / disabled defined in the HTML mockup, authored as Figma component variants, and wired up in the Unity builder (sprite-swap + state-change motion). If the mockup can't show you a state, you can't build it — go back to the concept phase. See §0.6 and §4.6.
- **Never approximate a mockup's visual effects.** Every `text-shadow`, `box-shadow`, `filter: drop-shadow`, and `filter: blur` in the concept HTML must reach Unity with its exact values — baked into sprite alpha (with padding), as TMP Glow/Underlay material properties, or via URP Bloom. "Close enough" is not a route. See §0.7 and §4.5.
- Never bake text into a background PNG. If a Figma export contains text you want live in Unity, restructure Figma first, then re-export.
- Never build in-game UI in UI Toolkit. Always uGUI.
- Never edit sprites by hand in Unity. Sprites come from Figma; changes go back to Figma and re-export.
- Never skip the `Background` rectangle pattern, even for "quick" prototypes. The hour you save in Figma becomes three hours of text-doubling debugging in Unity.
- Every builder script must declare `CanvasWidth`, `CanvasHeight`, `IsFullScreen` constants and assert the outer panel matches at build time.

---

## 7. Troubleshooting cheat sheet

**Exported PNG has text/content I didn't want.** Figma frame doesn't follow Rule 1 — add a `Background` rectangle child via `use_figma`, re-export the rectangle, not the frame.

**TMP text renders invisible in play mode, but other TMP text works.** Custom font asset's atlas wasn't saved as a sub-asset. Recreate with `AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset)` and `AssetDatabase.AddObjectToAsset(material, fontAsset)` after `TryAddCharacters`.

**Exported frame shows empty.** A child has `visible = false`. Check via `figma.getNodeByIdAsync(id).visible`. Toggle it back on with `use_figma`.

**Text doubled (baked + live at slightly different positions).** You're using a Figma export that has the text baked in AND rendering live TMP at the same spot. Delete one. Almost always the answer is: restructure Figma so the export has no text, keep the live TMP.

**uGUI panel doesn't show in screenshot via `manage_camera`.** UI Toolkit overlays don't show in game-view camera captures in editor mode — but uGUI does. If you're not seeing your uGUI panel, check: Canvas `renderMode = ScreenSpaceOverlay`, Canvas `sortingOrder` high enough, and that you're in play mode if the panel is built at runtime.

**Compile error in `execute_code` about `using` directives.** `execute_code` runs as a method body, not a file. Use fully-qualified names (`UnityEditor.AssetDatabase`, `TMPro.TMP_FontAsset`) instead of `using` statements. `codedom` compiler is C# 6 only; `roslyn` isn't always available.

**Panel is smaller than the canvas — there's a visible margin around it.** §0 was skipped. The Figma frame was authored at a size smaller than the Unity canvas reference resolution. Either (a) declare the screen a modal popup and add a dim backdrop Image as a sibling (the margin becomes intentional), or (b) rescale the Figma panel to match the canvas via `mcp__figma__use_figma` + `rescale(scale)` + `resizeWithoutConstraints(width, height)`, then re-export every `Background` rectangle and update the builder's `Scale` constant. Never fix this in Unity alone — the Figma source must match the Unity target or they drift again next iteration.

**Figma `rescale(scale)` leaves ~0.03px drift.** The node's aspect ratio wasn't mathematically exact to begin with, so scaling by a single factor can't land on exact target dimensions. Follow `rescale()` with `resizeWithoutConstraints(targetW, targetH)` to snap, and do the same on the node's `Background` child rectangle so their sizes match.

**Canvas reference resolution differs across builders.** All builders in this project use `1920×1080` for `CanvasScaler.referenceResolution` with `ScreenMatchMode.MatchWidthOrHeight = 0.5`. Do not deviate without user sign-off — it would cause existing UIs to misscale when mixed in the same scene.
