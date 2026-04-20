#!/usr/bin/env bash
# Supply Convoy Figma exports (Variant A · Indigo Codex).
# All panels with drop-shadow effects are fetched via REST with
# use_absolute_bounds=true (see download_inventory_assets.sh for
# the explanation — Plugin API refuses useAbsoluteBounds on non-
# text nodes, so REST is the only lever).
set -euo pipefail
cd "$(dirname "$0")/.."

J=.secrets/figma_supply_urls.json
SPR=Assets/UI/SupplyConvoy/Sprites
mkdir -p "$SPR"

get() { python -c "import json; d=json.load(open('$J')); print(d['images'].get('$1') or '')"; }

declare -A MAP=(
  # main chrome (panel backgrounds)
  ["3:5"]="$SPR/cartouche_bg.png"
  ["3:12"]="$SPR/portrait_panel_bg.png"
  ["3:14"]="$SPR/portrait_slab_bg.png"
  ["3:19"]="$SPR/bubble_bg.png"
  ["3:25"]="$SPR/give_btn_bg.png"
  ["3:29"]="$SPR/take_btn_bg.png"
  ["3:33"]="$SPR/lord_inv_bg.png"
  ["4:4"]="$SPR/convoy_head_bg.png"
  ["4:9"]="$SPR/tabstrip_bg.png"
  ["4:51"]="$SPR/convoy_list_bg.png"
  ["4:146"]="$SPR/scrollrail_bg.png"
  ["4:147"]="$SPR/scrollrail_thumb.png"
  ["4:148"]="$SPR/scrollrail_cap_top.png"
  ["4:149"]="$SPR/scrollrail_cap_bottom.png"
  ["4:151"]="$SPR/footer_hints_bg.png"
  # state variants
  ["5:3"]="$SPR/row_default.png"
  ["5:5"]="$SPR/row_hover.png"
  ["5:7"]="$SPR/row_focused.png"
  ["5:9"]="$SPR/row_depleted.png"
  ["5:11"]="$SPR/row_disabled.png"
  ["5:13"]="$SPR/tab_default.png"
  ["5:15"]="$SPR/tab_hover.png"
  ["5:17"]="$SPR/tab_focused.png"
  ["5:19"]="$SPR/tab_active.png"
  ["5:21"]="$SPR/submenu_default.png"
  ["5:23"]="$SPR/submenu_hover.png"
  ["5:25"]="$SPR/submenu_pressed.png"
  ["5:27"]="$SPR/submenu_active.png"
  ["5:29"]="$SPR/slot_default.png"
  ["5:31"]="$SPR/slot_focused.png"
  ["5:33"]="$SPR/slot_equipped.png"
  ["5:35"]="$SPR/slot_depleted.png"
  ["5:37"]="$SPR/slot_empty.png"
  ["5:39"]="$SPR/keycap_default.png"
  ["5:41"]="$SPR/keycap_pressed.png"
)

for k in "${!MAP[@]}"; do
  url=$(get "$k")
  out="${MAP[$k]}"
  if [ -z "$url" ]; then
    echo "SKIP $k (no URL)"
    continue
  fi
  echo "GET  $k -> $out"
  curl -s -o "$out" "$url"
done

echo "Done."
ls "$SPR" | wc -l
