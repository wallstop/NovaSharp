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
    /// Multi-version spec harness for Lua 5.3+ §6.6 (table.move semantics and compatibility gating).
    /// </summary>
    public sealed class LuaTableMoveMultiVersionSpecTUnitTests : LuaSpecTestBase
    {
        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task TableMoveIsUnavailableBeforeLua53(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Default);
            DynValue table = script.Globals.Get("table");
            await Assert.That(table.Type).IsEqualTo(DataType.Table);
            DynValue move = table.Table.Get("move");
            await Assert.That(move.IsNil()).IsTrue();
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TableMoveReturnsDestinationTable(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Default);
            DynValue result = script.DoString(
                @"
                local src = { 'a', 'b', 'c' }
                local dest = {}
                local returned = table.move(src, 1, #src, 1, dest)
                return dest[1], dest[2], dest[3], returned == dest
                "
            );

            await Assert.That(result.Tuple[0].String).IsEqualTo("a");
            await Assert.That(result.Tuple[1].String).IsEqualTo("b");
            await Assert.That(result.Tuple[2].String).IsEqualTo("c");
            await Assert.That(result.Tuple[3].Boolean).IsTrue();
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TableMoveHandlesOverlappingRanges(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Default);
            DynValue snapshot = script.DoString(
                @"
                local values = { 1, 2, 3, 4 }
                table.move(values, 1, 3, 2)
                return table.concat(values, ',')
                "
            );

            await Assert.That(snapshot.String).IsEqualTo("1,1,2,3");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TableMoveDefaultsDestinationToSource(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Default);
            DynValue tuple = script.DoString(
                @"
                local values = { 0, 0, 3, 4 }
                table.move(values, 3, 4, 1)
                return values[1], values[2], values[3], values[4]
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(3);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(4);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(3);
            await Assert.That(tuple.Tuple[3].Number).IsEqualTo(4);
        }
    }
}
