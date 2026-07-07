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
    public void TestAscii(char c, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(c));
    }

    [Theory]
    [InlineData('\u0590', BidiClass.R)]
    [InlineData('\u05D0', BidiClass.R)]
    [InlineData('\u05D1', BidiClass.R)]
    [InlineData('\u05FF', BidiClass.R)]
    [InlineData('\u0600', BidiClass.AN)]
    [InlineData('\u0627', BidiClass.AL)]
    [InlineData('\u07BF', BidiClass.AL)]
    [InlineData('\u07C0', BidiClass.R)]
    [InlineData('\u085F', BidiClass.R)]
    [InlineData('\u0860', BidiClass.AL)]
    [InlineData('\u0870', BidiClass.AL)]
    [InlineData('\u089F', BidiClass.NSM)]
    [InlineData('\u08A0', BidiClass.AL)]
    [InlineData('\u08FF', BidiClass.NSM)]
    [InlineData('\u20A0', BidiClass.ET)]
    [InlineData('\u20CF', BidiClass.ET)]
    [InlineData('\uFB1D', BidiClass.R)]
    [InlineData('\uFB4F', BidiClass.R)]
    [InlineData('\uFB50', BidiClass.AL)]
    [InlineData('\uFDCF', BidiClass.ON)]
    [InlineData('\uFDF0', BidiClass.AL)]
    [InlineData('\uFDFF', BidiClass.ON)]
    [InlineData('\uFE70', BidiClass.AL)]
    [InlineData('\uFEFE', BidiClass.AL)]
    [InlineData('\uFEFF', BidiClass.BN)]
    [InlineData('\uFDD0', BidiClass.L)]
    [InlineData('\uFDD1', BidiClass.L)]
    [InlineData('\uFDEE', BidiClass.L)]
    [InlineData('\uFDEF', BidiClass.L)]
    [InlineData('\uFFFE', BidiClass.L)]
    [InlineData('\uFFFF', BidiClass.L)]
    public void TestBmp(char c, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(c));
    }

    [Theory]
    [InlineData(0x10800, BidiClass.R)]
    [InlineData(0x10FFF, BidiClass.R)]
    [InlineData(0x1E800, BidiClass.R)]
    [InlineData(0x1EDFF, BidiClass.R)]
    [InlineData(0x1EE00, BidiClass.AL)]
    [InlineData(0x1EEFF, BidiClass.AL)]
    [InlineData(0x1EF00, BidiClass.R)]
    [InlineData(0x1EFFF, BidiClass.R)]
    public void TestSmp(int scalar, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(new Rune(scalar)));
    }

    [Theory]
    [InlineData(0x30000, BidiClass.L)]
    [InlineData(0x40000, BidiClass.L)]
    [InlineData(0x50000, BidiClass.L)]
    [InlineData(0x60000, BidiClass.L)]
    [InlineData(0x70000, BidiClass.L)]
    [InlineData(0x80000, BidiClass.L)]
    [InlineData(0x90000, BidiClass.L)]
    [InlineData(0xA0000, BidiClass.L)]
    public void TestUnassignedPlanes(int scalar, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(new Rune(scalar)));
    }

    [Fact]
    public void UnicodeVersion_TracksUpstreamCrateData()
    {
        Assert.Equal((16, 0, 0), CharData.UnicodeVersion);
    }
}
