/* Project Astra — Character Sheet renderer
 *
 * Loads a markdown sheet (frontmatter YAML + prose body), splits the body by
 * top-level "# Heading" sections, and lays out 3 pages:
 *   1. Front card     (synthesized from frontmatter)
 *   2. Visual brief   (frontmatter palette + portrait + "# Visual Design" body)
 *   3. Lore + meta    ("# Combat Kit" + "# Personality & Lore" + "# Dialogue Library" + "# Design Meta")
 */

const SHEET_PAGES = {
  visual: "Visual Design",
  combat: "Combat Kit",
  lore: "Personality & Lore",
  dialogue: "Dialogue Library",
  meta: "Design Meta",
};

const STAT_KEYS = ["hp","str","mag","skl","spd","lck","def","res","con","mov"];
const PALETTE_ROLES = ["primary","secondary","accent","metal","cloth"];

const EM_DASH = "—";

// ---------- Boot ----------

document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("print-btn").addEventListener("click", () => window.print());
  await buildSidebar();
  const initial = window.location.hash.replace(/^#/, "") || "TEMPLATE.md";
  await loadAndRender(initial);
  window.addEventListener("hashchange", () => {
    const target = window.location.hash.replace(/^#/, "") || "TEMPLATE.md";
    loadAndRender(target);
  });
});

async function buildSidebar() {
  const list = document.getElementById("character-list");
  list.innerHTML = "";

  // Always include the empty TEMPLATE preview at the top
  appendGroupLabel(list, "Template");
  appendLink(list, "TEMPLATE.md", "TEMPLATE — empty preview");

  // Append manifest characters
  let manifest = { characters: [] };
  try {
    const res = await fetch("characters/manifest.json", { cache: "no-store" });
    if (res.ok) manifest = await res.json();
  } catch (_) { /* manifest is optional */ }

  if (manifest.characters && manifest.characters.length) {
    appendGroupLabel(list, "Characters");
    for (const c of manifest.characters) {
      appendLink(list, c.file, c.title || c.id);
    }
  }
}

function appendGroupLabel(parent, text) {
  const el = document.createElement("div");
  el.className = "group-label";
  el.textContent = text;
  parent.appendChild(el);
}

function appendLink(parent, file, title) {
  const a = document.createElement("a");
  a.href = "#" + file;
  a.textContent = title;
  a.dataset.file = file;
  parent.appendChild(a);
}

// ---------- Load + render ----------

async function loadAndRender(file) {
  const error = document.getElementById("error");
  const pagesEl = document.getElementById("pages");
  error.hidden = true;

  // mark active link
  document.querySelectorAll("#character-list a").forEach(a => {
    a.classList.toggle("active", a.dataset.file === file);
  });

  let text = null;

  // Try fetch first (works under a local server). On file:// this throws.
  try {
    const res = await fetch(file, { cache: "no-store" });
    if (res.ok) text = await res.text();
  } catch (_) { /* fall through to embedded fallback */ }

  // Fallback: an embedded copy in index.html (lets file:// work for the built-in template)
  if (text === null) {
    const embedded = document.getElementById("embedded-" + file);
    if (embedded) text = embedded.textContent;
  }

  if (text === null) {
    pagesEl.innerHTML = "";
    error.hidden = false;
    error.innerHTML = `
      <strong>Could not load <code>${escapeHtml(file)}</code>.</strong>
      <p>Filled-in character files in <code>characters/</code> can&rsquo;t be read directly from <code>file://</code> — your browser blocks it for security.
      Run a one-line local server from this folder:</p>
      <pre>cd docs/character-sheets
python -m http.server 8000</pre>
      <p>Then open <a href="http://localhost:8000/">http://localhost:8000/</a>. The empty TEMPLATE preview works without a server.</p>
    `;
    return;
  }

  const { frontmatter, body } = parseSheet(text);
  const sections = splitSections(body);

  pagesEl.innerHTML = "";
  pagesEl.appendChild(renderFrontCard(frontmatter));
  pagesEl.appendChild(renderVisualBrief(frontmatter, sections[SHEET_PAGES.visual] || ""));
  pagesEl.appendChild(renderLoreAndMeta(sections));
}

// ---------- Parse ----------

function parseSheet(text) {
  const m = text.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n([\s\S]*)$/);
  if (!m) return { frontmatter: {}, body: text };
  let frontmatter = {};
  try {
    frontmatter = jsyaml.load(m[1]) || {};
  } catch (e) {
    console.error("YAML parse error:", e);
  }
  return { frontmatter, body: m[2] };
}

