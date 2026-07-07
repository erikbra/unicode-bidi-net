// Copyright 2015 The Servo Project Developers. See THIRD-PARTY-NOTICES.md.
//
// Licensed under the Apache License, Version 2.0 or the MIT license, at your option.

namespace Unicode.Bidi.char_data;

internal readonly record struct BidiClassRange(int Lo, int Hi, BidiClass Class);
