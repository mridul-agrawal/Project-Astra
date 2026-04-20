"""
Download one thumbnail per unique channel from data/candidates.csv.
For each channel, picks its highest-view video and downloads hqdefault.jpg.
Also assembles the thumbs into labeled grid images for efficient visual classification.

No API quota used — thumbnails come from i.ytimg.com directly.
"""

from __future__ import annotations

import csv
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

import requests
from PIL import Image, ImageDraw, ImageFont


try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
CSV_IN = PROJECT_ROOT / "data" / "candidates.csv"
THUMBS_DIR = PROJECT_ROOT / "data" / "thumbs"
GRID_DIR = PROJECT_ROOT / "data" / "grids"
MANIFEST = PROJECT_ROOT / "data" / "thumbs_manifest.json"


def slugify(s: str) -> str:
    s = re.sub(r"[^\w\s-]", "", s, flags=re.UNICODE)
    s = re.sub(r"[\s]+", "_", s).strip("_")
    return s[:60] or "unknown"


def best_per_channel(csv_path: Path) -> list[dict]:
    """Return one row per channel_id — the highest-viewed video for that channel."""
    best: dict[str, dict] = {}
    with csv_path.open(encoding="utf-8") as f:
        for row in csv.DictReader(f):
            cid = row["channel_id"]
            prev = best.get(cid)
            if prev is None or int(row["view_count"]) > int(prev["view_count"]):
                best[cid] = row
    return sorted(best.values(), key=lambda r: int(r["view_count"]), reverse=True)


def fetch_thumb(video_id: str) -> bytes | None:
    for variant in ("maxresdefault", "hqdefault", "mqdefault"):
        url = f"https://i.ytimg.com/vi/{video_id}/{variant}.jpg"
        r = requests.get(url, timeout=15)
        if r.status_code == 200 and len(r.content) > 2000:
            return r.content
    return None


def build_grid(tiles: list[tuple[int, str, Path]], out_path: Path, cols: int = 4) -> None:
    """tiles = [(idx, label, image_path)]. Writes a labeled grid image."""
    tile_w, tile_h = 480, 270
    label_h = 34
    rows = (len(tiles) + cols - 1) // cols
    canvas_w = cols * tile_w
    canvas_h = rows * (tile_h + label_h)
    canvas = Image.new("RGB", (canvas_w, canvas_h), (20, 20, 20))
    try:
        font = ImageFont.truetype("arial.ttf", 16)
    except Exception:
        font = ImageFont.load_default()

    for i, (idx, label, path) in enumerate(tiles):
        r, c = divmod(i, cols)
        x = c * tile_w
        y = r * (tile_h + label_h)
        try:
            im = Image.open(path).convert("RGB")
            im = im.resize((tile_w, tile_h), Image.LANCZOS)
        except Exception:
            im = Image.new("RGB", (tile_w, tile_h), (60, 60, 60))
        canvas.paste(im, (x, y))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle([x, y + tile_h, x + tile_w, y + tile_h + label_h], fill=(0, 0, 0))
        lab = f"#{idx:02d}  {label}"
        if len(lab) > 60:
            lab = lab[:57] + "..."
        draw.text((x + 6, y + tile_h + 6), lab, fill=(255, 255, 255), font=font)
    canvas.save(out_path, "JPEG", quality=85)


def main() -> None:
    THUMBS_DIR.mkdir(parents=True, exist_ok=True)
    GRID_DIR.mkdir(parents=True, exist_ok=True)

    reps = best_per_channel(CSV_IN)
    print(f"[*] Unique channels in candidates.csv: {len(reps)}")

    manifest = []
    tiles: list[tuple[int, str, Path]] = []

    for i, row in enumerate(reps, start=1):
        video_id = row["video_id"]
        channel_title = row["channel_title"]
        slug = slugify(channel_title)
        out_path = THUMBS_DIR / f"{i:03d}_{slug}.jpg"

        if not out_path.exists():
            data = fetch_thumb(video_id)
            if data is None:
                print(f"    #{i:03d} FAILED: {channel_title[:40]}  ({video_id})")
                continue
            out_path.write_bytes(data)

        manifest.append(
            {
                "idx": i,
                "channel_id": row["channel_id"],
                "channel_title": channel_title,
                "video_id": video_id,
                "video_title": row["title"],
                "view_count": int(row["view_count"]),
                "thumb_path": str(out_path.relative_to(PROJECT_ROOT)),
            }
        )
        label = f"{channel_title[:30]}  |  {row['title'][:38]}"
        tiles.append((i, label, out_path))

    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[*] Wrote {len(manifest)} thumbnails → {THUMBS_DIR}")
    print(f"[*] Manifest → {MANIFEST}")

    # Build grids: 12 tiles each (4 cols x 3 rows)
    grid_size = 12
    for g, start in enumerate(range(0, len(tiles), grid_size), start=1):
        chunk = tiles[start : start + grid_size]
        out = GRID_DIR / f"grid_{g:02d}.jpg"
        build_grid(chunk, out, cols=4)
        print(f"    grid_{g:02d}.jpg  → channels #{chunk[0][0]}–#{chunk[-1][0]}")


if __name__ == "__main__":
    main()
