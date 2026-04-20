"""
Stage 2 of the devlog timelapse pipeline.

Reads frames indexed by Stage 1, walks them in chronological order, and marks
each frame as novel if its perceptual hash is far (Hamming distance > threshold)
from every frame in earlier recordings. Consecutive novel frames within a single
recording collapse into segments, which is what Stage 3 will cluster.

Usage:
    python scripts/devlog_detect_novel.py [--threshold 12]
"""

from __future__ import annotations

import argparse
import sqlite3
import sys
from dataclasses import dataclass
from pathlib import Path


# === Paths & configuration ===================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent
DB_PATH = PROJECT_ROOT / "data" / "devlog_index.sqlite"

DEFAULT_HAMMING_THRESHOLD = 12        # out of 64 bits; larger = stricter novelty
MIN_SEGMENT_FRAMES = 1                # drop < N consecutive novel frames as noise


# === Entry point =============================================================

def main() -> int:
    args = parse_args()

    connection = sqlite3.connect(DB_PATH)
    ensure_segments_schema(connection)

    frames = load_frames_chronologically(connection)
    print(f"Loaded {len(frames)} frames across "
          f"{len({f.recording_id for f in frames})} recordings.")

    novel_flags, distances = compute_novelty(frames, args.threshold)
    segments = collapse_into_segments(frames, novel_flags, distances)

    print(f"Detected {sum(novel_flags)} novel frames -> {len(segments)} segments.")
    write_segments(connection, segments)
    connection.close()
    print("Stage 2 complete.")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--threshold", type=int, default=DEFAULT_HAMMING_THRESHOLD,
                        help="Hamming distance above which a frame is considered novel.")
    return parser.parse_args()


# === Data model ==============================================================

@dataclass
class Frame:
    recording_id: str
    captured_at: str     # ISO timestamp of recording, used for ordering
    t_seconds: int
    phash_hex: str
    thumb_relpath: str

    @property
    def phash_int(self) -> int:
        return int(self.phash_hex, 16)


@dataclass
class Segment:
    recording_id: str
    start_t: int
    end_t: int
    min_hamming: int                 # smallest distance to prior history seen in segment
    representative_t: int            # t with largest min-distance (most novel)
    representative_phash: str
    representative_thumb: str


# === Frame loading ===========================================================

def load_frames_chronologically(connection: sqlite3.Connection) -> list[Frame]:
    rows = connection.execute(
        "SELECT f.recording_id, r.captured_at, f.t_seconds, f.phash_hex, f.thumb_relpath "
        "FROM frames f "
        "JOIN recordings r ON r.recording_id = f.recording_id "
        "ORDER BY r.captured_at ASC, f.t_seconds ASC"
    ).fetchall()
    return [Frame(*row) for row in rows]


# === Novelty detection =======================================================

def compute_novelty(frames: list[Frame], threshold: int) -> tuple[list[bool], list[int]]:
    """Return per-frame novelty flags + distances-to-nearest-prior.

    A frame is novel if no earlier-recording frame is within `threshold` Hamming
    distance. Frames from the same recording never count as "earlier" — we're
    looking for cross-recording progress, not intra-recording variation.
    """
    flags: list[bool] = []
    distances: list[int] = []
    flat_prior_hashes: list[int] = []

    current_recording: str | None = None
    pending_hashes: list[int] = []

    for frame in frames:
        if frame.recording_id != current_recording:
            if current_recording is not None:
                flat_prior_hashes.extend(pending_hashes)
            current_recording = frame.recording_id
            pending_hashes = []

        min_distance = nearest_hamming(frame.phash_int, flat_prior_hashes)
        flags.append(min_distance > threshold)
        distances.append(min_distance)
        pending_hashes.append(frame.phash_int)

    return flags, distances


def nearest_hamming(query: int, corpus: list[int]) -> int:
    if not corpus:
        return 64  # nothing prior → maximally novel
    best = 64
    for other in corpus:
        distance = (query ^ other).bit_count()
        if distance < best:
            best = distance
            if best == 0:
                return 0
    return best


# === Segment assembly ========================================================

def collapse_into_segments(
    frames: list[Frame],
    flags: list[bool],
    distances: list[int],
) -> list[Segment]:
    """Group consecutive novel frames within the same recording into segments."""
    segments: list[Segment] = []
    run: list[tuple[Frame, int]] = []
    run_recording: str | None = None

    for frame, is_novel, distance in zip(frames, flags, distances):
        if is_novel and frame.recording_id == run_recording:
            run.append((frame, distance))
        elif is_novel:
            flush_run(run, segments)
            run = [(frame, distance)]
            run_recording = frame.recording_id
        else:
            flush_run(run, segments)
            run = []
            run_recording = None

    flush_run(run, segments)
    return [s for s in segments if (s.end_t - s.start_t + 1) >= MIN_SEGMENT_FRAMES]


def flush_run(run: list[tuple[Frame, int]], segments: list[Segment]) -> None:
    """Pick the most-novel frame (largest Hamming distance to prior) as representative."""
    if not run:
        return
    recording_id = run[0][0].recording_id
    start_t = run[0][0].t_seconds
    end_t = run[-1][0].t_seconds
    peak_frame, peak_distance = max(run, key=lambda pair: pair[1])
    segments.append(Segment(
        recording_id=recording_id,
        start_t=start_t,
        end_t=end_t,
        min_hamming=peak_distance,
        representative_t=peak_frame.t_seconds,
        representative_phash=peak_frame.phash_hex,
        representative_thumb=peak_frame.thumb_relpath,
    ))


# === Database ================================================================

SCHEMA_SQL = """
CREATE TABLE IF NOT EXISTS segments (
    segment_id           INTEGER PRIMARY KEY AUTOINCREMENT,
    recording_id         TEXT NOT NULL,
    start_t              INTEGER NOT NULL,
    end_t                INTEGER NOT NULL,
    min_hamming          INTEGER NOT NULL,
    representative_t     INTEGER NOT NULL,
    representative_phash TEXT NOT NULL,
    representative_thumb TEXT NOT NULL,
    FOREIGN KEY (recording_id) REFERENCES recordings(recording_id)
);
"""


def ensure_segments_schema(connection: sqlite3.Connection) -> None:
    connection.executescript(SCHEMA_SQL)
    connection.execute("DELETE FROM segments")
    connection.commit()


def write_segments(connection: sqlite3.Connection, segments: list[Segment]) -> None:
    connection.executemany(
        "INSERT INTO segments "
        "(recording_id, start_t, end_t, min_hamming, representative_t, "
        " representative_phash, representative_thumb) "
        "VALUES (?, ?, ?, ?, ?, ?, ?)",
        [(s.recording_id, s.start_t, s.end_t, s.min_hamming, s.representative_t,
          s.representative_phash, s.representative_thumb) for s in segments],
    )
    connection.commit()


if __name__ == "__main__":
    sys.exit(main())
