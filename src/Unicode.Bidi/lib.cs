/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/lib.rs
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

public enum Direction
{
    Ltr,
    Rtl,
    Mixed,
}

public readonly record struct TextRange(int Start, int End)
{
    public int Length => End - Start;

    public bool IsEmpty => Length == 0;
}

/// <summary>
/// Bidi information about a single paragraph.
/// </summary>
public sealed record ParagraphInfo(TextRange Range, Level Level)
{
    public int Length => Range.Length;

    public int Len()
    {
        return Length;
    }
}

/// <summary>
/// Initial bidi information of the text.
/// </summary>
public sealed class InitialInfo
{
    internal InitialInfo(string text, BidiClass[] originalClasses, ParagraphInfo[] paragraphs)
    {
        Text = text;
        OriginalClasses = originalClasses;
        Paragraphs = paragraphs;
    }

    public string Text { get; }

    /// <summary>
    /// The BidiClass of the character at each UTF-16 code unit in the text.
    /// If a scalar uses a surrogate pair, its class appears twice.
    /// </summary>
    public IReadOnlyList<BidiClass> OriginalClasses { get; }

    /// <summary>
    /// Paragraph boundaries and levels, expressed as UTF-16 code-unit ranges.
    /// </summary>
    public IReadOnlyList<ParagraphInfo> Paragraphs { get; }

    public static InitialInfo New(string text, Level? defaultParaLevel = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return InitialInfoExt.New(text, defaultParaLevel).Base;
    }

    internal static (BidiClass[] OriginalClasses, Level ParagraphLevel, bool IsPureLtr, bool HasIsolateControls)
        ComputeInitialInfo(string text, Level? defaultParaLevel, bool splitParagraphs, List<ParagraphInfo>? paragraphs, List<ParagraphInfoFlags>? flags)
    {
        List<BidiClass> originalClasses = new(text.Length);
        List<int> isolateStack = [];

        int paraStart = 0;
        Level? paraLevel = defaultParaLevel;
        bool isPureLtr = true;
        bool hasIsolateControls = false;

        foreach (TextScalar scalar in EnumerateScalars(text))
        {
            BidiClass bidiClass = CharData.BidiClass(scalar.Rune);
            for (int j = 0; j < scalar.CodeUnitLength; j++)
            {
                originalClasses.Add(bidiClass);
            }

            switch (bidiClass)
            {
                case BidiClass.B:
                    if (splitParagraphs)
                    {
                        int paraEnd = scalar.CodeUnitIndex + scalar.CodeUnitLength;
                        paragraphs!.Add(new ParagraphInfo(new TextRange(paraStart, paraEnd), paraLevel ?? Level.LtrLevel));
                        flags!.Add(new ParagraphInfoFlags(isPureLtr, hasIsolateControls));

                        paraStart = paraEnd;
                        paraLevel = defaultParaLevel;
                        isPureLtr = true;
                        hasIsolateControls = false;
                        isolateStack.Clear();
                    }

                    break;

                case BidiClass.L:
                case BidiClass.R:
                case BidiClass.AL:
                    if (bidiClass != BidiClass.L)
                    {
                        isPureLtr = false;
                    }

                    if (isolateStack.Count > 0)
                    {
                        int start = isolateStack[^1];
                        if (originalClasses[start] == BidiClass.FSI)
                        {
                            originalClasses[start] = bidiClass == BidiClass.L ? BidiClass.LRI : BidiClass.RLI;
                        }
                    }
                    else if (!paraLevel.HasValue)
                    {
                        paraLevel = bidiClass == BidiClass.L ? Level.LtrLevel : Level.RtlLevel;
                    }

                    break;

                case BidiClass.AN:
                case BidiClass.LRE:
                case BidiClass.RLE:
                case BidiClass.LRO:
                case BidiClass.RLO:
                    isPureLtr = false;
                    break;

                case BidiClass.RLI:
                case BidiClass.LRI:
                case BidiClass.FSI:
                    isPureLtr = false;
                    hasIsolateControls = true;
                    isolateStack.Add(scalar.CodeUnitIndex);
                    break;

                case BidiClass.PDI:
                    if (isolateStack.Count > 0)
                    {
                        isolateStack.RemoveAt(isolateStack.Count - 1);
                    }

                    break;
            }
        }

        if (splitParagraphs && paraStart < text.Length)
        {
            paragraphs!.Add(new ParagraphInfo(new TextRange(paraStart, text.Length), paraLevel ?? Level.LtrLevel));
            flags!.Add(new ParagraphInfoFlags(isPureLtr, hasIsolateControls));
        }

        return (originalClasses.ToArray(), paraLevel ?? Level.LtrLevel, isPureLtr, hasIsolateControls);
    }

