namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements Lua standard math library (§6.7) including trigonometric helpers, random number
    /// generation, and integer utilities. This module backs the global `math` table.
    /// </summary>
    [NovaSharpModule(Namespace = "math")]
    public static class MathModule
    {
        [NovaSharpModuleConstant(Name = "pi")]
        public const double PI = Math.PI;

        [NovaSharpModuleConstant]
        public const double HUGE = double.MaxValue;

        /// <summary>
        /// The maximum value for an integer in Lua 5.3+ (2^63 - 1).
        /// </summary>
        /// <remarks>
        /// Corresponds to Lua 5.3/5.4 <c>math.maxinteger</c> (§6.7).
        /// </remarks>
        [NovaSharpModuleConstant(Name = "maxinteger")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public const long MAXINTEGER = long.MaxValue;

        /// <summary>
        /// The minimum value for an integer in Lua 5.3+ (-2^63).
        /// </summary>
        /// <remarks>
        /// Corresponds to Lua 5.3/5.4 <c>math.mininteger</c> (§6.7).
        /// </remarks>
        [NovaSharpModuleConstant(Name = "mininteger")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public const long MININTEGER = long.MinValue;

        private static bool TryGetIntegerFromDouble(double number, out long value) =>
            LuaIntegerHelper.TryGetInteger(number, out value);

        private static bool TryGetIntegerFromDynValue(DynValue value, out long integer)
        {
            return LuaIntegerHelper.TryGetInteger(value, out integer);
        }

        private static long RequireIntegerArgument(
            CallbackArguments args,
            int index,
            string funcName
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue value = args.AsType(index, funcName, DataType.Number, false);

            // Use TryGetIntegerFromDynValue to preserve integer precision when the
            // underlying LuaNumber is already an integer (avoids double conversion loss)
            if (!TryGetIntegerFromDynValue(value, out long integer))
            {
                throw ScriptRuntimeException.BadArgument(
                    index,
                    funcName,
                    "integer",
                    value.Type.ToErrorTypeString(),
                    false
                );
            }

            return integer;
        }

        private static DynValue Exec1(
            CallbackArguments args,
            string funcName,
            Func<double, double> func
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.AsType(0, funcName, DataType.Number, false);
            return DynValue.NewNumber(func(arg.Number));
        }

        private static DynValue Exec2(
            CallbackArguments args,
            string funcName,
            Func<double, double, double> func
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.AsType(0, funcName, DataType.Number, false);
            DynValue arg2 = args.AsType(1, funcName, DataType.Number, false);
            return DynValue.NewNumber(func(arg.Number, arg2.Number));
        }

        private static DynValue Exec2N(
            CallbackArguments args,
            string funcName,
            double defVal,
            Func<double, double, double> func
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.AsType(0, funcName, DataType.Number, false);
            DynValue arg2 = args.AsType(1, funcName, DataType.Number, true);

            return DynValue.NewNumber(func(arg.Number, arg2.IsNil() ? defVal : arg2.Number));
        }

        private static DynValue Execaccum(
            CallbackArguments args,
            string funcName,
            Func<double, double, double> func
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            double accum = double.NaN;

            if (args.Count == 0)
            {
                throw new ScriptRuntimeException(
                    "bad argument #1 to '{0}' (number expected, got no value)",
                    funcName
                );
            }

            for (int i = 0; i < args.Count; i++)
            {
                DynValue arg = args.AsType(i, funcName, DataType.Number, false);

                if (i == 0)
                {
                    accum = arg.Number;
                }
                else
                {
                    accum = func(accum, arg.Number);
                }
            }

            return DynValue.NewNumber(accum);
        }

        /// <summary>
        /// Implements Lua 5.3+ `math.type`, returning `"integer"` or `"float"` depending on the
        /// numeric representation, or `nil` for non-numeric values (§6.7).
        /// </summary>
        /// <remarks>
        /// Per Lua 5.3+ spec: "If x is not a number, math.type returns nil."
        /// Reference: https://www.lua.org/manual/5.4/manual.html#pdf-math.type
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the value to inspect.</param>
        /// <returns>`"integer"`, `"float"`, or <see cref="DynValue.Nil"/> for non-numbers.</returns>
        [NovaSharpModuleMethod(Name = "type")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public static DynValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            // Per Lua spec, math.type returns nil for non-numbers (doesn't throw)
            DynValue value = args[0];
            if (value.Type != DataType.Number)
            {
                return DynValue.Nil;
            }

            // Use the LuaNumber's subtype information to determine integer vs float
            return DynValue.NewString(value.LuaNumber.LuaTypeName);
        }

        /// <summary>
        /// Implements Lua 5.3+ `math.tointeger`, coercing a numeric/string argument to an integer or
        /// returning <c>nil</c> when the conversion is not lossless (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 supplies the value to convert.</param>
        /// <returns>The converted integer or <see cref="DynValue.Nil"/>.</returns>
        [NovaSharpModuleMethod(Name = "tointeger")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public static DynValue ToInteger(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            if (args.Count == 0)
            {
                throw ScriptRuntimeException.BadArgumentNoValue(0, "tointeger", DataType.Number);
            }

            DynValue value = args[0];

            // Per Lua 5.3+ spec, math.tointeger returns nil for non-convertible types
            // (including boolean, table, function, userdata, etc.) - it does NOT throw an error.
            if (value.Type != DataType.Number && value.Type != DataType.String)
            {
                return DynValue.Nil;
            }

            if (TryGetIntegerFromDynValue(value, out long integer))
            {
                return DynValue.NewInteger(integer);
            }

            return DynValue.Nil;
        }

        /// <summary>
        /// Implements Lua 5.3+ `math.ult`, performing an unsigned less-than comparison (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments specifying the two integers to compare.</param>
        /// <returns><see cref="DynValue.True"/> when the first argument is &lt; the second.</returns>
        [NovaSharpModuleMethod(Name = "ult")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public static DynValue Ult(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            long left = RequireIntegerArgument(args, 0, "ult");
            long right = RequireIntegerArgument(args, 1, "ult");

            ulong leftUnsigned = unchecked((ulong)left);
            ulong rightUnsigned = unchecked((ulong)right);

            return DynValue.FromBoolean(leftUnsigned < rightUnsigned);
        }

        /// <summary>
        /// Implements Lua `math.abs`, returning the absolute value of the provided number (§6.7).
        /// </summary>
        /// <remarks>
        /// Per Lua 5.3+ specification, if the input is an integer subtype, the result is also
        /// an integer. For the special case of <c>math.mininteger</c>, the negation overflows
        /// and wraps back to <c>mininteger</c> due to two's complement arithmetic.
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 supplies the number to process.</param>
        /// <returns>Absolute value as a number (integer subtype preserved when applicable).</returns>
        [NovaSharpModuleMethod(Name = "abs")]
        public static DynValue Abs(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.AsType(0, "abs", DataType.Number, false);

            // For integer subtypes, preserve integer semantics per Lua 5.3+ spec
            // Use unsigned arithmetic to handle mininteger overflow correctly:
            // 0u - (ulong)mininteger wraps back to mininteger (two's complement behavior)
            if (arg.IsInteger)
            {
                long n = arg.LuaNumber.AsInteger;
                if (n < 0)
                {
                    // Use unsigned negation to match Lua's behavior:
                    // For mininteger, this wraps back to mininteger
                    n = (long)(0UL - (ulong)n);
                }
                return DynValue.NewInteger(n);
            }

            return DynValue.NewNumber(Math.Abs(arg.Number));
        }

        /// <summary>
        /// Implements Lua `math.acos`, returning the arccosine of the given number (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the operand.</param>
        /// <returns>Arccosine result in radians.</returns>
        [NovaSharpModuleMethod(Name = "acos")]
        public static DynValue Acos(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "acos", d => Math.Acos(d));
        }

        /// <summary>
        /// Implements Lua `math.asin`, returning the arcsine of the given number (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the operand.</param>
        /// <returns>Arcsine result in radians.</returns>
        [NovaSharpModuleMethod(Name = "asin")]
        public static DynValue Asin(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "asin", d => Math.Asin(d));
        }

        /// <summary>
        /// Implements Lua `math.atan` (single-argument form), returning arctangent in radians (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the operand.</param>
        /// <returns>Arctangent result.</returns>
        [NovaSharpModuleMethod(Name = "atan")]
        public static DynValue Atan(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "atan", d => Math.Atan(d));
        }

        /// <summary>
        /// Implements Lua `math.atan(y, x)` two-argument variant for quadrant-aware arctangent (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing <c>y</c> and <c>x</c>.</param>
        /// <returns>Arctangent result in radians.</returns>
        [NovaSharpModuleMethod(Name = "atan2")]
        public static DynValue Atan2(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec2(args, "atan2", (d1, d2) => Math.Atan2(d1, d2));
        }

        /// <summary>
        /// Implements Lua `math.ceil`, rounding a number toward positive infinity (§6.7).
        /// </summary>
        /// <remarks>
        /// Per Lua 5.3+ specification, if the input is already an integer subtype,
        /// or if the result fits within the integer range, the result is returned as an integer.
        /// For Lua 5.1/5.2, always returns a float.
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments with the number to round.</param>
        /// <returns>The rounded value (integer subtype when result fits in integer range, Lua 5.3+ only).</returns>
        [NovaSharpModuleMethod(Name = "ceil")]
        public static DynValue Ceil(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.AsType(0, "ceil", DataType.Number, false);

            // Check version for integer promotion behavior
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool supportsIntegerSubtype = version >= LuaCompatibilityVersion.Lua53;

            // Lua 5.1/5.2: always return float
            if (!supportsIntegerSubtype)
            {
                return DynValue.NewNumber(Math.Ceiling(arg.Number));
            }

            // Lua 5.3+: If input is already an integer, return it unchanged
            if (arg.IsInteger)
            {
                return arg;
            }

            double result = Math.Ceiling(arg.Number);

            // Return integer subtype if result fits in integer range.
            // Use TryGetInteger to correctly reject values >= 2^63 that would overflow.
            // Note: (double)long.MaxValue rounds up to 2^63 due to IEEE 754 precision loss,
            // so simple <= comparison doesn't work correctly for boundary values.
            if (LuaIntegerHelper.TryGetInteger(result, out long intResult))
            {
                return DynValue.NewInteger(intResult);
            }

            return DynValue.NewFloat(result);
        }

        /// <summary>
        /// Implements Lua `math.cos`, returning cosine of the given angle in radians (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the angle.</param>
        /// <returns>Cosine of the supplied angle.</returns>
        [NovaSharpModuleMethod(Name = "cos")]
        public static DynValue Cos(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "cos", d => Math.Cos(d));
        }

        /// <summary>
        /// Implements Lua `math.cosh`, returning the hyperbolic cosine (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments supplying the operand.</param>
        /// <returns>Hyperbolic cosine value.</returns>
        [NovaSharpModuleMethod(Name = "cosh")]
        public static DynValue Cosh(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "cosh", d => Math.Cosh(d));
        }

        /// <summary>
        /// Implements Lua `math.deg`, converting radians to degrees (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the angle in radians.</param>
        /// <returns>Angle in degrees.</returns>
        [NovaSharpModuleMethod(Name = "deg")]
        public static DynValue Deg(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "deg", d => d * 180.0 / Math.PI);
        }

        /// <summary>
        /// Implements Lua `math.exp`, returning e raised to the given power (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the exponent.</param>
        /// <returns>Result of <c>e^x</c>.</returns>
        [NovaSharpModuleMethod(Name = "exp")]
        public static DynValue Exp(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "exp", d => Math.Exp(d));
        }

        /// <summary>
        /// Implements Lua `math.floor`, rounding a number toward negative infinity (§6.7).
        /// </summary>
        /// <remarks>
        /// Per Lua 5.3+ specification, if the input is already an integer subtype,
        /// or if the result fits within the integer range, the result is returned as an integer.
        /// For Lua 5.1/5.2, always returns a float.
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments with the value to round.</param>
        /// <returns>The rounded value (integer subtype when result fits in integer range, Lua 5.3+ only).</returns>
        [NovaSharpModuleMethod(Name = "floor")]
        public static DynValue Floor(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue arg = args.AsType(0, "floor", DataType.Number, false);

            // Check version for integer promotion behavior
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool supportsIntegerSubtype = version >= LuaCompatibilityVersion.Lua53;

            // Lua 5.1/5.2: always return float
            if (!supportsIntegerSubtype)
            {
                return DynValue.NewNumber(Math.Floor(arg.Number));
            }

            // Lua 5.3+: If input is already an integer, return it unchanged
            if (arg.IsInteger)
            {
                return arg;
            }

            double result = Math.Floor(arg.Number);

            // Return integer subtype if result fits in integer range.
            // Use TryGetInteger to correctly reject values >= 2^63 that would overflow.
            // Note: (double)long.MaxValue rounds up to 2^63 due to IEEE 754 precision loss,
            // so simple <= comparison doesn't work correctly for boundary values.
            if (LuaIntegerHelper.TryGetInteger(result, out long intResult))
            {
                return DynValue.NewInteger(intResult);
            }

            return DynValue.NewFloat(result);
        }

        /// <summary>
        /// Implements Lua `math.fmod`, returning the remainder of division with sign matching the dividend (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments supplying dividend and divisor.</param>
        /// <returns>Remainder value.</returns>
        [NovaSharpModuleMethod(Name = "fmod")]
        public static DynValue Fmod(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec2(args, "fmod", (d1, d2) => Math.IEEERemainder(d1, d2));
        }

        /// <summary>
        /// Implements Lua `math.frexp`, decomposing a number into mantissa/exponent components (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the number to decompose.</param>
        /// <returns>Tuple {m, e} as defined by Lua.</returns>
        [NovaSharpModuleMethod(Name = "frexp")]
        public static DynValue Frexp(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            // http://stackoverflow.com/questions/389993/extracting-mantissa-and-exponent-from-double-in-c-sharp

            DynValue arg = args.AsType(0, "frexp", DataType.Number, false);

            double d = arg.Number;

            // Translate the double into sign, exponent and mantissa.
            long bits = BitConverter.DoubleToInt64Bits(d);
            // Note that the shift is sign-extended, hence the test against -1 not 1
            bool negative = (bits < 0);
            int exponent = (int)((bits >> 52) & 0x7ffL);
            long mantissa = bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                exponent++;
            }
            // Normal numbers; leave exponent as it is but add extra
            // bit to the front of the mantissa
            else
            {
                mantissa = mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            exponent -= 1075;

            if (mantissa == 0)
            {
                return DynValue.NewTuple(DynValue.FromNumber(0), DynValue.FromNumber(0));
            }

            /* Normalize */
            while ((mantissa & 1) == 0)
            { /*  i.e., Mantissa is even */
                mantissa >>= 1;
                exponent++;
            }

            double m = (double)mantissa;
            double e = (double)exponent;
            while (m >= 1)
            {
                m /= 2.0;
                e += 1.0;
            }

            if (negative)
            {
                m = -m;
            }

            return DynValue.NewTuple(DynValue.NewNumber(m), DynValue.NewNumber(e));
        }

        /// <summary>
        /// Implements Lua `math.ldexp`, building a number from mantissa and exponent (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing mantissa and exponent.</param>
        /// <returns>Result of <c>mantissa * 2^exp</c>.</returns>
        [NovaSharpModuleMethod(Name = "ldexp")]
        public static DynValue Ldexp(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec2(args, "ldexp", (d1, d2) => d1 * Math.Pow(2, d2));
        }

        /// <summary>
        /// Implements Lua `math.log`, returning the logarithm of a number with an optional base (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the value (and optional base).</param>
        /// <returns>The logarithm result.</returns>
        [NovaSharpModuleMethod(Name = "log")]
        public static DynValue Log(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec2N(args, "log", Math.E, (d1, d2) => Math.Log(d1, d2));
        }

        /// <summary>
        /// Implements Lua `math.max`, returning the largest argument (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Numeric arguments to compare.</param>
        /// <returns>Maximum value.</returns>
        [NovaSharpModuleMethod(Name = "max")]
        public static DynValue Max(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Execaccum(args, "max", (d1, d2) => Math.Max(d1, d2));
        }

        /// <summary>
        /// Implements Lua `math.min`, returning the smallest argument (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Numeric arguments to compare.</param>
        /// <returns>Minimum value.</returns>
        [NovaSharpModuleMethod(Name = "min")]
        public static DynValue Min(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Execaccum(args, "min", (d1, d2) => Math.Min(d1, d2));
        }

        /// <summary>
        /// Implements Lua `math.modf`, splitting a number into integer and fractional parts (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments with the value to split.</param>
        /// <returns>Tuple {integerPart, fractionalPart}.</returns>
        [NovaSharpModuleMethod(Name = "modf")]
        public static DynValue Modf(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue arg = args.AsType(0, "modf", DataType.Number, false);
            double integerPart = Math.Truncate(arg.Number);
            double fractionalPart = arg.Number - integerPart;
            return DynValue.NewTuple(
                DynValue.NewNumber(integerPart),
                DynValue.NewNumber(fractionalPart)
            );
        }

        /// <summary>
        /// Implements Lua `math.pow` / exponentiation operator fallback, returning x^y (§6.7).
        /// This function was deprecated in Lua 5.3 and removed in Lua 5.5.
        /// Use the <c>^</c> operator instead: <c>x^y</c>.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments for base and exponent.</param>
        /// <returns>Exponentiation result.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua54)]
        [NovaSharpModuleMethod(Name = "pow")]
        public static DynValue Pow(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec2(args, "pow", (d1, d2) => Math.Pow(d1, d2));
        }

        /// <summary>
        /// Implements Lua `math.rad`, converting degrees to radians (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the degree value.</param>
        /// <returns>Angle in radians.</returns>
        [NovaSharpModuleMethod(Name = "rad")]
        public static DynValue Rad(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "rad", d => d * Math.PI / 180.0);
        }

        /// <summary>
        /// Implements Lua `math.random`, returning a pseudo-random number using the per-script RNG (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// <para>
        /// When called without arguments, returns a pseudo-random float with uniform distribution in [0, 1).
        /// </para>
        /// <para>
        /// When called with an integer argument <c>m</c>, returns a pseudo-random integer in [1, m].
        /// The value m-1 must be representable as an integer, so <c>m</c> cannot be 0.
        /// </para>
        /// <para>
        /// When called with two integer arguments <c>m</c> and <c>n</c>, returns a pseudo-random integer in [m, n].
        /// </para>
        /// <para>
        /// When called with 0 as the only argument, returns an integer with all bits pseudo-random (Lua 5.4).
        /// </para>
        /// </param>
        /// <returns>The generated random number.</returns>
        [NovaSharpModuleMethod(Name = "random")]
        [SuppressMessage(
            "Security",
            "CA5394:Do not use insecure randomness",
            Justification = "Lua math.random is intentionally deterministic and non-cryptographic."
        )]
        public static DynValue Random(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue m = args.AsType(0, "random", DataType.Number, true);
            DynValue n = args.AsType(1, "random", DataType.Number, true);
            IRandomProvider r = executionContext.Script.RandomProvider;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            // No arguments: return float in [0, 1)
            if (m.IsNil() && n.IsNil())
            {
                return DynValue.NewNumber(r.NextDouble());
            }

            // One argument
            if (n.IsNil())
            {
                // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
                long mVal = Utilities.LuaNumberHelpers.ToLongWithValidation(
                    version,
                    m,
                    "random",
                    1
                );

                // math.random(0): return integer with all bits pseudo-random (Lua 5.4+ only, §6.7)
                // In Lua 5.1-5.3, math.random(0) throws "interval is empty"
                if (mVal == 0)
                {
                    // Resolve Latest to current default version for proper version comparison
                    LuaCompatibilityVersion effectiveVersion = LuaVersionDefaults.Resolve(version);
                    if (effectiveVersion >= LuaCompatibilityVersion.Lua54)
                    {
                        return DynValue.NewNumber(r.NextInt64());
                    }

                    throw new ScriptRuntimeException(
                        "bad argument #1 to 'random' (interval is empty)"
                    );
                }

                // math.random(m): return integer in [1, m]
                if (mVal < 1)
                {
                    throw new ScriptRuntimeException(
                        "bad argument #1 to 'random' (interval is empty)"
                    );
                }

                return DynValue.NewNumber(r.NextLong(1, mVal));
            }

            // Two arguments: math.random(m, n) returns integer in [m, n]
            // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
            long mValue = Utilities.LuaNumberHelpers.ToLongWithValidation(version, m, "random", 1);
            long nValue = Utilities.LuaNumberHelpers.ToLongWithValidation(version, n, "random", 2);

            if (mValue > nValue)
            {
                throw new ScriptRuntimeException("bad argument #2 to 'random' (interval is empty)");
            }

            return DynValue.NewNumber(r.NextLong(mValue, nValue));
        }

        /// <summary>
        /// Implements Lua `math.randomseed`, reinitializing the pseudo-random generator.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// <para>
        /// <b>Lua 5.4+ (§6.7):</b>
        /// When called with at least one argument, the integers x and y are joined into a 128-bit seed
        /// that is used to reinitialize the pseudo-random generator. When called with no arguments,
        /// Lua generates a seed with a weak attempt for randomness. Returns the two seed components.
        /// </para>
        /// <para>
        /// <b>Lua 5.1-5.3:</b>
        /// Requires exactly one numeric argument to seed the generator. Returns nothing.
        /// </para>
        /// </param>
        /// <returns>
        /// Lua 5.4+: Returns the two seed components that were effectively used.
        /// Lua 5.1-5.3: Returns <see cref="DynValue.Nil"/> (no return value).
        /// </returns>
        [NovaSharpModuleMethod(Name = "randomseed")]
        [SuppressMessage(
            "Security",
            "CA5394:Do not use insecure randomness",
            Justification = "Lua math.randomseed mirrors the non-cryptographic behavior of the upstream interpreter."
        )]
        public static DynValue RandomSeed(
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
            IRandomProvider r = script.RandomProvider;
            LuaCompatibilityVersion version = script.CompatibilityVersion;

            // Resolve Latest to current default version
            LuaCompatibilityVersion effectiveVersion = LuaVersionDefaults.Resolve(version);

            bool isLua54OrLater = effectiveVersion >= LuaCompatibilityVersion.Lua54;

            DynValue x = args.AsType(0, "randomseed", DataType.Number, true);
            DynValue y = args.AsType(1, "randomseed", DataType.Number, true);

            // Lua 5.1-5.3: require exactly one argument, return nothing
            if (!isLua54OrLater)
            {
                if (x.IsNil())
                {
                    throw ScriptRuntimeException.BadArgumentNoValue(
                        0,
                        "randomseed",
                        DataType.Number
                    );
                }

                // Lua 5.1-5.3: silently truncate to int (no integer representation check)
                r.SetSeed(
                    (int)
                        Math.Floor(
                            x.LuaNumber.IsInteger ? x.LuaNumber.AsInteger : x.LuaNumber.AsFloat
                        )
                );
                return DynValue.Nil;
            }

            // Lua 5.4+: 0-2 arguments, return seed tuple
            (long seedX, long seedY) result;

            if (x.IsNil())
            {
                // No arguments: use system randomness
                result = r.SetSeedFromSystemRandom();
            }
            else if (y.IsNil())
            {
                // One argument: convert to 128-bit seed
                // Lua 5.4+: require integer representation
                long xVal = Utilities.LuaNumberHelpers.ToLongWithValidation(
                    version,
                    x,
                    "randomseed",
                    1
                );
                result = r.SetSeed((int)xVal);
            }
            else
            {
                // Two arguments: full 128-bit seed
                // Lua 5.4+: require integer representation for both
                long xVal = Utilities.LuaNumberHelpers.ToLongWithValidation(
                    version,
                    x,
                    "randomseed",
                    1
                );
                long yVal = Utilities.LuaNumberHelpers.ToLongWithValidation(
                    version,
                    y,
                    "randomseed",
                    2
                );
                result = r.SetSeed(xVal, yVal);
            }

            // Return the two seed components (Lua 5.4 behavior)
            return DynValue.NewTuple(
                DynValue.NewNumber(result.seedX),
                DynValue.NewNumber(result.seedY)
            );
        }

        /// <summary>
        /// Implements Lua `math.sin`, returning sine of the provided angle in radians (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the angle.</param>
        /// <returns>Sine value.</returns>
        [NovaSharpModuleMethod(Name = "sin")]
        public static DynValue Sin(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "sin", d => Math.Sin(d));
        }

        /// <summary>
        /// Implements Lua `math.sinh`, returning the hyperbolic sine (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the operand.</param>
        /// <returns>Hyperbolic sine value.</returns>
        [NovaSharpModuleMethod(Name = "sinh")]
        public static DynValue Sinh(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "sinh", d => Math.Sinh(d));
        }

        /// <summary>
        /// Implements Lua `math.sqrt`, returning the square root (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the operand.</param>
        /// <returns>Square root value.</returns>
        [NovaSharpModuleMethod(Name = "sqrt")]
        public static DynValue Sqrt(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "sqrt", d => Math.Sqrt(d));
        }

        /// <summary>
        /// Implements Lua `math.tan`, returning tangent of the provided angle (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments with the angle.</param>
        /// <returns>Tangent result.</returns>
        [NovaSharpModuleMethod(Name = "tan")]
        public static DynValue Tan(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "tan", d => Math.Tan(d));
        }

        /// <summary>
        /// Implements Lua `math.tanh`, returning the hyperbolic tangent (§6.7).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the operand.</param>
        /// <returns>Hyperbolic tangent value.</returns>
        [NovaSharpModuleMethod(Name = "tanh")]
        public static DynValue Tanh(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return Exec1(args, "tanh", d => Math.Tanh(d));
        }
    }
}
