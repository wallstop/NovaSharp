namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    public sealed class DebugModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetInfoReturnsFunctionReferenceForLuaFunctions()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function sample() end
                local info = debug.getinfo(sample)
                return info.func == sample
            "
            );

            await Assert.That(result.CastToBool()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoReportsCallerLocation()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function probe()
                    local info = debug.getinfo(1)
                    return info.short_src, info.currentline, info.what
                end
                return probe()
            "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(3);
            await Assert.That(tuple[0].String).IsNotNullOrEmpty();
            await Assert.That(tuple[1].Number).IsGreaterThan(0d);
            await Assert.That(tuple[2].String).IsEqualTo("Lua");
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoReturnsNilWhenLevelExceedsStackDepth()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.getinfo(50)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoSurfacesArgumentErrors()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.getinfo('bad') end)
                return ok, err
            "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert.That(tuple[1].String).Contains("bad argument #1");
        }

        [global::TUnit.Core.Test]
        public async Task GetLocalExposesCurrentLevelArguments()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local name, value = debug.getlocal(0, 1)
                return type(name), value
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
            await Assert.That(tuple[0].String).IsEqualTo("string");
            await Assert.That(tuple[1].Number).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task SetLocalReturnsNameForValidSlot()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local assigned = debug.setlocal(0, 1, 0)
                local missing = debug.setlocal(0, 42, 0)
                return assigned, missing
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].Type).IsEqualTo(DataType.String);
            await Assert.That(tuple[1].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetLocalThrowsWhenLevelOutOfRange()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.getlocal(128, 1) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert.That(tuple[1].String).Contains("level out of range");
        }

        [global::TUnit.Core.Test]
        public async Task SetHookRecordsMaskAndCount()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function hook() end
                debug.sethook(hook, 'c', 42)
                local fn, mask, count = debug.gethook()
                return fn ~= nil, mask, count
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue();
            await Assert.That(tuple[1].String).IsEqualTo("c");
            await Assert.That(tuple[2].Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task SetLocalReportsLevelOutOfRange()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.setlocal(42, 1, true) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert
                .That(tuple[1].String)
                .Contains("bad argument #1 to 'setlocal'")
                .ConfigureAwait(false);
        }
    }
}
