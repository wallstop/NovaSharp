namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.CoreLib
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Covers C-style flag ordering, empty precision, and exponent digit rules for finite
    /// <c>string.format</c> conversions.
    /// </summary>
    public sealed class StringFormatFiniteTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatAcceptsZeroPaddingBeforeSignFlag(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("return string.format('%0+9g', 1.5)");

            await Assert.That(result.String).IsEqualTo("+000001.5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatTreatsEmptyPrecisionAsZero(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "return string.format('%.g', 1.5) .. ',' .. string.format('%.e', 1.5)"
            );

            await Assert.That(result.String).IsEqualTo("2,2e+00").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatUsesTwoDigitMinimumExponent(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "return string.format('%e', 1.5) .. ',' .. string.format('%E', 0.015625)"
            );

            await Assert
                .That(result.String)
                .IsEqualTo("1.500000e+00,1.562500E-02")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatClampsStringPrecision(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "return string.format('%.5s', 'abc') .. ',' .. string.format('%.2s', 'xyz')"
            );

            await Assert.That(result.String).IsEqualTo("abc,xy").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatPadsIntegerPrecision(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"return table.concat({
                    string.format('%.5d', 42),
                    string.format('%.5o', 42),
                    string.format('%.5x', 42),
                    string.format('%.5u', 42)
                }, ',')"
            );

            await Assert
                .That(result.String)
                .IsEqualTo("00042,00052,0002a,00042")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatPreservesSpaceSignWithZeroPadding(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"return string.format('%0 9g', 1.5) .. ',' .. string.format('%0 9f', 1.5)"
            );

            await Assert.That(result.String).IsEqualTo(" 000001.5, 1.500000").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatIntegerPrecisionOverridesZeroPadding(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "return string.format('%05.3d', 2) .. ',' .. string.format('%05.3o', 2)"
            );

            await Assert.That(result.String).IsEqualTo("  002,  002").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FormatSuppressesZeroAtZeroIntegerPrecision(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"return table.concat({
                    '[' .. string.format('%.d', 0) .. ']',
                    '[' .. string.format('%.o', 0) .. ']',
                    '[' .. string.format('%.x', 0) .. ']',
                    '[' .. string.format('%.u', 0) .. ']',
                    string.format('%.d', 42)
                }, ',')"
            );

            await Assert.That(result.String).IsEqualTo("[],[],[],[],42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task FormatRejectsCharacterPrecision(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.format('%.1c', 65)")
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid conversion specification: '%.1c'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task FormatRejectsRepeatedFlagsBeforeLua54(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.format('%------9g', 1.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid format (repeated flags)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task FormatAllowsRepeatedFlagsFromLua54(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("return string.format('%------9g', 1.5)");

            await Assert.That(result.String).IsEqualTo("1.5      ").ConfigureAwait(false);
        }
    }
}
