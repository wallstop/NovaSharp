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

    /// <summary>
    /// Tests for <c>load</c> and <c>loadstring</c> version-specific behavior:
    /// <list type="bullet">
    /// <item>Lua 5.1: <c>load(func [, chunkname])</c> - only accepts reader functions</item>
    /// <item>Lua 5.1: <c>loadstring(string [, chunkname])</c> - available</item>
    /// <item>Lua 5.2+: <c>load(chunk [, chunkname [, mode [, env]]])</c> - accepts strings and functions</item>
    /// <item>Lua 5.2+: <c>loadstring</c> removed (use <c>load</c>)</item>
    /// </list>
    /// </summary>
    public sealed class LoadModuleVersionParityTUnitTests
    {
        // =============================================================================
        // loadstring availability tests
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadstringIsAvailableInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return type(loadstring)");
            await Assert.That(result.String).IsEqualTo("function").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LoadstringIsNilInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return type(loadstring)");
            await Assert.That(result.String).IsEqualTo("nil").ConfigureAwait(false);
        }

        // =============================================================================
        // loadstring functionality tests (Lua 5.1 only)
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadstringCompilesStringInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local f, err = loadstring('return 42')
                assert(f ~= nil, 'loadstring should return a function')
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadstringUsesChunknameForErrors(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local f, err = loadstring('error(""boom"")', 'my-chunk')
                assert(f ~= nil, 'loadstring should return a function')
                local ok, msg = pcall(f)
                return msg
                "
            );
            await Assert.That(result.String).Contains("my-chunk").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadstringReturnsSyntaxErrorTupleOnInvalidSyntax(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local f, err = loadstring('function(')
                return f, err
                "
            );
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Type)
                .IsEqualTo(DataType.String)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("unexpected symbol")
                .ConfigureAwait(false);
        }

        // =============================================================================
        // load() signature tests - Lua 5.1 (function only)
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadRejectsStringsInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("load('return 1')")
            );

            await Assert
                .That(exception.Message)
                .Contains("function expected")
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("got string").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadAcceptsReaderFunctionInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local done = false
                local function reader()
                    if done then return nil end
                    done = true
                    return 'return 123'
                end
                local f = load(reader)
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(123d).ConfigureAwait(false);
        }

        // =============================================================================
        // load() signature tests - Lua 5.2+ (string and function)
        // =============================================================================

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LoadAcceptsStringsInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local f = load('return 42')
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LoadAcceptsReaderFunctionInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local done = false
                local function reader()
                    if done then return nil end
                    done = true
                    return 'return 456'
                end
                local f = load(reader)
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(456d).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LoadUsesEnvParameterInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local env = { x = 100 }
                local f = load('return x', 'chunk', 't', env)
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(100d).ConfigureAwait(false);
        }

        // =============================================================================
        // loadsafe behavior
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task LoadsafeRejectsStringsInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("loadsafe('return 1')")
            );

            await Assert
                .That(exception.Message)
                .Contains("function expected")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LoadsafeAcceptsStringsInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local f = loadsafe('return 99')
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(99d).ConfigureAwait(false);
        }

        // =============================================================================
        // Edge cases
        // =============================================================================

        [Test]
        [AllLuaVersions]
        public async Task LoadRejectsNonFunctionNonStringInAllVersions(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("load(123)")
            );

            await Assert
                .That(exception.Message)
                .Contains("function expected")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task LoadWithReaderConcatenatesFragmentsInAllVersions(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local parts = { 'return ', '1 + ', '2' }
                local index = 0
                local function reader()
                    index = index + 1
                    return parts[index]
                end
                local f = load(reader)
                return f()
                "
            );
            await Assert.That(result.Number).IsEqualTo(3d).ConfigureAwait(false);
        }
    }
}
