"""
Tier 2 — Medium-depth enrichment for videos in data/candidates_ai.csv.

For each video, produces data/enriched/{video_id}/:
    meta.json               # csv row + duration + any extras
    video.mp4               # downloaded video
    audio.wav               # 16kHz mono
    frames/f_{NN}.jpg       # one keyframe per second
    captions.{lang}.vtt     # YouTube auto-captions (if any)
    transcript.txt          # plain text (from captions if present, else whisper)
    transcript.json         # segments with start/end timestamps
    source.txt              # "auto_hi" | "auto_en" | "whisper" | "none"

Usage:
    python scripts/yt_enrich.py                    # enrich all rows
    python scripts/yt_enrich.py --video-id ABC123  # enrich single video (test)
    python scripts/yt_enrich.py --limit 5          # first N rows only
"""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
CSV_IN = PROJECT_ROOT / "data" / "candidates_ai.csv"
ENRICHED_DIR = PROJECT_ROOT / "data" / "enriched"


def run(cmd: list[str], cwd: Path | None = None, check: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(
        cmd,
        cwd=str(cwd) if cwd else None,
        check=check,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )


def sub_lang_for(row_lang: str) -> str:
    """Map row['language'] to a single yt-dlp sub-lang. Keeps to one call to avoid 429s."""
    lo = row_lang.lower()
    if lo.startswith("hi"):
        return "hi"
    if lo.startswith("en"):
        return "en"
    # Non hi/en (te/ta/or/zxx): try original language caption, fall back to English
    return f"{lo},en"


def download_video(video_id: str, out_dir: Path, row_lang: str = "hi") -> tuple[Path | None, list[Path]]:
    """Two-stage download: video first (cannot be aborted by caption 429s),
    then captions best-effort in a separate yt-dlp invocation."""
    url = f"https://www.youtube.com/shorts/{video_id}"

    # Stage 1: video only. If this fails, the whole enrichment for this ID fails.
    subprocess.run(
        [
            "yt-dlp",
            "--no-warnings",
            "-f", "mp4/bestvideo[ext=mp4]+bestaudio[ext=m4a]/best",
            "--merge-output-format", "mp4",
            "-o", "%(id)s.%(ext)s",
            "-P", str(out_dir),
            url,
        ],
        capture_output=True, text=True, encoding="utf-8", errors="replace",
    )
    video_path = out_dir / f"{video_id}.mp4"
    if not video_path.exists():
        candidates = list(out_dir.glob(f"{video_id}.*"))
        video_path = next((p for p in candidates if p.suffix.lower() == ".mp4"), None)
    if video_path is None or not video_path.exists():
        return None, []

    # Stage 2: captions only (skip video download). Caption 429s are tolerated.
    subprocess.run(
        [
            "yt-dlp",
            "--no-warnings",
            "--skip-download",
            "--write-auto-subs",
            "--sub-langs", sub_lang_for(row_lang),
            "--sub-format", "vtt",
            "-o", "%(id)s.%(ext)s",
            "-P", str(out_dir),
            url,
        ],
        capture_output=True, text=True, encoding="utf-8", errors="replace",
    )
    caption_paths = sorted(out_dir.glob(f"{video_id}*.vtt"))
    return video_path, caption_paths


def extract_frames(video_path: Path, frames_dir: Path, fps: int = 1) -> int:
    """Extract one keyframe per second (or per fps). Returns number of frames."""
    frames_dir.mkdir(parents=True, exist_ok=True)
    cmd = [
        "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
        "-i", str(video_path),
        "-vf", f"fps={fps}",
        "-q:v", "3",
        str(frames_dir / "f_%02d.jpg"),
    ]
    run(cmd)
    return len(list(frames_dir.glob("f_*.jpg")))


def extract_audio(video_path: Path, audio_path: Path) -> None:
    cmd = [
        "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
        "-i", str(video_path),
        "-ac", "1", "-ar", "16000",
        "-f", "wav",
        str(audio_path),
    ]
    run(cmd)


def parse_vtt(vtt_path: Path) -> tuple[str, list[dict]]:
    """VTT parser that collapses YouTube's rolling auto-captions.

    YouTube auto-caption cues are 'rolling' — each cue often restates the last
    few words of the previous cue before extending. Naive concatenation triplicates
    text. This parser strips each new cue's prefix overlap with the accumulated
    transcript and only keeps the genuinely new tail.
    """
    import re
    raw = []
    lines = vtt_path.read_text(encoding="utf-8", errors="replace").splitlines()
    i = 0
    ts_re = re.compile(r"(\d+):(\d+):(\d+)\.(\d+)\s+-->\s+(\d+):(\d+):(\d+)\.(\d+)")
    while i < len(lines):
        m = ts_re.search(lines[i])
        if m:
            h1, m1, s1, ms1, h2, m2, s2, ms2 = map(int, m.groups())
            start = h1 * 3600 + m1 * 60 + s1 + ms1 / 1000
            end = h2 * 3600 + m2 * 60 + s2 + ms2 / 1000
            text_parts = []
            i += 1
            while i < len(lines) and lines[i].strip():
                cleaned = re.sub(r"<[^>]+>", "", lines[i])
                cleaned = re.sub(r"&[a-z]+;", " ", cleaned)
                cleaned = cleaned.strip()
                if cleaned:
                    text_parts.append(cleaned)
                i += 1
            text = " ".join(text_parts).strip()
            if text:
                raw.append({"start": start, "end": end, "text": text})
        else:
            i += 1

    # Dedupe rolling captions: each cue's new contribution is the tail that
    # doesn't overlap with what's already in `accumulated`.
    accumulated = ""
    segments = []
    for cue in raw:
        text = cue["text"]
        if not accumulated:
            accumulated = text
            segments.append({**cue, "text": text})
            continue
        # Find longest suffix of `accumulated` that is also a prefix of `text`.
        max_k = min(len(accumulated), len(text))
        overlap = 0
        for k in range(max_k, 0, -1):
            if accumulated.endswith(text[:k]):
                overlap = k
                break
        new_tail = text[overlap:].strip()
        if new_tail:
            accumulated = (accumulated + " " + new_tail).strip()
            segments.append({**cue, "text": new_tail})
    return accumulated.strip(), segments


_WHISPER_MODEL = None


def whisper_transcribe(audio_path: Path) -> tuple[str, list[dict]]:
    """Fallback: transcribe audio via faster-whisper (base multilingual)."""
    global _WHISPER_MODEL
    from faster_whisper import WhisperModel

    if _WHISPER_MODEL is None:
        print("    [whisper] loading base model (first run downloads ~140MB)")
        _WHISPER_MODEL = WhisperModel("base", device="cpu", compute_type="int8")

    segments_iter, info = _WHISPER_MODEL.transcribe(
        str(audio_path),
        beam_size=1,
        language=None,  # auto-detect
        vad_filter=True,
    )
    segments = []
    for seg in segments_iter:
        segments.append({"start": seg.start, "end": seg.end, "text": seg.text.strip()})
    plain = " ".join(s["text"] for s in segments)
    return plain, segments


def enrich_one(row: dict, force: bool = False) -> dict:
    video_id = row["video_id"]
    out_dir = ENRICHED_DIR / video_id
    out_dir.mkdir(parents=True, exist_ok=True)

    (out_dir / "meta.json").write_text(json.dumps(row, ensure_ascii=False, indent=2), encoding="utf-8")

    result = {"video_id": video_id, "steps": {}}

    video_path = out_dir / f"{video_id}.mp4"
    caption_paths = sorted(out_dir.glob(f"{video_id}*.vtt"))
    if force or not video_path.exists():
        video_path, caption_paths = download_video(video_id, out_dir, row.get("language", "hi"))
        if video_path is None or not video_path.exists():
            result["steps"]["download"] = "FAILED"
            return result
    result["steps"]["download"] = "ok"
    result["video_path"] = str(video_path.relative_to(PROJECT_ROOT))
    result["caption_files"] = [str(p.relative_to(PROJECT_ROOT)) for p in caption_paths]

    # Frames
    frames_dir = out_dir / "frames"
    existing = list(frames_dir.glob("f_*.jpg"))
    if force or not existing:
        n = extract_frames(video_path, frames_dir, fps=1)
        result["steps"]["frames"] = f"ok ({n})"
    else:
        result["steps"]["frames"] = f"cached ({len(existing)})"

    # Audio
    audio_path = out_dir / "audio.wav"
    if force or not audio_path.exists():
        extract_audio(video_path, audio_path)
    result["steps"]["audio"] = "ok"

    # Transcript — prefer captions
    transcript_path = out_dir / "transcript.txt"
    transcript_json = out_dir / "transcript.json"
    source_path = out_dir / "source.txt"
    if not force and transcript_path.exists() and source_path.exists():
        result["steps"]["transcript"] = f"cached ({source_path.read_text().strip()})"
    else:
        plain, segments, source = "", [], "none"
        for lang in ("hi", "en", "en-IN", "en-US"):
            vtt = next((p for p in caption_paths if f".{lang}." in p.name), None)
            if vtt:
                plain, segments = parse_vtt(vtt)
                if plain.strip():
                    source = f"auto_{lang}"
                    break
        if not plain.strip():
            try:
                plain, segments = whisper_transcribe(audio_path)
                source = "whisper"
            except Exception as e:
                result["steps"]["transcript"] = f"FAILED whisper: {e}"
                return result
        transcript_path.write_text(plain, encoding="utf-8")
        transcript_json.write_text(json.dumps(segments, ensure_ascii=False, indent=2), encoding="utf-8")
        source_path.write_text(source, encoding="utf-8")
        result["steps"]["transcript"] = f"ok ({source}, {len(plain)} chars, {len(segments)} seg)"

    return result


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--video-id", help="enrich just this one ID (for testing)")
    ap.add_argument("--limit", type=int, help="max N rows")
    ap.add_argument("--force", action="store_true", help="re-run even if cached")
    args = ap.parse_args()

    with CSV_IN.open(encoding="utf-8") as f:
        rows = list(csv.DictReader(f))

    if args.video_id:
        rows = [r for r in rows if r["video_id"] == args.video_id]
        if not rows:
            sys.exit(f"no row with video_id={args.video_id}")
    elif args.limit:
        rows = rows[: args.limit]

    ENRICHED_DIR.mkdir(parents=True, exist_ok=True)
    print(f"[*] Enriching {len(rows)} videos")

    import time
    summary = []
    for i, row in enumerate(rows, 1):
        print(f"[{i:>3}/{len(rows)}] {row['video_id']}  [{row['language']:>5}]  {row['title'][:55]}")
        try:
            info = enrich_one(row, force=args.force)
            for step, status in info["steps"].items():
                print(f"             {step}: {status}")
            summary.append({"video_id": row["video_id"], **info["steps"]})
        except Exception as e:
            print(f"             ERROR: {e}")
            summary.append({"video_id": row["video_id"], "error": str(e)})
        time.sleep(1.2)  # pace requests to avoid YouTube 429s

    summary_path = PROJECT_ROOT / "data" / "enrichment_summary.json"
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[*] Summary → {summary_path}")


if __name__ == "__main__":
    main()
