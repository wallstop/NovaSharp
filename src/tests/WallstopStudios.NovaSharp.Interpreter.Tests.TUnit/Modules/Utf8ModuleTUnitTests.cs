namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class Utf8ModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task Utf8LibraryRespectsCompatibilityProfile()
        {
            Script lua52 = CreateScript(LuaCompatibilityVersion.Lua52);
            Script lua53 = CreateScript(LuaCompatibilityVersion.Lua53);

            await Assert.That(lua52.Globals.Get("utf8").IsNil()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(lua53.Globals.Get("utf8").Type)
                .IsEqualTo(DataType.Table)
                .ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: utf8.len counts characters within the provided range.
        [global::TUnit.Core.Test]
        public async Task Utf8LenCountsUtf8Characters()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            string sample = "héll😀";
            script.Globals.Set("sample", DynValue.NewString(sample));

            DynValue len = script.DoString("return utf8.len(sample)");
            await Assert.That(len.Number).IsEqualTo(5).ConfigureAwait(false);

            DynValue truncated = script.DoString("return utf8.len(sample, 5, 5)");
            await Assert.That(truncated.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(truncated.Tuple).IsNotNull().ConfigureAwait(false);
            await Assert.That(truncated.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(truncated.Tuple[1].Number).IsEqualTo(5).ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.len returns nil + position on invalid UTF-8.
        [global::TUnit.Core.Test]
        public async Task Utf8LenReturnsNilAndPositionForInvalidSequences()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("invalid", DynValue.NewString("\uD83D"));

            DynValue tuple = script.DoString("return utf8.len(invalid)");

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1).ConfigureAwait(false);
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

            await Assert.That(result.Tuple[0].Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: high surrogates not followed by low surrogates are invalid.
        [global::TUnit.Core.Test]
        public async Task Utf8LenReturnsNilForBrokenHighSurrogatePairs()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("broken", DynValue.NewString("\uD83DA"));

            DynValue tuple = script.DoString("return utf8.len(broken)");

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1).ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: utf8.char builds strings from code points.
        // Lua strings are byte sequences, so utf8.char produces UTF-8 encoded bytes.
        // 0x41='A' (1 byte), 0x1F600=😀 (4 bytes: F0 9F 98 80), 0x20AC=€ (3 bytes: E2 82 AC)
        [global::TUnit.Core.Test]
        public async Task Utf8CharBuildsStringsFromCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return utf8.char(0x41, 0x1F600, 0x20AC)");

            // Lua strings are byte sequences - verify the UTF-8 encoded bytes
            // Expected: 'A' (0x41), then 😀 (F0 9F 98 80), then € (E2 82 AC) = 8 bytes total
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String.Length).IsEqualTo(8).ConfigureAwait(false);

            // Verify exact byte values
            byte[] expectedBytes = { 0x41, 0xF0, 0x9F, 0x98, 0x80, 0xE2, 0x82, 0xAC };
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                await Assert
                    .That((byte)result.String[i])
                    .IsEqualTo(expectedBytes[i])
                    .ConfigureAwait(false);
            }
        }

        // Lua 5.3 manual §6.5: utf8.codepoint returns decoded scalars.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointReturnsCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀€"));

            DynValue values = script.DoString("return utf8.codepoint(word, 1, #word)");

            await Assert.That(values.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(values.Tuple[0].Number).IsEqualTo(65).ConfigureAwait(false);
            await Assert.That(values.Tuple[1].Number).IsEqualTo(0x1F600).ConfigureAwait(false);
            await Assert.That(values.Tuple[2].Number).IsEqualTo(0x20AC).ConfigureAwait(false);
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

            await Assert.That(values.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(values.Tuple[1].Number).IsEqualTo(66).ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.codepoint returns no values for reversed/empty ranges within bounds.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointReturnsVoidForReversedRange()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // Range where end < start but both are valid positions
            DynValue result = script.DoString("return utf8.codepoint('abc', 3, 1)");

            await Assert.That(result.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.codepoint throws for out-of-bounds positions.
        [global::TUnit.Core.Test]
        public async Task Utf8CodePointThrowsForOutOfBoundsRange()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // Position 5 is out of bounds for 3-character string
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.codepoint('abc', 5, 4)")
            );

            await Assert.That(exception.Message).Contains("out of bounds").ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.charpattern matches the documented regex.
        [global::TUnit.Core.Test]
        public async Task Utf8CharpatternMatchesLuaSpecification()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue pattern = script.DoString("return utf8.charpattern");

            const string Expected = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";
            await Assert.That(pattern.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(pattern.String).IsEqualTo(Expected).ConfigureAwait(false);
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

            await Assert.That(summary.String).IsEqualTo("1:41,2:1F600,4:42").ConfigureAwait(false);
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

            await Assert
                .That(exception.Message)
                .IsEqualTo("invalid UTF-8 code")
                .ConfigureAwait(false);
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

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0x61).ConfigureAwait(false);
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

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
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

            await Assert
                .That(exception.Message)
                .IsEqualTo("invalid UTF-8 code")
                .ConfigureAwait(false);
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

            await Assert.That(offsets.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(offsets.Tuple[1].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(offsets.Tuple[2].Number).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(offsets.Tuple[3].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(offsets.Tuple[4].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.offset fails when i is not on a boundary.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetRequiresCharacterBoundaries()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("word", DynValue.NewString("A😀B"));

            DynValue result = script.DoString("return utf8.offset(word, 1, 3)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.offset supports negative positions (counting from end).
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetSupportsNegativePositions()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue offsets = script.DoString(
                @"
                local fromEnd = utf8.offset('abcd', 1, -1)
                return fromEnd
                "
            );

            await Assert.That(offsets.Number).IsEqualTo(4).ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.offset throws for position 0 (out of bounds).
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetThrowsForPositionZero()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.offset('abcd', 1, 0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("position out of bounds")
                .ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: utf8.char rejects out-of-range code points (>0x10FFFF).
        // However, Lua 5.4 extends the range to accept code points up to 0x7FFFFFFF.
        [global::TUnit.Core.Test]
        public async Task Utf8CharRejectsOutOfRangeCodePointsLua53()
        {
            // Lua 5.3: rejects code points > 0x10FFFF
            Script script = CreateScript(LuaCompatibilityVersion.Lua53);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.char(0x110000)")
            );

            await Assert
                .That(exception.Message)
                .Contains("value out of range")
                .ConfigureAwait(false);
        }

        // Lua 5.4 accepts extended code points up to 0x7FFFFFFF
        [global::TUnit.Core.Test]
        public async Task Utf8CharAcceptsExtendedCodePointsLua54()
        {
            // Lua 5.4: accepts code points up to 0x7FFFFFFF
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // This should NOT throw in Lua 5.4
            DynValue result = script.DoString("return utf8.char(0x110000)");

            // Verify it returns a valid string with 4 bytes
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String.Length).IsEqualTo(4).ConfigureAwait(false);
        }

        // Verified against real Lua 5.3 - surrogates ARE accepted (contrary to common belief).
        // The only difference between 5.3 and 5.4 is the max code point value:
        // - Lua 5.3: 0 to 0x10FFFF (surrogates ARE allowed)
        // - Lua 5.4: 0 to 0x7FFFFFFF (extended range)
        [global::TUnit.Core.Test]
        public async Task Utf8CharAcceptsSurrogateCodePointsLua53()
        {
            // Verified: lua5.3 -e "print(utf8.char(0xD800))" works without error
            Script script = CreateScript(LuaCompatibilityVersion.Lua53);

            // This should NOT throw in Lua 5.3 - surrogates are accepted
            DynValue result = script.DoString("return utf8.char(0xD800)");

            // Verify it returns a valid string with 3 bytes (ED A0 80 for 0xD800)
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String.Length).IsEqualTo(3).ConfigureAwait(false);
        }

        // Lua 5.4 accepts surrogate code points (same as 5.3, but test is kept for completeness)
        [global::TUnit.Core.Test]
        public async Task Utf8CharAcceptsSurrogateCodePointsLua54()
        {
            // Lua 5.4: accepts surrogate code points
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // This should NOT throw in Lua 5.4
            DynValue result = script.DoString("return utf8.char(0xD800)");

            // Verify it returns a valid string with 3 bytes (ED A0 80 for 0xD800)
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String.Length).IsEqualTo(3).ConfigureAwait(false);
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

            await Assert
                .That(exception.Message)
                .IsEqualTo("invalid UTF-8 code")
                .ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: utf8.offset returns nil when advancing past the end.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetReturnsNilWhenAdvancingPastEndOfString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('\U0001F600', 2)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: utf8.offset returns nil when moving before the start.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetReturnsNilWhenMovingBeforeStart()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString("return utf8.offset('ab', -3)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Lua 5.4 manual §6.5: utf8.offset throws for positions outside valid range.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetThrowsForPositionOutOfBounds()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // Position 10 is outside the valid range [1, length+1] for a 3-character string
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return utf8.offset('abc', 0, 10)")
            );

            await Assert
                .That(exception.Message)
                .Contains("position out of bounds")
                .ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: negative offsets across leading low surrogates return nil.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNegativeReturnsNilForLeadingLowSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("leadingLow", DynValue.NewString("\uDC00"));

            DynValue result = script.DoString("return utf8.offset(leadingLow, -1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: negative offsets across standalone low surrogates return nil.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNegativeReturnsNilForStandaloneLowSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("mixedLow", DynValue.NewString("A\uDC00"));

            DynValue result = script.DoString("return utf8.offset(mixedLow, -1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: negative offsets across dangling high surrogates return nil.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetNegativeReturnsNilForDanglingHighSurrogate()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("danglingHigh", DynValue.NewString("\uD83D"));

            DynValue result = script.DoString("return utf8.offset(danglingHigh, -1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Lua 5.3 manual §6.5: utf8.offset(i = 0) returns nil for empty strings.
        [global::TUnit.Core.Test]
        public async Task Utf8OffsetZeroReturnsNilForEmptyString()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("empty", DynValue.NewString(string.Empty));

            DynValue result = script.DoString("return utf8.offset(empty, 0)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        // Data-driven tests for utf8.offset position bounds validation
        // Lua 5.4 manual §6.5: The third argument i defaults to 1 when n is non-negative and to #s + 1 otherwise.
        // If i is outside the range [1, #s + 1], the function fails with an error.
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("abc", 1, 0, "position out of bounds")] // position 0 is invalid
        [global::TUnit.Core.Arguments("abc", 1, -5, "position out of bounds")] // position -5 => #s+1+(-5)=-1 for "abc"(len 3) = -1, out of bounds
        [global::TUnit.Core.Arguments("abc", 1, 5, "position out of bounds")] // position 5 > 4 (#s+1) for "abc"
        [global::TUnit.Core.Arguments("abc", 1, 10, "position out of bounds")] // position 10 >> length+1
        [global::TUnit.Core.Arguments("", 1, 2, "position out of bounds")] // position 2 > 1 (#s+1) for ""
        public async Task Utf8OffsetThrowsForInvalidPositions(
            string input,
            int n,
            int pos,
            string expectedError
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("s", DynValue.NewString(input));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return utf8.offset(s, {n}, {pos})")
            );

            await Assert.That(exception.Message).Contains(expectedError).ConfigureAwait(false);
        }

        // Data-driven tests for utf8.offset valid negative positions
        // Negative positions are valid - they count from end: -1 = last position, -n = position #s+1-n
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("abc", 1, -1, 3)] // -1 => position 3 (last char position)
        [global::TUnit.Core.Arguments("abc", 1, -2, 2)] // -2 => position 2
        [global::TUnit.Core.Arguments("abc", 1, -3, 1)] // -3 => position 1 (first char position)
        [global::TUnit.Core.Arguments("abc", 0, -1, 3)] // n=0 means "find boundary at or before position"
        public async Task Utf8OffsetAcceptsValidNegativePositions(
            string input,
            int n,
            int pos,
            double expectedResult
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            script.Globals.Set("s", DynValue.NewString(input));

            DynValue result = script.DoString($"return utf8.offset(s, {n}, {pos})");

            await Assert.That(result.Number).IsEqualTo(expectedResult).ConfigureAwait(false);
        }

        // Data-driven tests for utf8.char validation by Lua version
        // Lua 5.3: accepts 0x0 to 0x10FFFF (including surrogates)
        // Lua 5.4: accepts 0x0 to 0x7FFFFFFF (extended range)
        // Only values ABOVE Unicode max are rejected in Lua 5.3
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x110000)] // just above Unicode max (0x10FFFF)
        [global::TUnit.Core.Arguments(0x200000)] // well above Unicode max
        public async Task Utf8CharRejectsInvalidCodePointsLua53(int codePoint)
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua53);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return utf8.char(0x{codePoint:X})")
            );

            await Assert
                .That(exception.Message)
                .Contains("value out of range")
                .ConfigureAwait(false);
        }

        // Data-driven tests for utf8.char acceptance of surrogates in Lua 5.3
        // Verified against real Lua 5.3 - surrogates ARE accepted
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0xD800, 3)] // surrogate start - 3 bytes (ED A0 80)
        [global::TUnit.Core.Arguments(0xD8FF, 3)] // surrogate middle - 3 bytes
        [global::TUnit.Core.Arguments(0xDFFF, 3)] // surrogate end - 3 bytes
        public async Task Utf8CharAcceptsSurrogateCodePointsLua53DataDriven(
            int codePoint,
            int expectedByteLength
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua53);

            DynValue result = script.DoString($"return utf8.char(0x{codePoint:X})");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert
                .That(result.String.Length)
                .IsEqualTo(expectedByteLength)
                .ConfigureAwait(false);
        }

        // Data-driven tests for utf8.char acceptance in Lua 5.4
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0xD800, 3)] // surrogate - 3 bytes (ED A0 80)
        [global::TUnit.Core.Arguments(0xDFFF, 3)] // surrogate end - 3 bytes
        [global::TUnit.Core.Arguments(0x110000, 4)] // above Unicode max - 4 bytes
        [global::TUnit.Core.Arguments(0x1FFFFF, 4)] // max 4-byte extended UTF-8
        [global::TUnit.Core.Arguments(0x200000, 5)] // starts 5-byte range
        [global::TUnit.Core.Arguments(0x3FFFFFF, 5)] // max 5-byte extended UTF-8
        [global::TUnit.Core.Arguments(0x4000000, 6)] // starts 6-byte range
        public async Task Utf8CharAcceptsExtendedCodePointsLua54DataDriven(
            int codePoint,
            int expectedByteLength
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString($"return utf8.char(0x{codePoint:X})");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert
                .That(result.String.Length)
                .IsEqualTo(expectedByteLength)
                .ConfigureAwait(false);
        }

        // Data-driven tests for utf8.char common valid code points (all versions)
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0x00, 1)] // NUL - 1 byte
        [global::TUnit.Core.Arguments(0x41, 1)] // 'A' - 1 byte
        [global::TUnit.Core.Arguments(0x7F, 1)] // DEL - max 1-byte
        [global::TUnit.Core.Arguments(0x80, 2)] // first 2-byte
        [global::TUnit.Core.Arguments(0x7FF, 2)] // max 2-byte
        [global::TUnit.Core.Arguments(0x800, 3)] // first 3-byte
        [global::TUnit.Core.Arguments(0x20AC, 3)] // € - 3 bytes
        [global::TUnit.Core.Arguments(0xFFFF, 3)] // max BMP (3-byte)
        [global::TUnit.Core.Arguments(0x10000, 4)] // first 4-byte (first supplementary)
        [global::TUnit.Core.Arguments(0x1F600, 4)] // 😀 - 4 bytes
        [global::TUnit.Core.Arguments(0x10FFFF, 4)] // max Unicode - 4 bytes
        public async Task Utf8CharEncodesValidCodePointsCorrectly(
            int codePoint,
            int expectedByteLength
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString($"return utf8.char(0x{codePoint:X})");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert
                .That(result.String.Length)
                .IsEqualTo(expectedByteLength)
                .ConfigureAwait(false);
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
