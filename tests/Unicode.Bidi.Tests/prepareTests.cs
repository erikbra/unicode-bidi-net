/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi prepare tests with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/prepare.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: test-adapted
 */

using Unicode.Bidi.char_data;

namespace Unicode.Bidi.Tests;

public class prepareTests
{
    [Fact]
    public void LevelRuns_MatchesRustExamples()
    {
        Assert.Empty(Prepare.LevelRuns([], []));

        Level[] levels = ToLevels([0, 0, 0, 1, 1, 2, 0, 0]);
        BidiClass[] classes = Enumerable.Repeat(BidiClass.L, 8).ToArray();

        Assert.Equal(
            [new TextRange(0, 3), new TextRange(3, 5), new TextRange(5, 6), new TextRange(6, 8)],
            Prepare.LevelRuns(levels, classes));
    }

    [Fact]
    public void RemovedByX9_MatchesRustPredicate()
    {
        BidiClass[] removed = [BidiClass.RLE, BidiClass.LRE, BidiClass.RLO, BidiClass.LRO, BidiClass.PDF, BidiClass.BN];
        BidiClass[] retained = [BidiClass.L, BidiClass.RLI, BidiClass.AL, BidiClass.LRI, BidiClass.PDI];

        Assert.All(removed, bidiClass => Assert.True(Prepare.RemovedByX9(bidiClass)));
        Assert.All(retained, bidiClass => Assert.False(Prepare.RemovedByX9(bidiClass)));
    }

    [Fact]
    public void IsolatingRunSequences_ExampleWithoutIsolates_MatchesRust()
    {
        BidiClass[] classes =
        [
            BidiClass.L, BidiClass.RLE, BidiClass.L, BidiClass.PDF,
            BidiClass.RLE, BidiClass.L, BidiClass.PDF, BidiClass.L,
        ];
        Level[] levels = ToLevels([0, 1, 1, 1, 1, 1, 1, 0]);
        List<IsolatingRunSequence> sequences = [];

        Prepare.IsolatingRunSequences(
            Level.Ltr(),
            classes,
            levels,
            Prepare.LevelRuns(levels, classes),
            hasIsolateControls: false,
            sequences);

        TextRange[][] runs = sequences
            .OrderBy(sequence => sequence.Runs[0].Start)
            .Select(sequence => sequence.Runs)
            .ToArray();

        Assert.Equal(
            [
                [new TextRange(0, 2)],
                [new TextRange(2, 7)],
                [new TextRange(7, 8)],
            ],
            runs);
    }

    [Fact]
    public void IsolatingRunSequences_ExampleWithIsolates_MatchesRust()
    {
        BidiClass[] classes =
        [
            BidiClass.L, BidiClass.RLI, BidiClass.L, BidiClass.PDI,
            BidiClass.RLI, BidiClass.L, BidiClass.PDI, BidiClass.L,
        ];
        Level[] levels = ToLevels([0, 0, 1, 0, 0, 1, 0, 0]);
        List<IsolatingRunSequence> sequences = [];

        Prepare.IsolatingRunSequences(
            Level.Ltr(),
            classes,
            levels,
            Prepare.LevelRuns(levels, classes),
            hasIsolateControls: true,
            sequences);

        TextRange[][] runs = sequences
            .OrderBy(sequence => sequence.Runs[0].Start)
            .Select(sequence => sequence.Runs)
            .ToArray();

        Assert.Equal(
            [
                [new TextRange(0, 2), new TextRange(3, 5), new TextRange(6, 8)],
                [new TextRange(2, 3)],
                [new TextRange(5, 6)],
            ],
            runs);
    }

    [Fact]
    public void IsolatingRunSequences_NestedIsolatesExample_MatchesRust()
    {
        BidiClass[] classes =
        [
            BidiClass.L, BidiClass.RLI, BidiClass.L, BidiClass.LRI, BidiClass.L, BidiClass.RLE, BidiClass.L,
            BidiClass.PDF, BidiClass.L, BidiClass.PDI, BidiClass.L, BidiClass.PDI, BidiClass.L,
        ];
        Level[] levels = ToLevels([0, 0, 1, 1, 2, 3, 3, 3, 2, 1, 1, 0, 0]);
        List<IsolatingRunSequence> sequences = [];

        Prepare.IsolatingRunSequences(
            Level.Ltr(),
            classes,
            levels,
            Prepare.LevelRuns(levels, classes),
            hasIsolateControls: true,
            sequences);

        TextRange[][] runs = sequences
            .OrderBy(sequence => sequence.Runs[0].Start)
            .Select(sequence => sequence.Runs)
            .ToArray();

        Assert.Equal(
            [
                [new TextRange(0, 2), new TextRange(11, 13)],
                [new TextRange(2, 4), new TextRange(9, 11)],
                [new TextRange(4, 6)],
                [new TextRange(6, 8)],
                [new TextRange(8, 9)],
            ],
            runs);
    }

