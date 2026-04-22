#!/usr/bin/env bash
# War's Ledger Figma exports (Variant A · warm cream parchment).
# Parchment sheet carries a big drop-shadow + foxing gradients that inflate
# its render bounds; fetch via REST with use_absolute_bounds=true so the PNG
# is cropped to the 1680×952 sheet geometry (same gotcha the Inventory
# Popup / Supply Convoy / Combat Forecast scripts document).
set -euo pipefail
cd "$(dirname "$0")/.."

J=.secrets/figma_warledger_urls.json
SPR=Assets/UI/WarLedger/Sprites
mkdir -p "$SPR"

get() { python -c "import json; d=json.load(open('$J')); print(d['images'].get('$1') or '')"; }

declare -A MAP=(
  ["1:4"]="$SPR/parchment_sheet.png"
)

for k in "${!MAP[@]}"; do
  url=$(get "$k"); out="${MAP[$k]}"
  if [ -z "$url" ]; then echo "SKIP $k"; continue; fi
  echo "GET  $k -> $out"
  curl -s -o "$out" "$url"
done

ls "$SPR"
