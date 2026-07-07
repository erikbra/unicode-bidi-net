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
    public const char ALM = '\u061C';
    public const char LRM = '\u200E';
    public const char RLM = '\u200F';

    public const char LRI = '\u2066';
    public const char RLI = '\u2067';
    public const char FSI = '\u2068';
    public const char PDI = '\u2069';

    public const char LRE = '\u202A';
    public const char RLE = '\u202B';
    public const char PDF = '\u202C';
    public const char LRO = '\u202D';
    public const char RLO = '\u202E';
}
