"""
For each enriched video, build a hook-weighted 8-frame grid image + a text
bundle (title, transcript, timing) that is ready to feed into Phase 3 analysis.

Outputs:
    data/analysis_grids/{video_id}.jpg
    data/analysis_bundle/{video_id}.md
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
ENRICHED_DIR = PROJECT_ROOT / "data" / "enriched"
GRIDS_DIR = PROJECT_ROOT / "data" / "analysis_grids"
BUNDLE_DIR = PROJECT_ROOT / "data" / "analysis_bundle"


def pick_timestamps(total: int) -> list[int]:
    """Return 8 frame indices (1-based, matches f_01.jpg...) emphasizing hooks.
    total = number of seconds of frames available."""
    if total <= 8:
        return list(range(1, total + 1))
    # Hook-weighted: heavy on seconds 0-7, then spread across remainder
    fixed = [1, 2, 4, 7]
    tail_start = 7
    tail_n = 4
    step = max(1, (total - tail_start) // tail_n)
    tail = [min(total, tail_start + step * (i + 1)) for i in range(tail_n)]
    out = sorted(set(fixed + tail))
    # pad if set deduped down to <8
    while len(out) < 8 and out[-1] < total:
        out.append(out[-1] + 1)
    return out[:8]


def build_grid(frame_paths: list[tuple[Path, str]], out_path: Path) -> None:
    """4 cols x 2 rows grid. Each tile labeled with its timestamp in the video."""
    cols, rows = 4, 2
    tile_w, tile_h = 540, 304  # ~16:9
    label_h = 30
    canvas = Image.new("RGB", (cols * tile_w, rows * (tile_h + label_h)), (16, 16, 16))
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except Exception:
        font = ImageFont.load_default()
    for i, (path, label) in enumerate(frame_paths[: cols * rows]):
        r, c = divmod(i, cols)
        x = c * tile_w
        y = r * (tile_h + label_h)
        try:
            im = Image.open(path).convert("RGB")
            im = im.resize((tile_w, tile_h), Image.LANCZOS)
        except Exception:
            im = Image.new("RGB", (tile_w, tile_h), (40, 40, 40))
        canvas.paste(im, (x, y))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle([x, y + tile_h, x + tile_w, y + tile_h + label_h], fill=(0, 0, 0))
        draw.text((x + 8, y + tile_h + 5), label, fill=(255, 255, 255), font=font)
    canvas.save(out_path, "JPEG", quality=88)


def build_bundle(video_dir: Path) -> None:
    vid = video_dir.name
    meta = json.loads((video_dir / "meta.json").read_text(encoding="utf-8"))
    transcript_txt = ""
    segments = []
    if (video_dir / "transcript.txt").exists():
        transcript_txt = (video_dir / "transcript.txt").read_text(encoding="utf-8").strip()
    if (video_dir / "transcript.json").exists():
        segments = json.loads((video_dir / "transcript.json").read_text(encoding="utf-8"))
    source = (video_dir / "source.txt").read_text(encoding="utf-8").strip() if (video_dir / "source.txt").exists() else ""

    frame_files = sorted((video_dir / "frames").glob("f_*.jpg"))
    if not frame_files:
        print(f"    {vid}: no frames; skipping")
        return

    total_seconds = len(frame_files)
    picks = pick_timestamps(total_seconds)
    picked_paths: list[tuple[Path, str]] = []
    for t in picks:
        frame_path = video_dir / "frames" / f"f_{t:02d}.jpg"
        if frame_path.exists():
            picked_paths.append((frame_path, f"{t}s"))

    GRIDS_DIR.mkdir(parents=True, exist_ok=True)
    BUNDLE_DIR.mkdir(parents=True, exist_ok=True)
    build_grid(picked_paths, GRIDS_DIR / f"{vid}.jpg")

    # Word/sec pacing if segments have timestamps
    wps = None
    if segments:
        total_time = max(s["end"] for s in segments) - min(s["start"] for s in segments)
        total_words = sum(len(s["text"].split()) for s in segments)
        if total_time > 0:
            wps = total_words / total_time

    bundle = []
    bundle.append(f"# {vid}")
    bundle.append(f"**Title:** {meta['title']}")
    bundle.append(f"**Channel:** {meta['channel_title']}")
    bundle.append(f"**URL:** {meta['url']}")
    bundle.append(f"**Views:** {int(meta['view_count']):,}  |  **Likes:** {int(meta['like_count']):,}  |  **Duration:** {meta['duration_seconds']}s  |  **Language:** {meta['language']}")
    bundle.append(f"**Description:** {meta.get('description', '')[:300]}")
    bundle.append(f"**Transcript source:** {source}  |  **wps:** {wps:.2f}" if wps else f"**Transcript source:** {source}")
    bundle.append("")
    bundle.append("## Transcript")
    bundle.append(transcript_txt or "(no transcript)")
    bundle.append("")
    bundle.append("## Segment timing (first 6)")
    for s in segments[:6]:
        bundle.append(f"  [{s['start']:>5.1f}s-{s['end']:>5.1f}s]  {s['text']}")
    (BUNDLE_DIR / f"{vid}.md").write_text("\n".join(bundle), encoding="utf-8")


def main() -> None:
    dirs = [d for d in ENRICHED_DIR.iterdir() if d.is_dir() and (d / "meta.json").exists()]
    print(f"[*] Building grid + bundle for {len(dirs)} videos")
    for d in sorted(dirs):
        build_bundle(d)
    print(f"[*] Grids → {GRIDS_DIR}")
    print(f"[*] Bundles → {BUNDLE_DIR}")


if __name__ == "__main__":
    main()
