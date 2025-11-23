namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
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
        [Description(
            "Lua 5.3 manual §6.5: utf8.len accepts negative i/j indices (counting from the end) and clamps zero to 1."
        )]
        public void Utf8LenHandlesNegativeRangeIndices()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("abcdef"));

            DynValue result = script.DoString(
                @"
                local fromEnd = utf8.len(word, -3, -1)
                local clamped = utf8.len(word, 0, 2)
                return fromEnd, clamped
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(3));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(2));
            });
        }

        [Test]
        [Description(
            "Lua 5.4 manual §6.5: utf8.len reports high surrogates not followed by a low surrogate as invalid UTF-8."
        )]
        public void Utf8LenReturnsNilForBrokenHighSurrogatePairs()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("broken", DynValue.NewString("\uD83DA"));

            DynValue tuple = script.DoString("return utf8.len(broken)");

            Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(tuple.Tuple[0].IsNil(), Is.True);
            Assert.That(tuple.Tuple[1].Number, Is.EqualTo(1));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.char builds strings from code points.")]
        public void Utf8CharBuildsStringsFromCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return utf8.char(0x41, 0x1F600, 0x20AC)");

            Assert.That(result.String, Is.EqualTo("A😀€"));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.codepoint returns the decoded scalars.")]
        public void Utf8CodePointReturnsCodePoints()
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
        [Description("Lua 5.3 manual §6.5: utf8.codepoint defaults j to i when omitted.")]
        public void Utf8CodePointDefaultsEndToStartWhenRangeIsOmitted()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("ABCDE"));

            DynValue values = script.DoString(
                @"
                local results = { utf8.codepoint(word, 2) }
                return #results, results[1]
                "
            );

            Assert.That(values.Tuple[0].Number, Is.EqualTo(1));
            Assert.That(values.Tuple[1].Number, Is.EqualTo(66));
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.codepoint returns no values when the range is empty."
        )]
        public void Utf8CodePointReturnsVoidWhenRangeHasNoCharacters()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.codepoint('abc', 5, 4)");

            Assert.That(result.Type, Is.EqualTo(DataType.Void));
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
            "Lua 5.3 manual §6.5: utf8.codes raises 'invalid UTF-8 code' for malformed sequences."
        )]
        public void Utf8CodesThrowsOnInvalidUtf8Sequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

            Assert.That(
                () => script.DoString("for _ in utf8.codes(invalid) do end"),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.EqualTo("invalid UTF-8 code")
            );
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: invoking utf8.codes with a nil control value starts iteration from the first rune."
        )]
        public void Utf8CodesIteratorAcceptsNilControlValue()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString(
                @"
                local iter, state = utf8.codes('ab')
                local pos, cp = iter(state, nil)
                return pos, cp
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(1));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(0x61));
            });
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.codes returns nil when the control value already points past the end of the string."
        )]
        public void Utf8CodesIteratorReturnsNilWhenControlIsPastEnd()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString(
                @"
                local iter, state = utf8.codes('abc')
                return iter(state, #state + 5)
                "
            );

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.codes raises 'invalid UTF-8 code' when the control value lands inside a surrogate pair."
        )]
        public void Utf8CodesIteratorThrowsWhenControlPointsInsideRune()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("emoji", DynValue.NewString("A😀B"));

            Assert.That(
                () =>
                    script.DoString(
                        @"
                        local iter, state = utf8.codes(emoji)
                        iter(state, 3)
                        "
                    ),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.EqualTo("invalid UTF-8 code")
            );
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

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset normalizes negative/zero boundaries to valid byte positions."
        )]
        public void Utf8OffsetNormalizesNegativeAndZeroBoundaries()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue offsets = script.DoString(
                @"
                local fromEnd = utf8.offset('abcd', 1, -1)
                local clamped = utf8.offset('abcd', 1, 0)
                return fromEnd, clamped
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(offsets.Tuple[0].Number, Is.EqualTo(4));
                Assert.That(offsets.Tuple[1].Number, Is.EqualTo(1));
            });
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.char rejects surrogate/out-of-range values.")]
        public void Utf8CharRejectsOutOfRangeCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.char(0x110000)")
            );

            Assert.That(ex.Message, Does.Contain("value out of range"));
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.char rejects surrogate code points even though they sit inside the BMP."
        )]
        public void Utf8CharRejectsSurrogateCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.char(0xD800)")
            );

            Assert.That(ex.Message, Does.Contain("value out of range"));
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.codepoint errors on malformed UTF-8 input.")]
        public void Utf8CodePointThrowsOnInvalidUtf8Sequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uDC00"));

            Assert.That(
                () => script.DoString("return utf8.codepoint(invalid)"),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.EqualTo("invalid UTF-8 code")
            );
        }

        [Test]
        [Description("Lua 5.3 manual §6.5: utf8.offset returns nil when the range is exhausted.")]
        public void Utf8OffsetReturnsNilWhenAdvancingPastEndOfString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('\U0001F600', 2)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset returns nil when stepping before the first rune."
        )]
        public void Utf8OffsetReturnsNilWhenMovingBeforeStart()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('ab', -3)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset with n = 0 rejects out-of-range boundaries."
        )]
        public void Utf8OffsetZeroReturnsNilWhenBoundaryIsOutsideString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('abc', 0, 10)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset returns nil when scanning backwards across a leading low surrogate."
        )]
        public void Utf8OffsetNegativeReturnsNilForLeadingLowSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("leadingLow", DynValue.NewString("\uDC00"));

            DynValue result = script.DoString("return utf8.offset(leadingLow, -1)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset returns nil when a low surrogate is preceded by an unrelated code unit."
        )]
        public void Utf8OffsetNegativeReturnsNilForStandaloneLowSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("mixedLow", DynValue.NewString("A\uDC00"));

            DynValue result = script.DoString("return utf8.offset(mixedLow, -1)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset returns nil when walking backwards across a dangling high surrogate."
        )]
        public void Utf8OffsetNegativeReturnsNilForDanglingHighSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("danglingHigh", DynValue.NewString("\uD83D"));

            DynValue result = script.DoString("return utf8.offset(danglingHigh, -1)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        [Description(
            "Lua 5.3 manual §6.5: utf8.offset(i = 0) returns nil for empty strings because no rune contains the boundary."
        )]
        public void Utf8OffsetZeroReturnsNilForEmptyString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("empty", DynValue.NewString(string.Empty));

            DynValue result = script.DoString("return utf8.offset(empty, 0)");

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
