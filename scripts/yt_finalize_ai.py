"""
Produce data/candidates_ai.csv — subset of candidates.csv limited to channels
that were visually classified as ai_art in data/channel_classifications.json.

Also writes data/candidates_non_ai.csv for transparency (channels the user
might want to audit my visual calls on).
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
CSV_IN = PROJECT_ROOT / "data" / "candidates.csv"
MANIFEST = PROJECT_ROOT / "data" / "thumbs_manifest.json"
CLASSIFICATIONS = PROJECT_ROOT / "data" / "channel_classifications.json"
CSV_OUT_AI = PROJECT_ROOT / "data" / "candidates_ai.csv"
CSV_OUT_NON = PROJECT_ROOT / "data" / "candidates_non_ai.csv"


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    cls_data = json.loads(CLASSIFICATIONS.read_text(encoding="utf-8"))
    cls = cls_data["classifications"]

    # channel_id -> classification
    channel_to_class: dict[str, str] = {}
    for entry in manifest:
        idx = str(entry["idx"])
        ch_id = entry["channel_id"]
        channel_to_class[ch_id] = cls.get(idx, "unclassified")

    with CSV_IN.open(encoding="utf-8") as f:
        rows = list(csv.DictReader(f))

    ai_rows, non_rows = [], []
    for r in rows:
        c = channel_to_class.get(r["channel_id"], "unclassified")
        r = {**r, "channel_class": c}
        if c == "ai_art":
            ai_rows.append(r)
        else:
            non_rows.append(r)

    fieldnames = list(rows[0].keys()) + ["channel_class"] if rows else []

    for out_path, out_rows, label in [
        (CSV_OUT_AI, ai_rows, "AI-art"),
        (CSV_OUT_NON, non_rows, "non-AI"),
    ]:
        with out_path.open("w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            for r in sorted(out_rows, key=lambda x: int(x["view_count"]), reverse=True):
                writer.writerow(r)
        print(f"[*] {label}: {len(out_rows)} rows → {out_path}")

    # Channel-class tally
    from collections import Counter
    tally = Counter(channel_to_class.values())
    print(f"[*] Channel classification tally: {dict(tally)}")
    ch_ai = sum(1 for r in ai_rows) / max(1, len(rows))
    print(f"[*] AI-art share of candidates: {len(ai_rows)}/{len(rows)} = {ch_ai:.1%}")

    if ai_rows:
        print(f"[*] Top 10 AI-art shorts by views:")
        for r in ai_rows[:10]:
            print(f"    {int(r['view_count']):>11,}  [{r['language']:>5}]  "
                  f"{r['channel_title'][:22]:22}  {r['title'][:70]}")


if __name__ == "__main__":
    main()