    internal static IEnumerable<TextScalar> EnumerateScalars(string text)
    {
        for (int i = 0; i < text.Length;)
        {
            char current = text[i];
            if (char.IsHighSurrogate(current) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                yield return new TextScalar(i, 2, new Rune(char.ConvertToUtf32(current, text[i + 1])));
                i += 2;
            }
            else if (char.IsSurrogate(current))
            {
                yield return new TextScalar(i, 1, new Rune(0xFFFD));
                i++;
            }
            else
            {
                yield return new TextScalar(i, 1, new Rune(current));
                i++;
            }
        }
    }

    internal readonly record struct TextScalar(int CodeUnitIndex, int CodeUnitLength, Rune Rune);
}

/// <summary>
/// Bidi information of the text.
/// </summary>
public sealed class BidiInfo
{
    private BidiInfo(string text, BidiClass[] originalClasses, Level[] levels, ParagraphInfo[] paragraphs)
    {
        Text = text;
        OriginalClasses = originalClasses;
        Levels = levels;
        Paragraphs = paragraphs;
    }

    public string Text { get; }

    public IReadOnlyList<BidiClass> OriginalClasses { get; }

    public IReadOnlyList<Level> Levels { get; }

    public IReadOnlyList<ParagraphInfo> Paragraphs { get; }

    public bool HasRtl()
    {
        return Levels.Any(level => level.IsRtl());
    }

    public Direction ParagraphDirection(ParagraphInfo paragraph)
    {
        return ParaDirection(Levels.Skip(paragraph.Range.Start).Take(paragraph.Range.Length));
    }

    public Level LevelAt(ParagraphInfo paragraph, int position)
    {
        return Levels[paragraph.Range.Start + position];
    }

    public Level[] ReorderedLevels(ParagraphInfo paragraph, TextRange line)
    {
        ValidateLine(line);

        Level[] levels = Levels.ToArray();
        if (line.IsEmpty)
        {
            return levels;
        }

        BidiClass[] lineClasses = OriginalClasses
            .Skip(line.Start)
            .Take(line.Length)
            .ToArray();
        Level[] lineLevels = levels
            .Skip(line.Start)
            .Take(line.Length)
            .ToArray();

        ReorderLevels(
            lineClasses,
            lineLevels,
            Text.Substring(line.Start, line.Length),
            paragraph.Level);

        for (int i = 0; i < lineLevels.Length; i++)
        {
            levels[line.Start + i] = lineLevels[i];
        }

        return levels;
    }

    public Level[] ReorderedLevelsPerChar(ParagraphInfo paragraph, TextRange line)
    {
        Level[] levels = ReorderedLevels(paragraph, line);
        return InitialInfo.EnumerateScalars(Text)
            .Select(scalar => levels[scalar.CodeUnitIndex])
            .ToArray();
    }

    public string ReorderLine(ParagraphInfo paragraph, TextRange line)
    {
        ValidateLine(line);
        if (line.IsEmpty || !HasRtl(Levels.Skip(line.Start).Take(line.Length)))
        {
            return Text.Substring(line.Start, line.Length);
        }

        (Level[] levels, TextRange[] runs) = VisualRuns(paragraph, line);
        return ReorderLine(Text, line, levels, runs);
    }

    public (Level[] Levels, TextRange[] Runs) VisualRuns(ParagraphInfo paragraph, TextRange line)
    {
        Level[] levels = ReorderedLevels(paragraph, line);
        return VisualRunsForLine(levels, line);
    }

    public static int[] ReorderVisual(IReadOnlyList<Level> levels)
    {
        if (levels.Count == 0)
        {
            return [];
        }

        Level minLevel = levels[0];
        Level maxLevel = levels[0];
        foreach (Level level in levels)
        {
            if (level < minLevel)
            {
                minLevel = level;
            }

            if (level > maxLevel)
            {
                maxLevel = level;
            }
        }

        int[] result = Enumerable.Range(0, levels.Count).ToArray();
        if (minLevel == maxLevel && minLevel.IsLtr())
        {
            return result;
        }

        minLevel = minLevel.NewLowestGeRtl();
        while (minLevel <= maxLevel)
        {
            TextRange range = new(0, 0);
            while (true)
            {
                range = NextRange(levels, range.End, maxLevel);
                Array.Reverse(result, range.Start, range.Length);

                if (range.End >= levels.Count)
                {
                    break;
                }
            }

            maxLevel = maxLevel.Lower(1);
        }

        return result;
    }

