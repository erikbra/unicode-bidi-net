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

    /// <summary>
    /// The left-to-right paragraph level, level 0.
    /// </summary>
    public static Level LtrLevel { get; } = new(0);

    /// <summary>
    /// The right-to-left paragraph level, level 1.
    /// </summary>
    public static Level RtlLevel { get; } = new(1);

    /// <summary>
    /// The maximum explicit embedding depth accepted by UAX #9.
    /// </summary>
    public static byte MaxExplicitDepth => MaxDepth;

    /// <summary>
    /// The maximum implicit embedding depth accepted by UAX #9.
    /// </summary>
    public static byte MaxImplicitDepth => MaxDepth + 1;

    /// <summary>
    /// Returns the left-to-right paragraph level.
    /// </summary>
    public static Level Ltr()
    {
        return LtrLevel;
    }

    /// <summary>
    /// Returns the right-to-left paragraph level.
    /// </summary>
    public static Level Rtl()
    {
        return RtlLevel;
    }

    /// <summary>
    /// Creates an embedding level, accepting values through the implicit maximum.
    /// </summary>
    /// <param name="number">The numeric embedding level.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when <paramref name="number"/> is above <see cref="MaxImplicitDepth"/>.</exception>
    public static Level New(byte number)
    {
        return number <= MaxImplicitDepth
            ? new Level(number)
            : throw new LevelOutOfRangeException(number, MaxImplicitDepth);
    }

    /// <summary>
    /// Creates an embedding level, accepting values through the implicit maximum.
    /// </summary>
    /// <param name="number">The numeric embedding level.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when <paramref name="number"/> is above <see cref="MaxImplicitDepth"/>.</exception>
    public static Level Create(byte number)
    {
        return New(number);
    }

    /// <summary>
    /// Creates an explicit embedding level, accepting values through the explicit maximum.
    /// </summary>
    /// <param name="number">The numeric embedding level.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when <paramref name="number"/> is above <see cref="MaxExplicitDepth"/>.</exception>
    public static Level NewExplicit(byte number)
    {
        return number <= MaxExplicitDepth
            ? new Level(number)
            : throw new LevelOutOfRangeException(number, MaxExplicitDepth);
    }

    /// <summary>
    /// Creates an explicit embedding level, accepting values through the explicit maximum.
    /// </summary>
    /// <param name="number">The numeric embedding level.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when <paramref name="number"/> is above <see cref="MaxExplicitDepth"/>.</exception>
    public static Level CreateExplicit(byte number)
    {
        return NewExplicit(number);
    }

    /// <summary>
    /// Gets the numeric embedding level.
    /// </summary>
    public byte Value => _value;

    /// <summary>
    /// Returns the numeric embedding level.
    /// </summary>
    public byte Number()
    {
        return _value;
    }

    /// <summary>
    /// Returns whether this level has left-to-right direction.
    /// </summary>
    public bool IsLtr()
    {
        return _value % 2 == 0;
    }

    /// <summary>
    /// Gets whether this level has left-to-right direction.
    /// </summary>
    public bool IsLeftToRight => IsLtr();

    /// <summary>
    /// Returns whether this level has right-to-left direction.
    /// </summary>
    public bool IsRtl()
    {
        return _value % 2 == 1;
    }

    /// <summary>
    /// Gets whether this level has right-to-left direction.
    /// </summary>
    public bool IsRightToLeft => IsRtl();

    /// <summary>
    /// Raises this level by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when the result is above <see cref="MaxImplicitDepth"/>.</exception>
    public Level Raise(byte amount)
    {
        int number = _value + amount;
        return number <= MaxImplicitDepth
            ? new Level((byte)number)
            : throw new LevelOutOfRangeException(number, MaxImplicitDepth);
    }

    /// <summary>
    /// Raises this level by the specified amount, applying the explicit-depth limit.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when the result is above <see cref="MaxExplicitDepth"/>.</exception>
    public Level RaiseExplicit(byte amount)
    {
        int number = _value + amount;
        return number <= MaxExplicitDepth
            ? new Level((byte)number)
            : throw new LevelOutOfRangeException(number, MaxExplicitDepth);
    }

    /// <summary>
    /// Lowers this level by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to subtract.</param>
    /// <exception cref="LevelOutOfRangeException">Thrown when the result would be below zero.</exception>
    public Level Lower(byte amount)
    {
        int number = _value - amount;
        return number >= 0
            ? new Level((byte)number)
            : throw new LevelOutOfRangeException(number, MaxImplicitDepth);
    }

    /// <summary>
    /// Returns the next greater explicit left-to-right level.
    /// </summary>
    public Level NewExplicitNextLtr()
    {
        return NewExplicit((byte)((_value + 2) & ~1));
    }

    /// <summary>
    /// Returns the next greater explicit right-to-left level.
    /// </summary>
    public Level NewExplicitNextRtl()
    {
        return NewExplicit((byte)((_value + 1) | 1));
    }

    /// <summary>
    /// Returns this level if it is right-to-left, otherwise the next greater right-to-left level.
    /// </summary>
    public Level NewLowestGeRtl()
    {
        return New((byte)(_value | 1));
    }

    /// <summary>
    /// Returns the bidi class implied by this embedding level direction.
    /// </summary>
    public BidiClass BidiClass()
    {
        return IsRtl() ? global::Unicode.Bidi.BidiClass.R : global::Unicode.Bidi.BidiClass.L;
    }

    /// <inheritdoc />
    public int CompareTo(Level other)
    {
        return _value.CompareTo(other._value);
    }

    /// <summary>
    /// Returns whether <paramref name="left"/> is lower than <paramref name="right"/>.
    /// </summary>
    public static bool operator <(Level left, Level right)
    {
        return left._value < right._value;
    }

    /// <summary>
    /// Returns whether <paramref name="left"/> is greater than <paramref name="right"/>.
    /// </summary>
    public static bool operator >(Level left, Level right)
    {
        return left._value > right._value;
    }

    /// <summary>
    /// Returns whether <paramref name="left"/> is lower than or equal to <paramref name="right"/>.
    /// </summary>
    public static bool operator <=(Level left, Level right)
    {
        return left._value <= right._value;
    }

    /// <summary>
    /// Returns whether <paramref name="left"/> is greater than or equal to <paramref name="right"/>.
    /// </summary>
    public static bool operator >=(Level left, Level right)
    {
        return left._value >= right._value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _value.ToString();
    }
}
