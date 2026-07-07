# Porting Plan

The port is organized as sequential GitHub issues so each PR can stay focused and easy to compare against Rust `unicode-bidi` `0.3.18`.

1. [#1](https://github.com/erikbra/unicode-bidi-net/issues/1) Bootstrap the .NET solution, CI, upstream fetch, Rust-vs-.NET parity harness, `char_data`, `level`, and `format_chars`.
2. [#2](https://github.com/erikbra/unicode-bidi-net/issues/2) Port paragraph discovery, `InitialInfo`, and public `BidiInfo` construction from `lib.rs`.
3. [#3](https://github.com/erikbra/unicode-bidi-net/issues/3) Port explicit level resolution from `explicit.rs` and isolating run preparation from `prepare.rs`.
4. [#4](https://github.com/erikbra/unicode-bidi-net/issues/4) Port weak, neutral, and implicit level resolution from `implicit.rs`.
5. [#5](https://github.com/erikbra/unicode-bidi-net/issues/5) Port line reordering, visual runs, and display APIs.
6. [#6](https://github.com/erikbra/unicode-bidi-net/issues/6) Add full conformance corpus, parity ratchets, package polish, and API compatibility review.

## Organization Rules

- Prefer file names that match Rust modules when that makes tracking easier.
- Use UTF-16 code-unit indices for .NET public ranges and compare them to Rust `unicode_bidi::utf16` APIs.
- Keep generated Unicode data in `char_data/tables.g.cs`.
- Keep harnesses under `tools/parity` and make them runnable outside CI.
- Track accepted divergences with issue ownership instead of broad allowances.
