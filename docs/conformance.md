# Unicode Bidi Conformance

The curated parity harness compares selected behavior against Rust `unicode-bidi` `0.3.18` on every PR.
The larger Unicode conformance harness uses Unicode 18.0.0 test data. Its version comes from `UnicodeBidiNetUnicodeVersion` in `Directory.Build.props`, shared with the character-data generator and package metadata.

## Fetch Data

```bash
python3 tools/conformance/fetch_unicode_bidi_tests.py
```

This downloads these files to `artifacts/unicode-18.0.0/`:

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
- `results.rows.jsonl`
- `failures.jsonl`
- `summary.md`

## GitHub Actions

The regular CI workflow runs the curated Rust parity suite and the Python generator unit tests.
The **Unicode Conformance** workflow verifies that the checked-in tables can be regenerated from the official UCD files and runs the full `both` corpus on pull requests, pushes to `main`, and weekly on the default branch. Manual runs can select a bounded sample or the full selected corpus.

The full `both` run uses `tools/conformance/full-ratchet-baseline.json`, which currently requires all 861,948 Unicode 18.0.0 cases and zero failures.

Unicode 18 character data intentionally differs from the Rust reference's Unicode 16 data. Curated Rust parity remains a check of shared behavior, while the UCD and its conformance corpus define the current character-data contract. See [maintenance](maintenance.md).
