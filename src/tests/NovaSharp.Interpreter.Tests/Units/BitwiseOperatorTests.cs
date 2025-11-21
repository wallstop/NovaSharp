namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BitwiseOperatorTests
    {
        [Test]
        public void BitwiseOperatorsRequireLua53Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua52;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return 1 & 1")
            )!;

            Assert.That(exception.Message, Does.Contain("Lua 5.3 manual §3.4.7"));
        }

        [Test]
        public void FloorDivisionRequiresLua53Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua52;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return 5 // 2")
            )!;

            Assert.That(exception.Message, Does.Contain("Lua 5.3 manual §3.4.1"));
        }

        [Test]
        public void BitwiseAndEvaluatesUsingIntegerSemantics()
        {
            DynValue result = Script.RunString("return 0xF0 & 0x0F");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.Zero);
            });
        }

        [Test]
        public void BitwiseOrEvaluatesCorrectly()
        {
            DynValue result = Script.RunString("return 0xF0 | 0x0F");

            Assert.That(result.Number, Is.EqualTo(255));
        }

        [Test]
        public void BitwiseXorEvaluatesCorrectly()
        {
            DynValue result = Script.RunString("return 0xAA ~ 0x55");

            Assert.That(result.Number, Is.EqualTo(0xFF));
        }

        [Test]
        public void ShiftOperatorsFollowLuaSemantics()
        {
            DynValue result = Script.RunString("return 1 << 3, (-8) >> 2, 1 << 64, (-1) >> 70");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(8));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(-2));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(0));
                Assert.That(result.Tuple[3].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void UnaryBitwiseNotProducesTwoComplement()
        {
            DynValue result = Script.RunString("return ~0");

            Assert.That(result.Number, Is.EqualTo(-1));
        }

        [Test]
        public void BitwiseOperatorsAcceptConvertibleStrings()
        {
            DynValue result = Script.RunString("return '3' & '1'");

            Assert.That(result.Number, Is.EqualTo(1));
        }

        [Test]
        public void BitwiseOperatorsRejectNonIntegers()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Script.RunString("return 0.5 & 1")
            )!;

            Assert.That(exception.Message, Does.Contain("bitwise operation on a float value"));
        }

        [Test]
        public void FloorDivisionMatchesLuaSemantics()
        {
            DynValue result = Script.RunString("return -5 // 2, 5 // 2, 5 // -2");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(-3));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(2));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-3));
            });
        }
    }
}
