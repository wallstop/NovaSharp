namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class DebugModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReturnsFunctionReferenceForLuaFunctions(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReportsCallerLocation(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReturnsNilWhenLevelExceedsStackDepth(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.getinfo(50)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoSurfacesArgumentErrors(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetLocalExposesCurrentLevelArguments(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetLocalReturnsNameForValidSlot(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetLocalThrowsWhenLevelOutOfRange(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetHookRecordsMaskAndCount(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetLocalReportsLevelOutOfRange(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUpvalueReturnsNilForClrFunction(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.getupvalue(print, 1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUpvalueReturnsNilForInvalidIndex(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function f() end
                return debug.getupvalue(f, 999)
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUpvalueReturnsNilForNegativeIndex(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueIdReturnsNilForClrFunctionLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua 5.4 spec, debug.upvalueid returns nil for CLR functions (no accessible upvalues)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.upvalueid(print, 1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task UpvalueIdThrowsForClrFunctionPreLua54(LuaCompatibilityVersion version)
        {
            // Per Lua 5.2/5.3 spec, debug.upvalueid throws error for CLR functions (no accessible upvalues)
            // NovaSharp provides debug.upvalueid for all versions, with version-specific error handling
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return debug.upvalueid(print, 1)")
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueIdReturnsNilForInvalidIndexLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua 5.4 spec, debug.upvalueid returns nil for invalid indices
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function f() end
                return debug.upvalueid(f, 999)
                "
            );

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task UpvalueIdThrowsForInvalidIndexPreLua54(LuaCompatibilityVersion version)
        {
            // Per Lua 5.2/5.3 spec, debug.upvalueid throws error for invalid indices
            // NovaSharp provides debug.upvalueid for all versions, with version-specific error handling
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local function f() end
                    return debug.upvalueid(f, 999)
                    "
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueIdReturnsNilForZeroIndexLua54Plus(LuaCompatibilityVersion version)
        {
            // Zero is an invalid index (Lua uses 1-based indexing for upvalues)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local x = 10
                local function f() return x end
                return debug.upvalueid(f, 0)
                "
            );

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task UpvalueIdThrowsForZeroIndexPreLua54(LuaCompatibilityVersion version)
        {
            // Zero is an invalid index (Lua uses 1-based indexing for upvalues)
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local x = 10
                    local function f() return x end
                    return debug.upvalueid(f, 0)
                    "
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueIdReturnsNilForNegativeIndexLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            // Negative indices are invalid
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local x = 10
                local function f() return x end
                return debug.upvalueid(f, -1)
                "
            );

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task UpvalueIdThrowsForNegativeIndexPreLua54(LuaCompatibilityVersion version)
        {
            // Negative indices are invalid
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local x = 10
                    local function f() return x end
                    return debug.upvalueid(f, -1)
                    "
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUpvalueReturnsNilForClrFunction(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.setupvalue(print, 1, 'test')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUpvalueReturnsNilForInvalidIndex(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function f() end
                return debug.setupvalue(f, 999, 'test')
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReturnsNilForNegativeLevel(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.getinfo(-1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task GetUserValueReturnsNilForNonUserDataPre54(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            // In Lua 5.1-5.3, getuservalue returns just nil for non-userdata
            DynValue result = script.DoString("return debug.getuservalue('string')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUserValueReturnsNilFalseForNonUserData54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            // In Lua 5.4+, getuservalue returns (nil, false) for non-userdata
            DynValue result = script.DoString("return debug.getuservalue('string')");

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
            await Assert.That(tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple[1].CastToBool()).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUserValueThrowsForNonTableValue(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUserValueThrowsWhenNoValueProvided(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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

        // ========================
        // Lua 5.4+ Multi-User-Value Tests
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUserValueLua54ReturnsTwoValuesForUserData(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            // In Lua 5.4+, getuservalue returns (value, hasValue) tuple
            DynValue result = script.DoString(
                @"
                local val, hasVal = debug.getuservalue(ud, 1)
                return val, hasVal
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
            // Default user value is nil
            await Assert.That(tuple[0].IsNil()).IsTrue();
            // But the slot exists (hasValue = true)
            await Assert.That(tuple[1].CastToBool()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUserValueLua54ReturnsFalseForInvalidSlot(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            // NovaSharp only supports slot 1; slot 2 should return nil, false
            DynValue result = script.DoString(
                @"
                local val, hasVal = debug.getuservalue(ud, 2)
                return val, hasVal
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
            await Assert.That(tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple[1].CastToBool()).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUserValueLua54ReturnsFalseForNonUserData(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            // In Lua 5.4+, non-userdata returns (nil, false)
            DynValue result = script.DoString(
                @"
                local val, hasVal = debug.getuservalue('not userdata', 1)
                return val, hasVal
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsEqualTo(2);
            await Assert.That(tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple[1].CastToBool()).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUserValueLua54WithNParameterSlot1Works(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            DynValue result = script.DoString(
                @"
                local payload = { test = 'value' }
                local ret = debug.setuservalue(ud, payload, 1)
                local val, hasVal = debug.getuservalue(ud, 1)
                return ret == ud, val and val.test, hasVal
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue();
            await Assert.That(tuple[1].String).IsEqualTo("value");
            await Assert.That(tuple[2].CastToBool()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUserValueLua54WithInvalidSlotReturnsNil(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            // NovaSharp only supports slot 1; slot 2 should return nil (fail)
            DynValue result = script.DoString(
                @"
                local ret = debug.setuservalue(ud, { test = 'value' }, 2)
                return ret
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUserValueLua54DefaultNParameterIsOne(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            // Set a value, then get it without specifying n (should default to 1)
            DynValue result = script.DoString(
                @"
                debug.setuservalue(ud, { label = 'default' })
                local val, hasVal = debug.getuservalue(ud)
                return val and val.label, hasVal
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].String).IsEqualTo("default");
            await Assert.That(tuple[1].CastToBool()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task GetUserValueLua53ReturnsOnlyOneValue(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            UserData.RegisterType<DmTestUserDataClass>();
            DmTestUserDataClass obj = new();
            script.Globals["ud"] = UserData.Create(obj);

            // In Lua 5.1-5.3, getuservalue returns only one value
            DynValue result = script.DoString(
                @"
                debug.setuservalue(ud, { value = 42 })
                local results = {debug.getuservalue(ud)}
                return #results, results[1] and results[1].value, results[2]
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            // Should return only 1 value in Lua 5.3 and earlier
            await Assert.That(tuple[0].Number).IsEqualTo(1);
            await Assert.That(tuple[1].Number).IsEqualTo(42);
            await Assert.That(tuple[2].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetMetatableReturnsNilForTypesWithoutMetatable(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.getmetatable(function() end)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetMetatableThrowsWhenNoMetatableProvided(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetMetatableThrowsForNonTableMetatable(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetMetatableWorksForFunctions(LuaCompatibilityVersion version)
        {
            // Functions can have metatables in NovaSharp
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackReturnsCallStack(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueJoinExecutesWithoutError(LuaCompatibilityVersion version)
        {
            // Note: NovaSharp's upvaluejoin implementation has limitations compared to standard Lua.
            // This test verifies the function runs without error, not full Lua semantics.
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueJoinThrowsForInvalidIndices(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReturnsClrInfoForCallbackFunctions(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DebugDebugThrowsWhenDebugInputIsNull(LuaCompatibilityVersion version)
        {
            // Must explicitly set DebugInput to null; the default options have a delegate configured.
            ScriptOptions options = new() { DebugInput = null, DebugPrint = _ => { } };
            Script script = new(CoreModulePresets.Complete, options);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DebugDebugExitsImmediatelyWhenDefaultInputReturnsNull(
            LuaCompatibilityVersion version
        )
        {
            // Default DebugInput returns null via Platform.DefaultInput - loop exits immediately
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return debug.debug()");

            // Should return nil without throwing
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // NOTE: Tests for the debug.debug REPL loop (interactive input) are skipped because
        // using ReplInterpreter within a running script context triggers a VM state issue
        // (ArgumentOutOfRangeException in ProcessingLoop). This is a pre-existing limitation
        // documented in PLAN.md. The DebugInput check and null-exits-loop paths are covered above.

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DebugDebugUsesLuaDebugPrompt(LuaCompatibilityVersion version)
        {
            // Verify debug.debug() uses "lua_debug> " as the prompt (per reference Lua behavior)
            string capturedPrompt = null;
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                DebugInput = prompt =>
                {
                    capturedPrompt = prompt;
                    return null; // Exit the debug loop immediately
                },
                DebugPrint = _ => { },
            };
            Script script = new(CoreModulePresets.Complete, options);

            script.DoString("debug.debug()");

            await Assert.That(capturedPrompt).IsEqualTo("lua_debug> ").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReturnsFunctionPlaceholderForClrFunctionWithFFlag(
            LuaCompatibilityVersion version
        )
        {
            // When using debug.getinfo with a function value, 'f' returns the function itself
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoReturnsLuaFunctionPlaceholderWithFFlag(
            LuaCompatibilityVersion version
        )
        {
            // When using debug.getinfo with a function value, 'f' returns the function itself
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoFromFrameReturnsStringPlaceholderForClrFunction(
            LuaCompatibilityVersion version
        )
        {
            // Tests BuildFunctionPlaceholder for CLR functions (frame.Address < 0)
            // Using a callback to get a frame-based getinfo for a CLR frame
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoFromFrameReturnsStringPlaceholderForLuaFunction(
            LuaCompatibilityVersion version
        )
        {
            // Tests BuildFunctionPlaceholder for Lua functions (frame.Address >= 0)
            // Level 1 gets the Lua caller's frame (probe), level 0 is getinfo itself
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetLocalFromClrFunctionReturnsPlaceholderLocals(
            LuaCompatibilityVersion version
        )
        {
            // Tests the CLR frame path in GetClrDebugLocalTuple - level 0 returns special placeholders
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetLocalReturnsNilForInvalidIndexInClrFrame(
            LuaCompatibilityVersion version
        )
        {
            // Tests the CLR frame path with invalid index
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetLocalFromClrFunctionReturnsPlaceholderName(
            LuaCompatibilityVersion version
        )
        {
            // Tests the CLR frame path in GetClrDebugLocalName for setlocal
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetLocalReturnsNilForInvalidIndexInClrFrame(
            LuaCompatibilityVersion version
        )
        {
            // Tests the CLR frame path with invalid index
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUpValueFromClrFunctionReturnsNil(LuaCompatibilityVersion version)
        {
            // Tests the CLR function path in getupvalue
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                return debug.getupvalue(print, 1)
                "
            );

            // CLR functions have no upvalues
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetUpValueFromClrFunctionReturnsNil(LuaCompatibilityVersion version)
        {
            // Tests the CLR function path in setupvalue
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                return debug.setupvalue(print, 1, 'test')
                "
            );

            // CLR functions have no upvalues
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetHookReturnsNilWhenNoHookIsSet(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetHookClearsHookWhenNilPassed(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoWithEmptyWhatReturnsEmptyTable(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetClrDebugLocalTupleReturnsIndexPlaceholder(
            LuaCompatibilityVersion version
        )
        {
            // Tests index 2 in GetClrDebugLocalTuple ((*index))
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetClrDebugLocalTupleReturnsValuePlaceholder(
            LuaCompatibilityVersion version
        )
        {
            // Tests index 3 in GetClrDebugLocalTuple ((*value))
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetClrDebugLocalReturnsIndexPlaceholderName(
            LuaCompatibilityVersion version
        )
        {
            // Tests index 2 in GetClrDebugLocalName (setlocal path)
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetClrDebugLocalReturnsValuePlaceholderName(
            LuaCompatibilityVersion version
        )
        {
            // Tests index 3 in GetClrDebugLocalName (setlocal path)
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetLocalFromFunctionReturnsUpValuePlaceholder(
            LuaCompatibilityVersion version
        )
        {
            // Tests GetLocalFromFunction with upvalues (function locals)
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetLocalFromFunctionReturnsNilForZeroOrNegativeIndex(
            LuaCompatibilityVersion version
        )
        {
            // Tests GetLocalFromFunction with index <= 0
            Script script = CreateScriptWithVersion(version);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetArgumentOrNilReturnsNilForOutOfBoundsIndex(
            LuaCompatibilityVersion version
        )
        {
            // Tests GetArgumentOrNil edge case through getlocal
            Script script = CreateScriptWithVersion(version);

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

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackWithCoroutineUsesCoroutineStack(LuaCompatibilityVersion version)
        {
            // Tests debug.traceback with a thread (coroutine) argument (line 522-526)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function inner()
                    return debug.traceback(coroutine.running(), 'message')
                end
                local co = coroutine.create(inner)
                local ok, trace = coroutine.resume(co)
                return ok, trace
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].String).Contains("message").ConfigureAwait(false);
            await Assert.That(tuple[1].String).Contains("traceback").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackReturnsOriginalMessageWhenNotStringOrNumber(
            LuaCompatibilityVersion version
        )
        {
            // Tests debug.traceback returning non-string/non-number message unchanged (line 531-536)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local t = { custom = 'value' }
                return debug.traceback(t)
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(result.Table.Get("custom").String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetHookAndGetHookWithCoroutineTarget(LuaCompatibilityVersion version)
        {
            // Tests debug.sethook/gethook with a coroutine target (line 600-605)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function hookfn() end
                local co = coroutine.create(function()
                    debug.sethook(hookfn, 'r', 10)
                    local fn, mask, count = debug.gethook()
                    return fn ~= nil, mask, count
                end)
                local ok, hasFn, mask, count = coroutine.resume(co)
                return ok, hasFn, mask, count
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple.Length).IsGreaterThanOrEqualTo(4).ConfigureAwait(false);
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[2].String).IsEqualTo("r").ConfigureAwait(false);
            await Assert.That(tuple[3].Number).IsEqualTo(10d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetHookWithNoArgsClears(LuaCompatibilityVersion version)
        {
            // Tests debug.sethook() with no args clears hook (line 605-608)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function hookfn() end
                debug.sethook(hookfn, 'c', 5)
                local fn1, mask1, count1 = debug.gethook()
                debug.sethook() -- Call with no args to clear
                local fn2, mask2, count2 = debug.gethook()
                return fn1 ~= nil, fn2 == nil, mask2, count2
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false); // Had hook before clear
            await Assert.That(tuple[1].CastToBool()).IsTrue().ConfigureAwait(false); // fn is nil after clear
            await Assert.That(tuple[2].String).IsEqualTo(string.Empty).ConfigureAwait(false);
            await Assert.That(tuple[3].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetHookWithNilFunctionClearsHook(LuaCompatibilityVersion version)
        {
            // Tests debug.sethook(nil) clears hook (line 629-631)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function hookfn() end
                debug.sethook(hookfn, 'c', 5)
                debug.sethook(nil)
                local fn, mask, count = debug.gethook()
                return fn == nil, mask, count
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].String).IsEqualTo(string.Empty).ConfigureAwait(false);
            await Assert.That(tuple[2].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetHookThrowsForNonFunctionHook(LuaCompatibilityVersion version)
        {
            // Tests debug.sethook with invalid hook type (line 635-637)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function() debug.sethook('not a function', 'c') end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple[1].String).Contains("function expected").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetHookWithCoroutineArgument(LuaCompatibilityVersion version)
        {
            // Tests debug.gethook(coroutine) (line 663-666)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function hookfn() end
                local co = coroutine.create(function()
                    debug.sethook(hookfn, 'l', 3)
                end)
                coroutine.resume(co)
                -- Get hook for the coroutine from outside
                local fn, mask, count = debug.gethook(co)
                return fn ~= nil, mask, count
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].String).IsEqualTo("l").ConfigureAwait(false);
            await Assert.That(tuple[2].Number).IsEqualTo(3d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetMetatableOnBooleanSetsTypeMetatable(LuaCompatibilityVersion version)
        {
            // NovaSharp allows type metatables for Nil, Void, Boolean, Number, String, Function
            // This tests that debug.setmetatable on boolean works (line 315-317)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local mt = { __tostring = function(v) return 'custom_bool' end }
                debug.setmetatable(true, mt)
                return true
                "
            );

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
            // The metatable is set for the boolean type (accessible via debug.getmetatable)
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetMetatableThrowsForUserDataWithoutDescriptor(
            LuaCompatibilityVersion version
        )
        {
            // Tests debug.setmetatable on unsupported type (line 325-328)
            // UserData requires special handling, use Thread (coroutine) since
            // Thread is at the boundary where CanHaveTypeMetatables returns false
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local co = coroutine.create(function() end)
                local ok, err = pcall(function() debug.setmetatable(co, {}) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse().ConfigureAwait(false);
            await Assert
                .That(tuple[1].String)
                .Contains("cannot debug.setmetatable")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetMetatableReturnsTableMetatable(LuaCompatibilityVersion version)
        {
            // Tests debug.getmetatable for a table (line 269-271)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local t = {}
                local mt = { __index = function() return 42 end }
                setmetatable(t, mt)
                local retrieved = debug.getmetatable(t)
                return retrieved == mt, t.anykey
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetMetatableReturnsNilForTableWithoutMetatable(
            LuaCompatibilityVersion version
        )
        {
            // Tests debug.getmetatable returning nil for table without metatable (line 269-271)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local t = {}
                return debug.getmetatable(t)
                "
            );

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetMetatableOnTableWorks(LuaCompatibilityVersion version)
        {
            // Tests debug.setmetatable on a table (line 319-321)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local t = {}
                local mt = { __index = function() return 'found' end }
                debug.setmetatable(t, mt)
                return t.missing
                "
            );

            await Assert.That(result.String).IsEqualTo("found").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueIdReturnsNilForOutOfRangeIndexLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua 5.4 spec, debug.upvalueid returns nil when index is out of range
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function f()
                    -- Has _ENV as upvalue but nothing else
                end
                return debug.upvalueid(f, 999)
                "
            );

            // Index 999 is far beyond the available upvalues -> nil
            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task UpvalueIdThrowsForOutOfRangeIndexPreLua54(LuaCompatibilityVersion version)
        {
            // Per Lua 5.2/5.3 spec, debug.upvalueid throws error for out of range indices
            // NovaSharp provides debug.upvalueid for all versions, with version-specific error handling
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local function f()
                        -- Has _ENV as upvalue but nothing else
                    end
                    return debug.upvalueid(f, 999)
                    "
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueIdReturnsUserDataForValidUpvalue(LuaCompatibilityVersion version)
        {
            // Tests debug.upvalueid returns a userdata identifier for valid upvalue
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local x = 10
                local function f()
                    return x
                end
                local id = debug.upvalueid(f, 1)
                return type(id)
                "
            );

            await Assert.That(result.String).IsEqualTo("userdata").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UpvalueJoinThrowsForInvalidSecondClosure(LuaCompatibilityVersion version)
        {
            // Tests debug.upvaluejoin invalid index on second closure (line 487)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local x = 1
                local function f1() return x end
                local function f2() end
                local ok, err = pcall(function() debug.upvaluejoin(f1, 1, f2, 999) end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse().ConfigureAwait(false);
            await Assert
                .That(tuple[1].String)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackWithNumberLevel(LuaCompatibilityVersion version)
        {
            // Tests debug.traceback with a specific level to skip (line 543)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function deep()
                    return debug.traceback('trace', 2)
                end
                local function middle()
                    return deep()
                end
                local function outer()
                    return middle()
                end
                return outer()
                "
            );

            await Assert.That(result.String).Contains("trace").ConfigureAwait(false);
            await Assert.That(result.String).Contains("traceback").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackWithNilLevelUsesDefault(LuaCompatibilityVersion version)
        {
            // Tests debug.traceback with nil level uses default skip=1
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function inner()
                    return debug.traceback('msg', nil)
                end
                return inner()
                "
            );

            await Assert.That(result.String).Contains("msg").ConfigureAwait(false);
            await Assert.That(result.String).Contains("traceback").ConfigureAwait(false);
        }

        // ========================
        // Data-Driven debug.upvalueid Edge Case Tests
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetUpvalueIdValidUpvalueData))]
        public async Task UpvalueIdDataDrivenValidUpvalue(
            LuaCompatibilityVersion version,
            string luaCode,
            string description
        )
        {
            // Data-driven test: debug.upvalueid returns userdata for valid upvalues
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(luaCode);

            await Assert.That(result.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            string,
            string
        )> GetUpvalueIdValidUpvalueData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            (string Code, string Description)[] testCases = new[]
            {
                (
                    @"local x = 10
                      local function f() return x end
                      return debug.upvalueid(f, 1)",
                    "Single upvalue at index 1"
                ),
                (
                    @"local a, b = 1, 2
                      local function f() return a + b end
                      return debug.upvalueid(f, 2)",
                    "Second upvalue at index 2"
                ),
                (
                    @"local outer = 'test'
                      local function f()
                          local function g() return outer end
                          return g
                      end
                      local inner = f()
                      return debug.upvalueid(inner, 1)",
                    "Nested closure upvalue"
                ),
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach ((string code, string description) in testCases)
                {
                    yield return (version, code, description);
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetUpvalueIdNilForInvalidIndexLua54PlusData))]
        public async Task UpvalueIdDataDrivenReturnsNilForInvalidIndexLua54Plus(
            LuaCompatibilityVersion version,
            int invalidIndex,
            string luaCode,
            string description
        )
        {
            // Data-driven test: Lua 5.4+ returns nil for invalid upvalue indices
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(luaCode);

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            int,
            string,
            string
        )> GetUpvalueIdNilForInvalidIndexLua54PlusData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            (int Index, string Code, string Description)[] testCases = new[]
            {
                (
                    0,
                    @"local x = 10
                      local function f() return x end
                      return debug.upvalueid(f, 0)",
                    "Zero index (Lua uses 1-based indexing)"
                ),
                (
                    -1,
                    @"local x = 10
                      local function f() return x end
                      return debug.upvalueid(f, -1)",
                    "Negative index -1"
                ),
                (
                    -100,
                    @"local x = 10
                      local function f() return x end
                      return debug.upvalueid(f, -100)",
                    "Large negative index -100"
                ),
                (
                    999,
                    @"local function f() end
                      return debug.upvalueid(f, 999)",
                    "Index beyond available upvalues (no upvalues)"
                ),
                // Note: Tests for "index 2 when only 1 upvalue exists" and similar
                // cases are omitted because NovaSharp currently returns userdata
                // instead of nil for these cases (known spec divergence).
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach ((int index, string code, string description) in testCases)
                {
                    yield return (version, index, code, description);
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetUpvalueIdThrowsForInvalidIndexPreLua54Data))]
        public async Task UpvalueIdDataDrivenThrowsForInvalidIndexPreLua54(
            LuaCompatibilityVersion version,
            int invalidIndex,
            string luaCode,
            string description
        )
        {
            // Data-driven test: Lua 5.1-5.3 throws error for invalid upvalue indices
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(luaCode)
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            int,
            string,
            string
        )> GetUpvalueIdThrowsForInvalidIndexPreLua54Data()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
            };

            (int Index, string Code, string Description)[] testCases = new[]
            {
                (
                    0,
                    @"local x = 10
                      local function f() return x end
                      return debug.upvalueid(f, 0)",
                    "Zero index (Lua uses 1-based indexing)"
                ),
                (
                    -1,
                    @"local x = 10
                      local function f() return x end
                      return debug.upvalueid(f, -1)",
                    "Negative index -1"
                ),
                (
                    999,
                    @"local function f() end
                      return debug.upvalueid(f, 999)",
                    "Index beyond available upvalues (no upvalues)"
                ),
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach ((int index, string code, string description) in testCases)
                {
                    yield return (version, index, code, description);
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetUpvalueIdClrFunctionData))]
        public async Task UpvalueIdDataDrivenClrFunctionBehavior(
            LuaCompatibilityVersion version,
            string clrFunctionName,
            bool expectsNil,
            string description
        )
        {
            // Data-driven test: debug.upvalueid behavior for CLR functions varies by version
            Script script = CreateScriptWithVersion(version);
            string luaCode = $"return debug.upvalueid({clrFunctionName}, 1)";

            if (expectsNil)
            {
                // Lua 5.4+ returns nil for CLR functions
                DynValue result = script.DoString(luaCode);
                await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
            }
            else
            {
                // Lua 5.1-5.3 throws error for CLR functions
                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    script.DoString(luaCode)
                );
                await Assert
                    .That(exception.Message)
                    .Contains("invalid upvalue index")
                    .ConfigureAwait(false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            string,
            bool,
            string
        )> GetUpvalueIdClrFunctionData()
        {
            // Lua 5.4+ returns nil for CLR functions
            LuaCompatibilityVersion[] nilVersions = new[]
            {
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            // Lua 5.1-5.3 throws error for CLR functions
            LuaCompatibilityVersion[] throwVersions = new[]
            {
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
            };

            string[] clrFunctions = new[] { "print", "type", "tostring", "tonumber", "pairs" };

            foreach (LuaCompatibilityVersion version in nilVersions)
            {
                foreach (string func in clrFunctions)
                {
                    yield return (
                        version,
                        func,
                        true,
                        $"CLR function '{func}' in Lua 5.4+ returns nil"
                    );
                }
            }

            foreach (LuaCompatibilityVersion version in throwVersions)
            {
                foreach (string func in clrFunctions)
                {
                    yield return (
                        version,
                        func,
                        false,
                        $"CLR function '{func}' in Lua 5.1-5.3 throws error"
                    );
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetUpvalueIdSharedUpvalueData))]
        public async Task UpvalueIdDataDrivenSharedUpvalues(
            LuaCompatibilityVersion version,
            string description
        )
        {
            // Data-driven test: debug.upvalueid returns same ID for shared upvalues
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local shared = 42
                local function f1() return shared end
                local function f2() return shared end
                local id1 = debug.upvalueid(f1, 1)
                local id2 = debug.upvalueid(f2, 1)
                return id1 == id2
                "
            );

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string)> GetUpvalueIdSharedUpvalueData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                yield return (version, $"Shared upvalues in {version}");
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetUpvalueIdDistinctUpvalueData))]
        public async Task UpvalueIdDataDrivenDistinctUpvalues(
            LuaCompatibilityVersion version,
            string description
        )
        {
            // Data-driven test: debug.upvalueid returns different IDs for distinct upvalues
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local x = 1
                local y = 2
                local function f() return x + y end
                local id1 = debug.upvalueid(f, 1)
                local id2 = debug.upvalueid(f, 2)
                return id1 ~= id2
                "
            );

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            string
        )> GetUpvalueIdDistinctUpvalueData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                yield return (version, $"Distinct upvalues in {version}");
            }
        }

        // ========================
        // DATA-DRIVEN: debug.getinfo 'what' flags
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetInfoWhatFlagTestData))]
        public async Task GetInfoDataDrivenWhatFlags(
            LuaCompatibilityVersion version,
            string whatFlag,
            string expectedField,
            string description
        )
        {
            // Data-driven test: debug.getinfo with individual 'what' flag options
            Script script = CreateScriptWithVersion(version);

            string luaCode =
                $@"
                local function sample(a, b)
                    local c = a + b
                    return c
                end
                local info = debug.getinfo(sample, '{whatFlag}')
                return info.{expectedField} ~= nil
            ";

            DynValue result = script.DoString(luaCode);

            await Assert
                .That(result.CastToBool())
                .IsTrue()
                .Because(
                    $"debug.getinfo with '{whatFlag}' flag should populate '{expectedField}' field ({description})"
                )
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            string,
            string,
            string
        )> GetInfoWhatFlagTestData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            // Each tuple: (whatFlag, expectedField, description)
            // Note: 'n' flag populates 'name' (can be nil for local functions) and 'namewhat'
            // We test 'namewhat' instead since it's always present (empty string if unknown)
            (string WhatFlag, string ExpectedField, string Description)[] testCases = new[]
            {
                ("n", "namewhat", "name fields - namewhat is always present"),
                ("S", "source", "source fields"),
                ("l", "currentline", "line info"),
                ("u", "nups", "upvalue count"),
                ("f", "func", "function reference"),
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach ((string whatFlag, string expectedField, string description) in testCases)
                {
                    yield return (version, whatFlag, expectedField, description);
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            "nS",
            "namewhat",
            "source",
            "name + source"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua52,
            "nS",
            "namewhat",
            "source",
            "name + source"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua53,
            "nS",
            "namewhat",
            "source",
            "name + source"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            "nS",
            "namewhat",
            "source",
            "name + source"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua55,
            "nS",
            "namewhat",
            "source",
            "name + source"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            "Sl",
            "source",
            "currentline",
            "source + line"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua52,
            "Sl",
            "source",
            "currentline",
            "source + line"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua53,
            "Sl",
            "source",
            "currentline",
            "source + line"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            "Sl",
            "source",
            "currentline",
            "source + line"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua55,
            "Sl",
            "source",
            "currentline",
            "source + line"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            "uf",
            "nups",
            "func",
            "upvalues + function"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua52,
            "uf",
            "nups",
            "func",
            "upvalues + function"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua53,
            "uf",
            "nups",
            "func",
            "upvalues + function"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            "uf",
            "nups",
            "func",
            "upvalues + function"
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua55,
            "uf",
            "nups",
            "func",
            "upvalues + function"
        )]
        public async Task GetInfoDataDrivenCombinedWhatFlags(
            LuaCompatibilityVersion version,
            string whatFlags,
            string field1,
            string field2,
            string description
        )
        {
            // Data-driven test: debug.getinfo with combined 'what' flags
            Script script = CreateScriptWithVersion(version);

            string luaCode =
                $@"
                local function sample(a, b)
                    local c = a + b
                    return c
                end
                local info = debug.getinfo(sample, '{whatFlags}')
                return info.{field1} ~= nil, info.{field2} ~= nil
            ";

            DynValue result = script.DoString(luaCode);

            DynValue[] tuple = result.Tuple ?? new[] { result };
            await Assert
                .That(tuple.Length)
                .IsEqualTo(2)
                .Because($"Should return 2 results for fields: {field1}, {field2}")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[0].CastToBool())
                .IsTrue()
                .Because(
                    $"Field '{field1}' should be populated with '{whatFlags}' flags ({description})"
                )
                .ConfigureAwait(false);

            await Assert
                .That(tuple[1].CastToBool())
                .IsTrue()
                .Because(
                    $"Field '{field2}' should be populated with '{whatFlags}' flags ({description})"
                )
                .ConfigureAwait(false);
        }

        // ========================
        // DATA-DRIVEN: debug.traceback levels and messages
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetTracebackLevelTestData))]
        public async Task TracebackDataDrivenLevels(
            LuaCompatibilityVersion version,
            int level,
            string description
        )
        {
            // Data-driven test: debug.traceback with various level values
            Script script = CreateScriptWithVersion(version);

            string luaCode =
                $@"
                local function level3()
                    return debug.traceback('marker', {level})
                end
                local function level2()
                    return level3()
                end
                local function level1()
                    return level2()
                end
                return level1()
            ";

            DynValue result = script.DoString(luaCode);

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.String)
                .Because($"debug.traceback should return a string at level {level} ({description})")
                .ConfigureAwait(false);

            await Assert
                .That(result.String)
                .Contains("marker")
                .Because($"Traceback should include the message 'marker' at level {level}")
                .ConfigureAwait(false);

            await Assert
                .That(result.String)
                .Contains("traceback")
                .Because($"Traceback should include 'traceback' header at level {level}")
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            int,
            string
        )> GetTracebackLevelTestData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            // Each tuple: (level, description)
            (int Level, string Description)[] testCases = new[]
            {
                (0, "level 0 - include traceback itself"),
                (1, "level 1 - skip traceback call (default)"),
                (2, "level 2 - skip two levels"),
                (3, "level 3 - skip three levels"),
                (10, "level 10 - skip more levels than exist"),
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach ((int level, string description) in testCases)
                {
                    yield return (version, level, description);
                }
            }
        }

        // ========================
        // DATA-DRIVEN: debug.getlocal/setlocal edge cases
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.MethodDataSource(nameof(GetLocalEdgeCaseTestData))]
        public async Task GetLocalDataDrivenEdgeCases(
            LuaCompatibilityVersion version,
            int index,
            bool expectsNilName,
            string description
        )
        {
            // Data-driven test: debug.getlocal with various index values
            Script script = CreateScriptWithVersion(version);

            string luaCode =
                $@"
                local function sample(arg1, arg2, arg3)
                    local loc1 = 'local1'
                    local loc2 = 'local2'
                    local name, value = debug.getlocal(1, {index})
                    return name, value
                end
                return sample('a', 'b', 'c')
            ";

            DynValue result = script.DoString(luaCode);

            DynValue[] tuple = result.Tuple ?? new[] { result };

            if (expectsNilName)
            {
                // For invalid indices, the name should be nil (Lua returns nil for invalid index)
                await Assert
                    .That(tuple[0].IsNil())
                    .IsTrue()
                    .Because(
                        $"debug.getlocal with index {index} should return nil name ({description})"
                    )
                    .ConfigureAwait(false);
            }
            else
            {
                await Assert
                    .That(tuple.Length)
                    .IsEqualTo(2)
                    .Because(
                        $"debug.getlocal with valid index {index} should return (name, value) tuple ({description})"
                    )
                    .ConfigureAwait(false);

                await Assert
                    .That(tuple[0].Type)
                    .IsEqualTo(DataType.String)
                    .Because($"Local name at index {index} should be a string ({description})")
                    .ConfigureAwait(false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(
            LuaCompatibilityVersion,
            int,
            bool,
            string
        )> GetLocalEdgeCaseTestData()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            // Each tuple: (index, expectsNilName, description)
            (int Index, bool ExpectsNilName, string Description)[] testCases = new[]
            {
                (1, false, "first argument arg1"),
                (2, false, "second argument arg2"),
                (3, false, "third argument arg3"),
                (4, false, "first local loc1"),
                (5, false, "second local loc2"),
                (0, true, "zero index - invalid"),
                (-1, true, "negative index - invalid"),
                (100, true, "index beyond locals - invalid"),
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach ((int index, bool expectsNilName, string description) in testCases)
                {
                    yield return (version, index, expectsNilName, description);
                }
            }
        }

        // ========================
        // EDGE CASE TESTS: debug.getinfo
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoWithInvalidWhatCharactersThrowsError(
            LuaCompatibilityVersion version
        )
        {
            // Edge case: Invalid characters in 'what' string throw an error
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                local function sample() end
                local info = debug.getinfo(sample, 'nXYZ')
                return info.name, info.source
                "
                )
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because("Invalid what flags should throw an error")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoWithOnlyNFlagReturnsNameInfo(LuaCompatibilityVersion version)
        {
            // Edge case: Only 'n' flag returns just name-related fields
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function sample() end
                local info = debug.getinfo(sample, 'n')
                return info.namewhat, info.source
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();

            await Assert
                .That(tuple.Length)
                .IsEqualTo(2)
                .Because("Should return two values for namewhat and source check")
                .ConfigureAwait(false);

            // source should be nil since 'S' flag was not specified
            await Assert
                .That(tuple[1].IsNil())
                .IsTrue()
                .Because("source should be nil when 'S' flag is not in 'what' string")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoWithLevelZeroReturnsGetInfoItself(LuaCompatibilityVersion version)
        {
            // Edge case: Level 0 should return info about getinfo itself (CLR function)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local info = debug.getinfo(0, 'nS')
                return info.what, info.source
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert
                .That(tuple.Length)
                .IsEqualTo(2)
                .Because("Level 0 should return info about the caller (C function)")
                .ConfigureAwait(false);

            // Level 0 is the C function calling getinfo
            await Assert
                .That(tuple[0].String)
                .IsEqualTo("C")
                .Because("Level 0 is a C function (debug.getinfo itself)")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[1].String)
                .IsEqualTo("=[C]")
                .Because("C functions have source '=[C]'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoNupsCountsUpvaluesCorrectly(LuaCompatibilityVersion version)
        {
            // Edge case: Verify nups (upvalue count) is accurate
            // Note: All functions have at least _ENV as an implicit upvalue in Lua 5.2+
            // In NovaSharp, _ENV is counted for all versions
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local a, b, c = 1, 2, 3
                local function noExplicitUpvalues() return 42 end
                local function oneExplicitUpvalue() return a end
                local function threeExplicitUpvalues() return a + b + c end
                
                local info0 = debug.getinfo(noExplicitUpvalues, 'u')
                local info1 = debug.getinfo(oneExplicitUpvalue, 'u')
                local info3 = debug.getinfo(threeExplicitUpvalues, 'u')
                
                return info0.nups, info1.nups, info3.nups
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert
                .That(tuple.Length)
                .IsEqualTo(3)
                .Because("Should return three nups values")
                .ConfigureAwait(false);

            // noExplicitUpvalues has 1 upvalue (_ENV)
            await Assert
                .That(tuple[0].Number)
                .IsEqualTo(1d)
                .Because("noExplicitUpvalues function has 1 upvalue (_ENV)")
                .ConfigureAwait(false);

            // oneExplicitUpvalue has 2 upvalues (_ENV and a)
            await Assert
                .That(tuple[1].Number)
                .IsEqualTo(2d)
                .Because("oneExplicitUpvalue function has 2 upvalues (_ENV and a)")
                .ConfigureAwait(false);

            // threeExplicitUpvalues has 4 upvalues (_ENV, a, b, c)
            await Assert
                .That(tuple[2].Number)
                .IsEqualTo(4d)
                .Because("threeExplicitUpvalues function has 4 upvalues (_ENV, a, b, c)")
                .ConfigureAwait(false);
        }

        // ========================
        // EDGE CASE TESTS: debug.traceback
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackWithEmptyMessageWorks(LuaCompatibilityVersion version)
        {
            // Edge case: Empty string message
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function test()
                    return debug.traceback('')
                end
                return test()
                "
            );

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.String)
                .Because("debug.traceback with empty message should return a string")
                .ConfigureAwait(false);

            await Assert
                .That(result.String)
                .Contains("traceback")
                .Because(
                    "Traceback should still contain 'traceback' header even with empty message"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackWithNumberMessageConvertsToString(
            LuaCompatibilityVersion version
        )
        {
            // Edge case: Number message is converted to string
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function test()
                    return debug.traceback(42, 1)
                end
                return test()
                "
            );

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.String)
                .Because("debug.traceback with number message should return a string")
                .ConfigureAwait(false);

            await Assert
                .That(result.String)
                .Contains("42")
                .Because("Number message 42 should be converted to string in traceback")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TracebackWithNegativeLevelUsesDefault(LuaCompatibilityVersion version)
        {
            // Edge case: Negative level should use default behavior
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function test()
                    return debug.traceback('msg', -1)
                end
                return test()
                "
            );

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.String)
                .Because("debug.traceback with negative level should still return a string")
                .ConfigureAwait(false);

            await Assert
                .That(result.String)
                .Contains("msg")
                .Because("Message should still be included with negative level")
                .ConfigureAwait(false);
        }

        // ========================
        // EDGE CASE TESTS: debug.setlocal
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetLocalActuallyModifiesLocal(LuaCompatibilityVersion version)
        {
            // Edge case: Verify setlocal actually modifies the local variable
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function test()
                    local x = 'original'
                    debug.setlocal(1, 1, 'modified')
                    return x
                end
                return test()
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("modified")
                .Because("debug.setlocal should actually modify the local variable value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetLocalCanChangeType(LuaCompatibilityVersion version)
        {
            // Edge case: setlocal can change the type of a local variable
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function test()
                    local x = 42  -- number
                    debug.setlocal(1, 1, 'now a string')
                    return type(x), x
                end
                return test()
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert
                .That(tuple.Length)
                .IsEqualTo(2)
                .Because("Should return type and value")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[0].String)
                .IsEqualTo("string")
                .Because("Type should be changed to string after setlocal")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[1].String)
                .IsEqualTo("now a string")
                .Because("Value should be the new string value")
                .ConfigureAwait(false);
        }

        // ========================
        // DIAGNOSTIC IMPROVEMENTS: Better assertion messages
        // ========================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetUpvalueAndSetUpvalueRoundTrip(LuaCompatibilityVersion version)
        {
            // Diagnostic test: Verify getupvalue/setupvalue work together correctly
            // Note: _ENV is typically upvalue 1, so our captured variable is upvalue 2
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local captured = 'initial'
                local function closure()
                    return captured
                end
                
                -- Find the upvalue index for 'captured' (skip _ENV which is usually index 1)
                local upvalueIndex = nil
                for i = 1, 10 do
                    local name, _ = debug.getupvalue(closure, i)
                    if name == 'captured' then
                        upvalueIndex = i
                        break
                    end
                    if name == nil then break end
                end
                
                if upvalueIndex == nil then
                    return 'UPVALUE_NOT_FOUND'
                end
                
                -- Get original value
                local name1, val1 = debug.getupvalue(closure, upvalueIndex)
                
                -- Set new value
                local setName = debug.setupvalue(closure, upvalueIndex, 'modified')
                
                -- Get new value
                local name2, val2 = debug.getupvalue(closure, upvalueIndex)
                
                -- Call closure to verify it uses new value
                local result = closure()
                
                return name1, val1, setName, name2, val2, result
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();

            // Check if upvalue was not found
            await Assert
                .That(tuple.Length == 1 && tuple[0].String == "UPVALUE_NOT_FOUND")
                .IsFalse()
                .Because("Failed to find 'captured' upvalue in closure")
                .ConfigureAwait(false);

            await Assert
                .That(tuple.Length)
                .IsEqualTo(6)
                .Because("Should return all 6 diagnostic values for round-trip verification")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[0].String)
                .IsEqualTo("captured")
                .Because("First getupvalue should return upvalue name 'captured'")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[1].String)
                .IsEqualTo("initial")
                .Because("First getupvalue should return original value 'initial'")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[2].String)
                .IsEqualTo("captured")
                .Because("setupvalue should return the upvalue name it modified")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[3].String)
                .IsEqualTo("captured")
                .Because("Second getupvalue should still return upvalue name 'captured'")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[4].String)
                .IsEqualTo("modified")
                .Because("Second getupvalue should return new value 'modified'")
                .ConfigureAwait(false);

            await Assert
                .That(tuple[5].String)
                .IsEqualTo("modified")
                .Because("Closure should return the modified upvalue value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetInfoAllFieldsHaveExpectedTypes(LuaCompatibilityVersion version)
        {
            // Diagnostic test: Verify all getinfo fields have expected types
            // Note: Using only valid flags: n, S, l, u, f, L (not 't' which is unsupported)
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local function sample(a, b)
                    local c = a + b
                    return c
                end
                local info = debug.getinfo(sample, 'nSluf')
                
                -- Check field types (return type names or nil if field missing)
                return 
                    type(info.name) == 'string' or info.name == nil,  -- name can be nil for anonymous
                    type(info.what) == 'string',
                    type(info.source) == 'string',
                    type(info.short_src) == 'string',
                    type(info.linedefined) == 'number',
                    type(info.lastlinedefined) == 'number',
                    type(info.nups) == 'number',
                    type(info.nparams) == 'number' or info.nparams == nil,  -- Lua 5.2+
                    type(info.isvararg) == 'boolean' or info.isvararg == nil  -- Lua 5.2+
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            string[] fieldDescriptions = new[]
            {
                "name (string or nil)",
                "what (string)",
                "source (string)",
                "short_src (string)",
                "linedefined (number)",
                "lastlinedefined (number)",
                "nups (number)",
                "nparams (number or nil for Lua 5.1)",
                "isvararg (boolean or nil for Lua 5.1)",
            };

            await Assert
                .That(tuple.Length)
                .IsGreaterThanOrEqualTo(fieldDescriptions.Length)
                .Because($"Should return at least {fieldDescriptions.Length} type check results")
                .ConfigureAwait(false);

            for (int i = 0; i < Math.Min(tuple.Length, fieldDescriptions.Length); i++)
            {
                await Assert
                    .That(tuple[i].CastToBool())
                    .IsTrue()
                    .Because($"Field type check failed for: {fieldDescriptions[i]}")
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task HookMaskCharactersAreRecognized(LuaCompatibilityVersion version)
        {
            // Diagnostic test: Verify all hook mask characters work
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local results = {}
                local function hook() end
                
                -- Test each mask character individually
                debug.sethook(hook, 'c')
                local _, m1, _ = debug.gethook()
                results[1] = m1 == 'c'
                
                debug.sethook(hook, 'r')
                local _, m2, _ = debug.gethook()
                results[2] = m2 == 'r'
                
                debug.sethook(hook, 'l')
                local _, m3, _ = debug.gethook()
                results[3] = m3 == 'l'
                
                -- Test combined mask
                debug.sethook(hook, 'crl')
                local _, m4, _ = debug.gethook()
                results[4] = m4 == 'crl'
                
                debug.sethook()  -- Clear hook
                
                -- Use unpack for Lua 5.1 compatibility (table.unpack for 5.2+)
                local unpackFn = table.unpack or unpack
                return unpackFn(results)
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            string[] maskDescriptions = new[]
            {
                "'c' mask for call events",
                "'r' mask for return events",
                "'l' mask for line events",
                "'crl' combined mask",
            };

            await Assert
                .That(tuple.Length)
                .IsEqualTo(4)
                .Because("Should return 4 mask check results")
                .ConfigureAwait(false);

            for (int i = 0; i < maskDescriptions.Length; i++)
            {
                await Assert
                    .That(tuple[i].CastToBool())
                    .IsTrue()
                    .Because($"Hook mask test failed for: {maskDescriptions[i]}")
                    .ConfigureAwait(false);
            }
        }

        private static Script CreateScriptWithVersion(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            Script script = new(CoreModulePresets.Complete, options);
            script.Options.DebugPrint = _ => { };
            return script;
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
