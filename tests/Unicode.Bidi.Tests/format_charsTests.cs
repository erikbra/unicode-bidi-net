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
