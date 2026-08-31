namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Numerics;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class Bit32ModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RegisteredBit32CallbacksUseArgumentViews()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52);
            Table bit32 = script.Globals.Get("bit32").Table;
            string[] callbackNames =
            {
                "arshift",
                "band",
                "bnot",
                "bor",
                "btest",
                "bxor",
                "extract",
                "lrotate",
                "lshift",
                "replace",
                "rrotate",
                "rshift",
            };

            for (int i = 0; i < callbackNames.Length; i++)
            {
                string callbackName = callbackNames[i];
                CallbackFunction callback = bit32.Get(callbackName).Callback;
                await Assert
                    .That(callback.HasArgumentViewCallback)
                    .IsTrue()
                    .Because($"bit32.{callbackName} should use stack-only arguments")
                    .ConfigureAwait(false);
            }

            MethodInfo[] moduleMethods = typeof(Bit32Module).GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );
            int attributedMethodCount = 0;
            for (int i = 0; i < moduleMethods.Length; i++)
            {
                MethodInfo method = moduleMethods[i];
                if (method.GetCustomAttribute<NovaSharpModuleMethodAttribute>() is null)
                {
                    continue;
                }

                attributedMethodCount++;
                await Assert
                    .That(method.IsPrivate)
                    .IsTrue()
                    .Because($"{method.Name} should not expose a host API")
                    .ConfigureAwait(false);

                ParameterInfo[] parameters = method.GetParameters();
                await Assert
                    .That(parameters.Length)
                    .IsEqualTo(2)
                    .Because($"{method.Name} should have one context and one argument view")
                    .ConfigureAwait(false);
                await Assert
                    .That(parameters[0].ParameterType)
                    .IsEqualTo(typeof(ScriptExecutionContext))
                    .ConfigureAwait(false);
                await Assert
                    .That(parameters[1].ParameterType)
                    .IsEqualTo(typeof(CallbackArgumentsView))
                    .ConfigureAwait(false);
            }

            await Assert
                .That(attributedMethodCount)
                .IsEqualTo(callbackNames.Length)
                .ConfigureAwait(false);
            await Assert
                .That(
                    typeof(Bit32Module).GetMethod(
                        "Bitwise",
                        BindingFlags.Public | BindingFlags.Static
                    )
                        is null
                )
                .IsTrue()
                .Because("the superseded host helper should be removed")
                .ConfigureAwait(false);

            script.DoString(
                @"
local expected_exports = {
    arshift = true,
    band = true,
    bnot = true,
    bor = true,
    btest = true,
    bxor = true,
    extract = true,
    lrotate = true,
    lshift = true,
    replace = true,
    rrotate = true,
    rshift = true,
}
local export_count = 0
for name, value in pairs(bit32) do
    assert(expected_exports[name], ""unexpected bit32 export: "" .. tostring(name))
    assert(type(value) == ""function"")
    export_count = export_count + 1
end
assert(export_count == 12)
assert(bit32.band() == 0xffffffff)
assert(bit32.bor() == 0)
assert(bit32.bxor() == 0)
assert(bit32.btest() == true)
assert(bit32.band(5.7, 3) == 2)
assert(bit32.band(-1.5, 0xffffffff) == 0xfffffffe)
assert(bit32.band(2^51 + 1, 0xffffffff) == 0)
assert(bit32.band(-(2^51 + 1), 0xffffffff) == 0xfffffffe)
assert(bit32.band(2^53 + 2, 0xffffffff) == 1)
assert(bit32.band(-(2^53 + 2), 0xffffffff) == 4)
assert(bit32.band(2^63 - 1024, 0xffffffff) == 0)
assert(bit32.band(-(2^63 - 1024), 0xffffffff) == 0xffffffff)
assert(bit32.band(1e20, 0xffffffff) == 2025163840)
assert(bit32.band(-1e20, 0xffffffff) == 2025163840)
assert(bit32.band(""1e20"", 0xffffffff) == 2025163840)
assert(bit32.band(9.007199254740995e15, 0xffffffff) == 2)
assert(bit32.btest(9.007199254740995e15, 4) == false)
assert(bit32.bnot(9.007199254740995e15) == 0xfffffffd)
assert(bit32.lshift(9.007199254740995e15, 0) == 2)
assert(bit32.extract(9.007199254740995e15, 1, 1) == 1)
assert(bit32.replace(0, 9.007199254740995e15, 0, 32) == 2)
assert(bit32.band(1.7976931348623157e308, 0xffffffff) == 0xffffffff)
assert(bit32.band(-1.7976931348623157e308, 0xffffffff) == 0xffffffff)
assert(bit32.band(5e-324, 0xffffffff) == 0)
assert(bit32.band(-5e-324, 0xffffffff) == 0)
assert(bit32.band(math.huge, 0xffffffff) == 0)
assert(bit32.band(-math.huge, 0xffffffff) == 0)
assert(bit32.band(0 / 0, 0xffffffff) == 0)
assert(bit32.lshift(1, 32) == 0)
assert(bit32.rshift(0xffffffff, 32) == 0)
assert(bit32.arshift(0x80000000, 32) == 0xffffffff)
assert(bit32.arshift(1, 32) == 0)
assert(bit32.rshift(8, -1.7) == 16)
assert(bit32.lrotate(1, -1.7) == 0x80000000)
assert(bit32.extract(1, -0.5) == 1)
assert(bit32.replace(0, 1, -0.5) == 1)
assert(bit32.extract(0xf0, 4, 4) == 15)
assert(bit32.extract(0xf0, 4, nil) == 1)
assert(bit32.replace(0, 1, 3, nil) == 8)
assert(bit32.extract(0xf0, 4, ""4"") == 15)
assert(bit32.replace(0, 15, 4, ""4"") == 240)
local extreme_value = 0x89abcdef
local below_int64_limit = 2^63 - 1024
assert(bit32.lshift(extreme_value, below_int64_limit) == 0)
assert(bit32.rshift(extreme_value, below_int64_limit) == 0)
assert(bit32.arshift(extreme_value, below_int64_limit) == 0)
assert(bit32.lrotate(extreme_value, below_int64_limit) == extreme_value)
assert(bit32.rrotate(extreme_value, below_int64_limit) == extreme_value)
-- Lua 5.2 leaves displacements outside (-2^51, 2^51) unspecified, and
-- reference builds differ across architectures. The exact narrowing matrix
-- is covered by the NovaSharp C# regression test instead.
local above_double_precision = 9007199254740993
assert(bit32.band(above_double_precision, 0xffffffff) == 0)
assert(bit32.extract(1, above_double_precision) == 1)
assert(bit32.replace(0, 1, above_double_precision) == 1)
local ok_large_width, large_width_error = pcall(function()
    bit32.extract(1, 0, above_double_precision)
end)
assert(not ok_large_width and string.find(large_width_error, ""width must be positive"", 1, true))
local ok_field, field_error = pcall(function() bit32.extract(0, -1, 34) end)
assert(not ok_field and string.find(field_error, ""field cannot be negative"", 1, true))
local ok_width, width_error = pcall(function() bit32.replace(0, 1, 32, 0) end)
assert(not ok_width and string.find(width_error, ""width must be positive"", 1, true))
local ok_nan_width, nan_width_error = pcall(function() bit32.extract(1, 0, 0 / 0) end)
assert(not ok_nan_width and string.find(nan_width_error, ""width must be positive"", 1, true))
local ok_band, band_error = pcall(function() bit32.band(false) end)
assert(not ok_band and string.find(band_error, ""to 'band'"", 1, true))
local ok_extract_type, extract_type_error = pcall(function() bit32.extract(0, 0, false) end)
assert(not ok_extract_type)
assert(string.find(extract_type_error,
    ""bad argument #3 to 'extract' (number expected, got boolean)"", 1, true))
local ok_replace_type, replace_type_error = pcall(function() bit32.replace(0, 0, 0, false) end)
assert(not ok_replace_type)
assert(string.find(replace_type_error,
    ""bad argument #4 to 'replace' (number expected, got boolean)"", 1, true))
print(""bit32 callback view parity"")
"
            );
        }

        [global::TUnit.Core.Test]
        public async Task ExtractDefaultsWidthToOneWhenThirdArgumentIsNil()
        {
            LuaValue result = Invoke(
                "extract",
                CreateArgs(LuaValue.NewNumber(0b_1111_0000), LuaValue.NewNumber(4), LuaValue.Nil)
            );

            await Assert.That(result.Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        public async Task ReplaceDefaultsWidthToOneWhenFourthArgumentIsNil()
        {
            LuaValue result = Invoke(
                "replace",
                CreateArgs(
                    LuaValue.NewNumber(0),
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(3),
                    LuaValue.Nil
                )
            );

            await Assert.That(result.Number).IsEqualTo(8d);
        }

        [global::TUnit.Core.Test]
        public async Task ReplaceOverwritesBitsInProvidedRange()
        {
            // Replace bits 4-7 of 0 with 0b_1010 (10 decimal) = 0b_1010_0000 (160)
            // Per Lua spec: only the low `width` bits of u are used
            LuaValue result = Invoke("replace", CreateNumberArgs(0, 0b_1010, 4, 4));

            await Assert.That(result.Number).IsEqualTo((double)0b_1010_0000);
        }

        [global::TUnit.Core.Test]
        public async Task ExtractThrowsWhenPosWidthInvalid()
        {
            (int Position, int Width, string Message)[] cases = new[]
            {
                (-1, 1, "field cannot be negative"),
                (-1, 34, "field cannot be negative"),
                (1, 0, "width must be positive"),
                (32, 0, "width must be positive"),
                (32, 1, "trying to access non-existent bits"),
                (30, 4, "trying to access non-existent bits"),
            };

            foreach ((int position, int width, string expected) in cases)
            {
                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    Invoke("extract", CreateNumberArgs(0, position, width))
                );

                await Assert.That(exception.Message).Contains(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task BandAggregatesAllArguments()
        {
            LuaValue result = Invoke("band", CreateNumberArgs(0xFF, 0x0F, 0xF0));

            await Assert.That(result.Number).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task BitTestEvaluatesBitwiseAnd()
        {
            (uint Left, uint Right, bool Expected)[] cases = new[]
            {
                (0xFFu, 0x01u, true),
                (0xF0u, 0x0Fu, false),
            };

            foreach ((uint left, uint right, bool expected) in cases)
            {
                LuaValue result = Invoke("btest", CreateNumberArgs(left, right));
                await Assert.That(result.Boolean).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task BnotInvertsAllBits()
        {
            LuaValue result = Invoke("bnot", CreateNumberArgs(0b_1111));

            await Assert.That(result.Number).IsEqualTo(~0b_1111u);
        }

        [global::TUnit.Core.Test]
        public async Task BxorCombinesValues()
        {
            LuaValue result = Invoke("bxor", CreateNumberArgs(0b_1010, 0b_0101));

            await Assert.That(result.Number).IsEqualTo((double)0b_1111);
        }

        [global::TUnit.Core.Test]
        public async Task NBitMaskHandlesZeroAndNegativeInputs()
        {
            uint resultZero = InvokeNBitMask(0);
            uint resultNegative = InvokeNBitMask(-5);

            await Assert.That(resultZero).IsEqualTo(0u);
            await Assert.That(resultNegative).IsEqualTo(0u);
        }

        [global::TUnit.Core.Test]
        public async Task NBitMaskSaturatesAt32Bits()
        {
            await Assert.That(InvokeNBitMask(64)).IsEqualTo(0xFFFFFFFFu);
        }

        [global::TUnit.Core.Test]
        public async Task RightShiftHandlesPositiveAndNegativeOffsets()
        {
            (uint Value, int Offset, uint Expected)[] cases = new[]
            {
                (0x10u, 2, 0x4u),
                (0x10u, -2, 0x40u),
            };

            foreach ((uint value, int offset, uint expected) in cases)
            {
                LuaValue result = Invoke("rshift", CreateNumberArgs(value, offset));

                await Assert.That(result.Number).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task LeftShiftHandlesPositiveAndNegativeOffsets()
        {
            (uint Value, int Offset, uint Expected)[] cases = new[] { (1u, 3, 8u), (8u, -1, 4u) };

            foreach ((uint value, int offset, uint expected) in cases)
            {
                LuaValue result = Invoke("lshift", CreateNumberArgs(value, offset));

                await Assert.That(result.Number).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticShiftHandlesPositiveAndNegativeOffsets()
        {
            (int Value, int Offset, uint Expected)[] cases = new[]
            {
                (-8, 2, 0xFFFFFFFEu),
                (8, -1, 16u),
            };

            foreach ((int value, int offset, uint expected) in cases)
            {
                LuaValue result = Invoke("arshift", CreateNumberArgs(value, offset));

                await Assert.That(result.Number).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExtremeDisplacementsMatchLua52Narrowing()
        {
            const double value = 0x89ABCDEF;
            const double twoPow63 = 9223372036854775808d;
            (double Displacement, double ShiftExpected)[] cases =
            {
                (twoPow63 - 1024d, 0d),
                (twoPow63, value),
                (18446744073709551616d, value),
                (1e20, value),
                (double.PositiveInfinity, value),
                (-twoPow63, value),
                (-18446744073709551616d, value),
                (double.NegativeInfinity, value),
                (double.NaN, value),
            };

            foreach ((double displacement, double shiftExpected) in cases)
            {
                await Assert
                    .That(Invoke("lshift", CreateNumberArgs(value, displacement)).Number)
                    .IsEqualTo(shiftExpected)
                    .ConfigureAwait(false);
                await Assert
                    .That(Invoke("rshift", CreateNumberArgs(value, displacement)).Number)
                    .IsEqualTo(shiftExpected)
                    .ConfigureAwait(false);
                await Assert
                    .That(Invoke("arshift", CreateNumberArgs(value, displacement)).Number)
                    .IsEqualTo(shiftExpected)
                    .ConfigureAwait(false);
                await Assert
                    .That(Invoke("lrotate", CreateNumberArgs(value, displacement)).Number)
                    .IsEqualTo(value)
                    .ConfigureAwait(false);
                await Assert
                    .That(Invoke("rrotate", CreateNumberArgs(value, displacement)).Number)
                    .IsEqualTo(value)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExtremeOperandsMatchLua52IeeeConversion()
        {
            (double Operand, uint Expected)[] cases =
            {
                (2251799813685249d, 0u),
                (-2251799813685249d, 4294967294u),
                (9007199254740994d, 1u),
                (-9007199254740994d, 4u),
                (9223372036854774784d, 0u),
                (-9223372036854774784d, uint.MaxValue),
                (1e20, 2025163840u),
                (-1e20, 2025163840u),
                (double.MaxValue, uint.MaxValue),
                (-double.MaxValue, uint.MaxValue),
                (double.Epsilon, 0u),
                (-double.Epsilon, 0u),
                (double.PositiveInfinity, 0u),
                (double.NegativeInfinity, 0u),
                (double.NaN, 0u),
            };

            foreach ((double operand, uint expected) in cases)
            {
                await Assert
                    .That(Invoke("band", CreateNumberArgs(operand, uint.MaxValue)).Number)
                    .IsEqualTo((double)expected)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task IntegerSubtypeInputsUseLua52DoublePrecision()
        {
            const long aboveDoublePrecision = 9007199254740993L;
            LuaValue largeInteger = LuaValue.NewInteger(aboveDoublePrecision);

            await Assert
                .That(Invoke("band", largeInteger, LuaValue.NewInteger(uint.MaxValue)).Number)
                .IsEqualTo(0d)
                .ConfigureAwait(false);

            LuaValue nonDegenerateLargeInteger = LuaValue.NewInteger(9007199254740995L);
            await Assert
                .That(
                    Invoke(
                        "band",
                        nonDegenerateLargeInteger,
                        LuaValue.NewInteger(uint.MaxValue)
                    ).Number
                )
                .IsEqualTo(2d)
                .ConfigureAwait(false);
            await Assert
                .That(Invoke("extract", LuaValue.NewInteger(1), largeInteger).Number)
                .IsEqualTo(1d)
                .ConfigureAwait(false);
            await Assert
                .That(
                    Invoke(
                        "replace",
                        LuaValue.NewInteger(0),
                        LuaValue.NewInteger(1),
                        largeInteger
                    ).Number
                )
                .IsEqualTo(1d)
                .ConfigureAwait(false);

            ScriptRuntimeException widthException = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("extract", LuaValue.NewInteger(1), LuaValue.NewInteger(0), largeInteger)
            );
            await Assert
                .That(widthException.Message)
                .Contains("width must be positive")
                .ConfigureAwait(false);

            LuaValue value = LuaValue.NewInteger(0x89ABCDEF);
            LuaValue displacement = LuaValue.NewInteger(long.MaxValue);
            string[] callbacks = { "lshift", "rshift", "arshift", "lrotate", "rrotate" };
            for (int i = 0; i < callbacks.Length; i++)
            {
                await Assert
                    .That(Invoke(callbacks[i], value, displacement).Number)
                    .IsEqualTo(2309737967d)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Data-driven test for bit32.lrotate matching System.Numerics.BitOperations.RotateLeft.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x12345678u, 4, "basic positive rotation")]
        [global::TUnit.Core.Arguments(0x12345678u, -8, "negative rotation (rotate right)")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 0, "zero rotation")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 32, "full rotation")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 64, "double full rotation")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 16, "all ones")]
        [global::TUnit.Core.Arguments(0x00000000u, 7, "all zeros")]
        [global::TUnit.Core.Arguments(0x80000000u, 1, "high bit set")]
        [global::TUnit.Core.Arguments(0x00000001u, 31, "low bit rotate to high")]
        public async Task LeftRotateMatchesBitOperationsRotateLeft(
            uint value,
            int offset,
            string description
        )
        {
            uint expected = BitOperations.RotateLeft(value, offset);
            LuaValue result = Invoke("lrotate", CreateNumberArgs(value, offset));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"LeftRotate(0x{value:X8}, {offset}) [{description}] should be 0x{expected:X8}"
                );
        }

        /// <summary>
        /// Data-driven test for bit32.rrotate matching System.Numerics.BitOperations.RotateRight.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 5, "basic positive rotation")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, -7, "negative rotation (rotate left)")]
        [global::TUnit.Core.Arguments(0x12345678u, 0, "zero rotation")]
        [global::TUnit.Core.Arguments(0x12345678u, 32, "full rotation")]
        [global::TUnit.Core.Arguments(0x12345678u, 64, "double full rotation")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 16, "all ones")]
        [global::TUnit.Core.Arguments(0x00000000u, 7, "all zeros")]
        [global::TUnit.Core.Arguments(0x00000001u, 1, "low bit rotate to high")]
        [global::TUnit.Core.Arguments(0x80000000u, 31, "high bit rotate to low")]
        public async Task RightRotateMatchesBitOperationsRotateRight(
            uint value,
            int offset,
            string description
        )
        {
            uint expected = BitOperations.RotateRight(value, offset);
            LuaValue result = Invoke("rrotate", CreateNumberArgs(value, offset));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"RightRotate(0x{value:X8}, {offset}) [{description}] should be 0x{expected:X8}"
                );
        }

        /// <summary>
        /// Tests that values > 2^31 are correctly converted to uint32 (regression test for IEEERemainder bug).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, "value > 2^31")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, "max uint32")]
        [global::TUnit.Core.Arguments(0x80000000u, "exactly 2^31")]
        [global::TUnit.Core.Arguments(0x7FFFFFFFu, "max int32")]
        [global::TUnit.Core.Arguments(0x00000000u, "zero")]
        [global::TUnit.Core.Arguments(0x00000001u, "one")]
        public async Task BitwiseNotPreservesHighBitValues(uint value, string description)
        {
            // bit32.bnot(x) = 0xFFFFFFFF xor x
            // This exercises the ToUInt32 conversion
            uint expected = ~value;
            LuaValue result = Invoke("bnot", CreateNumberArgs(value));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because($"Bnot(0x{value:X8}) [{description}] should be 0x{expected:X8}");
        }

        /// <summary>
        /// Tests that bit32.band works correctly with values > 2^31 (regression test).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 0xF0F0F0F0u, 0x80A0C0E0u)]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 0x12345678u, 0x12345678u)]
        [global::TUnit.Core.Arguments(0x80000000u, 0x80000000u, 0x80000000u)]
        public async Task BitwiseAndWorksWithHighBitValues(uint a, uint b, uint expected)
        {
            LuaValue result = Invoke("band", CreateNumberArgs(a, b));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because($"Band(0x{a:X8}, 0x{b:X8}) should be 0x{expected:X8}");
        }

        /// <summary>
        /// Tests that negative input values are correctly converted using Lua's modulo semantics.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(-1.0, 0xFFFFFFFFu, "negative one wraps to max uint")]
        [global::TUnit.Core.Arguments(-2.0, 0xFFFFFFFEu, "negative two")]
        [global::TUnit.Core.Arguments(-2147483648.0, 0x80000000u, "min int32 as double")]
        public async Task BitwiseNotHandlesNegativeInputs(
            double input,
            uint expectedInput,
            string description
        )
        {
            // First verify the input converts correctly by checking bnot(bnot(x)) = x
            LuaValue result = Invoke("bnot", CreateNumberArgs(input));
            uint notResult = Convert.ToUInt32(result.Number);
            uint expectedNotResult = ~expectedInput;

            await Assert
                .That(notResult)
                .IsEqualTo(expectedNotResult)
                .Because(
                    $"Bnot({input}) [{description}] should be 0x{expectedNotResult:X8}, input should convert to 0x{expectedInput:X8}"
                );
        }

        /// <summary>
        /// Tests shift operations with values > 2^31 (regression test for ToUInt32 bug).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 4, 0x9ABCDEF0u, "left shift high value")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, -4, 0x089ABCDEu, "negative left shift (right)")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 1, 0xFFFFFFFEu, "left shift all ones")]
        public async Task LeftShiftWorksWithHighBitValues(
            uint value,
            int shift,
            uint expected,
            string description
        )
        {
            LuaValue result = Invoke("lshift", CreateNumberArgs(value, shift));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"LeftShift(0x{value:X8}, {shift}) [{description}] should be 0x{expected:X8}"
                );
        }

        /// <summary>
        /// Tests right shift operations with values > 2^31 (regression test for ToUInt32 bug).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 4, 0x089ABCDEu, "right shift high value")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, -4, 0x9ABCDEF0u, "negative right shift (left)")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 1, 0x7FFFFFFFu, "right shift all ones")]
        public async Task RightShiftWorksWithHighBitValues(
            uint value,
            int shift,
            uint expected,
            string description
        )
        {
            LuaValue result = Invoke("rshift", CreateNumberArgs(value, shift));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"RightShift(0x{value:X8}, {shift}) [{description}] should be 0x{expected:X8}"
                );
        }

        /// <summary>
        /// Tests bor with values > 2^31 (regression test for ToUInt32 bug).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 0x00000000u, 0x89ABCDEFu)]
        [global::TUnit.Core.Arguments(0x80000000u, 0x00000001u, 0x80000001u)]
        [global::TUnit.Core.Arguments(0xF0F0F0F0u, 0x0F0F0F0Fu, 0xFFFFFFFFu)]
        public async Task BitwiseOrWorksWithHighBitValues(uint a, uint b, uint expected)
        {
            LuaValue result = Invoke("bor", CreateNumberArgs(a, b));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because($"Bor(0x{a:X8}, 0x{b:X8}) should be 0x{expected:X8}");
        }

        /// <summary>
        /// Tests bxor with values > 2^31 (regression test for ToUInt32 bug).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 0xFFFFFFFFu, 0x76543210u)]
        [global::TUnit.Core.Arguments(0x80000000u, 0x80000000u, 0x00000000u)]
        [global::TUnit.Core.Arguments(0xAAAAAAAAu, 0x55555555u, 0xFFFFFFFFu)]
        public async Task BitwiseXorWorksWithHighBitValues(uint a, uint b, uint expected)
        {
            LuaValue result = Invoke("bxor", CreateNumberArgs(a, b));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because($"Bxor(0x{a:X8}, 0x{b:X8}) should be 0x{expected:X8}");
        }

        /// <summary>
        /// Tests extract with values > 2^31 (regression test for ToUInt32 bug).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 0, 8, 0xEFu, "extract low byte from high value")]
        [global::TUnit.Core.Arguments(0x89ABCDEFu, 24, 7, 0x09u, "extract partial high byte")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 8, 16, 0xFFFFu, "extract middle word")]
        [global::TUnit.Core.Arguments(0x80000000u, 31, 1, 0x01u, "extract sign bit")]
        public async Task ExtractWorksWithHighBitValues(
            uint value,
            int field,
            int width,
            uint expected,
            string description
        )
        {
            LuaValue result = Invoke("extract", CreateNumberArgs(value, field, width));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"Extract(0x{value:X8}, {field}, {width}) [{description}] should be 0x{expected:X8}"
                );
        }

        /// <summary>
        /// Tests extract at maximum valid boundaries (pos+width=32).
        /// Validates Lua 5.2 spec: pos in [0,31], pos+width in [1,32].
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 31, 1, 0x01u, "pos=31, width=1 (pos+width=32)")]
        [global::TUnit.Core.Arguments(
            0xFFFFFFFFu,
            0,
            32,
            0xFFFFFFFFu,
            "pos=0, width=32 (full word)"
        )]
        [global::TUnit.Core.Arguments(0xFF000000u, 24, 8, 0xFFu, "pos=24, width=8 (pos+width=32)")]
        [global::TUnit.Core.Arguments(
            0x12345678u,
            16,
            16,
            0x1234u,
            "pos=16, width=16 (upper half)"
        )]
        public async Task ExtractWorksAtMaximumBoundary(
            uint value,
            int field,
            int width,
            uint expected,
            string description
        )
        {
            LuaValue result = Invoke("extract", CreateNumberArgs(value, field, width));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"Extract(0x{value:X8}, {field}, {width}) [{description}] should be 0x{expected:X8}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests extract throws for invalid pos or pos+width combinations per Lua 5.2 spec.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(32, 1, "pos > 31")]
        [global::TUnit.Core.Arguments(31, 2, "pos + width > 32")]
        [global::TUnit.Core.Arguments(25, 8, "pos + width = 33")]
        [global::TUnit.Core.Arguments(0, 33, "width > 32")]
        public async Task ExtractThrowsForInvalidPosWidth(int pos, int width, string description)
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("extract", CreateNumberArgs(0xFFFFFFFF, pos, width))
            );

            await Assert
                .That(exception.Message)
                .Contains("non-existent bits")
                .Because($"Extract with {description} should throw 'non-existent bits' error")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests extract throws for width less than or equal to zero per Lua 5.2 spec.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0, "width = 0")]
        [global::TUnit.Core.Arguments(-1, "width < 0")]
        public async Task ExtractThrowsForInvalidWidth(int width, string description)
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("extract", CreateNumberArgs(0xFFFFFFFF, 0, width))
            );

            await Assert
                .That(exception.Message)
                .Contains("width must be positive")
                .Because($"Extract with {description} should throw 'width must be positive' error")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests extract throws for negative field position per Lua 5.2 spec.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task ExtractThrowsForNegativePos()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("extract", CreateNumberArgs(0xFFFFFFFF, -1, 1))
            );

            await Assert
                .That(exception.Message)
                .Contains("field cannot be negative")
                .Because("Extract with negative pos should throw 'field cannot be negative' error")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests replace at maximum valid boundaries (pos+width=32).
        /// Validates Lua 5.2 spec: pos in [0,31], pos+width in [1,32].
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x00000000u, 0x01u, 31, 1, 0x80000000u, "set bit 31")]
        [global::TUnit.Core.Arguments(
            0x00000000u,
            0xFFFFFFFFu,
            0,
            32,
            0xFFFFFFFFu,
            "replace full word"
        )]
        [global::TUnit.Core.Arguments(0x00000000u, 0xFFu, 24, 8, 0xFF000000u, "set high byte")]
        [global::TUnit.Core.Arguments(0xFFFFFFFFu, 0x00u, 24, 8, 0x00FFFFFFu, "clear high byte")]
        public async Task ReplaceWorksAtMaximumBoundary(
            uint value,
            uint insert,
            int field,
            int width,
            uint expected,
            string description
        )
        {
            LuaValue result = Invoke("replace", CreateNumberArgs(value, insert, field, width));

            await Assert
                .That(Convert.ToUInt32(result.Number))
                .IsEqualTo(expected)
                .Because(
                    $"Replace(0x{value:X8}, 0x{insert:X8}, {field}, {width}) [{description}] should be 0x{expected:X8}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests replace throws for invalid pos or pos+width combinations per Lua 5.2 spec.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(32, 1, "pos > 31")]
        [global::TUnit.Core.Arguments(31, 2, "pos + width > 32")]
        [global::TUnit.Core.Arguments(25, 8, "pos + width = 33")]
        public async Task ReplaceThrowsForInvalidPosWidth(int pos, int width, string description)
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("replace", CreateNumberArgs(0x00000000, 0xFF, pos, width))
            );

            await Assert
                .That(exception.Message)
                .Contains("non-existent bits")
                .Because($"Replace with {description} should throw 'non-existent bits' error")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests replace throws for width less than or equal to zero per Lua 5.2 spec.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0, "width = 0")]
        [global::TUnit.Core.Arguments(-1, "width < 0")]
        public async Task ReplaceThrowsForInvalidWidth(int width, string description)
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("replace", CreateNumberArgs(0x00000000, 0xFF, 0, width))
            );

            await Assert
                .That(exception.Message)
                .Contains("width must be positive")
                .Because($"Replace with {description} should throw 'width must be positive' error")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests replace throws for negative field position per Lua 5.2 spec.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task ReplaceThrowsForNegativePos()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Invoke("replace", CreateNumberArgs(0x00000000, 0xFF, -1, 1))
            );

            await Assert
                .That(exception.Message)
                .Contains("field cannot be negative")
                .Because("Replace with negative pos should throw 'field cannot be negative' error")
                .ConfigureAwait(false);
        }

        private static LuaValue Invoke(string callbackName, params LuaValue[] args)
        {
            Script script = CreateScript();
            CallbackFunction callback = script
                .Globals.Get("bit32")
                .Table.Get(callbackName)
                .Callback;
            return callback.InvokeArgumentViewSpan(script, args);
        }

        private static LuaValue[] CreateArgs(params LuaValue[] values)
        {
            return values;
        }

        private static LuaValue[] CreateNumberArgs(params double[] numbers)
        {
            LuaValue[] values = new LuaValue[numbers.Length];

            for (int i = 0; i < numbers.Length; i++)
            {
                values[i] = LuaValue.NewNumber(numbers[i]);
            }

            return values;
        }

        private static uint InvokeNBitMask(int bits)
        {
            MethodInfo method = typeof(Bit32Module).GetMethod(
                "NBitMask",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            return (uint)method.Invoke(null, new object[] { bits })!;
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task BandRoundsFractionalOperandLua52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // The default Lua 5.2 IEEE conversion rounds 5.7 to 6 before applying bit32.
            LuaValue result = script.DoString(
                @"
local result = bit32.band(5.7, 3)
assert(result == 2)
print(""bit32 fractional normalization parity"")
return result
"
            );

            await Assert.That(result.Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task BandAcceptsIntegralFloatLua52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            LuaValue result = script.DoString(
                @"
local result = bit32.band(5.0, 3.0)
assert(result == 1)
print(""bit32 integral-float parity"")
return result
"
            );

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task LshiftTruncatesNonIntegerShiftAmountLua52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            LuaValue result = script.DoString(
                @"
local result = bit32.lshift(1, 2.5)
assert(result == 4)
print(""bit32 fractional displacement parity"")
return result
"
            );

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task ExtractTruncatesNonIntegerPositionLua52(LuaCompatibilityVersion version)
        {
            // Note: bit32 is only available in Lua 5.2, where non-integer values truncate
            Script script = new Script(version, CoreModulePresets.Complete);

            LuaValue result = script.DoString(
                @"
local result = bit32.extract(0xFF, 1.5)
assert(result == 1)
print(""bit32 fractional field parity"")
return result
"
            );

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        private static Script CreateScript(
            LuaCompatibilityVersion version = LuaCompatibilityVersion.Lua52
        )
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
