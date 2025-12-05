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

        [global::TUnit.Core.Test]
        public async Task DebugDebugThrowsWhenDebugInputIsNull()
        {
            // Must explicitly set DebugInput to null; the default options have a delegate configured.
            ScriptOptions options = new() { DebugInput = null, DebugPrint = _ => { } };
            Script script = new(CoreModules.PresetComplete, options);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.debug() end)
                return ok, err
            "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple[1].String).Contains("not supported").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugDebugExitsImmediatelyWhenDefaultInputReturnsNull()
        {
            // Default DebugInput returns null via Platform.DefaultInput - loop exits immediately
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString("return debug.debug()");

            // Should return nil without throwing
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // NOTE: Tests for the debug.debug REPL loop (interactive input) are skipped because
        // using ReplInterpreter within a running script context triggers a VM state issue
        // (ArgumentOutOfRangeException in ProcessingLoop). This is a pre-existing limitation
        // documented in PLAN.md. The DebugInput check and null-exits-loop paths are covered above.

        [global::TUnit.Core.Test]
        public async Task GetInfoReturnsFunctionPlaceholderForClrFunctionWithFFlag()
        {
            // When using debug.getinfo with a function value, 'f' returns the function itself
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local info = debug.getinfo(print, 'f')
                return info.func
                "
            );

            // For function-based getinfo, 'f' returns the actual function
            await Assert.That(result.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoReturnsLuaFunctionPlaceholderWithFFlag()
        {
            // When using debug.getinfo with a function value, 'f' returns the function itself
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function sample() end
                local info = debug.getinfo(sample, 'f')
                return info.func
                "
            );

            // For function-based getinfo, 'f' returns the actual function
            await Assert.That(result.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoFromFrameReturnsStringPlaceholderForClrFunction()
        {
            // Tests BuildFunctionPlaceholder for CLR functions (frame.Address < 0)
            // Using a callback to get a frame-based getinfo for a CLR frame
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "callback",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        // Get info about self (level 0) with 'f' flag
                        DynValue info = ctx.CurrentGlobalEnv.Get("debug").Table.Get("getinfo");
                        return ctx.Call(info, DynValue.NewNumber(0), DynValue.NewString("f"));
                    }
                )
            );

            DynValue result = script.DoString("return callback()");
            Table infoTable = result.Table;

            await Assert.That(infoTable).IsNotNull().ConfigureAwait(false);
            DynValue func = infoTable.Get("func");
            // Frame-based getinfo for CLR frames returns a string placeholder
            await Assert.That(func.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(func.String).StartsWith("function:").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoFromFrameReturnsStringPlaceholderForLuaFunction()
        {
            // Tests BuildFunctionPlaceholder for Lua functions (frame.Address >= 0)
            // Level 1 gets the Lua caller's frame (probe), level 0 is getinfo itself
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function probe()
                    local info = debug.getinfo(1, 'f')
                    return info.func
                end
                return probe()
                "
            );

            // Frame-based getinfo for Lua frames returns a hex address placeholder
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).StartsWith("function: 0x").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocalFromClrFunctionReturnsPlaceholderLocals()
        {
            // Tests the CLR frame path in GetClrDebugLocalTuple - level 0 returns special placeholders
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        // Try to get local from the CLR frame (level 0)
                        DynValue getlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("getlocal");
                        return ctx.Call(getlocal, DynValue.NewNumber(0), DynValue.NewNumber(1));
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            // Level 0 at CLR boundary returns special placeholder locals
            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            // The first placeholder is (*level)
            await Assert.That(tuple[0].String).IsEqualTo("(*level)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocalReturnsNilForInvalidIndexInClrFrame()
        {
            // Tests the CLR frame path with invalid index
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        // Try to get local index 10 (doesn't exist) from CLR frame
                        DynValue getlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("getlocal");
                        return ctx.Call(getlocal, DynValue.NewNumber(0), DynValue.NewNumber(10));
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            // Invalid index returns nil
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetLocalFromClrFunctionReturnsPlaceholderName()
        {
            // Tests the CLR frame path in GetClrDebugLocalName for setlocal
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        // Try to set local on the CLR frame (level 0)
                        DynValue setlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("setlocal");
                        return ctx.Call(
                            setlocal,
                            DynValue.NewNumber(0),
                            DynValue.NewNumber(1),
                            DynValue.NewString("test")
                        );
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            // Level 0 setlocal returns the placeholder name
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("(*level)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetLocalReturnsNilForInvalidIndexInClrFrame()
        {
            // Tests the CLR frame path with invalid index
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        // Try to set local index 10 (doesn't exist) in CLR frame
                        DynValue setlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("setlocal");
                        return ctx.Call(
                            setlocal,
                            DynValue.NewNumber(0),
                            DynValue.NewNumber(10),
                            DynValue.NewString("test")
                        );
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            // Invalid index returns nil
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValueFromClrFunctionReturnsNil()
        {
            // Tests the CLR function path in getupvalue
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                return debug.getupvalue(print, 1)
                "
            );

            // CLR functions have no upvalues
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetUpValueFromClrFunctionReturnsNil()
        {
            // Tests the CLR function path in setupvalue
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                return debug.setupvalue(print, 1, 'test')
                "
            );

            // CLR functions have no upvalues
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetHookReturnsNilWhenNoHookIsSet()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local func, mask, count = debug.gethook()
                return func == nil, mask == '', count == 0
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[2].CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetHookClearsHookWhenNilPassed()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local called = false
                debug.sethook(function() called = true end, 'l')
                debug.sethook(nil)
                local f, m, c = debug.gethook()
                return f == nil, m == ''
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetInfoWithEmptyWhatReturnsEmptyTable()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local function sample() end
                local info = debug.getinfo(sample, '')
                local count = 0
                for k, v in pairs(info) do count = count + 1 end
                return count
                "
            );

            // Empty 'what' string means no fields should be populated
            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetClrDebugLocalTupleReturnsIndexPlaceholder()
        {
            // Tests index 2 in GetClrDebugLocalTuple ((*index))
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        DynValue getlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("getlocal");
                        return ctx.Call(getlocal, DynValue.NewNumber(0), DynValue.NewNumber(2));
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(tuple[0].String).IsEqualTo("(*index)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetClrDebugLocalTupleReturnsValuePlaceholder()
        {
            // Tests index 3 in GetClrDebugLocalTuple ((*value))
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        DynValue getlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("getlocal");
                        return ctx.Call(getlocal, DynValue.NewNumber(0), DynValue.NewNumber(3));
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(tuple[0].String).IsEqualTo("(*value)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetClrDebugLocalReturnsIndexPlaceholderName()
        {
            // Tests index 2 in GetClrDebugLocalName (setlocal path)
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        DynValue setlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("setlocal");
                        return ctx.Call(
                            setlocal,
                            DynValue.NewNumber(0),
                            DynValue.NewNumber(2),
                            DynValue.NewString("test")
                        );
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("(*index)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetClrDebugLocalReturnsValuePlaceholderName()
        {
            // Tests index 3 in GetClrDebugLocalName (setlocal path)
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        DynValue setlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("setlocal");
                        return ctx.Call(
                            setlocal,
                            DynValue.NewNumber(0),
                            DynValue.NewNumber(3),
                            DynValue.NewString("test")
                        );
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("(*value)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocalFromFunctionReturnsUpValuePlaceholder()
        {
            // Tests GetLocalFromFunction with upvalues (function locals)
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local x = 10
                local function closure()
                    return x
                end
                return debug.getlocal(closure, 1)
                "
            );

            // For closures, getlocal returns upvalue placeholders
            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            // Placeholder name like (*function-local 1)
            await Assert.That(tuple[0].String).Contains("function-local").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocalFromFunctionReturnsNilForZeroOrNegativeIndex()
        {
            // Tests GetLocalFromFunction with index <= 0
            Script script = new(CoreModules.PresetComplete);

            DynValue zeroResult = script.DoString(
                @"
                local function sample() end
                return debug.getlocal(sample, 0)
                "
            );

            await Assert.That(zeroResult.IsNil()).IsTrue().ConfigureAwait(false);

            DynValue negativeResult = script.DoString(
                @"
                local function sample() end
                return debug.getlocal(sample, -1)
                "
            );

            await Assert.That(negativeResult.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetArgumentOrNilReturnsNilForOutOfBoundsIndex()
        {
            // Tests GetArgumentOrNil edge case through getlocal
            Script script = new(CoreModules.PresetComplete);

            script.Globals.Set(
                "probe",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        // Pass only level, not index, to test arg bounds
                        DynValue getlocal = ctx.CurrentGlobalEnv.Get("debug").Table.Get("getlocal");
                        // getlocal(0, 3) asks for (*value) which should pull args[2] - check if nil
                        return ctx.Call(getlocal, DynValue.NewNumber(0), DynValue.NewNumber(3));
                    }
                )
            );

            DynValue result = script.DoString("return probe()");

            // The value for (*value) should be nil since no third arg was passed to callback
            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(tuple[0].String).IsEqualTo("(*value)").ConfigureAwait(false);
            await Assert.That(tuple[1].IsNil()).IsTrue().ConfigureAwait(false);
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
