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
    public void TestNew()
    {
        Assert.Equal((byte)0, Level.New(0).Number());
        Assert.Equal((byte)1, Level.New(1).Number());
        Assert.Equal((byte)10, Level.New(10).Number());
        Assert.Equal((byte)125, Level.New(125).Number());
        Assert.Equal((byte)126, Level.New(126).Number());
        Assert.Throws<LevelOutOfRangeException>(() => Level.New(127));
        Assert.Throws<LevelOutOfRangeException>(() => Level.New(255));
    }

    [Fact]
    public void TestNewExplicit()
    {
        Assert.Equal((byte)0, Level.NewExplicit(0).Number());
        Assert.Equal((byte)1, Level.NewExplicit(1).Number());
        Assert.Equal((byte)10, Level.NewExplicit(10).Number());
        Assert.Equal((byte)125, Level.NewExplicit(125).Number());
        Assert.Throws<LevelOutOfRangeException>(() => Level.NewExplicit(126));
        Assert.Throws<LevelOutOfRangeException>(() => Level.NewExplicit(255));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(124, true)]
    [InlineData(125, false)]
    public void TestIsLtr(byte number, bool expected)
    {
        Assert.Equal(expected, Level.New(number).IsLtr());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10, false)]
    [InlineData(11, true)]
    [InlineData(124, false)]
    [InlineData(125, true)]
    public void TestIsRtl(byte number, bool expected)
    {
        Assert.Equal(expected, Level.New(number).IsRtl());
    }

    [Fact]
    public void TestRaise()
    {
        Level level = Level.Ltr();

        Assert.Equal((byte)0, level.Number());
        level = level.Raise(100);
        Assert.Equal((byte)100, level.Number());
        level = level.Raise(26);
        Assert.Equal((byte)126, level.Number());
        Assert.Throws<LevelOutOfRangeException>(() => level.Raise(1));
        Assert.Throws<LevelOutOfRangeException>(() => level.Raise(250));
        Assert.Equal((byte)126, level.Number());
    }

    [Fact]
    public void TestRaiseExplicit()
    {
        Level level = Level.Ltr();

        Assert.Equal((byte)0, level.Number());
        level = level.RaiseExplicit(100);
        Assert.Equal((byte)100, level.Number());
        level = level.RaiseExplicit(25);
        Assert.Equal((byte)125, level.Number());
        Assert.Throws<LevelOutOfRangeException>(() => level.RaiseExplicit(1));
        Assert.Throws<LevelOutOfRangeException>(() => level.RaiseExplicit(250));
        Assert.Equal((byte)125, level.Number());
    }

    [Fact]
    public void TestLower()
    {
        Level level = Level.Rtl();

        Assert.Equal((byte)1, level.Number());
        level = level.Lower(1);
        Assert.Equal((byte)0, level.Number());
        Assert.Throws<LevelOutOfRangeException>(() => level.Lower(1));
        Assert.Throws<LevelOutOfRangeException>(() => level.Lower(250));
        Assert.Equal((byte)0, level.Number());
    }

    [Theory]
    [InlineData(new byte[] { 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0, 1, 0 }, true)]
    [InlineData(new byte[] { 0, 2, 0 }, false)]
    [InlineData(new byte[] { 0, 125, 0 }, true)]
    [InlineData(new byte[] { 0, 126, 0 }, false)]
    public void TestHasRtl(byte[] levelNumbers, bool expected)
    {
        Assert.Equal(expected, levelNumbers.Select(Level.New).Any(level => level.IsRtl()));
    }

    [Fact]
    public void TestInto()
    {
        Level level = Level.Rtl();

        Assert.Equal((byte)1, level.Number());
    }

    [Fact]
    public void TestVec()
    {
        Assert.Equal([Level.New(0), Level.New(1), Level.New(125)], ToLevels([0, 1, 125]));
    }

    [Fact]
    public void TestStrEq()
    {
        Assert.True(LevelsMatch(ToLevels([0, 1, 4, 125]), ["0", "1", "x", "125"]));
        Assert.False(LevelsMatch(ToLevels([0, 1, 4, 125]), ["0", "1", "5", "125"]));
    }

    [Fact]
    public void TestStringEq()
    {
        Assert.True(LevelsMatch(ToLevels([0, 1, 4, 125]), ["0".ToString(), "1".ToString(), "x".ToString(), "125".ToString()]));
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

    private static bool LevelsMatch(IReadOnlyList<Level> levels, IReadOnlyList<string> expected)
    {
        return levels.Count == expected.Count
            && levels.Zip(expected).All(pair => pair.Second == "x" || pair.First.ToString() == pair.Second);
    }

    private static Level[] ToLevels(int[] levels)
    {
        return levels.Select(level => Level.New((byte)level)).ToArray();
    }
}
