namespace NovaSharp.Interpreter.Tests.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    /// <summary>
    /// Multi-version spec harness for Lua 5.3+ §6.5 (utf8 library semantics).
    /// </summary>
    [TestFixture]
    public sealed class LuaUtf8MultiVersionSpecTests : LuaSpecTestBase
    {
        private static readonly LuaCompatibilityVersion[] Lua53PlusVersions =
        {
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
            LuaCompatibilityVersion.Latest,
        };

        [Test]
        [Description("Lua 5.2 manual §6.4: utf8 library is unavailable before Lua 5.3.")]
        public void Utf8LibraryIsUnavailableBeforeLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52, CoreModules.PresetDefault);
            DynValue utf8 = script.Globals.Get("utf8");
            Assert.That(utf8.IsNil(), Is.True);
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.5: utf8.len counts characters and returns nil,pos on invalid input."
        )]
        public void Utf8LenCountsCharactersAndFlagsInvalidSequences(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            script.Globals.Set("sample", DynValue.NewString("héll😀"));
            script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

            DynValue len = script.DoString("return utf8.len(sample)");
            Assert.That(len.Number, Is.EqualTo(5));

            DynValue tuple = script.DoString("return utf8.len(invalid)");
            Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(tuple.Tuple[0].IsNil(), Is.True);
            Assert.That(tuple.Tuple[1].Number, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.5: utf8.codepoint decodes the requested slice and defaults j=i."
        )]
        public void Utf8CodepointDecodesRequestedSlice(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            script.Globals.Set("word", DynValue.NewString("A😀€"));

            DynValue tuple = script.DoString("return utf8.codepoint(word, 1, #word)");

            Assert.That(tuple.Tuple.Length, Is.EqualTo(3));
            Assert.That(tuple.Tuple[0].Number, Is.EqualTo(65));
            Assert.That(tuple.Tuple[1].Number, Is.EqualTo(0x1F600));
            Assert.That(tuple.Tuple[2].Number, Is.EqualTo(0x20AC));
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset navigates forward/backward and errors when i is mid-sequence."
        )]
        public void Utf8OffsetEnforcesCharacterBoundaries(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            script.Globals.Set("word", DynValue.NewString("A😀B"));

            DynValue offsets = script.DoString(
                @"
                local forward2 = utf8.offset(word, 2)
                local back1 = utf8.offset(word, -1)
                local align = utf8.offset(word, 0, 3)
                return forward2, back1, align
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(offsets.Tuple[0].Number, Is.EqualTo(2));
                Assert.That(offsets.Tuple[1].Number, Is.EqualTo(4));
                Assert.That(offsets.Tuple[2].Number, Is.EqualTo(2));
            });

            DynValue invalid = script.DoString("return utf8.offset(word, 1, 3)");
            Assert.That(invalid.IsNil(), Is.True);
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description("Lua 5.3 manual §6.5: utf8.codes iterates positions and scalars in order.")]
        public void Utf8CodesIteratesPositionsAndScalars(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
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

            Assert.That(summary.String, Is.EqualTo("1:41,2:1F600,4:42"));
        }

        [TestCaseSource(nameof(Lua53PlusVersions))]
        [Description("Lua 5.3 manual §6.5: utf8.charpattern exposes the canonical pattern string.")]
        public void Utf8CharpatternMatchesManualDefinition(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModules.PresetDefault);
            DynValue pattern = script.DoString("return utf8.charpattern");

            const string Expected = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";
            Assert.That(pattern.String, Is.EqualTo(Expected));
        }
    }
}
