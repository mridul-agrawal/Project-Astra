"""
Tier 1 — Metadata-only harvest of Indian-mythology YouTube Shorts.

Outputs data/candidates.csv with up to 100 rows that satisfy:
  - duration <= 60s
  - view_count >= 1,000,000
  - title/description matches mythology topic filter
  - not in devotional / wallpaper / status exclusion filter
Sorted by view_count descending.
"""

from __future__ import annotations

import csv
import re
import sys
import time
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable

import requests


try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
SECRETS_FILE = PROJECT_ROOT / ".secrets" / "youtube.env"
OUTPUT_CSV = PROJECT_ROOT / "data" / "candidates.csv"
RAW_DUMP = PROJECT_ROOT / "data" / "candidates_raw.json"

API_BASE = "https://www.googleapis.com/youtube/v3"

SEARCH_QUERIES = [
    "Mahabharata story",
    "Mahabharata facts",
    "Mahabharata untold story",
    "Ramayana story",
    "Ramayana facts",
    "Shiva story",
    "Mahadev story",
    "Krishna story",
    "Krishna leela",
    "Hanuman story",
    "Karna story",
    "Arjuna story",
    "Ravana story",
    "Bhishma story",
    "Hindu mythology story",
    "Hindu mythology facts",
    "Indian mythology",
    "Bhagavad Gita story",
    "Vishnu avatar",
    "Devi story",
    "asura story",
    "mythology untold",
    "पौराणिक कथा",
    "महाभारत कहानी",
    "रामायण कहानी",
]

MYTHOLOGY_KEYWORDS = {
    "shiva", "shiv", "mahadev", "mahadeva", "rudra", "bholenath", "neelkanth",
    "vishnu", "narayan", "narayana", "hari", "krishna", "kanha", "kanhaiya",
    "ram", "rama", "raam", "raghav", "raghava", "lakshman", "laxman", "sita",
    "hanuman", "hanumaan", "bajrangbali", "bajrang bali", "anjaneya", "maruti",
    "ganesh", "ganesha", "ganpati", "ganapati", "vinayak",
    "devi", "durga", "kali", "parvati", "lakshmi", "saraswati",
    "brahma", "indra", "agni", "surya", "yama", "varuna", "kubera",
    "mahabharat", "mahabharata", "ramayan", "ramayana",
    "arjun", "arjuna", "karna", "bhishma", "drona", "duryodhan", "duryodhana",
    "yudhishthir", "yudhishthira", "bhima", "nakul", "sahadev", "draupadi",
    "ravan", "ravana", "vibhishan", "kumbhkaran", "kumbhakarna", "meghnad",
    "bhagavad gita", "bhagavat", "gita", "srimad",
    "asura", "rakshas", "rakshasa", "daitya", "danav", "danava",
    "vedic", "vedas", "puran", "purana", "puranic", "upanishad",
    "mythology", "mythos", "mythological",
    "dharma", "adharma", "karma",
    "hindu god", "indian god", "hindu mythology", "indian mythology",
    "samudra manthan", "kurukshetra", "ayodhya", "dwarka", "vaikuntha",
    "trishul", "sudarshan", "chakra", "gandeev",
    # devanagari
    "शिव", "महादेव", "विष्णु", "कृष्ण", "राम", "हनुमान", "रावण",
    "महाभारत", "रामायण", "पौराणिक", "कथा",
}

EXCLUDE_KEYWORDS = {
    "bhajan", "aarti", "aartis", "mantra", "chalisa", "kirtan", "namavali",
    "whatsapp status", "wallpaper", "4k wallpaper", "wallpaper 4k",
    "live darshan", "live aarti", "temple live", "mandir live",
    "devotional song", "devotional music", "bhakti song",
    "status video", "fb status", "insta status",
    "full episode", "full movie",  # knocks out uploaded TV episodes / films
}


@dataclass
class VideoRow:
    video_id: str
    title: str
    url: str
    channel_title: str
    channel_id: str
    view_count: int
    like_count: int
    comment_count: int
    duration_seconds: int
    published_at: str
    language: str
    description: str
    tags: str


def load_api_key() -> str:
    if not SECRETS_FILE.exists():
        sys.exit(f"Missing {SECRETS_FILE}. Create it with YOUTUBE_API_KEY=...")
    for line in SECRETS_FILE.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" in line:
            k, v = line.split("=", 1)
            if k.strip() == "YOUTUBE_API_KEY":
                return v.strip()
    sys.exit("YOUTUBE_API_KEY not found in youtube.env")


def yt_get(path: str, params: dict) -> dict:
    url = f"{API_BASE}/{path}"
    r = requests.get(url, params=params, timeout=30)
    if r.status_code != 200:
        raise RuntimeError(f"{path} {r.status_code}: {r.text[:400]}")
    return r.json()


def search_shorts(api_key: str, query: str, per_query: int = 50) -> list[str]:
    """Return a list of candidate videoIds for a query. videoDuration=short (<4min)."""
    ids: list[str] = []
    page_token = None
    fetched = 0
    while fetched < per_query:
        params = {
            "key": api_key,
            "part": "snippet",
            "q": query,
            "type": "video",
            "videoDuration": "short",
            "maxResults": min(50, per_query - fetched),
            "order": "viewCount",
            "safeSearch": "none",
        }
        if page_token:
            params["pageToken"] = page_token
        data = yt_get("search", params)
        for item in data.get("items", []):
            vid = item.get("id", {}).get("videoId")
            if vid:
                ids.append(vid)
        page_token = data.get("nextPageToken")
        fetched += len(data.get("items", []))
        if not page_token:
            break
    return ids


