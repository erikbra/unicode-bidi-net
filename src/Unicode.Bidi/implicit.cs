/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/implicit.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

using System.Text;
using Unicode.Bidi.char_data;

namespace Unicode.Bidi;

internal static class Implicit
{
    internal static void ResolveWeak(string text, IsolatingRunSequence sequence, BidiClass[] processingClasses)
    {
        BidiClass prevClassBeforeW4 = sequence.Sos;
        BidiClass prevClassBeforeW5 = sequence.Sos;
        BidiClass prevClassBeforeW1 = sequence.Sos;
        bool lastStrongIsAl = false;
        List<int> etRunIndices = [];
        List<int> bnRunIndices = [];

        for (int runIndex = 0; runIndex < sequence.Runs.Length; runIndex++)
        {
            TextRange levelRun = sequence.Runs[runIndex];
            for (int i = levelRun.Start; i < levelRun.End; i++)
            {
                if (processingClasses[i] == BidiClass.BN)
                {
                    bnRunIndices.Add(i);
                    continue;
                }

                BidiClass w2ProcessingClass = processingClasses[i];

                if (processingClasses[i] == BidiClass.NSM)
                {
                    processingClasses[i] = prevClassBeforeW1 switch
                    {
                        BidiClass.RLI or BidiClass.LRI or BidiClass.FSI or BidiClass.PDI => BidiClass.ON,
                        _ => prevClassBeforeW1,
                    };
                    w2ProcessingClass = processingClasses[i];
                }

                prevClassBeforeW1 = processingClasses[i];

                switch (processingClasses[i])
                {
                    case BidiClass.EN when lastStrongIsAl:
                        processingClasses[i] = BidiClass.AN;
                        break;
                    case BidiClass.AL:
                        processingClasses[i] = BidiClass.R;
                        break;
                }

                switch (w2ProcessingClass)
                {
                    case BidiClass.L:
                    case BidiClass.R:
                        lastStrongIsAl = false;
                        break;
                    case BidiClass.AL:
                        lastStrongIsAl = true;
                        break;
                }

                BidiClass classBeforeW456 = processingClasses[i];

                switch (processingClasses[i])
                {
                    case BidiClass.EN:
                        foreach (int j in etRunIndices)
                        {
                            processingClasses[j] = BidiClass.EN;
                        }

                        etRunIndices.Clear();
                        break;

                    case BidiClass.ES:
                    case BidiClass.CS:
                        int? charLength = CodeUnitLengthAt(text, i);
                        if (charLength.HasValue)
                        {
                            BidiClass nextClass = FirstClassOrDefault(
                                sequence.IterForwardsFrom(i + charLength.Value, runIndex),
                                processingClasses,
                                Prepare.NotRemovedByX9,
                                sequence.Eos);

                            if (nextClass == BidiClass.EN && lastStrongIsAl)
                            {
                                nextClass = BidiClass.AN;
                            }

                            processingClasses[i] = (prevClassBeforeW4, processingClasses[i], nextClass) switch
                            {
                                (BidiClass.EN, BidiClass.ES, BidiClass.EN) => BidiClass.EN,
                                (BidiClass.EN, BidiClass.CS, BidiClass.EN) => BidiClass.EN,
                                (BidiClass.AN, BidiClass.CS, BidiClass.AN) => BidiClass.AN,
                                _ => BidiClass.ON,
                            };

                            if (processingClasses[i] == BidiClass.ON)
                            {
                                foreach (int index in sequence.IterBackwardsFrom(i, runIndex))
                                {
                                    if (processingClasses[index] != BidiClass.BN)
                                    {
                                        break;
                                    }

                                    processingClasses[index] = BidiClass.ON;
                                }

                                foreach (int index in sequence.IterForwardsFrom(i + charLength.Value, runIndex))
                                {
                                    if (processingClasses[index] != BidiClass.BN)
                                    {
                                        break;
                                    }

                                    processingClasses[index] = BidiClass.ON;
                                }
                            }
                        }
                        else
                        {
                            processingClasses[i] = processingClasses[i - 1];
                        }

                        break;

                    case BidiClass.ET:
                        if (prevClassBeforeW5 == BidiClass.EN)
                        {
                            processingClasses[i] = BidiClass.EN;
                        }
                        else
                        {
                            etRunIndices.AddRange(bnRunIndices);
                            etRunIndices.Add(i);
                        }

                        break;
                }

                bnRunIndices.Clear();
                prevClassBeforeW5 = processingClasses[i];

                if (prevClassBeforeW5 != BidiClass.ET)
                {
                    foreach (int j in etRunIndices)
                    {
                        processingClasses[j] = BidiClass.ON;
                    }

                    etRunIndices.Clear();
                }

                prevClassBeforeW4 = classBeforeW456;
            }
        }

        foreach (int j in etRunIndices)
        {
            processingClasses[j] = BidiClass.ON;
        }

        etRunIndices.Clear();

        bool lastStrongIsL = sequence.Sos == BidiClass.L;
        foreach (TextRange run in sequence.Runs)
        {
            for (int i = run.Start; i < run.End; i++)
            {
                switch (processingClasses[i])
                {
                    case BidiClass.EN when lastStrongIsL:
                        processingClasses[i] = BidiClass.L;
                        break;
                    case BidiClass.L:
                        lastStrongIsL = true;
                        break;
                    case BidiClass.R:
                    case BidiClass.AL:
                        lastStrongIsL = false;
                        break;
                }
            }
        }
    }

