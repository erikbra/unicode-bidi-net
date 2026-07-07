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

    private static IEnumerable<TextScalar> EnumerateScalars(string text)
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

    private readonly record struct TextScalar(int CodeUnitIndex, int CodeUnitLength, Rune Rune);
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

    public static BidiInfo New(string text, Level? defaultParaLevel = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        InitialInfoExt initial = InitialInfoExt.New(text, defaultParaLevel);
        Level[] levels = new Level[text.Length];
        foreach (ParagraphInfo paragraph in initial.Base.Paragraphs)
        {
            for (int i = paragraph.Range.Start; i < paragraph.Range.End; i++)
            {
                levels[i] = paragraph.Level;
            }
        }

        return new BidiInfo(
            text,
            initial.Base.OriginalClasses.ToArray(),
            levels,
            initial.Base.Paragraphs.ToArray());
    }
}

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
