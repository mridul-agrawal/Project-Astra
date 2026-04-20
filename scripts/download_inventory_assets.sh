#!/usr/bin/env bash
# One-off downloader for Inventory Popup Figma exports.
#
# Note on panel backgrounds (7:4 / 7:6 / 7:8): those rects carry a DROP_SHADOW
# effect that inflates the render bounds by L/R/T/B = 60/60/36/84 px, producing a
# PNG with transparent halo around the colored fill. To strip that halo we re-fetch
# those three node IDs separately with &use_absolute_bounds=true (Figma's REST
# query parameter — the Plugin API refuses useAbsoluteBounds on non-text nodes).
# If you ever regenerate the URL cache, make sure the panel URLs were fetched that
# way; otherwise the popup will render shrunken again.
set -euo pipefail
cd "$(dirname "$0")/.."

J=.secrets/figma_inventory_urls.json
SPR=Assets/UI/InventoryPopup/Sprites
ICO=Assets/UI/InventoryPopup/Icons
mkdir -p "$SPR" "$ICO"

get() { python -c "import json,sys; d=json.load(open('$J')); print(d['images'].get('$1') or '')"; }

declare -A MAP=(
  ["7:4"]="$SPR/portrait_panel_bg.png"
  ["7:6"]="$SPR/inventory_panel_bg.png"
  ["7:8"]="$SPR/stats_panel_bg.png"
  ["8:3"]="$SPR/portrait_art_bg.png"
  ["10:9"]="$SPR/stat_pill_bg.png"
  ["10:42"]="$SPR/provenance_footer_bg.png"
  ["12:5"]="$SPR/row_default.png"
  ["12:13"]="$SPR/row_empty.png"
  ["12:18"]="$SPR/row_depleted.png"
  ["16:2"]="$SPR/row_selected.png"
  ["11:4"]="$ICO/sigil_sword.png"
  ["11:11"]="$ICO/sigil_lance.png"
  ["11:18"]="$ICO/sigil_axe.png"
  ["11:24"]="$ICO/sigil_bow.png"
  ["11:31"]="$ICO/sigil_staff.png"
  ["11:38"]="$ICO/sigil_consumable.png"
  ["11:44"]="$ICO/kirtimukha.png"
  ["11:55"]="$ICO/corner_filigree.png"
  ["11:62"]="$ICO/butiband.png"
  ["11:82"]="$ICO/selection_caret.png"
)

for k in "${!MAP[@]}"; do
  url=$(get "$k")
  out="${MAP[$k]}"
  if [ -z "$url" ]; then
    echo "SKIP $k (no URL — layout-only frame)"
    continue
  fi
  echo "GET  $k -> $out"
  curl -s -o "$out" "$url"
done

echo "Done."
ls -la "$SPR" "$ICO"
