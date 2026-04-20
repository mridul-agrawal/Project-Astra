"""
Stage 1 of the devlog timelapse pipeline.

Samples each recording in Recordings/ at 1fps into normalized 256x256 thumbnails
and fingerprints each frame with a perceptual hash. Results land in a SQLite
index at data/devlog_index.sqlite so later stages can diff novel content across
the full history. Re-runs are incremental: only new recordings are processed.

Usage:
    python scripts/devlog_index.py
"""

from __future__ import annotations

import re
import sqlite3
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

import imagehash
from PIL import Image


# === Paths & configuration ===================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent
RECORDINGS_DIR = PROJECT_ROOT / "Recordings"
DATA_DIR = PROJECT_ROOT / "data"
FRAMES_DIR = DATA_DIR / "devlog_frames"
DB_PATH = DATA_DIR / "devlog_index.sqlite"

SAMPLE_FPS = 1
THUMB_SIZE = 256
FILENAME_PATTERN = re.compile(r"devlog_(\d{4}-\d{2}-\d{2})_(\d{2}-\d{2}-\d{2})\.webm$")


# === Entry point =============================================================

def main() -> int:
    ensure_layout()
    connection = open_database()

    recordings = discover_recordings()
    already_indexed = load_indexed_ids(connection)
    pending = [r for r in recordings if r.recording_id not in already_indexed]

    print(f"Found {len(recordings)} recordings, {len(pending)} new to index.")
    for i, recording in enumerate(pending, start=1):
        print(f"  [{i}/{len(pending)}] {recording.recording_id}")
        index_recording(connection, recording)

    connection.close()
    print("Stage 1 complete.")
    return 0


# === Discovery ===============================================================

@dataclass
class Recording:
    recording_id: str
    video_path: Path
    captured_at: datetime

    @property
    def frames_dir(self) -> Path:
        return FRAMES_DIR / self.recording_id


def discover_recordings() -> list[Recording]:
    found: list[Recording] = []
    for video_path in sorted(RECORDINGS_DIR.glob("devlog_*.webm")):
        captured_at = parse_capture_time(video_path.name)
        if captured_at is None:
            print(f"  skip (bad name): {video_path.name}")
            continue
        found.append(Recording(
            recording_id=video_path.stem,
            video_path=video_path,
            captured_at=captured_at,
        ))
    found.sort(key=lambda r: r.captured_at)
    return found


def parse_capture_time(filename: str) -> datetime | None:
    match = FILENAME_PATTERN.search(filename)
    if not match:
        return None
    date_part, time_part = match.groups()
    return datetime.strptime(f"{date_part} {time_part.replace('-', ':')}", "%Y-%m-%d %H:%M:%S")


# === Per-recording indexing ==================================================

def index_recording(connection: sqlite3.Connection, recording: Recording) -> None:
    recording.frames_dir.mkdir(parents=True, exist_ok=True)
    try:
        duration = extract_frames(recording)
    except subprocess.CalledProcessError as err:
        print(f"    skip (ffmpeg failed, exit {err.returncode}): {recording.recording_id}")
        return

    frame_rows = hash_frames(recording)
    if not frame_rows:
        print(f"    skip (no frames produced): {recording.recording_id}")
        return

    write_recording_row(connection, recording, duration, len(frame_rows))
    write_frame_rows(connection, recording.recording_id, frame_rows)
    connection.commit()


def extract_frames(recording: Recording) -> float:
    pad_expr = f"scale={THUMB_SIZE}:{THUMB_SIZE}:force_original_aspect_ratio=decrease," \
               f"pad={THUMB_SIZE}:{THUMB_SIZE}:(ow-iw)/2:(oh-ih)/2:color=black"
    filter_chain = f"fps={SAMPLE_FPS},{pad_expr}"
    output_template = str(recording.frames_dir / "t_%04d.jpg")

    cmd = [
        "ffmpeg", "-v", "error", "-y",
        "-i", str(recording.video_path),
        "-vf", filter_chain,
        "-q:v", "4",
        output_template,
    ]
    subprocess.run(cmd, check=True)
    return probe_duration(recording.video_path)


def probe_duration(video_path: Path) -> float:
    result = subprocess.run(
        ["ffprobe", "-v", "error", "-show_entries", "format=duration",
         "-of", "default=noprint_wrappers=1:nokey=1", str(video_path)],
        check=True, capture_output=True, text=True,
    )
    return float(result.stdout.strip() or 0.0)


@dataclass
class FrameRow:
    t_seconds: int
    phash_hex: str
    thumb_relpath: str


def hash_frames(recording: Recording) -> list[FrameRow]:
    rows: list[FrameRow] = []
    for thumb_path in sorted(recording.frames_dir.glob("t_*.jpg")):
        t_seconds = int(thumb_path.stem.split("_")[1]) - 1  # ffmpeg numbering is 1-indexed
        with Image.open(thumb_path) as image:
            phash = imagehash.phash(image, hash_size=8)
        rows.append(FrameRow(
            t_seconds=t_seconds,
            phash_hex=str(phash),
            thumb_relpath=str(thumb_path.relative_to(DATA_DIR).as_posix()),
        ))
    return rows


# === Database ================================================================

SCHEMA_SQL = """
CREATE TABLE IF NOT EXISTS recordings (
    recording_id TEXT PRIMARY KEY,
    video_path   TEXT NOT NULL,
    captured_at  TEXT NOT NULL,
    duration_s   REAL NOT NULL,
    frame_count  INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS frames (
    recording_id   TEXT NOT NULL,
    t_seconds      INTEGER NOT NULL,
    phash_hex      TEXT NOT NULL,
    thumb_relpath  TEXT NOT NULL,
    PRIMARY KEY (recording_id, t_seconds),
    FOREIGN KEY (recording_id) REFERENCES recordings(recording_id)
);

CREATE INDEX IF NOT EXISTS idx_frames_phash ON frames(phash_hex);
"""


def open_database() -> sqlite3.Connection:
    connection = sqlite3.connect(DB_PATH)
    connection.executescript(SCHEMA_SQL)
    connection.commit()
    return connection


def load_indexed_ids(connection: sqlite3.Connection) -> set[str]:
    rows = connection.execute("SELECT recording_id FROM recordings").fetchall()
    return {row[0] for row in rows}


def write_recording_row(
    connection: sqlite3.Connection,
    recording: Recording,
    duration: float,
    frame_count: int,
) -> None:
    connection.execute(
        "INSERT OR REPLACE INTO recordings "
        "(recording_id, video_path, captured_at, duration_s, frame_count) "
        "VALUES (?, ?, ?, ?, ?)",
        (
            recording.recording_id,
            str(recording.video_path.relative_to(PROJECT_ROOT).as_posix()),
            recording.captured_at.isoformat(),
            duration,
            frame_count,
        ),
    )


def write_frame_rows(
    connection: sqlite3.Connection,
    recording_id: str,
    rows: list[FrameRow],
) -> None:
    connection.executemany(
        "INSERT OR REPLACE INTO frames "
        "(recording_id, t_seconds, phash_hex, thumb_relpath) VALUES (?, ?, ?, ?)",
        [(recording_id, r.t_seconds, r.phash_hex, r.thumb_relpath) for r in rows],
    )


# === Layout ==================================================================

def ensure_layout() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    FRAMES_DIR.mkdir(parents=True, exist_ok=True)


if __name__ == "__main__":
    sys.exit(main())
