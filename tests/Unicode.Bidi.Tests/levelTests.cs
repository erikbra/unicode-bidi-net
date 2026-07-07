/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi level behavior with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/level.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

using Unicode.Bidi.char_data;

namespace Unicode.Bidi.Tests;

public class levelTests
{
    [Fact]
    public void Constructors_CreateExpectedBaseLevels()
    {
        Assert.Equal((byte)0, Level.Ltr().Number());
        Assert.Equal((byte)1, Level.Rtl().Number());
        Assert.True(Level.Ltr().IsLtr());
        Assert.True(Level.Rtl().IsRtl());
    }

    [Fact]
    public void New_AllowsImplicitMaximumAndRejectsAboveIt()
    {
        Assert.Equal(Level.MaxImplicitDepth, Level.New(Level.MaxImplicitDepth).Number());

        Assert.Throws<LevelOutOfRangeException>(() => Level.New((byte)(Level.MaxImplicitDepth + 1)));
    }

    [Fact]
    public void NewExplicit_AllowsExplicitMaximumAndRejectsAboveIt()
    {
        Assert.Equal(Level.MaxExplicitDepth, Level.NewExplicit(Level.MaxExplicitDepth).Number());

        Assert.Throws<LevelOutOfRangeException>(() => Level.NewExplicit((byte)(Level.MaxExplicitDepth + 1)));
    }

    [Fact]
    public void MutatorLikeHelpers_ReturnAdjustedLevels()
    {
        Level level = Level.New(4);

        Assert.Equal((byte)7, level.Raise(3).Number());
        Assert.Equal((byte)6, level.RaiseExplicit(2).Number());
        Assert.Equal((byte)2, level.Lower(2).Number());
    }

    [Fact]
    public void DirectionHelpers_MatchRustLevelHelpers()
    {
        Level level = Level.New(4);

        Assert.Equal((byte)6, level.NewExplicitNextLtr().Number());
        Assert.Equal((byte)5, level.NewExplicitNextRtl().Number());
        Assert.Equal((byte)5, level.NewLowestGeRtl().Number());
        Assert.Equal(BidiClass.L, level.BidiClass());
        Assert.Equal(BidiClass.R, Level.New(5).BidiClass());
    }
}
