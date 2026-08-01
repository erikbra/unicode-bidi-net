# Performance benchmarks

The performance harness compares the .NET port with Rust `unicode-bidi` 0.3.18 using equivalent
UTF-16 workloads from `workloads.json`.

Run the full cross-runtime comparison from the repository root:

```sh
python3 benchmarks/run_comparison.py
```

Use `--quick` to validate the harness with shorter measurement runs. Reports are written under
`artifacts/benchmarks`, including the native BenchmarkDotNet and Criterion results plus a normalized
`summary.md` and `comparison.json`.

To compare alternative .NET bidi-class lookup implementations without running Rust:

```sh
dotnet run --configuration Release \
  --project benchmarks/Unicode.Bidi.Benchmarks/Unicode.Bidi.Benchmarks.csproj -- \
  --filter '*BidiClassSearchBenchmarks*'
```

BenchmarkDotNet reports time and managed allocations for .NET. Criterion reports statistically
sampled wall-clock time for Rust. The comparison runner uses each framework's median estimate and
normalizes the bidi-class batch to one Unicode scalar lookup. Run both sides sequentially on the
same otherwise-idle machine; ratios from different hosts are not comparable.

The lookup strategy benchmarks cover the public .NET options that preserve the Rust algorithm's
sorted, non-overlapping range semantics:

- `Array.BinarySearch` over primitive range endpoints.
- `MemoryExtensions.BinarySearch` over a span, both for primitive endpoints and with a value-type
  `IComparable<T>` that applies the original interval comparator.
- A manual lower-bound search over primitive endpoints.
- A BMP direct table with binary-search fallback for supplementary scalars.

`SearchValues<T>`, dictionaries, and frozen collections are intentionally excluded: they implement
set membership or exact-key lookup, not interval-to-value lookup. Expanding all Unicode scalar values
into exact keys would change the table's compact data model substantially.

The production implementation uses a 64 KiB byte table for BMP scalars and retains the upstream-style
range search for supplementary scalars. The direct table is initialized once from the generated Rust
range data, so there is still one source of Unicode truth and no generated-data drift.
