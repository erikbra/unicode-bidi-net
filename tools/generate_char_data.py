#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import re
from pathlib import Path

from conformance.fetch_unicode_bidi_tests import ROOT, UNICODE_VERSION


RANGE_RE = re.compile(
    r"\(\s*'\\u\{(?P<lo>[0-9a-f]+)\}'\s*,\s*"
    r"'\\u\{(?P<hi>[0-9a-f]+)\}'\s*,\s*"
    r"(?P<class>[A-Z]+)\s*\)",
    re.MULTILINE,
)
PAIR_RE = re.compile(
    r"\(\s*'\\u\{(?P<opening>[0-9a-f]+)\}'\s*,\s*"
    r"'\\u\{(?P<closing>[0-9a-f]+)\}'\s*,\s*"
    r"(?P<skeleton>None|Some\('\\u\{[0-9a-f]+\}'\))\s*\)",
    re.MULTILINE,
)
SKELETON_RE = re.compile(r"Some\('\\u\{(?P<skeleton>[0-9a-f]+)\}'\)")
BIDI_CLASS_ALIASES = {
    "Left_To_Right": "L", "Right_To_Left": "R",
    "Arabic_Letter": "AL", "European_Terminator": "ET",
}
BIDI_CLASSES = {
    "L", "R", "AL", "EN", "ES", "ET", "AN", "CS", "NSM", "BN", "B", "S", "WS",
    "ON", "LRE", "LRO", "RLE", "RLO", "PDF", "LRI", "RLI", "FSI", "PDI",
}


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate C# bidi tables from UCD files or the original Rust tables.")
    sources = parser.add_mutually_exclusive_group()
    sources.add_argument("--tables-rs", type=Path, help="Reproduce the original Unicode 16 / Rust 0.3.18 tables.")
    sources.add_argument("--ucd-dir", type=Path, default=ROOT / f"artifacts/unicode-{UNICODE_VERSION}", help="UCD input directory (defaults to artifacts/unicode-<configured version>).")
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()

    if not args.tables_rs:
        classes_path = args.ucd_dir / "DerivedBidiClass.txt"
        brackets_path = args.ucd_dir / "BidiBrackets.txt"
        unicode_data_path = args.ucd_dir / "UnicodeData.txt"
        for path in (classes_path, brackets_path):
            if f"{path.stem}-{UNICODE_VERSION}.txt" not in path.read_text(encoding="utf-8").splitlines()[0]:
                raise ValueError(f"{path} is not Unicode {UNICODE_VERSION} data")
        ranges = ucd_ranges(classes_path.read_text(encoding="utf-8"))
        pairs = ucd_pairs(brackets_path.read_text(encoding="utf-8"), unicode_data_path.read_text(encoding="utf-8"))
        provenance = "\n".join(
            f" * SHA256 {path.name}: {hashlib.sha256(path.read_bytes()).hexdigest()}"
            for path in (classes_path, brackets_path, unicode_data_path)
        )
        notice = f"""/*
 * Generated from Unicode {UNICODE_VERSION} by tools/generate_char_data.py.
 * Source: https://www.unicode.org/Public/{UNICODE_VERSION}/ucd/
 * Copyright © 1991-2026 Unicode, Inc. Unicode License V3.
 * See UNICODE-LICENSE.txt and THIRD-PARTY-NOTICES.md.
{provenance}
 */"""
        version = UNICODE_VERSION
    else:
        ranges, pairs = rust_tables(args.tables_rs)
        version = "16.0.0"
        notice = """/*
 * Generated from Rust unicode-bidi 0.3.18, src/char_data/tables.rs.
 * Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */"""

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(render(ranges, pairs, version, notice), encoding="utf-8")
    return 0


def rust_tables(path: Path) -> tuple[list, list]:
    source = path.read_text(encoding="utf-8")
    ranges = [
        (int(match.group("lo"), 16), int(match.group("hi"), 16), match.group("class"))
        for match in RANGE_RE.finditer(source)
    ]
    pairs = []
    for match in PAIR_RE.finditer(source):
        skeleton = match.group("skeleton")
        skeleton_match = SKELETON_RE.fullmatch(skeleton)
        pairs.append((
            int(match.group("opening"), 16),
            int(match.group("closing"), 16),
            int(skeleton_match.group("skeleton"), 16) if skeleton_match else None,
        ))

    if not ranges:
        raise RuntimeError(f"No bidi class ranges found in {path}")
    if not pairs:
        raise RuntimeError(f"No bidi bracket pairs found in {path}")

    return ranges, pairs


