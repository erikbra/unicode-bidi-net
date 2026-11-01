#!/usr/bin/env python3
from __future__ import annotations

import argparse
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
UNICODE_VERSION = ET.parse(ROOT / "Directory.Build.props").findtext(".//UnicodeBidiNetUnicodeVersion")
BASE_URL = f"https://www.unicode.org/Public/{UNICODE_VERSION}/ucd"
FILES = ["BidiTest.txt", "BidiCharacterTest.txt"]


def main() -> int:
    parser = argparse.ArgumentParser(description="Download Unicode Bidi conformance data.")
    parser.add_argument("--out-dir", type=Path, default=ROOT / f"artifacts/unicode-{UNICODE_VERSION}")
    parser.add_argument("--include-character-data", action="store_true", help="Also fetch the UCD inputs for tools/generate_char_data.py.")
    args = parser.parse_args()

    args.out_dir.mkdir(parents=True, exist_ok=True)
    files = FILES + (["extracted/DerivedBidiClass.txt", "BidiBrackets.txt", "UnicodeData.txt"] if args.include_character_data else [])
    for name in files:
        url = f"{BASE_URL}/{name}"
        destination = args.out_dir / Path(name).name
        print(f"fetch {url} -> {destination}")
        urllib.request.urlretrieve(url, destination)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
