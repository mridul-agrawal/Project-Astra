"""
Stage 3 of the devlog timelapse pipeline.

Clusters Stage-2 segments across every recording so "the same thing appearing
many times" collapses into one group. Each cluster gets a representative
thumbnail copied into data/devlog_clusters/ so the next stage (in-session
human/AI review) can label ~30 clusters instead of hundreds of segments.

Usage:
    python scripts/devlog_cluster.py [--cluster-threshold 10]
"""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


# === Paths & configuration ===================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = PROJECT_ROOT / "data"
DB_PATH = DATA_DIR / "devlog_index.sqlite"
CLUSTERS_DIR = DATA_DIR / "devlog_clusters"
MANIFEST_PATH = CLUSTERS_DIR / "manifest.json"

DEFAULT_CLUSTER_THRESHOLD = 10   # Hamming distance for two segments to be "the same thing"


# === Entry point =============================================================

def main() -> int:
    args = parse_args()

    CLUSTERS_DIR.mkdir(parents=True, exist_ok=True)
    connection = sqlite3.connect(DB_PATH)
    ensure_clusters_schema(connection)

    segments = load_segments(connection)
    print(f"Loaded {len(segments)} segments.")

    cluster_members = group_by_single_link(segments, args.cluster_threshold)
    clusters = finalize_clusters(cluster_members, segments)
    print(f"Formed {len(clusters)} clusters.")

    reset_cluster_outputs()
    write_cluster_rows(connection, clusters)
    copy_representative_thumbnails(clusters)
    write_manifest(clusters, cluster_members, segments)

    connection.close()
    print("Stage 3 complete. Review thumbnails in data/devlog_clusters/.")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cluster-threshold", type=int, default=DEFAULT_CLUSTER_THRESHOLD,
                        help="Max Hamming distance for two segments to share a cluster.")
    return parser.parse_args()


# === Data model ==============================================================

@dataclass
class SegmentRow:
    segment_id: int
    recording_id: str
    captured_at: str
    start_t: int
    end_t: int
    min_hamming: int
    representative_phash: str
    representative_thumb: str

    @property
    def phash_int(self) -> int:
        return int(self.representative_phash, 16)

    @property
    def duration(self) -> int:
        return self.end_t - self.start_t + 1


@dataclass
class Cluster:
    cluster_id: int
    member_segment_ids: list[int]
    representative_segment_id: int
    representative_thumb_src: str
    representative_thumb_dst: str
    first_seen_at: str
    size: int


# === Segment loading =========================================================

def load_segments(connection: sqlite3.Connection) -> list[SegmentRow]:
    rows = connection.execute(
        "SELECT s.segment_id, s.recording_id, r.captured_at, s.start_t, s.end_t, "
        "       s.min_hamming, s.representative_phash, s.representative_thumb "
        "FROM segments s "
        "JOIN recordings r ON r.recording_id = s.recording_id "
        "ORDER BY r.captured_at ASC, s.start_t ASC"
    ).fetchall()
    return [SegmentRow(*row) for row in rows]


# === Clustering ==============================================================

def group_by_single_link(segments: list[SegmentRow], threshold: int) -> dict[int, list[int]]:
    """Single-link clustering via union-find on Hamming edges.

    Two segments connect if their representative pHashes are within `threshold`
    bits. Transitively, any chain of near-matches forms one cluster — which is
    what we want for "same UI element seen across multiple recordings".
    """
    parents = list(range(len(segments)))

    def find(x: int) -> int:
        while parents[x] != x:
            parents[x] = parents[parents[x]]
            x = parents[x]
        return x

    def union(a: int, b: int) -> None:
        ra, rb = find(a), find(b)
        if ra != rb:
            parents[rb] = ra

    hashes = [s.phash_int for s in segments]
    for i in range(len(segments)):
        for j in range(i + 1, len(segments)):
            if (hashes[i] ^ hashes[j]).bit_count() <= threshold:
                union(i, j)

    groups: dict[int, list[int]] = defaultdict(list)
    for i, segment in enumerate(segments):
        groups[find(i)].append(segment.segment_id)
    return groups


