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

/// <summary>
/// Directionality of resolved bidi levels in a paragraph or line.
/// </summary>
public enum Direction
{
    /// <summary>All inspected levels are left-to-right.</summary>
    Ltr,
    /// <summary>All inspected levels are right-to-left.</summary>
    Rtl,
    /// <summary>The inspected levels contain both left-to-right and right-to-left levels.</summary>
    Mixed,
}

/// <summary>
/// A half-open UTF-16 code-unit range.
/// </summary>
/// <param name="Start">The inclusive start code-unit index.</param>
/// <param name="End">The exclusive end code-unit index.</param>
public readonly record struct TextRange(int Start, int End)
{
    /// <summary>
    /// Gets the number of UTF-16 code units in the range.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// Gets whether the range contains no code units.
    /// </summary>
    public bool IsEmpty => Length == 0;
}

/// <summary>
/// Bidi information about a single paragraph.
/// </summary>
/// <param name="Range">The paragraph range in UTF-16 code units.</param>
/// <param name="Level">The paragraph embedding level.</param>
public sealed record ParagraphInfo(TextRange Range, Level Level)
{
    /// <summary>
    /// Gets the paragraph length in UTF-16 code units.
    /// </summary>
    public int Length => Range.Length;

    /// <summary>
    /// Returns the paragraph length, matching the Rust crate API name.
    /// </summary>
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

    /// <summary>
    /// Gets the original input text.
    /// </summary>
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

    /// <summary>
    /// Computes initial bidi classes and paragraph boundaries for text.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="defaultParaLevel">The paragraph level to use, or <see langword="null"/> to determine it from text.</param>
    /// <returns>The initial bidi information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static InitialInfo New(string text, Level? defaultParaLevel = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return InitialInfoExt.New(text, defaultParaLevel).Base;
    }

    /// <summary>
    /// Computes initial bidi classes and paragraph boundaries for text.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="defaultParaLevel">The paragraph level to use, or <see langword="null"/> to determine it from text.</param>
    /// <returns>The initial bidi information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static InitialInfo Create(string text, Level? defaultParaLevel = null)
    {
        return New(text, defaultParaLevel);
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
/// Bidi information of text treated as a single paragraph.
/// </summary>
public sealed class ParagraphBidiInfo
{
    private ParagraphBidiInfo(
        string text,
        BidiClass[] originalClasses,
        Level[] levels,
        Level paragraphLevel,
        bool isPureLtr)
    {
        Text = text;
        OriginalClasses = originalClasses;
        Levels = levels;
        ParagraphLevel = paragraphLevel;
        IsPureLtr = isPureLtr;
    }

    /// <summary>
    /// Gets the original input text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the original bidi class for each UTF-16 code unit in the input text.
    /// </summary>
    public IReadOnlyList<BidiClass> OriginalClasses { get; }

    /// <summary>
    /// Gets the resolved embedding level for each UTF-16 code unit in the input text.
    /// </summary>
    public IReadOnlyList<Level> Levels { get; }

    /// <summary>
    /// Gets the paragraph embedding level.
    /// </summary>
    public Level ParagraphLevel { get; }

    /// <summary>
    /// Gets whether the paragraph can be treated as purely left-to-right.
    /// </summary>
    public bool IsPureLtr { get; }

    /// <summary>
    /// Computes bidi information for text treated as one paragraph.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="defaultParaLevel">The paragraph level to use, or <see langword="null"/> to determine it from text.</param>
    /// <returns>The resolved single-paragraph bidi information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static ParagraphBidiInfo New(string text, Level? defaultParaLevel = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        (BidiClass[] originalClasses, Level paragraphLevel, bool isPureLtr, bool hasIsolateControls) =
            InitialInfo.ComputeInitialInfo(
                text,
                defaultParaLevel,
                splitParagraphs: false,
                paragraphs: null,
                flags: null);

        Level[] levels = [];
        if (text.Length != 0)
        {
            BidiClass[] processingClasses = originalClasses.ToArray();
            List<Level> resolvedLevels = [];
            BidiInfo.ComputeBidiInfoForPara(
                new ParagraphInfo(new TextRange(0, text.Length), paragraphLevel),
                isPureLtr,
                hasIsolateControls,
                text,
                originalClasses.ToArray(),
                processingClasses,
                resolvedLevels);
            levels = resolvedLevels.ToArray();
        }

        return new ParagraphBidiInfo(text, originalClasses, levels, paragraphLevel, isPureLtr);
    }

    /// <summary>
    /// Computes bidi information for text treated as one paragraph.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="defaultParaLevel">The paragraph level to use, or <see langword="null"/> to determine it from text.</param>
    /// <returns>The resolved single-paragraph bidi information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static ParagraphBidiInfo Create(string text, Level? defaultParaLevel = null)
    {
        return New(text, defaultParaLevel);
    }
}

/// <summary>
/// Lightweight helpers for bidi operations that do not need full level resolution.
/// </summary>
public static class Bidi
{
    /// <summary>
    /// Gets the base direction of the first paragraph according to the first strong character outside isolate runs.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <returns>The base direction, or <see cref="Direction.Mixed"/> when no strong character is found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static Direction GetBaseDirection(string text)
    {
        return GetBaseDirectionImpl(text, useFullText: false);
    }

    /// <summary>
    /// Gets the base direction, continuing after neutral paragraphs until a strong character is found.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <returns>The base direction, or <see cref="Direction.Mixed"/> when no strong character is found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static Direction GetBaseDirectionFull(string text)
    {
        return GetBaseDirectionImpl(text, useFullText: true);
    }

