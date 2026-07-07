/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

using System.Text;

namespace Unicode.Bidi.char_data;

/// <summary>
/// Accessor for the Unicode Bidi_Class property from Unicode Character Database data.
/// </summary>
public static class CharData
{
    /// <summary>
    /// Gets the Unicode Character Database version used by this package.
    /// </summary>
    public static (int Major, int Minor, int Patch) UnicodeVersion => (16, 0, 0);

    /// <summary>
    /// Looks up the Unicode Bidi_Class value for a BMP character.
    /// </summary>
    /// <param name="c">The UTF-16 character to classify. Surrogates are rejected.</param>
    /// <returns>The bidi class for <paramref name="c"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="c"/> is a surrogate code unit.</exception>
    public static BidiClass BidiClass(char c)
    {
        if (char.IsSurrogate(c))
        {
            throw new ArgumentException("Use the Rune overload for supplementary Unicode scalar values.", nameof(c));
        }

        return BidiClass(new Rune(c));
    }

    /// <summary>
    /// Looks up the Unicode Bidi_Class value for a Unicode scalar value.
    /// </summary>
    /// <param name="rune">The Unicode scalar value to classify.</param>
    /// <returns>The bidi class for <paramref name="rune"/>.</returns>
    public static BidiClass BidiClass(Rune rune)
    {
        int codePoint = rune.Value;
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

        // UCD/extracted/DerivedBidiClass.txt: all unlisted code points default to L.
        return global::Unicode.Bidi.char_data.BidiClass.L;
    }

    /// <summary>
    /// Returns whether the bidi class is a right-to-left explicit or isolate formatting class.
    /// </summary>
    /// <param name="bidiClass">The bidi class to inspect.</param>
    public static bool IsRtl(BidiClass bidiClass)
    {
        return bidiClass is global::Unicode.Bidi.char_data.BidiClass.RLE
            or global::Unicode.Bidi.char_data.BidiClass.RLO
            or global::Unicode.Bidi.char_data.BidiClass.RLI;
    }

    internal static BidiMatchedOpeningBracket? BidiMatchedOpeningBracket(Rune rune)
    {
        int codePoint = rune.Value;
        foreach (BidiPair pair in Tables.BidiPairTable)
        {
            if (pair.Opening == codePoint || pair.Closing == codePoint)
            {
                return new BidiMatchedOpeningBracket(
                    new Rune(pair.Skeleton ?? pair.Opening),
                    pair.Opening == codePoint);
            }
        }

        return null;
    }
}
