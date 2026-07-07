/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi initial-info behavior with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/lib.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

using Unicode.Bidi.char_data;

namespace Unicode.Bidi.Tests;

public class initialInfoTests
{
    [Fact]
    public void New_ReturnsNoParagraphsForEmptyText()
    {
        InitialInfo initialInfo = InitialInfo.New(string.Empty);

        Assert.Empty(initialInfo.OriginalClasses);
        Assert.Empty(initialInfo.Paragraphs);
    }

    [Fact]
    public void New_DetectsSimpleLtrParagraph()
    {
        InitialInfo initialInfo = InitialInfo.New("abc");

        ParagraphInfo paragraph = Assert.Single(initialInfo.Paragraphs);
        Assert.Equal(new TextRange(0, 3), paragraph.Range);
        Assert.Equal(Level.Ltr(), paragraph.Level);
        Assert.Equal([BidiClass.L, BidiClass.L, BidiClass.L], initialInfo.OriginalClasses);
    }

    [Fact]
    public void New_DetectsSimpleRtlParagraph()
    {
        InitialInfo initialInfo = InitialInfo.New("\u05D0\u05D1\u05D2");

        ParagraphInfo paragraph = Assert.Single(initialInfo.Paragraphs);
        Assert.Equal(new TextRange(0, 3), paragraph.Range);
        Assert.Equal(Level.Rtl(), paragraph.Level);
        Assert.Equal([BidiClass.R, BidiClass.R, BidiClass.R], initialInfo.OriginalClasses);
    }

    [Fact]
    public void New_KeepsParagraphSeparatorWithPreviousParagraph()
    {
        InitialInfo initialInfo = InitialInfo.New("a\n\u05D0");

        Assert.Equal(2, initialInfo.Paragraphs.Count);
        Assert.Equal(new TextRange(0, 2), initialInfo.Paragraphs[0].Range);
        Assert.Equal(Level.Ltr(), initialInfo.Paragraphs[0].Level);
        Assert.Equal(new TextRange(2, 3), initialInfo.Paragraphs[1].Range);
        Assert.Equal(Level.Rtl(), initialInfo.Paragraphs[1].Level);
        Assert.Equal([BidiClass.L, BidiClass.B, BidiClass.R], initialInfo.OriginalClasses);
    }

    [Fact]
    public void New_UsesProvidedDefaultParagraphLevel()
    {
        InitialInfo ltrTextWithRtlDefault = InitialInfo.New("abc", Level.Rtl());
        InitialInfo rtlTextWithLtrDefault = InitialInfo.New("\u05D0\u05D1", Level.Ltr());

        Assert.Equal(Level.Rtl(), Assert.Single(ltrTextWithRtlDefault.Paragraphs).Level);
        Assert.Equal(Level.Ltr(), Assert.Single(rtlTextWithLtrDefault.Paragraphs).Level);
    }

    [Fact]
    public void New_ResolvesFirstStrongIsolateClassWhenStrongCharacterIsPresent()
    {
        InitialInfo rtlIsolate = InitialInfo.New($"{FormatChars.FSI}\u05D0{FormatChars.PDI}");
        InitialInfo ltrIsolate = InitialInfo.New($"{FormatChars.FSI}a{FormatChars.PDI}");

        Assert.Equal(BidiClass.RLI, rtlIsolate.OriginalClasses[0]);
        Assert.Equal(BidiClass.LRI, ltrIsolate.OriginalClasses[0]);
    }

    [Fact]
    public void New_RepeatsSupplementaryScalarClassForBothUtf16CodeUnits()
    {
        string text = char.ConvertFromUtf32(0x1F600) + "a";

        InitialInfo initialInfo = InitialInfo.New(text);

        Assert.Equal(3, text.Length);
        Assert.Equal([BidiClass.ON, BidiClass.ON, BidiClass.L], initialInfo.OriginalClasses);
        Assert.Equal(new TextRange(0, 3), Assert.Single(initialInfo.Paragraphs).Range);
    }

    [Fact]
    public void BidiInfo_New_InitializesParagraphMetadataAndPlaceholderLevels()
    {
        BidiInfo bidiInfo = BidiInfo.New("a\n\u05D0");

        Assert.Equal(2, bidiInfo.Paragraphs.Count);
        Assert.Equal([Level.Ltr(), Level.Ltr(), Level.Rtl()], bidiInfo.Levels);
    }
}
