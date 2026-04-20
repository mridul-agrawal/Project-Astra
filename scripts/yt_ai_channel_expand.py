"""
Channel-seeded expansion: identify likely-AI-gen mythology channels in the existing raw pool,
then query the YouTube API for each channel's top Shorts (ordered by viewCount).

Outputs data/candidates_ai_raw.json — merged with data/candidates_raw.json downstream.

Heuristic for "likely-AI-gen channel":
  channel name contains one of the brand-style keywords (fact/tales/chronicles/lore/...)
  AND does NOT look like a personal-name channel.

This is imperfect — the final cut is made visually via thumbnail classification.
The goal here is to cast a wide net of plausible candidate channels.
"""

from __future__ import annotations

import json
import re
import sys
import time
from pathlib import Path

import requests


try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
SECRETS_FILE = PROJECT_ROOT / ".secrets" / "youtube.env"
RAW_INPUT = PROJECT_ROOT / "data" / "candidates_raw.json"
OUTPUT_RAW = PROJECT_ROOT / "data" / "candidates_ai_raw.json"
API_BASE = "https://www.googleapis.com/youtube/v3"


# Channel-name keywords that suggest a brand / content channel (often AI-gen).
# Lowercase substring match.
AI_CHANNEL_KEYWORDS = {
    # Content-brand nouns
    "fact", "facts", "gyan", "gyaan", "knowledge", "knwldg",
    "tales", "stories", "story", "kahani", "kahaani", "katha",
    "chronicles", "lore", "myth", "mythos", "mythology", "mytho",
    "beyond", "truth", "truths", "hidden", "secret", "mysteries", "mysticism",
    "hindu", "vedic", "sanatan", "sanatana", "puran", "purana", "puranic",
    "ancient", "divine", "sacred", "holy", "supreme", "celestial",
    "dharma", "dharmik", "dharmic", "spiritual",
    "bhakti", "sadhna", "sadhana", "yoga", "yogi",
    "speaks", "samvaad", "spectrum", "chakra",
    "tips", "dip", "snippets", "shakti", "saga",
    "perfect", "pure", "eternal",
    "realm", "kingdom", "odyssey",
    "trending", "viral",
    "plus", "pro", "official",
    # Devanagari
    "कथा", "कहानी", "तथ्य", "पौराणिक", "सनातन", "रहस्य",
    "ज्ञान", "धर्म", "भक्ति",
}

# Strong signals that a channel is a PERSON — skip these.
PERSONAL_CHANNEL_SIGNALS = {
    "vlog", "vlogs", "vlogger",
    "official vlogs", "life vlogs",
    "ji vlogs", "ke facts",  # single-creator branding with name prefix
    "by arvind", "by akshay", "by sanjot",
}


def load_api_key() -> str:
    for line in SECRETS_FILE.read_text(encoding="utf-8").splitlines():
        if line.strip().startswith("YOUTUBE_API_KEY="):
            return line.split("=", 1)[1].strip()
    sys.exit("YOUTUBE_API_KEY missing")


def looks_like_ai_channel(name: str) -> bool:
    lo = name.lower()
    if any(sig in lo for sig in PERSONAL_CHANNEL_SIGNALS):
        return False
    return any(kw in lo for kw in AI_CHANNEL_KEYWORDS)


def yt_get(path: str, params: dict) -> dict:
    r = requests.get(f"{API_BASE}/{path}", params=params, timeout=30)
    if r.status_code != 200:
        raise RuntimeError(f"{path} {r.status_code}: {r.text[:400]}")
    return r.json()


def channel_top_shorts(api_key: str, channel_id: str, max_n: int = 25) -> list[str]:
    """Return top video IDs for a channel, ordered by viewCount, duration=short."""
    params = {
        "key": api_key,
        "part": "snippet",
        "channelId": channel_id,
        "type": "video",
        "videoDuration": "short",
        "order": "viewCount",
        "maxResults": min(50, max_n),
    }
    try:
        data = yt_get("search", params)
    except Exception as e:
        print(f"    ERROR for {channel_id}: {e}")
        return []
    return [it["id"]["videoId"] for it in data.get("items", []) if it.get("id", {}).get("videoId")]


def hydrate(api_key: str, ids: list[str]) -> list[dict]:
    items = []
    for i in range(0, len(ids), 50):
        batch = ids[i : i + 50]
        data = yt_get(
            "videos",
            {
                "key": api_key,
                "part": "snippet,contentDetails,statistics",
                "id": ",".join(batch),
                "maxResults": 50,
            },
        )
        items.extend(data.get("items", []))
    return items


def main() -> None:
    api_key = load_api_key()
    raw = json.loads(RAW_INPUT.read_text(encoding="utf-8"))

    channels: dict[str, str] = {}  # id -> title
    for it in raw:
        s = it.get("snippet", {})
        cid = s.get("channelId")
        ct = s.get("channelTitle", "")
        if cid and cid not in channels:
            channels[cid] = ct

    ai_channels = {cid: ct for cid, ct in channels.items() if looks_like_ai_channel(ct)}
    print(f"[*] Total channels in pool: {len(channels)}")
    print(f"[*] Likely-AI channels by name heuristic: {len(ai_channels)}")
    for cid, ct in list(ai_channels.items())[:30]:
        print(f"    {ct}")
    if len(ai_channels) > 30:
        print(f"    ... and {len(ai_channels) - 30} more")

    print(f"[*] Fetching top Shorts per channel (order=viewCount, max 25 each)...")
    all_ids: set[str] = set()
    for i, (cid, ct) in enumerate(ai_channels.items(), 1):
        ids = channel_top_shorts(api_key, cid, max_n=25)
        all_ids.update(ids)
        if i % 10 == 0 or i == len(ai_channels):
            print(f"    [{i}/{len(ai_channels)}]  {ct[:40]:40}  running unique IDs: {len(all_ids)}")
        time.sleep(0.05)

    print(f"[*] Hydrating {len(all_ids)} video IDs...")
    items = hydrate(api_key, list(all_ids))
    print(f"[*] Got details for {len(items)} videos.")

    OUTPUT_RAW.write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[*] Wrote {OUTPUT_RAW}")


if __name__ == "__main__":
    main()
