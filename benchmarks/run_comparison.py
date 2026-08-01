#!/usr/bin/env python3
# Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
# UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs; src/lib.rs; src/utf16.rs
# UNICODE_BIDI_RUST_VERSION: 0.3.18
# PORT_MODE: infrastructure

from __future__ import annotations

import argparse
import json
import os
import platform
import shutil
import subprocess
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DOTNET_PROJECT = ROOT / "benchmarks" / "Unicode.Bidi.Benchmarks" / "Unicode.Bidi.Benchmarks.csproj"
RUST_MANIFEST = ROOT / "benchmarks" / "rust" / "Cargo.toml"


@dataclass(frozen=True)
class BenchmarkCase:
    display_name: str
    dotnet_method: str
    rust_result: str
    rust_operations: int = 1


CASES = (
    BenchmarkCase("Bidi class lookup", "BidiClassLookup", "bidi_class_lookup/rust", 16),
    BenchmarkCase("InitialInfo, mixed", "InitialInfoMixed", "initial_info_mixed"),
    BenchmarkCase("BidiInfo, LTR", "BidiInfoLtr", "bidi_info_ltr"),
    BenchmarkCase("BidiInfo, mixed", "BidiInfoMixed", "bidi_info_mixed"),
    BenchmarkCase("BidiInfo + reorder, mixed", "BidiInfoReorderMixed", "bidi_info_reorder_mixed"),
)


