#!/usr/bin/env python3
from __future__ import annotations

import argparse
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
UNICODE_VERSION = "16.0.0"
BASE_URL = f"https://www.unicode.org/Public/{UNICODE_VERSION}/ucd"
FILES = ["BidiTest.txt", "BidiCharacterTest.txt"]


def main() -> int:
    parser = argparse.ArgumentParser(description="Download Unicode Bidi conformance data.")
    parser.add_argument("--out-dir", type=Path, default=ROOT / f"artifacts/unicode-{UNICODE_VERSION}")
    args = parser.parse_args()

    args.out_dir.mkdir(parents=True, exist_ok=True)
    for name in FILES:
        url = f"{BASE_URL}/{name}"
        destination = args.out_dir / name
        print(f"fetch {url} -> {destination}")
        urllib.request.urlretrieve(url, destination)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
