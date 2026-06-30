# Community Claude Skills for Character Design — Research Findings

*Web survey (2026-06-21) of third-party/community Claude Code skills & marketplaces relevant to character design, concept art, narrative, and game design for Project Astra. Goal: find an off-the-shelf skill worth installing, or confirm the gap and decide to build our own.*

---

## Headline

**There is no mature, dedicated "game character designer" skill that fits an HD-2D pixel-art, Indian-mythology SRPG.** The community ecosystem clusters into four buckets, none a bullseye:

1. **3D/engine/shader dev skills** — the bulk of "game" skills (Unity/Unreal/Godot/Blender/Three.js/shaders). Irrelevant to our 2D pixel pipeline.
2. **Prose-fiction "story" skills** — strong on character writing, lore, world-building, plot. The closest useful match for our *narrative* side.
3. **Whole-studio agent swarms** — large multi-agent "AI game studio" templates. Useful prompts buried inside, but built to scaffold a *new* project, not drop into our mature repo.
4. **Generic image-prompt / design skills** — anime-character-prompt generators and web/graphic-design skills. Either stylistically mismatched or a competitor to our existing PixelLab pipeline.

Two skills are worth installing/evaluating; the rest are skip or mine-for-ideas. Full verdicts below.

---

## Tier 1 — Worth installing & trialling

### `story-skills` (danjdewhurst) — the best narrative/character match
- **What it is:** Six modular agent-skills for fiction, MIT-licensed, actively developed.
  - `story-init` — scaffolds a story bible, folders, registries
  - **`character-management`** — character profiles with relationships, traits, arcs, **family trees**
  - **`worldbuilding`** — locations + systems (magic, politics, religion, technology)
  - `plot-structure` — arcs via three-act, hero's journey, Save the Cat, **kishōtenketsu**
  - `chapter-writing` — outline-first drafting pulling from story context
  - `revision-continuity` — continuity audits, keeps character state/timeline consistent
- **Install:** `/plugin marketplace add danjdewhurst/story-skills` → `/plugin install story-skills@story-skills`
- **Fit:** `character-management` + `worldbuilding` map closely onto our `character-sheets/TEMPLATE.md` (the Personality/Background/Relationships/Family-Tree/Arc sections). It could speed up the *writing* half of character design and keep a consistent story bible across the roster.
- **Caveat:** Prose-fiction framing — it knows nothing about FE stat blocks, pancha-bhuta affinity, dharma alignment, map sprites, or our HD-2D identity. It complements our sheet; it doesn't replace it. The kishōtenketsu/arc tooling is a nice bonus for an Indian-myth narrative.
- **Verdict:** **Install and trial on one character.** Lowest-risk way to get community value today. Clean marketplace install, modular, MIT.

---

## Tier 2 — Mine for ideas, don't adopt wholesale

### `Claude-Code-Game-Studios` (Donchitos) — the comprehensive studio template
- **What it is:** A full "AI game studio" — ~49 agents + ~72 skills in a director→lead→specialist hierarchy. MIT. Reported ~22k stars / ~3.2k forks / v1.0.0 (May 2026); very active.
- **Relevant pieces:**
  - Agents: `art-director`, `world-builder`, `narrative-director`, `writer`, `technical-artist`, `ux-designer`
  - Skills (slash commands): `/art-bible` (visual direction), `/brainstorm` (character/world concepts), `/asset-spec`, `/asset-audit`, `/design-system`
- **Install:** It's a **`git clone` template**, not a marketplace skill — `git clone …/Claude-Code-Game-Studios.git my-game` then run `claude` inside. It scaffolds a *new* game project.
- **Fit / caveat:** The individual prompts (`/art-bible`, the `art-director` / `world-builder` / `narrative-director` agent definitions) are high-quality and worth reading/adapting. But grafting a 49-agent swarm onto our existing, mature Astra repo is the wrong shape — it assumes a greenfield project and its own folder conventions.
- **Verdict:** **Don't install into Astra. Read its `art-bible`, `world-builder`, and `narrative-director` prompts and lift the good structure into our own pipeline / a custom skill.**

---

## Tier 3 — Alternative tools, but we're already covered

### `game-asset-generation` (eachlabs) — pixel/sprite generation via external API
- **What:** Generates pixel-art sprites, **character animation sheets**, tilesets, icons; has a `session_id` for style consistency across a set.
- **Dependency:** Routes through the external **each::sense AI API** (another image model). ~246 installs / ~15 stars.
- **Install:** `npx -y skills add eachlabs/skills --skill game-asset-generation --agent claude-code`
- **Fit:** Overlaps directly with our **PixelLab** pipeline, which we already use, have a documented prompting playbook for, and which is purpose-built for game sprites. This adds a second external dependency for no clear gain.
- **Verdict:** **Skip unless you want to A/B a second generator against PixelLab.** Not a reason to change our visual pipeline.

