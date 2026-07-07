#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CASES = ROOT / "tools/parity/bidi-class-cases.txt"
DOTNET_PROBE = ROOT / "tools/parity/dotnet_probe/UnicodeBidiDotnetProbe.csproj"
RUST_PROBE = ROOT / "tools/parity/rust_probe/Cargo.toml"


def main() -> int:
    parser = argparse.ArgumentParser(description="Compare selected .NET bidi results with unicode-bidi 0.3.18.")
    parser.add_argument("--cases", type=Path, default=DEFAULT_CASES)
    parser.add_argument("--out-dir", type=Path, default=ROOT / "artifacts/bidi-parity")
    args = parser.parse_args()

    env = parity_environment()
    require_tool("dotnet", env)
    require_tool("cargo", env)

    cases = read_cases(args.cases)
    args.out_dir.mkdir(parents=True, exist_ok=True)

    subprocess.run(
        ["dotnet", "build", str(DOTNET_PROBE), "--configuration", "Release", "--nologo"],
        cwd=ROOT,
        check=True,
        env=env,
    )

    rows = []
    failures = []
    for code_point in cases:
        rust = run_probe(
            ["cargo", "run", "--quiet", "--manifest-path", str(RUST_PROBE), "--", "bidi-class", code_point],
            env,
        )
        dotnet = run_probe(
            [
                "dotnet",
                "run",
                "--project",
                str(DOTNET_PROBE),
                "--configuration",
                "Release",
                "--no-build",
                "--",
                "bidi-class",
                code_point,
            ],
            env,
        )
        ok = rust == dotnet
        row = {"codePoint": code_point, "rust": rust, "dotnet": dotnet, "ok": ok}
        rows.append(row)
        if not ok:
            failures.append(row)

    (args.out_dir / "bidi-class-results.json").write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
    summary = f"bidi-class parity: {len(cases) - len(failures)}/{len(cases)} matched"
    (args.out_dir / "summary.md").write_text(f"# Bidi Parity\n\n{summary}\n", encoding="utf-8")
    print(summary)

    if failures:
        for failure in failures:
            print(
                f"mismatch {failure['codePoint']}: rust={failure['rust']} dotnet={failure['dotnet']}",
                file=sys.stderr,
            )
        return 1

    return 0


def read_cases(path: Path) -> list[str]:
    cases = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        cases.append(line.split()[0])
    return cases


def run_probe(command: list[str], env: dict[str, str]) -> str:
    result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, env=env)
    if result.returncode != 0:
        print(f"Command failed with exit code {result.returncode}: {' '.join(command)}", file=sys.stderr)
        if result.stdout:
            print(result.stdout, file=sys.stderr)
        if result.stderr:
            print(result.stderr, file=sys.stderr)
        raise SystemExit(result.returncode)

    return result.stdout.strip()


def require_tool(name: str, env: dict[str, str]) -> None:
    if shutil.which(name, path=env.get("PATH")) is None:
        raise SystemExit(f"Required tool '{name}' was not found on PATH.")


def parity_environment() -> dict[str, str]:
    env = os.environ.copy()
    local_rust = ROOT / "artifacts/rust"
    local_cargo_bin = local_rust / "cargo/bin"
    if (local_cargo_bin / "cargo").exists():
        env["CARGO_HOME"] = str(local_rust / "cargo")
        env["RUSTUP_HOME"] = str(local_rust / "rustup")
        env["PATH"] = f"{local_cargo_bin}{os.pathsep}{env.get('PATH', '')}"
    return env


if __name__ == "__main__":
    raise SystemExit(main())
