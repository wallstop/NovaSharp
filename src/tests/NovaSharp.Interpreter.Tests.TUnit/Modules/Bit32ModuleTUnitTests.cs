namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Numerics;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;

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
            DynValue result = Bit32Module.Replace(
                CreateContext(),
                CreateNumberArgs(0, 0b_1010_0000, 4, 4)
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
                Bit32Module.Bitwise("band", null, (uint _, uint _) => 0u)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseThrowsWhenAccumulatorNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                Bit32Module.Bitwise("band", CreateNumberArgs(1), null)
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

        [global::TUnit.Core.Test]
        public async Task LeftRotateMatchesBitOperationsRotateLeft()
        {
            (uint Value, int Offset)[] cases = new[] { (0x12345678u, 4), (0x12345678u, -8) };

            foreach ((uint value, int offset) in cases)
            {
                uint expected = BitOperations.RotateLeft(value, offset);
                DynValue result = Bit32Module.LeftRotate(
                    CreateContext(),
                    CreateNumberArgs(value, offset)
                );

                await Assert.That(Convert.ToUInt32(result.Number)).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task RightRotateMatchesBitOperationsRotateRight()
        {
            (uint Value, int Offset)[] cases = new[] { (0x89ABCDEFu, 5), (0x89ABCDEFu, -7) };

            foreach ((uint value, int offset) in cases)
            {
                uint expected = BitOperations.RotateRight(value, offset);
                DynValue result = Bit32Module.RightRotate(
                    CreateContext(),
                    CreateNumberArgs(value, offset)
                );

                await Assert.That(Convert.ToUInt32(result.Number)).IsEqualTo(expected);
            }
        }

        private static ScriptExecutionContext CreateContext()
        {
            return new Script(CoreModules.PresetDefault).CreateDynamicExecutionContext();
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
    }
}
