/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted for .NET public API smoke tests with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/lib.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

using Unicode.Bidi;

namespace UnicodeBidiNet.Tests;

public class PublicApiSmokeTests
{
    [Fact]
    public void RootNamespace_ExposesPublishedEntryPoints()
    {
        Assert.Equal(BidiClass.R, CharData.BidiClass('\u05D0'));

        Level level = Level.CreateExplicit(1);
        Assert.Equal((byte)1, level.Value);
        Assert.True(level.IsRightToLeft);

        BidiInfo info = BidiInfo.Create("abc אבג", Level.Ltr());
        ParagraphInfo paragraph = Assert.Single(info.Paragraphs);
        Assert.Equal(info.Text.Length, paragraph.Length);
        Assert.NotEmpty(info.ReorderLine(paragraph, paragraph.Range));
    }
}
