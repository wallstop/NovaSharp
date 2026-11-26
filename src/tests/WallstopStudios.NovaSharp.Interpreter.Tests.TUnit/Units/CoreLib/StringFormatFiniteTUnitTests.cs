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
