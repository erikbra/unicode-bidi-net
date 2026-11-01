# API Compatibility Review

This project's algorithm tracks Rust `unicode-bidi` `0.3.18`, with .NET APIs shaped around UTF-16 strings. From package `0.4.0`, character data follows Unicode `18.0.0` directly; see [maintenance](maintenance.md) for the intentional data differences.

## Implemented Surface

- `BidiClass`, `CharData`, and Unicode 18.0.0 hardcoded bidi data under the root `Unicode.Bidi` namespace.
- `Level` creation, level arithmetic, direction helpers, and .NET-style `Value`, `IsLeftToRight`, and `IsRightToLeft` properties.
- `Level.Create` and `Level.CreateExplicit` as idiomatic .NET aliases for `Level.New` and `Level.NewExplicit`.
- Unicode formatting character constants.
- `InitialInfo.New`.
- `InitialInfo.Create` as an idiomatic .NET alias for `InitialInfo.New`.
- `BidiInfo.New`.
- `BidiInfo.Create` as an idiomatic .NET alias for `BidiInfo.New`.
- `ParagraphBidiInfo.New`.
- `ParagraphBidiInfo.Create` as an idiomatic .NET alias for `ParagraphBidiInfo.New`.
- `Bidi.GetBaseDirection`.
- `Bidi.GetBaseDirectionFull`.
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
- Public character-data APIs live in `Unicode.Bidi`, even though their files stay under `src/Unicode.Bidi/char_data` to preserve Rust source traceability.
- The package targets `net10.0` during the initial preview so it can follow the workspace/tooling used for the port.
- Custom `BidiDataSource`, `no_std`, `serde`, and deprecated Rust modules are not ported. These are Rust ecosystem features rather than .NET package requirements.
- Rule L3 combining-mark shaping and L4 glyph mirroring are not performed by `ReorderLine`, matching the upstream crate's documented display API boundary.

## Review Decisions

- Keep Rust-style `New` factory names as the traceable primary API.
- Add `Create` aliases for `InitialInfo`, `BidiInfo`, `ParagraphBidiInfo`, and `Level`, where .NET callers naturally enter the library.
- Keep internal generated/support data in the lowercase Rust-style `char_data` namespace, but do not expose that namespace as the expected consumer API.
- Include the single-paragraph `ParagraphBidiInfo` API because upstream unit-test parity exercises it directly.
- Keep `net10.0` for the preview package and revisit broader target frameworks once the consuming PdfBox.Net integration proves the required baseline.
- Ship XML documentation for the public preview surface.
