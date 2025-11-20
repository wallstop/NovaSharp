namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Numerics;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NUnit.Framework;

    [TestFixture]
    public sealed class Bit32ModuleTests
    {
        [Test]
        public void ExtractDefaultsWidthToOneWhenThirdArgumentIsNil()
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(0b_1111_0000), DynValue.NewNumber(4), DynValue.Nil },
                false
            );

            DynValue result = Bit32Module.Extract(null, args);

            Assert.That(result.Number, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceOverwritesBitsInProvidedRange()
        {
            CallbackArguments args = new(
                new[]
                {
                    DynValue.NewNumber(0),
                    DynValue.NewNumber(0b_1010_0000),
                    DynValue.NewNumber(4),
                    DynValue.NewNumber(4),
                },
                false
            );

            DynValue result = Bit32Module.Replace(null, args);

            Assert.That(result.Number, Is.EqualTo(0b_1010_0000));
        }

        [TestCase(-1, 1, "field cannot be negative")]
        [TestCase(1, 0, "width must be positive")]
        [TestCase(32, 1, "trying to access non-existent bits")]
        [TestCase(30, 4, "trying to access non-existent bits")]
        public void ExtractThrowsWhenPosWidthInvalid(
            int position,
            int width,
            string messageFragment
        )
        {
            CallbackArguments args = new(
                new[]
                {
                    DynValue.NewNumber(0),
                    DynValue.NewNumber(position),
                    DynValue.NewNumber(width),
                },
                false
            );

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                Bit32Module.Extract(null, args)
            )!;

            Assert.That(ex.Message, Does.Contain(messageFragment));
        }

        [Test]
        public void BandAggregatesAllArguments()
        {
            CallbackArguments args = new(
                new[]
                {
                    DynValue.NewNumber(0xFF),
                    DynValue.NewNumber(0x0F),
                    DynValue.NewNumber(0xF0),
                },
                false
            );

            DynValue result = Bit32Module.Band(null, args);

            Assert.That(result.Number, Is.EqualTo(0));
        }

        [TestCase(0xFFu, 0x01u, true)]
        [TestCase(0xF0u, 0x0Fu, false)]
        public void BtestEvaluatesBitwiseAnd(uint left, uint right, bool expected)
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(left), DynValue.NewNumber(right) },
                false
            );

            DynValue result = Bit32Module.Btest(null, args);

            Assert.That(result.Boolean, Is.EqualTo(expected));
        }

        [Test]
        public void BnotInvertsAllBits()
        {
            CallbackArguments args = new(new[] { DynValue.NewNumber(0b_1111) }, false);

            DynValue result = Bit32Module.Bnot(null, args);

            Assert.That(result.Number, Is.EqualTo(~0b_1111u));
        }

        [Test]
        public void BxorCombinesValues()
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(0b_1010), DynValue.NewNumber(0b_0101) },
                false
            );

            DynValue result = Bit32Module.Bxor(null, args);

            Assert.That(result.Number, Is.EqualTo(0b_1111));
        }

        [TestCase(0x10u, 2, 0x4u)]
        [TestCase(0x10u, -2, 0x40u)]
        public void RshiftHandlesPositiveAndNegativeOffsets(uint value, int offset, uint expected)
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(value), DynValue.NewNumber(offset) },
                false
            );

            DynValue result = Bit32Module.Rshift(null, args);

            Assert.That(result.Number, Is.EqualTo(expected));
        }

        [TestCase(1u, 3, 8u)]
        [TestCase(8u, -1, 4u)]
        public void LshiftHandlesPositiveAndNegativeOffsets(uint value, int offset, uint expected)
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(value), DynValue.NewNumber(offset) },
                false
            );

            DynValue result = Bit32Module.Lshift(null, args);

            Assert.That(result.Number, Is.EqualTo(expected));
        }

        [TestCase(-8, 2, -2)]
        [TestCase(8, -1, 16)]
        public void ArshiftHandlesPositiveAndNegativeOffsets(int value, int offset, int expected)
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(value), DynValue.NewNumber(offset) },
                false
            );

            DynValue result = Bit32Module.Arshift(null, args);

            Assert.That(result.Number, Is.EqualTo(expected));
        }

        [TestCase(0x12345678u, 4)]
        [TestCase(0x12345678u, -8)]
        public void LrotateMatchesBitOperationsRotateLeft(uint value, int offset)
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(value), DynValue.NewNumber(offset) },
                false
            );

            uint expected = BitOperations.RotateLeft(value, offset);
            DynValue result = Bit32Module.Lrotate(null, args);

            Assert.That(Convert.ToUInt32(result.Number), Is.EqualTo(expected));
        }

        [TestCase(0x89ABCDEFu, 5)]
        [TestCase(0x89ABCDEFu, -7)]
        public void RrotateMatchesBitOperationsRotateRight(uint value, int offset)
        {
            CallbackArguments args = new(
                new[] { DynValue.NewNumber(value), DynValue.NewNumber(offset) },
                false
            );

            uint expected = BitOperations.RotateRight(value, offset);
            DynValue result = Bit32Module.Rrotate(null, args);

            Assert.That(Convert.ToUInt32(result.Number), Is.EqualTo(expected));
        }
    }
}