def main() -> int:
    parser = argparse.ArgumentParser(description="Run and compare .NET and Rust unicode-bidi benchmarks.")
    parser.add_argument("--out-dir", type=Path, default=ROOT / "artifacts" / "benchmarks")
    parser.add_argument("--quick", action="store_true", help="Use short runs suitable for harness validation.")
    parser.add_argument("--skip-dotnet", action="store_true")
    parser.add_argument("--skip-rust", action="store_true")
    args = parser.parse_args()

    out_dir = args.out_dir.resolve()
    dotnet_dir = out_dir / "dotnet"
    rust_target_dir = out_dir / "rust-target"
    out_dir.mkdir(parents=True, exist_ok=True)

    if not args.skip_dotnet:
        run_dotnet(dotnet_dir, args.quick)
    if not args.skip_rust:
        run_rust(rust_target_dir, args.quick)

    dotnet = read_dotnet_results(dotnet_dir)
    rust = read_rust_results(rust_target_dir)
    comparison = compare(dotnet, rust)

    summary = {
        "environment": {
            "machine": platform.machine(),
            "platform": platform.platform(),
            "python": platform.python_version(),
        },
        "quick": args.quick,
        "benchmarks": comparison,
    }
    (out_dir / "comparison.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    markdown = render_markdown(comparison, args.quick)
    (out_dir / "summary.md").write_text(markdown, encoding="utf-8")
    print(markdown)
    return 0


def run_dotnet(out_dir: Path, quick: bool) -> None:
    command = [
        "dotnet",
        "run",
        "--configuration",
        "Release",
        "--project",
        str(DOTNET_PROJECT),
        "--",
        "--filter",
        "*CrossRuntimeBenchmarks*",
        "--artifacts",
        str(out_dir),
    ]
    if quick:
        command.extend(["--job", "short"])
    run(command)


def run_rust(target_dir: Path, quick: bool) -> None:
    env = rust_environment()
    env["CARGO_TARGET_DIR"] = str(target_dir)
    command = [
        env["UNICODE_BIDI_CARGO"],
        "bench",
        "--locked",
        "--manifest-path",
        str(RUST_MANIFEST),
        "--bench",
        "unicode_bidi",
        "--",
        "--noplot",
    ]
    if quick:
        command.extend(["--warm-up-time", "1", "--measurement-time", "1", "--sample-size", "20"])
    run(command, env=env)


def rust_environment() -> dict[str, str]:
    env = os.environ.copy()
    cargo = shutil.which("cargo")
    if cargo:
        env["UNICODE_BIDI_CARGO"] = cargo
        return env

    local_root = ROOT / "artifacts" / "rust"
    local_cargo = local_root / "cargo" / "bin" / "cargo"
    if not local_cargo.exists():
        run([str(ROOT / "tools" / "bootstrap-local-rust.sh")])

    env["CARGO_HOME"] = str(local_root / "cargo")
    env["RUSTUP_HOME"] = str(local_root / "rustup")
    env["PATH"] = f"{local_cargo.parent}{os.pathsep}{env.get('PATH', '')}"
    env["UNICODE_BIDI_CARGO"] = str(local_cargo)
    return env


def read_dotnet_results(out_dir: Path) -> dict[str, dict[str, float | None]]:
    reports = list((out_dir / "results").glob("*CrossRuntimeBenchmarks*-report-full.json"))
    if len(reports) != 1:
        raise RuntimeError(f"Expected one BenchmarkDotNet JSON report, found {len(reports)} in {out_dir}.")

    data = json.loads(reports[0].read_text(encoding="utf-8"))
    results: dict[str, dict[str, float | None]] = {}
    for benchmark in data["Benchmarks"]:
        memory = benchmark.get("Memory", {})
        results[benchmark["Method"]] = {
            "median_ns": benchmark["Statistics"]["Median"],
            "allocated_bytes": memory.get("BytesAllocatedPerOperation"),
        }
    return results


def read_rust_results(target_dir: Path) -> dict[str, float]:
    criterion_dir = target_dir / "criterion"
    results: dict[str, float] = {}
    for case in CASES:
        estimates = criterion_dir / case.rust_result / "new" / "estimates.json"
        if not estimates.exists():
            raise RuntimeError(f"Criterion result not found: {estimates}")
        data = json.loads(estimates.read_text(encoding="utf-8"))
        results[case.rust_result] = data["median"]["point_estimate"] / case.rust_operations
    return results


def compare(
    dotnet: dict[str, dict[str, float | None]],
    rust: dict[str, float],
) -> list[dict[str, float | str | None]]:
    results = []
    for case in CASES:
        dotnet_result = dotnet[case.dotnet_method]
        dotnet_ns = float(dotnet_result["median_ns"])
        rust_ns = rust[case.rust_result]
        results.append(
            {
                "case": case.display_name,
                "dotnet_median_ns": dotnet_ns,
                "rust_median_ns": rust_ns,
                "dotnet_over_rust": dotnet_ns / rust_ns,
                "dotnet_allocated_bytes": dotnet_result["allocated_bytes"],
            }
        )
    return results


def render_markdown(results: list[dict[str, float | str | None]], quick: bool) -> str:
    mode = "quick harness check" if quick else "full benchmark"
    lines = [
        "# .NET / Rust benchmark comparison",
        "",
        f"Mode: {mode}. Times are medians normalized per lookup or API call; lower is better.",
        "",
        "| Workload | .NET median | Rust median | .NET / Rust | .NET allocated |",
        "|---|---:|---:|---:|---:|",
    ]
    for result in results:
        allocated = result["dotnet_allocated_bytes"]
        allocated_text = "n/a" if allocated is None else f"{float(allocated):,.0f} B"
        lines.append(
            f"| {result['case']} | {format_ns(float(result['dotnet_median_ns']))} | "
            f"{format_ns(float(result['rust_median_ns']))} | "
            f"{float(result['dotnet_over_rust']):.2f}x | {allocated_text} |"
        )
    lines.append("")
    return "\n".join(lines)


def format_ns(value: float) -> str:
    if value >= 1_000_000:
        return f"{value / 1_000_000:.3f} ms"
    if value >= 1_000:
        return f"{value / 1_000:.3f} us"
    return f"{value:.2f} ns"


def run(command: list[str], env: dict[str, str] | None = None) -> None:
    print("+", " ".join(command), flush=True)
    subprocess.run(command, cwd=ROOT, env=env, check=True)


if __name__ == "__main__":
    raise SystemExit(main())
