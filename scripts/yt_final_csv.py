"""
Final Phase 4 deliverable: data/analysis_phase3.csv

Merges metadata (candidates_true_ai.csv) + visual/script analysis (analysis_phase3.json)
+ pacing stats (computed from enriched bundles). Columns match the user's brief plus
practical extensions.
"""

from __future__ import annotations

import csv
import json
import sys
from pathlib import Path


try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
CSV_META = PROJECT_ROOT / "data" / "candidates_true_ai.csv"
ANALYSIS_JSON = PROJECT_ROOT / "data" / "analysis_phase3.json"
ENRICHED = PROJECT_ROOT / "data" / "enriched"
CSV_OUT = PROJECT_ROOT / "data" / "analysis_phase3.csv"


def pacing_for(video_id: str) -> tuple[str, str]:
    """Return (wps_str, transcript_source) from enriched data."""
    vd = ENRICHED / video_id
    source_file = vd / "source.txt"
    segs_file = vd / "transcript.json"
    source = source_file.read_text(encoding="utf-8").strip() if source_file.exists() else ""
    if segs_file.exists():
        segs = json.loads(segs_file.read_text(encoding="utf-8"))
        if segs:
            total_time = max(s["end"] for s in segs) - min(s["start"] for s in segs)
            total_words = sum(len(s["text"].split()) for s in segs)
            if total_time > 0:
                return f"{total_words / total_time:.2f}", source
    return "", source


def main() -> None:
    with CSV_META.open(encoding="utf-8") as f:
        meta_rows = {r["video_id"]: r for r in csv.DictReader(f)}

    analysis = {v["video_id"]: v for v in json.loads(ANALYSIS_JSON.read_text(encoding="utf-8"))["videos"]}

    columns = [
        "rank",
        "video_id",
        "title",
        "url",
        "channel_title",
        "view_count",
        "like_count",
        "language",
        "duration_seconds",
        "production_style",
        "core_topic",
        "opening_visual_hook",
        "opening_script_hook_original",
        "opening_script_hook_en",
        "art_style",
        "motion_type",
        "pacing_wps",
        "transcript_source",
        "vo_notes",
        "audio_notes",
        "on_screen_text",
        "viral_element_primary",
        "viral_element_secondary",
    ]

    # Sort by views desc
    sorted_rows = sorted(meta_rows.values(), key=lambda r: int(r["view_count"]), reverse=True)

    with CSV_OUT.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=columns)
        writer.writeheader()
        for i, m in enumerate(sorted_rows, start=1):
            a = analysis.get(m["video_id"], {})
            wps, source = pacing_for(m["video_id"])
            row = {
                "rank": i,
                "video_id": m["video_id"],
                "title": m["title"],
                "url": m["url"],
                "channel_title": m["channel_title"],
                "view_count": m["view_count"],
                "like_count": m["like_count"],
                "language": m["language"],
                "duration_seconds": m["duration_seconds"],
                "production_style": m.get("production_style", ""),
                "core_topic": a.get("core_topic", ""),
                "opening_visual_hook": a.get("opening_visual_hook", ""),
                "opening_script_hook_original": a.get("opening_script_hook_original", ""),
                "opening_script_hook_en": a.get("opening_script_hook_en", ""),
                "art_style": a.get("art_style", ""),
                "motion_type": a.get("motion_type", ""),
                "pacing_wps": wps,
                "transcript_source": source,
                "vo_notes": a.get("vo_notes", ""),
                "audio_notes": a.get("audio_notes", ""),
                "on_screen_text": a.get("on_screen_text", ""),
                "viral_element_primary": a.get("viral_element_primary", ""),
                "viral_element_secondary": a.get("viral_element_secondary", ""),
            }
            writer.writerow(row)

    print(f"[*] Wrote {len(sorted_rows)} rows → {CSV_OUT}")
    print(f"[*] Columns: {len(columns)}")


if __name__ == "__main__":
    main()