    [Fact]
    public void IsolatingRunSequences_SosAndEosWithoutIsolates_MatchesRust()
    {
        BidiClass[] classes =
        [
            BidiClass.L, BidiClass.RLE, BidiClass.L, BidiClass.LRE, BidiClass.L, BidiClass.PDF,
            BidiClass.L, BidiClass.PDF, BidiClass.RLE, BidiClass.L, BidiClass.PDF, BidiClass.L,
        ];
        Level[] levels = ToLevels([0, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 0]);
        List<IsolatingRunSequence> sequences = [];

        Prepare.IsolatingRunSequences(
            Level.Ltr(),
            classes,
            levels,
            Prepare.LevelRuns(levels, classes),
            hasIsolateControls: false,
            sequences);

        IsolatingRunSequence[] sorted = sequences.OrderBy(sequence => sequence.Runs[0].Start).ToArray();
        AssertSequence(sorted[0], [new TextRange(0, 2)], BidiClass.L, BidiClass.R);
        AssertSequence(sorted[1], [new TextRange(2, 4)], BidiClass.R, BidiClass.L);
        AssertSequence(sorted[2], [new TextRange(4, 6)], BidiClass.L, BidiClass.L);
        AssertSequence(sorted[3], [new TextRange(6, 11)], BidiClass.L, BidiClass.R);
        AssertSequence(sorted[4], [new TextRange(11, 12)], BidiClass.R, BidiClass.L);
    }

    [Fact]
    public void IsolatingRunSequences_SosAndEosWithIsolates_MatchesRust()
    {
        BidiClass[] classes =
        [
            BidiClass.L, BidiClass.RLI, BidiClass.L, BidiClass.LRI, BidiClass.L, BidiClass.PDI,
            BidiClass.L, BidiClass.PDI, BidiClass.RLI, BidiClass.L, BidiClass.PDI, BidiClass.L,
        ];
        Level[] levels = ToLevels([0, 0, 1, 1, 2, 1, 1, 0, 0, 1, 0, 0]);
        List<IsolatingRunSequence> sequences = [];

        Prepare.IsolatingRunSequences(
            Level.Ltr(),
            classes,
            levels,
            Prepare.LevelRuns(levels, classes),
            hasIsolateControls: true,
            sequences);

        IsolatingRunSequence[] sorted = sequences.OrderBy(sequence => sequence.Runs[0].Start).ToArray();
        AssertSequence(
            sorted[0],
            [new TextRange(0, 2), new TextRange(7, 9), new TextRange(10, 12)],
            BidiClass.L,
            BidiClass.L);
        AssertSequence(
            sorted[1],
            [new TextRange(2, 4), new TextRange(5, 7)],
            BidiClass.R,
            BidiClass.R);
        AssertSequence(sorted[2], [new TextRange(4, 5)], BidiClass.L, BidiClass.L);
        AssertSequence(sorted[3], [new TextRange(9, 10)], BidiClass.R, BidiClass.R);
    }

    [Fact]
    public void NotRemovedByX9_MatchesRustPredicate()
    {
        BidiClass[] retained =
        [
            BidiClass.L, BidiClass.R, BidiClass.AL, BidiClass.EN, BidiClass.ES, BidiClass.ET,
            BidiClass.AN, BidiClass.CS, BidiClass.NSM, BidiClass.B, BidiClass.S, BidiClass.WS,
            BidiClass.ON, BidiClass.LRI, BidiClass.RLI, BidiClass.FSI, BidiClass.PDI,
        ];

        Assert.All(retained, bidiClass => Assert.True(Prepare.NotRemovedByX9(bidiClass)));
    }

    private static void AssertSequence(
        IsolatingRunSequence actual,
        TextRange[] expectedRuns,
        BidiClass expectedSos,
        BidiClass expectedEos)
    {
        Assert.Equal(expectedRuns, actual.Runs);
        Assert.Equal(expectedSos, actual.Sos);
        Assert.Equal(expectedEos, actual.Eos);
    }

    private static Level[] ToLevels(int[] levels)
    {
        return levels.Select(level => Level.New((byte)level)).ToArray();
    }
}
