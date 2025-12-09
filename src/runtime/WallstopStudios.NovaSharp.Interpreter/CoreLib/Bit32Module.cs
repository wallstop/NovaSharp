namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements Lua 5.2's <c>bit32</c> standard library (§6.7) for compatibility profiles that expose it.
    /// </summary>
    [NovaSharpModule(Namespace = "bit32")]
    public static class Bit32Module
    {
        private static readonly uint[] Masks =
        {
            0x1,
            0x3,
            0x7,
            0xF,
            0x1F,
            0x3F,
            0x7F,
            0xFF,
            0x1FF,
            0x3FF,
            0x7FF,
            0xFFF,
            0x1FFF,
            0x3FFF,
            0x7FFF,
            0xFFFF,
            0x1FFFF,
            0x3FFFF,
            0x7FFFF,
            0xFFFFF,
            0x1FFFFF,
            0x3FFFFF,
            0x7FFFFF,
            0xFFFFFF,
            0x1FFFFFF,
            0x3FFFFFF,
            0x7FFFFFF,
            0xFFFFFFF,
            0x1FFFFFFF,
            0x3FFFFFFF,
            0x7FFFFFFF,
            0xFFFFFFFF,
        };

        /// <summary>
        /// The modulus used for 32-bit unsigned integer wrapping (2^32).
        /// </summary>
        private const double Mod32 = 4294967296.0; // 2^32

        /// <summary>
        /// Converts a Lua number to an unsigned 32-bit integer using Lua's conversion semantics.
        /// Per Lua 5.2 bit32 spec, numbers are converted via floor(x) mod 2^32.
        /// </summary>
        /// <remarks>
        /// This method correctly handles values greater than 2^31 (unlike IEEERemainder which
        /// returns values in (-y/2, y/2] and can produce negative results that cast to 0).
        /// </remarks>
        private static uint ToUInt32(DynValue v)
        {
            double d = Math.Floor(v.Number);

            // Handle negative values: Lua's modulo always returns non-negative
            d = d % Mod32;
            if (d < 0)
            {
                d += Mod32;
            }

            return (uint)d;
        }

        /// <summary>
        /// Converts a Lua number to a signed 32-bit integer using Lua's conversion semantics.
        /// </summary>
        private static int ToInt32(DynValue v)
        {
            double d = Math.Floor(v.Number);

            // Handle negative values: Lua's modulo always returns non-negative
            d = d % Mod32;
            if (d < 0)
            {
                d += Mod32;
            }

            // Convert to signed: values >= 2^31 become negative
            if (d >= 2147483648.0) // 2^31
            {
                return (int)(d - Mod32);
            }

            return (int)d;
        }

        private static uint NBitMask(int bits)
        {
            if (bits <= 0)
            {
                return 0;
            }

            if (bits >= 32)
            {
                return Masks[31];
            }

            return Masks[bits - 1];
        }

        /// <summary>
        /// Applies a bitwise accumulator across the supplied arguments using the provided delegate.
        /// </summary>
        /// <param name="funcName">Lua-visible function name (used for diagnostics).</param>
        /// <param name="args">Arguments passed to the Lua helper.</param>
        /// <param name="accumFunc">Accumulator that combines the running value with the next operand.</param>
        /// <returns>The accumulated 32-bit unsigned integer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> or <paramref name="accumFunc"/> is null.</exception>
        public static uint Bitwise(
            string funcName,
            CallbackArguments args,
            Func<uint, uint, uint> accumFunc
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (accumFunc == null)
            {
                throw new ArgumentNullException(nameof(accumFunc));
            }

            uint accum = ToUInt32(args.AsType(0, funcName, DataType.Number, false));

            for (int i = 1; i < args.Count; i++)
            {
                uint vv = ToUInt32(args.AsType(i, funcName, DataType.Number, false));
                accum = accumFunc(accum, vv);
            }

            return accum;
        }

        /// <summary>
        /// Implements <c>bit32.extract</c>, returning a bit-field slice starting at <c>pos</c> with an optional width.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, position, optional width).</param>
        /// <returns>A <see cref="DynValue"/> containing the extracted unsigned integer.</returns>
        [NovaSharpModuleMethod(Name = "extract")]
        public static DynValue Extract(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "extract", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vPos = args.AsType(1, "extract", DataType.Number);
            DynValue vWidth = args.AsType(2, "extract", DataType.Number, true);

            int pos = (int)vPos.Number;
            int width = (vWidth).IsNilOrNan() ? 1 : (int)vWidth.Number;

            ValidatePosWidth("extract", 2, pos, width);

            uint res = (v >> pos) & NBitMask(width);
            return DynValue.NewNumber(res);
        }

        /// <summary>
        /// Implements <c>bit32.replace</c>, injecting bits from <c>u</c> into <c>v</c> starting at <c>pos</c> for the specified width.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, insert, position, optional width).</param>
        /// <returns>A <see cref="DynValue"/> containing the modified unsigned integer.</returns>
        [NovaSharpModuleMethod(Name = "replace")]
        public static DynValue Replace(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "replace", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vU = args.AsType(1, "replace", DataType.Number);
            uint u = ToUInt32(vU);
            DynValue vPos = args.AsType(2, "replace", DataType.Number);
            DynValue vWidth = args.AsType(3, "replace", DataType.Number, true);

            int pos = (int)vPos.Number;
            int width = (vWidth).IsNilOrNan() ? 1 : (int)vWidth.Number;

            ValidatePosWidth("replace", 3, pos, width);

            uint mask = NBitMask(width) << pos;
            v = v & (~mask);
            u = (u & NBitMask(width)) << pos;
            v = v | u;

            return DynValue.NewNumber(v);
        }

        private static void ValidatePosWidth(string func, int argPos, int pos, int width)
        {
            if (pos > 31 || (pos + width) > 32)
            {
                throw new ScriptRuntimeException("trying to access non-existent bits");
            }

            if (pos < 0)
            {
                throw new ScriptRuntimeException(
                    "bad argument #{1} to '{0}' (field cannot be negative)",
                    func,
                    argPos
                );
            }

            if (width <= 0)
            {
                throw new ScriptRuntimeException(
                    "bad argument #{1} to '{0}' (width must be positive)",
                    func,
                    argPos + 1
                );
            }
        }

        /// <summary>
        /// Implements <c>bit32.arshift</c>, performing an arithmetic right/left shift depending on the sign of the offset.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, shift amount).</param>
        /// <returns>The shifted integer wrapped in a <see cref="DynValue"/>.</returns>
        [NovaSharpModuleMethod(Name = "arshift")]
        public static DynValue ArithmeticShift(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "arshift", DataType.Number);
            int v = ToInt32(vV);

            DynValue vA = args.AsType(1, "arshift", DataType.Number);

            int a = (int)vA.Number;

            if (a < 0)
            {
                v = v << -a;
            }
            else
            {
                v = v >> a;
            }

            return DynValue.NewNumber(v);
        }

        /// <summary>
        /// Implements <c>bit32.rshift</c>, performing a logical right shift (or left shift for negative offsets).
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, shift amount).</param>
        /// <returns>The shifted unsigned integer as a <see cref="DynValue"/>.</returns>
        [NovaSharpModuleMethod(Name = "rshift")]
        public static DynValue RightShift(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "rshift", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "rshift", DataType.Number);

            int a = (int)vA.Number;

            if (a < 0)
            {
                v = v << -a;
            }
            else
            {
                v = v >> a;
            }

            return DynValue.NewNumber(v);
        }

        /// <summary>
        /// Implements <c>bit32.lshift</c>, performing a logical left shift (or right shift for negative offsets).
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, shift amount).</param>
        /// <returns>The shifted unsigned integer as a <see cref="DynValue"/>.</returns>
        [NovaSharpModuleMethod(Name = "lshift")]
        public static DynValue LeftShift(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "lshift", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "lshift", DataType.Number);

            int a = (int)vA.Number;

            if (a < 0)
            {
                v = v >> -a;
            }
            else
            {
                v = v << a;
            }

            return DynValue.NewNumber(v);
        }

        /// <summary>
        /// Implements <c>bit32.band</c>, returning the bitwise AND of all arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to combine.</param>
        /// <returns>The AND'd result.</returns>
        [NovaSharpModuleMethod(Name = "band")]
        public static DynValue Band(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.NewNumber(Bitwise("band", args, (x, y) => x & y));
        }

        /// <summary>
        /// Implements <c>bit32.btest</c>, returning true when the bitwise AND of all operands is non-zero.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to test.</param>
        /// <returns><c>true</c> when any bit overlaps; otherwise <c>false</c>.</returns>
        [NovaSharpModuleMethod(Name = "btest")]
        public static DynValue BitTest(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.FromBoolean(0 != Bitwise("btest", args, (x, y) => x & y));
        }

        /// <summary>
        /// Implements <c>bit32.bor</c>, returning the bitwise OR of all arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to combine.</param>
        /// <returns>The OR'd result.</returns>
        [NovaSharpModuleMethod(Name = "bor")]
        public static DynValue Bor(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.NewNumber(Bitwise("bor", args, (x, y) => x | y));
        }

        /// <summary>
        /// Implements <c>bit32.bnot</c>, inverting every bit of the supplied value.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (single unsigned integer).</param>
        /// <returns>The ones-complement result.</returns>
        [NovaSharpModuleMethod(Name = "bnot")]
        public static DynValue Bnot(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "bnot", DataType.Number);
            uint v = ToUInt32(vV);
            return DynValue.NewNumber(~v);
        }

        /// <summary>
        /// Implements <c>bit32.bxor</c>, returning the bitwise XOR of all arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to combine.</param>
        /// <returns>The XOR'd result.</returns>
        [NovaSharpModuleMethod(Name = "bxor")]
        public static DynValue Bxor(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.NewNumber(Bitwise("bxor", args, (x, y) => x ^ y));
        }

        /// <summary>
        /// Implements <c>bit32.lrotate</c>, rotating a 32-bit value left by the provided amount.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, rotation amount).</param>
        /// <returns>The rotated unsigned integer.</returns>
        [NovaSharpModuleMethod(Name = "lrotate")]
        public static DynValue LeftRotate(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "lrotate", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "lrotate", DataType.Number);

            int a = ((int)vA.Number) % 32;

            if (a < 0)
            {
                v = (v >> (-a)) | (v << (32 + a));
            }
            else
            {
                v = (v << a) | (v >> (32 - a));
            }

            return DynValue.NewNumber(v);
        }

        /// <summary>
        /// Implements <c>bit32.rrotate</c>, rotating a 32-bit value right by the provided amount.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, rotation amount).</param>
        /// <returns>The rotated unsigned integer.</returns>
        [NovaSharpModuleMethod(Name = "rrotate")]
        public static DynValue RightRotate(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vV = args.AsType(0, "rrotate", DataType.Number);
            uint v = ToUInt32(vV);

            DynValue vA = args.AsType(1, "rrotate", DataType.Number);

            int a = ((int)vA.Number) % 32;

            if (a < 0)
            {
                v = (v << (-a)) | (v >> (32 + a));
            }
            else
            {
                v = (v >> a) | (v << (32 - a));
            }

            return DynValue.NewNumber(v);
        }
    }
}
