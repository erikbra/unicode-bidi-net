/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi constants with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/format_chars.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

namespace Unicode.Bidi.Tests;

public class format_charsTests
{
    [Fact]
    public void DirectionalFormattingCharacters_MatchRustConstants()
    {
        Assert.Equal('\u061C', FormatChars.ALM);
        Assert.Equal('\u200E', FormatChars.LRM);
        Assert.Equal('\u200F', FormatChars.RLM);
        Assert.Equal('\u2066', FormatChars.LRI);
        Assert.Equal('\u2067', FormatChars.RLI);
        Assert.Equal('\u2068', FormatChars.FSI);
        Assert.Equal('\u2069', FormatChars.PDI);
        Assert.Equal('\u202A', FormatChars.LRE);
        Assert.Equal('\u202B', FormatChars.RLE);
        Assert.Equal('\u202C', FormatChars.PDF);
        Assert.Equal('\u202D', FormatChars.LRO);
        Assert.Equal('\u202E', FormatChars.RLO);
    }
}
