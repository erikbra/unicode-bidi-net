# Maintenance verification — 2026-11-01

Scope: the `unicode-bidi-net` source tree based on main commit
`14fc0a5d273d2ffdc59dc3e6b2828847dbd1cda9`, with the Unicode 18 maintenance changes.
This records source/build verification, not a published NuGet release.

## Changes

- Unicode data: 16.0.0 → 18.0.0, generated directly from the official UCD files.
  Algorithm baseline remains Rust unicode-bidi 0.3.18. Package metadata is prepared
  for 0.4.0. Compatibility differences are described in `docs/maintenance.md`.
- NuGet: coverlet.collector 10.1.0, Microsoft.NET.Test.Sdk 18.11.0,
  xunit.v3.mtp-off 4.0.2, xunit.runner.visualstudio 4.0.1.
  BenchmarkDotNet remains 0.15.8 (no newer stable version reported).
- CI: current GitHub Actions releases pinned by commit; full Unicode conformance
  and table regeneration on PRs and main pushes, in addition to the weekly schedule.
- Dependency maintenance: weekly Dependabot configuration for NuGet, Actions and
  both Cargo harnesses; restore audits direct and transitive NuGet packages and
  fails for advisories or unavailable audit feeds.
- Live GitHub settings verified: Unicode Conformance workflow active, Dependabot
  alerts enabled (HTTP 204), Dependabot security updates enabled.

## Local verification

Environment: macOS arm64, .NET SDK 10.0.401, runtime 10.0.12; repo-local Rust stable
1.99.1 for the reference harness.

| Check | Result |
| --- | --- |
| `dotnet restore UnicodeBidiNet.slnx --force --no-http-cache` | Passed, no audit warnings |
| `dotnet build UnicodeBidiNet.slnx --configuration Release --no-restore --nologo` | Passed, 0 warnings, 0 errors |
| `dotnet test UnicodeBidiNet.slnx --configuration Release --no-build --nologo --collect:'XPlat Code Coverage'` | 171 passed, 0 failed, 0 skipped; coverage artifact produced |
| `python3 -m unittest discover -s tools -p 'test_*.py' -v` | 5 passed |
| `python3 tools/parity/run_bidi_parity.py --out-dir artifacts/bidi-parity-maintenance` | 44/44 matched Rust 0.3.18 |
| Full Unicode 18 conformance, both suites, zero-failure ratchet | 861,948/861,948 matched |
| Regenerate tables and compare against committed content | Identical |
| `dotnet package list --project UnicodeBidiNet.slnx --vulnerable --include-transitive --format json --no-restore` | No known vulnerable packages reported by `https://data.nuget.org/v3/index.json` |
| `dotnet package list --project UnicodeBidiNet.slnx --outdated --format json --no-restore` | No newer stable direct package versions reported |
| `actionlint` and `git diff --check` | Passed |
| Release pack | 0.4.0 nupkg and snupkg built; Unicode license, README, release notes and dependency-free net10.0 target inspected |

The six new UCD assignment/default regression cases were also run against an
isolated copy of the original main revision. All six failed with the expected old
classifications, and all pass with the new data.

The NuGet result covers the resolved solution graph on the checked date, not Rust
advisories or consumer runtime patch state. The previous status check on the same
date also reported no known vulnerable NuGet packages. Generated-file SHA-256
values identify the three official UCD inputs used for this update.

No NuGet publication or merge was performed as part of local verification.
