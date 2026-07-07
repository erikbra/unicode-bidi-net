/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi lib tests with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/lib.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

namespace Unicode.Bidi.Tests;

public class libTests
{
    [Fact]
    public void TestUtf16TextSource()
    {
        string text = new(
        [
            '\u0041', '\uD801', '\uDC01', '\u0020', '\uD800',
            '\u0020', '\uDFFF', '\u0020', '\uDC00', '\uD800',
        ]);

        InitialInfo.TextScalar[] scalars = InitialInfo.EnumerateScalars(text).ToArray();

        AssertScalar(scalars, 0, 0, 1, 0x41);
        AssertScalar(scalars, 1, 1, 2, 0x10401);
        Assert.DoesNotContain(scalars, scalar => scalar.CodeUnitIndex == 2);
        AssertScalar(scalars, 2, 3, 1, 0x20);
        AssertScalar(scalars, 3, 4, 1, 0xFFFD);
        AssertScalar(scalars, 4, 5, 1, 0x20);
        AssertScalar(scalars, 5, 6, 1, 0xFFFD);
        AssertScalar(scalars, 6, 7, 1, 0x20);
        AssertScalar(scalars, 7, 8, 1, 0xFFFD);
        AssertScalar(scalars, 8, 9, 1, 0xFFFD);
    }

    [Fact]
    public void TestUtf16CharIter()
    {
        string text = new(
        [
            '\u0041', '\uD801', '\uDC01', '\u0020', '\uD800',
            '\u0020', '\uDFFF', '\u0020', '\uDC00', '\uD800',
        ]);

        Assert.Equal(10, text.Length);
        Assert.Equal(
            [0x41, 0x10401, 0x20, 0xFFFD, 0x20, 0xFFFD, 0x20, 0xFFFD, 0xFFFD],
            InitialInfo.EnumerateScalars(text).Select(scalar => scalar.Rune.Value).ToArray());
    }

    [Fact]
    public void TestInitialTextInfo()
    {
        AssertInitialInfo(
            "a1",
            [BidiClass.L, BidiClass.EN],
            [new ParagraphInfo(new TextRange(0, 2), Level.Ltr())]);

        AssertInitialInfo(
            "\u0639 \u05D0",
            [BidiClass.AL, BidiClass.WS, BidiClass.R],
            [new ParagraphInfo(new TextRange(0, 3), Level.Rtl())]);

        AssertInitialInfo(
            CodePoints(0x10A00, 0x12000, 0x1E900),
            [BidiClass.R, BidiClass.R, BidiClass.L, BidiClass.L, BidiClass.R, BidiClass.R],
            [new ParagraphInfo(new TextRange(0, 6), Level.Rtl())]);

        AssertInitialInfo(
            "a\u2029b",
            [BidiClass.L, BidiClass.B, BidiClass.L],
            [
                new ParagraphInfo(new TextRange(0, 2), Level.Ltr()),
                new ParagraphInfo(new TextRange(2, 3), Level.Ltr()),
            ]);

        AssertInitialInfo(
            "\u2068\u05D0\u2069a",
            [BidiClass.RLI, BidiClass.R, BidiClass.PDI, BidiClass.L],
            [new ParagraphInfo(new TextRange(0, 4), Level.Ltr())]);
    }

