namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TailCallTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailRecursionHandlesThousandsOfFrames(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(20000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailCallPreservesMultipleReturnValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RecursiveSumMatchesArithmeticBaseline(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(55).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RecursiveSumHandlesVeryDeepTailRecursion(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(2450035000.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailCallRequestPropagatesAcrossClrCallback(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(468).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BasicModuleToStringConvertsNumbers(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Basic);
            DynValue result = script.DoString(
                @"
                return tostring(9)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("9").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TostringUsesMetamethodsWhenAvailable(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("ciao").ConfigureAwait(false);
        }
    }
}
