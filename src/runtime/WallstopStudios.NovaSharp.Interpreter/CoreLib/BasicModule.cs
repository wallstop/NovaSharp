namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using global::NovaSharp;
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
        [ThreadStatic]
        private static CallbackFunction ToStringContinuationCallback;

        /// <summary>
        /// Implements Lua's <c>type</c> function (§6.1), returning the textual Lua type name for the first argument.
        /// </summary>
        /// <param name="executionContext">
        /// Execution context supplied by the runtime (unused but required by the module contract).
        /// </param>
        /// <param name="args">Arguments passed to <c>type</c>; the first entry is inspected.</param>
        /// <returns>
        /// A string <see cref="LuaValue"/> representing the Lua type name (e.g., <c>"nil"</c>, <c>"table"</c>, <c>"function"</c>).
        /// </returns>
        [NovaSharpModuleMethod(Name = "type")]
        public static LuaValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Type(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "type")]
        private static LuaValue Type(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "type");
            }

            LuaValue v = args[0];
            return LuaValue.NewString(v.Type.ToLuaTypeString());
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
        public static LuaValue Assert(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Assert(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "assert")]
        private static LuaValue Assert(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue v = args[0];
            LuaValue message = args[1];

            if (!v.CastToBool())
            {
                if (message.IsNil)
                {
                    throw new ScriptRuntimeException("assertion failed!"); // { DoNotDecorateMessage = true };
                }
                else
                {
                    throw new ScriptRuntimeException(message.ToPrintString()); // { DoNotDecorateMessage = true };
                }
            }

            return LuaValue.NewTupleNested(args.GetArray());
        }

        /// <summary>
        /// Implements Lua's <c>collectgarbage</c> helper (§6.1) by forwarding the supported modes to the CLR GC.
        /// </summary>
        /// <param name="executionContext">Execution context supplied by the runtime.</param>
        /// <param name="args">Arguments describing the requested mode (nil/<c>"collect"</c>/<c>"restart"</c> trigger a GC).</param>
        /// <returns><see cref="LuaValue.Nil"/> to match Lua's API surface.</returns>
        [NovaSharpModuleMethod(Name = "collectgarbage")]
        public static LuaValue CollectGarbage(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return CollectGarbage(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "collectgarbage")]
        private static LuaValue CollectGarbage(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue opt = args[0];

            string mode = opt.CastToString();

            if (mode == null || mode == "collect" || mode == "restart")
            {
#if PCL || ENABLE_DOTNET
                GC.Collect();
#else
                GC.Collect(2, GCCollectionMode.Forced);
#endif
            }

            return LuaValue.Nil;
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
        public static LuaValue Error(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Error(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "error")]
        private static LuaValue Error(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue message = args.AsType(0, "error", DataType.String, false);
            LuaValue level = args.AsType(1, "error", DataType.Number, true);

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
            if (level.IsNil)
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
        public static LuaValue ToString(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return ToString(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "tostring")]
        private static LuaValue ToString(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "tostring");
            }

            LuaValue v = args[0];
            if (
                !executionContext.TryGetMetamethodTailCall(
                    v,
                    Metamethods.ToStringMeta,
                    out LuaValue tail,
                    v
                )
            )
            {
                // Use version-aware formatting for numbers
                LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
                return LuaValue.NewString(v.ToPrintString(version));
            }

            tail.TailCallData.Continuation = GetToStringContinuationCallback();

            return tail;
        }

        private static CallbackFunction GetToStringContinuationCallback()
        {
            CallbackFunction callback = ToStringContinuationCallback;
            if (callback == null)
            {
                callback = CallbackFunction.FromArgumentView(
                    ToStringContinuation,
                    Metamethods.ToStringMeta
                );
                ToStringContinuationCallback = callback;
            }

            callback.AdditionalData = null;
            return callback;
        }

        /// <summary>
        /// Continuation that validates the result of a <c>__tostring</c> metamethod before returning it to Lua.
        /// </summary>
        /// <remarks>
        /// <para><b>Lua 5.1–5.2:</b> <c>__tostring</c> can return any value (gets passed through, including nil).</para>
        /// <para><b>Lua 5.3+:</b> <c>__tostring</c> MUST return a string; otherwise, an error is raised:
        /// <c>'__tostring' must return a string</c>.</para>
        /// </remarks>
        /// <param name="executionContext">Execution context driving the metamethod invocation.</param>
        /// <param name="args">Arguments flowing out of the metamethod call.</param>
        /// <returns>The validated string result.</returns>
        internal static LuaValue ToStringContinuation(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            LuaValue b = args[0].ToScalar();

            // Lua 5.3+ requires __tostring to return a string; Lua 5.1-5.2 allows any return value
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            bool requireStringReturn = resolved >= LuaCompatibilityVersion.Lua53;

            if (b.IsNil)
            {
                if (requireStringReturn)
                {
                    throw new ScriptRuntimeException("'__tostring' must return a string");
                }

                return b;
            }

            if (b.Type != DataType.String)
            {
                if (requireStringReturn)
                {
                    throw new ScriptRuntimeException("'__tostring' must return a string");
                }

                // Lua 5.1-5.2: allow non-string returns to pass through
                return b;
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
        public static LuaValue Select(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Select(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "select")]
        private static LuaValue Select(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            // Handle "#" case first - doesn't need executionContext
            if (args[0].Type == DataType.String && args[0].String == "#")
            {
                if (args[args.Count - 1].Type == DataType.Tuple)
                {
                    return LuaValue.FromNumber(args.Count - 1 + args[args.Count - 1].Tuple.Length);
                }
                else
                {
                    return LuaValue.FromNumber(args.Count - 1);
                }
            }

            // Numeric index path needs executionContext for version check
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaValue vNum = args.AsType(0, "select", DataType.Number, false);

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
                return LuaValue.Void;
            }

            // Fast path for single element
            if (resultCount == 1)
            {
                return LuaValue.NewTupleNested(args[startIndex]);
            }

            // General case - use pooled list for tuple flattening
            using (ListPool<LuaValue>.Get(resultCount, out List<LuaValue> values))
            {
                for (int i = startIndex; i < args.Count; i++)
                {
                    values.Add(args[i]);
                }

                return LuaValue.NewTupleNested(ListPool<LuaValue>.ToExactArray(values));
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
        /// A numeric <see cref="LuaValue"/> when conversion succeeds; otherwise <see cref="LuaValue.Nil"/>.
        /// </returns>
        [NovaSharpModuleMethod(Name = "tonumber")]
        public static LuaValue ToNumber(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return ToNumber(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "tonumber")]
        private static LuaValue ToNumber(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "tonumber");
            }

            LuaValue e = args[0];
            LuaValue b = args.AsType(1, "tonumber", DataType.Number, true);

            if (b.IsNil)
            {
                return TryConvertStandardNumeral(e, executionContext.Script);
            }
            else
            {
                LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(
                    executionContext.Script.CompatibilityVersion
                );

                int bb;
                if (resolved >= LuaCompatibilityVersion.Lua53)
                {
                    // Lua 5.3+ require the base to have an exact integer representation
                    if (!b.LuaNumber.TryToInteger(out long baseInteger))
                    {
                        throw new ScriptRuntimeException(
                            "bad argument #2 to 'tonumber' (number has no integer representation)"
                        );
                    }

                    if (baseInteger < 2 || baseInteger > 36)
                    {
                        throw new ScriptRuntimeException(
                            "bad argument #2 to 'tonumber' (base out of range)"
                        );
                    }

                    bb = (int)baseInteger;
                }
                else
                {
                    // Lua 5.1/5.2 truncate a fractional base (luaL_checkint) and report
                    // NaN/Infinity conversions as out of range
                    double baseValue = b.Number;
                    if (double.IsNaN(baseValue) || double.IsInfinity(baseValue))
                    {
                        throw new ScriptRuntimeException(
                            "bad argument #2 to 'tonumber' (base out of range)"
                        );
                    }

                    bb = (int)Math.Truncate(baseValue);
                }

                if (bb < 2 || bb > 36)
                {
                    throw new ScriptRuntimeException(
                        "bad argument #2 to 'tonumber' (base out of range)"
                    );
                }

                // Lua 5.1 converts an explicit base 10 like the base-less form:
                // strtod semantics, so hexadecimal and float syntax are accepted
                if (resolved == LuaCompatibilityVersion.Lua51 && bb == 10)
                {
                    return TryConvertStandardNumeral(e, executionContext.Script);
                }

                string numeral = GetBaseNumeralText(e, resolved);

                if (TryParseIntegerInBase(numeral, bb, resolved, out LuaNumber parsedValue))
                {
                    return LuaValue.NewNumber(parsedValue);
                }

                return LuaValue.Nil;
            }
        }

        /// <summary>
        /// Applies the base-less <c>tonumber</c> conversion: numbers pass through, strings parse
        /// with the script's numeral grammar, and every other type yields <c>nil</c>.
        /// </summary>
        private static LuaValue TryConvertStandardNumeral(LuaValue e, Script script)
        {
            if (e.Type == DataType.Number)
            {
                return e;
            }

            if (e.Type != DataType.String)
            {
                return LuaValue.Nil;
            }

            if (LuaNumber.TryParse(e.String, script.CompatibilityVersion, out LuaNumber luaNum))
            {
                return LuaValue.NewNumber(luaNum);
            }

            return LuaValue.Nil;
        }

        /// <summary>
        /// Resolves the numeral text for <c>tonumber(v, base)</c>. Lua 5.1/5.2 coerce number
        /// arguments to strings with the version's <c>tostring</c> formatting (like
        /// <c>luaL_checkstring</c>); Lua 5.3+ require a string argument.
        /// </summary>
        private static string GetBaseNumeralText(LuaValue e, LuaCompatibilityVersion resolved)
        {
            if (e.Type == DataType.String)
            {
                return e.String;
            }

            if (e.Type == DataType.Number && resolved < LuaCompatibilityVersion.Lua53)
            {
                return e.ToPrintString(resolved);
            }

            throw ScriptRuntimeException.BadArgument(0, "tonumber", DataType.String, e.Type, false);
        }

        private static bool TryParseIntegerInBase(
            string text,
            int numberBase,
            LuaCompatibilityVersion resolved,
            out LuaNumber value
        )
        {
            value = LuaNumber.Zero;
            ReadOnlySpan<char> span = text.AsSpan().TrimWhitespace();
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

            // Lua 5.1 defers to strtoul, which accepts an optional 0x/0X prefix in base 16
            if (
                numberBase == 16
                && resolved == LuaCompatibilityVersion.Lua51
                && index + 1 < span.Length
                && span[index] == '0'
                && (span[index + 1] == 'x' || span[index + 1] == 'X')
            )
            {
                index += 2;
            }

            if (index >= span.Length)
            {
                return false;
            }

            if (resolved >= LuaCompatibilityVersion.Lua53)
            {
                // Lua 5.3+ accumulate modulo 2^64 and keep the integer subtype
                ulong bits = 0;
                for (; index < span.Length; index++)
                {
                    int digit = GetDigitValue(span[index]);
                    if (digit < 0 || digit >= numberBase)
                    {
                        return false;
                    }

                    bits = unchecked((bits * (ulong)numberBase) + (ulong)digit);
                }

                if (negative)
                {
                    bits = unchecked(0UL - bits);
                }

                value = LuaNumber.FromInteger(unchecked((long)bits));
                return true;
            }

            if (resolved == LuaCompatibilityVersion.Lua51)
            {
                // Lua 5.1 uses strtoul: valid digits accumulate in unsigned long,
                // saturating at the platform's unsigned long width on overflow
                // (regardless of sign), with the sign otherwise applied through
                // unsigned wraparound. Reference Windows builds have a 32-bit
                // unsigned long; LP64 platforms (Linux/macOS) have 64 bits.
                ulong unsignedMax = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? 0xFFFF_FFFFUL
                    : ulong.MaxValue;
                ulong magnitude = 0;
                bool saturated = false;
                for (; index < span.Length; index++)
                {
                    int digit = GetDigitValue(span[index]);
                    if (digit < 0 || digit >= numberBase)
                    {
                        return false;
                    }

                    if (saturated)
                    {
                        continue;
                    }

                    if (magnitude > (unsignedMax - (ulong)digit) / (ulong)numberBase)
                    {
                        saturated = true;
                        magnitude = unsignedMax;
                        continue;
                    }

                    magnitude = (magnitude * (ulong)numberBase) + (ulong)digit;
                }

                ulong result =
                    saturated ? unsignedMax
                    : negative
                        ? magnitude == 0 ? 0
                            : (unsignedMax - magnitude) + 1
                    : magnitude;
                value = LuaNumber.FromFloat(result);
                return true;
            }

            // Lua 5.2 accumulates in double precision with a signed negation
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

            value = LuaNumber.FromFloat(negative ? -accumulator : accumulator);
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
        /// <returns><see cref="LuaValue.Nil"/>, matching Lua's return contract.</returns>
        [NovaSharpModuleMethod(Name = "print")]
        public static LuaValue Print(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Print(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "print")]
        private static LuaValue Print(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
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

            return LuaValue.Nil;
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
            LuaValue value,
            LuaCompatibilityVersion version
        )
        {
            // Get the global tostring function
            LuaValue tostringFunc = script.Globals.RawGet("tostring");

            if (tostringFunc.Type == DataType.Function || tostringFunc.Type == DataType.ClrFunction)
            {
                // Call the global tostring function (user-overridable, including CLR callbacks)
                LuaValue result = script.CallValues(tostringFunc, value);

                if (result.Type == DataType.String)
                {
                    return result.String;
                }

                // tostring must return a string - throw error if not
                // In Lua 5.1-5.3, 'print' requires 'tostring' to return a string
                throw new ScriptRuntimeException("'tostring' must return a string to 'print'");
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
        public static LuaValue GetFenv(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return GetFenv(executionContext, new CallbackArgumentsView(args));
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [NovaSharpModuleMethod(Name = "getfenv")]
        private static LuaValue GetFenv(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue arg = args.Count > 0 ? args[0] : LuaValue.Nil;

            // If no argument or nil, default to level 1 (calling function)
            if (arg.IsNil)
            {
                arg = LuaValue.NewNumber(1);
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
                return LuaValue.NewTable(executionContext.Script.Globals);
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
                    return LuaValue.NewTable(executionContext.Script.Globals);
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
        public static LuaValue SetFenv(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return SetFenv(executionContext, new CallbackArgumentsView(args));
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [NovaSharpModuleMethod(Name = "setfenv")]
        private static LuaValue SetFenv(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (args.Count < 2)
            {
                throw ScriptRuntimeException.BadArgumentNoValue(1, "setfenv", DataType.Table);
            }

            LuaValue arg = args[0];
            LuaValue envArg = args[1];

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
                        executionContext.Script.Globals.SetValue(pair.Key, pair.Value);
                    }
                    return LuaValue.Nil;
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
                    closureScope.Symbols.Count > 0
                    && closureScope.Symbols[0] == WellKnownSymbols.ENV
                )
                {
                    closureScope.GetSlot(0).Value = LuaValue.NewTable(newEnv);
                    // Return nil for stack-level setfenv (matches Lua 5.1 behavior for level > 0)
                    // Actually, Lua 5.1 returns the function for level > 0, but we don't have easy access to it
                    return LuaValue.Nil;
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
        private static LuaValue GetEnvironmentFromClosure(Closure closure, Script script)
        {
            if (closure.UpValuesCount > 0 && closure.GetUpValueName(0) == WellKnownSymbols.ENV)
            {
                LuaValue envValue = closure.GetUpValue(0);
                if (envValue.Type == DataType.Table)
                {
                    return envValue;
                }
            }

            // If no _ENV upvalue, return global environment
            return LuaValue.NewTable(script.Globals);
        }

        /// <summary>
        /// Gets the environment table from a closure context.
        /// </summary>
        private static LuaValue GetEnvironmentFromClosureContext(
            ClosureContext context,
            Script script
        )
        {
            if (
                context != null
                && context.Count > 0
                && context.Symbols.Count > 0
                && context.Symbols[0] == WellKnownSymbols.ENV
            )
            {
                LuaValue envValue = context[0];
                if (envValue.Type == DataType.Table)
                {
                    return envValue;
                }
            }

            // If no _ENV upvalue, return global environment
            return LuaValue.NewTable(script.Globals);
        }

        /// <summary>
        /// Sets the environment table on a closure's _ENV upvalue.
        /// </summary>
        private static void SetEnvironmentOnClosure(Closure closure, Table newEnv)
        {
            if (closure.UpValuesCount > 0 && closure.GetUpValueName(0) == WellKnownSymbols.ENV)
            {
                closure.GetUpValueSlot(0).Value = LuaValue.NewTable(newEnv);
            }
            else
            {
                throw new ScriptRuntimeException(
                    "'setfenv' cannot change environment of given object"
                );
            }
        }

        /// <summary>
        /// Implements Lua 5.4's <c>warn</c> helper, including its script-local disabled state and control messages.
        /// </summary>
        /// <param name="executionContext">Execution context used to access the host script and warning sink.</param>
        /// <param name="args">Control message or warning arguments to validate and concatenate.</param>
        /// <returns><see cref="LuaValue.Nil"/>, matching Lua's return contract.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua54)]
        [NovaSharpModuleMethod(Name = "warn")]
        public static LuaValue Warn(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Warn(executionContext, new CallbackArgumentsView(args));
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua54)]
        [NovaSharpModuleMethod(Name = "warn")]
        private static LuaValue Warn(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            Script script = executionContext.Script;
            LuaValue firstArgument = args.AsType(0, "warn", DataType.String);

            if (args.Count == 1 && firstArgument.String.StartsWith('@'))
            {
                if (string.Equals(firstArgument.String, "@on", StringComparison.Ordinal))
                {
                    script.WarningOutputEnabled = true;
                }
                else if (string.Equals(firstArgument.String, "@off", StringComparison.Ordinal))
                {
                    script.WarningOutputEnabled = false;
                }

                return LuaValue.Nil;
            }

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = 0; i < args.Count; i++)
            {
                LuaValue argument =
                    i == 0 ? firstArgument : args.AsType(i, "warn", DataType.String);
                sb.Append(argument.String);
            }

            string payload = sb.ToString();
            if (!script.WarningOutputEnabled)
            {
                return LuaValue.Nil;
            }

            LuaValue warnHandler = script.Globals.RawGet("_WARN");

            if (warnHandler.Type == DataType.Function || warnHandler.Type == DataType.ClrFunction)
            {
                script.CallValues(warnHandler, LuaValue.NewString(payload));
            }
            else if (script.Options.Stderr != null)
            {
                using StreamWriter writer = new(
                    script.Options.Stderr,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    bufferSize: 1024,
                    leaveOpen: true
                );
                writer.Write("Lua warning: ");
                writer.WriteLine(payload);
            }
            else
            {
                Console.Error.Write("Lua warning: ");
                Console.Error.WriteLine(payload);
            }

            return LuaValue.Nil;
        }
    }
}
