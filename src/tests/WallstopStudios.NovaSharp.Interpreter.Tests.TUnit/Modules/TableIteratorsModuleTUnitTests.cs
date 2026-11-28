namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableIteratorsModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisteredTableIteratorsUseArgumentViews(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            foreach (string name in new[] { "next", "pairs", "ipairs" })
            {
                LuaValue callback = script.Globals.Get(name);
                await Assert
                    .That(callback.Type)
                    .IsEqualTo(DataType.ClrFunction)
                    .ConfigureAwait(false);
                await Assert
                    .That(callback.Callback.HasArgumentViewCallback)
                    .IsTrue()
                    .Because($"{name} should use stack-only arguments")
                    .ConfigureAwait(false);
            }

            LuaValue table = LuaValue.NewTable(script);
            LuaValue pairsIterator = script.Call(script.Globals.Get("pairs"), table).Tuple[0];
            LuaValue ipairsIterator = script.Call(script.Globals.Get("ipairs"), table).Tuple[0];
            await Assert
                .That(pairsIterator.Callback.HasArgumentViewCallback)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(ipairsIterator.Callback.HasArgumentViewCallback)
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NextCallsUseStackOnlyArgumentsWithoutWrapperAllocations(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                makeTable = function(n)
                    local t = {}
                    for i = 1, n do t[i] = i end
                    return t
                end
                prober = function(t, n)
                    local key = nil
                    for i = 1, n do
                        key = next(t, key)
                        if key == nil then key = nil end
                    end
                end
                "
            );
            const int callsPerProbe = 1000;
            LuaValue table = script.Call(
                script.Globals.Get("makeTable"),
                LuaValue.FromNumber(callsPerProbe)
            );
            LuaValue prober = script.Globals.Get("prober");
            for (int i = 0; i < 20; i++)
            {
                script.Call(prober, table, LuaValue.FromNumber(callsPerProbe));
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            script.Call(prober, table, LuaValue.FromNumber(callsPerProbe));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            await Assert
                .That(allocated / (double)callsPerProbe)
                .IsLessThan(200.0)
                .Because(
                    $"next should not allocate legacy CallbackArguments wrappers; measured {allocated} B"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NextReturnsKeyValuePairsAndRejectsInvalidKeys(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local values = { first = 1 }
                local key, value = next(values)
                local key2, value2 = next(values, key)
                local invalidOk, invalidError = pcall(next, values, 'missing')
                return key, value, key2, value2, invalidOk, invalidError
                "
            );

            LuaValue[] values = result.Tuple;
            await Assert.That(values[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(values[0].String).IsEqualTo("first").ConfigureAwait(false);
            await Assert.That(values[1].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(values[2].IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(values[3].IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(values[4].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(values[5].String)
                .Contains("invalid key to 'next'")
                .ConfigureAwait(false);
        }
    }
}
