/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/level.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

namespace Unicode.Bidi;

/// <summary>
/// Exception thrown when a bidi embedding level is outside the range accepted by UAX #9.
/// </summary>
public sealed class LevelOutOfRangeException : ArgumentOutOfRangeException
{
    /// <summary>
    /// Initializes an exception for an invalid bidi embedding level.
    /// </summary>
    /// <param name="number">The invalid level.</param>
    /// <param name="max">The maximum accepted level.</param>
    public LevelOutOfRangeException(int number, int max)
        : base(nameof(number), number, $"Bidi embedding level must be between 0 and {max}.")
    {
    }
}