    [Fact]
    public void TestProcessText()
    {
        AssertBidiInfo(
            "",
            Level.Rtl(),
            [],
            [],
            []);

        AssertBidiInfo(
            "abc123",
            Level.Ltr(),
            [0, 0, 0, 0, 0, 0],
            [BidiClass.L, BidiClass.L, BidiClass.L, BidiClass.EN, BidiClass.EN, BidiClass.EN],
            [new ParagraphInfo(new TextRange(0, 6), Level.Ltr())]);

        AssertBidiInfo(
            "abc \u05D0\u05D1\u05D2",
            Level.Ltr(),
            [0, 0, 0, 0, 1, 1, 1],
            [BidiClass.L, BidiClass.L, BidiClass.L, BidiClass.WS, BidiClass.R, BidiClass.R, BidiClass.R],
            [new ParagraphInfo(new TextRange(0, 7), Level.Ltr())]);

        AssertBidiInfo(
            "abc \u05D0\u05D1\u05D2",
            Level.Rtl(),
            [2, 2, 2, 1, 1, 1, 1],
            [BidiClass.L, BidiClass.L, BidiClass.L, BidiClass.WS, BidiClass.R, BidiClass.R, BidiClass.R],
            [new ParagraphInfo(new TextRange(0, 7), Level.Rtl())]);

        AssertBidiInfo(
            "\u05D0\u05D1\u05D2 abc",
            Level.Ltr(),
            [1, 1, 1, 0, 0, 0, 0],
            [BidiClass.R, BidiClass.R, BidiClass.R, BidiClass.WS, BidiClass.L, BidiClass.L, BidiClass.L],
            [new ParagraphInfo(new TextRange(0, 7), Level.Ltr())]);

        AssertBidiInfo(
            "\u05D0\u05D1\u05D2 abc",
            null,
            [1, 1, 1, 1, 2, 2, 2],
            [BidiClass.R, BidiClass.R, BidiClass.R, BidiClass.WS, BidiClass.L, BidiClass.L, BidiClass.L],
            [new ParagraphInfo(new TextRange(0, 7), Level.Rtl())]);

        AssertBidiInfo(
            "\u063A2\u0638 \u05D02\u05D2",
            Level.Ltr(),
            [1, 2, 1, 1, 1, 2, 1],
            [BidiClass.AL, BidiClass.EN, BidiClass.AL, BidiClass.WS, BidiClass.R, BidiClass.EN, BidiClass.R],
            [new ParagraphInfo(new TextRange(0, 7), Level.Ltr())]);

        AssertBidiInfo(
            "a \u05D0.\n\u05D2",
            null,
            [0, 0, 1, 0, 0, 1],
            [BidiClass.L, BidiClass.WS, BidiClass.R, BidiClass.CS, BidiClass.B, BidiClass.R],
            [
                new ParagraphInfo(new TextRange(0, 5), Level.Ltr()),
                new ParagraphInfo(new TextRange(5, 6), Level.Rtl()),
            ]);

        AssertBidiInfo(
            "\u060B\u20CF\u06F9",
            null,
            [1, 1, 2],
            [BidiClass.AL, BidiClass.ET, BidiClass.EN],
            [new ParagraphInfo(new TextRange(0, 3), Level.Rtl())]);
    }

    [Fact]
    public void TestParagraphBidiInfo()
    {
        AssertParagraphBidiInfo(
            "",
            Level.Rtl(),
            [],
            [],
            Level.Rtl(),
            isPureLtr: true);

        AssertParagraphBidiInfo(
            "a \u05D0.\n\u05D2",
            null,
            [BidiClass.L, BidiClass.WS, BidiClass.R, BidiClass.CS, BidiClass.B, BidiClass.R],
            [0, 0, 1, 1, 1, 1],
            Level.Ltr(),
            isPureLtr: false);

        AssertParagraphBidiInfo(
            "\u05D1 a.\nb.",
            null,
            [BidiClass.R, BidiClass.WS, BidiClass.L, BidiClass.CS, BidiClass.B, BidiClass.L, BidiClass.CS],
            [1, 1, 2, 2, 2, 2, 1],
            Level.Rtl(),
            isPureLtr: false);

        AssertParagraphBidiInfo(
            "a \u05D0.\t\u05D2",
            null,
            [BidiClass.L, BidiClass.WS, BidiClass.R, BidiClass.CS, BidiClass.S, BidiClass.R],
            [0, 0, 1, 1, 1, 1],
            Level.Ltr(),
            isPureLtr: false);

        AssertParagraphBidiInfo(
            "\u05D1 a.\tb.",
            null,
            [BidiClass.R, BidiClass.WS, BidiClass.L, BidiClass.CS, BidiClass.S, BidiClass.L, BidiClass.CS],
            [1, 1, 2, 2, 2, 2, 1],
            Level.Rtl(),
            isPureLtr: false);
    }

