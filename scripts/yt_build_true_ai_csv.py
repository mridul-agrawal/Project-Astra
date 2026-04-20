"""
Build data/candidates_true_ai.csv — the frame-verified AI-only subset.
Also builds data/candidates_ai_mixed.csv (true_ai + mixed_stock_ai, loosest cut).
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
CSV_IN = PROJECT_ROOT / "data" / "candidates_ai.csv"
STYLES_IN = PROJECT_ROOT / "data" / "production_style.json"
CSV_OUT_STRICT = PROJECT_ROOT / "data" / "candidates_true_ai.csv"
CSV_OUT_LOOSE = PROJECT_ROOT / "data" / "candidates_true_ai_plus_mixed.csv"


def main() -> None:
    styles = json.loads(STYLES_IN.read_text(encoding="utf-8"))["classifications"]
    with CSV_IN.open(encoding="utf-8") as f:
        rows = list(csv.DictReader(f))

    strict, loose = [], []
    for r in rows:
        st = styles.get(r["video_id"], {}).get("style", "unclassified")
        r = {**r, "production_style": st,
             "style_notes": styles.get(r["video_id"], {}).get("notes", "")}
        if st == "true_ai":
            strict.append(r)
        if st in ("true_ai", "mixed_stock_ai"):
            loose.append(r)

    strict.sort(key=lambda r: int(r["view_count"]), reverse=True)
    loose.sort(key=lambda r: int(r["view_count"]), reverse=True)

    fieldnames = list(rows[0].keys()) + ["production_style", "style_notes"] if rows else []

    for out_path, out_rows, label in [
        (CSV_OUT_STRICT, strict, "true_ai (strict)"),
        (CSV_OUT_LOOSE, loose, "true_ai + mixed_stock_ai (loose)"),
    ]:
        with out_path.open("w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            for r in out_rows:
                writer.writerow(r)
        print(f"[*] {label}: {len(out_rows)} rows → {out_path}")

    print(f"\n[*] Top 15 true_ai by views:")
    for r in strict[:15]:
        print(f"    {int(r['view_count']):>11,}  [{r['language']:>5}]  {r['channel_title'][:22]:22}  {r['title'][:70]}")


if __name__ == "__main__":
    main()
