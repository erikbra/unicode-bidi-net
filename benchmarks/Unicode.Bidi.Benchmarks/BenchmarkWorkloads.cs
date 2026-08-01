/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs; src/lib.rs; src/utf16.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: infrastructure
 */

using System.Text;
using System.Text.Json;

namespace Unicode.Bidi.Benchmarks;

internal sealed class BenchmarkWorkloads
{
    public required int[] BidiClassCodePoints { get; init; }

    public required string LtrFragment { get; init; }

    public required string MixedFragment { get; init; }

    public int RepeatCount { get; init; }

    public Rune[] BidiClassRunes => BidiClassCodePoints.Select(static value => new Rune(value)).ToArray();

    public string LtrText => string.Concat(Enumerable.Repeat(LtrFragment, RepeatCount));

    public string MixedText => string.Concat(Enumerable.Repeat(MixedFragment, RepeatCount));

    public static BenchmarkWorkloads Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "workloads.json");
        return JsonSerializer.Deserialize<BenchmarkWorkloads>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException($"Could not deserialize benchmark workloads from {path}.");
    }
}
