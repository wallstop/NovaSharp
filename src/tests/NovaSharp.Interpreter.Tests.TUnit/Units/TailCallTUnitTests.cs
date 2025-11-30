#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

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

        [global::TUnit.Core.Test]
        public async Task RecursiveSumMatchesArithmeticBaseline()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local function recsum(n, partial)
                    if n == 0 then
                        return partial
                    end
                    return recsum(n - 1, partial + n)
                end

                return recsum(10, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(55);
        }

        [global::TUnit.Core.Test]
        public async Task RecursiveSumHandlesVeryDeepTailRecursion()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local function recsum(n, partial)
                    if n == 0 then
                        return partial
                    end
                    return recsum(n - 1, partial + n)
                end

                return recsum(70000, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(2450035000.0);
        }

        [global::TUnit.Core.Test]
        public async Task TailCallRequestPropagatesAcrossClrCallback()
        {
            Script script = new();
            script.Globals.Set(
                "clrtail",
                DynValue.NewCallback(
                    (context, args) =>
                    {
                        DynValue function = script.Globals.Get("getResult");
                        DynValue adjusted = DynValue.NewNumber(args[0].Number / 3);
                        return DynValue.NewTailCallReq(function, adjusted);
                    }
                )
            );

            DynValue result = script.DoString(
                @"
                function getResult(x)
                    return 156 * x
                end

                return clrtail(9)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(468);
        }

        [global::TUnit.Core.Test]
        public async Task BasicModuleToStringConvertsNumbers()
        {
            Script script = new(CoreModules.Basic);
            DynValue result = script.DoString(
                @"
                return tostring(9)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("9");
        }

        [global::TUnit.Core.Test]
        public async Task TostringUsesMetamethodsWhenAvailable()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local target = {}
                local meta = {
                    __tostring = function()
                        return 'ciao'
                    end
                }

                setmetatable(target, meta)
                return tostring(target)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("ciao");
        }
    }
}
#pragma warning restore CA2007
