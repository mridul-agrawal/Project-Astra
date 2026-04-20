"""
Stage 4 of the devlog timelapse pipeline.

Reads cluster labels produced by the in-session review step and assembles a
silent, Day-N-overlaid timelapse from the source recordings. Picks the first
occurrence of each cluster (plus any iteration explicitly marked keep), paces
each clip by importance, and hard-caps total runtime at 120 seconds by applying
a global speed-up if the natural length overshoots.

Usage:
    python scripts/devlog_assemble.py [--max-seconds 120] [--labels data/devlog_labels.json]
"""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path


# === Paths & configuration ===================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = PROJECT_ROOT / "data"
DB_PATH = DATA_DIR / "devlog_index.sqlite"
LABELS_PATH = DATA_DIR / "devlog_labels.json"
WORK_DIR = DATA_DIR / "devlog_build"
OUTPUT_DIR = PROJECT_ROOT / "marketing"
OUTPUT_PATH = OUTPUT_DIR / "devlog_timelapse_v02.mp4"

TARGET_FPS = 30
OUTPUT_RESOLUTION = (1920, 1080)
DEFAULT_MAX_SECONDS = 120.0
# drawtext requires an explicit fontfile on Windows (no fontconfig fallback).
# Forward slashes + escaped colon is the incantation ffmpeg's filter parser needs.
DRAWTEXT_FONT = "C\\:/Windows/Fonts/arialbd.ttf"

# Importance weight per clip (relative; actual seconds computed to fit the budget).
IMPORTANCE_WEIGHT = {5: 2.5, 4: 1.8, 3: 1.3, 2: 1.0, 1: 0.6}
# Floor / ceiling on any single clip so the pacing stays timelapse-feel.
MIN_CLIP_SECONDS = 0.4
MAX_CLIP_SECONDS = 2.5


# === Entry point =============================================================

def main() -> int:
    args = parse_args()

    labels = load_labels(args.labels_path)
    print(f"Loaded {len(labels)} cluster labels.")

    connection = sqlite3.connect(DB_PATH)
    picks = select_clips(connection, labels)
    connection.close()
    print(f"Selected {len(picks)} clips for the timelapse.")

    day_assignments = assign_day_numbers(picks)
    clip_seconds = budget_clip_durations(picks, args.max_seconds)
    total = sum(clip_seconds)
    print(f"Rendering {len(picks)} clips; total {total:.1f}s "
          f"(range {min(clip_seconds):.2f}s - {max(clip_seconds):.2f}s).")

    reset_work_dir()
    clip_paths = render_clips(picks, day_assignments, clip_seconds)
    concat_clips(clip_paths, OUTPUT_PATH)

    print(f"Wrote {OUTPUT_PATH}")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--max-seconds", type=float, default=DEFAULT_MAX_SECONDS)
    parser.add_argument("--labels", dest="labels_path", type=Path, default=LABELS_PATH)
    return parser.parse_args()


# === Data model ==============================================================

@dataclass
class Label:
    cluster_id: int
    text: str
    category: str
    importance: int
    keep_as: str   # "first" | "iteration" | "skip"


@dataclass
class Pick:
    cluster_id: int
    segment_id: int
    recording_id: str
    video_relpath: str
    captured_at: datetime
    start_t: int
    end_t: int
    label: Label

    @property
    def weight(self) -> float:
        return IMPORTANCE_WEIGHT.get(self.label.importance, 1.0)

    @property
    def source_duration(self) -> float:
        # +1 because start_t and end_t are inclusive second indices
        return max(1.0, float(self.end_t - self.start_t + 1))


# === Label loading ===========================================================

def load_labels(path: Path) -> dict[int, Label]:
    if not path.exists():
        raise SystemExit(
            f"Missing {path}. Run Stage 3 and the in-session review step first.")

    raw = json.loads(path.read_text())
    labels: dict[int, Label] = {}
    for entry in raw:
        labels[entry["cluster_id"]] = Label(
            cluster_id=entry["cluster_id"],
            text=entry.get("label", ""),
            category=entry.get("category", ""),
            importance=int(entry.get("importance", 3)),
            keep_as=entry.get("keep_as", "first"),
        )
    return labels


# === Clip selection ==========================================================

def select_clips(connection: sqlite3.Connection, labels: dict[int, Label]) -> list[Pick]:
    """Include every segment whose cluster isn't skipped, sorted chronologically.

    This is evolution mode: repeated appearances of the same feature become
    consecutive-ish clips so you actually see a Trade Screen iterate from v1
    to v2 to v3 rather than seeing each thing exactly once.
    """
    rows = connection.execute(
        "SELECT cm.cluster_id, s.segment_id, s.recording_id, r.video_path, "
        "       r.captured_at, s.start_t, s.end_t "
        "FROM segments s "
        "JOIN cluster_members cm ON cm.segment_id = s.segment_id "
        "JOIN recordings r ON r.recording_id = s.recording_id "
        "ORDER BY r.captured_at ASC, s.start_t ASC"
    ).fetchall()

    picks: list[Pick] = []
    for row in rows:
        cluster_id, segment_id, recording_id, video_path, captured_at_iso, start_t, end_t = row
        label = labels.get(cluster_id, unlabeled_placeholder(cluster_id))
        if label.keep_as == "skip":
            continue
        picks.append(Pick(
            cluster_id=cluster_id,
            segment_id=segment_id,
            recording_id=recording_id,
            video_relpath=video_path,
            captured_at=datetime.fromisoformat(captured_at_iso),
            start_t=start_t,
            end_t=end_t,
            label=label,
        ))
    return picks


