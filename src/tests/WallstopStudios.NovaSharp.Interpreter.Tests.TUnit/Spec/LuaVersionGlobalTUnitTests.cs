namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

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
        [Arguments(LuaCompatibilityVersion.Lua51, "Lua 5.1")]
        [Arguments(LuaCompatibilityVersion.Lua52, "Lua 5.2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "Lua 5.3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "Lua 5.4")]
        [Arguments(LuaCompatibilityVersion.Lua55, "Lua 5.5")]
        public async Task VersionGlobalReturnsCorrectVersionPerMode(
            LuaCompatibilityVersion version,
            string expectedVersionString
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo(expectedVersionString).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task VersionGlobalReturnsLua54ForLatestMode(LuaCompatibilityVersion version)
        {
            // Latest currently maps to Lua 5.4 (the current default per LuaVersionDefaults)
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("Lua 5.4").ConfigureAwait(false);
        }

        // ========================================
        // _VERSION Accessibility Tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task VersionGlobalIsAccessibleWithoutExplicitImport(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("local v = _VERSION; return type(v)");

            await Assert.That(result.String).IsEqualTo("string").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, "Lua 5.1")]
        [Arguments(LuaCompatibilityVersion.Lua52, "Lua 5.2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "Lua 5.3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "Lua 5.4")]
        [Arguments(LuaCompatibilityVersion.Lua55, "Lua 5.5")]
        public async Task VersionGlobalCanBeUsedInComparisons(
            LuaCompatibilityVersion version,
            string expectedVersionString
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return _VERSION == '{expectedVersionString}'");

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, 5, 1)]
        [Arguments(LuaCompatibilityVersion.Lua52, 5, 2)]
        [Arguments(LuaCompatibilityVersion.Lua53, 5, 3)]
        [Arguments(LuaCompatibilityVersion.Lua54, 5, 4)]
        [Arguments(LuaCompatibilityVersion.Lua55, 5, 5)]
        public async Task VersionGlobalCanBeUsedForVersionDetection(
            LuaCompatibilityVersion version,
            int expectedMajor,
            int expectedMinor
        )
        {
            // Common pattern: checking Lua version at runtime
            Script script = CreateScript(version, CoreModulePresets.Complete);
            string code =
                @"
                local major, minor = _VERSION:match('Lua (%d+)%.(%d+)')
                return tonumber(major), tonumber(minor)
            ";
            DynValue result = script.DoString(code);

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(expectedMajor)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(expectedMinor)
                .ConfigureAwait(false);
        }

        // ========================================
        // _NovaSharp Table Tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task NovaSharpTableContainsVersionInfo(LuaCompatibilityVersion version)
        {
            // NovaSharp-specific version info is available in _NovaSharp table
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _NovaSharp.version");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).Contains(".").ConfigureAwait(false); // Version contains dots
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, "Lua 5.1")]
        [Arguments(LuaCompatibilityVersion.Lua52, "Lua 5.2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "Lua 5.3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "Lua 5.4")]
        [Arguments(LuaCompatibilityVersion.Lua55, "Lua 5.5")]
        public async Task NovaSharpTableContainsLuaCompatInfo(
            LuaCompatibilityVersion version,
            string expectedLuaCompat
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _NovaSharp.luacompat");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo(expectedLuaCompat).ConfigureAwait(false);
        }
    }
}