DURATION_RE = re.compile(r"^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?$")


def parse_duration(iso: str) -> int:
    m = DURATION_RE.match(iso or "")
    if not m:
        return 0
    h, mi, s = (int(x) if x else 0 for x in m.groups())
    return h * 3600 + mi * 60 + s


def chunked(seq: list, size: int) -> Iterable[list]:
    for i in range(0, len(seq), size):
        yield seq[i : i + size]


def hydrate(api_key: str, video_ids: list[str]) -> list[dict]:
    """Batch videos.list in groups of 50 to get full metadata + stats."""
    all_items: list[dict] = []
    for batch in chunked(video_ids, 50):
        params = {
            "key": api_key,
            "part": "snippet,contentDetails,statistics",
            "id": ",".join(batch),
            "maxResults": 50,
        }
        data = yt_get("videos", params)
        all_items.extend(data.get("items", []))
    return all_items


def detect_language(snippet: dict) -> str:
    lang = snippet.get("defaultAudioLanguage") or snippet.get("defaultLanguage") or ""
    if lang:
        return lang
    title = snippet.get("title", "")
    # crude devanagari sniff
    if re.search(r"[\u0900-\u097F]", title):
        return "hi"
    return "en"  # assumption for English-script titles


def is_topical(snippet: dict) -> bool:
    hay = " ".join(
        [
            snippet.get("title", "") or "",
            snippet.get("description", "") or "",
            " ".join(snippet.get("tags", []) or []),
            snippet.get("channelTitle", "") or "",
        ]
    ).lower()
    if any(bad in hay for bad in EXCLUDE_KEYWORDS):
        return False
    return any(kw in hay for kw in MYTHOLOGY_KEYWORDS)


def build_row(item: dict) -> VideoRow | None:
    vid = item["id"]
    snippet = item.get("snippet", {})
    stats = item.get("statistics", {})
    content = item.get("contentDetails", {})

    duration = parse_duration(content.get("duration", ""))
    if duration == 0 or duration > 60:
        return None

    views = int(stats.get("viewCount", 0) or 0)
    if views < 1_000_000:
        return None

    if not is_topical(snippet):
        return None

    desc = (snippet.get("description") or "").replace("\n", " ").strip()
    return VideoRow(
        video_id=vid,
        title=snippet.get("title", ""),
        url=f"https://www.youtube.com/shorts/{vid}",
        channel_title=snippet.get("channelTitle", ""),
        channel_id=snippet.get("channelId", ""),
        view_count=views,
        like_count=int(stats.get("likeCount", 0) or 0),
        comment_count=int(stats.get("commentCount", 0) or 0),
        duration_seconds=duration,
        published_at=snippet.get("publishedAt", ""),
        language=detect_language(snippet),
        description=desc[:500],
        tags=", ".join((snippet.get("tags") or [])[:20]),
    )


def main() -> None:
    api_key = load_api_key()

    print(f"[*] Searching {len(SEARCH_QUERIES)} queries...")
    all_ids: set[str] = set()
    for q in SEARCH_QUERIES:
        try:
            ids = search_shorts(api_key, q, per_query=50)
            print(f"    '{q}': {len(ids)} ids")
            all_ids.update(ids)
        except Exception as e:
            print(f"    '{q}': ERROR {e}")
        time.sleep(0.1)

    print(f"[*] Total unique candidate IDs: {len(all_ids)}")
    ids_list = list(all_ids)

    print(f"[*] Hydrating metadata in batches of 50...")
    items = hydrate(api_key, ids_list)
    print(f"[*] Got details for {len(items)} videos.")

    # Dump raw for debugging.
    import json
    RAW_DUMP.write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding="utf-8")

    rows: list[VideoRow] = []
    rejected = {"duration": 0, "views": 0, "topic": 0, "other": 0}
    for item in items:
        snippet = item.get("snippet", {})
        content = item.get("contentDetails", {})
        stats = item.get("statistics", {})

        duration = parse_duration(content.get("duration", ""))
        if duration == 0 or duration > 60:
            rejected["duration"] += 1
            continue
        views = int(stats.get("viewCount", 0) or 0)
        if views < 1_000_000:
            rejected["views"] += 1
            continue
        if not is_topical(snippet):
            rejected["topic"] += 1
            continue

        row = build_row(item)
        if row:
            rows.append(row)
        else:
            rejected["other"] += 1

    print(f"[*] Rejections: {rejected}")

    rows.sort(key=lambda r: r.view_count, reverse=True)
    rows = rows[:100]

    print(f"[*] Writing {len(rows)} rows to {OUTPUT_CSV}")
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(VideoRow.__dataclass_fields__.keys()))
        writer.writeheader()
        for r in rows:
            writer.writerow(asdict(r))

    if rows:
        print(f"[*] Top 5 by views:")
        for r in rows[:5]:
            print(f"    {r.view_count:>12,}  [{r.language}]  {r.title[:70]}")

    print(f"[*] Done.")


if __name__ == "__main__":
    main()
