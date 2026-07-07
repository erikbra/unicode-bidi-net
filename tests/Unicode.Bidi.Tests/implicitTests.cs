/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi implicit behavior with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/implicit.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

namespace Unicode.Bidi.Tests;

public class implicitTests
{
    [Fact]
    public void ResolveWeak_HandlesArabicLetterAndNumberSeparatorRules()
    {
        string text = "\u0627" + "1,2";
        BidiClass[] processingClasses = [BidiClass.AL, BidiClass.EN, BidiClass.CS, BidiClass.EN];
        IsolatingRunSequence sequence = new([new TextRange(0, 4)], BidiClass.R, BidiClass.R);

        Implicit.ResolveWeak(text, sequence, processingClasses);

        Assert.Equal([BidiClass.R, BidiClass.AN, BidiClass.AN, BidiClass.AN], processingClasses);
    }

    [Fact]
    public void ResolveWeak_ChangesEuropeanNumberToLAfterStrongL()
    {
        string text = "a+1";
        BidiClass[] processingClasses = [BidiClass.L, BidiClass.ES, BidiClass.EN];
        IsolatingRunSequence sequence = new([new TextRange(0, 3)], BidiClass.L, BidiClass.L);

        Implicit.ResolveWeak(text, sequence, processingClasses);

        Assert.Equal([BidiClass.L, BidiClass.ON, BidiClass.L], processingClasses);
    }

    [Fact]
    public void ResolveNeutral_ResolvesNeutralRunToEmbeddingDirection()
    {
        string text = "\u05D0 a";
        BidiClass[] processingClasses = [BidiClass.R, BidiClass.WS, BidiClass.L];
        Level[] levels = [Level.Rtl(), Level.Rtl(), Level.Rtl()];
        BidiClass[] originalClasses = processingClasses.ToArray();
        IsolatingRunSequence sequence = new([new TextRange(0, 3)], BidiClass.R, BidiClass.R);

        Implicit.ResolveNeutral(text, sequence, levels, originalClasses, processingClasses);

        Assert.Equal([BidiClass.R, BidiClass.R, BidiClass.L], processingClasses);
    }

    [Fact]
    public void ResolveLevels_RaisesLevelsFromResolvedClasses()
    {
        BidiClass[] processingClasses = [BidiClass.R, BidiClass.AN, BidiClass.L];
        Level[] levels = [Level.Ltr(), Level.Ltr(), Level.Rtl()];

        Level maxLevel = Implicit.ResolveLevels(processingClasses, levels);

        Assert.Equal([1, 2, 2], levels.Select(level => (int)level.Number()));
        Assert.Equal(2, maxLevel.Number());
    }

    [Fact]
    public void BidiInfo_New_ResolvesWeakAndNeutralLevelsLikeRust()
    {
        AssertLevels("\u05D0 12", [1, 1, 2, 2]);
        AssertLevels("\u0627" + "1,2", [1, 2, 2, 2]);
        AssertLevels("a \u05D0", [0, 0, 1]);
        AssertLevels("\u05D0 a", [1, 1, 2]);
    }

    [Fact]
    public void BidiInfo_New_ResolvesBracketPairsLikeRust()
    {
        AssertLevels("a(\u05D0)b", [0, 0, 1, 0, 0]);
        AssertLevels("\u05D0(a)\u05D1", [1, 1, 2, 1, 1]);
    }

    private static void AssertLevels(string text, int[] expectedLevels)
    {
        BidiInfo bidiInfo = BidiInfo.New(text);

        Assert.Equal(expectedLevels, bidiInfo.Levels.Select(level => (int)level.Number()));
    }
}