function splitSections(body) {
  const out = {};
  const re = /^# (.+)$/gm;
  const headings = [];
  let mm;
  while ((mm = re.exec(body)) !== null) {
    headings.push({ title: mm[1].trim(), index: mm.index, headerEnd: mm.index + mm[0].length });
  }
  for (let i = 0; i < headings.length; i++) {
    const h = headings[i];
    const next = headings[i + 1];
    const start = h.headerEnd;
    const end = next ? next.index : body.length;
    out[h.title] = body.slice(start, end).trim();
  }
  return out;
}

// ---------- Render: Page 1 — Front Card ----------

function renderFrontCard(fm) {
  const page = pageEl("Page 1 · Front Card");

  const header = document.createElement("div");
  header.className = "fc-header";
  header.innerHTML = `
    <div class="fc-name-block">
      <h2 class="fc-name-deva">${escapeHtml(orDash(fm.name_devanagari))}</h2>
      <p class="fc-name-rom">${escapeHtml(orDash(fm.name_romanized))}</p>
      <p class="fc-name-ipa">${fm.name_ipa ? "/" + escapeHtml(fm.name_ipa) + "/" : ""}</p>
    </div>
    <div class="fc-epithet-block">
      <p class="fc-epithet">${fm.epithet ? "&ldquo;" + escapeHtml(fm.epithet) + "&rdquo;" : EM_DASH}</p>
      <span class="fc-status">${escapeHtml(orDash(fm.status, "draft"))}</span>
    </div>
  `;
  page.appendChild(header);

  const hook = document.createElement("p");
  hook.className = "fc-hook";
  hook.textContent = orDash(fm.silhouette_one_liner, "(silhouette one-liner — describe what makes this character readable from across the map)");
  page.appendChild(hook);

  const body = document.createElement("div");
  body.className = "fc-body";

  // Portrait + palette
  const portraitBlock = document.createElement("div");
  portraitBlock.className = "fc-portrait-block";
  const portrait = document.createElement("div");
  portrait.className = "fc-portrait";
  if (fm.portrait_path) {
    const img = document.createElement("img");
    img.src = fm.portrait_path;
    img.alt = "portrait";
    img.onerror = () => { portrait.innerHTML = "PORTRAIT 1024 × 1024"; };
    portrait.appendChild(img);
  } else {
    portrait.textContent = "PORTRAIT 1024 × 1024";
  }
  portraitBlock.appendChild(portrait);
  portraitBlock.appendChild(renderPalette(fm.palette || {}));
  body.appendChild(portraitBlock);

  // Identity grid
  const id = document.createElement("div");
  id.className = "fc-identity";
  id.appendChild(renderIdentityGrid(fm));
  body.appendChild(id);

  page.appendChild(body);

  // Stat grid
  page.appendChild(renderStatGrid(fm));

  // Auxiliary blocks (weapon ranks + promotion paths)
  page.appendChild(renderAuxBlocks(fm));

  // Footer
  const foot = document.createElement("div");
  foot.className = "fc-foot";
  foot.innerHTML = `
    <span>Project Astra · Character Sheet</span>
    <span>${escapeHtml(orDash(fm.author, "author —"))} · ${escapeHtml(orDash(fm.last_updated, "YYYY-MM-DD"))}</span>
  `;
  page.appendChild(foot);

  return page;
}

function renderPalette(palette) {
  const wrap = document.createElement("div");
  wrap.className = "fc-palette";
  wrap.innerHTML = `<div class="fc-palette-label">Palette</div>`;
  const row = document.createElement("div");
  row.className = "fc-palette-row";
  for (const role of PALETTE_ROLES) {
    const hex = palette[role] || "";
    const sw = document.createElement("div");
    sw.className = "fc-swatch";
    const color = document.createElement("div");
    color.className = "fc-swatch-color" + (hex ? "" : " fc-swatch-empty");
    if (hex) color.style.background = hex;
    const r = document.createElement("div");
    r.className = "fc-swatch-role";
    r.textContent = role;
    const h = document.createElement("div");
    h.className = "fc-swatch-hex";
    h.textContent = hex || EM_DASH;
    sw.appendChild(color); sw.appendChild(r); sw.appendChild(h);
    row.appendChild(sw);
  }
  wrap.appendChild(row);
  return wrap;
}

