/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/data_source.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

using System.Text;

namespace Unicode.Bidi.char_data;

internal readonly record struct BidiMatchedOpeningBracket(Rune Opening, bool IsOpen);