    [Theory]
    [InlineData("123", null, false)]
    [InlineData("123", 0, false)]
    [InlineData("123", 1, false)]
    [InlineData("abc", null, false)]
    [InlineData("abc", 0, false)]
    [InlineData("abc", 1, false)]
    [InlineData("abc 123", null, false)]
    [InlineData("abc\n123", null, false)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2", null, true)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2", 0, true)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2", 1, true)]
    [InlineData("abc \u05D0\u05D1\u05BC\u05D2", null, true)]
    [InlineData("abc\n\u05D0\u05D1\u05BC\u05D2", null, true)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2 abc", null, true)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2\nabc", null, true)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2 123", null, true)]
    [InlineData("\u05D0\u05D1\u05BC\u05D2\n123", null, true)]
    public void TestBidiInfoHasRtl(string text, int? defaultLevel, bool expected)
    {
        Assert.Equal(expected, BidiInfo.New(text, ToLevel(defaultLevel)).HasRtl());
    }

    [Fact]
    public void TestReorderLine()
    {
        AssertReorderedParagraphs("abc\ndef\nghi", ["abc\n", "def\n", "ghi"]);
        AssertReorderedParagraphs("ab1\nde2\ngh3", ["ab1\n", "de2\n", "gh3"]);
        AssertReorderedParagraphs("abc\n\u0627\u0628\u062C", ["abc\n", "\u062C\u0628\u0627"]);
        AssertReorderedParagraphs("\u0627\u0628\u062C\nabc", ["\n\u062C\u0628\u0627", "abc"]);
        AssertReorderedParagraphs("1.-2", ["1.-2"]);
        AssertReorderedParagraphs("1-.2", ["1-.2"]);
        AssertReorderedParagraphs("abc \u05D0\u05D1\u05D2", ["abc \u05D2\u05D1\u05D0"]);
        AssertReorderedParagraphs("123 \u05D0\u05D1\u05D2", ["\u05D2\u05D1\u05D0 123"]);
        AssertReorderedParagraphs("abc\u202Adef", ["abc\u202Adef"]);
        AssertReorderedParagraphs("abc\u202Adef\u202Cghi", ["abc\u202Adef\u202Cghi"]);
        AssertReorderedParagraphs("abc\u2066def\u2069ghi", ["abc\u2066def\u2069ghi"]);
        AssertReorderedParagraphs("\u202Babc \u05D0\u05D1\u05D2\u202C", ["\u202B\u05D2\u05D1\u05D0 abc\u202C"]);
        AssertReorderedParagraphs("\u05D0\u05D1\u05D2? \u05D0\u05D1\u05D2", ["\u05D2\u05D1\u05D0 ?\u05D2\u05D1\u05D0"]);
        AssertReorderedParagraphs("A \u05D0\u05D1\u05D2?", ["A \u05D2\u05D1\u05D0?"]);
        AssertReorderedParagraphs("A \u05D0\u05D1\u05D2?\u200F", ["A \u200F?\u05D2\u05D1\u05D0"]);
        AssertReorderedParagraphs("\u05D0\u05D1\u05D2 abc", ["abc \u05D2\u05D1\u05D0"]);
        AssertReorderedParagraphs("abc\u2067.-\u2069ghi", ["abc\u2067-.\u2069ghi"]);
        AssertReorderedParagraphs("Hello, \u2068\u202Eworld\u202C\u2069!", ["Hello, \u2068\u202E\u202Cdlrow\u2069!"]);
        AssertReorderedParagraphs("\u05D0(\u05D1)\u05D2.", [".\u05D2)\u05D1(\u05D0"]);
        AssertReorderedParagraphs("\u05D0\u05D1(\u05D2\u05D3[&ef].)gh", ["gh).]ef&[\u05D3\u05D2(\u05D1\u05D0"]);
    }

