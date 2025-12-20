namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    [UserDataIsolation]
    public sealed class DebugModuleTapParityTUnitTests
    {
        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            return new Script(version, CoreModulePresets.Complete);
        }

        [Test]
        [AllLuaVersions]
        public async Task RequireDebugReturnsFunctionTable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue module = script.DoString("return require('debug')");

            await Assert.That(module.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(module.Table.Get("getinfo").Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);
            await Assert
                .That(module.Table.Get("traceback").Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task RequireDebugReturnsSameInstanceAsGlobal(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue tuple = script.DoString(
                @"
                local first = require('debug')
                local second = require('debug')
                return first == debug, first == second
                "
            );

            DynValue[] results = tuple.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(results.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(results[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(results[1].CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GetInfoReturnsFunctionMetadata(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue info = script.DoString(
                @"
                local function sample()
                    return 1
                end
                return debug.getinfo(sample)
                "
            );

            await Assert.That(info.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(info.Table.Get("func").Type)
                .IsEqualTo(DataType.Function)
                .ConfigureAwait(false);
            await Assert
                .That(info.Table.Get("what").CastToString())
                .IsEqualTo("Lua")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GetInfoLevelOutOfRangeReturnsNil(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue info = script.DoString("return debug.getinfo(999)");

            await Assert.That(info.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GetInfoInvalidArgumentThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("debug.getinfo('invalid')");
            });

            await Assert
                .That(exception.Message)
                .Contains("function or level expected")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GetRegistryExposesLoadedTable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue registry = script.DoString(
                @"
                local debug = require('debug')
                return debug.getregistry()
                "
            );

            await Assert.That(registry.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            DynValue loaded = registry.Table.RawGet("_LOADED") ?? DynValue.Nil;
            await Assert.That(loaded.IsNil()).IsFalse().ConfigureAwait(false);
            await Assert.That(loaded.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SetMetatableRoundTrips(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local target = {}
                local mt = { flag = true }
                debug.setmetatable(target, mt)
                return debug.getmetatable(target) == mt
                "
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SetMetatableErrorMatchesLuaFormat(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function()
                    debug.setmetatable({}, true)
                end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse().ConfigureAwait(false);
            string message = tuple[1].String ?? string.Empty;
            await Assert
                .That(message.Contains("nil or table expected", StringComparison.Ordinal))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(message.Contains(", got ", StringComparison.Ordinal)).IsFalse();
        }

        [Test]
        [AllLuaVersions]
        public async Task SetUserValueRoundTrips(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<TrackedHandle>(ensureUnregistered: true);
            registrationScope.RegisterType<TrackedHandle>();
            script.Globals["handle"] = UserData.Create(new TrackedHandle());

            DynValue result = script.DoString(
                @"
                debug.setuservalue(handle, { label = 'userdata' })
                local value = debug.getuservalue(handle)
                return value and value.label
                "
            );

            await Assert.That(result.String).IsEqualTo("userdata").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SetUserValueReturnsOriginalHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<TrackedHandle>(ensureUnregistered: true);
            registrationScope.RegisterType<TrackedHandle>();
            script.Globals["handle"] = UserData.Create(new TrackedHandle());

            DynValue result = script.DoString(
                @"
                local payload = { flag = true }
                local assigned = debug.setuservalue(handle, payload)
                return assigned == handle, debug.getuservalue(handle) == payload
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple[1].CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SetUserValueRejectsNonTablesWithLuaMessage(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<TrackedHandle>(ensureUnregistered: true);
            registrationScope.RegisterType<TrackedHandle>();
            script.Globals["handle"] = UserData.Create(new TrackedHandle());

            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function()
                    debug.setuservalue(handle, true)
                end)
                return ok, err
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].CastToBool()).IsFalse().ConfigureAwait(false);
            string message = tuple[1].String ?? string.Empty;
            await Assert
                .That(message.Contains("table expected, got boolean", StringComparison.Ordinal))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(message.Contains("nil or table", StringComparison.Ordinal))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GetUpvalueReturnsTuple(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue tuple = script.DoString(
                @"
                local function make()
                    local captured = 7
                    local function inner()
                        return captured
                    end
                    return inner
                end
                local fn = make()
                return debug.getupvalue(fn, 2)
                "
            );

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(tuple.Tuple[0].String).IsEqualTo("captured").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(7d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SetupValueUpdatesClosure(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local function make()
                    local captured = 1
                    local function inner()
                        return captured
                    end
                    return inner
                end
                local fn = make()
                debug.setupvalue(fn, 2, 42)
                return fn()
                "
            );

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task UpvalueJoinSharesState(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local function counter(start)
                    local value = start
                    return function(delta)
                        if delta ~= nil then
                            value = value + delta
                        end
                        return value
                    end
                end

                local first = counter(0)
                local second = counter(100)
                local beforeShared = debug.upvalueid(first, 2) == debug.upvalueid(second, 2)
                debug.upvaluejoin(second, 2, first, 2)
                local afterShared = debug.upvalueid(first, 2) == debug.upvalueid(second, 2)
                second(5)
                local firstValue = first()
                local secondValue = second()

                return {
                    before = beforeShared,
                    after = afterShared,
                    firstValue = firstValue,
                    secondValue = secondValue
                }
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert.That(result.Table.Get("before").Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Table.Get("after").Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Table.Get("firstValue").Number)
                .IsEqualTo(result.Table.Get("secondValue").Number)
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task UpvalueIdReturnsUserDataHandles(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local function make()
                    local captured = 1
                    return function()
                        captured = captured + 1
                        return captured
                    end
                end
                local fn = make()
                local first = debug.upvalueid(fn, 2)
                local second = debug.upvalueid(fn, 2)
                return type(first), first == second
                "
            );

            DynValue[] tuple = result.Tuple ?? Array.Empty<DynValue>();
            await Assert.That(tuple[0].String).IsEqualTo("userdata").ConfigureAwait(false);
            await Assert.That(tuple[1].CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task TracebackIncludesMessage(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue message = script.DoString("return debug.traceback('traceback message')");

            await Assert
                .That(message.String.StartsWith("traceback message", StringComparison.Ordinal))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task TracebackUsesLfLineEndings(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue trace = script.DoString("return debug.traceback()");

            string payload = trace.String ?? string.Empty;
            await Assert
                .That(payload.Contains('\r', StringComparison.Ordinal))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(payload.StartsWith("stack traceback:\n", StringComparison.Ordinal))
                .IsTrue()
                .ConfigureAwait(false);
        }

        private sealed class TrackedHandle { }
    }
}
