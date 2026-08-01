# Unicode.Bidi

.NET port of Rust [`unicode-bidi`](https://crates.io/crates/unicode-bidi/0.3.18), tracking crate version `0.3.18` and Unicode data `16.0.0`.

The port intentionally keeps source organization close to the Rust crate so individual files can be compared as the implementation grows:

- `src/Unicode.Bidi/char_data` mirrors `src/char_data`.
- `src/Unicode.Bidi/lib.cs` mirrors the public `BidiInfo` construction and reordering APIs from `src/lib.rs`.
- `src/Unicode.Bidi/level.cs` mirrors `src/level.rs`.
- `src/Unicode.Bidi/format_chars.cs` mirrors `src/format_chars.rs`.
- `src/Unicode.Bidi/explicit.cs`, `prepare.cs`, and `implicit.cs` mirror the matching Rust modules.

## Indexing Policy

.NET strings are UTF-16, so public text ranges and per-code-unit arrays in this port use UTF-16 code-unit indices. The parity harness compares these results against Rust's `unicode_bidi::utf16` module.

## Usage

Install the package as `Unicode.Bidi` and import the root namespace:

```csharp
using Unicode.Bidi;

BidiInfo info = BidiInfo.Create("abc אבג", Level.Ltr());
ParagraphInfo paragraph = info.Paragraphs[0];
string visual = info.ReorderLine(paragraph, paragraph.Range);

BidiClass alefClass = CharData.BidiClass('\u05D0');
Level explicitLevel = Level.CreateExplicit(1);
```

Rust-compatible factory names such as `BidiInfo.New` and `Level.New` are kept for source traceability. .NET-style `Create` aliases are available on the main entry points.

## Local Validation

```bash
dotnet restore UnicodeBidiNet.slnx
dotnet build UnicodeBidiNet.slnx --configuration Release --no-restore
dotnet test UnicodeBidiNet.slnx --configuration Release --no-build --nologo
```

The parity harness compares selected .NET outputs with the original Rust crate:

```bash
./tools/fetch-upstream.sh
python3 tools/parity/run_bidi_parity.py --out-dir artifacts/bidi-parity
```

The parity run requires `cargo` because the Rust probe is built against `unicode-bidi = "=0.3.18"`.
If Rust is not installed globally, use the repo-local bootstrap:

```bash
./tools/bootstrap-local-rust.sh
python3 tools/parity/run_bidi_parity.py --out-dir artifacts/bidi-parity
```

## Unicode Conformance

The larger Unicode 16.0.0 conformance files are downloaded on demand into `artifacts/`:

```bash
python3 tools/conformance/fetch_unicode_bidi_tests.py
python3 tools/conformance/run_unicode_bidi_conformance.py \
  --max-cases 1000 \
  --out-dir artifacts/bidi-conformance \
  --fail-on-regression
```

Use `--full` for a full local/provisioned run. GitHub Actions also provides a manual **Unicode Conformance** workflow and a weekly full-corpus ratchet.

## Performance

The repository contains a BenchmarkDotNet and Criterion comparison harness:

```bash
python3 benchmarks/run_comparison.py
```

Use `--quick` for a short harness check. The manual **Performance benchmarks** GitHub Actions
workflow runs the same command and uploads both native reports plus the normalized comparison.
See `docs/performance.md` for the measured .NET/Rust results and lookup-strategy analysis.

## Packaging

NuGet packages can be produced locally with:

```bash
dotnet pack src/Unicode.Bidi/Unicode.Bidi.csproj --configuration Release --output artifacts/packages
```

Publishing to nuget.org is done through the manual **Publish to NuGet** GitHub Actions workflow. A successful publication also creates a matching GitHub Release with the package artifacts attached. See `docs/publishing.md` for the required NuGet Trusted Publishing policy and publish inputs.

See `docs/api-compatibility.md` and `docs/conformance.md` for the current parity notes and conformance workflow.
