/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 * Adapted from Rust unicode-bidi source with AI assistance.
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/level.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: adapted
 */

/*
 * Copyright 2017 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
 *
 * Licensed under the Apache License, Version 2.0 or the MIT license, at your option.
 */

using Unicode.Bidi.char_data;

namespace Unicode.Bidi;

/// <summary>
/// Bidi embedding level. Even values are LTR and odd values are RTL.
/// </summary>
public readonly record struct Level : IComparable<Level>
{
    private const byte MaxDepth = 125;
    private readonly byte _value;

    private Level(byte value)
    {
        _value = value;
    }

    public static Level LtrLevel { get; } = new(0);

    public static Level RtlLevel { get; } = new(1);

    public static byte MaxExplicitDepth => MaxDepth;

    public static byte MaxImplicitDepth => MaxDepth + 1;

    public static Level Ltr()
    {
        return LtrLevel;
    }

    public static Level Rtl()
    {
        return RtlLevel;
    }

    public static Level New(byte number)
    {
        return number <= MaxImplicitDepth
            ? new Level(number)
            : throw new LevelOutOfRangeException(number, MaxImplicitDepth);
    }

    public static Level NewExplicit(byte number)
    {
        return number <= MaxExplicitDepth
            ? new Level(number)
            : throw new LevelOutOfRangeException(number, MaxExplicitDepth);
    }

    public byte Number()
    {
        return _value;
    }

    public bool IsLtr()
    {
        return _value % 2 == 0;
    }

    public bool IsRtl()
    {
        return _value % 2 == 1;
    }

    public Level Raise(byte amount)
    {
        int number = _value + amount;
        return number <= MaxImplicitDepth
            ? new Level((byte)number)
            : throw new LevelOutOfRangeException(number, MaxImplicitDepth);
    }

    public Level RaiseExplicit(byte amount)
    {
        int number = _value + amount;
        return number <= MaxExplicitDepth
            ? new Level((byte)number)
            : throw new LevelOutOfRangeException(number, MaxExplicitDepth);
    }

    public Level Lower(byte amount)
    {
        int number = _value - amount;
        return number >= 0
            ? new Level((byte)number)
            : throw new LevelOutOfRangeException(number, MaxImplicitDepth);
    }

    public Level NewExplicitNextLtr()
    {
        return NewExplicit((byte)((_value + 2) & ~1));
    }

    public Level NewExplicitNextRtl()
    {
        return NewExplicit((byte)((_value + 1) | 1));
    }

    public Level NewLowestGeRtl()
    {
        return New((byte)(_value | 1));
    }

    public BidiClass BidiClass()
    {
        return IsRtl() ? char_data.BidiClass.R : char_data.BidiClass.L;
    }

    public int CompareTo(Level other)
    {
        return _value.CompareTo(other._value);
    }

    public static bool operator <(Level left, Level right)
    {
        return left._value < right._value;
    }

    public static bool operator >(Level left, Level right)
    {
        return left._value > right._value;
    }

    public static bool operator <=(Level left, Level right)
    {
        return left._value <= right._value;
    }

    public static bool operator >=(Level left, Level right)
    {
        return left._value >= right._value;
    }

    public override string ToString()
    {
        return _value.ToString();
    }
}