    public static BidiInfo New(string text, Level? defaultParaLevel = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        InitialInfoExt initial = InitialInfoExt.New(text, defaultParaLevel);
        BidiClass[] processingClasses = initial.Base.OriginalClasses.ToArray();
        List<Level> resolvedLevels = [];
        foreach ((ParagraphInfo paragraph, ParagraphInfoFlags flags) in initial.Base.Paragraphs.Zip(initial.Flags))
        {
            ComputeBidiInfoForPara(
                paragraph,
                flags.IsPureLtr,
                flags.HasIsolateControls,
                text,
                initial.Base.OriginalClasses.ToArray(),
                processingClasses,
                resolvedLevels);
        }

        return new BidiInfo(
            text,
            initial.Base.OriginalClasses.ToArray(),
            resolvedLevels.ToArray(),
            initial.Base.Paragraphs.ToArray());
    }

    internal static ExplicitDiagnostics ComputeExplicitDiagnostics(string text, Level? defaultParaLevel = null)
    {
        InitialInfoExt initial = InitialInfoExt.New(text, defaultParaLevel);
        BidiClass[] processingClasses = initial.Base.OriginalClasses.ToArray();
        List<Level> levels = [];
        List<ParagraphExplicitDiagnostics> paragraphDiagnostics = [];

        foreach ((ParagraphInfo paragraph, ParagraphInfoFlags flags) in initial.Base.Paragraphs.Zip(initial.Flags))
        {
            BidiClass[] paragraphOriginalClasses = initial.Base.OriginalClasses
                .Skip(paragraph.Range.Start)
                .Take(paragraph.Range.Length)
                .ToArray();
            BidiClass[] paragraphProcessingClasses = processingClasses
                .Skip(paragraph.Range.Start)
                .Take(paragraph.Range.Length)
                .ToArray();
            Level[] paragraphLevels = Enumerable.Repeat(paragraph.Level, paragraph.Range.Length).ToArray();
            List<TextRange> levelRuns = [];

            Explicit.Compute(
                text.Substring(paragraph.Range.Start, paragraph.Range.Length),
                paragraph.Level,
                paragraphOriginalClasses,
                paragraphLevels,
                paragraphProcessingClasses,
                levelRuns);

            List<IsolatingRunSequence> sequences = [];
            Prepare.IsolatingRunSequences(
                paragraph.Level,
                paragraphOriginalClasses,
                paragraphLevels,
                levelRuns,
                flags.HasIsolateControls,
                sequences);

            paragraphDiagnostics.Add(new ParagraphExplicitDiagnostics(
                paragraph,
                paragraphLevels,
                paragraphProcessingClasses,
                levelRuns.ToArray(),
                sequences.ToArray()));

            for (int i = paragraph.Range.Start; i < paragraph.Range.End; i++)
            {
                int local = i - paragraph.Range.Start;
                levels.Add(paragraphLevels[local]);
                processingClasses[i] = paragraphProcessingClasses[local];
            }
        }

        return new ExplicitDiagnostics(
            initial.Base.OriginalClasses.ToArray(),
            processingClasses,
            levels.ToArray(),
            initial.Base.Paragraphs.ToArray(),
            paragraphDiagnostics.ToArray());
    }