    [Fact]
    public void TestReorderedLevelsRange()
    {
        string text = "\u202AA\u202C\u202AA\u202C";
        TextRange range = new(2, 5);
        BidiInfo bidiInfo = BidiInfo.New(text);

        (_, TextRange[] runs) = bidiInfo.VisualRuns(bidiInfo.Paragraphs[0], range);

        Assert.All(runs, run =>
        {
            Assert.True(run.Start >= range.Start);
            Assert.True(run.End <= range.End);
            _ = text.Substring(run.Start, run.Length);
        });
    }

    [Fact]
    public void TestReorderedLevels()
    {
        AssertReorderedLevels(
            "\u2067\u2069",
            [[0, 0]],
            [[0, 0]]);

        AssertReorderedLevels(
            "\u060B\u20CF\u06F9",
            [[1, 1, 2]],
            [[1, 1, 2]]);

        AssertReorderedLevels(
            "\u0605\u2067\u202C\u0590",
            [["2", "0", "x", "1"]],
            [["2", "0", "x", "1"]]);

        string text = "aa \u05D8\u05B6";
        BidiInfo bidiInfo = BidiInfo.New(text);
        Assert.Equal(
            ToLevels([0, 0, 0, 1, 1]),
            bidiInfo.ReorderedLevels(bidiInfo.Paragraphs[0], new TextRange(1, 4)));
    }

    [Fact]
    public void TestParagraphInfoLen()
    {
        string text = "hello world";
        BidiInfo bidiInfo = BidiInfo.New(text);
        Assert.Single(bidiInfo.Paragraphs);
        Assert.Equal(text.Length, bidiInfo.Paragraphs[0].Len());

        string text2 = "How are you";
        string wholeText = $"{text}\n{text2}";
        bidiInfo = BidiInfo.New(wholeText);
        Assert.Equal(2, bidiInfo.Paragraphs.Count);
        Assert.Equal(text.Length + 1, bidiInfo.Paragraphs[0].Len());
        Assert.Equal(text2.Length, bidiInfo.Paragraphs[1].Len());
    }

    [Fact]
    public void TestDirection()
    {
        string ltrText = "hello world";
        string rtlText = "\u0623\u0647\u0644\u0627 \u0628\u0643\u0645";
        string allParagraphs = $"{ltrText}\n{rtlText}\n{ltrText}{rtlText}";
        BidiInfo bidiInfo = BidiInfo.New(allParagraphs);

        Assert.Equal(3, bidiInfo.Paragraphs.Count);
        Assert.Equal(Direction.Ltr, bidiInfo.ParagraphDirection(bidiInfo.Paragraphs[0]));
        Assert.Equal(Direction.Rtl, bidiInfo.ParagraphDirection(bidiInfo.Paragraphs[1]));
        Assert.Equal(Direction.Mixed, bidiInfo.ParagraphDirection(bidiInfo.Paragraphs[2]));
    }

    [Fact]
    public void TestEdgeCasesDirection()
    {
        BidiInfo empty = BidiInfo.New(string.Empty, Level.Rtl());
        Assert.Empty(empty.Paragraphs);

        AssertParagraphDirection("\n", null, Direction.Ltr);
        AssertParagraphDirection("\n", Level.Ltr(), Direction.Ltr);
        AssertParagraphDirection("\n", Level.Rtl(), Direction.Rtl);
    }

