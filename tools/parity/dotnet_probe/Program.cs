/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted for parity checks against Rust unicode-bidi with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: parity-harness
 */

using System.Globalization;
using System.Text;
using Unicode.Bidi;

if (args.Length < 1)
{
    Usage();
    return 2;
}

switch (args[0])
{
    case "bidi-class" when args.Length == 2:
        int codePoint = ParseCodePoint(args[1]);
        BidiClass bidiClass = CharData.BidiClass(new Rune(codePoint));
        Console.WriteLine(bidiClass);
        return 0;

    case "initial-info" when args.Length == 3:
        string text = ParseUtf16Text(args[1]);
        InitialInfo initialInfo = InitialInfo.New(text, ParseDefaultLevel(args[2]));
        Console.WriteLine(FormatInitialInfo(initialInfo));
        return 0;

    case "bidi-info" when args.Length == 3:
        string bidiText = ParseUtf16Text(args[1]);
        BidiInfo bidiInfo = BidiInfo.New(bidiText, ParseDefaultLevel(args[2]));
        Console.WriteLine(FormatBidiInfo(bidiInfo));
        return 0;

    case "reorder-line" when args.Length == 3:
        string reorderText = ParseUtf16Text(args[1]);
        BidiInfo reorderInfo = BidiInfo.New(reorderText, ParseDefaultLevel(args[2]));
        Console.WriteLine(FormatReorderedLines(reorderInfo));
        return 0;

    case "conformance" when args.Length == 3:
        string conformanceText = ParseUtf16Text(args[1]);
        BidiInfo conformanceInfo = BidiInfo.New(conformanceText, ParseDefaultLevel(args[2]));
        Console.WriteLine(FormatConformance(conformanceInfo));
        return 0;

    case "conformance-batch" when args.Length == 1:
        return RunConformanceBatch();

    default:
        Usage();
        return 2;
}

static void Usage()
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine("  dotnet run --project tools/parity/dotnet_probe -- bidi-class <codepoint>");
    Console.Error.WriteLine("  dotnet run --project tools/parity/dotnet_probe -- initial-info <utf16-code-units> <auto|ltr|rtl|number>");
    Console.Error.WriteLine("  dotnet run --project tools/parity/dotnet_probe -- bidi-info <utf16-code-units> <auto|ltr|rtl|number>");
    Console.Error.WriteLine("  dotnet run --project tools/parity/dotnet_probe -- reorder-line <utf16-code-units> <auto|ltr|rtl|number>");
    Console.Error.WriteLine("  dotnet run --project tools/parity/dotnet_probe -- conformance <utf16-code-units> <auto|ltr|rtl|number>");
    Console.Error.WriteLine("  dotnet run --project tools/parity/dotnet_probe -- conformance-batch < cases.tsv");
}

static int ParseCodePoint(string value)
{
    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        return int.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    return int.Parse(value, CultureInfo.InvariantCulture);
}

static string ParseUtf16Text(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    return new string(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(unit => (char)int.Parse(unit, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        .ToArray());
}

static Level? ParseDefaultLevel(string value)
{
    return value switch
    {
        "auto" => null,
        "ltr" => Level.Ltr(),
        "rtl" => Level.Rtl(),
        _ => Level.New(byte.Parse(value, CultureInfo.InvariantCulture)),
    };
}

static string FormatInitialInfo(InitialInfo initialInfo)
{
    string classes = string.Join(",", initialInfo.OriginalClasses);
    string paragraphs = string.Join(",", initialInfo.Paragraphs.Select(paragraph =>
        $"{paragraph.Range.Start}-{paragraph.Range.End}:{paragraph.Level.Number()}"));

    return $"classes={classes};paragraphs={paragraphs}";
}

static string FormatBidiInfo(BidiInfo bidiInfo)
{
    string classes = string.Join(",", bidiInfo.OriginalClasses);
    string paragraphs = string.Join(",", bidiInfo.Paragraphs.Select(paragraph =>
        $"{paragraph.Range.Start}-{paragraph.Range.End}:{paragraph.Level.Number()}"));
    string levels = string.Join(",", bidiInfo.Levels.Select(level => level.Number()));
    return $"classes={classes};paragraphs={paragraphs};levels={levels}";
}

static string FormatReorderedLines(BidiInfo bidiInfo)
{
    string lines = string.Join("|", bidiInfo.Paragraphs.Select(paragraph =>
        FormatUtf16Units(bidiInfo.ReorderLine(paragraph, paragraph.Range))));
    return $"lines={lines}";
}

static string FormatUtf16Units(string text)
{
    return string.Join(",", text.Select(c => ((int)c).ToString("X4", CultureInfo.InvariantCulture)));
}

static int RunConformanceBatch()
{
    string? line;
    while ((line = Console.In.ReadLine()) is not null)
    {
        string[] fields = line.Split('\t');
        if (fields.Length != 2)
        {
            Console.Error.WriteLine("conformance-batch expects '<utf16-code-units>\\t<auto|ltr|rtl|number>' per line.");
            return 2;
        }

        string text = ParseUtf16Text(fields[0]);
        BidiInfo bidiInfo = BidiInfo.New(text, ParseDefaultLevel(fields[1]));
        Console.WriteLine(FormatConformance(bidiInfo));
    }

    return 0;
}

static string FormatConformance(BidiInfo bidiInfo)
{
    if (bidiInfo.Paragraphs.Count == 0)
    {
        return "para=;levels=;reorder=";
    }

    ParagraphInfo paragraph = bidiInfo.Paragraphs[0];
    Level[] reorderedLevels = bidiInfo.ReorderedLevels(paragraph, paragraph.Range);
    InitialInfo.TextScalar[] scalars = InitialInfo.EnumerateScalars(bidiInfo.Text).ToArray();

    string levels = string.Join(" ", scalars.Select(scalar =>
        Prepare.RemovedByX9(bidiInfo.OriginalClasses[scalar.CodeUnitIndex])
            ? "x"
            : reorderedLevels[scalar.CodeUnitIndex].Number().ToString(CultureInfo.InvariantCulture)));

    (_, TextRange[] visualRuns) = bidiInfo.VisualRuns(paragraph, paragraph.Range);
    List<int> visualOrder = [];
    Dictionary<int, int> scalarIndexByCodeUnit = scalars
        .Select((scalar, index) => (scalar.CodeUnitIndex, index))
        .ToDictionary(item => item.CodeUnitIndex, item => item.index);

    foreach (TextRange run in visualRuns)
    {
        IEnumerable<InitialInfo.TextScalar> runScalars = scalars
            .Where(scalar => scalar.CodeUnitIndex >= run.Start && scalar.CodeUnitIndex < run.End);
        if (reorderedLevels[run.Start].IsRtl())
        {
            runScalars = runScalars.Reverse();
        }

        foreach (InitialInfo.TextScalar scalar in runScalars)
        {
            if (!Prepare.RemovedByX9(bidiInfo.OriginalClasses[scalar.CodeUnitIndex]))
            {
                visualOrder.Add(scalarIndexByCodeUnit[scalar.CodeUnitIndex]);
            }
        }
    }

    string reorder = string.Join(" ", visualOrder);
    return $"para={paragraph.Level.Number()};levels={levels};reorder={reorder}";
}
