#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class TailCallTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TailRecursionHandlesThousandsOfFrames()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local function accumulate(n, acc)
                    if n == 0 then
                        return acc
                    end
                    return accumulate(n - 1, acc + 1)
                end

                return accumulate(20000, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(20000);
        }

        [global::TUnit.Core.Test]
        public async Task TailCallPreservesMultipleReturnValues()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local function id(a, b, c)
                    return a, b, c
                end

                local function forward(...)
                    return id(...)
                end

                return forward(1, 2, 3)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(3);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3);
        }
    }
}
#pragma warning restore CA2007
