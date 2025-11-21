namespace NovaSharp.Interpreter.Tests.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    /// <summary>
    /// Spec harness for Lua basic library behaviours (§6.1), currently focused on tonumber base parsing.
    /// </summary>
    [TestFixture]
    public sealed class LuaBasicMultiVersionSpecTests : LuaSpecTestBase
    {
        [Test]
        [Description(
            "Lua 5.4 manual §6.1: tonumber with base accepts digits up to base 36 and optional sign."
        )]
        public void TonumberParsesIntegersAcrossSupportedBases()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue tuple = script.DoString(
                @"
                return tonumber('1010', 2),
                       tonumber('-77', 8),
                       tonumber('+1e', 16),
                       tonumber('Z', 36)
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(10));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(-63));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(30));
                Assert.That(tuple.Tuple[3].Number, Is.EqualTo(35));
            });
        }

        [Test]
        [Description(
            "Lua 5.4 manual §6.1: tonumber returns nil when the numeral is invalid for the given base."
        )]
        public void TonumberReturnsNilWhenDigitsExceedBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue tuple = script.DoString("return tonumber('2', 2), tonumber('g', 16)");

            Assert.That(tuple.Tuple[0].IsNil(), Is.True);
            Assert.That(tuple.Tuple[1].IsNil(), Is.True);
        }

        [Test]
        [Description("Lua 5.4 manual §6.1: tonumber errors if the base is outside the 2–36 range.")]
        public void TonumberErrorsWhenBaseIsOutOfRange()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue result = script.DoString(
                "local ok, err = pcall(tonumber, '1', 40) return ok, err"
            );

            Assert.That(result.Tuple[0].Boolean, Is.False);
            Assert.That(result.Tuple[1].String, Does.Contain("base out of range"));
        }

        [Test]
        [Description(
            "Lua 5.4 manual §6.1: tonumber errors when the base argument is not an integer."
        )]
        public void TonumberErrorsWhenBaseIsFractional()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue result = script.DoString(
                "local ok, err = pcall(tonumber, '10', 2.5) return ok, err"
            );

            Assert.That(result.Tuple[0].Boolean, Is.False);
            Assert.That(result.Tuple[1].String, Does.Contain("integer"));
        }
    }
}
