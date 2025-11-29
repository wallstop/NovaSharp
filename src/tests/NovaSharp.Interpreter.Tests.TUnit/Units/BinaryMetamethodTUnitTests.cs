#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class BinaryMetamethodTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task FloorDivisionMetamethodOverridesOperator()
        {
            Script script = new();

            DynValue result = script.DoString(
                @"
                local meta = {}
                function meta.__idiv(lhs, rhs)
                    assert(lhs.value == 10)
                    assert(rhs == 3)
                    return lhs.value + rhs
                end

                local operand = setmetatable({ value = 10 }, meta)
                return operand // 3
            "
            );

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(13d);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseNotMetamethodOverridesOperator()
        {
            Script script = new();

            DynValue result = script.DoString(
                @"
                local meta = {}
                function meta.__bnot(value)
                    assert(value.tag == 'payload')
                    return 64
                end

                local operand = setmetatable({ tag = 'payload' }, meta)
                return ~operand
            "
            );

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(64d);
        }
    }
}
#pragma warning restore CA2007
