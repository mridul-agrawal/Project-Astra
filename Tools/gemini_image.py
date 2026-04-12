#!/usr/bin/env python3
"""
Gemini image generation helper for Project Astra concept art validation.

Reads GEMINI_API_KEY from Tools/.env or environment.
Calls gemini-2.5-flash-image-preview (Nano Banana) and writes the result PNG to disk.
Prints the absolute output path on success.

Usage:
    python gemini_image.py "your prompt here"
    python gemini_image.py --prompt-file path/to/prompt.txt --output output/foo.png
    python gemini_image.py "edit prompt" --reference path/to/existing.png
"""

import argparse
import base64
import os
import sys
from datetime import datetime
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent.resolve()
DEFAULT_OUTPUT_DIR = SCRIPT_DIR / "output"
ENV_FILE = SCRIPT_DIR / ".env"
MODEL = os.environ.get("GEMINI_IMAGE_MODEL", "gemini-2.5-flash-image")


def load_env():
    """Load GEMINI_API_KEY from Tools/.env (preferred) or process env."""
    if ENV_FILE.exists():
        for line in ENV_FILE.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            if "=" in line:
                k, _, v = line.partition("=")
                os.environ.setdefault(k.strip(), v.strip().strip('"').strip("'"))
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        sys.exit("ERROR: GEMINI_API_KEY not found in Tools/.env or environment.")
    return key


def generate(prompt: str, output_path: Path, reference_path: Path | None = None):
    from google import genai
    from google.genai import types

    client = genai.Client(api_key=load_env())

    contents: list = [prompt]
    if reference_path:
        if not reference_path.exists():
            sys.exit(f"ERROR: reference image not found: {reference_path}")
        image_bytes = reference_path.read_bytes()
        mime = "image/png" if reference_path.suffix.lower() == ".png" else "image/jpeg"
        contents.append(types.Part.from_bytes(data=image_bytes, mime_type=mime))

    response = client.models.generate_content(
        model=MODEL,
        contents=contents,
    )

    image_saved = False
    text_chunks = []
    for cand in response.candidates or []:
        for part in cand.content.parts or []:
            if getattr(part, "inline_data", None) and part.inline_data.data:
                data = part.inline_data.data
                if isinstance(data, str):
                    data = base64.b64decode(data)
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_bytes(data)
                image_saved = True
            elif getattr(part, "text", None):
                text_chunks.append(part.text)

    if not image_saved:
        msg = "ERROR: model returned no image data."
        if text_chunks:
            msg += "\nModel text response:\n" + "\n".join(text_chunks)
        sys.exit(msg)

    print(str(output_path.resolve()))
    if text_chunks:
        print("--- model commentary ---", file=sys.stderr)
        print("\n".join(text_chunks), file=sys.stderr)


def main():
    parser = argparse.ArgumentParser(description="Gemini image gen for concept art")
    parser.add_argument("prompt", nargs="?", help="Prompt text (or use --prompt-file)")
    parser.add_argument("--prompt-file", type=Path, help="Path to a text file containing the prompt")
    parser.add_argument("--output", type=Path, help="Output PNG path (default: Tools/output/gen_<timestamp>.png)")
    parser.add_argument("--reference", type=Path, help="Optional reference image for editing/iteration")
    args = parser.parse_args()

    if args.prompt_file:
        prompt = args.prompt_file.read_text(encoding="utf-8")
    elif args.prompt:
        prompt = args.prompt
    else:
        sys.exit("ERROR: provide a prompt as argument or --prompt-file")

    if args.output:
        output_path = args.output
    else:
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        output_path = DEFAULT_OUTPUT_DIR / f"gen_{ts}.png"

    generate(prompt, output_path, args.reference)


if __name__ == "__main__":
    main()
