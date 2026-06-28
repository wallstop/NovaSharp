namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Multi-version spec harness covering Lua 5.3+ math library additions (§6.7).
    /// </summary>
    public sealed class LuaMathMultiVersionSpecTUnitTests : LuaSpecTestBase
    {
        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task MathIntegerHelpersAreUnavailableBeforeLua53(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue mathTable = script.Globals.Get("math");
            await Assert.That(mathTable.Type).IsEqualTo(DataType.Table);

            Table mt = mathTable.Table;
            await Assert.That(mt.Get("type").IsNil()).IsTrue();
            await Assert.That(mt.Get("tointeger").IsNil()).IsTrue();
            await Assert.That(mt.Get("ult").IsNil()).IsTrue();
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathTypeReportsIntegerAndFloat(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue tuple = script.DoString("return math.type(5), math.type(3.5), math.type(1.0)");

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("integer");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("float");
            // Per Lua 5.3+ spec: 1.0 is a float literal, so math.type returns "float"
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("float");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathToIntegerConvertsNumbersAndStrings(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue tuple = script.DoString(
                "return math.tointeger(10.0), math.tointeger(-3), math.tointeger('42'), math.tointeger(3.25)"
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(10);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(-3);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(42);
            await Assert.That(tuple.Tuple[3].IsNil()).IsTrue();
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathToIntegerReturnsNilForUnsupportedTypes(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua 5.3+ spec, math.tointeger returns nil for non-number/non-string types
            // (boolean, table, function, userdata, etc.) - it does NOT throw an error.
            // Reference: Lua 5.3 Manual §6.7
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger({})");

            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathToIntegerReturnsNilForBoolean(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(true)");

            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathToIntegerReturnsNilForFunction(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(function() end)");

            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathUltComparesUsingUnsignedOrdering(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue tuple = script.DoString(
                "return math.ult(0, -1), math.ult(-1, 0), math.ult(10, 20)"
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[2].Boolean).IsTrue();
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathUltRejectsNonIntegerArguments(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue tuple = script.DoString(
                "local ok, err = pcall(math.ult, 1.5, 2) return ok, err"
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("integer");
        }
    }
}
