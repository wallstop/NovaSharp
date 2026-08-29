// Disable warnings about XML documentation
namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib.StringLib;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.LuaPort;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Implements Lua's `string` library (§6.4), providing formatting, pattern matching, and utility helpers.
    /// </summary>
    [NovaSharpModule(Namespace = "string")]
    public static class StringModule
    {
        public const string Base64DumpHeader = "NovaSharp_dump_b64::";

        /// <summary>
        /// Registers the `string` metatable so literal strings inherit the library functions.
        /// For Lua 5.4+, also registers arithmetic metamethods that provide string-to-number coercion.
        /// </summary>
        /// <param name="globalTable">Global table provided by the module host.</param>
        /// <param name="stringTable">Library table containing the exported functions.</param>
        public static void NovaSharpInit(Table globalTable, Table stringTable)
        {
            globalTable = ModuleArgumentValidation.RequireTable(globalTable, nameof(globalTable));
            stringTable = ModuleArgumentValidation.RequireTable(stringTable, nameof(stringTable));

            Script script = globalTable.OwnerScript;
            Table stringMetatable = new(script);
            stringMetatable.Set(Metamethods.Index, LuaValue.NewTable(stringTable));

            // Lua 5.4+: String-to-number coercion was removed from the arithmetic operators themselves.
            // Instead, the string metatable provides arithmetic metamethods that perform the coercion.
            // See Lua 5.4 Reference Manual §3.4.1
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                script.CompatibilityVersion
            );
            if (version >= LuaCompatibilityVersion.Lua54)
            {
                RegisterStringArithmeticMetamethods(stringMetatable);
            }

            script.SetTypeMetatable(DataType.String, stringMetatable);
        }

        /// <summary>
        /// Registers arithmetic metamethods (__add, __sub, __mul, etc.) on the string metatable.
        /// These metamethods coerce string operands to numbers for arithmetic in Lua 5.4+.
        /// Per Lua 5.4 manual §3.4.3: "If the conversion fails, the library calls the metamethod
        /// of the other operand (if present) or it raises an error."
        /// </summary>
        private static void RegisterStringArithmeticMetamethods(Table stringMetatable)
        {
            Script ownerScript = stringMetatable.OwnerScript;

            // __add: a + b
            stringMetatable.Set(
                Metamethods.Add,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(ctx, args, Metamethods.Add, LuaNumber.Add)
                )
            );

            // __sub: a - b
            stringMetatable.Set(
                Metamethods.Sub,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(ctx, args, Metamethods.Sub, LuaNumber.Subtract)
                )
            );

            // __mul: a * b
            stringMetatable.Set(
                Metamethods.Mul,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(ctx, args, Metamethods.Mul, LuaNumber.Multiply)
                )
            );

            // __div: a / b
            stringMetatable.Set(
                Metamethods.Div,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(ctx, args, Metamethods.Div, LuaNumber.Divide)
                )
            );

            // __mod: a % b
            stringMetatable.Set(
                Metamethods.Mod,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(
                            ctx,
                            args,
                            Metamethods.Mod,
                            static (a, b) => LuaNumber.Modulo(a, b, LuaCompatibilityVersion.Lua54)
                        )
                )
            );

            // __pow: a ^ b
            stringMetatable.Set(
                Metamethods.Pow,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(ctx, args, Metamethods.Pow, LuaNumber.Power)
                )
            );

            // __idiv: a // b (floor division)
            stringMetatable.Set(
                Metamethods.IDiv,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                        StringBinaryArithmetic(ctx, args, Metamethods.IDiv, LuaNumber.FloorDivide)
                )
            );

            // __unm: -a (unary minus)
            stringMetatable.Set(
                Metamethods.Unm,
                LuaValue.NewCallbackView(
                    ownerScript,
                    static (ctx, args) =>
                    {
                        LuaNumber? a = CoerceToLuaNumber(args[0]);
                        if (!a.HasValue)
                        {
                            throw ScriptRuntimeException.ArithmeticOnNonNumber(args[0]);
                        }
                        return LuaValue.NewNumber(LuaNumber.Negate(a.Value));
                    }
                )
            );
        }

        /// <summary>
        /// Implements a binary arithmetic metamethod for strings with proper fallback behavior.
        /// Per Lua 5.4 manual: if coercion fails, calls the other operand's metamethod (if present).
        /// </summary>
        private static LuaValue StringBinaryArithmetic(
            ScriptExecutionContext ctx,
            CallbackArgumentsView args,
            string metamethodName,
            Func<LuaNumber, LuaNumber, LuaNumber> operation
        )
        {
            LuaValue left = args[0];
            LuaValue right = args[1];

            LuaNumber? a = CoerceToLuaNumber(left);
            LuaNumber? b = CoerceToLuaNumber(right);

            // If both can be coerced to numbers, perform the operation
            if (a.HasValue && b.HasValue)
            {
                return LuaValue.NewNumber(operation(a.Value, b.Value));
            }

            // Coercion failed - try to fall back to the other operand's metamethod
            // We need to check if the non-string operand has its own metamethod
            LuaValue nonStringOperand = left.Type == DataType.String ? right : left;

            // Only try fallback if the other operand could have a metamethod (tables, userdata)
            if (
                nonStringOperand.Type == DataType.Table
                || nonStringOperand.Type == DataType.UserData
            )
            {
                if (
                    ctx.TryGetBinaryMetamethod(
                        nonStringOperand,
                        nonStringOperand,
                        metamethodName,
                        out LuaValue otherMetamethod
                    ) && otherMetamethod.IsNotNil()
                )
                {
                    // Call the other operand's metamethod with the original arguments
                    return ctx.Script.CallValues(otherMetamethod, left, right);
                }
            }

            // No fallback available - throw the appropriate error
            throw ArithmeticCoercionError(left, right);
        }

        /// <summary>
        /// Coerces a LuaValue to a LuaNumber for string arithmetic metamethods.
        /// Returns null if the value cannot be coerced.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LuaNumber? CoerceToLuaNumber(LuaValue value)
        {
            if (value.Type == DataType.Number)
            {
                return value.LuaNumber;
            }

            if (value.Type == DataType.String)
            {
                if (LuaNumber.TryParse(value.String, out LuaNumber result))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates an appropriate arithmetic error for string coercion failure.
        /// </summary>
        private static ScriptRuntimeException ArithmeticCoercionError(LuaValue a, LuaValue b)
        {
            // Report the first non-number operand
            if (
                a.Type != DataType.Number
                && (a.Type != DataType.String || !LuaNumber.TryParse(a.String, out _))
            )
            {
                return ScriptRuntimeException.ArithmeticOnNonNumber(a);
            }
            return ScriptRuntimeException.ArithmeticOnNonNumber(b);
        }

        /// <summary>
        /// Implements Lua `string.dump`, returning a base64-encoded binary chunk for a function (§6.4.2).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments supplying the function to dump.</param>
        /// <returns>Serialized chunk as a string.</returns>
        [NovaSharpModuleMethod(Name = "dump")]
        public static LuaValue Dump(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Dump(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "dump")]
        private static LuaValue Dump(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue fn = args.AsType(0, "dump", DataType.Function, false);

            try
            {
                byte[] bytes;
                using (MemoryStream ms = new())
                {
                    executionContext.Script.Dump(fn, ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    bytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(bytes);
                return LuaValue.NewString(Base64DumpHeader + base64);
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }
        }

        /// <summary>
        /// Implements Lua `string.char`, converting numeric code-points into a string (§6.4.1).
        /// </summary>
        /// <remarks>
        /// In Lua 5.3+, arguments must have an exact integer representation (no NaN, Infinity, or fractional parts).
        /// In Lua 5.1/5.2, NaN and Infinity are treated as 0, and floats are truncated to integers.
        /// All versions require the final value to be in the range 0-255.
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Numeric or numeric-string arguments.</param>
        /// <returns>Concatenated characters.</returns>
        [NovaSharpModuleMethod(Name = "char")]
        public static LuaValue CharFunction(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return CharFunction(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "char")]
        private static LuaValue CharFunction(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool requiresIntegerRepresentation = version >= LuaCompatibilityVersion.Lua53;

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = 0; i < args.Count; i++)
            {
                LuaValue v = args[i];
                double d = 0d;

                if (v.Type == DataType.String)
                {
                    double? nullableNumber = v.CastToNumber();
                    if (nullableNumber == null)
                    {
                        args.AsType(i, "char", DataType.Number, false);
                    }
                    else
                    {
                        d = nullableNumber.Value;
                    }
                }
                else
                {
                    args.AsType(i, "char", DataType.Number, false);
                    d = v.Number;
                }

                int charValue;

                if (requiresIntegerRepresentation)
                {
                    // Lua 5.3+ behavior: Require exact integer representation
                    // NaN, Infinity, and non-integer floats all error
                    if (double.IsNaN(d) || double.IsInfinity(d))
                    {
                        using Utf16ValueStringBuilder errSb = ZStringBuilder.Create();
                        errSb.Append("bad argument #");
                        errSb.Append(i + 1);
                        errSb.Append(" to 'char' (number has no integer representation)");
                        throw new ScriptRuntimeException(errSb.ToString());
                    }

                    double floored = Math.Floor(d);
                    if (floored != d)
                    {
                        // Non-integer float (e.g., 65.5)
                        using Utf16ValueStringBuilder errSb = ZStringBuilder.Create();
                        errSb.Append("bad argument #");
                        errSb.Append(i + 1);
                        errSb.Append(" to 'char' (number has no integer representation)");
                        throw new ScriptRuntimeException(errSb.ToString());
                    }

                    if (floored < 0 || floored > 255)
                    {
                        using Utf16ValueStringBuilder errSb = ZStringBuilder.Create();
                        errSb.Append("bad argument #");
                        errSb.Append(i + 1);
                        errSb.Append(" to 'char' (value out of range)");
                        throw new ScriptRuntimeException(errSb.ToString());
                    }

                    charValue = (int)floored;
                }
                else
                {
                    // Lua 5.1/5.2 behavior:
                    // - NaN → 0 (silent)
                    // - Negative infinity → 0 (silent)
                    // - Positive infinity → 0 (silent)
                    // All special float values are silently converted to 0 in Lua 5.1/5.2.
                    // Verified with lua5.1 -e "print(string.byte(string.char(1/0)))" → 0
                    double floored = Math.Floor(d);
                    if (
                        double.IsNaN(floored)
                        || double.IsNegativeInfinity(floored)
                        || double.IsPositiveInfinity(floored)
                    )
                    {
                        charValue = 0;
                    }
                    else if (floored < 0 || floored > 255)
                    {
                        using Utf16ValueStringBuilder errSb = ZStringBuilder.Create();
                        errSb.Append("bad argument #");
                        errSb.Append(i + 1);
                        errSb.Append(" to 'char' (value out of range)");
                        throw new ScriptRuntimeException(errSb.ToString());
                    }
                    else
                    {
                        charValue = (int)floored;
                    }
                }

                sb.Append((char)charValue);
            }

            return LuaValue.NewString(sb.ToString());
        }

        /// <summary>
        /// Implements Lua `string.byte`, returning byte values for a string slice (§6.4.1).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">String plus optional start/end indices.</param>
        /// <returns>Tuple of byte values.</returns>
        /// <remarks>
        /// In Lua 5.3+, index arguments must have an exact integer representation.
        /// Non-integer indices (including NaN, Infinity, and fractional values) will throw
        /// "bad argument #N to 'byte' (number has no integer representation)".
        /// In Lua 5.1/5.2, non-integer indices are silently truncated via floor.
        /// </remarks>
        [NovaSharpModuleMethod(Name = "byte")]
        public static LuaValue Byte(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Byte(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "byte")]
        private static LuaValue Byte(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue vs = args.AsType(0, "byte", DataType.String, false);
            LuaValue vi = args.AsType(1, "byte", DataType.Number, true);
            LuaValue vj = args.AsType(2, "byte", DataType.Number, true);

            // Validate indices for Lua 5.3+ integer representation requirements
            LuaNumberHelpers.ValidateStringIndices(
                executionContext.Script.CompatibilityVersion,
                vi,
                vj,
                "byte"
            );

            return PerformByteLike(vs, vi, vj, static i => NormalizeByte(i));
        }

        /// <summary>
        /// NovaSharp extension mirroring `string.byte` but returning the raw Unicode code points.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">String plus optional range.</param>
        /// <returns>Tuple of code points.</returns>
        [NovaSharpModuleMethod(Name = "unicode")]
        public static LuaValue Unicode(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Unicode(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "unicode")]
        private static LuaValue Unicode(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue vs = args.AsType(0, "unicode", DataType.String, false);
            LuaValue vi = args.AsType(1, "unicode", DataType.Number, true);
            LuaValue vj = args.AsType(2, "unicode", DataType.Number, true);

            return PerformByteLike(vs, vi, vj, static i => i);
        }

        private static LuaValue PerformByteLike(
            LuaValue vs,
            LuaValue vi,
            LuaValue vj,
            Func<int, int> filter
        )
        {
            StringRange range = StringRange.FromLuaRange(vi, vj, null);
            ReadOnlySpan<char> span = range.ApplyToSpan(vs.String);

            int length = span.Length;

            if (length == 0)
            {
                return LuaValue.Void;
            }

            // Fast path for single character - avoid array allocation
            if (length == 1)
            {
                return LuaValue.NewNumber(filter(span[0]));
            }

            LuaValue[] rets = new LuaValue[length];

            for (int i = 0; i < length; ++i)
            {
                rets[i] = LuaValue.NewNumber(filter(span[i]));
            }

            return LuaValue.NewTuple(rets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NormalizeByte(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            long truncated = (long)Math.Floor(value);
            int normalized = (int)(truncated % 256);
            if (normalized < 0)
            {
                normalized += 256;
            }

            return normalized;
        }

        /// <summary>
        /// Normalizes Lua substring indices (1-based/negative) into zero-based offsets.
        /// </summary>
        private static int? AdjustIndex(string s, LuaValue vi, int defval)
        {
            if (vi.IsNil)
            {
                return defval;
            }

            int i = (int)Math.Round(vi.Number, 0);

            if (i == 0)
            {
                return null;
            }

            if (i > 0)
            {
                return i - 1;
            }

            return s.Length - i;
        }

        /// <summary>
        /// Internal helpers exposed to tests so Lua index normalization can be validated without duplicating logic.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Mirrors <see cref="AdjustIndex"/> for unit tests.
            /// </summary>
            public static int? AdjustIndex(string s, LuaValue vi, int defaultValue)
            {
                return StringModule.AdjustIndex(s, vi, defaultValue);
            }
        }

        /// <summary>
        /// Implements Lua `string.len`, returning the number of bytes in the string (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "len")]
        public static LuaValue Len(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Len(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "len")]
        private static LuaValue Len(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue vs = args.AsType(0, "len", DataType.String, false);
            return LuaValue.FromNumber(vs.String.Length);
        }

        /// <summary>
        /// Implements Lua `string.match`, returning captures for the first pattern match (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "match")]
        public static LuaValue Match(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Match(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "match")]
        private static LuaValue Match(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return executionContext.EmulateClassicCall(args, "match", KopiLuaStringLib.str_match);
        }

        /// <summary>
        /// Implements Lua `string.gmatch`, returning an iterator over pattern matches (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "gmatch")]
        public static LuaValue GMatch(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return GMatch(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "gmatch")]
        private static LuaValue GMatch(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return executionContext.EmulateClassicCall(args, "gmatch", KopiLuaStringLib.str_gmatch);
        }

        /// <summary>
        /// Implements Lua `string.gsub`, performing pattern substitution (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "gsub")]
        public static LuaValue GSub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return GSub(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "gsub")]
        private static LuaValue GSub(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return executionContext.EmulateClassicCall(args, "gsub", KopiLuaStringLib.str_gsub);
        }

        /// <summary>
        /// Implements Lua `string.find`, locating the first pattern occurrence (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "find")]
        public static LuaValue Find(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Find(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "find")]
        private static LuaValue Find(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return executionContext.EmulateClassicCall(args, "find", KopiLuaStringLib.str_find);
        }

        /// <summary>
        /// Implements Lua `string.lower`, converting characters to lower-case (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "lower")]
        public static LuaValue Lower(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Lower(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "lower")]
        private static LuaValue Lower(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS = args.AsType(0, "lower", DataType.String, false);
            return LuaValue.NewString(InvariantString.ToLowerInvariantIfNeeded(argS.String));
        }

        /// <summary>
        /// Implements Lua `string.upper`, converting characters to upper-case (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "upper")]
        public static LuaValue Upper(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Upper(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "upper")]
        private static LuaValue Upper(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS = args.AsType(0, "upper", DataType.String, false);
            return LuaValue.NewString(InvariantString.ToUpperInvariantIfNeeded(argS.String));
        }

        /// <summary>
        /// Implements Lua `string.rep`, repeating a string N times with an optional separator (§6.4.1).
        /// </summary>
        /// <remarks>
        /// <para>
        /// In Lua 5.3+, the count argument must have an exact integer representation.
        /// Non-integer counts (including NaN, Infinity, and fractional values) will throw
        /// "bad argument #2 to 'rep' (number has no integer representation)".
        /// In Lua 5.1/5.2, non-integer counts are silently truncated via floor.
        /// </para>
        /// <para>
        /// The separator parameter was added in Lua 5.2 and is ignored in Lua 5.1 mode.
        /// </para>
        /// </remarks>
        [NovaSharpModuleMethod(Name = "rep")]
        public static LuaValue Rep(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Rep(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "rep")]
        private static LuaValue Rep(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS = args.AsType(0, "rep", DataType.String, false);
            LuaValue argN = args.AsType(1, "rep", DataType.Number, false);
            LuaValue argSep = args.AsType(2, "rep", DataType.String, true);

            // Validate count for Lua 5.3+ integer representation requirements
            LuaNumberHelpers.ValidateIntegerArgument(
                executionContext.Script.CompatibilityVersion,
                argN,
                "rep",
                2
            );

            if (String.IsNullOrEmpty(argS.String) || (argN.Number < 1))
            {
                return LuaValue.EmptyString;
            }

            // Separator was added in Lua 5.2 - ignore it in Lua 5.1 mode
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            string sep =
                (argSep.IsNotNil() && version >= LuaCompatibilityVersion.Lua52)
                    ? argSep.String
                    : null;

            int count = (int)Math.Floor(argN.Number);
            using Utf16ValueStringBuilder result = ZStringBuilder.Create();

            for (int i = 0; i < count; ++i)
            {
                if (i != 0 && sep != null)
                {
                    result.Append(sep);
                }

                result.Append(argS.String);
            }

            return LuaValue.NewString(result.ToString());
        }

        /// <summary>
        /// Implements Lua `string.format`, producing formatted strings (printf-style) (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "format")]
        public static LuaValue Format(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Format(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "format")]
        private static LuaValue Format(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return executionContext.EmulateClassicCall(args, "format", KopiLuaStringLib.str_format);
        }

        /// <summary>
        /// Implements Lua `string.reverse`, reversing byte order (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "reverse")]
        public static LuaValue Reverse(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Reverse(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "reverse")]
        private static LuaValue Reverse(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS = args.AsType(0, "reverse", DataType.String, false);
            string str = argS.String;

            if (String.IsNullOrEmpty(str))
            {
                return LuaValue.EmptyString;
            }

            // Use stackalloc for common small strings to avoid heap allocation
            const int StackAllocThreshold = 256;

            if (str.Length <= StackAllocThreshold)
            {
                Span<char> buffer = stackalloc char[str.Length];
                str.AsSpan().CopyTo(buffer);
                buffer.Reverse();
                return LuaValue.NewString(new string(buffer));
            }
            else
            {
                // Fallback to heap allocation for large strings
                char[] elements = str.ToCharArray();
                Array.Reverse(elements);
                return LuaValue.NewString(new string(elements));
            }
        }

        /// <summary>
        /// Implements Lua `string.sub`, returning a substring defined by Lua indices (§6.4.1).
        /// </summary>
        /// <remarks>
        /// In Lua 5.3+, index arguments must have an exact integer representation.
        /// Non-integer indices (including NaN, Infinity, and fractional values) will throw
        /// "bad argument #N to 'sub' (number has no integer representation)".
        /// In Lua 5.1/5.2, non-integer indices are silently truncated via floor.
        /// </remarks>
        [NovaSharpModuleMethod(Name = "sub")]
        public static LuaValue Sub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Sub(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "sub")]
        private static LuaValue Sub(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS = args.AsType(0, "sub", DataType.String, false);
            LuaValue argI = args.AsType(1, "sub", DataType.Number, true);
            LuaValue argJ = args.AsType(2, "sub", DataType.Number, true);

            // Validate indices for Lua 5.3+ integer representation requirements
            LuaNumberHelpers.ValidateStringIndices(
                executionContext.Script.CompatibilityVersion,
                argI,
                argJ,
                "sub"
            );

            StringRange range = StringRange.FromLuaRange(argI, argJ, -1);
            string s = range.ApplyToString(argS.String);

            return LuaValue.NewString(s);
        }

        /// <summary>
        /// NovaSharp helper: returns whether a string begins with the provided prefix (ordinal comparison).
        /// </summary>
        [NovaSharpModuleMethod(Name = "startswith")]
        public static LuaValue StartsWith(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return StartsWith(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "startswith")]
        private static LuaValue StartsWith(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS1 = args.AsType(0, "startsWith", DataType.String, true);
            LuaValue argS2 = args.AsType(1, "startsWith", DataType.String, true);

            if (argS1.IsNil || argS2.IsNil)
            {
                return LuaValue.False;
            }

            return LuaValue.NewBoolean(
                argS1.String.StartsWith(argS2.String, StringComparison.Ordinal)
            );
        }

        /// <summary>
        /// NovaSharp helper: returns whether a string ends with the provided suffix (ordinal comparison).
        /// </summary>
        [NovaSharpModuleMethod(Name = "endswith")]
        public static LuaValue EndsWith(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return EndsWith(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "endswith")]
        private static LuaValue EndsWith(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS1 = args.AsType(0, "endsWith", DataType.String, true);
            LuaValue argS2 = args.AsType(1, "endsWith", DataType.String, true);

            if (argS1.IsNil || argS2.IsNil)
            {
                return LuaValue.False;
            }

            return LuaValue.NewBoolean(
                argS1.String.EndsWith(argS2.String, StringComparison.Ordinal)
            );
        }

        /// <summary>
        /// NovaSharp helper: returns whether a string contains the provided substring (ordinal comparison).
        /// </summary>
        [NovaSharpModuleMethod(Name = "contains")]
        public static LuaValue Contains(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return Contains(executionContext, new CallbackArgumentsView(args));
        }

        [NovaSharpModuleMethod(Name = "contains")]
        private static LuaValue Contains(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            LuaValue argS1 = args.AsType(0, "contains", DataType.String, true);
            LuaValue argS2 = args.AsType(1, "contains", DataType.String, true);

            if (argS1.IsNil || argS2.IsNil)
            {
                return LuaValue.False;
            }

            return LuaValue.NewBoolean(
                argS1.String.Contains(argS2.String, StringComparison.Ordinal)
            );
        }
    }
}
