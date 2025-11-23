namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BinaryMetamethodTests
    {
        [Test]
        public void FloorDivisionMetamethodOverridesOperator()
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

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(13d));
        }

        [Test]
        public void BitwiseNotMetamethodOverridesOperator()
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

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(64d));
        }
    }
}
