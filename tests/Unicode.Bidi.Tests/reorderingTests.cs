/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi reordering tests with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/lib.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

namespace Unicode.Bidi.Tests;

public class reorderingTests
{
    [Fact]
    public void ReorderVisual_MatchesRustExamples()
    {
        Assert.Equal([0, 1, 2, 3], BidiInfo.ReorderVisual(ToLevels([0, 0, 0, 0])));
        Assert.Equal([0, 1, 2, 6, 7, 5, 4, 3], BidiInfo.ReorderVisual(ToLevels([0, 0, 0, 1, 1, 1, 2, 2])));
    }

    [Fact]
    public void ReorderLine_MatchesRustExamples()
    {
        AssertReorderedParagraphs("abc\ndef\nghi", ["abc\n", "def\n", "ghi"]);
        AssertReorderedParagraphs("abc\n\u0627\u0628\u062C", ["abc\n", "\u062C\u0628\u0627"]);
        AssertReorderedParagraphs("\u0627\u0628\u062C\nabc", ["\n\u062C\u0628\u0627", "abc"]);
        AssertReorderedParagraphs("abc \u05D0\u05D1\u05D2", ["abc \u05D2\u05D1\u05D0"]);
        AssertReorderedParagraphs("123 \u05D0\u05D1\u05D2", ["\u05D2\u05D1\u05D0 123"]);
        AssertReorderedParagraphs("\u05D0\u05D1\u05D2? \u05D0\u05D1\u05D2", ["\u05D2\u05D1\u05D0 ?\u05D2\u05D1\u05D0"]);
        AssertReorderedParagraphs("\u05D0(\u05D1)\u05D2.", [".\u05D2)\u05D1(\u05D0"]);
    }

    [Fact]
    public void ReorderedLevels_ResetsLineWhitespaceAndControls()
    {
        BidiInfo bidiInfo = BidiInfo.New("aa \u05D8\u05B6");
        ParagraphInfo paragraph = Assert.Single(bidiInfo.Paragraphs);

        Level[] levels = bidiInfo.ReorderedLevels(paragraph, new TextRange(1, 4));

        Assert.Equal([0, 0, 0, 1, 1], levels.Select(level => (int)level.Number()));
    }

    [Fact]
    public void ReorderedLevelsPerChar_MapsUtf16ScalarStarts()
    {
        string text = "a" + char.ConvertFromUtf32(0x1F600) + "\u05D0";
        BidiInfo bidiInfo = BidiInfo.New(text);
        ParagraphInfo paragraph = Assert.Single(bidiInfo.Paragraphs);

        Level[] levels = bidiInfo.ReorderedLevelsPerChar(paragraph, paragraph.Range);

        Assert.Equal([0, 0, 1], levels.Select(level => (int)level.Number()));
    }

    [Fact]
    public void VisualRuns_ReturnsRunsInDisplayOrder()
    {
        BidiInfo bidiInfo = BidiInfo.New("abc \u05D0\u05D1\u05D2");
        ParagraphInfo paragraph = Assert.Single(bidiInfo.Paragraphs);

        (_, TextRange[] runs) = bidiInfo.VisualRuns(paragraph, paragraph.Range);

        Assert.Equal([new TextRange(0, 4), new TextRange(4, 7)], runs);
    }

    [Fact]
    public void ParagraphDirectionAndLevelAt_MatchResolvedLevels()
    {
        string text = "hello\n\u0627\u0628\u062C\nhello\u0627";
        BidiInfo bidiInfo = BidiInfo.New(text);

        Assert.Equal(Direction.Ltr, bidiInfo.ParagraphDirection(bidiInfo.Paragraphs[0]));
        Assert.Equal(Direction.Rtl, bidiInfo.ParagraphDirection(bidiInfo.Paragraphs[1]));
        Assert.Equal(Direction.Mixed, bidiInfo.ParagraphDirection(bidiInfo.Paragraphs[2]));
        Assert.Equal(Level.Ltr(), bidiInfo.LevelAt(bidiInfo.Paragraphs[0], 0));
        Assert.True(bidiInfo.HasRtl());
    }

    private static void AssertReorderedParagraphs(string text, string[] expected)
    {
        BidiInfo bidiInfo = BidiInfo.New(text);
        string[] actual = bidiInfo.Paragraphs
            .Select(paragraph => bidiInfo.ReorderLine(paragraph, paragraph.Range))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private static Level[] ToLevels(int[] levels)
    {
        return levels.Select(level => Level.New((byte)level)).ToArray();
    }
}