def unlabeled_placeholder(cluster_id: int) -> Label:
    return Label(
        cluster_id=cluster_id,
        text=f"Cluster {cluster_id}",
        category="unlabeled",
        importance=2,
        keep_as="first",
    )


# === Day numbering ===========================================================

def assign_day_numbers(picks: list[Pick]) -> dict[str, int]:
    """Map each calendar date seen to a sequential Day N, starting from 1."""
    unique_dates = sorted({p.captured_at.date() for p in picks})
    return {date.isoformat(): i + 1 for i, date in enumerate(unique_dates)}


# === Pacing ==================================================================

def budget_clip_durations(picks: list[Pick], max_seconds: float) -> list[float]:
    """Divide the budget across clips proportionally to importance weights.

    Enforces MIN/MAX per clip so the overall motion stays timelapse-feel and no
    single clip dominates. If the weighted sum overshoots, scale down; if it
    undershoots, the total is just shorter than max — which is fine.
    """
    if not picks:
        return []

    weights = [p.weight for p in picks]
    raw_share = max_seconds / sum(weights)
    durations = [clamp(w * raw_share, MIN_CLIP_SECONDS, MAX_CLIP_SECONDS) for w in weights]

    total = sum(durations)
    if total > max_seconds:
        scale = max_seconds / total
        durations = [d * scale for d in durations]
    return durations


def clamp(value: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, value))


# === Rendering ===============================================================

def reset_work_dir() -> None:
    if WORK_DIR.exists():
        shutil.rmtree(WORK_DIR)
    WORK_DIR.mkdir(parents=True)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def render_clips(
    picks: list[Pick],
    day_assignments: dict[str, int],
    clip_seconds: list[float],
) -> list[Path]:
    clip_paths: list[Path] = []
    for i, (pick, seconds) in enumerate(zip(picks, clip_seconds)):
        day_number = day_assignments[pick.captured_at.date().isoformat()]
        clip_path = WORK_DIR / f"clip_{i:04d}.mp4"
        render_single_clip(pick, day_number, seconds, clip_path)
        clip_paths.append(clip_path)
    return clip_paths


def render_single_clip(
    pick: Pick,
    day_number: int,
    target_seconds: float,
    output: Path,
) -> None:
    source_seconds = min(pick.source_duration, max(target_seconds, 1.0))
    effective_pts = target_seconds / source_seconds
    label_text = escape_for_drawtext(pick.label.text)
    day_text = f"Day {day_number}"

    filter_graph = (
        f"scale={OUTPUT_RESOLUTION[0]}:{OUTPUT_RESOLUTION[1]}:"
        f"force_original_aspect_ratio=decrease,"
        f"pad={OUTPUT_RESOLUTION[0]}:{OUTPUT_RESOLUTION[1]}:(ow-iw)/2:(oh-ih)/2:color=black,"
        f"setpts={effective_pts:.6f}*PTS,"
        f"fps={TARGET_FPS},"
        f"drawtext=fontfile='{DRAWTEXT_FONT}':text='{day_text}':fontcolor=white:fontsize=56:"
        f"box=1:boxcolor=black@0.55:boxborderw=16:x=40:y=40,"
        f"drawtext=fontfile='{DRAWTEXT_FONT}':text='{label_text}':fontcolor=white:fontsize=40:"
        f"box=1:boxcolor=black@0.55:boxborderw=16:x=40:y=h-th-60"
    )

    source = PROJECT_ROOT / pick.video_relpath
    cmd = [
        "ffmpeg", "-v", "error", "-y",
        "-ss", str(pick.start_t),
        "-t", f"{source_seconds:.3f}",
        "-i", str(source),
        "-an",
        "-vf", filter_graph,
        "-r", str(TARGET_FPS),
        "-c:v", "libx264", "-preset", "medium", "-crf", "20",
        "-pix_fmt", "yuv420p",
        str(output),
    ]
    subprocess.run(cmd, check=True)


def escape_for_drawtext(text: str) -> str:
    return (text
            .replace("\\", "\\\\")
            .replace(":", r"\:")
            .replace("'", r"\'"))


# === Concat ==================================================================

def concat_clips(clip_paths: list[Path], output: Path) -> None:
    list_file = WORK_DIR / "concat.txt"
    list_file.write_text(
        "\n".join(f"file '{p.as_posix()}'" for p in clip_paths) + "\n"
    )

    cmd = [
        "ffmpeg", "-v", "error", "-y",
        "-f", "concat", "-safe", "0",
        "-i", str(list_file),
        "-c", "copy",
        str(output),
    ]
    subprocess.run(cmd, check=True)


if __name__ == "__main__":
    sys.exit(main())
