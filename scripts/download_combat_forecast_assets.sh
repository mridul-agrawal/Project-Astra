#!/usr/bin/env bash
# Combat Forecast Figma exports (Variant A · Indigo Codex).
# REST with use_absolute_bounds=true for every sprite — the ForecastPanel
# composite carries drop-shadow/vermillion edge bleed that would inflate the
# PNG by ~120px otherwise (same gotcha as InventoryPopup + SupplyConvoy).
set -euo pipefail
cd "$(dirname "$0")/.."

J=.secrets/figma_forecast_urls.json
SPR=Assets/UI/CombatForecast/Sprites
mkdir -p "$SPR"

get() { python -c "import json; d=json.load(open('$J')); print(d['images'].get('$1') or '')"; }

declare -A MAP=(
  ["1:4"]="$SPR/forecast_panel_composite.png"
  ["2:43"]="$SPR/chip_effective.png"
  ["2:45"]="$SPR/badge_ko.png"
  ["2:47"]="$SPR/badge_vs.png"
  ["2:49"]="$SPR/chip_double_as.png"
  ["2:51"]="$SPR/chip_double_brave.png"
  ["2:53"]="$SPR/chip_double_combined.png"
  ["2:55"]="$SPR/tag_atk.png"
  ["2:57"]="$SPR/tag_def.png"
  ["2:59"]="$SPR/weapon_row.png"
  ["2:62"]="$SPR/ribbon_nocounter.png"
  ["2:64"]="$SPR/hp_track.png"
  ["2:66"]="$SPR/hp_fill_green.png"
  ["2:68"]="$SPR/hp_fill_yellow.png"
  ["2:70"]="$SPR/hp_fill_red.png"
  ["2:72"]="$SPR/hp_pred.png"
  ["2:76"]="$SPR/staff_panel.png"
  ["2:86"]="$SPR/badge_status.png"
)

for k in "${!MAP[@]}"; do
  url=$(get "$k"); out="${MAP[$k]}"
  if [ -z "$url" ]; then echo "SKIP $k"; continue; fi
  echo "GET  $k -> $out"
  curl -s -o "$out" "$url"
done

echo "Done."; ls "$SPR" | wc -l