function renderIdentityGrid(fm) {
  const grid = document.createElement("div");
  grid.className = "fc-identity-grid";

  const fields = [
    ["Lineage",      fm.mythological_lineage, true],
    ["Faction",      fm.faction],
    ["Banner",       fm.banner],
    ["Class",        fm.class],
    ["Role",         fm.role_archetype],
    ["Recruit",      joinPair(fm.recruitment_chapter, fm.recruitment_method, " · ")],
    ["Available",    joinRange(fm.availability_start, fm.availability_end)],
    ["Move Type",    fm.movement_type],
    ["Affinity",     fm.pancha_bhuta_affinity],
    ["Personality",  fm.personality_enum],
    ["Age",          fm.age],
    ["Gender",       fm.gender],
    ["Species",      fm.species],
    ["Height",       fm.height],
    ["Voice",        fm.voice_direction],
    ["Level",        fm.starting_level],
  ];

  for (const [label, value, em] of fields) {
    const row = document.createElement("div");
    row.className = "fc-id-row";
    row.innerHTML = `
      <span class="fc-id-label">${label}</span>
      <span class="fc-id-value${em ? " fc-id-value--em" : ""}">${escapeHtml(orDash(value))}</span>
    `;
    grid.appendChild(row);
  }
  return grid;
}

function renderStatGrid(fm) {
  const wrap = document.createElement("div");
  wrap.className = "fc-stats";
  wrap.innerHTML = `<div class="fc-stats-title">Stats &middot; FE-canonical</div>`;
  const grid = document.createElement("div");
  grid.className = "stat-grid";

  // header row
  grid.appendChild(cell("", "stat-head stat-row-label"));
  for (const k of STAT_KEYS) grid.appendChild(cell(k.toUpperCase(), "stat-head"));

  // bases / growths / caps
  appendStatRow(grid, "Bases", fm.bases || {}, "stat-cell");
  appendStatRow(grid, "Growth %", fm.growths || {}, "stat-cell stat-cell--growth", v => v ? v + "%" : "0%");
  appendStatRow(grid, "Cap ±", fm.cap_modifiers || {}, "stat-cell stat-cell--cap", v => (v > 0 ? "+" + v : (v || 0).toString()));

  wrap.appendChild(grid);
  return wrap;
}

function appendStatRow(grid, label, data, cellCls, formatter) {
  grid.appendChild(cell(label, "stat-row-label"));
  for (const k of STAT_KEYS) {
    const v = data[k];
    const display = formatter ? formatter(v) : ((v === undefined || v === null) ? "0" : String(v));
    const cls = cellCls + (v ? "" : " stat-cell--zero");
    grid.appendChild(cell(display, cls));
  }
}

function renderAuxBlocks(fm) {
  const aux = document.createElement("div");
  aux.className = "fc-aux";

  // Weapon Ranks
  const wr = document.createElement("div");
  wr.className = "fc-aux-block";
  wr.innerHTML = `<div class="fc-aux-label">Weapon Ranks</div>`;
  const wrList = document.createElement("div");
  wrList.className = "fc-aux-list";
  const ranks = fm.weapon_ranks || {};
  const wkeys = ["sword","lance","axe","bow","dagger","tome","staff"];
  for (const wk of wkeys) {
    const k = document.createElement("span"); k.className = "k"; k.textContent = wk;
    const v = document.createElement("span"); v.className = "v"; v.textContent = orDash(ranks[wk]);
    wrList.appendChild(k); wrList.appendChild(v);
  }
  wr.appendChild(wrList);
  aux.appendChild(wr);

  // Promotion Paths
  const pp = document.createElement("div");
  pp.className = "fc-aux-block";
  pp.innerHTML = `<div class="fc-aux-label">Promotion Paths</div>`;
  const ppText = document.createElement("div");
  ppText.style.fontFamily = "var(--font-display)";
  ppText.style.fontSize = "13px";
  ppText.style.color = "var(--ink)";
  const paths = (fm.promotion_paths && fm.promotion_paths.length) ? fm.promotion_paths.join(" → ") : EM_DASH;
  ppText.textContent = paths;
  pp.appendChild(ppText);
  aux.appendChild(pp);

  return aux;
}

// ---------- Render: Page 2 — Visual Brief ----------