    internal static void ResolveNeutral(
        string text,
        IsolatingRunSequence sequence,
        IReadOnlyList<Level> levels,
        IReadOnlyList<BidiClass> originalClasses,
        BidiClass[] processingClasses)
    {
        BidiClass embeddingDirection = levels[sequence.Runs[0].Start].BidiClass();
        BidiClass notEmbeddingDirection = embeddingDirection == BidiClass.L ? BidiClass.R : BidiClass.L;

        List<BracketPair> bracketPairs = [];
        IdentifyBracketPairs(text, sequence, processingClasses, bracketPairs);

        foreach (BracketPair pair in bracketPairs)
        {
            bool foundEmbeddingDirection = false;
            bool foundNotEmbeddingDirection = false;
            BidiClass? classToSet = null;

            int startCharLength = CodeUnitLengthAt(text, pair.Start) ?? 1;
            foreach (int enclosedIndex in sequence.IterForwardsFrom(pair.Start + startCharLength, pair.StartRun))
            {
                if (enclosedIndex >= pair.End)
                {
                    break;
                }

                BidiClass bidiClass = processingClasses[enclosedIndex];
                if (bidiClass == embeddingDirection)
                {
                    foundEmbeddingDirection = true;
                }
                else if (bidiClass == notEmbeddingDirection)
                {
                    foundNotEmbeddingDirection = true;
                }
                else if (bidiClass is BidiClass.EN or BidiClass.AN)
                {
                    if (embeddingDirection == BidiClass.L)
                    {
                        foundNotEmbeddingDirection = true;
                    }
                    else
                    {
                        foundEmbeddingDirection = true;
                    }
                }

                if (foundEmbeddingDirection)
                {
                    break;
                }
            }

            if (foundEmbeddingDirection)
            {
                classToSet = embeddingDirection;
            }
            else if (foundNotEmbeddingDirection)
            {
                BidiClass previousStrong = FirstClassOrDefault(
                    sequence.IterBackwardsFrom(pair.Start, pair.StartRun),
                    processingClasses,
                    bidiClass => bidiClass is BidiClass.L or BidiClass.R or BidiClass.EN or BidiClass.AN,
                    sequence.Sos);

                if (previousStrong is BidiClass.EN or BidiClass.AN)
                {
                    previousStrong = BidiClass.R;
                }

                classToSet = previousStrong;
            }

            if (classToSet.HasValue)
            {
                BidiClass value = classToSet.Value;
                int endCharLength = CodeUnitLengthAt(text, pair.End) ?? 1;

                SetRange(processingClasses, pair.Start, pair.Start + startCharLength, value);
                SetRange(processingClasses, pair.End, pair.End + endCharLength, value);

                foreach (int index in sequence.IterBackwardsFrom(pair.Start, pair.StartRun))
                {
                    if (processingClasses[index] != BidiClass.BN)
                    {
                        break;
                    }

                    processingClasses[index] = value;
                }

                int nsmStart = pair.Start + startCharLength;
                foreach (int index in sequence.IterForwardsFrom(nsmStart, pair.StartRun))
                {
                    if (originalClasses[index] == BidiClass.NSM || processingClasses[index] == BidiClass.BN)
                    {
                        processingClasses[index] = value;
                    }
                    else
                    {
                        break;
                    }
                }

                int nsmEnd = pair.End + endCharLength;
                foreach (int index in sequence.IterForwardsFrom(nsmEnd, pair.EndRun))
                {
                    if (originalClasses[index] == BidiClass.NSM || processingClasses[index] == BidiClass.BN)
                    {
                        processingClasses[index] = value;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        List<int> indices = Flatten(sequence.Runs).ToList();
        BidiClass prevClass = sequence.Sos;
        int position = 0;
        while (position < indices.Count)
        {
            int i = indices[position++];
            List<int> niRun = [];
            if (IsNI(processingClasses[i]) || processingClasses[i] == BidiClass.BN)
            {
                niRun.Add(i);
                BidiClass nextClass;
                while (true)
                {
                    if (position < indices.Count)
                    {
                        int j = indices[position++];
                        i = j;
                        nextClass = processingClasses[j];
                        if (IsNI(nextClass) || nextClass == BidiClass.BN)
                        {
                            niRun.Add(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        nextClass = sequence.Eos;
                        break;
                    }
                }

                BidiClass newClass = (prevClass, nextClass) switch
                {
                    (BidiClass.L, BidiClass.L) => BidiClass.L,
                    (BidiClass.R, BidiClass.R) => BidiClass.R,
                    (BidiClass.R, BidiClass.AN) => BidiClass.R,
                    (BidiClass.R, BidiClass.EN) => BidiClass.R,
                    (BidiClass.AN, BidiClass.R) => BidiClass.R,
                    (BidiClass.AN, BidiClass.AN) => BidiClass.R,
                    (BidiClass.AN, BidiClass.EN) => BidiClass.R,
                    (BidiClass.EN, BidiClass.R) => BidiClass.R,
                    (BidiClass.EN, BidiClass.AN) => BidiClass.R,
                    (BidiClass.EN, BidiClass.EN) => BidiClass.R,
                    _ => embeddingDirection,
                };

                foreach (int j in niRun)
                {
                    processingClasses[j] = newClass;
                }
            }

            prevClass = processingClasses[i];
        }
    }

    internal static Level ResolveLevels(IReadOnlyList<BidiClass> processingClasses, Level[] levels)
    {
        Level maxLevel = Level.Ltr();
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i] = (levels[i].IsRtl(), processingClasses[i]) switch
            {
                (false, BidiClass.AN) => levels[i].Raise(2),
                (false, BidiClass.EN) => levels[i].Raise(2),
                (false, BidiClass.R) => levels[i].Raise(1),
                (true, BidiClass.L) => levels[i].Raise(1),
                (true, BidiClass.EN) => levels[i].Raise(1),
                (true, BidiClass.AN) => levels[i].Raise(1),
                _ => levels[i],
            };

            if (levels[i] > maxLevel)
            {
                maxLevel = levels[i];
            }
        }

        return maxLevel;
    }

    private static void IdentifyBracketPairs(
        string text,
        IsolatingRunSequence runSequence,
        IReadOnlyList<BidiClass> classes,
        List<BracketPair> bracketPairs)
    {
        List<(Rune Opening, int Index, int RunIndex)> stack = [];

        for (int runIndex = 0; runIndex < runSequence.Runs.Length; runIndex++)
        {
            foreach (InitialInfo.TextScalar scalar in EnumerateScalars(text, runSequence.Runs[runIndex]))
            {
                int actualIndex = scalar.CodeUnitIndex;
                if (classes[actualIndex] != BidiClass.ON)
                {
                    continue;
                }

                BidiMatchedOpeningBracket? matched = CharData.BidiMatchedOpeningBracket(scalar.Rune);
                if (!matched.HasValue)
                {
                    continue;
                }

                if (matched.Value.IsOpen)
                {
                    if (stack.Count >= 63)
                    {
                        break;
                    }

                    stack.Add((matched.Value.Opening, actualIndex, runIndex));
                }
                else
                {
                    for (int stackIndex = stack.Count - 1; stackIndex >= 0; stackIndex--)
                    {
                        (Rune opening, int index, int openingRunIndex) = stack[stackIndex];
                        if (opening == matched.Value.Opening)
                        {
                            bracketPairs.Add(new BracketPair(index, actualIndex, openingRunIndex, runIndex));
                            stack.RemoveRange(stackIndex, stack.Count - stackIndex);
                            break;
                        }
                    }
                }
            }
        }

        bracketPairs.Sort((left, right) => left.Start.CompareTo(right.Start));
    }

    private static IEnumerable<InitialInfo.TextScalar> EnumerateScalars(string text, TextRange range)
    {
        for (int i = range.Start; i < range.End;)
        {
            char current = text[i];
            if (char.IsHighSurrogate(current) && i + 1 < range.End && char.IsLowSurrogate(text[i + 1]))
            {
                yield return new InitialInfo.TextScalar(i, 2, new Rune(char.ConvertToUtf32(current, text[i + 1])));
                i += 2;
            }
            else if (char.IsSurrogate(current))
            {
                yield return new InitialInfo.TextScalar(i, 1, new Rune(0xFFFD));
                i++;
            }
            else
            {
                yield return new InitialInfo.TextScalar(i, 1, new Rune(current));
                i++;
            }
        }
    }

    private static int? CodeUnitLengthAt(string text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return null;
        }

        char current = text[index];
        if (char.IsLowSurrogate(current) && index > 0 && char.IsHighSurrogate(text[index - 1]))
        {
            return null;
        }

        if (char.IsHighSurrogate(current) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            return 2;
        }

        return 1;
    }

    private static BidiClass FirstClassOrDefault(
        IEnumerable<int> indices,
        IReadOnlyList<BidiClass> classes,
        Func<BidiClass, bool> predicate,
        BidiClass defaultValue)
    {
        foreach (int index in indices)
        {
            BidiClass bidiClass = classes[index];
            if (predicate(bidiClass))
            {
                return bidiClass;
            }
        }

        return defaultValue;
    }

    private static IEnumerable<int> Flatten(IEnumerable<TextRange> ranges)
    {
        foreach (TextRange range in ranges)
        {
            for (int i = range.Start; i < range.End; i++)
            {
                yield return i;
            }
        }
    }

    private static void SetRange(BidiClass[] classes, int start, int end, BidiClass value)
    {
        for (int i = start; i < end; i++)
        {
            classes[i] = value;
        }
    }

    private static bool IsNI(BidiClass bidiClass)
    {
        return bidiClass is BidiClass.B
            or BidiClass.S
            or BidiClass.WS
            or BidiClass.ON
            or BidiClass.FSI
            or BidiClass.LRI
            or BidiClass.RLI
            or BidiClass.PDI;
    }

    private readonly record struct BracketPair(int Start, int End, int StartRun, int EndRun);
}
