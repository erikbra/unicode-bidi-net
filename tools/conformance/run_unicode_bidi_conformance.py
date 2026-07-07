#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
UNICODE_VERSION = "16.0.0"
DEFAULT_DATA_DIR = ROOT / f"artifacts/unicode-{UNICODE_VERSION}"
DOTNET_PROBE = ROOT / "tools/parity/dotnet_probe/UnicodeBidiDotnetProbe.csproj"
DOTNET_PROBE_ASSEMBLY = ROOT / "tools/parity/dotnet_probe/bin/Release/net10.0/UnicodeBidiDotnetProbe.dll"
DEFAULT_BASELINE = ROOT / "tools/conformance/ratchet-baseline.json"

CLASS_REPRESENTATIVES = {
    "AL": 0x0627,
    "AN": 0x0661,
    "B": 0x000A,
    "BN": 0x00AD,
    "CS": 0x002C,
    "EN": 0x0031,
    "ES": 0x002B,
    "ET": 0x0023,
    "FSI": 0x2068,
    "L": 0x0061,
    "LRE": 0x202A,
    "LRI": 0x2066,
    "LRO": 0x202D,
    "NSM": 0x0300,
    "ON": 0x0021,
    "PDF": 0x202C,
    "PDI": 0x2069,
    "R": 0x05D0,
    "RLE": 0x202B,
    "RLI": 0x2067,
    "RLO": 0x202E,
    "S": 0x0009,
    "WS": 0x0020,
}

@dataclass(frozen=True)
class Case:
    suite: str
    name: str
    units: str
    default_level: str
    expected_para: str | None
    expected_levels: str
    expected_reorder: str


def main() -> int:
    parser = argparse.ArgumentParser(description="Run Unicode Bidi conformance samples against the .NET port.")
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--out-dir", type=Path, default=ROOT / "artifacts/bidi-conformance")
    parser.add_argument("--suite", choices=["both", "character", "class"], default="both")
    parser.add_argument("--max-cases", type=int, default=1000, help="Maximum cases to run unless --full is set.")
    parser.add_argument("--full", action="store_true", help="Run every parsed case from the selected suite(s).")
    parser.add_argument("--baseline", type=Path, default=DEFAULT_BASELINE)
    parser.add_argument("--fail-on-regression", action="store_true")
    parser.add_argument("--fail-on-failure", action="store_true")
    args = parser.parse_args()

    env = os.environ.copy()
    require_tool("dotnet", env)
    ensure_data_files(args.data_dir)
    args.out_dir.mkdir(parents=True, exist_ok=True)

    subprocess.run(
        ["dotnet", "build", str(DOTNET_PROBE), "--configuration", "Release", "--nologo"],
        cwd=ROOT,
        check=True,
        env=env,
    )

    cases = load_bounded_cases(args.data_dir, args.suite, args.max_cases, args.full)

    rows = []
    failures = []
    for case in cases:
        actual = run_probe(case, env)
        expected = {
            "para": case.expected_para,
            "levels": case.expected_levels,
            "reorder": case.expected_reorder,
        }
        mismatches = compare_expected(expected, actual)
        row = {
            "suite": case.suite,
            "name": case.name,
            "defaultLevel": case.default_level,
            "units": case.units,
            "expected": expected,
            "actual": actual,
            "ok": not mismatches,
            "mismatches": mismatches,
        }
        rows.append(row)
        if mismatches:
            failures.append(row)

    summary = {
        "unicodeVersion": UNICODE_VERSION,
        "suite": args.suite,
        "full": args.full,
        "total": len(rows),
        "passed": len(rows) - len(failures),
        "failed": len(failures),
    }
    baseline = read_baseline(args.baseline)
    regression = summary["failed"] > baseline["maxFailures"] or summary["total"] < baseline["minCases"]

    (args.out_dir / "results.json").write_text(json.dumps({"summary": summary, "rows": rows}, indent=2) + "\n", encoding="utf-8")
    (args.out_dir / "summary.md").write_text(render_summary(summary, baseline, regression, failures), encoding="utf-8")
    print(f"bidi conformance: {summary['passed']}/{summary['total']} matched ({summary['failed']} failed)")

    if failures:
        for failure in failures[:20]:
            print(
                f"mismatch {failure['suite']} {failure['name']}: {failure['mismatches']}",
                file=sys.stderr,
            )

    if args.fail_on_failure and failures:
        return 1
    if args.fail_on_regression and regression:
        return 1
    return 0


def ensure_data_files(data_dir: Path) -> None:
    missing = [name for name in ("BidiTest.txt", "BidiCharacterTest.txt") if not (data_dir / name).exists()]
    if missing:
        raise SystemExit(
            f"Missing Unicode conformance data in {data_dir}: {', '.join(missing)}. "
            "Run tools/conformance/fetch_unicode_bidi_tests.py first."
        )


def load_cases(data_dir: Path, suite: str):
    if suite in ("both", "character"):
        yield from load_character_cases(data_dir / "BidiCharacterTest.txt")
    if suite in ("both", "class"):
        yield from load_class_cases(data_dir / "BidiTest.txt")