def finalize_clusters(
    cluster_members: dict[int, list[int]],
    segments: list[SegmentRow],
) -> list[Cluster]:
    by_id = {s.segment_id: s for s in segments}
    clusters: list[Cluster] = []

    # Stable ordering: clusters sorted by the earliest captured_at of any member.
    ordered_roots = sorted(
        cluster_members.keys(),
        key=lambda root: min(by_id[sid].captured_at for sid in cluster_members[root]),
    )

    for cluster_id, root in enumerate(ordered_roots, start=1):
        members = cluster_members[root]
        member_segments = [by_id[sid] for sid in members]
        representative = pick_representative(member_segments)
        clusters.append(Cluster(
            cluster_id=cluster_id,
            member_segment_ids=members,
            representative_segment_id=representative.segment_id,
            representative_thumb_src=representative.representative_thumb,
            representative_thumb_dst=f"devlog_clusters/cluster_{cluster_id:03d}.jpg",
            first_seen_at=min(s.captured_at for s in member_segments),
            size=len(members),
        ))
    return clusters


def pick_representative(members: list[SegmentRow]) -> SegmentRow:
    """Earliest-appearance segment, with highest novelty breaking ties.

    Using the earliest member biases toward "first time this thing existed",
    which is usually the most meaningful clip to show in a progress montage.
    """
    return min(members, key=lambda s: (s.captured_at, -s.min_hamming))


# === Output ==================================================================

def reset_cluster_outputs() -> None:
    for existing in CLUSTERS_DIR.glob("cluster_*.jpg"):
        existing.unlink()


def copy_representative_thumbnails(clusters: list[Cluster]) -> None:
    for cluster in clusters:
        src = DATA_DIR / cluster.representative_thumb_src
        dst = DATA_DIR / cluster.representative_thumb_dst
        shutil.copyfile(src, dst)


def write_manifest(
    clusters: list[Cluster],
    cluster_members: dict[int, list[int]],
    segments: list[SegmentRow],
) -> None:
    by_id = {s.segment_id: s for s in segments}
    payload = []
    for cluster in clusters:
        payload.append({
            "cluster_id": cluster.cluster_id,
            "size": cluster.size,
            "first_seen_at": cluster.first_seen_at,
            "representative_thumb": cluster.representative_thumb_dst,
            "members": [
                {
                    "segment_id": sid,
                    "recording_id": by_id[sid].recording_id,
                    "captured_at": by_id[sid].captured_at,
                    "start_t": by_id[sid].start_t,
                    "end_t": by_id[sid].end_t,
                    "duration": by_id[sid].duration,
                    "min_hamming": by_id[sid].min_hamming,
                }
                for sid in cluster.member_segment_ids
            ],
        })
    MANIFEST_PATH.write_text(json.dumps(payload, indent=2))


# === Database ================================================================

SCHEMA_SQL = """
CREATE TABLE IF NOT EXISTS clusters (
    cluster_id               INTEGER PRIMARY KEY,
    representative_segment_id INTEGER NOT NULL,
    representative_thumb     TEXT NOT NULL,
    first_seen_at            TEXT NOT NULL,
    size                     INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS cluster_members (
    cluster_id INTEGER NOT NULL,
    segment_id INTEGER NOT NULL,
    PRIMARY KEY (cluster_id, segment_id),
    FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id),
    FOREIGN KEY (segment_id) REFERENCES segments(segment_id)
);
"""


def ensure_clusters_schema(connection: sqlite3.Connection) -> None:
    connection.executescript(SCHEMA_SQL)
    connection.execute("DELETE FROM cluster_members")
    connection.execute("DELETE FROM clusters")
    connection.commit()


def write_cluster_rows(connection: sqlite3.Connection, clusters: list[Cluster]) -> None:
    connection.executemany(
        "INSERT INTO clusters "
        "(cluster_id, representative_segment_id, representative_thumb, first_seen_at, size) "
        "VALUES (?, ?, ?, ?, ?)",
        [(c.cluster_id, c.representative_segment_id, c.representative_thumb_dst,
          c.first_seen_at, c.size) for c in clusters],
    )
    connection.executemany(
        "INSERT INTO cluster_members (cluster_id, segment_id) VALUES (?, ?)",
        [(c.cluster_id, sid) for c in clusters for sid in c.member_segment_ids],
    )
    connection.commit()


if __name__ == "__main__":
    sys.exit(main())
