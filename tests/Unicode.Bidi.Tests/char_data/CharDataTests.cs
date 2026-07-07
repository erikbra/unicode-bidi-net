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
