namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Assertions.Extensions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class LuaVersionGuardTUnitTests
    {
        // ThrowIfUnavailable Tests

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua54, "coroutine.close")]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua54, "coroutine.close")]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua54, "coroutine.close")]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua53, "utf8.char")]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua53, "utf8.char")]
        public async Task ThrowIfUnavailableThrowsWhenVersionTooOld(
            LuaCompatibilityVersion activeVersion,
            LuaCompatibilityVersion minimumVersion,
            string functionName
        )
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaVersionGuard.ThrowIfUnavailable(activeVersion, minimumVersion, functionName)
            );

            await Assert.That(exception.Message).Contains(functionName).ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("requires").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua54, LuaCompatibilityVersion.Lua54, "coroutine.close")]
        [Arguments(LuaCompatibilityVersion.Lua55, LuaCompatibilityVersion.Lua54, "coroutine.close")]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua53, "utf8.char")]
        [Arguments(LuaCompatibilityVersion.Lua54, LuaCompatibilityVersion.Lua53, "utf8.char")]
        [Arguments(LuaCompatibilityVersion.Lua55, LuaCompatibilityVersion.Lua53, "utf8.char")]
        [Arguments(
            LuaCompatibilityVersion.Latest,
            LuaCompatibilityVersion.Lua54,
            "coroutine.close"
        )]
        public async Task ThrowIfUnavailableDoesNotThrowWhenVersionSufficient(
            LuaCompatibilityVersion activeVersion,
            LuaCompatibilityVersion minimumVersion,
            string functionName
        )
        {
            // Should not throw - just call directly
            LuaVersionGuard.ThrowIfUnavailable(activeVersion, minimumVersion, functionName);

            await Task.CompletedTask.ConfigureAwait(false);
        }

        // ThrowIfRemoved Tests

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua51, "setfenv")]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua51, "setfenv")]
        [Arguments(LuaCompatibilityVersion.Lua54, LuaCompatibilityVersion.Lua51, "setfenv")]
        [Arguments(LuaCompatibilityVersion.Lua55, LuaCompatibilityVersion.Lua51, "setfenv")]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua52, "loadstring")]
        [Arguments(LuaCompatibilityVersion.Lua54, LuaCompatibilityVersion.Lua52, "loadstring")]
        public async Task ThrowIfRemovedThrowsWhenVersionTooNew(
            LuaCompatibilityVersion activeVersion,
            LuaCompatibilityVersion maximumVersion,
            string functionName
        )
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaVersionGuard.ThrowIfRemoved(activeVersion, maximumVersion, functionName)
            );

            await Assert.That(exception.Message).Contains(functionName).ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("removed").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51, "setfenv")]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua52, "loadstring")]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua52, "loadstring")]
        public async Task ThrowIfRemovedDoesNotThrowWhenVersionSupported(
            LuaCompatibilityVersion activeVersion,
            LuaCompatibilityVersion maximumVersion,
            string functionName
        )
        {
            // Should not throw - just call directly
            LuaVersionGuard.ThrowIfRemoved(activeVersion, maximumVersion, functionName);

            await Task.CompletedTask.ConfigureAwait(false);
        }

        // IsAvailable Tests

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua54, LuaCompatibilityVersion.Lua54, true)]
        [Arguments(LuaCompatibilityVersion.Lua55, LuaCompatibilityVersion.Lua54, true)]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua54, false)]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua54, false)]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua54, false)]
        [Arguments(LuaCompatibilityVersion.Latest, LuaCompatibilityVersion.Lua54, true)]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua53, true)]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51, true)]
        public async Task IsAvailableReturnsCorrectResult(
            LuaCompatibilityVersion activeVersion,
            LuaCompatibilityVersion minimumVersion,
            bool expected
        )
        {
            bool result = LuaVersionGuard.IsAvailable(activeVersion, minimumVersion);

            await Assert.That(result).IsEqualTo(expected).ConfigureAwait(false);
        }

        // IsRemoved Tests

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua51, true)]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua51, true)]
        [Arguments(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51, false)]
        [Arguments(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua52, false)]
        [Arguments(LuaCompatibilityVersion.Lua53, LuaCompatibilityVersion.Lua52, true)]
        [Arguments(LuaCompatibilityVersion.Latest, LuaCompatibilityVersion.Lua51, true)]
        public async Task IsRemovedReturnsCorrectResult(
            LuaCompatibilityVersion activeVersion,
            LuaCompatibilityVersion maximumVersion,
            bool expected
        )
        {
            bool result = LuaVersionGuard.IsRemoved(activeVersion, maximumVersion);

            await Assert.That(result).IsEqualTo(expected).ConfigureAwait(false);
        }

        // IsAvailableInRange Tests

        [Test]
        public async Task IsAvailableInRangeReturnsTrueWhenInRange()
        {
            // bit32 is only available in Lua 5.2
            bool result = LuaVersionGuard.IsAvailableInRange(
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(result).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task IsAvailableInRangeReturnsFalseWhenBelowRange()
        {
            // bit32 is not available in Lua 5.1
            bool result = LuaVersionGuard.IsAvailableInRange(
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task IsAvailableInRangeReturnsFalseWhenAboveRange()
        {
            // bit32 is not available in Lua 5.3+
            bool result = LuaVersionGuard.IsAvailableInRange(
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task IsAvailableInRangeWithNullMaximumReturnsTrueWhenAboveMinimum()
        {
            // utf8 library is available in Lua 5.3+ (no upper limit)
            bool result = LuaVersionGuard.IsAvailableInRange(
                LuaCompatibilityVersion.Lua55,
                LuaCompatibilityVersion.Lua53,
                null
            );

            await Assert.That(result).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task IsAvailableInRangeWithNullMaximumReturnsFalseWhenBelowMinimum()
        {
            // utf8 library is not available in Lua 5.2
            bool result = LuaVersionGuard.IsAvailableInRange(
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                null
            );

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        // ThrowIfOutsideRange Tests

        [Test]
        public async Task ThrowIfOutsideRangeThrowsWhenBelowRange()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaVersionGuard.ThrowIfOutsideRange(
                    LuaCompatibilityVersion.Lua51,
                    LuaCompatibilityVersion.Lua52,
                    LuaCompatibilityVersion.Lua52,
                    "bit32.band"
                )
            );

            await Assert.That(exception.Message).Contains("bit32.band").ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("requires").ConfigureAwait(false);
        }

        [Test]
        public async Task ThrowIfOutsideRangeThrowsWhenAboveRange()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaVersionGuard.ThrowIfOutsideRange(
                    LuaCompatibilityVersion.Lua53,
                    LuaCompatibilityVersion.Lua52,
                    LuaCompatibilityVersion.Lua52,
                    "bit32.band"
                )
            );

            await Assert.That(exception.Message).Contains("bit32.band").ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("removed").ConfigureAwait(false);
        }

        [Test]
        public async Task ThrowIfOutsideRangeDoesNotThrowWhenInRange()
        {
            // Should not throw - just call directly
            LuaVersionGuard.ThrowIfOutsideRange(
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua52,
                "bit32.band"
            );

            await Task.CompletedTask.ConfigureAwait(false);
        }

        // GetVersionDisplayName Tests

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, "Lua 5.1")]
        [Arguments(LuaCompatibilityVersion.Lua52, "Lua 5.2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "Lua 5.3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "Lua 5.4")]
        [Arguments(LuaCompatibilityVersion.Lua55, "Lua 5.5")]
        [Arguments(LuaCompatibilityVersion.Latest, "Lua 5.4")]
        public async Task GetVersionDisplayNameReturnsCorrectName(
            LuaCompatibilityVersion version,
            string expectedName
        )
        {
            string displayName = LuaVersionGuard.GetVersionDisplayName(version);

            await Assert.That(displayName).IsEqualTo(expectedName).ConfigureAwait(false);
        }

        [Test]
        public async Task GetVersionDisplayNameThrowsForInvalidVersion()
        {
            LuaCompatibilityVersion invalidVersion = (LuaCompatibilityVersion)999;

            InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(
                () =>
                    LuaVersionGuard.GetVersionDisplayName(invalidVersion)
            );

            await Assert.That(exception.ParamName).IsEqualTo("version").ConfigureAwait(false);
        }

        // GetNextVersionDisplayName Tests

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, "Lua 5.2")]
        [Arguments(LuaCompatibilityVersion.Lua52, "Lua 5.3")]
        [Arguments(LuaCompatibilityVersion.Lua53, "Lua 5.4")]
        [Arguments(LuaCompatibilityVersion.Lua54, "Lua 5.5")]
        [Arguments(LuaCompatibilityVersion.Lua55, "Lua 5.6")]
        [Arguments(LuaCompatibilityVersion.Latest, "Lua 5.5")]
        public async Task GetNextVersionDisplayNameReturnsCorrectName(
            LuaCompatibilityVersion version,
            string expectedNextName
        )
        {
            string nextVersion = LuaVersionGuard.GetNextVersionDisplayName(version);

            await Assert.That(nextVersion).IsEqualTo(expectedNextName).ConfigureAwait(false);
        }

        [Test]
        public async Task GetNextVersionDisplayNameThrowsForInvalidVersion()
        {
            LuaCompatibilityVersion invalidVersion = (LuaCompatibilityVersion)999;

            InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(
                () =>
                    LuaVersionGuard.GetNextVersionDisplayName(invalidVersion)
            );

            await Assert.That(exception.ParamName).IsEqualTo("version").ConfigureAwait(false);
        }

        // Error Message Format Tests

        [Test]
        public async Task ThrowIfUnavailableErrorMessageIncludesVersionInfo()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaVersionGuard.ThrowIfUnavailable(
                    LuaCompatibilityVersion.Lua51,
                    LuaCompatibilityVersion.Lua54,
                    "test.func"
                )
            );

            // Should include the function name
            await Assert.That(exception.Message).Contains("test.func").ConfigureAwait(false);
            // Should include required version
            await Assert.That(exception.Message).Contains("Lua 5.4").ConfigureAwait(false);
            // Should include active version
            await Assert.That(exception.Message).Contains("Lua 5.1").ConfigureAwait(false);
        }

        [Test]
        public async Task ThrowIfRemovedErrorMessageIncludesVersionInfo()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaVersionGuard.ThrowIfRemoved(
                    LuaCompatibilityVersion.Lua53,
                    LuaCompatibilityVersion.Lua51,
                    "test.func"
                )
            );

            // Should include the function name
            await Assert.That(exception.Message).Contains("test.func").ConfigureAwait(false);
            // Should include removed-in version (5.2)
            await Assert.That(exception.Message).Contains("Lua 5.2").ConfigureAwait(false);
            // Should include active version
            await Assert.That(exception.Message).Contains("Lua 5.3").ConfigureAwait(false);
        }
    }
}
