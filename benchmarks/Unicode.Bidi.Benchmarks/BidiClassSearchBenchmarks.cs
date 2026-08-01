/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: infrastructure
 */

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;
using Unicode.Bidi.char_data;

namespace Unicode.Bidi.Benchmarks;

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class BidiClassSearchBenchmarks
{
    private Rune[] _runes = null!;
    private int[] _rangeStarts = null!;
    private int[] _rangeEnds = null!;
    private BidiClass[] _rangeClasses = null!;
    private byte[] _bmpClasses = null!;

    [GlobalSetup]
    public void Setup()
    {
        _runes = BenchmarkWorkloads.Load().BidiClassRunes;
        _rangeStarts = Tables.BidiClassTable.Select(static range => range.Lo).ToArray();
        _rangeEnds = Tables.BidiClassTable.Select(static range => range.Hi).ToArray();
        _rangeClasses = Tables.BidiClassTable.Select(static range => range.Class).ToArray();
        _bmpClasses = Enumerable.Repeat((byte)BidiClass.L, char.MaxValue + 1).ToArray();

        foreach (BidiClassRange range in Tables.BidiClassTable)
        {
            if (range.Lo > char.MaxValue)
            {
                break;
            }

            int end = Math.Min(range.Hi, char.MaxValue);
            _bmpClasses.AsSpan(range.Lo, end - range.Lo + 1).Fill((byte)range.Class);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 16)]
    public int UpstreamRangeSearch()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            checksum += (int)LookupUpstreamRange(rune.Value);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int SpanComparableRangeSearch()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            int index = Tables.BidiClassTable.AsSpan().BinarySearch(
                new CodePointRangeComparable(rune.Value));
            checksum += index >= 0 ? (int)Tables.BidiClassTable[index].Class : (int)BidiClass.L;
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int ArrayBinarySearchByEnd()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            checksum += (int)LookupByEnd(Array.BinarySearch(_rangeEnds, rune.Value), rune.Value);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int SpanBinarySearchByEnd()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            int index = MemoryExtensions.BinarySearch<int, int>(_rangeEnds, rune.Value);
            checksum += (int)LookupByEnd(index, rune.Value);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int ManualBinarySearchByEnd()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            checksum += (int)LookupByEnd(FindFirstRangeEndingAtOrAfter(rune.Value), rune.Value);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int BmpDirectLookup()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            int codePoint = rune.Value;
            checksum += codePoint <= char.MaxValue
                ? _bmpClasses[codePoint]
                : (int)LookupByEnd(FindFirstRangeEndingAtOrAfter(codePoint), codePoint);
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int ProductionLookup()
    {
        int checksum = 0;
        foreach (Rune rune in _runes)
        {
            checksum += (int)CharData.BidiClass(rune);
        }

        return checksum;
    }

    private static BidiClass LookupUpstreamRange(int codePoint)
    {
        int lo = 0;
        int hi = Tables.BidiClassTable.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            BidiClassRange range = Tables.BidiClassTable[mid];
            if (codePoint < range.Lo)
            {
                hi = mid - 1;
            }
            else if (codePoint > range.Hi)
            {
                lo = mid + 1;
            }
            else
            {
                return range.Class;
            }
        }

        return BidiClass.L;
    }

    private BidiClass LookupByEnd(int index, int codePoint)
    {
        if (index < 0)
        {
            index = ~index;
        }

        return index < _rangeStarts.Length && codePoint >= _rangeStarts[index]
            ? _rangeClasses[index]
            : BidiClass.L;
    }

    private int FindFirstRangeEndingAtOrAfter(int codePoint)
    {
        int lo = 0;
        int hi = _rangeEnds.Length;
        while (lo < hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (_rangeEnds[mid] < codePoint)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private readonly record struct CodePointRangeComparable(int CodePoint) : IComparable<BidiClassRange>
    {
        public int CompareTo(BidiClassRange range)
        {
            if (CodePoint < range.Lo)
            {
                return -1;
            }

            return CodePoint > range.Hi ? 1 : 0;
        }
    }
}
