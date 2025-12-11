namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests for the <c>_VERSION</c> global variable across all supported Lua compatibility modes.
    /// Per the Lua Reference Manual, <c>_VERSION</c> must be a string containing the running
    /// Lua version (e.g., "Lua 5.4").
    /// </summary>
    /// <remarks>
    /// §8.41 from PLAN.md: Fix _VERSION global to return correct string per compatibility mode.
    /// Reference: Lua 5.4 Manual §6.1 - Basic Functions.
    /// </remarks>
    public sealed class LuaVersionGlobalTUnitTests : LuaSpecTestBase
    {
        // ========================================
        // _VERSION Tests Per Compatibility Mode
        // ========================================

        [Test]
        public async Task VersionGlobalReturnsLua51InLua51Mode()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua51, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.1").ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalReturnsLua52InLua52Mode()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.2").ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalReturnsLua53InLua53Mode()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua53, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.3").ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalReturnsLua54InLua54Mode()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.4").ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalReturnsLua55InLua55Mode()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.5").ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalReturnsLua54ForLatestMode()
        {
            // Latest currently maps to Lua 5.4 (the current default per LuaVersionDefaults)
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.4").ConfigureAwait(false);
        }

        // ========================================
        // _VERSION Accessibility Tests
        // ========================================

        [Test]
        public async Task VersionGlobalIsAccessibleWithoutExplicitImport()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            DynValue result = script.DoString("local v = _VERSION; return type(v)");

            await Assert.That(result.String).IsEqualTo("string").ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalCanBeUsedInComparisons()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION == 'Lua 5.4'");

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task VersionGlobalCanBeUsedForVersionDetection()
        {
            // Common pattern: checking Lua version at runtime
            Script script = CreateScript(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            string code =
                @"
                local major, minor = _VERSION:match('Lua (%d+)%.(%d+)')
                return tonumber(major), tonumber(minor)
            ";
            DynValue result = script.DoString(code);

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(4).ConfigureAwait(false);
        }

        // ========================================
        // _NovaSharp Table Tests
        // ========================================

        [Test]
        public async Task NovaSharpTableContainsVersionInfo()
        {
            // NovaSharp-specific version info is available in _NovaSharp table
            Script script = CreateScript(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _NovaSharp.version");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).Contains(".").ConfigureAwait(false); // Version contains dots
        }

        [Test]
        public async Task NovaSharpTableContainsLuaCompatInfo()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _NovaSharp.luacompat");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.4").ConfigureAwait(false);
        }
    }
}
