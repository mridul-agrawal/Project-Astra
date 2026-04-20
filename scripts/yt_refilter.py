"""
Refilter the raw YouTube Shorts harvest with stricter rules and write data/candidates.csv.

Reads data/candidates_raw.json (produced by yt_harvest.py). No API calls.

Stricter than harvest's built-in filter:
  - requires a NARRATIVE INTENT keyword (story/facts/untold/etc.) in title+description
  - aggressive exclusion list (kids vlogs, crafts, devotional status, festival greetings,
    songs/dance/reels, reactions, cute content)
  - dedup by (channel, normalized-title-prefix) keeping higher-view copy
"""

from __future__ import annotations

import csv
import json
import re
import sys
from dataclasses import asdict, dataclass
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


PROJECT_ROOT = Path(__file__).resolve().parent.parent
RAW_DUMP = PROJECT_ROOT / "data" / "candidates_raw.json"
OUTPUT_CSV = PROJECT_ROOT / "data" / "candidates.csv"


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


MYTHOLOGY_KEYWORDS = {
    "shiva", "shiv ji", "shivji", "mahadev", "mahadeva", "rudra", "bholenath", "neelkanth",
    "vishnu", "narayan", "narayana", "krishna", "kanhaiya",
    "shri ram", "shree ram", "bhagwan ram", "ramji", "ram ji", "raghav", "raghava",
    "lakshman", "laxman", "sita", "sita mata",
    "hanuman", "hanumaan", "bajrangbali", "bajrang bali", "anjaneya",
    "ganesh", "ganesha", "ganpati", "ganapati",
    "durga", "kali maa", "kali ma", "parvati", "lakshmi mata", "saraswati",
    "brahma", "indra", "surya dev", "yamraj",
    "mahabharat", "mahabharata", "ramayan", "ramayana",
    "arjun", "arjuna", "karn", "karan mahabharat", "bhishma", "drona",
    "duryodhan", "duryodhana",
    "yudhishthir", "yudhishthira", "bhima", "draupadi", "pandav", "kaurav",
    "ravan", "ravana", "vibhishan", "kumbhkaran", "kumbhakarna", "meghnad", "indrajit",
    "bhagavad gita", "bhagavat gita", "gita upadesh", "srimad",
    "asura", "rakshas", "rakshasa", "daitya", "danav", "danava",
    "vedas", "vedic", "puran", "purana", "puranas", "puranic", "upanishad",
    "mythology", "mythological",
    "hindu god", "indian god", "hindu mythology", "indian mythology",
    "sanatan dharma", "sanatana dharma",
    "samudra manthan", "kurukshetra", "ayodhya", "dwarka", "vaikuntha", "kedarnath",
    "trishul", "sudarshan chakra", "gandeev",
    "parshuram", "parashuram", "jatayu", "sugriv", "sugriva",
    "prahlad", "hiranyakashipu", "narasimha", "vamana",
    "takshak", "vasuki",
    # devanagari
    "शिव", "महादेव", "विष्णु", "कृष्ण", "राम", "हनुमान", "रावण",
    "महाभारत", "रामायण", "पौराणिक", "कर्ण", "अर्जुन", "पांडव", "कौरव",
    "द्रौपदी", "सीता", "लक्ष्मण", "मह1काल", "महाकाल",
}

NARRATIVE_KEYWORDS = {
    "story", "stories", "kahani", "kahaani", "kahaniya", "kahaniyan",
    "katha", "kathayein", "tale", "tales",
    "legend", "legends", "myth", "mytho", "mythology", "mythological",
    "fact", "facts", "truth", "truths", "secret", "secrets",
    "untold", "unknown", "shocking", "amazing", "interesting", "fascinating",
    "did you know", "did u know", "didyouknow",
    "explained", "explain", "explaining", "explanation",
    "hidden", "mystery", "mysterious", "mysteries",
    "lore", "origin", "origins", "backstory",
    "real reason", "real story", "why did", "why does",
    "what happened", "how did", "how does",
    "revealed", "reveal", "reveals",
    "जब ", "ki kahani", "की कहानी", "ki katha", "की कथा",
    "कहानी", "कथा", "रहस्य", "सच्चाई", "जानिए", "जानें",
    "क्यों", "कैसे", "असली कहानी", "रोचक", "तथ्य",
    "ancient", "prachin", "prachen",
}

