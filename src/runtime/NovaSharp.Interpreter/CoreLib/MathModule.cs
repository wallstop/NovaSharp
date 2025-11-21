// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.PredefinedUserData;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing math Lua functions
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "math")]
    public class MathModule
    {
        [NovaSharpModuleConstant(Name = "pi")]
        public const double PI = Math.PI;

        [NovaSharpModuleConstant]
        public const double HUGE = double.MaxValue;

        private static Random GetRandom(Script s)
        {
            DynValue rr = s.Registry.Get("F61E3AA7247D4D1EB7A45430B0C8C9BB_MATH_RANDOM");
            return (rr.UserData.Object as AnonWrapper<Random>).Value;
        }

        private static void SetRandom(Script s, Random random)
        {
            DynValue rr = UserData.Create(new AnonWrapper<Random>(random));
            s.Registry.Set("F61E3AA7247D4D1EB7A45430B0C8C9BB_MATH_RANDOM", rr);
        }

        private static bool TryGetIntegerFromDouble(double number, out long value)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                value = 0;
                return false;
            }

            if (number < long.MinValue || number > long.MaxValue)
            {
                value = 0;
                return false;
            }

            double truncated = Math.Truncate(number);

            if (truncated != number)
            {
                value = 0;
                return false;
            }

            value = (long)truncated;
            return true;
        }

        private static bool TryGetIntegerFromDynValue(DynValue value, out long integer)
        {
            if (value.Type == DataType.Number)
            {
                return TryGetIntegerFromDouble(value.Number, out integer);
            }

            if (
                value.Type == DataType.String
                && double.TryParse(
                    value.String,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsed
                )
            )
            {
                return TryGetIntegerFromDouble(parsed, out integer);
            }

            integer = 0;
            return false;
        }

        private static long RequireIntegerArgument(
            CallbackArguments args,
            int index,
            string funcName
        )
        {
            DynValue value = args.AsType(index, funcName, DataType.Number, false);

            if (!TryGetIntegerFromDouble(value.Number, out long integer))
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

        public static void NovaSharpInit(Table globalTable, Table ioTable)
        {
            SetRandom(globalTable.OwnerScript, new Random());
        }

        private static DynValue Exec1(
            CallbackArguments args,
            string funcName,
            Func<double, double> func
        )
        {
            DynValue arg = args.AsType(0, funcName, DataType.Number, false);
            return DynValue.NewNumber(func(arg.Number));
        }

        private static DynValue Exec2(
            CallbackArguments args,
            string funcName,
            Func<double, double, double> func
        )
        {
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

        [NovaSharpModuleMethod(Name = "type")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public static DynValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue value = args.AsType(0, "type", DataType.Number, false);
            return DynValue.NewString(
                TryGetIntegerFromDouble(value.Number, out _) ? "integer" : "float"
            );
        }

        [NovaSharpModuleMethod(Name = "tointeger")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public static DynValue ToInteger(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (args.Count == 0)
            {
                throw ScriptRuntimeException.BadArgumentNoValue(0, "tointeger", DataType.Number);
            }

            DynValue value = args[0];

            if (value.Type != DataType.Number && value.Type != DataType.String)
            {
                throw ScriptRuntimeException.BadArgument(
                    0,
                    "tointeger",
                    DataType.Number,
                    value.Type,
                    false
                );
            }

            if (TryGetIntegerFromDynValue(value, out long integer))
            {
                return DynValue.NewNumber(integer);
            }

            return DynValue.Nil;
        }

        [NovaSharpModuleMethod(Name = "ult")]
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        public static DynValue Ult(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            long left = RequireIntegerArgument(args, 0, "ult");
            long right = RequireIntegerArgument(args, 1, "ult");

            ulong leftUnsigned = unchecked((ulong)left);
            ulong rightUnsigned = unchecked((ulong)right);

            return DynValue.NewBoolean(leftUnsigned < rightUnsigned);
        }

        [NovaSharpModuleMethod(Name = "abs")]
        public static DynValue Abs(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "abs", d => Math.Abs(d));
        }

        [NovaSharpModuleMethod(Name = "acos")]
        public static DynValue Acos(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "acos", d => Math.Acos(d));
        }

        [NovaSharpModuleMethod(Name = "asin")]
        public static DynValue Asin(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "asin", d => Math.Asin(d));
        }

        [NovaSharpModuleMethod(Name = "atan")]
        public static DynValue Atan(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "atan", d => Math.Atan(d));
        }

        [NovaSharpModuleMethod(Name = "atan2")]
        public static DynValue Atan2(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return Exec2(args, "atan2", (d1, d2) => Math.Atan2(d1, d2));
        }

        [NovaSharpModuleMethod(Name = "ceil")]
        public static DynValue Ceil(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "ceil", d => Math.Ceiling(d));
        }

        [NovaSharpModuleMethod(Name = "cos")]
        public static DynValue Cos(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "cos", d => Math.Cos(d));
        }

        [NovaSharpModuleMethod(Name = "cosh")]
        public static DynValue Cosh(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "cosh", d => Math.Cosh(d));
        }

        [NovaSharpModuleMethod(Name = "deg")]
        public static DynValue Deg(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "deg", d => d * 180.0 / Math.PI);
        }

        [NovaSharpModuleMethod(Name = "exp")]
        public static DynValue Exp(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "exp", d => Math.Exp(d));
        }

        [NovaSharpModuleMethod(Name = "floor")]
        public static DynValue Floor(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return Exec1(args, "floor", d => Math.Floor(d));
        }

        [NovaSharpModuleMethod(Name = "fmod")]
        public static DynValue Fmod(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec2(args, "fmod", (d1, d2) => Math.IEEERemainder(d1, d2));
        }

        [NovaSharpModuleMethod(Name = "frexp")]
        public static DynValue Frexp(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
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
                return DynValue.NewTuple(DynValue.NewNumber(0), DynValue.NewNumber(0));
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

        [NovaSharpModuleMethod(Name = "ldexp")]
        public static DynValue Ldexp(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return Exec2(args, "ldexp", (d1, d2) => d1 * Math.Pow(2, d2));
        }

        [NovaSharpModuleMethod(Name = "log")]
        public static DynValue Log(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec2N(args, "log", Math.E, (d1, d2) => Math.Log(d1, d2));
        }

        [NovaSharpModuleMethod(Name = "max")]
        public static DynValue Max(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Execaccum(args, "max", (d1, d2) => Math.Max(d1, d2));
        }

        [NovaSharpModuleMethod(Name = "min")]
        public static DynValue Min(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Execaccum(args, "min", (d1, d2) => Math.Min(d1, d2));
        }

        [NovaSharpModuleMethod(Name = "modf")]
        public static DynValue Modf(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue arg = args.AsType(0, "modf", DataType.Number, false);
            double integerPart = Math.Truncate(arg.Number);
            double fractionalPart = arg.Number - integerPart;
            return DynValue.NewTuple(
                DynValue.NewNumber(integerPart),
                DynValue.NewNumber(fractionalPart)
            );
        }

        [NovaSharpModuleMethod(Name = "pow")]
        public static DynValue Pow(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec2(args, "pow", (d1, d2) => Math.Pow(d1, d2));
        }

        [NovaSharpModuleMethod(Name = "rad")]
        public static DynValue Rad(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "rad", d => d * Math.PI / 180.0);
        }

        [NovaSharpModuleMethod(Name = "random")]
        public static DynValue Random(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue m = args.AsType(0, "random", DataType.Number, true);
            DynValue n = args.AsType(1, "random", DataType.Number, true);
            Random r = GetRandom(executionContext.GetScript());
            double d;

            if (m.IsNil() && n.IsNil())
            {
                d = r.NextDouble();
            }
            else
            {
                int a = n.IsNil() ? 1 : (int)n.Number;
                int b = (int)m.Number;

                if (a < b)
                {
                    d = r.Next(a, b + 1);
                }
                else
                {
                    d = r.Next(b, a + 1);
                }
            }

            return DynValue.NewNumber(d);
        }

        [NovaSharpModuleMethod(Name = "randomseed")]
        public static DynValue RandomSeed(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue arg = args.AsType(0, "randomseed", DataType.Number, false);
            Script script = executionContext.GetScript();
            SetRandom(script, new Random((int)arg.Number));
            return DynValue.Nil;
        }

        [NovaSharpModuleMethod(Name = "sin")]
        public static DynValue Sin(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "sin", d => Math.Sin(d));
        }

        [NovaSharpModuleMethod(Name = "sinh")]
        public static DynValue Sinh(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "sinh", d => Math.Sinh(d));
        }

        [NovaSharpModuleMethod(Name = "sqrt")]
        public static DynValue Sqrt(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "sqrt", d => Math.Sqrt(d));
        }

        [NovaSharpModuleMethod(Name = "tan")]
        public static DynValue Tan(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "tan", d => Math.Tan(d));
        }

        [NovaSharpModuleMethod(Name = "tanh")]
        public static DynValue Tanh(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return Exec1(args, "tanh", d => Math.Tanh(d));
        }
    }
}
