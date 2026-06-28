namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class BinaryMetamethodTUnitTests
    {
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorDivisionMetamethodOverridesOperator(LuaCompatibilityVersion version)
        {
            // Floor division (//) is Lua 5.3+
            Script script = new(version);

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

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(13d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task BitwiseNotMetamethodOverridesOperator(LuaCompatibilityVersion version)
        {
            // Bitwise operators are Lua 5.3+
            Script script = new(version);

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

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(64d).ConfigureAwait(false);
        }
    }
}