EXCLUDE_KEYWORDS = {
    # Kids / non-mythology channels that surface on mythology-adjacent search
    "vlad and niki", "diana", "vlad", "niki", "home alone",
    "toy story", "vending machine", "tom", "jerry", "rithvi", "kavi",
    "kids craft", "kids video", "kidsvideo",

    # Status / reel / greeting
    "status", "ytshorts", "#status", "whatsapp status", "insta status", "fb status",
    "happy janmashtami", "happy krishna", "happy ram navami",
    "happy shiv", "happy mahashivratri", "happy diwali", "happy navratri", "happy holi",
    "shivratri special", "janmashtami special", "navratri special",
    "ram navami", "ram naum", "ram nav",
    "celebrate", "wishing you", "wishes",

    # Devotional
    "bhajan", "aarti", "aartis", "mantra", "chalisa", "chaalisa", "kirtan",
    "pooja", "puja path",
    "mahima", "mandir darshan", "live darshan", "live aarti", "temple live",
    "mandir live", "cm ke darshan", "darshan live",
    "powerful hanuman", "hari om", "radhe radhe", "jai shree krishna status",
    "jai shri krishna status", "har har mahadev status",
    "devotional song", "devotional music", "bhakti song", "bhakti gana",
    "god", "#devotional",

    # Craft / DIY / drawing
    "diy ", "diy#", "#diy", "craft", "paper cup", "paper cups",
    "drawing", "sketch", "art tutorial", "how to draw", "how to make",
    "origami", "clay art",

    # Songs / dance / remix / reel
    "remix", "dj ", "dance cover", "choreography", "dance", "song lyrics",
    "haryanvi song", "full song", "new song", "song teaser",
    "#song", "#dance",

    # Comedy / vlog / reaction / prank
    "vlog", "vlogger", "reaction", "reacting", "prank", "comedy scene",
    "cute krishna", "cute baby krishna", "cute ram", "jhula",
    "rone lag gayi", "emotional #beta", "beta beti",

    # Other religions / unrelated
    "jesus", "allah", "bible", "quran", "christian",

    # Movies/trailers/promo
    "full movie", "full episode", "trailer", "official teaser",
    "new movie", "song teaser", "movie scene",

    # Snake / misc storytelling that's NOT mythology
    "nagin ki kahani", "ek nagin",

    # Cartoon comedy / kids animation using mythology characters
    "cartoon", "shortscomedy", "shorts comedy",
    "gulli bulli", "tmkoc", "taarak mehta", "chota bheem", "chhota bheem",
    "granny", "mirzapur",

    # TV-serial / actor / Bollywood meta-content (about shows, not myth)
    "tv serial", "tv show", "serial actor", "doordarshan",
    "ramayan tv", "mahabharat tv", "bollywood", "star plus", "colors tv",
    "saurabh raj jain", "nitish bhardwaj",

    # Devotional prayer reels (mythology name used as an appeal, not a story)
    "meri raksha karo", "meri beti ki raksha", "raksha karna bholenath",
    "bachalo prabhu", "kripa karo",

    # Video games using mythology names (Capcom's Asura's Wrath etc.)
    "asura's wrath", "asuras wrath", "smite game",

    # Actor interviews / bhakti serial clips (meta content, not mythology itself)
    "jr ntr", "jr. ntr", "jr.ntr",
    "bhakti serial", "bhakti serials",
}

DEVOTIONAL_HASHTAG_PATTERNS = [
    "#shivstatus", "#ramstatus", "#krishnastatus", "#mahadevstatus",
    "#hanumanstatus", "#jaishriram", "#harharmahadev #shorts #status",
]


def load_raw() -> list[dict]:
    if not RAW_DUMP.exists():
        sys.exit(f"Missing {RAW_DUMP}. Run yt_harvest.py first.")
    return json.loads(RAW_DUMP.read_text(encoding="utf-8"))


DURATION_RE = re.compile(r"^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?$")


def parse_duration(iso: str) -> int:
    m = DURATION_RE.match(iso or "")
    if not m:
        return 0
    h, mi, s = (int(x) if x else 0 for x in m.groups())
    return h * 3600 + mi * 60 + s


def has_any(text: str, keywords) -> bool:
    """Substring match — used for EXCLUDE list where we want loose matching."""
    return any(kw in text for kw in keywords)


def _build_boundary_regex(keywords) -> re.Pattern:
    """Word-boundary match that respects Devanagari characters as word-part."""
    escaped = sorted((re.escape(k.lower()) for k in keywords), key=len, reverse=True)
    pattern = r"(?<![\w\u0900-\u097F])(?:" + "|".join(escaped) + r")(?![\w\u0900-\u097F])"
    return re.compile(pattern, re.IGNORECASE)


MYTHOLOGY_RE = _build_boundary_regex(MYTHOLOGY_KEYWORDS)
NARRATIVE_RE = _build_boundary_regex(NARRATIVE_KEYWORDS)


