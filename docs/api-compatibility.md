# API Compatibility Review

This project tracks Rust `unicode-bidi` `0.3.18`, with .NET APIs shaped around UTF-16 strings.

## Implemented Surface

- `BidiClass` and Unicode 16.0.0 hardcoded bidi data.
- `Level` creation, level arithmetic, and direction helpers.
- Unicode formatting character constants.
- `InitialInfo.New`.
- `InitialInfo.Create` as an idiomatic .NET alias for `InitialInfo.New`.
- `BidiInfo.New`.
- `BidiInfo.Create` as an idiomatic .NET alias for `BidiInfo.New`.
- Explicit, prepare, weak, neutral, implicit, and level resolution stages.
- `BidiInfo.ReorderedLevels`.
- `BidiInfo.ReorderedLevelsPerChar`.
- `BidiInfo.ReorderLine`.
- `BidiInfo.ReorderVisual`.
- `BidiInfo.VisualRuns`.
- Paragraph direction, level lookup, and RTL detection helpers.

## Intentional .NET Differences

- Public text is `string`, and all ranges are UTF-16 code-unit ranges. This matches Rust `unicode_bidi::utf16`, not Rust's UTF-8 `str` API.
- `TextRange` replaces Rust `Range<usize>` in public APIs.
- The port exposes .NET arrays and named tuples where Rust returns `Vec` and tuple values.
- The package targets `net10.0` during the initial preview so it can follow the workspace/tooling used for the port.
- Custom `BidiDataSource`, `no_std`, `serde`, and deprecated Rust modules are not ported. These are Rust ecosystem features rather than .NET package requirements.
- Rule L3 combining-mark shaping and L4 glyph mirroring are not performed by `ReorderLine`, matching the upstream crate's documented display API boundary.

## Review Decisions

- Keep Rust-style `New` factory names as the traceable primary API.
- Add `Create` aliases only for `InitialInfo` and `BidiInfo`, where .NET callers naturally enter the library.
- Defer a single-paragraph convenience type comparable to Rust `ParagraphBidiInfo`; the current `BidiInfo` plus `ParagraphInfo` surface already preserves paragraph metadata without introducing another ownership shape.
- Keep `net10.0` for the preview package and revisit broader target frameworks once the consuming PdfBox.Net integration proves the required baseline.
- Ship XML documentation for the public preview surface.