    private static void ComputeBidiInfoForPara(
        ParagraphInfo paragraph,
        bool isPureLtr,
        bool hasIsolateControls,
        string text,
        BidiClass[] originalClasses,
        BidiClass[] processingClasses,
        List<Level> levels)
    {
        int newLength = levels.Count + paragraph.Range.Length;
        while (levels.Count < newLength)
        {
            levels.Add(paragraph.Level);
        }

        if (paragraph.Level == Level.LtrLevel && isPureLtr)
        {
            return;
        }

        BidiClass[] paragraphOriginalClasses = originalClasses
            .Skip(paragraph.Range.Start)
            .Take(paragraph.Range.Length)
            .ToArray();
        BidiClass[] paragraphProcessingClasses = processingClasses
            .Skip(paragraph.Range.Start)
            .Take(paragraph.Range.Length)
            .ToArray();
        Level[] paragraphLevels = levels
            .Skip(paragraph.Range.Start)
            .Take(paragraph.Range.Length)
            .ToArray();
        List<TextRange> levelRuns = [];
        string paragraphText = text.Substring(paragraph.Range.Start, paragraph.Range.Length);

        Explicit.Compute(
            paragraphText,
            paragraph.Level,
            paragraphOriginalClasses,
            paragraphLevels,
            paragraphProcessingClasses,
            levelRuns);

        List<IsolatingRunSequence> sequences = [];
        Prepare.IsolatingRunSequences(
            paragraph.Level,
            paragraphOriginalClasses,
            paragraphLevels,
            levelRuns,
            hasIsolateControls,
            sequences);

        foreach (IsolatingRunSequence sequence in sequences)
        {
            Implicit.ResolveWeak(paragraphText, sequence, paragraphProcessingClasses);
            Implicit.ResolveNeutral(
                paragraphText,
                sequence,
                paragraphLevels,
                paragraphOriginalClasses,
                paragraphProcessingClasses);
        }

        Implicit.ResolveLevels(paragraphProcessingClasses, paragraphLevels);
        AssignLevelsToRemovedChars(paragraph.Level, paragraphOriginalClasses, paragraphLevels);

        for (int i = 0; i < paragraph.Range.Length; i++)
        {
            levels[paragraph.Range.Start + i] = paragraphLevels[i];
            processingClasses[paragraph.Range.Start + i] = paragraphProcessingClasses[i];
        }
    }

    private static void AssignLevelsToRemovedChars(Level paraLevel, IReadOnlyList<BidiClass> classes, Level[] levels)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (Prepare.RemovedByX9(classes[i]))
            {
                levels[i] = i > 0 ? levels[i - 1] : paraLevel;
            }
        }
    }

    private void ValidateLine(TextRange line)
    {
        if (line.Start < 0 || line.End < line.Start || line.End > Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(line));
        }
    }

    private static void ReorderLevels(
        IReadOnlyList<BidiClass> lineClasses,
        Level[] lineLevels,
        string lineText,
        Level paraLevel)
    {
        int? resetFrom = 0;
        int? resetTo = null;
        Level prevLevel = paraLevel;

        foreach (InitialInfo.TextScalar scalar in InitialInfo.EnumerateScalars(lineText))
        {
            int i = scalar.CodeUnitIndex;
            switch (lineClasses[i])
            {
                case BidiClass.B:
                case BidiClass.S:
                    resetTo = i + scalar.CodeUnitLength;
                    resetFrom ??= i;
                    break;

                case BidiClass.WS:
                case BidiClass.FSI:
                case BidiClass.LRI:
                case BidiClass.RLI:
                case BidiClass.PDI:
                    resetFrom ??= i;
                    break;

                case BidiClass.RLE:
                case BidiClass.LRE:
                case BidiClass.RLO:
                case BidiClass.LRO:
                case BidiClass.PDF:
                case BidiClass.BN:
                    resetFrom ??= i;
                    for (int j = i; j < i + scalar.CodeUnitLength; j++)
                    {
                        lineLevels[j] = prevLevel;
                    }

                    break;

                default:
                    resetFrom = null;
                    break;
            }

            if (resetFrom.HasValue && resetTo.HasValue)
            {
                for (int j = resetFrom.Value; j < resetTo.Value; j++)
                {
                    lineLevels[j] = paraLevel;
                }

                resetFrom = null;
                resetTo = null;
            }

            prevLevel = lineLevels[i];
        }

        if (resetFrom.HasValue)
        {
            for (int j = resetFrom.Value; j < lineLevels.Length; j++)
            {
                lineLevels[j] = paraLevel;
            }
        }
    }

    private static (Level[] Levels, TextRange[] Runs) VisualRunsForLine(Level[] levels, TextRange line)
    {
        if (line.IsEmpty)
        {
            return (levels, []);
        }

        List<TextRange> runs = [];
        int start = line.Start;
        Level runLevel = levels[start];
        Level minLevel = runLevel;
        Level maxLevel = runLevel;

        for (int i = start + 1; i < line.End; i++)
        {
            Level newLevel = levels[i];
            if (newLevel != runLevel)
            {
                runs.Add(new TextRange(start, i));
                start = i;
                runLevel = newLevel;
                if (runLevel < minLevel)
                {
                    minLevel = runLevel;
                }

                if (runLevel > maxLevel)
                {
                    maxLevel = runLevel;
                }
            }
        }

        runs.Add(new TextRange(start, line.End));
        int runCount = runs.Count;
        minLevel = minLevel.NewLowestGeRtl();
        while (maxLevel >= minLevel)
        {
            int seqStart = 0;
            while (seqStart < runCount)
            {
                if (levels[runs[seqStart].Start] < maxLevel)
                {
                    seqStart++;
                    continue;
                }

                int seqEnd = seqStart + 1;
                while (seqEnd < runCount && levels[runs[seqEnd].Start] >= maxLevel)
                {
                    seqEnd++;
                }

                runs.Reverse(seqStart, seqEnd - seqStart);
                seqStart = seqEnd;
            }

            maxLevel = maxLevel.Lower(1);
        }

        return (levels, runs.ToArray());
    }

    private static string ReorderLine(string text, TextRange line, IReadOnlyList<Level> levels, IReadOnlyList<TextRange> runs)
    {
        if (runs.All(run => levels[run.Start].IsLtr()))
        {
            return text.Substring(line.Start, line.Length);
        }

        StringBuilder result = new(line.Length);
        foreach (TextRange run in runs)
        {
            if (levels[run.Start].IsRtl())
            {
                AppendReversedScalars(result, text, run);
            }
            else
            {
                result.Append(text, run.Start, run.Length);
            }
        }

        return result.ToString();
    }

    private static void AppendReversedScalars(StringBuilder result, string text, TextRange range)
    {
        for (int i = range.End; i > range.Start;)
        {
            char previous = text[i - 1];
            if (char.IsLowSurrogate(previous)
                && i - 2 >= range.Start
                && char.IsHighSurrogate(text[i - 2]))
            {
                result.Append(text[i - 2]);
                result.Append(previous);
                i -= 2;
            }
            else
            {
                result.Append(previous);
                i--;
            }
        }
    }

    private static TextRange NextRange(IReadOnlyList<Level> levels, int startIndex, Level maxLevel)
    {
        if (levels.Count == 0 || startIndex >= levels.Count)
        {
            return new TextRange(startIndex, startIndex);
        }

        while (startIndex < levels.Count && levels[startIndex] < maxLevel)
        {
            startIndex++;
        }

        if (startIndex >= levels.Count)
        {
            return new TextRange(startIndex, startIndex);
        }

        int endIndex = startIndex + 1;
        while (endIndex < levels.Count && levels[endIndex] >= maxLevel)
        {
            endIndex++;
        }

        return new TextRange(startIndex, endIndex);
    }

    private static Direction ParaDirection(IEnumerable<Level> levels)
    {
        bool ltr = false;
        bool rtl = false;
        foreach (Level level in levels)
        {
            if (level.IsLtr())
            {
                ltr = true;
                if (rtl)
                {
                    return Direction.Mixed;
                }
            }

            if (level.IsRtl())
            {
                rtl = true;
                if (ltr)
                {
                    return Direction.Mixed;
                }
            }
        }

        return ltr ? Direction.Ltr : Direction.Rtl;
    }

    private static bool HasRtl(IEnumerable<Level> levels)
    {
        return levels.Any(level => level.IsRtl());
    }
}

