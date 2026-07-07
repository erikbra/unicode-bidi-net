# Unicode Bidi Conformance

The curated parity harness compares selected behavior against Rust `unicode-bidi` `0.3.18` on every PR.
The larger Unicode conformance harness uses Unicode 16.0.0 test data and is intended for local or manual CI runs.

## Fetch Data

```bash
python3 tools/conformance/fetch_unicode_bidi_tests.py
```

This downloads these files to `artifacts/unicode-16.0.0/`:

- `BidiTest.txt`
- `BidiCharacterTest.txt`

The files are not committed because they are generated upstream corpus files and are large enough to make normal diffs noisy.

## Run A Bounded Ratchet Sample

```bash
python3 tools/conformance/run_unicode_bidi_conformance.py \
  --max-cases 1000 \
  --out-dir artifacts/bidi-conformance \
  --fail-on-regression
```

The baseline in `tools/conformance/ratchet-baseline.json` currently requires at least 1000 cases and zero failures.

## Run The Full Selected Corpus

```bash
python3 tools/conformance/run_unicode_bidi_conformance.py \
  --suite both \
  --full \
  --out-dir artifacts/bidi-conformance-full \
  --fail-on-failure
```

The runner emits:

- `results.json`
- `summary.md`

## GitHub Actions

The regular CI workflow compiles the conformance scripts but keeps PRs on the curated parity suite.
The manual **Unicode Conformance** workflow downloads Unicode data and runs either a bounded sample or the full selected corpus.

[#13](https://github.com/erikbra/unicode-bidi-net/issues/13) tracks promoting the full corpus to a scheduled/provisioned ratchet gate.
