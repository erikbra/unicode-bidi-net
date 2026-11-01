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
    // UCD DerivedBidiClass explicitly assigns BN to noncharacters.
    [InlineData('\uFDD0', BidiClass.BN)]
    [InlineData('\uFDD1', BidiClass.BN)]
    [InlineData('\uFDEE', BidiClass.BN)]
    [InlineData('\uFDEF', BidiClass.BN)]
    [InlineData('\uFFFE', BidiClass.BN)]
    [InlineData('\uFFFF', BidiClass.BN)]
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
    public void UnicodeVersion_TracksUnicodeCharacterDatabase()
    {
        Assert.Equal((18, 0, 0), CharData.UnicodeVersion);
    }

    [Theory]
    [InlineData(0x05C8, BidiClass.NSM)] // Hebrew combining point.
    [InlineData(0x088F, BidiClass.AL)] // Arabic letter.
    [InlineData(0x10ECB, BidiClass.NSM)] // Supplementary Arabic combining mark.
    [InlineData(0x086B, BidiClass.AL)] // Unassigned: regional @missing overrides default L.
    [InlineData(0x10EC0, BidiClass.AL)] // Supplementary regional @missing.
    [InlineData(0x10FFFF, BidiClass.BN)] // Supplementary noncharacter.
    public void BidiClass_UsesCurrentUcdAssignmentsAndDefaults(int scalar, BidiClass expected)
    {
        Assert.Equal(expected, CharData.BidiClass(new Rune(scalar)));
    }

    [Fact]
    public void BidiMatchedOpeningBracket_RecognizesCanonicalEquivalents()
    {
        Assert.Equal(new BidiMatchedOpeningBracket(new Rune(0x3008), true), CharData.BidiMatchedOpeningBracket(new Rune(0x2329)));
        Assert.Equal(new BidiMatchedOpeningBracket(new Rune(0x3008), false), CharData.BidiMatchedOpeningBracket(new Rune(0x232A)));
        Assert.Equal(new BidiMatchedOpeningBracket(new Rune(0x3008), true), CharData.BidiMatchedOpeningBracket(new Rune(0x3008)));
        Assert.Equal(new BidiMatchedOpeningBracket(new Rune(0x3008), false), CharData.BidiMatchedOpeningBracket(new Rune(0x3009)));
    }

    [Fact]
    public void BidiClass_MatchesGeneratedRangesForEveryUnicodeScalar()
    {
        Assert.All(Enum.GetValues<BidiClass>(), value => Assert.InRange((int)value, 0, byte.MaxValue));

        int rangeIndex = 0;
        for (int scalar = 0; scalar <= 0x10FFFF; scalar++)
        {
            if (scalar is >= 0xD800 and <= 0xDFFF)
            {
                continue;
            }

            while (rangeIndex < Tables.BidiClassTable.Length
                && scalar > Tables.BidiClassTable[rangeIndex].Hi)
            {
                rangeIndex++;
            }

            BidiClass expected = rangeIndex < Tables.BidiClassTable.Length
                && scalar >= Tables.BidiClassTable[rangeIndex].Lo
                    ? Tables.BidiClassTable[rangeIndex].Class
                    : BidiClass.L;
            BidiClass actual = CharData.BidiClass(new Rune(scalar));
            if (actual != expected)
            {
                Assert.Fail($"U+{scalar:X} expected {expected}, but got {actual}.");
            }
        }
    }
}
