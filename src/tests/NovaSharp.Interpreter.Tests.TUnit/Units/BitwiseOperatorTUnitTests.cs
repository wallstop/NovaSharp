#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    public sealed class BitwiseOperatorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task BitwiseOperatorsRequireLua53Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua52;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return 1 & 1")
            );

            await Assert.That(exception.Message).Contains("Lua 5.3 manual §3.4.7");
        }

        [global::TUnit.Core.Test]
        public async Task FloorDivisionRequiresLua53Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua52;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return 5 // 2")
            );

            await Assert.That(exception.Message).Contains("Lua 5.3 manual §3.4.1");
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseAndEvaluatesUsingIntegerSemantics()
        {
            DynValue result = Script.RunString("return 0xF0 & 0x0F");

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsZero();
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOrEvaluatesCorrectly()
        {
            DynValue result = Script.RunString("return 0xF0 | 0x0F");

            await Assert.That(result.Number).IsEqualTo(255d);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseXorEvaluatesCorrectly()
        {
            DynValue result = Script.RunString("return 0xAA ~ 0x55");

            await Assert.That(result.Number).IsEqualTo(0xFF);
        }

        [global::TUnit.Core.Test]
        public async Task ShiftOperatorsFollowLuaSemantics()
        {
            DynValue result = Script.RunString("return 1 << 3, (-8) >> 2, 1 << 64, (-1) >> 70");

            await Assert.That(result.Tuple[0].Number).IsEqualTo(8d);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(-2d);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(0d);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(-1d);
        }

        [global::TUnit.Core.Test]
        public async Task UnaryBitwiseNotProducesTwoComplement()
        {
            DynValue result = Script.RunString("return ~0");

            await Assert.That(result.Number).IsEqualTo(-1d);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOperatorsAcceptConvertibleStrings()
        {
            DynValue result = Script.RunString("return '3' & '1'");

            await Assert.That(result.Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOperatorsRejectNonIntegers()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Script.RunString("return 0.5 & 1")
            );

            await Assert.That(exception.Message).Contains("bitwise operation on a float value");
        }

        [global::TUnit.Core.Test]
        public async Task FloorDivisionMatchesLuaSemantics()
        {
            DynValue result = Script.RunString("return -5 // 2, 5 // 2, 5 // -2");

            await Assert.That(result.Tuple[0].Number).IsEqualTo(-3d);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2d);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-3d);
        }
    }
}
#pragma warning restore CA2007
