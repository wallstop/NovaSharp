namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

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

        // Cached static delegates to avoid allocations in hot paths (Initiative 12 Phase 4)
        private static readonly Func<uint, uint, uint> BitAndOp = (x, y) => x & y;
        private static readonly Func<uint, uint, uint> BitOrOp = (x, y) => x | y;
        private static readonly Func<uint, uint, uint> BitXorOp = (x, y) => x ^ y;

        /// <summary>
        /// The modulus used for 32-bit unsigned integer wrapping (2^32).
        /// </summary>
        private const double Mod32 = 4294967296.0; // 2^32

        /// <summary>
        /// Bias used by Lua 5.2's default <c>LUA_IEEE754TRICK</c> conversion.
        /// </summary>
        private const double Lua52UnsignedBias = 6755399441055744.0; // 1.5 * 2^52

        /// <summary>
        /// Validates and normalizes a Lua number to the unsigned 32-bit representation used by
        /// Lua 5.2's default IEEE build.
        /// </summary>
        private static uint ToUInt32(
            LuaCompatibilityVersion version,
            LuaValue v,
            string functionName,
            int argIndex
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);

            // Lua 5.3+: require exact integer representation
            if (resolved >= LuaCompatibilityVersion.Lua53)
            {
                LuaNumberHelpers.RequireIntegerRepresentation(v, functionName, argIndex);
            }

            LuaNumber luaNum = v.LuaNumber;
            if (resolved >= LuaCompatibilityVersion.Lua53 && luaNum.IsInteger)
            {
                return unchecked((uint)luaNum.AsInteger);
            }

            if (resolved < LuaCompatibilityVersion.Lua53)
            {
                // Lua 5.2's default IEEE build adds this bias and extracts the low 32 bits of
                // the resulting double. Besides providing nearest-even conversion in the
                // documented operand range, that exact operation defines the reference build's
                // observable behavior for finite extremes. Always pass NovaSharp's internal
                // integer subtype through double because Lua 5.2 has only one numeric type.
                double biased = luaNum.AsFloat + Lua52UnsignedBias;
                return unchecked((uint)BitConverter.DoubleToInt64Bits(biased));
            }

            double normalized = Math.Round(luaNum.AsFloat, MidpointRounding.ToEven) % Mod32;
            if (normalized < 0)
            {
                normalized += Mod32;
            }

            return (uint)normalized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToCInt(LuaValue value)
        {
            // Lua 5.2 has one double-based number type, even when NovaSharp can retain a more
            // precise integer internally for later compatibility profiles.
            double floating = value.LuaNumber.AsFloat;
            const double Int64UpperBound = 9223372036854775808.0;
            if (
                double.IsNaN(floating)
                || floating < -Int64UpperBound
                || floating >= Int64UpperBound
            )
            {
                // Lua 5.2's default conversion narrows through lua_Integer. Values outside its
                // range become INT64_MIN in the reference build and then narrow to C int zero.
                // Make that observable result deterministic across .NET runtimes and Unity CPUs.
                return 0;
            }

            return unchecked((int)(long)floating);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private static uint Reduce(
            LuaCompatibilityVersion version,
            string funcName,
            CallbackArgumentsView args,
            uint identity,
            Func<uint, uint, uint> accumFunc
        )
        {
            uint accum = identity;
            for (int i = 0; i < args.Count; i++)
            {
                LuaValue arg = args.AsType(i, funcName, DataType.Number, false);
                uint vv = ToUInt32(version, arg, funcName, i + 1);
                accum = accumFunc(accum, vv);
            }

            return accum;
        }

        /// <summary>
        /// Implements <c>bit32.extract</c>, returning a bit-field slice starting at <c>pos</c> with an optional width.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, position, optional width).</param>
        /// <returns>A <see cref="LuaValue"/> containing the extracted unsigned integer.</returns>
        [NovaSharpModuleMethod("extract")]
        private static LuaValue Extract(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "extract", DataType.Number);
            uint v = ToUInt32(version, vV, "extract", 1);

            LuaValue vPos = args.AsType(1, "extract", DataType.Number);
            LuaValue vWidth = args[2];
            if (!vWidth.IsNil)
            {
                vWidth = args.AsType(2, "extract", DataType.Number);
            }

            // Validate position and width (Lua 5.3+ requires integer representation)
            LuaNumberHelpers.ValidateIntegerArgument(version, vPos, "extract", 2);
            LuaNumberHelpers.ValidateIntegerArgument(version, vWidth, "extract", 3);

            int pos = ToCInt(vPos);

            int width = vWidth.IsNil ? 1 : ToCInt(vWidth);

            ValidatePosWidth("extract", 2, pos, width);

            uint res = (v >> pos) & NBitMask(width);
            return LuaValue.NewNumber(res);
        }

        /// <summary>
        /// Implements <c>bit32.replace</c>, injecting bits from <c>u</c> into <c>v</c> starting at <c>pos</c> for the specified width.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, insert, position, optional width).</param>
        /// <returns>A <see cref="LuaValue"/> containing the modified unsigned integer.</returns>
        [NovaSharpModuleMethod("replace")]
        private static LuaValue Replace(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "replace", DataType.Number);
            uint v = ToUInt32(version, vV, "replace", 1);

            LuaValue vU = args.AsType(1, "replace", DataType.Number);
            uint u = ToUInt32(version, vU, "replace", 2);

            LuaValue vPos = args.AsType(2, "replace", DataType.Number);
            LuaValue vWidth = args[3];
            if (!vWidth.IsNil)
            {
                vWidth = args.AsType(3, "replace", DataType.Number);
            }

            // Validate position and width (Lua 5.3+ requires integer representation)
            LuaNumberHelpers.ValidateIntegerArgument(version, vPos, "replace", 3);
            LuaNumberHelpers.ValidateIntegerArgument(version, vWidth, "replace", 4);

            int pos = ToCInt(vPos);

            int width = vWidth.IsNil ? 1 : ToCInt(vWidth);

            ValidatePosWidth("replace", 3, pos, width);

            uint mask = NBitMask(width) << pos;
            v = v & (~mask);
            u = (u & NBitMask(width)) << pos;
            v = v | u;

            return LuaValue.NewNumber(v);
        }

        private static void ValidatePosWidth(string func, int argPos, int pos, int width)
        {
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

            if (pos > 31 || width > 32 - pos)
            {
                throw new ScriptRuntimeException("trying to access non-existent bits");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Shift(uint value, long displacement)
        {
            if (displacement <= -32 || displacement >= 32)
            {
                return 0;
            }

            return displacement < 0 ? value >> (int)-displacement : value << (int)displacement;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rotate(uint value, long displacement)
        {
            int bits = (int)(displacement & 31);
            return bits == 0 ? value : (value << bits) | (value >> (32 - bits));
        }

        /// <summary>
        /// Implements <c>bit32.arshift</c>, performing an arithmetic right/left shift depending on the sign of the offset.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, shift amount).</param>
        /// <returns>The shifted integer wrapped in a <see cref="LuaValue"/>.</returns>
        [NovaSharpModuleMethod("arshift")]
        private static LuaValue ArithmeticShift(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "arshift", DataType.Number);
            uint v = ToUInt32(version, vV, "arshift", 1);

            LuaValue vA = args.AsType(1, "arshift", DataType.Number);
            LuaNumberHelpers.ValidateIntegerArgument(version, vA, "arshift", 2);
            int displacement = ToCInt(vA);

            if (displacement < 0 || (v & 0x80000000u) == 0)
            {
                return LuaValue.NewNumber(Shift(v, -(long)displacement));
            }

            if (displacement >= 32)
            {
                return LuaValue.NewNumber(uint.MaxValue);
            }

            uint result = (v >> displacement) | ~(uint.MaxValue >> displacement);
            return LuaValue.NewNumber(result);
        }

        /// <summary>
        /// Implements <c>bit32.rshift</c>, performing a logical right shift (or left shift for negative offsets).
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, shift amount).</param>
        /// <returns>The shifted unsigned integer as a <see cref="LuaValue"/>.</returns>
        [NovaSharpModuleMethod("rshift")]
        private static LuaValue RightShift(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "rshift", DataType.Number);
            uint v = ToUInt32(version, vV, "rshift", 1);

            LuaValue vA = args.AsType(1, "rshift", DataType.Number);
            LuaNumberHelpers.ValidateIntegerArgument(version, vA, "rshift", 2);

            int displacement = ToCInt(vA);
            return LuaValue.NewNumber(Shift(v, -(long)displacement));
        }

        /// <summary>
        /// Implements <c>bit32.lshift</c>, performing a logical left shift (or right shift for negative offsets).
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, shift amount).</param>
        /// <returns>The shifted unsigned integer as a <see cref="LuaValue"/>.</returns>
        [NovaSharpModuleMethod("lshift")]
        private static LuaValue LeftShift(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "lshift", DataType.Number);
            uint v = ToUInt32(version, vV, "lshift", 1);

            LuaValue vA = args.AsType(1, "lshift", DataType.Number);
            LuaNumberHelpers.ValidateIntegerArgument(version, vA, "lshift", 2);

            int displacement = ToCInt(vA);
            return LuaValue.NewNumber(Shift(v, displacement));
        }

        /// <summary>
        /// Implements <c>bit32.band</c>, returning the bitwise AND of all arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to combine.</param>
        /// <returns>The AND'd result.</returns>
        [NovaSharpModuleMethod("band")]
        private static LuaValue Band(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            return LuaValue.NewNumber(
                Reduce(
                    executionContext.Script.CompatibilityVersion,
                    "band",
                    args,
                    uint.MaxValue,
                    BitAndOp
                )
            );
        }

        /// <summary>
        /// Implements <c>bit32.btest</c>, returning true when the bitwise AND of all operands is non-zero.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to test.</param>
        /// <returns><c>true</c> when any bit overlaps; otherwise <c>false</c>.</returns>
        [NovaSharpModuleMethod("btest")]
        private static LuaValue BitTest(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return LuaValue.FromBoolean(
                0
                    != Reduce(
                        executionContext.Script.CompatibilityVersion,
                        "btest",
                        args,
                        uint.MaxValue,
                        BitAndOp
                    )
            );
        }

        /// <summary>
        /// Implements <c>bit32.bor</c>, returning the bitwise OR of all arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to combine.</param>
        /// <returns>The OR'd result.</returns>
        [NovaSharpModuleMethod("bor")]
        private static LuaValue Bor(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return LuaValue.NewNumber(
                Reduce(executionContext.Script.CompatibilityVersion, "bor", args, 0, BitOrOp)
            );
        }

        /// <summary>
        /// Implements <c>bit32.bnot</c>, inverting every bit of the supplied value.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (single unsigned integer).</param>
        /// <returns>The ones-complement result.</returns>
        [NovaSharpModuleMethod("bnot")]
        private static LuaValue Bnot(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            LuaValue vV = args.AsType(0, "bnot", DataType.Number);
            uint v = ToUInt32(version, vV, "bnot", 1);
            return LuaValue.NewNumber(~v);
        }

        /// <summary>
        /// Implements <c>bit32.bxor</c>, returning the bitwise XOR of all arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments to combine.</param>
        /// <returns>The XOR'd result.</returns>
        [NovaSharpModuleMethod("bxor")]
        private static LuaValue Bxor(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            return LuaValue.NewNumber(
                Reduce(executionContext.Script.CompatibilityVersion, "bxor", args, 0, BitXorOp)
            );
        }

        /// <summary>
        /// Implements <c>bit32.lrotate</c>, rotating a 32-bit value left by the provided amount.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, rotation amount).</param>
        /// <returns>The rotated unsigned integer.</returns>
        [NovaSharpModuleMethod("lrotate")]
        private static LuaValue LeftRotate(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "lrotate", DataType.Number);
            uint v = ToUInt32(version, vV, "lrotate", 1);

            LuaValue vA = args.AsType(1, "lrotate", DataType.Number);
            LuaNumberHelpers.ValidateIntegerArgument(version, vA, "lrotate", 2);

            int displacement = ToCInt(vA);
            return LuaValue.NewNumber(Rotate(v, displacement));
        }

        /// <summary>
        /// Implements <c>bit32.rrotate</c>, rotating a 32-bit value right by the provided amount.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value, rotation amount).</param>
        /// <returns>The rotated unsigned integer.</returns>
        [NovaSharpModuleMethod("rrotate")]
        private static LuaValue RightRotate(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            LuaValue vV = args.AsType(0, "rrotate", DataType.Number);
            uint v = ToUInt32(version, vV, "rrotate", 1);

            LuaValue vA = args.AsType(1, "rrotate", DataType.Number);
            LuaNumberHelpers.ValidateIntegerArgument(version, vA, "rrotate", 2);

            int displacement = ToCInt(vA);
            return LuaValue.NewNumber(Rotate(v, -(long)displacement));
        }
    }
}
