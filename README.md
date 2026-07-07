# Unicode.Bidi

.NET port of Rust [`unicode-bidi`](https://crates.io/crates/unicode-bidi/0.3.18), tracking crate version `0.3.18` and Unicode data `16.0.0`.

The port intentionally keeps source organization close to the Rust crate so individual files can be compared as the implementation grows:

- `src/Unicode.Bidi/char_data` mirrors `src/char_data`.
- `src/Unicode.Bidi/lib.cs` mirrors the initial-info and paragraph-construction parts of `src/lib.rs`.
- `src/Unicode.Bidi/level.cs` mirrors `src/level.rs`.
- `src/Unicode.Bidi/format_chars.cs` mirrors `src/format_chars.rs`.
- Future issues will add `explicit`, `implicit`, `prepare`, and the remaining `lib.rs` public API in the same style.

## Indexing Policy

.NET strings are UTF-16, so public text ranges and per-code-unit arrays in this port use UTF-16 code-unit indices. The parity harness compares these results against Rust's `unicode_bidi::utf16` module.

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
