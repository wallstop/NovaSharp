namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Assertions.Extensions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    public sealed class LuaCompatibilityProfileTUnitTests
    {
        [Test]
        public async Task ForVersionLua52ReturnsCorrectProfile()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );

            await Assert
                .That(profile.Version)
                .IsEqualTo(LuaCompatibilityVersion.Lua52)
                .ConfigureAwait(false);
            await Assert.That(profile.SupportsBitwiseOperators).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsBit32Library).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsUtf8Library).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsTableMove).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsToBeClosedVariables).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsConstLocals).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsWarnFunction).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ForVersionLua53ReturnsCorrectProfile()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert
                .That(profile.Version)
                .IsEqualTo(LuaCompatibilityVersion.Lua53)
                .ConfigureAwait(false);
            await Assert.That(profile.SupportsBitwiseOperators).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsBit32Library).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsUtf8Library).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsTableMove).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsToBeClosedVariables).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsConstLocals).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsWarnFunction).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ForVersionLua54ReturnsCorrectProfile()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );

            await Assert
                .That(profile.Version)
                .IsEqualTo(LuaCompatibilityVersion.Lua54)
                .ConfigureAwait(false);
            await Assert.That(profile.SupportsBitwiseOperators).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsBit32Library).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsUtf8Library).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsTableMove).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsToBeClosedVariables).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsConstLocals).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsWarnFunction).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ForVersionLua55ReturnsCorrectProfile()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua55
            );

            await Assert
                .That(profile.Version)
                .IsEqualTo(LuaCompatibilityVersion.Lua55)
                .ConfigureAwait(false);
            await Assert.That(profile.SupportsBitwiseOperators).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsBit32Library).IsFalse().ConfigureAwait(false);
            await Assert.That(profile.SupportsUtf8Library).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsTableMove).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsToBeClosedVariables).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsConstLocals).IsTrue().ConfigureAwait(false);
            await Assert.That(profile.SupportsWarnFunction).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ForVersionLatestReturnsLua54Profile()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Latest
            );

            // Latest should resolve to CurrentDefault (Lua 5.4), not the highest supported version (5.5)
            await Assert
                .That(profile.Version)
                .IsEqualTo(LuaCompatibilityVersion.Lua54)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ForVersionThrowsForInvalidVersion()
        {
            LuaCompatibilityVersion invalidVersion = (LuaCompatibilityVersion)999;

            InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(
                () =>
                    LuaCompatibilityProfile.ForVersion(invalidVersion)
            );

            await Assert.That(exception.ParamName).IsEqualTo("version").ConfigureAwait(false);
        }

        [Test]
        public async Task DisplayNameLua52ReturnsCorrectString()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(profile.DisplayName).IsEqualTo("Lua 5.2").ConfigureAwait(false);
        }

        [Test]
        public async Task DisplayNameLua53ReturnsCorrectString()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.DisplayName).IsEqualTo("Lua 5.3").ConfigureAwait(false);
        }

        [Test]
        public async Task DisplayNameLua54ReturnsCorrectString()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );

            await Assert.That(profile.DisplayName).IsEqualTo("Lua 5.4").ConfigureAwait(false);
        }

        [Test]
        public async Task DisplayNameLua55ReturnsCorrectString()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua55
            );

            await Assert.That(profile.DisplayName).IsEqualTo("Lua 5.5").ConfigureAwait(false);
        }

        [Test]
        public async Task GetDisplayNameLatestReturnsLuaLatest()
        {
            string displayName = LuaCompatibilityProfile.GetDisplayName(
                LuaCompatibilityVersion.Latest
            );

            await Assert.That(displayName).IsEqualTo("Lua Latest").ConfigureAwait(false);
        }

        [Test]
        public async Task GetDisplayNameUnknownVersionReturnsFallbackString()
        {
            // Use an undefined enum value to exercise the default branch
            LuaCompatibilityVersion unknownVersion = (LuaCompatibilityVersion)999;

            string displayName = LuaCompatibilityProfile.GetDisplayName(unknownVersion);

            // The default case calls version.ToString()
            await Assert.That(displayName).IsEqualTo("999").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ForVersionReturnsSameInstanceForSameVersion(
            LuaCompatibilityVersion version
        )
        {
            LuaCompatibilityProfile profile1 = LuaCompatibilityProfile.ForVersion(version);
            LuaCompatibilityProfile profile2 = LuaCompatibilityProfile.ForVersion(version);

            // Profiles are cached singleton instances
            await Assert.That(profile1).IsSameReferenceAs(profile2).ConfigureAwait(false);
        }

        [Test]
        public async Task Lua52ProfileDoesNotSupportBitwiseOperators()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(profile.SupportsBitwiseOperators).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileSupportsBitwiseOperators()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsBitwiseOperators).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua52ProfileSupportsBit32Library()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(profile.SupportsBit32Library).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileDoesNotSupportBit32Library()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsBit32Library).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua52ProfileDoesNotSupportUtf8Library()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(profile.SupportsUtf8Library).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileSupportsUtf8Library()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsUtf8Library).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua52ProfileDoesNotSupportTableMove()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );

            await Assert.That(profile.SupportsTableMove).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileSupportsTableMove()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsTableMove).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileDoesNotSupportToBeClosedVariables()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsToBeClosedVariables).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua54ProfileSupportsToBeClosedVariables()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );

            await Assert.That(profile.SupportsToBeClosedVariables).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileDoesNotSupportConstLocals()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsConstLocals).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua54ProfileSupportsConstLocals()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );

            await Assert.That(profile.SupportsConstLocals).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua53ProfileDoesNotSupportWarnFunction()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );

            await Assert.That(profile.SupportsWarnFunction).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua54ProfileSupportsWarnFunction()
        {
            LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );

            await Assert.That(profile.SupportsWarnFunction).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task Lua55ProfileHasSameFeaturesAsLua54()
        {
            LuaCompatibilityProfile lua54 = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );
            LuaCompatibilityProfile lua55 = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua55
            );

            await Assert
                .That(lua55.SupportsBitwiseOperators)
                .IsEqualTo(lua54.SupportsBitwiseOperators)
                .ConfigureAwait(false);
            await Assert
                .That(lua55.SupportsBit32Library)
                .IsEqualTo(lua54.SupportsBit32Library)
                .ConfigureAwait(false);
            await Assert
                .That(lua55.SupportsUtf8Library)
                .IsEqualTo(lua54.SupportsUtf8Library)
                .ConfigureAwait(false);
            await Assert
                .That(lua55.SupportsTableMove)
                .IsEqualTo(lua54.SupportsTableMove)
                .ConfigureAwait(false);
            await Assert
                .That(lua55.SupportsToBeClosedVariables)
                .IsEqualTo(lua54.SupportsToBeClosedVariables)
                .ConfigureAwait(false);
            await Assert
                .That(lua55.SupportsConstLocals)
                .IsEqualTo(lua54.SupportsConstLocals)
                .ConfigureAwait(false);
            await Assert
                .That(lua55.SupportsWarnFunction)
                .IsEqualTo(lua54.SupportsWarnFunction)
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52, "Lua 5.2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "Lua 5.3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "Lua 5.4")]
        [Arguments(LuaCompatibilityVersion.Lua55, "Lua 5.5")]
        [Arguments(LuaCompatibilityVersion.Latest, "Lua Latest")]
        public async Task GetDisplayNameReturnsExpectedValue(
            LuaCompatibilityVersion version,
            string expectedDisplayName
        )
        {
            string displayName = LuaCompatibilityProfile.GetDisplayName(version);

            await Assert.That(displayName).IsEqualTo(expectedDisplayName).ConfigureAwait(false);
        }
    }
}