internal sealed record ExplicitDiagnostics(
    BidiClass[] OriginalClasses,
    BidiClass[] ProcessingClasses,
    Level[] Levels,
    ParagraphInfo[] Paragraphs,
    ParagraphExplicitDiagnostics[] ParagraphDiagnostics);

internal sealed record ParagraphExplicitDiagnostics(
    ParagraphInfo Paragraph,
    Level[] Levels,
    BidiClass[] ProcessingClasses,
    TextRange[] LevelRuns,
    IsolatingRunSequence[] IsolatingRunSequences);

internal sealed class InitialInfoExt
{
    private InitialInfoExt(InitialInfo @base, ParagraphInfoFlags[] flags)
    {
        Base = @base;
        Flags = flags;
    }

    public InitialInfo Base { get; }

    public IReadOnlyList<ParagraphInfoFlags> Flags { get; }

    public static InitialInfoExt New(string text, Level? defaultParaLevel = null)
    {
        List<ParagraphInfo> paragraphs = [];
        List<ParagraphInfoFlags> flags = [];
        (BidiClass[] originalClasses, _, _, _) =
            InitialInfo.ComputeInitialInfo(text, defaultParaLevel, splitParagraphs: true, paragraphs, flags);

        InitialInfo initialInfo = new(
            text,
            originalClasses,
            paragraphs.ToArray());

        return new InitialInfoExt(initialInfo, flags.ToArray());
    }
}

internal sealed record ParagraphInfoFlags(bool IsPureLtr, bool HasIsolateControls);
