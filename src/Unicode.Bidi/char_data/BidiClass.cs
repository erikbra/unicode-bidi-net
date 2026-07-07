/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/tables.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

namespace Unicode.Bidi;

/// <summary>
/// Represents values of the Unicode Bidi_Class property.
/// </summary>
public enum BidiClass
{
    /// <summary>Arabic Letter.</summary>
    AL,
    /// <summary>Arabic Number.</summary>
    AN,
    /// <summary>Paragraph Separator.</summary>
    B,
    /// <summary>Boundary Neutral.</summary>
    BN,
    /// <summary>Common Number Separator.</summary>
    CS,
    /// <summary>European Number.</summary>
    EN,
    /// <summary>European Number Separator.</summary>
    ES,
    /// <summary>European Number Terminator.</summary>
    ET,
    /// <summary>First Strong Isolate.</summary>
    FSI,
    /// <summary>Left-to-Right.</summary>
    L,
    /// <summary>Left-to-Right Embedding.</summary>
    LRE,
    /// <summary>Left-to-Right Isolate.</summary>
    LRI,
    /// <summary>Left-to-Right Override.</summary>
    LRO,
    /// <summary>Nonspacing Mark.</summary>
    NSM,
    /// <summary>Other Neutral.</summary>
    ON,
    /// <summary>Pop Directional Format.</summary>
    PDF,
    /// <summary>Pop Directional Isolate.</summary>
    PDI,
    /// <summary>Right-to-Left.</summary>
    R,
    /// <summary>Right-to-Left Embedding.</summary>
    RLE,
    /// <summary>Right-to-Left Isolate.</summary>
    RLI,
    /// <summary>Right-to-Left Override.</summary>
    RLO,
    /// <summary>Segment Separator.</summary>
    S,
    /// <summary>Whitespace.</summary>
    WS,
}
