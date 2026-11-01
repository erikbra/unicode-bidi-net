# Unicode.Bidi Parity Execution Tracker

Last updated: 2026-11-01

## Target

The algorithm parity target is Rust `unicode-bidi` `0.3.18`. The package uses Unicode
`18.0.0` character data directly from the UCD; intentional differences from Rust's
Unicode `16.0.0` data are documented in `docs/maintenance.md`.

## Current Status

- Core UBA stages through L2 are ported.
- Curated Rust parity harness is part of CI.
- Unicode conformance data is fetched on demand; the full ratchet runs on pull requests, pushes to main, and weekly.
- Upstream Rust unit-test parity is tracked in `reports/upstream-unit-test-parity.md`.
- Package metadata is prepared for `0.4.0` NuGet packing; publication is manual.

## Baselines

| Gate | Baseline |
|---|---:|
| Curated parity failures | 0 |
| Unicode conformance sample cases | 1000 |
| Unicode conformance sample failures | 0 |
| Unicode conformance full cases | 861948 |
| Unicode conformance full failures | 0 |

## Required Slice Closeout

1. Run `dotnet test UnicodeBidiNet.slnx --configuration Release --nologo`.
2. Run `python3 tools/parity/run_bidi_parity.py --out-dir artifacts/bidi-parity`.
3. Run or intentionally defer Unicode conformance:
   - bounded: `python3 tools/conformance/run_unicode_bidi_conformance.py --max-cases 1000 --out-dir artifacts/bidi-conformance --fail-on-regression`
   - full/provisioned: `python3 tools/conformance/run_unicode_bidi_conformance.py --full --baseline tools/conformance/full-ratchet-baseline.json --out-dir artifacts/bidi-conformance-full --fail-on-regression --fail-on-failure`
4. Pack preview package with `dotnet pack src/Unicode.Bidi/Unicode.Bidi.csproj --configuration Release --output artifacts/packages`.

## Known Gaps

See `docs/api-compatibility.md`.

Follow-up issues:

- [#12](https://github.com/erikbra/unicode-bidi-net/issues/12): post-preview API compatibility and documentation review.
