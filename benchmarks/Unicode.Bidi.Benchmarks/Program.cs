/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs; src/lib.rs; src/utf16.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: infrastructure
 */

using BenchmarkDotNet.Running;

namespace Unicode.Bidi.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