---

## Tier 4 — Skip (poor fit or off-topic)

- **`AI Character Designer` (mcpmarket.com)** — directly named, but per its listing it's a generic **anime / sketch / "yuru-chara"** *image-prompt generator* for "game assets, icons, marketing." (Page was rate-limited; couldn't fully vet.) Stylistically mismatched with our HD-2D Indian-myth pixel identity and our culturally-grounded, sheet-driven process. **Skip** (or open it yourself to confirm).
- **Snyk's "Top 8 Claude Skills for Game Dev"** — all 3D: Unity/Unreal/Godot expertise, Blender (bpy), Three.js/R3F, shader (HLSL/GLSL), CAD. **Zero 2D/pixel/character relevance** for us.
- **General design skills** — `canvas-design`, `theme-factory`, `image-enhancer` (ComposioHQ); `anydesign` (@uxKero); `swiftui-design-skill`; `jiji262/claude-design-skill` (HTML artifacts); `rohitg00/awesome-claude-design`. These are **web/graphic/UI** design, not game character art. (The last is a decent bookmark for UI *aesthetic* prompts only.)

---

## Curated lists & directories to bookmark for ongoing discovery

There is no single global skill index — discovery is via these:
- **Curated lists:** [ComposioHQ/awesome-claude-skills](https://github.com/ComposioHQ/awesome-claude-skills) · [travisvn/awesome-claude-skills](https://github.com/travisvn/awesome-claude-skills) · [BehiSecc/awesome-claude-skills](https://github.com/BehiSecc/awesome-claude-skills) · [hesreallyhim/awesome-claude-code](https://github.com/hesreallyhim/awesome-claude-code)
- **Big multi-platform collections:** [VoltAgent/awesome-agent-skills](https://github.com/VoltAgent/awesome-agent-skills) (1000+) · [alirezarezvani/claude-skills](https://github.com/alirezarezvani/claude-skills) (337) · [simota/agent-skills](https://github.com/simota/agent-skills) (140+)
- **Directory sites:** [claudemarketplaces.com](https://claudemarketplaces.com/) · [mcpmarket.com](https://mcpmarket.com/) · [awesomeclaude.ai](https://awesomeclaude.ai/)
- **Design-specific:** [rohitg00/awesome-claude-design](https://github.com/rohitg00/awesome-claude-design)

How to add any community marketplace: `/plugin marketplace add owner/repo`, then its skills appear in the Discover tab.

---

## Conclusion → the recommended move

The survey **confirms the gap**: the highest-leverage option is still to author a **custom `astra-character-designer` skill** that fuses the best of what's out there with what we already have:
- the **modular structure** of `story-skills` → `character-management` (relationships, arcs, family trees, continuity),
- the **art-direction discipline** of Game-Studios' `/art-bible` + `world-builder` prompts,
- our **existing `character-sheets/TEMPLATE.md` schema** (FE stats, pancha-bhuta, dharma, silhouette/garment/sigil briefs, dialogue libraries),
- our **PixelLab prompting conventions** for the visual output.

That produces a tool tuned to Astra that no community skill matches. Cheaper interim option: install `story-skills`, trial it on one character, and decide whether its flow earns a place before building anything.

---

## Sources
- [ComposioHQ/awesome-claude-skills](https://github.com/ComposioHQ/awesome-claude-skills) · [travisvn/awesome-claude-skills](https://github.com/travisvn/awesome-claude-skills) · [hesreallyhim/awesome-claude-code](https://github.com/hesreallyhim/awesome-claude-code) · [VoltAgent/awesome-agent-skills](https://github.com/VoltAgent/awesome-agent-skills)
- [Donchitos/Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios) · [danjdewhurst/story-skills](https://github.com/danjdewhurst/story-skills) · [rohitg00/awesome-claude-code-toolkit](https://github.com/rohitg00/awesome-claude-code-toolkit)
- [Snyk — Top Claude Skills for 3D/Game Dev](https://snyk.io/articles/top-claude-skills-3d-modeling-game-dev-shader-programming/) · [eachlabs game-asset-generation](https://claudemarketplaces.com/skills/eachlabs/skills/game-asset-generation) · [AI Character Designer (mcpmarket)](https://mcpmarket.com/tools/skills/ai-character-designer)
- [claudemarketplaces.com](https://claudemarketplaces.com/) · [Claude Code plugin marketplaces docs](https://code.claude.com/docs/en/plugin-marketplaces)
