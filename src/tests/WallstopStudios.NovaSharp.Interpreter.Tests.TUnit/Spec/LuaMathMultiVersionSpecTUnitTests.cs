namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Multi-version spec harness covering Lua 5.3+ math library additions (§6.7).
    /// </summary>
    public sealed class LuaMathMultiVersionSpecTUnitTests : LuaSpecTestBase
    {
        private static readonly LuaCompatibilityVersion[] Lua53PlusVersions =
        {
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
            LuaCompatibilityVersion.Latest,
        };

        [global::TUnit.Core.Test]
        public async Task MathIntegerHelpersAreUnavailableBeforeLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52, CoreModules.PresetComplete);
            DynValue mathTable = script.Globals.Get("math");
            await Assert.That(mathTable.Type).IsEqualTo(DataType.Table);

            Table mt = mathTable.Table;
            await Assert.That(mt.Get("type").IsNil()).IsTrue();
            await Assert.That(mt.Get("tointeger").IsNil()).IsTrue();
            await Assert.That(mt.Get("ult").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task MathTypeReportsIntegerAndFloatAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = CreateScript(version, CoreModules.PresetComplete);
                DynValue tuple = script.DoString(
                    "return math.type(5), math.type(3.5), math.type(1.0)"
                );

                await Assert.That(tuple.Tuple[0].String).IsEqualTo("integer");
                await Assert.That(tuple.Tuple[1].String).IsEqualTo("float");
                await Assert.That(tuple.Tuple[2].String).IsEqualTo("integer");
            }
        }

        [global::TUnit.Core.Test]
        public async Task MathToIntegerConvertsNumbersAndStringsAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = CreateScript(version, CoreModules.PresetComplete);
                DynValue tuple = script.DoString(
                    "return math.tointeger(10.0), math.tointeger(-3), math.tointeger('42'), math.tointeger(3.25)"
                );

                await Assert.That(tuple.Tuple[0].Number).IsEqualTo(10);
                await Assert.That(tuple.Tuple[1].Number).IsEqualTo(-3);
                await Assert.That(tuple.Tuple[2].Number).IsEqualTo(42);
                await Assert.That(tuple.Tuple[3].IsNil()).IsTrue();
            }
        }

        [global::TUnit.Core.Test]
        public async Task MathToIntegerErrorsOnUnsupportedTypesAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = CreateScript(version, CoreModules.PresetComplete);
                DynValue tuple = script.DoString(
                    "local ok, err = pcall(math.tointeger, {}) return ok, err"
                );

                await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
                await Assert.That(tuple.Tuple[1].String).Contains("table");
            }
        }

        [global::TUnit.Core.Test]
        public async Task MathUltComparesUsingUnsignedOrderingAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = CreateScript(version, CoreModules.PresetComplete);
                DynValue tuple = script.DoString(
                    "return math.ult(0, -1), math.ult(-1, 0), math.ult(10, 20)"
                );

                await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
                await Assert.That(tuple.Tuple[1].Boolean).IsFalse();
                await Assert.That(tuple.Tuple[2].Boolean).IsTrue();
            }
        }

        [global::TUnit.Core.Test]
        public async Task MathUltRejectsNonIntegerArgumentsAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = CreateScript(version, CoreModules.PresetComplete);
                DynValue tuple = script.DoString(
                    "local ok, err = pcall(math.ult, 1.5, 2) return ok, err"
                );

                await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
                await Assert.That(tuple.Tuple[1].String).Contains("integer");
            }
        }
    }
}
