/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/level.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

namespace Unicode.Bidi;

public sealed class LevelOutOfRangeException : ArgumentOutOfRangeException
{
    public LevelOutOfRangeException(int number, int max)
        : base(nameof(number), number, $"Bidi embedding level must be between 0 and {max}.")
    {
    }
}
