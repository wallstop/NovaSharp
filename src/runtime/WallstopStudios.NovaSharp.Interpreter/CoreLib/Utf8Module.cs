namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements the Lua 5.3+ utf8 library (§6.5).
    /// </summary>
    [NovaSharpModule(Namespace = "utf8")]
    internal static class Utf8Module
    {
        private const string InvalidUtf8CodeMessage = "invalid UTF-8 code";

        // Cached callback to avoid allocation on every utf8.codes call (non-lax mode)
        private static readonly DynValue CachedCodesIteratorCallback = DynValue.NewCallback(
            CodesIterator
        );

        // Cached callback for lax mode utf8.codes
        private static readonly DynValue CachedCodesIteratorLaxCallback = DynValue.NewCallback(
            CodesIteratorLax
        );

        [NovaSharpModuleConstant(Name = "charpattern")]
        public const string CharPattern = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";

        /// <summary>
        /// Implements Lua `utf8.len`, returning the number of UTF-8 codepoints in a slice or the position of the first error.
        /// Lua 5.4+: An optional `lax` parameter allows decoding of surrogates and code points above 0x10FFFF.
        /// </summary>
        /// <remarks>
        /// Lua 5.3+ requires integer representation for index arguments. Non-integer floats,
        /// NaN, and Infinity will throw "number has no integer representation".
        /// </remarks>
        [NovaSharpModuleMethod(Name = "len")]
        public static DynValue Len(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue value = args.AsType(0, "utf8.len", DataType.String, false);
            DynValue start = args.AsType(1, "utf8.len", DataType.Number, true);
            DynValue end = args.AsType(2, "utf8.len", DataType.Number, true);

            // Lua 5.4+ adds an optional lax parameter (argument #4)
            bool lax = false;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            if (version >= LuaCompatibilityVersion.Lua54)
            {
                DynValue laxArg = args.AsType(3, "utf8.len", DataType.Boolean, true);
                lax = !laxArg.IsNil() && laxArg.Boolean;
            }

            // utf8 module is Lua 5.3+ only - always require integer representation
            Utilities.LuaNumberHelpers.ValidateIntegerArgument(version, start, "len", 2);
            Utilities.LuaNumberHelpers.ValidateIntegerArgument(version, end, "len", 3);

            (int startIndex, int endExclusive) = NormalizeRange(value.String, start, end);
            DynValue result = CountRunesOrError(value.String, startIndex, endExclusive, lax);

            return result;
        }

        /// <summary>
        /// Implements Lua `utf8.codepoint`, returning the code points within the requested range (§6.5).
        /// Lua 5.4+: An optional `lax` parameter allows decoding of surrogates and code points above 0x10FFFF.
        /// </summary>
        /// <remarks>
        /// Lua 5.3+ requires integer representation for index arguments. Non-integer floats,
        /// NaN, and Infinity will throw "number has no integer representation".
        /// </remarks>
        [NovaSharpModuleMethod(Name = "codepoint")]
        public static DynValue CodePoint(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue value = args.AsType(0, "utf8.codepoint", DataType.String, false);
            DynValue start = args.AsType(1, "utf8.codepoint", DataType.Number, true);
            DynValue end = args.AsType(2, "utf8.codepoint", DataType.Number, true);

            // Lua 5.4+ adds an optional lax parameter (argument #4)
            bool lax = false;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            if (version >= LuaCompatibilityVersion.Lua54)
            {
                DynValue laxArg = args.AsType(3, "utf8.codepoint", DataType.Boolean, true);
                lax = !laxArg.IsNil() && laxArg.Boolean;
            }

            // utf8 module is Lua 5.3+ only - always require integer representation
            Utilities.LuaNumberHelpers.ValidateIntegerArgument(version, start, "codepoint", 2);
            Utilities.LuaNumberHelpers.ValidateIntegerArgument(version, end, "codepoint", 3);

            int length = value.String.Length;

            // Validate and normalize i (start position)
            int i = start.IsNil() ? 1 : (int)start.Number;
            if (i < 0)
            {
                i = length + i + 1;
            }

            // Validate and normalize j (end position)
            // When end is nil, j defaults to i (endDefaultsToStart: true)
            int j = end.IsNil() ? i : (int)end.Number;
            if (j < 0)
            {
                j = length + j + 1;
            }

            // Per Lua spec: positions must be within [1, length] after normalization
            // utf8.codepoint throws "out of bounds" for positions outside this range
            // Check start position (i) first - it maps to argument #2
            if (i < 1 || i > length)
            {
                throw new ScriptRuntimeException("bad argument #2 to 'codepoint' (out of bounds)");
            }

            // Check end position (j) - it maps to argument #3
            if (j < 1 || j > length)
            {
                throw new ScriptRuntimeException("bad argument #3 to 'codepoint' (out of bounds)");
            }

            (int startIndex, int endExclusive) = NormalizeRange(
                value.String,
                start,
                end,
                endDefaultsToStart: true
            );

            // Fast path: empty range (when start > end after normalization)
            if (startIndex >= endExclusive)
            {
                return DynValue.Void;
            }

            // Fast path: single character (very common case)
            if (
                TryDecodeScalarWithinRange(
                    value.String,
                    startIndex,
                    endExclusive,
                    out int firstCodePoint,
                    out int firstWidth,
                    lax
                )
            )
            {
                // Check if this is the only character
                if (startIndex + firstWidth >= endExclusive)
                {
                    return DynValue.FromNumber(firstCodePoint);
                }
            }
            else
            {
                throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
            }

            // Multi-character path: estimate capacity based on remaining bytes
            int remainingBytes = endExclusive - startIndex;
            // Use pooled list for common case (most strings have < 64 codepoints)
            using (ListPool<DynValue>.Get(Math.Min(remainingBytes, 64), out List<DynValue> numbers))
            {
                numbers.Add(DynValue.FromNumber(firstCodePoint));

                int index = startIndex + firstWidth;
                while (index < endExclusive)
                {
                    if (
                        !TryDecodeScalarWithinRange(
                            value.String,
                            index,
                            endExclusive,
                            out int codePoint,
                            out int width,
                            lax
                        )
                    )
                    {
                        throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
                    }

                    numbers.Add(DynValue.FromNumber(codePoint));
                    index += width;
                }

                return DynValue.NewTuple(ListPool<DynValue>.ToExactArray(numbers));
            }
        }

        // Lua 5.4 MAXUTF constant: (0x7FFFFFFFu) - the maximum code point value
        private const long Lua54MaxUtf = 0x7FFFFFFF;

        // Unicode maximum code point (used for Lua 5.3)
        private const int UnicodeMaxCodePoint = 0x10FFFF;

        /// <summary>
        /// Implements Lua `utf8.char`, building a UTF-8 string from the provided scalar values (§6.5).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Lua 5.4 accepts code points up to 0x7FFFFFFF (using extended UTF-8 encoding with 5-6 bytes).
        /// Lua 5.3 accepts code points 0-0x10FFFF (including surrogates).
        /// Both versions accept surrogate code points (0xD800-0xDFFF).
        /// </para>
        /// <para>
        /// Lua 5.3+ requires integer representation for all arguments. Non-integer floats,
        /// NaN, and Infinity will throw "number has no integer representation".
        /// </para>
        /// </remarks>
        [NovaSharpModuleMethod(Name = "char")]
        public static DynValue Char(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            bool useLua54ExtendedRange = version >= LuaCompatibilityVersion.Lua54;

            // Use a byte list for extended UTF-8 encoding
            using PooledResource<List<byte>> pooledBytes = ListPool<byte>.Get(out List<byte> bytes);

            for (int i = 0; i < args.Count; i++)
            {
                // utf8 module is Lua 5.3+ only - always require integer representation
                DynValue argValue = args.AsType(i, "utf8.char", DataType.Number, false);
                Utilities.LuaNumberHelpers.RequireIntegerRepresentation(
                    argValue.LuaNumber,
                    "utf8.char",
                    i + 1
                );

                // Use long to handle values up to 0x7FFFFFFF
                long codePoint = (long)argValue.Number;

                if (useLua54ExtendedRange)
                {
                    // Lua 5.4: accept 0 to 0x7FFFFFFF (no surrogate check)
                    if (codePoint < 0 || codePoint > Lua54MaxUtf)
                    {
                        using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                        sb.Append("bad argument #");
                        sb.Append(i + 1);
                        sb.Append(" to 'utf8.char' (value out of range)");
                        throw new ScriptRuntimeException(sb.ToString());
                    }

                    EncodeExtendedUtf8(codePoint, bytes);
                }
                else
                {
                    // Lua 5.3: Unicode range only (0 to 0x10FFFF), surrogates ARE accepted
                    // The only difference from 5.4 is the maximum code point value
                    if (codePoint < 0 || codePoint > UnicodeMaxCodePoint)
                    {
                        using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                        sb.Append("bad argument #");
                        sb.Append(i + 1);
                        sb.Append(" to 'utf8.char' (value out of range)");
                        throw new ScriptRuntimeException(sb.ToString());
                    }

                    EncodeExtendedUtf8(codePoint, bytes);
                }
            }

            // Convert byte array to string using Latin-1 encoding (each byte maps directly to a char)
            // This is how Lua strings work internally - they are byte sequences
            char[] chars = new char[bytes.Count];
            for (int j = 0; j < bytes.Count; j++)
            {
                chars[j] = (char)bytes[j];
            }
            return DynValue.NewString(new string(chars));
        }

        /// <summary>
        /// Encodes a code point to extended UTF-8 (supports values up to 0x7FFFFFFF).
        /// This follows Lua 5.4's encoding scheme which uses the original UTF-8 specification
        /// allowing 5 and 6 byte sequences for code points beyond the Unicode range.
        /// </summary>
        private static void EncodeExtendedUtf8(long codePoint, List<byte> output)
        {
            if (codePoint <= 0x7F)
            {
                // 1-byte sequence: 0xxxxxxx
                output.Add((byte)codePoint);
            }
            else if (codePoint <= 0x7FF)
            {
                // 2-byte sequence: 110xxxxx 10xxxxxx
                output.Add((byte)(0xC0 | (codePoint >> 6)));
                output.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else if (codePoint <= 0xFFFF)
            {
                // 3-byte sequence: 1110xxxx 10xxxxxx 10xxxxxx
                output.Add((byte)(0xE0 | (codePoint >> 12)));
                output.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                output.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else if (codePoint <= 0x1FFFFF)
            {
                // 4-byte sequence: 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
                output.Add((byte)(0xF0 | (codePoint >> 18)));
                output.Add((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
                output.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                output.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else if (codePoint <= 0x3FFFFFF)
            {
                // 5-byte sequence: 111110xx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx
                output.Add((byte)(0xF8 | (codePoint >> 24)));
                output.Add((byte)(0x80 | ((codePoint >> 18) & 0x3F)));
                output.Add((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
                output.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                output.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else
            {
                // 6-byte sequence: 1111110x 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx
                output.Add((byte)(0xFC | (codePoint >> 30)));
                output.Add((byte)(0x80 | ((codePoint >> 24) & 0x3F)));
                output.Add((byte)(0x80 | ((codePoint >> 18) & 0x3F)));
                output.Add((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
                output.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                output.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
        }

        /// <summary>
        /// Implements Lua `utf8.codes`, returning the iterator triple for traversing code points (§6.5).
        /// In Lua 5.4+, accepts an optional lax parameter to allow surrogates and extended codepoints.
        /// </summary>
        [NovaSharpModuleMethod(Name = "codes")]
        public static DynValue Codes(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue value = args.AsType(0, "utf8.codes", DataType.String, false);

            // Lua 5.4+ supports optional 'lax' parameter
            bool lax = false;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            if (version >= LuaCompatibilityVersion.Lua54)
            {
                DynValue laxArg = args.RawGet(1, false);
                lax = laxArg != null && laxArg.Type == DataType.Boolean && laxArg.Boolean;
            }

            DynValue iterator = lax ? CachedCodesIteratorLaxCallback : CachedCodesIteratorCallback;

            return DynValue.NewTuple(iterator, value, DynValue.FromNumber(0));
        }

        /// <summary>
        /// Implements Lua `utf8.offset`, locating the byte offset of the nth code point relative to a position (§6.5).
        /// </summary>
        /// <remarks>
        /// Lua 5.3+ requires integer representation for n and i arguments. Non-integer floats,
        /// NaN, and Infinity will throw "number has no integer representation".
        /// </remarks>
        [NovaSharpModuleMethod(Name = "offset")]
        public static DynValue Offset(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            DynValue value = args.AsType(0, "utf8.offset", DataType.String, false);
            DynValue nArg = args.AsType(1, "utf8.offset", DataType.Number, false);
            DynValue indexArg = args.AsType(2, "utf8.offset", DataType.Number, true);

            // utf8 module is Lua 5.3+ only - always require integer representation
            Utilities.LuaNumberHelpers.RequireIntegerRepresentation(nArg.LuaNumber, "offset", 2);
            Utilities.LuaNumberHelpers.ValidateIntegerArgument(version, indexArg, "offset", 3);

            int n = (int)nArg.Number;

            // Validate position (i) before normalizing - position 0 is never valid
            if (!indexArg.IsNil())
            {
                int rawPosition = (int)indexArg.Number;
                int length = value.String.Length;

                // Position 0 is always invalid per Lua spec
                if (rawPosition == 0)
                {
                    throw new ScriptRuntimeException(
                        "bad argument #3 to 'offset' (position out of bounds)"
                    );
                }

                // Normalize negative positions, then check bounds
                int normalizedPosition = rawPosition < 0 ? length + rawPosition + 1 : rawPosition;

                // Position must be in range [1, length+1] after normalization
                if (normalizedPosition < 1 || normalizedPosition > length + 1)
                {
                    throw new ScriptRuntimeException(
                        "bad argument #3 to 'offset' (position out of bounds)"
                    );
                }
            }

            if (n == 0)
            {
                int boundary = NormalizeBoundary(
                    value.String,
                    indexArg.IsNil() ? 1 : (int)indexArg.Number
                );
                int containing = FindRuneStartContainingBoundary(value.String, boundary);

                return containing >= 0 ? DynValue.FromNumber(containing + 1) : DynValue.Nil;
            }

            int initial = indexArg.IsNil()
                ? (n > 0 ? 1 : value.String.Length + 1)
                : (int)indexArg.Number;
            int boundaryOffset = NormalizeBoundary(value.String, initial);

            if (n > 0)
            {
                if (!IsRuneBoundary(value.String, boundaryOffset))
                {
                    return DynValue.Nil;
                }

                int index = boundaryOffset;
                int remaining = n;

                while (remaining > 0)
                {
                    if (
                        !TryDecodeScalarWithinRange(
                            value.String,
                            index,
                            value.String.Length,
                            out int _,
                            out int width
                        )
                    )
                    {
                        return DynValue.Nil;
                    }

                    remaining--;

                    if (remaining == 0)
                    {
                        return DynValue.FromNumber(index + 1);
                    }

                    index += width;
                }
            }
            else
            {
                int index = boundaryOffset;
                int remaining = -n;

                while (remaining > 0)
                {
                    index = MoveToPreviousRuneBoundary(value.String, index);

                    if (index < 0)
                    {
                        return DynValue.Nil;
                    }

                    remaining--;

                    if (remaining == 0)
                    {
                        return DynValue.FromNumber(index + 1);
                    }
                }
            }

            return DynValue.Nil;
        }

        private static DynValue CodesIterator(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue state = args.AsType(0, "utf8.codes", DataType.String, false);
            DynValue control = args.AsType(1, "utf8.codes", DataType.Number, true);

            string value = state.String;
            int index = GetNextIteratorIndex(value, control);

            if (index >= value.Length)
            {
                return DynValue.Nil;
            }

            if (
                !TryDecodeScalarWithinRange(
                    value,
                    index,
                    value.Length,
                    out int codePoint,
                    out int width
                )
            )
            {
                throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
            }

            return DynValue.NewTuple(
                DynValue.FromNumber(index + 1),
                DynValue.FromNumber(codePoint)
            );
        }

        private static DynValue CodesIteratorLax(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue state = args.AsType(0, "utf8.codes", DataType.String, false);
            DynValue control = args.AsType(1, "utf8.codes", DataType.Number, true);

            string value = state.String;
            int index = GetNextIteratorIndex(value, control, true);

            if (index >= value.Length)
            {
                return DynValue.Nil;
            }

            if (
                !TryDecodeScalarWithinRange(
                    value,
                    index,
                    value.Length,
                    out int codePoint,
                    out int width,
                    true
                )
            )
            {
                throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
            }

            return DynValue.NewTuple(
                DynValue.FromNumber(index + 1),
                DynValue.FromNumber(codePoint)
            );
        }

        private static DynValue CountRunesOrError(
            string value,
            int startIndex,
            int endExclusive,
            bool lax = false
        )
        {
            int count = 0;
            int index = startIndex;

            while (index < endExclusive)
            {
                if (
                    !TryDecodeScalarWithinRange(
                        value,
                        index,
                        endExclusive,
                        out int _,
                        out int width,
                        lax
                    )
                )
                {
                    return DynValue.NewTuple(DynValue.Nil, DynValue.FromNumber(index + 1));
                }

                count++;
                index += width;
            }

            return DynValue.FromNumber(count);
        }

        private static (int StartIndex, int EndExclusive) NormalizeRange(
            string value,
            DynValue start,
            DynValue end,
            bool endDefaultsToStart = false
        )
        {
            int i = start.IsNil() ? 1 : (int)start.Number;
            int j = end.IsNil() ? (endDefaultsToStart ? i : value.Length) : (int)end.Number;

            if (i < 0)
            {
                i = value.Length + i + 1;
            }

            if (j < 0)
            {
                j = value.Length + j + 1;
            }

            if (i < 1)
            {
                i = 1;
            }

            if (j > value.Length)
            {
                j = value.Length;
            }

            if (i > j)
            {
                return (i - 1, i - 1);
            }

            return (i - 1, j);
        }

        private static bool TryDecodeScalarWithinRange(
            string value,
            int index,
            int limit,
            out int codePoint,
            out int width
        )
        {
            return TryDecodeScalarWithinRange(value, index, limit, out codePoint, out width, false);
        }

        private static bool TryDecodeScalarWithinRange(
            string value,
            int index,
            int limit,
            out int codePoint,
            out int width,
            bool lax
        )
        {
            codePoint = 0;
            width = 0;

            if (index < 0 || index >= value.Length || index >= limit)
            {
                return false;
            }

            int remaining = Math.Min(limit - index, value.Length - index);

            if (remaining <= 0)
            {
                return false;
            }

            char current = value[index];

            if (char.IsHighSurrogate(current))
            {
                if (remaining < 2)
                {
                    // In lax mode, allow lone high surrogate
                    if (lax)
                    {
                        codePoint = current;
                        width = 1;
                        return true;
                    }
                    return false;
                }

                char next = value[index + 1];

                if (!char.IsLowSurrogate(next))
                {
                    // In lax mode, allow lone high surrogate
                    if (lax)
                    {
                        codePoint = current;
                        width = 1;
                        return true;
                    }
                    return false;
                }

                codePoint = char.ConvertToUtf32(current, next);
                width = 2;
                return true;
            }

            if (char.IsLowSurrogate(current))
            {
                // In lax mode, allow lone low surrogate
                if (lax)
                {
                    codePoint = current;
                    width = 1;
                    return true;
                }
                return false;
            }

            codePoint = current;
            width = 1;
            return true;
        }

        private static int GetNextIteratorIndex(string value, DynValue control)
        {
            return GetNextIteratorIndex(value, control, false);
        }

        private static int GetNextIteratorIndex(string value, DynValue control, bool lax)
        {
            if (control.IsNil() || control.IsVoid())
            {
                return 0;
            }

            int previousPos = (int)Math.Floor(control.Number);

            if (previousPos <= 0)
            {
                return 0;
            }

            int previousIndex = Math.Min(previousPos - 1, value.Length);

            if (previousIndex >= value.Length)
            {
                return value.Length;
            }

            if (
                !TryDecodeScalarWithinRange(
                    value,
                    previousIndex,
                    value.Length,
                    out int _,
                    out int width,
                    lax
                )
            )
            {
                throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
            }

            return previousIndex + width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NormalizeBoundary(string value, int position)
        {
            int length = value.Length;
            int normalized = position;

            if (normalized < 0)
            {
                normalized = length + normalized + 1;
            }

            if (normalized < 1)
            {
                normalized = 1;
            }

            if (normalized > length + 1)
            {
                normalized = length + 1;
            }

            return normalized - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRuneBoundary(string value, int boundary)
        {
            if (boundary <= 0 || boundary >= value.Length)
            {
                return true;
            }

            return !char.IsLowSurrogate(value[boundary]);
        }

        private static int MoveToPreviousRuneBoundary(string value, int boundary)
        {
            if (boundary <= 0)
            {
                return -1;
            }

            int index = boundary - 1;

            if (index < 0)
            {
                return -1;
            }

            if (char.IsLowSurrogate(value[index]))
            {
                if (index == 0 || !char.IsHighSurrogate(value[index - 1]))
                {
                    return -1;
                }

                index--;
            }

            if (!TryDecodeScalarWithinRange(value, index, value.Length, out int _, out int _))
            {
                return -1;
            }

            return index;
        }

        private static int FindRuneStartContainingBoundary(string value, int boundary)
        {
            if (value.Length == 0)
            {
                return -1;
            }

            if (boundary < 0 || boundary >= value.Length)
            {
                return -1;
            }

            int index = boundary;

            if (index < 0)
            {
                return -1;
            }

            if (char.IsLowSurrogate(value[index]) && index > 0)
            {
                index--;
            }

            if (!TryDecodeScalarWithinRange(value, index, value.Length, out int _, out int _))
            {
                return -1;
            }

            return index;
        }
    }
}
