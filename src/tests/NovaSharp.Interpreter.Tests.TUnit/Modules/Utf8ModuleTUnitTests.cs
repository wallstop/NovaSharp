#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class Utf8ModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task Utf8LibraryRespectsCompatibilityProfile()
        {
            Script lua52 = CreateScript(LuaCompatibilityVersion.Lua52);
            Script lua53 = CreateScript(LuaCompatibilityVersion.Lua53);

            await Assert.That(lua52.Globals.Get("utf8").IsNil()).IsTrue();
            await Assert.That(lua53.Globals.Get("utf8").Type).IsEqualTo(DataType.Table);
        }

        // Lua 5.3 manual §6.5: utf8.len counts characters within the provided range.
        [global::TUnit.Core.Test]
        public async Task Utf8LenCountsUtf8Characters()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            string sample = "héll😀";
            script.Globals.Set("sample", DynValue.NewString(sample));

            DynValue len = script.DoString("return utf8.len(sample)");
            await Assert.That(len.Number).IsEqualTo(5);

            DynValue truncated = script.DoString("return utf8.len(sample, 5, 5)");
            await Assert.That(truncated.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(truncated.Tuple).IsNotNull();
            await Assert.That(truncated.Tuple[0].IsNil()).IsTrue();
            await Assert.That(truncated.Tuple[1].Number).IsEqualTo(5);
        }

        // Lua 5.4 manual §6.5: utf8.len returns nil + position on invalid UTF-8.
        [global::TUnit.Core.Test]
        public async Task Utf8LenReturnsNilAndPositionForInvalidSequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

            DynValue tuple = script.DoString("return utf8.len(invalid)");

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1);
        }

        // Lua 5.3 manual §6.5: utf8.len accepts negative i/j indices and clamps zero to 1.
        [global::TUnit.Core.Test]
        public async Task Utf8LenHandlesNegativeRangeIndices()
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

            await Assert.That(result.Tuple[0].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2);
        }

        // Lua 5.4 manual §6.5: high surrogates not followed by low surrogates are invalid.
        [global::TUnit.Core.Test]
        public async Task Utf8LenReturnsNilForBrokenHighSurrogatePairs()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("broken", DynValue.NewString("\uD83DA"));

            DynValue tuple = script.DoString("return utf8.len(broken)");

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1);
        }

        // Lua 5.3 manual §6.5: utf8.char builds strings from code points.
        [global::TUnit.Core.Test]
        public async Task Utf8CharBuildsStringsFromCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return utf8.char(0x41, 0x1F600, 0x20AC)");

            await Assert.That(result.String).IsEqualTo("A😀€");
        }

        // Lua 5.3 manual §6.5: utf8.codepoint returns decoded scalars.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointReturnsCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀€"));

            DynValue values = script.DoString("return utf8.codepoint(word, 1, #word)");

            await Assert.That(values.Tuple.Length).IsEqualTo(3);
            await Assert.That(values.Tuple[0].Number).IsEqualTo(65);
            await Assert.That(values.Tuple[1].Number).IsEqualTo(0x1F600);
            await Assert.That(values.Tuple[2].Number).IsEqualTo(0x20AC);
        }

        // Lua 5.3 manual §6.5: utf8.codepoint defaults j to i when omitted.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointDefaultsEndToStartWhenRangeIsOmitted()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("ABCDE"));

            DynValue values = script.DoString(
                @"
                local results = { utf8.codepoint(word, 2) }
                return #results, results[1]
                "
            );

            await Assert.That(values.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(values.Tuple[1].Number).IsEqualTo(66);
        }

        // Lua 5.3 manual §6.5: utf8.codepoint returns no values for empty ranges.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointReturnsVoidWhenRangeHasNoCharacters()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.codepoint('abc', 5, 4)");

            await Assert.That(result.Type).IsEqualTo(DataType.Void);
        }

        // Lua 5.4 manual §6.5: utf8.charpattern matches the documented regex.
        [global::TUnit.Core.Test]
        public async Task Utf8CharpatternMatchesLuaSpecification()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue pattern = script.DoString("return utf8.charpattern");

            const string Expected = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";
            await Assert.That(pattern.Type).IsEqualTo(DataType.String);
            await Assert.That(pattern.String).IsEqualTo(Expected);
        }

        // Lua 5.3 manual §6.5: utf8.codes emits positions and scalars.
        [global::TUnit.Core.Test]
        public async Task Utf8CodesIteratesPositionsAndScalars()
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

            await Assert.That(summary.String).IsEqualTo("1:41,2:1F600,4:42");
        }

        // Lua 5.3 manual §6.5: utf8.codes rejects invalid UTF-8.
        [global::TUnit.Core.Test]
        public async Task Utf8CodesThrowsOnInvalidUtf8Sequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("for _ in utf8.codes(invalid) do end")
            );

            await Assert.That(exception.Message).IsEqualTo("invalid UTF-8 code");
        }

        // Lua 5.3 manual §6.5: utf8.codes accepts nil control values.
        [global::TUnit.Core.Test]
        public async Task Utf8CodesIteratorAcceptsNilControlValue()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString(
                @"
                local iter, state = utf8.codes('ab')
                local pos, cp = iter(state, nil)
                return pos, cp
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0x61);
        }

        // Lua 5.3 manual §6.5: utf8.codes returns nil once the control value passes the end.
        [global::TUnit.Core.Test]
        public async Task Utf8CodesIteratorReturnsNilWhenControlIsPastEnd()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString(
                @"
                local iter, state = utf8.codes('abc')
                return iter(state, #state + 5)
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: utf8.codes throws when the control points inside a rune.
        [global::TUnit.Core.Test]
        public async Task Utf8CodesIteratorThrowsWhenControlPointsInsideRune()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("emoji", DynValue.NewString("A😀B"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local iter, state = utf8.codes(emoji)
                    iter(state, 3)
                    "
                )
            );

            await Assert.That(exception.Message).IsEqualTo("invalid UTF-8 code");
        }

        // Lua 5.3 manual §6.5: utf8.offset navigates forward/backward across boundaries.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNavigatesBoundaries()
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

            await Assert.That(offsets.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(offsets.Tuple[1].Number).IsEqualTo(2);
            await Assert.That(offsets.Tuple[2].Number).IsEqualTo(4);
            await Assert.That(offsets.Tuple[3].Number).IsEqualTo(2);
            await Assert.That(offsets.Tuple[4].Number).IsEqualTo(2);
        }

        // Lua 5.4 manual §6.5: utf8.offset fails when i is not on a boundary.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetRequiresCharacterBoundaries()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀B"));

            DynValue result = script.DoString("return utf8.offset(word, 1, 3)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: utf8.offset normalizes negative/zero boundaries.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNormalizesNegativeAndZeroBoundaries()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue offsets = script.DoString(
                @"
                local fromEnd = utf8.offset('abcd', 1, -1)
                local clamped = utf8.offset('abcd', 1, 0)
                return fromEnd, clamped
                "
            );

            await Assert.That(offsets.Tuple[0].Number).IsEqualTo(4);
            await Assert.That(offsets.Tuple[1].Number).IsEqualTo(1);
        }

        // Lua 5.3 manual §6.5: utf8.char rejects out-of-range code points.
        [global::TUnit.Core.Test]
        public async Task Utf8CharRejectsOutOfRangeCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.char(0x110000)")
            );

            await Assert.That(exception.Message).Contains("value out of range");
        }

        // Lua 5.3 manual §6.5: utf8.char rejects surrogate code points.
        [global::TUnit.Core.Test]
        public async Task Utf8CharRejectsSurrogateCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.char(0xD800)")
            );

            await Assert.That(exception.Message).Contains("value out of range");
        }

        // Lua 5.3 manual §6.5: utf8.codepoint errors on malformed UTF-8.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointThrowsOnInvalidUtf8Sequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uDC00"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.codepoint(invalid)")
            );

            await Assert.That(exception.Message).IsEqualTo("invalid UTF-8 code");
        }

        // Lua 5.3 manual §6.5: utf8.offset returns nil when advancing past the end.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetReturnsNilWhenAdvancingPastEndOfString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('\U0001F600', 2)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: utf8.offset returns nil when moving before the start.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetReturnsNilWhenMovingBeforeStart()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('ab', -3)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: utf8.offset with n = 0 rejects out-of-range boundaries.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetZeroReturnsNilWhenBoundaryIsOutsideString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('abc', 0, 10)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: negative offsets across leading low surrogates return nil.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNegativeReturnsNilForLeadingLowSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("leadingLow", DynValue.NewString("\uDC00"));

            DynValue result = script.DoString("return utf8.offset(leadingLow, -1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: negative offsets across standalone low surrogates return nil.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNegativeReturnsNilForStandaloneLowSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("mixedLow", DynValue.NewString("A\uDC00"));

            DynValue result = script.DoString("return utf8.offset(mixedLow, -1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: negative offsets across dangling high surrogates return nil.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNegativeReturnsNilForDanglingHighSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("danglingHigh", DynValue.NewString("\uD83D"));

            DynValue result = script.DoString("return utf8.offset(danglingHigh, -1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        // Lua 5.3 manual §6.5: utf8.offset(i = 0) returns nil for empty strings.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetZeroReturnsNilForEmptyString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("empty", DynValue.NewString(string.Empty));

            DynValue result = script.DoString("return utf8.offset(empty, 0)");

            await Assert.That(result.IsNil()).IsTrue();
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
#pragma warning restore CA2007
