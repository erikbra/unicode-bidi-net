/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/prepare.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

namespace Unicode.Bidi;

internal sealed record IsolatingRunSequence(TextRange[] Runs, BidiClass Sos, BidiClass Eos)
{
    internal IEnumerable<int> IterForwardsFrom(int position, int levelRunIndex)
    {
        TextRange current = Runs[levelRunIndex];
        for (int i = position; i < current.End; i++)
        {
            yield return i;
        }

        for (int runIndex = levelRunIndex + 1; runIndex < Runs.Length; runIndex++)
        {
            for (int i = Runs[runIndex].Start; i < Runs[runIndex].End; i++)
            {
                yield return i;
            }
        }
    }

    internal IEnumerable<int> IterBackwardsFrom(int position, int levelRunIndex)
    {
        TextRange current = Runs[levelRunIndex];
        for (int i = position - 1; i >= current.Start; i--)
        {
            yield return i;
        }

        for (int runIndex = levelRunIndex - 1; runIndex >= 0; runIndex--)
        {
            for (int i = Runs[runIndex].Start; i < Runs[runIndex].End; i++)
            {
                yield return i;
            }
        }
    }
}

internal static class Prepare
{
    internal static void IsolatingRunSequences(
        Level paraLevel,
        IReadOnlyList<BidiClass> originalClasses,
        IReadOnlyList<Level> levels,
        IReadOnlyList<TextRange> runs,
        bool hasIsolateControls,
        List<IsolatingRunSequence> isolatingRunSequences)
    {
        if (!hasIsolateControls)
        {
            foreach (TextRange run in runs)
            {
                Level seqLevel = levels[FirstIndexInRun(run, originalClasses, NotRemovedByX9) ?? run.Start];
                Level endLevel = levels[LastIndexInRun(run, originalClasses, NotRemovedByX9) ?? (run.End - 1)];
                Level predLevel = LastIndexBefore(run.Start, originalClasses, NotRemovedByX9) is int pred
                    ? levels[pred]
                    : paraLevel;
                Level succLevel = FirstIndexFrom(run.End, originalClasses, NotRemovedByX9) is int succ
                    ? levels[succ]
                    : paraLevel;

                isolatingRunSequences.Add(new IsolatingRunSequence(
                    [run],
                    Max(seqLevel, predLevel).BidiClass(),
                    Max(endLevel, succLevel).BidiClass()));
            }

            return;
        }

        List<TextRange[]> sequences = new(runs.Count);
        List<List<TextRange>> stack = [[]];

        foreach (TextRange run in runs)
        {
            BidiClass startClass = originalClasses[run.Start];
            BidiClass endClass = LastClassInRun(run, originalClasses, NotRemovedByX9) ?? startClass;

            List<TextRange> sequence = startClass == BidiClass.PDI && stack.Count > 1
                ? PopLast(stack)
                : [];
            sequence.Add(run);

            if (endClass is BidiClass.RLI or BidiClass.LRI or BidiClass.FSI)
            {
                stack.Add(sequence);
            }
            else
            {
                sequences.Add(sequence.ToArray());
            }
        }

        foreach (List<TextRange> sequence in stack.AsEnumerable().Reverse().Where(sequence => sequence.Count != 0))
        {
            sequences.Add(sequence.ToArray());
        }

        foreach (TextRange[] sequence in sequences)
        {
            int startOfSeq = sequence[0].Start;
            int endOfSeq = sequence[^1].End;
            IsolatingRunSequence partial = new(sequence, BidiClass.L, BidiClass.L);

            Level seqLevel = levels[FirstOrDefault(
                partial.IterForwardsFrom(startOfSeq, 0),
                i => NotRemovedByX9(originalClasses[i]),
                startOfSeq)];
            Level endLevel = levels[FirstOrDefault(
                partial.IterBackwardsFrom(endOfSeq, sequence.Length - 1),
                i => NotRemovedByX9(originalClasses[i]),
                endOfSeq - 1)];
            Level predLevel = LastIndexBefore(startOfSeq, originalClasses, NotRemovedByX9) is int pred
                ? levels[pred]
                : paraLevel;

            BidiClass lastNonRemoved = LastClassBefore(endOfSeq, originalClasses, NotRemovedByX9) ?? BidiClass.BN;
            Level succLevel = lastNonRemoved is BidiClass.RLI or BidiClass.LRI or BidiClass.FSI
                ? paraLevel
                : FirstIndexFrom(endOfSeq, originalClasses, NotRemovedByX9) is int succ
                    ? levels[succ]
                    : paraLevel;

            isolatingRunSequences.Add(partial with
            {
                Sos = Max(seqLevel, predLevel).BidiClass(),
                Eos = Max(endLevel, succLevel).BidiClass(),
            });
        }
    }