def code_point_range(value: str) -> tuple[int, int]:
    ends = value.strip().split("..")
    lo, hi = int(ends[0], 16), int(ends[-1], 16)
    if not 0 <= lo <= hi <= 0x10FFFF:
        raise ValueError(f"Invalid Unicode range: {value}")
    return lo, hi


def ucd_ranges(source: str) -> list[tuple[int, int, str]]:
    classes = ["L"] * 0x110000
    defaults, explicit = [], []
    for line in source.splitlines():
        if "@missing:" in line:
            fields = line.split("@missing:", 1)[1].split("#", 1)[0].split(";")
            defaults.append(fields)
        else:
            content = line.split("#", 1)[0].strip()
            if content:
                explicit.append(content.split(";"))
    if not defaults or not explicit:
        raise ValueError("Expected UCD @missing defaults and explicit bidi classes")
    # UAX #44: later @missing ranges override earlier defaults; explicit entries win.
    for interval, value in defaults + explicit:
        lo, hi = code_point_range(interval)
        bidi_class = BIDI_CLASS_ALIASES.get(value.strip(), value.strip())
        if bidi_class not in BIDI_CLASSES:
            raise ValueError(f"Unknown bidi class: {value}")
        classes[lo:hi + 1] = [bidi_class] * (hi - lo + 1)

    ranges = []
    start = 0
    for end in range(1, len(classes) + 1):
        if end == len(classes) or classes[end] != classes[start]:
            # L is the runtime lookup's fallback, so it need not occupy a range.
            if classes[start] != "L":
                ranges.append((start, end - 1, classes[start]))
            start = end
    return ranges


def ucd_pairs(brackets: str, unicode_data: str) -> list[tuple[int, int, int | None]]:
    decompositions = {}
    for line in unicode_data.splitlines():
        if not line.strip():
            continue
        fields = line.split(";")
        decomposition = fields[5].split()
        if len(decomposition) == 1 and not decomposition[0].startswith("<"):
            decompositions[int(fields[0], 16)] = int(decomposition[0], 16)
    entries = {}
    for line in brackets.splitlines():
        content = line.split("#", 1)[0].strip()
        if content:
            point, partner, kind = [field.strip() for field in content.split(";")]
            entries[int(point, 16)] = (int(partner, 16), kind)
    pairs = []
    for opening, (closing, kind) in sorted(entries.items()):
        if kind != "o":
            continue
        if entries.get(closing) != (opening, "c"):
            raise ValueError(f"Non-reciprocal bidi bracket pair: {opening:X}; {closing:X}")
        # BD16 identifies canonically equivalent brackets (e.g. U+2329 and U+3008).
        skeleton = decompositions.get(opening)
        pairs.append((opening, closing, skeleton))
    if not pairs:
        raise ValueError("No opening bracket pairs found")
    return pairs


def render(ranges: list[tuple[int, int, str]], pairs: list[tuple[int, int, int | None]], version: str, notice: str) -> str:
    rows = "\n".join(
        f"        new(0x{lo:X}, 0x{hi:X}, BidiClass.{bidi_class}),"
        for lo, hi, bidi_class in ranges
    )
    pair_rows = "\n".join(
        f"        new(0x{opening:X}, 0x{closing:X}, {f'0x{skeleton:X}' if skeleton is not None else 'null'}),"
        for opening, closing, skeleton in pairs
    )

    return f"""// <auto-generated />
{notice}

using Unicode.Bidi;

namespace Unicode.Bidi.char_data;

internal static class Tables
{{
    internal static readonly (int Major, int Minor, int Patch) UnicodeVersion = ({version.replace('.', ', ')});

    internal static readonly BidiClassRange[] BidiClassTable =
    [
{rows}
    ];

    internal static readonly BidiPair[] BidiPairTable =
    [
{pair_rows}
    ];
}}
"""


if __name__ == "__main__":
    raise SystemExit(main())
