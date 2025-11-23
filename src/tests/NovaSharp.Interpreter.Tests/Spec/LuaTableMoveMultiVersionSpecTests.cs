namespace NovaSharp.Interpreter.Tests.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    /// <summary>
    /// Multi-version spec harness for Lua 5.3+ §6.6 (table.move semantics and compatibility gating).
    /// </summary>
    [TestFixture]
    public sealed class LuaTableMoveMultiVersionSpecTests : LuaSpecTestBase
    {
        private static readonly LuaCompatibilityVersion[] Lua53PlusVersions =
        {
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
            LuaCompatibilityVersion.Latest,
        };

        [Test]
        [Description("Lua 5.2 manual §6.6: table.move does not exist prior to Lua 5.3.")]
        public void TableMoveIsUnavailableBeforeLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52, CoreModules.PresetDefault);
            DynValue table = script.Globals.Get("table");
            Assert.That(table.Type, Is.EqualTo(DataType.Table));
            DynValue move = table.Table.Get("move");
            Assert.That(move.IsNil(), Is.True);
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.6: table.move copies the inclusive range and returns the destination table."
        )]
        public void TableMoveReturnsDestinationTable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            DynValue result = script.DoString(
                @"
                local src = { 'a', 'b', 'c' }
                local dest = {}
                local returned = table.move(src, 1, #src, 1, dest)
                return dest[1], dest[2], dest[3], returned == dest
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].String, Is.EqualTo("a"));
                Assert.That(result.Tuple[1].String, Is.EqualTo("b"));
                Assert.That(result.Tuple[2].String, Is.EqualTo("c"));
                Assert.That(result.Tuple[3].Boolean, Is.True);
            });
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.6: table.move correctly handles overlapping ranges by copying in the proper order."
        )]
        public void TableMoveHandlesOverlappingRanges(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            DynValue snapshot = script.DoString(
                @"
                local values = { 1, 2, 3, 4 }
                table.move(values, 1, 3, 2)
                return table.concat(values, ',')
                "
            );

            Assert.That(snapshot.String, Is.EqualTo("1,1,2,3"));
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.6: omitting the destination table argument defaults to the source table."
        )]
        public void TableMoveDefaultsDestinationToSource(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            DynValue tuple = script.DoString(
                @"
                local values = { 0, 0, 3, 4 }
                table.move(values, 3, 4, 1)
                return values[1], values[2], values[3], values[4]
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(3));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(4));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(3));
                Assert.That(tuple.Tuple[3].Number, Is.EqualTo(4));
            });
        }
    }
}
