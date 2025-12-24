namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using Cysharp.Text;
    using Debugging;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Class implementing basic Lua functions (print, type, tostring, etc) as a NovaSharp module.
    /// </summary>
    [NovaSharpModule]
    public static class BasicModule
    {
        /// <summary>
        /// Implements Lua's <c>type</c> function (§6.1), returning the textual Lua type name for the first argument.
        /// </summary>
        /// <param name="executionContext">
        /// Execution context supplied by the runtime (unused but required by the module contract).
        /// </param>
        /// <param name="args">Arguments passed to <c>type</c>; the first entry is inspected.</param>
        /// <returns>
        /// A string <see cref="DynValue"/> representing the Lua type name (e.g., <c>"nil"</c>, <c>"table"</c>, <c>"function"</c>).
        /// </returns>
        [NovaSharpModuleMethod(Name = "type")]
        public static DynValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "type");
            }

            DynValue v = args[0];
            return DynValue.NewString(v.Type.ToLuaTypeString());
        }

        /// <summary>
        /// Implements Lua's <c>assert</c> helper (§6.1) by throwing when the first argument is falsy.
        /// </summary>
        /// <param name="executionContext">Execution context used for diagnostics.</param>
        /// <param name="args">
        /// Arguments passed to <c>assert</c>; index <c>0</c> is the test value and index <c>1</c> is the optional error message.
        /// </param>
        /// <returns>The original argument tuple when the assertion succeeds.</returns>
        /// <exception cref="ScriptRuntimeException">Thrown when the assertion fails.</exception>
        [NovaSharpModuleMethod(Name = "assert")]
        public static DynValue Assert(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue v = args[0];
            DynValue message = args[1];

            if (!v.CastToBool())
            {
                if (message.IsNil())
                {
                    throw new ScriptRuntimeException("assertion failed!"); // { DoNotDecorateMessage = true };
                }
                else
                {
                    throw new ScriptRuntimeException(message.ToPrintString()); // { DoNotDecorateMessage = true };
                }
            }

            return DynValue.NewTupleNested(args.GetArray());
        }

        /// <summary>
        /// Implements Lua's <c>collectgarbage</c> helper (§6.1) by forwarding the supported modes to the CLR GC.
        /// </summary>
        /// <param name="executionContext">Execution context supplied by the runtime.</param>
        /// <param name="args">Arguments describing the requested mode (nil/<c>"collect"</c>/<c>"restart"</c> trigger a GC).</param>
        /// <returns><see cref="DynValue.Nil"/> to match Lua's API surface.</returns>
        [NovaSharpModuleMethod(Name = "collectgarbage")]
        public static DynValue CollectGarbage(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue opt = args[0];

            string mode = opt.CastToString();

            if (mode == null || mode == "collect" || mode == "restart")
            {
#if PCL || ENABLE_DOTNET
                GC.Collect();
#else
                GC.Collect(2, GCCollectionMode.Forced);
#endif
            }

            return DynValue.Nil;
        }

        /// <summary>
        /// Implements Lua's <c>error</c> function (§6.1), raising a <see cref="ScriptRuntimeException"/> with the optional
        /// stack-level adjustment requested by the caller.
        /// </summary>
        /// <param name="executionContext">Execution context used to resolve coroutines and call frames for decoration.</param>
        /// <param name="args">
        /// Argument zero contains the error message; argument one optionally supplies the stack level used during decoration.
        /// </param>
        /// <returns>This method never returns because it always throws.</returns>
        /// <exception cref="ScriptRuntimeException">Always thrown to surface the Lua-visible error.</exception>
        [NovaSharpModuleMethod(Name = "error")]
        public static DynValue Error(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue message = args.AsType(0, "error", DataType.String, false);
            DynValue level = args.AsType(1, "error", DataType.Number, true);

            // Lua 5.3+: level must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(
                executionContext.Script.CompatibilityVersion,
                level,
                "error",
                2
            );

            Coroutine cor = executionContext.CallingCoroutine;

            WatchItem[] stacktrace = cor.GetStackTrace(0, executionContext.CallingLocation);

            ScriptRuntimeException e = new(message.String);

            long levelValue;
            if (level.IsNil())
            {
                levelValue = 1; // Default
            }
            else
            {
                // Use LuaNumber for proper integer extraction
                LuaNumber levelNum = level.LuaNumber;
                levelValue = levelNum.IsInteger
                    ? levelNum.AsInteger
                    : (long)Math.Floor(levelNum.AsFloat);
            }

            if (levelValue > 0 && levelValue < stacktrace.Length)
            {
                // Lua allows levels up to max. value of a double, while this has to be cast to int
                // Probably never will be a problem, just leaving this note here
                WatchItem wi = stacktrace[(int)levelValue];

                e.DecorateMessage(executionContext.Script, wi.Location);
            }
            else
            {
                e.DoNotDecorateMessage = true;
            }

            throw e;
        }

        /// <summary>
        /// Implements Lua's <c>tostring</c> helper (§6.1) by formatting values or invoking the <c>__tostring</c> metamethod.
        /// </summary>
        /// <param name="executionContext">Execution context used to resolve metamethod tail calls.</param>
        /// <param name="args">Arguments passed to <c>tostring</c>; the first value is converted to a Lua string.</param>
        /// <returns>A string representation of the supplied value.</returns>
        [NovaSharpModuleMethod(Name = "tostring")]
        public static DynValue ToString(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "tostring");
            }

            DynValue v = args[0];
            DynValue tail = executionContext.GetMetamethodTailCall(v, Metamethods.ToStringMeta, v);

            if (tail == null || tail.IsNil())
            {
                // Use version-aware formatting for numbers
                LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
                return DynValue.NewString(v.ToPrintString(version));
            }

            tail.TailCallData.Continuation = new CallbackFunction(
                ToStringContinuation,
                Metamethods.ToStringMeta
            );

            return tail;
        }

        /// <summary>
        /// Continuation that validates the result of a <c>__tostring</c> metamethod before returning it to Lua.
        /// </summary>
        /// <param name="executionContext">Execution context driving the metamethod invocation.</param>
        /// <param name="args">Arguments flowing out of the metamethod call.</param>
        /// <returns>The validated string result.</returns>
        internal static DynValue ToStringContinuation(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue b = args[0].ToScalar();

            if (b.IsNil())
            {
                return b;
            }

            if (b.Type != DataType.String)
            {
                throw new ScriptRuntimeException("'tostring' must return a string");
            }

            return b;
        }

        /// <summary>
        /// Implements Lua's <c>select</c> helper (§6.1), returning either the argument count or a slice of the varargs.
        /// </summary>
        /// <param name="executionContext">Execution context supplied by the runtime.</param>
        /// <param name="args">
        /// Arguments passed to <c>select</c>; index zero is the selector (<c>"#"</c> or a numeric offset), followed by the tuple.
        /// </param>
        /// <returns>A tuple containing the requested slice or a number describing the argument count.</returns>
        [NovaSharpModuleMethod(Name = "select")]
        public static DynValue Select(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            // Handle "#" case first - doesn't need executionContext
            if (args[0].Type == DataType.String && args[0].String == "#")
            {
                if (args[^1].Type == DataType.Tuple)
                {
                    return DynValue.FromNumber(args.Count - 1 + args[^1].Tuple.Length);
                }
                else
                {
                    return DynValue.FromNumber(args.Count - 1);
                }
            }

            // Numeric index path needs executionContext for version check
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            DynValue vNum = args.AsType(0, "select", DataType.Number, false);

            // Lua 5.3+: index must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(
                executionContext.Script.CompatibilityVersion,
                vNum,
                "select",
                1
            );

            // Use LuaNumber for proper integer extraction
            LuaNumber luaNum = vNum.LuaNumber;
            int num = luaNum.IsInteger ? (int)luaNum.AsInteger : (int)Math.Floor(luaNum.AsFloat);

            int startIndex;
            if (num > 0)
            {
                startIndex = num;
            }
            else if (num < 0)
            {
                startIndex = args.Count + num;

                if (startIndex < 1)
                {
                    throw ScriptRuntimeException.BadArgumentIndexOutOfRange("select", 0);
                }
            }
            else
            {
                throw ScriptRuntimeException.BadArgumentIndexOutOfRange("select", 0);
            }

            int resultCount = args.Count - startIndex;

            // Fast path for empty result
            if (resultCount <= 0)
            {
                return DynValue.Void;
            }

            // Fast path for single element
            if (resultCount == 1)
            {
                return DynValue.NewTupleNested(args[startIndex]);
            }

            // General case - use pooled list for tuple flattening
            using (ListPool<DynValue>.Get(resultCount, out List<DynValue> values))
            {
                for (int i = startIndex; i < args.Count; i++)
                {
                    values.Add(args[i]);
                }

                return DynValue.NewTupleNested(ListPool<DynValue>.ToExactArray(values));
            }
        }

        /// <summary>
        /// Implements Lua's <c>tonumber</c> helper (§6.1), converting values to doubles with optional radix parsing.
        /// </summary>
        /// <param name="executionContext">Execution context used for diagnostics.</param>
        /// <param name="args">
        /// Arguments describing the value to convert (index zero) and the optional numeric base (index one, 2-36).
        /// </param>
        /// <returns>
        /// A numeric <see cref="DynValue"/> when conversion succeeds; otherwise <see cref="DynValue.Nil"/>.
        /// </returns>
        [NovaSharpModuleMethod(Name = "tonumber")]
        public static DynValue ToNumber(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "tonumber");
            }

            DynValue e = args[0];
            DynValue b = args.AsType(1, "tonumber", DataType.Number, true);

            if (b.IsNil())
            {
                if (e.Type == DataType.Number)
                {
                    return e;
                }

                if (e.Type != DataType.String)
                {
                    return DynValue.Nil;
                }

                // Lua 5.2+ tonumber without base parses hex literals (0x/0X prefix) per §3.1
                // Lua 5.1 does NOT support hex parsing without explicit base
                if (
                    TryParseLuaNumeral(
                        e.String,
                        executionContext.Script.CompatibilityVersion,
                        out LuaNumber luaNum
                    )
                )
                {
                    return DynValue.NewNumber(luaNum);
                }
                return DynValue.Nil;
            }
            else
            {
                DynValue numeral =
                    args[0].Type != DataType.Number
                        ? args.AsType(0, "tonumber", DataType.String, false)
                        : DynValue.NewString(args[0].Number.ToString(CultureInfo.InvariantCulture));

                double baseValue = b.Number;
                if (double.IsNaN(baseValue) || double.IsInfinity(baseValue))
                {
                    throw ScriptRuntimeException.BadArgument(
                        1,
                        "tonumber",
                        "integer",
                        "number",
                        false
                    );
                }

                if (Math.Truncate(baseValue) != baseValue)
                {
                    throw ScriptRuntimeException.BadArgument(
                        1,
                        "tonumber",
                        "integer",
                        "number",
                        false
                    );
                }

                int bb = (int)baseValue;

                if (bb < 2 || bb > 36)
                {
                    throw new ScriptRuntimeException(
                        "bad argument #2 to 'tonumber' (base out of range)"
                    );
                }

                ReadOnlySpan<char> numeralSpan = numeral.String.AsSpan().TrimWhitespace();

                if (numeralSpan.IsEmpty)
                {
                    return DynValue.Nil;
                }

                if (TryParseIntegerInBase(numeralSpan, bb, out double parsedValue))
                {
                    return DynValue.NewNumber(parsedValue);
                }

                return DynValue.Nil;
            }
        }

        private static bool TryParseIntegerInBase(
            ReadOnlySpan<char> text,
            int numberBase,
            out double value
        )
        {
            value = 0;
            ReadOnlySpan<char> span = text.TrimWhitespace();
            if (span.IsEmpty)
            {
                return false;
            }

            int index = 0;
            bool negative = false;

            if (span[index] == '+' || span[index] == '-')
            {
                negative = span[index] == '-';
                index++;
            }

            if (index >= span.Length)
            {
                return false;
            }

            double accumulator = 0;
            for (; index < span.Length; index++)
            {
                int digit = GetDigitValue(span[index]);

                if (digit < 0 || digit >= numberBase)
                {
                    return false;
                }

                accumulator = (accumulator * numberBase) + digit;
            }

            value = negative ? -accumulator : accumulator;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDigitValue(char candidate)
        {
            if (candidate >= '0' && candidate <= '9')
            {
                return candidate - '0';
            }

            if (candidate >= 'A' && candidate <= 'Z')
            {
                return candidate - 'A' + 10;
            }

            if (candidate >= 'a' && candidate <= 'z')
            {
                return candidate - 'a' + 10;
            }

            return -1;
        }

        /// <summary>
        /// Parses a Lua numeral string (decimal, hexadecimal integer, or hexadecimal float) per §3.1.
        /// </summary>
        /// <param name="text">Input text to parse.</param>
        /// <param name="version">Lua compatibility version for version-specific parsing rules.</param>
        /// <param name="value">Outputs the parsed numeric value as a <see cref="LuaNumber"/> on success.</param>
        /// <returns><c>true</c> if the text represents a valid Lua numeral; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Lua 5.1 does NOT support hex string parsing in tonumber without an explicit base.
        /// Hex parsing (0x prefix) was added in Lua 5.2.
        /// </para>
        /// <para>
        /// For Lua 5.3+, integers are parsed to full 64-bit precision and returned as integer subtypes.
        /// Floats (including hex floats with 'p' exponent) are returned as float subtypes.
        /// </para>
        /// </remarks>
        private static bool TryParseLuaNumeral(
            string text,
            LuaCompatibilityVersion version,
            out LuaNumber value
        )
        {
            value = LuaNumber.Zero;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            ReadOnlySpan<char> span = text.AsSpan().Trim();
            if (span.IsEmpty)
            {
                return false;
            }

            int index = 0;
            bool negative = false;

            // Handle leading sign
            if (span[index] == '+' || span[index] == '-')
            {
                negative = span[index] == '-';
                index++;
                if (index >= span.Length)
                {
                    return false;
                }
            }

            // Check for hex prefix - only supported in Lua 5.2+
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            if (
                resolved >= LuaCompatibilityVersion.Lua52
                && index + 1 < span.Length
                && span[index] == '0'
                && (span[index + 1] == 'x' || span[index + 1] == 'X')
            )
            {
                // Parse as hex (integer or float) - Lua 5.2+ only
                return TryParseHexLuaNumeral(span, index + 2, negative, out value);
            }

            // Decimal fallback using invariant culture
            if (
                double.TryParse(
                    text,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out double doubleValue
                )
            )
            {
                // Lua 5.2+ does NOT accept "nan" or "inf" string literals (returns nil)
                // Lua 5.1 accepts them via C's strtod
                if (
                    resolved >= LuaCompatibilityVersion.Lua52
                    && (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                )
                {
                    return false;
                }

                // Use LuaNumber.FromDouble to auto-promote whole numbers to integers
                value = LuaNumber.FromDouble(doubleValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a hexadecimal Lua numeral (integer or floating point with optional <c>p</c> exponent).
        /// </summary>
        /// <param name="span">Full span containing the original string (including optional sign and <c>0x</c> prefix).</param>
        /// <param name="startIndex">Index where hex digits begin (after <c>0x</c>).</param>
        /// <param name="negative">Whether a leading minus sign was present.</param>
        /// <param name="value">Outputs the parsed value as a <see cref="LuaNumber"/> on success.</param>
        /// <returns><c>true</c> when the hex literal is valid; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// Hex integers are parsed with full 64-bit precision. Hex floats (with '.' or 'p' exponent)
        /// are parsed as IEEE 754 doubles.
        /// </remarks>
        private static bool TryParseHexLuaNumeral(
            ReadOnlySpan<char> span,
            int startIndex,
            bool negative,
            out LuaNumber value
        )
        {
            value = LuaNumber.Zero;
            int index = startIndex;

            // Track if this is an integer or float - integers get full 64-bit precision
            bool isFloat = false;
            bool digitsSeen = false;
            int integerDigitStart = index;

            // First pass: scan to determine structure and check validity
            while (index < span.Length && IsHexDigit(span[index]))
            {
                index++;
                digitsSeen = true;
            }

            int integerDigitEnd = index;

            // Check for fractional part - makes this a float
            if (index < span.Length && span[index] == '.')
            {
                isFloat = true;
                index++;
                while (index < span.Length && IsHexDigit(span[index]))
                {
                    index++;
                    digitsSeen = true;
                }
            }

            if (!digitsSeen)
            {
                return false;
            }

            // Check for binary exponent - makes this a float
            int exponent = 0;
            if (index < span.Length && (span[index] == 'p' || span[index] == 'P'))
            {
                isFloat = true;
                index++;
                if (index >= span.Length)
                {
                    return false;
                }

                int expSign = 1;
                if (span[index] == '+' || span[index] == '-')
                {
                    if (span[index] == '-')
                    {
                        expSign = -1;
                    }
                    index++;
                }

                if (index >= span.Length || !char.IsDigit(span[index]))
                {
                    return false;
                }

                int expValue = 0;
                while (index < span.Length && char.IsDigit(span[index]))
                {
                    expValue = (expValue * 10) + (span[index] - '0');
                    index++;
                }

                exponent = expSign * expValue;
            }

            // Must have consumed entire input
            if (index != span.Length)
            {
                return false;
            }

            if (isFloat)
            {
                // Parse as floating point with proper handling
                return TryParseHexFloat(span, startIndex, negative, out value);
            }
            else
            {
                // Parse as integer with full 64-bit precision
                return TryParseHexInteger(
                    span.Slice(integerDigitStart, integerDigitEnd - integerDigitStart),
                    negative,
                    out value
                );
            }
        }

        /// <summary>
        /// Parses a hexadecimal integer with full 64-bit precision.
        /// </summary>
        private static bool TryParseHexInteger(
            ReadOnlySpan<char> hexDigits,
            bool negative,
            out LuaNumber value
        )
        {
            value = LuaNumber.Zero;

            if (hexDigits.IsEmpty)
            {
                return false;
            }

            // For very large numbers that would overflow long, fall back to double
            // A long can hold up to 16 hex digits (64 bits / 4 bits per digit)
            // But we need to be careful with overflow during accumulation
            if (hexDigits.Length > 16)
            {
                // Too many digits - parse as double (will lose precision but won't overflow)
                double doubleValue = 0;
                foreach (char c in hexDigits)
                {
                    doubleValue = (doubleValue * 16.0) + HexDigitToValue(c);
                }
                if (negative)
                {
                    doubleValue = -doubleValue;
                }
                value = LuaNumber.FromFloat(doubleValue);
                return true;
            }

            // Parse with overflow checking
            ulong accumulator = 0;
            foreach (char c in hexDigits)
            {
                int digit = HexDigitToValue(c);

                // Check for overflow before multiplication
                if (accumulator > (ulong.MaxValue / 16))
                {
                    // Would overflow - fall back to double
                    double doubleValue = 0;
                    foreach (char ch in hexDigits)
                    {
                        doubleValue = (doubleValue * 16.0) + HexDigitToValue(ch);
                    }
                    if (negative)
                    {
                        doubleValue = -doubleValue;
                    }
                    value = LuaNumber.FromFloat(doubleValue);
                    return true;
                }

                accumulator = (accumulator * 16) + (ulong)digit;
            }

            // Convert to signed long with proper handling of negative numbers
            if (negative)
            {
                // For negative numbers, check if value fits in long range
                if (accumulator > (ulong)long.MaxValue + 1)
                {
                    // Too large for long - return as negative double
                    value = LuaNumber.FromFloat(-(double)accumulator);
                }
                else if (accumulator == (ulong)long.MaxValue + 1)
                {
                    // Exactly long.MinValue
                    value = LuaNumber.FromInteger(long.MinValue);
                }
                else
                {
                    value = LuaNumber.FromInteger(-(long)accumulator);
                }
            }
            else
            {
                // Positive number
                if (accumulator > (ulong)long.MaxValue)
                {
                    // Too large for long - return as double (loses precision but correct behavior)
                    value = LuaNumber.FromFloat((double)accumulator);
                }
                else
                {
                    value = LuaNumber.FromInteger((long)accumulator);
                }
            }

            return true;
        }

        /// <summary>
        /// Parses a hexadecimal floating point number (with '.' or 'p' exponent).
        /// </summary>
        private static bool TryParseHexFloat(
            ReadOnlySpan<char> span,
            int startIndex,
            bool negative,
            out LuaNumber value
        )
        {
            value = LuaNumber.Zero;
            int index = startIndex;

            double significand = 0;
            bool digitsSeen = false;
            int fractionalDigits = 0;

            // Parse integer part
            while (index < span.Length && IsHexDigit(span[index]))
            {
                significand = (significand * 16.0) + HexDigitToValue(span[index]);
                index++;
                digitsSeen = true;
            }

            // Parse fractional part
            if (index < span.Length && span[index] == '.')
            {
                index++;
                while (index < span.Length && IsHexDigit(span[index]))
                {
                    significand = (significand * 16.0) + HexDigitToValue(span[index]);
                    index++;
                    digitsSeen = true;
                    fractionalDigits++;
                }
            }

            if (!digitsSeen)
            {
                return false;
            }

            int exponent = -4 * fractionalDigits;

            // Parse binary exponent (p/P)
            if (index < span.Length && (span[index] == 'p' || span[index] == 'P'))
            {
                index++;
                if (index >= span.Length)
                {
                    return false;
                }

                int expSign = 1;
                if (span[index] == '+' || span[index] == '-')
                {
                    if (span[index] == '-')
                    {
                        expSign = -1;
                    }
                    index++;
                }

                if (index >= span.Length || !char.IsDigit(span[index]))
                {
                    return false;
                }

                int expValue = 0;
                while (index < span.Length && char.IsDigit(span[index]))
                {
                    expValue = (expValue * 10) + (span[index] - '0');
                    index++;
                }

                exponent += expSign * expValue;
            }

            // Must have consumed entire input
            if (index != span.Length)
            {
                return false;
            }

            double result = significand * Math.Pow(2, exponent);
            if (negative)
            {
                result = -result;
            }
            value = LuaNumber.FromFloat(result);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HexDigitToValue(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            return c - 'A' + 10;
        }

        /// <summary>
        /// Implements Lua's <c>print</c> function (§6.1) by formatting the arguments with tabs and forwarding them to
        /// the host-provided debug sink.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Version-specific behavior:</b>
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <b>Lua 5.1–5.3:</b> <c>print</c> calls the global <c>tostring</c> function for each argument.
        /// If the user has overridden <c>tostring</c> in the global environment, that override is called.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>Lua 5.4+:</b> <c>print</c> uses the <c>__tostring</c> metamethod directly (hardwired behavior),
        /// bypassing the global <c>tostring</c> function entirely.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="executionContext">Current execution context, used to resolve the script's debug printer.</param>
        /// <param name="args">Arguments to format and print.</param>
        /// <returns><see cref="DynValue.Nil"/>, matching Lua's return contract.</returns>
        [NovaSharpModuleMethod(Name = "print")]
        public static DynValue Print(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            Script script = executionContext.Script;
            LuaCompatibilityVersion version = script.CompatibilityVersion;
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            // Lua 5.4+ behavior: print uses __tostring metamethod directly (hardwired)
            // Lua 5.1-5.3 behavior: print calls global tostring function (user-overridable)
            bool useLua54HardwiredTostring = resolved >= LuaCompatibilityVersion.Lua54;

            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].IsVoid())
                {
                    break;
                }

                if (i != 0)
                {
                    sb.Append('\t');
                }

                if (useLua54HardwiredTostring)
                {
                    // Lua 5.4+: Use __tostring metamethod directly (current behavior)
                    sb.Append(args.AsStringUsingMeta(executionContext, i, "print"));
                }
                else
                {
                    // Lua 5.1-5.3: Call global tostring function (user-overridable)
                    sb.Append(CallGlobalTostring(script, args[i], version));
                }
            }

            script.Options.DebugPrint(sb.ToString());

            return DynValue.Nil;
        }

        /// <summary>
        /// Calls the global <c>tostring</c> function for a value, respecting user overrides.
        /// Used by <see cref="Print"/> in Lua 5.1–5.3 mode.
        /// </summary>
        /// <param name="script">The script containing the global environment.</param>
        /// <param name="value">The value to convert to string.</param>
        /// <param name="version">The Lua compatibility version for number formatting.</param>
        /// <returns>The string representation of the value.</returns>
        private static string CallGlobalTostring(
            Script script,
            DynValue value,
            LuaCompatibilityVersion version
        )
        {
            // Get the global tostring function
            DynValue tostringFunc = script.Globals.RawGet("tostring");

            if (
                tostringFunc != null
                && (
                    tostringFunc.Type == DataType.Function
                    || tostringFunc.Type == DataType.ClrFunction
                )
            )
            {
                // Call the global tostring function (user-overridable, including CLR callbacks)
                DynValue result = script.Call(tostringFunc, value);

                if (result.Type == DataType.String)
                {
                    return result.String;
                }

                // tostring must return a string - if not, fall back to default formatting
                // This matches Lua's behavior where invalid tostring results are not fatal in print
                return value.ToPrintString(version);
            }

            // No global tostring or not a callable - use default formatting
            return value.ToPrintString(version);
        }

        /// <summary>
        /// Implements Lua 5.1's <c>getfenv</c> function (§5.1) which retrieves the environment table
        /// of a function or the running function at a given stack level.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function was removed in Lua 5.2 and replaced by the <c>_ENV</c> upvalue mechanism.
        /// </para>
        /// <para>
        /// If <paramref name="f"/> is a function, returns its environment.
        /// If <paramref name="f"/> is a number <c>n</c>, returns the environment of the function at stack level <c>n</c>:
        /// Level 0 returns the global environment (thread), level 1 is the function calling <c>getfenv</c>, etc.
        /// Without arguments, returns the environment of the calling function.
        /// </para>
        /// </remarks>
        /// <param name="executionContext">Execution context used to walk the call stack.</param>
        /// <param name="args">Optional function or stack level (defaults to 1 if omitted).</param>
        /// <returns>The environment table for the specified function or stack level.</returns>
        /// <exception cref="ScriptRuntimeException">Thrown if the stack level is invalid or negative.</exception>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [NovaSharpModuleMethod(Name = "getfenv")]
        public static DynValue GetFenv(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.Count > 0 ? args[0] : DynValue.Nil;

            // If no argument or nil, default to level 1 (calling function)
            if (arg.IsNil())
            {
                arg = DynValue.NewNumber(1);
            }

            // Handle function argument
            if (arg.Type == DataType.Function)
            {
                Closure closure = arg.Function;
                return GetEnvironmentFromClosure(closure, executionContext.Script);
            }
            else if (arg.Type == DataType.ClrFunction)
            {
                // C functions always return the global environment
                return DynValue.NewTable(executionContext.Script.Globals);
            }
            else if (arg.Type == DataType.Number)
            {
                // Handle stack level
                double levelDouble = arg.Number;

                if (levelDouble < 0 || levelDouble != Math.Floor(levelDouble))
                {
                    throw ScriptRuntimeException.BadArgument(
                        0,
                        "getfenv",
                        "non-negative integer expected"
                    );
                }

                int level = (int)levelDouble;

                // Level 0 returns the global environment (thread)
                if (level == 0)
                {
                    return DynValue.NewTable(executionContext.Script.Globals);
                }

                // Find the Lua function at the given stack level
                if (
                    !TryGetLuaStackFrameForGetSetFenv(
                        executionContext,
                        level,
                        out CallStackItem frame
                    )
                )
                {
                    throw new ScriptRuntimeException("'getfenv': invalid level");
                }

                // Get the environment from the closure context
                ClosureContext closureScope = frame.ClosureScope;
                return GetEnvironmentFromClosureContext(closureScope, executionContext.Script);
            }
            else
            {
                throw ScriptRuntimeException.BadArgument(
                    0,
                    "getfenv",
                    "function or number expected, got " + arg.Type.ToLuaTypeString()
                );
            }
        }

        /// <summary>
        /// Implements Lua 5.1's <c>setfenv</c> function (§5.1) which changes the environment table
        /// of a function or the running function at a given stack level.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function was removed in Lua 5.2 and replaced by the <c>_ENV</c> upvalue mechanism.
        /// </para>
        /// <para>
        /// If <paramref name="f"/> is a function, sets its environment to the given table.
        /// If <paramref name="f"/> is a number <c>n</c>, sets the environment of the function at stack level <c>n</c>.
        /// Level 0 sets the global environment (thread), level 1 is the function calling <c>setfenv</c>, etc.
        /// </para>
        /// <para>
        /// Returns the function after modifying its environment (except for level 0 which returns nothing).
        /// Cannot change the environment of C functions.
        /// </para>
        /// </remarks>
        /// <param name="executionContext">Execution context used to walk the call stack.</param>
        /// <param name="args">Function or stack level (arg 0) and the new environment table (arg 1).</param>
        /// <returns>The function with modified environment, or nil for level 0.</returns>
        /// <exception cref="ScriptRuntimeException">Thrown if arguments are invalid or trying to change a C function's environment.</exception>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [NovaSharpModuleMethod(Name = "setfenv")]
        public static DynValue SetFenv(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args.Count < 2)
            {
                throw ScriptRuntimeException.BadArgumentNoValue(1, "setfenv", DataType.Table);
            }

            DynValue arg = args[0];
            DynValue envArg = args[1];

            if (envArg.Type != DataType.Table)
            {
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "setfenv",
                    "table expected, got " + envArg.Type.ToLuaTypeString()
                );
            }

            Table newEnv = envArg.Table;

            // Handle function argument
            if (arg.Type == DataType.Function)
            {
                Closure closure = arg.Function;
                SetEnvironmentOnClosure(closure, newEnv);
                return arg; // Return the function
            }
            else if (arg.Type == DataType.ClrFunction)
            {
                throw new ScriptRuntimeException(
                    "'setfenv' cannot change environment of given object"
                );
            }
            else if (arg.Type == DataType.Number)
            {
                double levelDouble = arg.Number;

                if (levelDouble < 0 || levelDouble != Math.Floor(levelDouble))
                {
                    throw ScriptRuntimeException.BadArgument(
                        0,
                        "setfenv",
                        "non-negative integer expected"
                    );
                }

                int level = (int)levelDouble;

                // Level 0 sets the global environment (thread) - return nil
                if (level == 0)
                {
                    // Note: In reference Lua 5.1, setfenv(0, t) sets the global environment
                    // of the running thread. We approximate this by setting _G on the script.
                    // This is a simplified implementation - full thread support would require more infrastructure.
                    executionContext.Script.Globals.MetaTable = newEnv.MetaTable;
                    foreach (TablePair pair in newEnv.GetPairsEnumerator())
                    {
                        executionContext.Script.Globals.Set(pair.Key, pair.Value);
                    }
                    return DynValue.Nil;
                }

                // Find the Lua function at the given stack level
                if (
                    !TryGetLuaStackFrameForGetSetFenv(
                        executionContext,
                        level,
                        out CallStackItem frame
                    )
                )
                {
                    throw new ScriptRuntimeException("'setfenv': invalid level");
                }

                // Set the environment on the closure context
                ClosureContext closureScope = frame.ClosureScope;
                if (closureScope == null || closureScope.Count == 0)
                {
                    throw new ScriptRuntimeException(
                        "'setfenv' cannot change environment of given object"
                    );
                }

                // The first upvalue should be _ENV
                if (
                    closureScope.Symbols.Length > 0
                    && closureScope.Symbols[0] == WellKnownSymbols.ENV
                )
                {
                    closureScope[0].Assign(DynValue.NewTable(newEnv));
                    // Return nil for stack-level setfenv (matches Lua 5.1 behavior for level > 0)
                    // Actually, Lua 5.1 returns the function for level > 0, but we don't have easy access to it
                    return DynValue.Nil;
                }
                else
                {
                    throw new ScriptRuntimeException(
                        "'setfenv' cannot change environment of given object"
                    );
                }
            }
            else
            {
                throw ScriptRuntimeException.BadArgument(
                    0,
                    "setfenv",
                    "function or number expected, got " + arg.Type.ToLuaTypeString()
                );
            }
        }

        /// <summary>
        /// Walks the call stack to find a Lua (non-CLR) function frame at the specified level.
        /// Level 1 is the first Lua function in the stack (after skipping CLR frames).
        /// </summary>
        private static bool TryGetLuaStackFrameForGetSetFenv(
            ScriptExecutionContext executionContext,
            int luaLevel,
            out CallStackItem frame
        )
        {
            frame = null;

            if (luaLevel <= 0)
            {
                return false;
            }

            int matched = 0;

            for (
                int depth = 0;
                executionContext.TryGetStackFrame(depth, out CallStackItem candidate);
                depth++
            )
            {
                // Skip CLR function frames
                if (candidate.ClrFunction != null)
                {
                    continue;
                }

                matched++;

                if (matched == luaLevel)
                {
                    frame = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the environment table from a closure's upvalues.
        /// </summary>
        private static DynValue GetEnvironmentFromClosure(Closure closure, Script script)
        {
            if (closure.UpValuesCount > 0 && closure.GetUpValueName(0) == WellKnownSymbols.ENV)
            {
                DynValue envValue = closure.GetUpValue(0);
                if (envValue.Type == DataType.Table)
                {
                    return envValue;
                }
            }

            // If no _ENV upvalue, return global environment
            return DynValue.NewTable(script.Globals);
        }

        /// <summary>
        /// Gets the environment table from a closure context.
        /// </summary>
        private static DynValue GetEnvironmentFromClosureContext(
            ClosureContext context,
            Script script
        )
        {
            if (
                context != null
                && context.Count > 0
                && context.Symbols.Length > 0
                && context.Symbols[0] == WellKnownSymbols.ENV
            )
            {
                DynValue envValue = context[0];
                if (envValue.Type == DataType.Table)
                {
                    return envValue;
                }
            }

            // If no _ENV upvalue, return global environment
            return DynValue.NewTable(script.Globals);
        }

        /// <summary>
        /// Sets the environment table on a closure's _ENV upvalue.
        /// </summary>
        private static void SetEnvironmentOnClosure(Closure closure, Table newEnv)
        {
            if (closure.UpValuesCount > 0 && closure.GetUpValueName(0) == WellKnownSymbols.ENV)
            {
                DynValue upvalue = closure.GetUpValueMutable(0);
                upvalue.Assign(DynValue.NewTable(newEnv));
            }
            else
            {
                throw new ScriptRuntimeException(
                    "'setfenv' cannot change environment of given object"
                );
            }
        }

        /// <summary>
        /// Implements Lua 5.4's <c>warn</c> helper by routing formatted arguments to <c>_WARN</c> or the debug printer.
        /// </summary>
        /// <param name="executionContext">Execution context used to access the host script and debug sink.</param>
        /// <param name="args">Arguments to format before invoking <c>_WARN</c> or printing.</param>
        /// <returns><see cref="DynValue.Nil"/>, matching Lua's return contract.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua54)]
        [NovaSharpModuleMethod(Name = "warn")]
        public static DynValue Warn(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append('\t');
                }

                sb.Append(args.AsStringUsingMeta(executionContext, i, "warn"));
            }

            string payload = sb.ToString();
            Script script = executionContext.Script;
            DynValue warnHandler = script.Globals.RawGet("_WARN");

            if (
                warnHandler != null
                && (
                    warnHandler.Type == DataType.Function
                    || warnHandler.Type == DataType.ClrFunction
                )
            )
            {
                script.Call(warnHandler, DynValue.NewString(payload));
            }
            else
            {
                Action<string> sink = script.Options.DebugPrint;

                if (sink != null)
                {
                    sink(payload);
                }
                else
                {
                    Console.Error.WriteLine(payload);
                }
            }

            return DynValue.Nil;
        }
    }
}
