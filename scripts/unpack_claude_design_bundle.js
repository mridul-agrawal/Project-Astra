#!/usr/bin/env node
// Unpacks a Claude Design "bundler" HTML export into plain HTML + extracted assets.
// The exported file ships as a loader wrapping three <script> blocks:
//   __bundler/manifest      — base64 (+ optional gzip) assets keyed by UUID
//   __bundler/ext_resources — array of { uuid, id } mapping external ids to assets
//   __bundler/template      — JSON-encoded string holding the real HTML, with UUIDs
//                             used as placeholders wherever an asset is referenced
// At runtime, JS atob/gunzips the manifest, swaps UUIDs for blob URLs, and hands
// control to the unpacked document. This script does the same thing statically:
// writes each asset to disk and produces a plain .html pointing at relative paths.
//
// Usage: node unpack_claude_design_bundle.js <input.html> [output-dir]

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const MIME_EXT = {
  'image/png': 'png',
  'image/jpeg': 'jpg',
  'image/jpg': 'jpg',
  'image/gif': 'gif',
  'image/webp': 'webp',
  'image/svg+xml': 'svg',
  'font/woff2': 'woff2',
  'font/woff': 'woff',
  'font/ttf': 'ttf',
  'font/otf': 'otf',
  'application/font-woff2': 'woff2',
  'application/font-woff': 'woff',
  'application/x-font-ttf': 'ttf',
  'application/json': 'json',
  'text/css': 'css',
  'text/javascript': 'js',
  'application/javascript': 'js',
  'application/octet-stream': 'bin',
};

function extFromMime(mime) {
  if (!mime) return 'bin';
  const e = MIME_EXT[mime.toLowerCase()];
  if (e) return e;
  const slash = mime.indexOf('/');
  return slash >= 0 ? mime.slice(slash + 1).replace(/[^a-z0-9]/gi, '').toLowerCase() || 'bin' : 'bin';
}

function extractScriptBlock(html, typeAttr) {
  const re = new RegExp(
    `<script[^>]*type=["']${typeAttr.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&')}["'][^>]*>([\\s\\S]*?)</script>`,
    'i'
  );
  const m = html.match(re);
  return m ? m[1] : null;
}

function main() {
  const [,, inputPath, outputDirArg] = process.argv;
  if (!inputPath) {
    console.error('Usage: node unpack_claude_design_bundle.js <input.html> [output-dir]');
    process.exit(1);
  }

  const inputAbs = path.resolve(inputPath);
  const baseName = path.basename(inputAbs, '.html');
  const outputDir = outputDirArg
    ? path.resolve(outputDirArg)
    : path.join(path.dirname(inputAbs), baseName + '.unpacked');
  const assetsDir = path.join(outputDir, 'assets');
  fs.mkdirSync(assetsDir, { recursive: true });

  const html = fs.readFileSync(inputAbs, 'utf8');

  const manifestText = extractScriptBlock(html, '__bundler/manifest');
  const templateText = extractScriptBlock(html, '__bundler/template');
  const extResText = extractScriptBlock(html, '__bundler/ext_resources');

  if (!manifestText || !templateText) {
    console.error('Missing __bundler/manifest or __bundler/template script block.');
    process.exit(2);
  }

  const manifest = JSON.parse(manifestText);
  let template = JSON.parse(templateText);
  const extResources = extResText ? JSON.parse(extResText) : [];

  const uuids = Object.keys(manifest);
  console.log(`Found ${uuids.length} assets. Extracting to ${assetsDir} ...`);

  const uuidToRelPath = {};
  const mimeCounts = {};
  for (const uuid of uuids) {
    const entry = manifest[uuid];
    let buf = Buffer.from(entry.data, 'base64');
    if (entry.compressed) buf = zlib.gunzipSync(buf);

    const mime = entry.mime || 'application/octet-stream';
    mimeCounts[mime] = (mimeCounts[mime] || 0) + 1;
    const ext = extFromMime(mime);
    const filename = `${uuid}.${ext}`;
    fs.writeFileSync(path.join(assetsDir, filename), buf);
    uuidToRelPath[uuid] = `./assets/${filename}`;
  }

  // Substitute UUID placeholders in the template with relative asset paths.
  // Sort by length desc so longer UUIDs replace before any shorter substring matches.
  const sortedUuids = uuids.slice().sort((a, b) => b.length - a.length);
  for (const uuid of sortedUuids) {
    template = template.split(uuid).join(uuidToRelPath[uuid]);
  }

  // Recreate window.__resources for ext_resources-driven references (e.g. fonts loaded by id).
  const resourceMap = {};
  for (const entry of extResources) {
    if (uuidToRelPath[entry.uuid]) resourceMap[entry.id] = uuidToRelPath[entry.uuid];
  }
  if (Object.keys(resourceMap).length) {
    const payload = JSON.stringify(resourceMap).split('</' + 'script>').join('<\\/' + 'script>');
    const injected = `<script>window.__resources = ${payload};</` + `script>`;
    const headMatch = template.match(/<head[^>]*>/i);
    if (headMatch) {
      const i = headMatch.index + headMatch[0].length;
      template = template.slice(0, i) + injected + template.slice(i);
    } else {
      template = injected + template;
    }
  }

  // SRI hashes + crossorigin were computed against the bundled blobs — stripping them
  // keeps the unpacked references loadable from a local file:// origin.
  template = template
    .replace(/\s+integrity="[^"]*"/gi, '')
    .replace(/\s+crossorigin="[^"]*"/gi, '');

  const outHtmlPath = path.join(outputDir, `${baseName}.unpacked.html`);
  fs.writeFileSync(outHtmlPath, template, 'utf8');

  console.log(`\nWrote ${outHtmlPath}`);
  console.log(`HTML size: ${(template.length / 1024).toFixed(1)} KB`);
  console.log(`Assets: ${uuids.length} files`);
  console.log('Mime breakdown:');
  for (const [mime, n] of Object.entries(mimeCounts).sort((a, b) => b[1] - a[1])) {
    console.log(`  ${mime.padEnd(30)} ${n}`);
  }
  if (extResources.length) {
    console.log(`ext_resources entries: ${extResources.length}`);
  }
}

main();