    [Fact]
    public void TestLevelAt()
    {
        string ltrText = "hello world";
        string rtlText = "\u0623\u0647\u0644\u0627 \u0628\u0643\u0645";
        string allParagraphs = $"{ltrText}\n{rtlText}\n{ltrText}{rtlText}";
        BidiInfo bidiInfo = BidiInfo.New(allParagraphs);

        Assert.Equal(3, bidiInfo.Paragraphs.Count);
        ParagraphInfo pLtr = bidiInfo.Paragraphs[0];
        ParagraphInfo pRtl = bidiInfo.Paragraphs[1];
        ParagraphInfo pMixed = bidiInfo.Paragraphs[2];

        Assert.Equal(Level.Ltr(), bidiInfo.LevelAt(pLtr, 0));
        Assert.Equal(Level.Rtl(), bidiInfo.LevelAt(pRtl, 0));
        Assert.Equal(Level.Ltr(), bidiInfo.LevelAt(pMixed, 0));
        Assert.Equal(40, bidiInfo.Levels.Count);
        Assert.Equal(21, pMixed.Range.Start);
        Assert.Equal(Level.Rtl(), bidiInfo.LevelAt(pMixed, ltrText.Length));
    }

    [Theory]
    [InlineData("", Direction.Mixed)]
    [InlineData("123[]-+\u2019\u2060\u00BF?", Direction.Mixed)]
    [InlineData("3.14\npi", Direction.Mixed)]
    [InlineData("[123 'abc']", Direction.Ltr)]
    [InlineData("[123 '\u0628' abc", Direction.Rtl)]
    [InlineData("[123 '\u2066abc\u2069'\u0628]", Direction.Rtl)]
    [InlineData("[123 '\u2066abc\u2068'\u0628]", Direction.Mixed)]
    public void TestGetBaseDirection(string text, Direction expected)
    {
        Assert.Equal(expected, Bidi.GetBaseDirection(text));
    }

    [Theory]
    [InlineData("", Direction.Mixed)]
    [InlineData("123[]-+\u2019\u2060\u00BF?", Direction.Mixed)]
    [InlineData("3.14\npi", Direction.Ltr)]
    [InlineData("3.14\n\u05D0", Direction.Rtl)]
    [InlineData("[123 'abc']", Direction.Ltr)]
    [InlineData("[123 '\u0628' abc", Direction.Rtl)]
    [InlineData("[123 '\u2066abc\u2069'\u0628]", Direction.Rtl)]
    [InlineData("[123 '\u2066abc\u2068'\u0628]", Direction.Mixed)]
    [InlineData("[123 '\u2066abc\u2068'\n\u0628]", Direction.Rtl)]
    public void TestGetBaseDirectionFull(string text, Direction expected)
    {
        Assert.Equal(expected, Bidi.GetBaseDirectionFull(text));
    }

    private static void AssertScalar(
        InitialInfo.TextScalar[] scalars,
        int ordinal,
        int expectedCodeUnitIndex,
        int expectedCodeUnitLength,
        int expectedScalar)
    {
        Assert.Equal(expectedCodeUnitIndex, scalars[ordinal].CodeUnitIndex);
        Assert.Equal(expectedCodeUnitLength, scalars[ordinal].CodeUnitLength);
        Assert.Equal(expectedScalar, scalars[ordinal].Rune.Value);
    }

    private static void AssertInitialInfo(
        string text,
        BidiClass[] expectedClasses,
        ParagraphInfo[] expectedParagraphs)
    {
        InitialInfo initialInfo = InitialInfo.New(text);

        Assert.Equal(text, initialInfo.Text);
        Assert.Equal(expectedClasses, initialInfo.OriginalClasses);
        Assert.Equal(expectedParagraphs, initialInfo.Paragraphs);
    }

    private static void AssertBidiInfo(
        string text,
        Level? defaultLevel,
        int[] expectedLevels,
        BidiClass[] expectedClasses,
        ParagraphInfo[] expectedParagraphs)
    {
        BidiInfo bidiInfo = BidiInfo.New(text, defaultLevel);

        Assert.Equal(text, bidiInfo.Text);
        Assert.Equal(ToLevels(expectedLevels), bidiInfo.Levels);
        Assert.Equal(expectedClasses, bidiInfo.OriginalClasses);
        Assert.Equal(expectedParagraphs, bidiInfo.Paragraphs);
    }

