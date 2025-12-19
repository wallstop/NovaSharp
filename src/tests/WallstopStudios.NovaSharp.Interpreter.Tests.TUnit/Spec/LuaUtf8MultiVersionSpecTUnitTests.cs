namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Multi-version spec harness for Lua 5.3+ §6.5 (utf8 library semantics).
    /// </summary>
    public sealed class LuaUtf8MultiVersionSpecTUnitTests : LuaSpecTestBase
    {
        private static readonly LuaCompatibilityVersion[] Lua53PlusVersions =
        {
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
            LuaCompatibilityVersion.Latest,
        };

        [global::TUnit.Core.Test]
        public async Task Utf8LibraryIsUnavailableBeforeLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52, CoreModulePresets.Default);
            DynValue utf8 = script.Globals.Get("utf8");
            await Assert.That(utf8.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task Utf8LenCountsCharactersAndFlagsInvalidSequencesAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = new Script(version, CoreModulePresets.Default);
                script.Globals.Set("sample", DynValue.NewString("héll😀"));
                script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

                DynValue len = script.DoString("return utf8.len(sample)");
                await Assert.That(len.Number).IsEqualTo(5);

                DynValue tuple = script.DoString("return utf8.len(invalid)");
                await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
                await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
                await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1);
            }
        }

        [global::TUnit.Core.Test]
        public async Task Utf8CodePointDecodesRequestedSliceAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = new Script(version, CoreModulePresets.Default);
                script.Globals.Set("word", DynValue.NewString("A😀€"));

                DynValue tuple = script.DoString("return utf8.codepoint(word, 1, #word)");

                await Assert.That(tuple.Tuple.Length).IsEqualTo(3);
                await Assert.That(tuple.Tuple[0].Number).IsEqualTo(65);
                await Assert.That(tuple.Tuple[1].Number).IsEqualTo(0x1F600);
                await Assert.That(tuple.Tuple[2].Number).IsEqualTo(0x20AC);
            }
        }

        [global::TUnit.Core.Test]
        public async Task Utf8OffsetEnforcesCharacterBoundariesAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = new Script(version, CoreModulePresets.Default);
                script.Globals.Set("word", DynValue.NewString("A😀B"));

                DynValue offsets = script.DoString(
                    @"
                    local forward2 = utf8.offset(word, 2)
                    local back1 = utf8.offset(word, -1)
                    local align = utf8.offset(word, 0, 3)
                    return forward2, back1, align
                    "
                );

                await Assert.That(offsets.Tuple[0].Number).IsEqualTo(2);
                await Assert.That(offsets.Tuple[1].Number).IsEqualTo(4);
                await Assert.That(offsets.Tuple[2].Number).IsEqualTo(2);

                DynValue invalid = script.DoString("return utf8.offset(word, 1, 3)");
                await Assert.That(invalid.IsNil()).IsTrue();
            }
        }

        [global::TUnit.Core.Test]
        public async Task Utf8CodesIteratesPositionsAndScalarsAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = new Script(version, CoreModulePresets.Default);
                script.Globals.Set("word", DynValue.NewString("A😀B"));

                DynValue summary = script.DoString(
                    @"
                    local parts = {}
                    for pos, cp in utf8.codes(word) do
                        parts[#parts + 1] = string.format('%d:%X', pos, cp)
                    end
                    return table.concat(parts, ',')
                    "
                );

                await Assert.That(summary.String).IsEqualTo("1:41,2:1F600,4:42");
            }
        }

        [global::TUnit.Core.Test]
        public async Task Utf8CharpatternMatchesManualDefinitionAcrossLua53PlusVersions()
        {
            foreach (LuaCompatibilityVersion version in Lua53PlusVersions)
            {
                Script script = new Script(version, CoreModulePresets.Default);
                DynValue pattern = script.DoString("return utf8.charpattern");

                const string Expected = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";
                await Assert.That(pattern.String).IsEqualTo(Expected);
            }
        }
    }
}
