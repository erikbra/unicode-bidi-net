/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi explicit behavior with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/explicit.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

using Unicode.Bidi.char_data;

namespace Unicode.Bidi.Tests;

public class explicitTests
{
    [Fact]
    public void Compute_ResolvesEmbeddingLevelsAndRuns()
    {
        string text = $"a{FormatChars.RLE}\u05D0{FormatChars.PDF}b";
        BidiClass[] originalClasses = InitialInfo.New(text).OriginalClasses.ToArray();
        BidiClass[] processingClasses = originalClasses.ToArray();
        Level[] levels = Enumerable.Repeat(Level.Ltr(), text.Length).ToArray();
        List<TextRange> runs = [];

        Explicit.Compute(text, Level.Ltr(), originalClasses, levels, processingClasses, runs);

        Assert.Equal([0, 1, 1, 0, 0], levels.Select(level => (int)level.Number()));
        Assert.Equal(
            [BidiClass.L, BidiClass.BN, BidiClass.R, BidiClass.BN, BidiClass.L],
            processingClasses);
        Assert.Equal([new TextRange(0, 2), new TextRange(2, 4), new TextRange(4, 5)], runs);
    }

    [Fact]
    public void Compute_AppliesDirectionalOverride()
    {
        string text = $"a{FormatChars.RLO}b{FormatChars.PDF}";
        BidiClass[] originalClasses = InitialInfo.New(text).OriginalClasses.ToArray();
        BidiClass[] processingClasses = originalClasses.ToArray();
        Level[] levels = Enumerable.Repeat(Level.Ltr(), text.Length).ToArray();
        List<TextRange> runs = [];

        Explicit.Compute(text, Level.Ltr(), originalClasses, levels, processingClasses, runs);

        Assert.Equal([0, 1, 1, 0], levels.Select(level => (int)level.Number()));
        Assert.Equal(
            [BidiClass.L, BidiClass.BN, BidiClass.R, BidiClass.BN],
            processingClasses);
    }

    [Fact]
    public void Compute_HandlesIsolateInitiatorAndPdi()
    {
        string text = $"a{FormatChars.RLI}\u05D0{FormatChars.PDI}b";
        BidiClass[] originalClasses = InitialInfo.New(text).OriginalClasses.ToArray();
        BidiClass[] processingClasses = originalClasses.ToArray();
        Level[] levels = Enumerable.Repeat(Level.Ltr(), text.Length).ToArray();
        List<TextRange> runs = [];

        Explicit.Compute(text, Level.Ltr(), originalClasses, levels, processingClasses, runs);

        Assert.Equal([0, 0, 1, 0, 0], levels.Select(level => (int)level.Number()));
        Assert.Equal(
            [new TextRange(0, 2), new TextRange(2, 3), new TextRange(3, 5)],
            runs);
    }

    [Fact]
    public void BidiInfo_New_AssignsLevelsToFormattingControls()
    {
        string text = $"a{FormatChars.RLE}\u05D0{FormatChars.PDF}b";

        BidiInfo bidiInfo = BidiInfo.New(text);

        Assert.Equal([0, 0, 1, 1, 0], bidiInfo.Levels.Select(level => (int)level.Number()));
    }
}
