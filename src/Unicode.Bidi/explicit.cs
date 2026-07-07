/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/explicit.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

namespace Unicode.Bidi;

internal static class Explicit
{
    internal static void Compute(
        string text,
        Level paraLevel,
        IReadOnlyList<BidiClass> originalClasses,
        Level[] levels,
        BidiClass[] processingClasses,
        List<TextRange> runs)
    {
        Stack<Status> stack = new();
        stack.Push(new Status(paraLevel, OverrideStatus.Neutral));

        uint overflowIsolateCount = 0;
        uint overflowEmbeddingCount = 0;
        uint validIsolateCount = 0;

        Level currentRunLevel = Level.Ltr();
        int currentRunStart = 0;

        foreach (InitialInfo.TextScalar scalar in InitialInfo.EnumerateScalars(text))
        {
            int i = scalar.CodeUnitIndex;
            int length = scalar.CodeUnitLength;
            Status last = stack.Peek();

            switch (originalClasses[i])
            {
                case BidiClass.RLE:
                case BidiClass.LRE:
                case BidiClass.RLO:
                case BidiClass.LRO:
                case BidiClass.RLI:
                case BidiClass.LRI:
                case BidiClass.FSI:
                    levels[i] = last.Level;

                    bool isIsolate = originalClasses[i] is BidiClass.RLI or BidiClass.LRI or BidiClass.FSI;
                    if (isIsolate)
                    {
                        ApplyOverride(last.Override, processingClasses, i);
                    }

                    Level? newLevel = TryNextLevel(last.Level, originalClasses[i]);
                    if (newLevel.HasValue && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                    {
                        OverrideStatus newStatus = originalClasses[i] switch
                        {
                            BidiClass.RLO => OverrideStatus.Rtl,
                            BidiClass.LRO => OverrideStatus.Ltr,
                            BidiClass.RLI or BidiClass.LRI or BidiClass.FSI => OverrideStatus.Isolate,
                            _ => OverrideStatus.Neutral,
                        };
                        stack.Push(new Status(newLevel.Value, newStatus));

                        if (isIsolate)
                        {
                            validIsolateCount++;
                        }
                        else
                        {
                            levels[i] = newLevel.Value;
                        }
                    }
                    else if (isIsolate)
                    {
                        overflowIsolateCount++;
                    }
                    else if (overflowIsolateCount == 0)
                    {
                        overflowEmbeddingCount++;
                    }

                    if (!isIsolate)
                    {
                        processingClasses[i] = BidiClass.BN;
                    }

                    break;

                case BidiClass.PDI:
                    if (overflowIsolateCount > 0)
                    {
                        overflowIsolateCount--;
                    }
                    else if (validIsolateCount > 0)
                    {
                        overflowEmbeddingCount = 0;
                        while (stack.Count > 0 && stack.Pop().Override != OverrideStatus.Isolate)
                        {
                        }

                        validIsolateCount--;
                    }

                    last = stack.Peek();
                    levels[i] = last.Level;
                    ApplyOverride(last.Override, processingClasses, i);
                    break;

                case BidiClass.PDF:
                    if (overflowIsolateCount > 0)
                    {
                    }
                    else if (overflowEmbeddingCount > 0)
                    {
                        overflowEmbeddingCount--;
                    }
                    else if (last.Override != OverrideStatus.Isolate && stack.Count >= 2)
                    {
                        stack.Pop();
                    }

                    levels[i] = stack.Peek().Level;
                    processingClasses[i] = BidiClass.BN;
                    break;

                case BidiClass.B:
                    break;

                default:
                    levels[i] = last.Level;
                    if (originalClasses[i] != BidiClass.BN)
                    {
                        ApplyOverride(last.Override, processingClasses, i);
                    }

                    break;
            }

            for (int j = 1; j < length; j++)
            {
                levels[i + j] = levels[i];
                processingClasses[i + j] = processingClasses[i];
            }

            if (i == 0)
            {
                currentRunLevel = levels[i];
            }
            else if (!Prepare.RemovedByX9(originalClasses[i]) && levels[i] != currentRunLevel)
            {
                runs.Add(new TextRange(currentRunStart, i));
                currentRunLevel = levels[i];
                currentRunStart = i;
            }
        }

        if (levels.Length > currentRunStart)
        {
            runs.Add(new TextRange(currentRunStart, levels.Length));
        }
    }

    private static void ApplyOverride(OverrideStatus status, BidiClass[] processingClasses, int index)
    {
        switch (status)
        {
            case OverrideStatus.Rtl:
                processingClasses[index] = BidiClass.R;
                break;
            case OverrideStatus.Ltr:
                processingClasses[index] = BidiClass.L;
                break;
        }
    }

    private static Level? TryNextLevel(Level level, BidiClass bidiClass)
    {
        try
        {
            return CharData.IsRtl(bidiClass)
                ? level.NewExplicitNextRtl()
                : level.NewExplicitNextLtr();
        }
        catch (LevelOutOfRangeException)
        {
            return null;
        }
    }

    private readonly record struct Status(Level Level, OverrideStatus Override);

    private enum OverrideStatus
    {
        Neutral,
        Rtl,
        Ltr,
        Isolate,
    }
}