    private static Direction GetBaseDirectionImpl(string text, bool useFullText)
    {
        ArgumentNullException.ThrowIfNull(text);

        int isolateLevel = 0;
        foreach (InitialInfo.TextScalar scalar in InitialInfo.EnumerateScalars(text))
        {
            switch (CharData.BidiClass(scalar.Rune))
            {
                case BidiClass.LRI:
                case BidiClass.RLI:
                case BidiClass.FSI:
                    isolateLevel++;
                    break;

                case BidiClass.PDI when isolateLevel > 0:
                    isolateLevel--;
                    break;

                case BidiClass.L when isolateLevel == 0:
                    return Direction.Ltr;

                case BidiClass.R when isolateLevel == 0:
                case BidiClass.AL when isolateLevel == 0:
                    return Direction.Rtl;

                case BidiClass.B when !useFullText:
                    return Direction.Mixed;

                case BidiClass.B:
                    isolateLevel = 0;
                    break;
            }
        }

        return Direction.Mixed;
    }
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

    /// <summary>
    /// Gets the original input text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the original bidi class for each UTF-16 code unit in the input text.
    /// </summary>
    public IReadOnlyList<BidiClass> OriginalClasses { get; }

    /// <summary>
    /// Gets the resolved embedding level for each UTF-16 code unit in the input text.
    /// </summary>
    public IReadOnlyList<Level> Levels { get; }

    /// <summary>
    /// Gets the resolved paragraph ranges and paragraph levels.
    /// </summary>
    public IReadOnlyList<ParagraphInfo> Paragraphs { get; }

    /// <summary>
    /// Returns whether any resolved embedding level is right-to-left.
    /// </summary>
    public bool HasRtl()
    {
        return Levels.Any(level => level.IsRtl());
    }

    /// <summary>
    /// Determines whether the paragraph resolves to left-to-right, right-to-left, or mixed direction.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    public Direction ParagraphDirection(ParagraphInfo paragraph)
    {
        return ParaDirection(Levels.Skip(paragraph.Range.Start).Take(paragraph.Range.Length));
    }

    /// <summary>
    /// Gets the resolved level at a paragraph-relative UTF-16 code-unit position.
    /// </summary>
    /// <param name="paragraph">The paragraph containing the position.</param>
    /// <param name="position">The UTF-16 code-unit offset relative to the paragraph start.</param>
    public Level LevelAt(ParagraphInfo paragraph, int position)
    {
        return Levels[paragraph.Range.Start + position];
    }

    /// <summary>
    /// Applies the UAX #9 L1/L2 line reordering rules and returns levels for the whole text.
    /// </summary>
    /// <param name="paragraph">The paragraph containing <paramref name="line"/>.</param>
    /// <param name="line">The line range in UTF-16 code units.</param>
    /// <returns>A copy of the resolved levels with the specified line adjusted for visual reordering.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="line"/> is outside the input text.</exception>
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

    /// <summary>
    /// Applies line reordering and returns one level per Unicode scalar value in the input text.
    /// </summary>
    /// <param name="paragraph">The paragraph containing <paramref name="line"/>.</param>
    /// <param name="line">The line range in UTF-16 code units.</param>
    /// <returns>The reordered levels projected to scalar-value positions.</returns>
    public Level[] ReorderedLevelsPerChar(ParagraphInfo paragraph, TextRange line)
    {
        Level[] levels = ReorderedLevels(paragraph, line);
        return InitialInfo.EnumerateScalars(Text)
            .Select(scalar => levels[scalar.CodeUnitIndex])
            .ToArray();
    }

    /// <summary>
    /// Returns the visual-order text for a single line.
    /// </summary>
    /// <param name="paragraph">The paragraph containing <paramref name="line"/>.</param>
    /// <param name="line">The line range in UTF-16 code units.</param>
    /// <returns>The line text reordered into visual order.</returns>
    /// <remarks>Glyph mirroring and combining-mark shaping are outside this API, matching the upstream crate boundary.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="line"/> is outside the input text.</exception>
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

    /// <summary>
    /// Gets visual runs for a line after applying UAX #9 line-level reordering.
    /// </summary>
    /// <param name="paragraph">The paragraph containing <paramref name="line"/>.</param>
    /// <param name="line">The line range in UTF-16 code units.</param>
    /// <returns>The adjusted levels and visual-order UTF-16 ranges.</returns>
    public (Level[] Levels, TextRange[] Runs) VisualRuns(ParagraphInfo paragraph, TextRange line)
    {
        Level[] levels = ReorderedLevels(paragraph, line);
        return VisualRunsForLine(levels, line);
    }

    /// <summary>
    /// Computes the visual order of level-indexed items.
    /// </summary>
    /// <param name="levels">The bidi levels for logical-order items.</param>
    /// <returns>Indices that map logical-order items into visual order.</returns>
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

    /// <summary>
    /// Computes full bidi information for text.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="defaultParaLevel">The paragraph level to use, or <see langword="null"/> to determine it from text.</param>
    /// <returns>The resolved bidi information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Computes full bidi information for text.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="defaultParaLevel">The paragraph level to use, or <see langword="null"/> to determine it from text.</param>
    /// <returns>The resolved bidi information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public static BidiInfo Create(string text, Level? defaultParaLevel = null)
    {
        return New(text, defaultParaLevel);
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

    internal static void ComputeBidiInfoForPara(
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