    private static void AssertParagraphBidiInfo(
        string text,
        Level? defaultLevel,
        BidiClass[] expectedClasses,
        int[] expectedLevels,
        Level expectedParagraphLevel,
        bool isPureLtr)
    {
        ParagraphBidiInfo bidiInfo = ParagraphBidiInfo.New(text, defaultLevel);

        Assert.Equal(text, bidiInfo.Text);
        Assert.Equal(expectedClasses, bidiInfo.OriginalClasses);
        Assert.Equal(ToLevels(expectedLevels), bidiInfo.Levels);
        Assert.Equal(expectedParagraphLevel, bidiInfo.ParagraphLevel);
        Assert.Equal(isPureLtr, bidiInfo.IsPureLtr);
    }

    private static void AssertReorderedParagraphs(string text, string[] expected)
    {
        BidiInfo bidiInfo = BidiInfo.New(text);
        string[] actual = bidiInfo.Paragraphs
            .Select(paragraph => bidiInfo.ReorderLine(paragraph, paragraph.Range))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private static void AssertReorderedLevels(
        string text,
        int[][] expectedLevels,
        int[][] expectedPerCharLevels)
    {
        AssertReorderedLevels(
            text,
            expectedLevels.Select(levels => levels.Select(level => level.ToString()).ToArray()).ToArray(),
            expectedPerCharLevels.Select(levels => levels.Select(level => level.ToString()).ToArray()).ToArray());
    }

    private static void AssertReorderedLevels(
        string text,
        string[][] expectedLevels,
        string[][] expectedPerCharLevels)
    {
        BidiInfo bidiInfo = BidiInfo.New(text);
        string[][] actualLevels = bidiInfo.Paragraphs
            .Select(paragraph => FormatLevelExpectations(
                bidiInfo.ReorderedLevels(paragraph, paragraph.Range),
                bidiInfo.OriginalClasses))
            .ToArray();
        string[][] actualPerCharLevels = bidiInfo.Paragraphs
            .Select(paragraph => FormatLevelExpectationsPerChar(
                text,
                bidiInfo.ReorderedLevels(paragraph, paragraph.Range),
                bidiInfo.OriginalClasses))
            .ToArray();

        Assert.Equal(expectedLevels, actualLevels);
        Assert.Equal(expectedPerCharLevels, actualPerCharLevels);
    }

    private static string[] FormatLevelExpectations(IReadOnlyList<Level> levels, IReadOnlyList<BidiClass> classes)
    {
        return levels
            .Select((level, index) => Prepare.RemovedByX9(classes[index]) ? "x" : level.Number().ToString())
            .ToArray();
    }

    private static string[] FormatLevelExpectationsPerChar(
        string text,
        IReadOnlyList<Level> levels,
        IReadOnlyList<BidiClass> classes)
    {
        return InitialInfo.EnumerateScalars(text)
            .Select(scalar => Prepare.RemovedByX9(classes[scalar.CodeUnitIndex])
                ? "x"
                : levels[scalar.CodeUnitIndex].Number().ToString())
            .ToArray();
    }

    private static void AssertParagraphDirection(string text, Level? defaultLevel, Direction expected)
    {
        BidiInfo bidiInfo = BidiInfo.New(text, defaultLevel);
        ParagraphInfo paragraph = Assert.Single(bidiInfo.Paragraphs);

        Assert.Equal(expected, bidiInfo.ParagraphDirection(paragraph));
    }

    private static Level? ToLevel(int? number)
    {
        return number.HasValue ? Level.New((byte)number.Value) : null;
    }

    private static Level[] ToLevels(int[] levels)
    {
        return levels.Select(level => Level.New((byte)level)).ToArray();
    }

    private static string CodePoints(params int[] codePoints)
    {
        return string.Concat(codePoints.Select(char.ConvertFromUtf32));
    }
}