def has_boundary(text: str, pattern: re.Pattern) -> bool:
    return bool(pattern.search(text))


def detect_language(snippet: dict) -> str:
    lang = snippet.get("defaultAudioLanguage") or snippet.get("defaultLanguage") or ""
    if lang:
        return lang
    title = snippet.get("title", "")
    if re.search(r"[\u0900-\u097F]", title):
        return "hi"
    return "en"


def normalize_for_dedup(title: str) -> str:
    # strip emojis, hashtags, punctuation, collapse whitespace, take first 50 chars
    t = re.sub(r"[#@][\w_]+", "", title)
    t = re.sub(r"[^\w\s\u0900-\u097F]", "", t, flags=re.UNICODE)
    t = re.sub(r"\s+", " ", t).strip().lower()
    return t[:50]


def classify(item: dict) -> tuple[bool, str]:
    """Return (keep, reason). reason is the rejection category if not kept."""
    snippet = item.get("snippet", {})
    stats = item.get("statistics", {})
    content = item.get("contentDetails", {})

    duration = parse_duration(content.get("duration", ""))
    if duration == 0 or duration > 60:
        return False, "duration"

    views = int(stats.get("viewCount", 0) or 0)
    if views < 1_000_000:
        return False, "views"

    title = (snippet.get("title", "") or "").lower()
    desc = (snippet.get("description", "") or "").lower()
    tags = " ".join(snippet.get("tags", []) or []).lower()
    channel = (snippet.get("channelTitle", "") or "").lower()

    hay_all = f"{title} {desc} {tags} {channel}"
    title_and_channel = f"{title} {channel}"

    if has_any(hay_all, EXCLUDE_KEYWORDS):
        return False, "excluded"
    for pat in DEVOTIONAL_HASHTAG_PATTERNS:
        if pat in hay_all:
            return False, "excluded"

    # Mythology keyword must appear in title OR channel — rules out videos that only
    # reference gods via hashtags while being about unrelated motivational/emotional content.
    if not has_boundary(title_and_channel, MYTHOLOGY_RE):
        return False, "off_topic"

    # Narrative intent can appear anywhere (title, desc, tags, channel).
    if not has_boundary(hay_all, NARRATIVE_RE):
        return False, "no_narrative_intent"

    return True, "keep"


def build_row(item: dict) -> VideoRow:
    vid = item["id"]
    snippet = item.get("snippet", {})
    stats = item.get("statistics", {})
    content = item.get("contentDetails", {})

    desc = (snippet.get("description") or "").replace("\n", " ").strip()
    return VideoRow(
        video_id=vid,
        title=snippet.get("title", ""),
        url=f"https://www.youtube.com/shorts/{vid}",
        channel_title=snippet.get("channelTitle", ""),
        channel_id=snippet.get("channelId", ""),
        view_count=int(stats.get("viewCount", 0) or 0),
        like_count=int(stats.get("likeCount", 0) or 0),
        comment_count=int(stats.get("commentCount", 0) or 0),
        duration_seconds=parse_duration(content.get("duration", "")),
        published_at=snippet.get("publishedAt", ""),
        language=detect_language(snippet),
        description=desc[:500],
        tags=", ".join((snippet.get("tags") or [])[:20]),
    )


def main() -> None:
    items = load_raw()
    print(f"[*] Loaded {len(items)} raw candidates.")

    rejections: dict[str, int] = {}
    survivors: list[VideoRow] = []
    for item in items:
        keep, reason = classify(item)
        if not keep:
            rejections[reason] = rejections.get(reason, 0) + 1
            continue
        survivors.append(build_row(item))

    print(f"[*] Rejection breakdown: {rejections}")
    print(f"[*] {len(survivors)} survive strict filter.")

    # Dedup by (channel_id, normalized title prefix) — keep higher-view copy.
    best: dict[tuple[str, str], VideoRow] = {}
    for r in survivors:
        key = (r.channel_id, normalize_for_dedup(r.title))
        prev = best.get(key)
        if prev is None or r.view_count > prev.view_count:
            best[key] = r
    deduped = list(best.values())
    print(f"[*] After dedup: {len(deduped)}")

    deduped.sort(key=lambda r: r.view_count, reverse=True)
    top = deduped[:100]

    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(VideoRow.__dataclass_fields__.keys()))
        writer.writeheader()
        for r in top:
            writer.writerow(asdict(r))

    print(f"[*] Wrote {len(top)} rows to {OUTPUT_CSV}")
    print(f"[*] Top 15 by views:")
    for r in top[:15]:
        print(f"    {r.view_count:>12,}  [{r.language}]  {r.channel_title[:22]:22}  {r.title[:80]}")


if __name__ == "__main__":
    main()
