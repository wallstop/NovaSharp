namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
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

        [global::TUnit.Core.Test]
        public async Task GetUpvalueReturnsNilForClrFunction()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.getupvalue(print, 1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetUpvalueReturnsNilForInvalidIndex()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function f() end
                return debug.getupvalue(f, 999)
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetUpvalueReturnsNilForNegativeIndex()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local x = 10
                local function f() return x end
                return debug.getupvalue(f, -1)
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task UpvalueIdReturnsNilForClrFunction()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.upvalueid(print, 1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task UpvalueIdReturnsNilForInvalidIndex()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function f() end
                return debug.upvalueid(f, 999)
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetUpvalueReturnsNilForClrFunction()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.setupvalue(print, 1, 'test')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetUpvalueReturnsNilForInvalidIndex()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function f() end
                return debug.setupvalue(f, 999, 'test')
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoReturnsNilForNegativeLevel()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.getinfo(-1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetUserValueReturnsNilForNonUserData()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.getuservalue('string')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetUserValueThrowsForNonTableValue()
        {
            Script script = new(CoreModules.PresetComplete);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.setuservalue(ud, 'not a table') end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert.That(tuple[1].String).Contains("table expected");
        }

        [global::TUnit.Core.Test]
        public async Task SetUserValueThrowsWhenNoValueProvided()
        {
            Script script = new(CoreModules.PresetComplete);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.setuservalue(ud) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert.That(tuple[1].String).Contains("bad argument #2");
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableReturnsNilForTypesWithoutMetatable()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.getmetatable(function() end)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetMetatableThrowsWhenNoMetatableProvided()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.setmetatable({}) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert.That(tuple[1].String).Contains("bad argument #2");
        }

        [global::TUnit.Core.Test]
        public async Task SetMetatableThrowsForNonTableMetatable()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.setmetatable({}, 'notatable') end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
            await Assert.That(tuple[1].String).Contains("nil or table expected");
        }

        [global::TUnit.Core.Test]
        public async Task SetMetatableWorksForFunctions()
        {
            // Functions can have metatables in NovaSharp
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local f = function() return 42 end
                local mt = { __call = function() return 'called' end }
                local success = debug.setmetatable(f, mt) ~= nil
                return success
                "
            );

            await Assert.That(result.CastToBool()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TracebackReturnsCallStack()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function inner()
                    return debug.traceback()
                end
                local function outer()
                    return inner()
                end
                return outer()
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).Contains("traceback");
        }

        [global::TUnit.Core.Test]
        public async Task UpvalueJoinExecutesWithoutError()
        {
            // Note: NovaSharp's upvaluejoin implementation has limitations compared to standard Lua.
            // This test verifies the function runs without error, not full Lua semantics.
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local x = 1
                local y = 2
                local function f1() return x end
                local function f2() return y end
                debug.upvaluejoin(f1, 1, f2, 1)
                return f1(), f2()
                "
            );

            // Verify it runs without throwing
            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task UpvalueJoinThrowsForInvalidIndices()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function f() end
                local ok, err = pcall(function() debug.upvaluejoin(f, 999, f, 1) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoReturnsClrInfoForCallbackFunctions()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local info = debug.getinfo(print)
                return info.what, info.source
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].String).IsEqualTo("C");
            await Assert.That(tuple[1].String).IsEqualTo("=[C]");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Instantiated via Lua UserData registration"
        )]
        private sealed class DmTestUserDataClass
        {
            public int Value { get; set; }
        }
    }
}