    internal static List<TextRange> LevelRuns(IReadOnlyList<Level> levels, IReadOnlyList<BidiClass> originalClasses)
    {
        List<TextRange> runs = [];
        if (levels.Count == 0)
        {
            return runs;
        }

        Level currentRunLevel = levels[0];
        int currentRunStart = 0;
        for (int i = 1; i < levels.Count; i++)
        {
            if (!RemovedByX9(originalClasses[i]) && levels[i] != currentRunLevel)
            {
                runs.Add(new TextRange(currentRunStart, i));
                currentRunLevel = levels[i];
                currentRunStart = i;
            }
        }

        runs.Add(new TextRange(currentRunStart, levels.Count));
        return runs;
    }

    internal static bool RemovedByX9(BidiClass bidiClass)
    {
        return bidiClass is BidiClass.RLE or BidiClass.LRE or BidiClass.RLO or BidiClass.LRO or BidiClass.PDF or BidiClass.BN;
    }

    internal static bool NotRemovedByX9(BidiClass bidiClass)
    {
        return !RemovedByX9(bidiClass);
    }

    private static List<TextRange> PopLast(List<List<TextRange>> stack)
    {
        List<TextRange> value = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return value;
    }

    private static Level Max(Level left, Level right)
    {
        return left >= right ? left : right;
    }

    private static int FirstOrDefault(IEnumerable<int> source, Func<int, bool> predicate, int defaultValue)
    {
        foreach (int item in source)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return defaultValue;
    }

    private static int? FirstIndexInRun(TextRange run, IReadOnlyList<BidiClass> classes, Func<BidiClass, bool> predicate)
    {
        for (int i = run.Start; i < run.End; i++)
        {
            if (predicate(classes[i]))
            {
                return i;
            }
        }

        return null;
    }

    private static int? LastIndexInRun(TextRange run, IReadOnlyList<BidiClass> classes, Func<BidiClass, bool> predicate)
    {
        for (int i = run.End - 1; i >= run.Start; i--)
        {
            if (predicate(classes[i]))
            {
                return i;
            }
        }

        return null;
    }

    private static BidiClass? LastClassInRun(TextRange run, IReadOnlyList<BidiClass> classes, Func<BidiClass, bool> predicate)
    {
        return LastIndexInRun(run, classes, predicate) is int index ? classes[index] : null;
    }

    private static int? LastIndexBefore(int end, IReadOnlyList<BidiClass> classes, Func<BidiClass, bool> predicate)
    {
        for (int i = end - 1; i >= 0; i--)
        {
            if (predicate(classes[i]))
            {
                return i;
            }
        }

        return null;
    }

    private static BidiClass? LastClassBefore(int end, IReadOnlyList<BidiClass> classes, Func<BidiClass, bool> predicate)
    {
        return LastIndexBefore(end, classes, predicate) is int index ? classes[index] : null;
    }

    private static int? FirstIndexFrom(int start, IReadOnlyList<BidiClass> classes, Func<BidiClass, bool> predicate)
    {
        for (int i = start; i < classes.Count; i++)
        {
            if (predicate(classes[i]))
            {
                return i;
            }
        }

        return null;
    }
}
