# Performance

The benchmark suite uses BenchmarkDotNet 0.15.8 for the .NET port and Criterion 0.8.2 for Rust
`unicode-bidi` 0.3.18. Both sides load the same scalar corpus and UTF-16 text fragments from
`benchmarks/workloads.json`.

## Reproducing the comparison

Run the full suite sequentially on an otherwise-idle machine:

```bash
python3 benchmarks/run_comparison.py
```

The runner writes the native BenchmarkDotNet and Criterion artifacts, `comparison.json`, and a
normalized `summary.md` under `artifacts/benchmarks`. Use `--quick` only to validate the harness;
short-run confidence intervals are intentionally wider.

## Reference environment

Results below were measured on 2026-08-01 with:

- Apple M4, 10 physical/logical cores, arm64.
- macOS 26.5.2.
- .NET SDK 10.0.301 and .NET runtime 10.0.9.
- Rust 1.96.1.

Times are medians. A ratio below 1.00 means .NET was faster. Managed allocation figures are available
only from BenchmarkDotNet and therefore do not imply a Rust allocation comparison.

| Workload | .NET | Rust | .NET / Rust | .NET allocated |
|---|---:|---:|---:|---:|
| Bidi class lookup | 2.70 ns | 10.02 ns | 0.27x | 0 B |
| `InitialInfo`, mixed | 1.280 us | 3.197 us | 0.40x | 2,744 B |
| `BidiInfo`, LTR | 2.221 us | 3.938 us | 0.56x | 9,640 B |
| `BidiInfo`, mixed | 8.045 us | 7.937 us | 1.01x | 25,408 B |
| `BidiInfo` plus reorder, mixed | 8.863 us | 9.952 us | 0.89x | 31,608 B |

The mixed `BidiInfo` run observed a processor-frequency shift and BenchmarkDotNet extended the sample
set. Its median is effectively tied with Rust; the other workloads had tighter distributions.

## Range lookup analysis

The Rust implementation performs a binary search over sorted, non-overlapping `(lo, hi, class)`
ranges. The comparison must return the class for the interval containing a Unicode scalar, or `L`
when no interval contains it. This is interval-to-value lookup, not exact-key lookup.

.NET provides [`Array.BinarySearch`](https://learn.microsoft.com/dotnet/api/system.array.binarysearch)
and generic span-based
[`MemoryExtensions.BinarySearch`](https://learn.microsoft.com/dotnet/api/system.memoryextensions.binarysearch).
The span API
can express the original interval comparator without boxing by using a value type that implements
`IComparable<BidiClassRange>`. [`SearchValues<T>`](https://learn.microsoft.com/dotnet/api/system.buffers.searchvalues-1),
dictionaries, and frozen collections do not directly
fit: they optimize membership or exact-key lookup rather than mapping a value through an interval.

Full BenchmarkDotNet results for the 16-scalar corpus were:

| Strategy | Median | Relative to upstream range search |
|---|---:|---:|
| Upstream-style range search | 8.34 ns | 1.00x |
| `MemoryExtensions.BinarySearch` with interval comparer | 8.29 ns | 0.99x |
| `Array.BinarySearch` over range ends | 11.41 ns | 1.37x |
| `MemoryExtensions.BinarySearch` over range ends | 9.82 ns | 1.18x |
| Manual lower-bound search over range ends | 7.64 ns | 0.92x |
| BMP direct table with supplementary fallback | 2.22 ns | 0.27x |
| Production API, including call and fallback branch | 2.70 ns | 0.33x |

No general-purpose .NET search API materially beats the compact upstream algorithm. The useful gain
comes from the workload-aware data layout: a 64 KiB byte table handles BMP scalars directly, while
supplementary scalars retain the upstream range search. The table is initialized once from the same
generated range data, preserving one Unicode source of truth at the cost of 64 KiB managed memory.

In controlled short runs before and after that change, the direct BMP path reduced `InitialInfo` mixed
text from 2.971 us to 1.283 us, LTR `BidiInfo` from 3.764 us to 2.230 us, mixed `BidiInfo` from 9.009 us
to 7.326 us, and mixed processing plus reorder from 10.485 us to 8.915 us. The exhaustive unit test
checks the optimized path against the generated range table for every valid Unicode scalar.
