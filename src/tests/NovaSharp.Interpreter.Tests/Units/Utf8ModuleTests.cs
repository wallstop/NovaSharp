namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class Utf8ModuleTests
    {
        [Test]
        public void Utf8LibraryRespectsCompatibilityProfile()
        {
            Script lua52 = CreateScript(LuaCompatibilityVersion.Lua52);
            Assert.That(lua52.Globals.Get("utf8").IsNil(), Is.True);

            Script lua53 = CreateScript(LuaCompatibilityVersion.Lua53);
            Assert.That(lua53.Globals.Get("utf8").Type, Is.EqualTo(DataType.Table));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.len counts characters within the provided range.")]
        public void Utf8LenCountsUtf8Characters()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            string sample = "héll😀";
            script.Globals.Set("sample", DynValue.NewString(sample));

            DynValue len = script.DoString("return utf8.len(sample)");
            Assert.That(len.Number, Is.EqualTo(5));

            DynValue truncated = script.DoString("return utf8.len(sample, 5, 5)");
            Assert.That(
                truncated.Type,
                Is.EqualTo(DataType.Tuple),
                "utf8.len should return a tuple on invalid slices."
            );
            Assert.That(truncated.Tuple, Is.Not.Null);
            Assert.That(truncated.Tuple[0].IsNil(), Is.True);
            Assert.That(truncated.Tuple[1].Number, Is.EqualTo(5));
        }

        [Test]
        [Description("Lua 5.4 manual §6.5: utf8.len returns nil + position on invalid UTF-8.")]
        public void Utf8LenReturnsNilAndPositionForInvalidSequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

            DynValue tuple = script.DoString("return utf8.len(invalid)");

            Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(tuple.Tuple[0].IsNil(), Is.True);
            Assert.That(tuple.Tuple[1].Number, Is.EqualTo(1));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.char builds strings from code points.")]
        public void Utf8CharBuildsStringsFromCodepoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return utf8.char(0x41, 0x1F600, 0x20AC)");

            Assert.That(result.String, Is.EqualTo("A😀€"));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.codepoint returns the decoded scalars.")]
        public void Utf8CodepointReturnsCodepoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀€"));

            DynValue values = script.DoString("return utf8.codepoint(word, 1, #word)");

            Assert.That(values.Tuple.Length, Is.EqualTo(3));
            Assert.That(values.Tuple[0].Number, Is.EqualTo(65));
            Assert.That(values.Tuple[1].Number, Is.EqualTo(0x1F600));
            Assert.That(values.Tuple[2].Number, Is.EqualTo(0x20AC));
        }

        [Test]
        [Description("Lua 5.4 manual §6.5: utf8.charpattern is exposed verbatim.")]
        public void Utf8CharpatternMatchesLuaSpecification()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue pattern = script.DoString("return utf8.charpattern");

            const string Expected = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";
            Assert.That(pattern.Type, Is.EqualTo(DataType.String));
            Assert.That(pattern.String, Is.EqualTo(Expected));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.codes iterates positions and scalars.")]
        public void Utf8CodesIteratesPositionsAndScalars()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
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

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset navigates forward and backward across characters."
        )]
        public void Utf8OffsetNavigatesBoundaries()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀B"));

            DynValue offsets = script.DoString(
                @"
                local forward1 = utf8.offset(word, 1)
                local forward2 = utf8.offset(word, 2)
                local back1 = utf8.offset(word, -1)
                local back2 = utf8.offset(word, -2)
                local align = utf8.offset(word, 0, 3)
                return forward1, forward2, back1, back2, align
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(offsets.Tuple[0].Number, Is.EqualTo(1));
                Assert.That(offsets.Tuple[1].Number, Is.EqualTo(2));
                Assert.That(offsets.Tuple[2].Number, Is.EqualTo(4));
                Assert.That(offsets.Tuple[3].Number, Is.EqualTo(2));
                Assert.That(offsets.Tuple[4].Number, Is.EqualTo(2));
            });
        }

        [Test]
        [Description("Lua 5.4 manual §6.5: utf8.offset fails when i is not on a boundary.")]
        public void Utf8OffsetRequiresCharacterBoundaries()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀B"));

            DynValue result = script.DoString("return utf8.offset(word, 1, 3)");

            Assert.That(result.IsNil(), Is.True);
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(
                CoreModules.Basic | CoreModules.StringLib | CoreModules.Table,
                options
            );
        }
    }
}
