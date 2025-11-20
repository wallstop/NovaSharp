namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class CompatibilityVersionTests
    {
        [SetUp]
        public void ResetGlobalCompatibility()
        {
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Latest;
        }

        [Test]
        public void DefaultsToLatestCompatibilityVersion()
        {
            Assert.That(
                Script.GlobalOptions.CompatibilityVersion,
                Is.EqualTo(LuaCompatibilityVersion.Latest)
            );

            Script script = new();
            Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Latest));
        }

        [Test]
        public void ScriptOptionsCanOverrideCompatibilityVersion()
        {
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Lua54;
            Script script = new();

            Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua54));
            Assert.That(
                script.CompatibilityProfile.Version,
                Is.EqualTo(LuaCompatibilityVersion.Lua54)
            );
            Assert.That(script.CompatibilityProfile.SupportsConstLocals, Is.True);
            Assert.That(script.CompatibilityProfile.SupportsToBeClosedVariables, Is.True);

            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;
            Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua53));
            Assert.That(
                script.CompatibilityProfile.Version,
                Is.EqualTo(LuaCompatibilityVersion.Lua53)
            );
            Assert.That(script.CompatibilityProfile.SupportsConstLocals, Is.False);
            Assert.That(script.CompatibilityProfile.SupportsBitwiseOperators, Is.True);
        }

        [Test]
        public void CompatibilityProfileReflectsVersionFeatureSet()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua52;

            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            Assert.Multiple(() =>
            {
                Assert.That(profile.Version, Is.EqualTo(LuaCompatibilityVersion.Lua52));
                Assert.That(profile.SupportsBitwiseOperators, Is.False);
                Assert.That(profile.SupportsUtf8Library, Is.False);
                Assert.That(profile.SupportsTableMove, Is.False);
                Assert.That(profile.SupportsToBeClosedVariables, Is.False);
                Assert.That(profile.SupportsConstLocals, Is.False);
                Assert.That(profile.SupportsWarnFunction, Is.False);
            });

            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua55;
            profile = script.CompatibilityProfile;
            Assert.Multiple(() =>
            {
                Assert.That(profile.Version, Is.EqualTo(LuaCompatibilityVersion.Lua55));
                Assert.That(profile.SupportsBitwiseOperators, Is.True);
                Assert.That(profile.SupportsUtf8Library, Is.True);
                Assert.That(profile.SupportsTableMove, Is.True);
                Assert.That(profile.SupportsToBeClosedVariables, Is.True);
                Assert.That(profile.SupportsConstLocals, Is.True);
                Assert.That(profile.SupportsWarnFunction, Is.True);
            });
        }

        [Test]
        public void Bit32LibraryRemainsAvailableAcrossProfiles()
        {
            Script lua52 = new(
                CoreModules.PresetComplete,
                new ScriptOptions() { CompatibilityVersion = LuaCompatibilityVersion.Lua52 }
            );
            Assert.That(lua52.Globals.Get("bit32").Type, Is.EqualTo(DataType.Table));

            Script lua53 = new(
                CoreModules.PresetComplete,
                new ScriptOptions() { CompatibilityVersion = LuaCompatibilityVersion.Lua53 }
            );
            Assert.That(lua53.Globals.Get("bit32").Type, Is.EqualTo(DataType.Table));
        }

        [Test]
        public void WarnFunctionOnlyAvailableInLua54Plus()
        {
            Script lua53 = new(
                CoreModules.Basic,
                new ScriptOptions() { CompatibilityVersion = LuaCompatibilityVersion.Lua53 }
            );
            Assert.That(lua53.Globals.Get("warn").IsNil(), Is.True);

            List<string> warnings = new();
            Script lua54 = new(
                CoreModules.Basic,
                new ScriptOptions() { CompatibilityVersion = LuaCompatibilityVersion.Lua54 }
            );
            lua54.Options.DebugPrint = s => warnings.Add(s);

            lua54.DoString("warn('hello', 'world')");

            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(warnings[0], Is.EqualTo("hello\tworld"));
        }

        [Test]
        public void TableMoveOnlyAvailableInLua53Plus()
        {
            Script lua52 = new(
                CoreModules.Table,
                new ScriptOptions() { CompatibilityVersion = LuaCompatibilityVersion.Lua52 }
            );
            Assert.That(
                lua52.Globals.Get("table").Table.Get("move").IsNil(),
                Is.True,
                "Lua 5.2 should not expose table.move"
            );

            Script lua53 = new(
                CoreModules.Table,
                new ScriptOptions() { CompatibilityVersion = LuaCompatibilityVersion.Lua53 }
            );

            DynValue result = lua53.DoString(
                @"
                local src = { 'a', 'b', 'c' }
                local dest = {}
                table.move(src, 1, 3, 1, dest)
                return dest[1], dest[2], dest[3]
                "
            );

            Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Tuple[0].String, Is.EqualTo("a"));
            Assert.That(result.Tuple[1].String, Is.EqualTo("b"));
            Assert.That(result.Tuple[2].String, Is.EqualTo("c"));
        }

        [Test]
        public void ForVersionReturnsSingletonsWithExpectedFeatureFlags()
        {
            LuaCompatibilityProfile lua52 = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua52
            );
            LuaCompatibilityProfile lua53 = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua53
            );
            LuaCompatibilityProfile lua54 = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua54
            );
            LuaCompatibilityProfile lua55 = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Lua55
            );
            LuaCompatibilityProfile latest = LuaCompatibilityProfile.ForVersion(
                LuaCompatibilityVersion.Latest
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    LuaCompatibilityProfile.ForVersion(LuaCompatibilityVersion.Lua52),
                    Is.SameAs(lua52)
                );
                Assert.That(lua52.SupportsBitwiseOperators, Is.False);
                Assert.That(lua52.SupportsBit32Library, Is.True);
                Assert.That(lua52.SupportsUtf8Library, Is.False);
                Assert.That(lua52.SupportsWarnFunction, Is.False);

                Assert.That(lua53.SupportsBitwiseOperators, Is.True);
                Assert.That(lua53.SupportsTableMove, Is.True);
                Assert.That(lua53.SupportsConstLocals, Is.False);

                Assert.That(lua54.SupportsToBeClosedVariables, Is.True);
                Assert.That(lua54.SupportsConstLocals, Is.True);
                Assert.That(lua54.SupportsWarnFunction, Is.True);

                Assert.That(lua55, Is.SameAs(latest));
                Assert.That(lua55.SupportsWarnFunction, Is.True);
            });
        }

        [Test]
        public void ForVersionThrowsWhenVersionIsUnsupported()
        {
            const LuaCompatibilityVersion invalid = (LuaCompatibilityVersion)999;

            Assert.That(
                () => LuaCompatibilityProfile.ForVersion(invalid),
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }
    }
}
