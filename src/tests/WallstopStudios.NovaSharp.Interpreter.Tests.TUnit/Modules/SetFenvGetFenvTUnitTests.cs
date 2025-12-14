namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    /// <summary>
    /// Tests for Lua 5.1's setfenv and getfenv functions.
    /// These functions were removed in Lua 5.2+ (replaced by _ENV).
    /// </summary>
    public sealed class SetFenvGetFenvTUnitTests
    {
        private static Script CreateScript(
            LuaCompatibilityVersion version = LuaCompatibilityVersion.Lua54
        )
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModulePresets.Complete, options);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvExistsInLua51(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return type(getfenv)");

            await Assert.That(result.String).IsEqualTo("function").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvExistsInLua51(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return type(setfenv)");

            await Assert.That(result.String).IsEqualTo("function").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetFenvIsNilInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return getfenv");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetFenvIsNilInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return setfenv");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvReturnsGlobalEnvironmentByDefault(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return getfenv() == _G");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvWithZeroReturnsGlobalEnvironment(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return getfenv(0) == _G");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvWithOneReturnsCurrentEnvironment(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return getfenv(1) == _G");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvOnFunctionReturnsEnvironment(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local function f() return 1 end
                return getfenv(f) == _G
            "
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvChangesFunctionEnvironment(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local function f() return x end
                local env = { x = 42 }
                setmetatable(env, { __index = _G })
                setfenv(f, env)
                return f()
            "
            );

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvReturnsTheFunction(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local function f() return 1 end
                local returned = setfenv(f, _G)
                return returned == f
            "
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvReflectsSetFenvChanges(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local function f() return 1 end
                local env = { custom = true }
                setmetatable(env, { __index = _G })
                setfenv(f, env)
                return getfenv(f).custom
            "
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvWithInvalidLevelThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("getfenv(100)")
            );

            await Assert.That(exception.Message).Contains("invalid level").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvWithNegativeLevelThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("getfenv(-1)")
            );

            await Assert.That(exception.Message).Contains("non-negative").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvWithStringThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("getfenv('string')")
            );

            await Assert.That(exception.Message).Contains("number expected").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvWithInvalidLevelThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("setfenv(100, {})")
            );

            await Assert.That(exception.Message).Contains("invalid level").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvWithNonTableSecondArgThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("local f = function() end; setfenv(f, 'string')")
            );

            await Assert.That(exception.Message).Contains("table expected").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvOnClrFunctionThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("setfenv(print, {})")
            );

            await Assert.That(exception.Message).Contains("cannot change").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GetFenvOnClrFunctionReturnsGlobal(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return getfenv(print) == _G");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task SetFenvWithLevelChangesEnvironment(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local result = nil
                local function test_level()
                    local new_env = { custom_value = 123 }
                    setmetatable(new_env, { __index = _G })
                    setfenv(1, new_env)
                    result = custom_value
                end
                test_level()
                return result
            "
            );

            await Assert.That(result.Number).IsEqualTo(123d).ConfigureAwait(false);
        }
    }
}