def load_bounded_cases(data_dir: Path, suite: str, max_cases: int, full: bool) -> list[Case]:
    if full or suite != "both":
        cases = list(load_cases(data_dir, suite))
        return cases if full else cases[:max_cases]

    character_budget = max_cases // 2
    class_budget = max_cases - character_budget
    character_cases = list(load_character_cases(data_dir / "BidiCharacterTest.txt"))[:character_budget]
    class_cases = list(load_class_cases(data_dir / "BidiTest.txt"))[:class_budget]
    return character_cases + class_cases


def load_character_cases(path: Path):
    for line_number, raw_line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw_line.split("#", 1)[0].strip()
        if not line:
            continue

        fields = [field.strip() for field in line.split(";")]
        if len(fields) != 5:
            continue

        code_points = [int(part, 16) for part in fields[0].split()]
        default_level = {"0": "ltr", "1": "rtl", "2": "auto"}[fields[1]]
        yield Case(
            suite="BidiCharacterTest",
            name=f"{path.name}:{line_number}",
            units=utf16_units(code_points),
            default_level=default_level,
            expected_para=fields[2],
            expected_levels=fields[3],
            expected_reorder=fields[4],
        )


def load_class_cases(path: Path):
    current_levels = ""
    current_reorder = ""
    for line_number, raw_line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw_line.split("#", 1)[0].strip()
        if not line:
            continue
        if line.startswith("@Levels:"):
            current_levels = line.split(":", 1)[1].strip()
            continue
        if line.startswith("@Reorder:"):
            current_reorder = line.split(":", 1)[1].strip()
            continue
        if line.startswith("@"):
            continue

        fields = [field.strip() for field in line.split(";")]
        if len(fields) != 2:
            continue

        class_names = fields[0].split()
        bitset = int(fields[1], 16)
        code_points = [CLASS_REPRESENTATIVES[name] for name in class_names]
        for flag, default_level in ((1, "auto"), (2, "ltr"), (4, "rtl")):
            if bitset & flag:
                yield Case(
                    suite="BidiTest",
                    name=f"{path.name}:{line_number}:{default_level}",
                    units=utf16_units(code_points),
                    default_level=default_level,
                    expected_para=None,
                    expected_levels=current_levels,
                    expected_reorder=current_reorder,
                )


def run_probe(case: Case, env: dict[str, str]) -> dict[str, str]:
    command = [
        "dotnet",
        str(DOTNET_PROBE_ASSEMBLY),
        "conformance",
        case.units,
        case.default_level,
    ]
    completed = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, env=env)
    if completed.returncode != 0:
        print(completed.stdout, file=sys.stderr)
        print(completed.stderr, file=sys.stderr)
        raise SystemExit(completed.returncode)

    return parse_probe_output(completed.stdout.strip())


def parse_probe_output(value: str) -> dict[str, str]:
    result = {}
    for part in value.split(";"):
        key, item = part.split("=", 1)
        result[key] = item
    return result


def compare_expected(expected: dict[str, str | None], actual: dict[str, str]) -> list[str]:
    mismatches = []
    if expected["para"] is not None and expected["para"] != actual["para"]:
        mismatches.append("para")
    if expected["levels"] != actual["levels"]:
        mismatches.append("levels")
    if expected["reorder"] != actual["reorder"]:
        mismatches.append("reorder")
    return mismatches


def utf16_units(code_points: list[int]) -> str:
    units = []
    for code_point in code_points:
        encoded = chr(code_point).encode("utf-16-be")
        for i in range(0, len(encoded), 2):
            units.append(f"{int.from_bytes(encoded[i:i + 2], 'big'):04X}")
    return ",".join(units)


def read_baseline(path: Path) -> dict[str, int]:
    if not path.exists():
        return {"minCases": 0, "maxFailures": 0}
    data = json.loads(path.read_text(encoding="utf-8"))
    return {
        "minCases": int(data.get("minCases", 0)),
        "maxFailures": int(data.get("maxFailures", 0)),
    }


def render_summary(summary: dict[str, object], baseline: dict[str, int], regression: bool, failures: list[dict[str, object]]) -> str:
    lines = [
        "# Unicode Bidi Conformance",
        "",
        f"- Unicode version: {summary['unicodeVersion']}",
        f"- Suite: {summary['suite']}",
        f"- Full run: {summary['full']}",
        f"- Passed: {summary['passed']} / {summary['total']}",
        f"- Failed: {summary['failed']}",
        f"- Baseline min cases: {baseline['minCases']}",
        f"- Baseline max failures: {baseline['maxFailures']}",
        f"- Ratchet regression: {'yes' if regression else 'no'}",
        "",
    ]
    if failures:
        lines.append("## First Failures")
        lines.append("")
        for failure in failures[:20]:
            lines.append(f"- `{failure['suite']}` `{failure['name']}`: {', '.join(failure['mismatches'])}")
        lines.append("")
    return "\n".join(lines)


def require_tool(name: str, env: dict[str, str]) -> None:
    if shutil.which(name, path=env.get("PATH")) is None:
        raise SystemExit(f"Required tool '{name}' was not found on PATH.")


if __name__ == "__main__":
    raise SystemExit(main())
