/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs; src/lib.rs; src/utf16.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: infrastructure
 */

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Unicode.Bidi.Benchmarks;

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class CrossRuntimeBenchmarks
{
    private Rune[] _bidiClassRunes = null!;
    private string _ltrText = null!;
    private string _mixedText = null!;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkWorkloads workloads = BenchmarkWorkloads.Load();
        _bidiClassRunes = workloads.BidiClassRunes;
        _ltrText = workloads.LtrText;
        _mixedText = workloads.MixedText;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int BidiClassLookup()
    {
        int checksum = 0;
        foreach (Rune rune in _bidiClassRunes)
        {
            checksum += (int)CharData.BidiClass(rune);
        }

        return checksum;
    }

    [Benchmark]
    public InitialInfo InitialInfoMixed()
    {
        return InitialInfo.New(_mixedText);
    }

    [Benchmark]
    public BidiInfo BidiInfoLtr()
    {
        return BidiInfo.New(_ltrText);
    }

    [Benchmark]
    public BidiInfo BidiInfoMixed()
    {
        return BidiInfo.New(_mixedText);
    }

    [Benchmark]
    public int BidiInfoReorderMixed()
    {
        BidiInfo info = BidiInfo.New(_mixedText);
        int checksum = 0;
        foreach (ParagraphInfo paragraph in info.Paragraphs)
        {
            checksum += info.ReorderLine(paragraph, paragraph.Range).Length;
        }

        return checksum;
    }
}
