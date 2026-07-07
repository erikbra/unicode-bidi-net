/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi inline tests with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

using System.Text;
using Unicode.Bidi.char_data;

namespace Unicode.Bidi.Tests.char_data;

public class CharDataTests
{
    [Theory]
    [InlineData('\u0000', BidiClass.BN)]
    [InlineData('\u0040', BidiClass.ON)]
    [InlineData('\u0041', BidiClass.L)]
    [InlineData('\u0062', BidiClass.L)]
    [InlineData('\u007F', BidiClass.BN)]
    [InlineData('\u0590', BidiClass.R)]
    [InlineData('\u05D0', BidiClass.R)]
    [InlineData('\u0600', BidiClass.AN)]
    [InlineData('\u0627', BidiClass.AL)]
    [InlineData('\u20A0', BidiClass.ET)]
    [InlineData('\uFEFF', BidiClass.BN)]
    [InlineData('\uFDD0', BidiClass.L)]
    public void BidiClass_MatchesRustInlineTestsForBmpCharacters(char c, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(c));
    }

    [Theory]
    [InlineData(0x10800, BidiClass.R)]
    [InlineData(0x10FFF, BidiClass.R)]
    [InlineData(0x1E800, BidiClass.R)]
    [InlineData(0x1EE00, BidiClass.AL)]
    [InlineData(0x30000, BidiClass.L)]
    public void BidiClass_MatchesRustInlineTestsForSupplementaryScalars(int scalar, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(new Rune(scalar)));
    }

    [Fact]
    public void UnicodeVersion_TracksUpstreamCrateData()
    {
        Assert.Equal((16, 0, 0), CharData.UnicodeVersion);
    }
}