function renderVisualBrief(fm, mdSection) {
  const page = pageEl("Page 2 · Visual Brief");

  const title = document.createElement("h2");
  title.className = "section-title";
  title.textContent = "Visual Design";
  page.appendChild(title);

  const sub = document.createElement("p");
  sub.className = "section-sub";
  sub.textContent = "Artist hand-off · everything needed to illustrate this character";
  page.appendChild(sub);

  // Top strip: portrait + palette + silhouette one-liner
  const strip = document.createElement("div");
  strip.style.display = "grid";
  strip.style.gridTemplateColumns = "200px 1fr";
  strip.style.gap = "20px";
  strip.style.marginBottom = "16px";
  strip.style.paddingBottom = "16px";
  strip.style.borderBottom = "1px solid var(--rule)";

  const pBox = document.createElement("div");
  pBox.className = "fc-portrait";
  pBox.style.width = "200px"; pBox.style.height = "200px";
  if (fm.portrait_path) {
    const img = document.createElement("img");
    img.src = fm.portrait_path; img.alt = "portrait";
    img.onerror = () => { pBox.innerHTML = "PORTRAIT"; };
    pBox.appendChild(img);
  } else {
    pBox.textContent = "PORTRAIT";
  }
  strip.appendChild(pBox);

  const right = document.createElement("div");
  right.appendChild(renderPalette(fm.palette || {}));
  const sil = document.createElement("p");
  sil.style.marginTop = "12px";
  sil.style.fontFamily = "var(--font-display)";
  sil.style.fontStyle = "italic";
  sil.style.fontSize = "14px";
  sil.style.color = "var(--ink-soft)";
  sil.textContent = orDash(fm.silhouette_one_liner, "(silhouette one-liner)");
  right.appendChild(sil);
  strip.appendChild(right);

  page.appendChild(strip);

  // Body — markdown of the Visual Design section
  const md = document.createElement("div");
  md.className = "md-content";
  md.innerHTML = renderMarkdown(mdSection);
  page.appendChild(md);

  return page;
}

// ---------- Render: Page 3 — Lore + Meta ----------

function renderLoreAndMeta(sections) {
  const page = pageEl("Page 3 · Lore · Dialogue · Meta");

  const order = [
    { key: "Combat Kit",         sub: "Mechanical kit · skills · personal weapon" },
    { key: "Personality & Lore", sub: "Background · arc · relationships" },
    { key: "Dialogue Library",   sub: "Voice · quotes · in-game lines" },
    { key: "Design Meta",        sub: "Why this character exists" },
  ];

  for (const { key, sub } of order) {
    const md = sections[key] || "";
    const block = document.createElement("section");
    block.style.marginBottom = "20px";
    const title = document.createElement("h2");
    title.className = "section-title"; title.textContent = key;
    const subEl = document.createElement("p");
    subEl.className = "section-sub"; subEl.textContent = sub;
    const content = document.createElement("div");
    content.className = "md-content";
    content.innerHTML = renderMarkdown(md);
    block.appendChild(title);
    block.appendChild(subEl);
    block.appendChild(content);
    page.appendChild(block);
  }

  return page;
}

// ---------- helpers ----------

function pageEl(corner) {
  const p = document.createElement("section");
  p.className = "sheet-page";
  const c = document.createElement("div");
  c.className = "page-corner";
  c.textContent = corner;
  p.appendChild(c);
  return p;
}

function cell(text, cls) {
  const d = document.createElement("div");
  d.className = cls;
  d.textContent = text;
  return d;
}

function orDash(v, fallback) {
  if (v === undefined || v === null) return fallback || EM_DASH;
  if (typeof v === "string" && v.trim() === "") return fallback || EM_DASH;
  return v;
}

function joinPair(a, b, sep) {
  const aa = a && String(a).trim(); const bb = b && String(b).trim();
  if (aa && bb) return aa + sep + bb;
  return aa || bb || "";
}
function joinRange(a, b) {
  const aa = a && String(a).trim(); const bb = b && String(b).trim();
  if (aa && bb) return aa + " → " + bb;
  return aa || bb || "";
}

function renderMarkdown(md) {
  if (!md) return "";
  // Strip HTML comments (our hint markers) so they don't appear as blanks
  const cleaned = md.replace(/<!--[\s\S]*?-->/g, "").trim();
  return marked.parse(cleaned, { breaks: false, gfm: true });
}

function escapeHtml(s) {
  if (s === undefined || s === null) return "";
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
