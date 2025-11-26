namespace NovaSharp.Interpreter.Tests.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    /// <summary>
    /// Multi-version spec harness covering Lua 5.3+ math library additions (§6.7).
    /// </summary>
    [TestFixture]
    public sealed class LuaMathMultiVersionSpecTests : LuaSpecTestBase
    {
        private static readonly LuaCompatibilityVersion[] Lua53PlusVersions =
        {
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
            LuaCompatibilityVersion.Latest,
        };

        [Test]
        [Description(
            "Lua 5.2 manual §6.7: math.type/math.tointeger/math.ult are unavailable before Lua 5.3."
        )]
        public void MathIntegerHelpersAreUnavailableBeforeLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52, CoreModules.PresetComplete);
            DynValue mathTable = script.Globals.Get("math");
            Assert.That(mathTable.Type, Is.EqualTo(DataType.Table));

            Table mt = mathTable.Table;
            Assert.That(mt.Get("type").IsNil(), Is.True);
            Assert.That(mt.Get("tointeger").IsNil(), Is.True);
            Assert.That(mt.Get("ult").IsNil(), Is.True);
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description("Lua 5.3 manual §6.7: math.type reports integer vs float.")]
        public void MathTypeReportsIntegerAndFloat(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetComplete);
            DynValue tuple = script.DoString("return math.type(5), math.type(3.5), math.type(1.0)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("integer"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("float"));
                Assert.That(tuple.Tuple[2].String, Is.EqualTo("integer"));
            });
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.7: math.tointeger converts integral numbers and strings, nil otherwise."
        )]
        public void MathToIntegerConvertsNumbersAndStrings(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetComplete);
            DynValue tuple = script.DoString(
                "return math.tointeger(10.0), math.tointeger(-3), math.tointeger('42'), math.tointeger(3.25)"
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(10));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(-3));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(42));
                Assert.That(tuple.Tuple[3].IsNil(), Is.True);
            });
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description("Lua 5.3 manual §6.7: math.tointeger rejects unsupported types.")]
        public void MathToIntegerErrorsOnUnsupportedTypes(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetComplete);
            DynValue tuple = script.DoString(
                "local ok, err = pcall(math.tointeger, {}) return ok, err"
            );

            Assert.That(tuple.Tuple[0].Boolean, Is.False);
            Assert.That(tuple.Tuple[1].String, Does.Contain("table"));
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description("Lua 5.3 manual §6.7: math.ult performs unsigned comparisons.")]
        public void MathUltComparesUsingUnsignedOrdering(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetComplete);
            DynValue tuple = script.DoString(
                "return math.ult(0, -1), math.ult(-1, 0), math.ult(10, 20)"
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].Boolean, Is.False);
                Assert.That(tuple.Tuple[2].Boolean, Is.True);
            });
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description("Lua 5.3 manual §6.7: math.ult errors when arguments are non-integers.")]
        public void MathUltRejectsNonIntegerArguments(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetComplete);
            DynValue tuple = script.DoString(
                "local ok, err = pcall(math.ult, 1.5, 2) return ok, err"
            );

            Assert.That(tuple.Tuple[0].Boolean, Is.False);
            Assert.That(tuple.Tuple[1].String, Does.Contain("integer"));
        }
    }
}
