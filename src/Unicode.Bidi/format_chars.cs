/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/format_chars.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2017 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

namespace Unicode.Bidi;

/// <summary>
/// Directional formatting characters from UAX #9.
/// </summary>
public static class FormatChars
{
    /// <summary>Arabic Letter Mark.</summary>
    public const char ALM = '\u061C';
    /// <summary>Left-to-Right Mark.</summary>
    public const char LRM = '\u200E';
    /// <summary>Right-to-Left Mark.</summary>
    public const char RLM = '\u200F';

    /// <summary>Left-to-Right Isolate.</summary>
    public const char LRI = '\u2066';
    /// <summary>Right-to-Left Isolate.</summary>
    public const char RLI = '\u2067';
    /// <summary>First Strong Isolate.</summary>
    public const char FSI = '\u2068';
    /// <summary>Pop Directional Isolate.</summary>
    public const char PDI = '\u2069';

    /// <summary>Left-to-Right Embedding.</summary>
    public const char LRE = '\u202A';
    /// <summary>Right-to-Left Embedding.</summary>
    public const char RLE = '\u202B';
    /// <summary>Pop Directional Format.</summary>
    public const char PDF = '\u202C';
    /// <summary>Left-to-Right Override.</summary>
    public const char LRO = '\u202D';
    /// <summary>Right-to-Left Override.</summary>
    public const char RLO = '\u202E';
}
