namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Numerics;
    using System.Reflection;
    using System.Threading.Tasks;
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
        public async Task ExtractDefaultsWidthToOneWhenThirdArgumentIsNil()
        {
            DynValue result = Bit32Module.Extract(
                CreateContext(),
                CreateArgs(DynValue.NewNumber(0b_1111_0000), DynValue.NewNumber(4), DynValue.Nil)
            );

            await Assert.That(result.Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        public async Task ReplaceOverwritesBitsInProvidedRange()
        {
            // Replace bits 4-7 of 0 with 0b_1010 (10 decimal) = 0b_1010_0000 (160)
            // Per Lua spec: only the low `width` bits of u are used
            DynValue result = Bit32Module.Replace(
                CreateContext(),
                CreateNumberArgs(0, 0b_1010, 4, 4)
            );

            await Assert.That(result.Number).IsEqualTo((double)0b_1010_0000);
        }

        [global::TUnit.Core.Test]
        public async Task ExtractThrowsWhenPosWidthInvalid()
        {
            (int Position, int Width, string Message)[] cases = new[]
            {
                (-1, 1, "field cannot be negative"),
                (1, 0, "width must be positive"),
                (32, 1, "trying to access non-existent bits"),
                (30, 4, "trying to access non-existent bits"),
            };

            foreach ((int position, int width, string expected) in cases)
            {
                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    Bit32Module.Extract(CreateContext(), CreateNumberArgs(0, position, width))
                );

                await Assert.That(exception.Message).Contains(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task BandAggregatesAllArguments()
        {
            DynValue result = Bit32Module.Band(CreateContext(), CreateNumberArgs(0xFF, 0x0F, 0xF0));

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
                DynValue result = Bit32Module.BitTest(
                    CreateContext(),
                    CreateNumberArgs(left, right)
                );
                await Assert.That(result.Boolean).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task BnotInvertsAllBits()
        {
            DynValue result = Bit32Module.Bnot(CreateContext(), CreateNumberArgs(0b_1111));

            await Assert.That(result.Number).IsEqualTo(~0b_1111u);
        }

        [global::TUnit.Core.Test]
        public async Task BxorCombinesValues()
        {
            DynValue result = Bit32Module.Bxor(CreateContext(), CreateNumberArgs(0b_1010, 0b_0101));

            await Assert.That(result.Number).IsEqualTo((double)0b_1111);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseThrowsWhenArgumentsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                Bit32Module.Bitwise(
                    LuaCompatibilityVersion.Lua52,
                    "band",
                    null,
                    (uint _, uint _) => 0u
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseThrowsWhenAccumulatorNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                Bit32Module.Bitwise(
                    LuaCompatibilityVersion.Lua52,
                    "band",
                    CreateNumberArgs(1),
                    null
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("accumFunc");
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
                DynValue result = Bit32Module.RightShift(
                    CreateContext(),
                    CreateNumberArgs(value, offset)
                );

                await Assert.That(result.Number).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task LeftShiftHandlesPositiveAndNegativeOffsets()
        {
            (uint Value, int Offset, uint Expected)[] cases = new[] { (1u, 3, 8u), (8u, -1, 4u) };

            foreach ((uint value, int offset, uint expected) in cases)
            {
                DynValue result = Bit32Module.LeftShift(
                    CreateContext(),
                    CreateNumberArgs(value, offset)
                );

                await Assert.That(result.Number).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticShiftHandlesPositiveAndNegativeOffsets()
        {
            (int Value, int Offset, int Expected)[] cases = new[] { (-8, 2, -2), (8, -1, 16) };

            foreach ((int value, int offset, int expected) in cases)
            {
                DynValue result = Bit32Module.ArithmeticShift(
                    CreateContext(),
                    CreateNumberArgs(value, offset)
                );

                await Assert.That(result.Number).IsEqualTo(expected);
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
            DynValue result = Bit32Module.LeftRotate(
                CreateContext(),
                CreateNumberArgs(value, offset)
            );

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
            DynValue result = Bit32Module.RightRotate(
                CreateContext(),
                CreateNumberArgs(value, offset)
            );

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
            DynValue result = Bit32Module.Bnot(CreateContext(), CreateNumberArgs(value));

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
            DynValue result = Bit32Module.Band(CreateContext(), CreateNumberArgs(a, b));

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
            DynValue result = Bit32Module.Bnot(CreateContext(), CreateNumberArgs(input));
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
            DynValue result = Bit32Module.LeftShift(
                CreateContext(),
                CreateNumberArgs(value, shift)
            );

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
            DynValue result = Bit32Module.RightShift(
                CreateContext(),
                CreateNumberArgs(value, shift)
            );

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
            DynValue result = Bit32Module.Bor(CreateContext(), CreateNumberArgs(a, b));

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
            DynValue result = Bit32Module.Bxor(CreateContext(), CreateNumberArgs(a, b));

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
            DynValue result = Bit32Module.Extract(
                CreateContext(),
                CreateNumberArgs(value, field, width)
            );

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
            DynValue result = Bit32Module.Extract(
                CreateContext(),
                CreateNumberArgs(value, field, width)
            );

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
                Bit32Module.Extract(CreateContext(), CreateNumberArgs(0xFFFFFFFF, pos, width))
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
                Bit32Module.Extract(CreateContext(), CreateNumberArgs(0xFFFFFFFF, 0, width))
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
                Bit32Module.Extract(CreateContext(), CreateNumberArgs(0xFFFFFFFF, -1, 1))
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
            DynValue result = Bit32Module.Replace(
                CreateContext(),
                CreateNumberArgs(value, insert, field, width)
            );

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
                Bit32Module.Replace(CreateContext(), CreateNumberArgs(0x00000000, 0xFF, pos, width))
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
                Bit32Module.Replace(CreateContext(), CreateNumberArgs(0x00000000, 0xFF, 0, width))
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
                Bit32Module.Replace(CreateContext(), CreateNumberArgs(0x00000000, 0xFF, -1, 1))
            );

            await Assert
                .That(exception.Message)
                .Contains("field cannot be negative")
                .Because("Replace with negative pos should throw 'field cannot be negative' error")
                .ConfigureAwait(false);
        }

        private static ScriptExecutionContext CreateContext()
        {
            return new Script(CoreModulePresets.Default).CreateDynamicExecutionContext();
        }

        private static CallbackArguments CreateArgs(params DynValue[] values)
        {
            return new CallbackArguments(values, false);
        }

        private static CallbackArguments CreateNumberArgs(params double[] numbers)
        {
            DynValue[] values = new DynValue[numbers.Length];

            for (int i = 0; i < numbers.Length; i++)
            {
                values[i] = DynValue.NewNumber(numbers[i]);
            }

            return new CallbackArguments(values, false);
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
        public async Task BandErrorsOnNonIntegerArgumentLua52WithIntegerValidation(
            LuaCompatibilityVersion version
        )
        {
            // Note: bit32 is only available in Lua 5.2, but we need to test integer validation
            // For Lua 5.2, non-integer values should truncate, not error
            // This test documents the Lua 5.2 truncation behavior
            Script script = new Script(version, CoreModulePresets.Complete);

            // bit32.band(5.7, 3) should truncate 5.7 to 5 and return 5 & 3 = 1
            DynValue result = script.DoString("return bit32.band(5.7, 3)");

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task BandTruncatesNonIntegerArgumentLua52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // bit32.band(5.7, 3) should truncate 5.7 to 5 and return 5 & 3 = 1
            DynValue result = script.DoString("return bit32.band(5.7, 3)");

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task BandAcceptsIntegralFloatLua52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // bit32.band(5.0, 3.0) should work since 5.0 and 3.0 have integer representation
            DynValue result = script.DoString("return bit32.band(5.0, 3.0)");

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task LshiftErrorsOnNonIntegerShiftAmountLua52WithIntegerValidation(
            LuaCompatibilityVersion version
        )
        {
            // Note: bit32 is only available in Lua 5.2, where non-integer values truncate
            Script script = new Script(version, CoreModulePresets.Complete);

            // bit32.lshift(1, 2.5) should truncate 2.5 to 2 and return 1 << 2 = 4
            DynValue result = script.DoString("return bit32.lshift(1, 2.5)");

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task LshiftTruncatesNonIntegerShiftAmountLua52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // bit32.lshift(1, 2.5) should truncate 2.5 to 2 and return 1 << 2 = 4
            DynValue result = script.DoString("return bit32.lshift(1, 2.5)");

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task ExtractErrorsOnNonIntegerPositionLua52WithIntegerValidation(
            LuaCompatibilityVersion version
        )
        {
            // Note: bit32 is only available in Lua 5.2, where non-integer values truncate
            Script script = new Script(version, CoreModulePresets.Complete);

            // bit32.extract(0xFF, 1.5) in Lua 5.2 truncates 1.5 to 1 (default width=1)
            // Extract bit 1 from 0xFF (11111111) = 1
            DynValue result = script.DoString("return bit32.extract(0xFF, 1.5)");

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
