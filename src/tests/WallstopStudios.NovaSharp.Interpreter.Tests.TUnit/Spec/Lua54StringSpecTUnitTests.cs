namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Spec-oriented acceptance tests derived from the Lua 5.4 reference manual (§6.4 String Manipulation).
    /// </summary>
    public sealed class Lua54StringSpecTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task StringByteDefaultsToFirstCharacter()
        {
            DynValue result = Evaluate("return string.byte('Lua')");
            await Assert.That(result.Number).IsEqualTo(76);
        }

        [global::TUnit.Core.Test]
        public async Task StringByteSupportsRanges()
        {
            DynValue result = Evaluate("return string.byte('Lua', 2, 3)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(117);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(97);
        }

        [global::TUnit.Core.Test]
        public async Task StringByteAcceptsNegativeIndices()
        {
            DynValue result = Evaluate("return string.byte('Lua', -1)");
            await Assert.That(result.Number).IsEqualTo(97);
        }

        [global::TUnit.Core.Test]
        public async Task StringByteReturnsNilForOutOfRangeIndex()
        {
            DynValue result = Evaluate("return string.byte('abc', 4)");
            await Assert.That(result.IsNil()).IsTrue();
        }

        /// <remarks>
        /// Lua 5.4 §6.4 string.byte — indices must have an exact integer representation.
        /// Non-integer floats (1.9) throw "number has no integer representation".
        /// This is a change from Lua 5.1/5.2 which silently truncated.
        /// </remarks>
        [global::TUnit.Core.Test]
        public async Task StringByteErrorsOnNonIntegerIndex()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Evaluate("return string.byte('Lua', 1.9)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <remarks>Lua 5.4 accepts integral floats like 2.0 as valid indices.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringByteAcceptsIntegralFloatIndices()
        {
            DynValue result = Evaluate("return string.byte('Lua', 2.0)");
            await Assert.That(result.Number).IsEqualTo(117);
        }

        /// <remarks>Lua 5.4 §6.4 string.byte — empty ranges yield no values; NovaSharp currently returns void.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringByteReturnsEmptyTupleWhenRangeIsEmpty()
        {
            DynValue result = Evaluate("return string.byte('Lua', 3, 2)");
            await Assert.That(result.IsVoid()).IsTrue();

            DynValue count = Evaluate("return select('#', string.byte('Lua', 3, 2))");
            await Assert.That(count.Number).IsEqualTo(0);
        }

        /// <remarks>Lua 5.4 §6.4 string.byte — indices follow string.sub clamping rules.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringByteClampsIndicesWithinBounds()
        {
            DynValue result = Evaluate("return string.byte('Lua', -10, 10)");

            await Assert.That(result.Tuple.Length).IsEqualTo(3);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(76);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(117);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(97);
        }

        [global::TUnit.Core.Test]
        public async Task StringCharConcatenatesByteValues()
        {
            DynValue result = Evaluate("return string.char(97, 98, 99)");
            await Assert.That(result.String).IsEqualTo("abc");
        }

        /// <remarks>Lua 5.4 §6.4 string.char — values outside 0-255 raise "value out of range" error.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringCharErrorsOnOutOfRangeValues()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Evaluate("return string.char(-1, 256)")
            );

            await Assert.That(exception.Message).Contains("value out of range");
        }

        /// <remarks>
        /// Lua 5.4 §6.4 string.char — Non-integer floats throw "number has no integer representation".
        /// Truncation behavior was only valid in Lua 5.1/5.2.
        /// </remarks>
        [global::TUnit.Core.Test]
        public async Task StringCharErrorsOnNonIntegerFloat()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                Evaluate("return string.char(65.8)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <remarks>
        /// Lua 5.4 §6.4 string.char — Integer floats like 65.0 are valid.
        /// </remarks>
        [global::TUnit.Core.Test]
        public async Task StringCharAcceptsIntegerFloat()
        {
            DynValue result = Evaluate("return string.char(65.0)");
            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        /// <remarks>Lua 5.4 §6.4 string.char — zero arguments yield an empty string.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringCharWithoutArgumentsReturnsEmptyString()
        {
            DynValue result = Evaluate("return string.char()");
            await Assert.That(result.String).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task StringLenReturnsLengthInBytes()
        {
            DynValue result = Evaluate("return string.len('Lua')");
            await Assert.That(result.Number).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task StringSubExtractsInclusiveRange()
        {
            DynValue result = Evaluate("return string.sub('abcdefg', 2, 4)");
            await Assert.That(result.String).IsEqualTo("bcd");
        }

        [global::TUnit.Core.Test]
        public async Task StringSubSupportsNegativeBounds()
        {
            DynValue result = Evaluate("return string.sub('abcdefg', -3)");
            await Assert.That(result.String).IsEqualTo("efg");
        }

        /// <remarks>Lua 5.4 §6.4 string.sub — starting index past the end returns the empty string.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringSubReturnsEmptyStringWhenStartExceedsLength()
        {
            DynValue result = Evaluate("return string.sub('abc', 5, 7)");
            await Assert.That(result.String).IsEqualTo(string.Empty);
        }

        /// <remarks>Lua 5.4 §6.4 string.sub — indices are clamped to the string bounds.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringSubClampsIndicesToStringBounds()
        {
            DynValue result = Evaluate("return string.sub('abcdef', 0, 3)");
            await Assert.That(result.String).IsEqualTo("abc");
        }

        [global::TUnit.Core.Test]
        public async Task StringFindReturnsStartAndEndPositions()
        {
            DynValue result = Evaluate("return string.find('hello world', 'world')");

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(7);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(11);
        }

        /// <remarks>Lua 5.4 §6.4 string.find — plain flag disables pattern semantics.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringFindPlainSearchTreatsPatternLiterally()
        {
            DynValue result = Evaluate("return string.find('a^b', '^b', 1, true)");

            await Assert.That(result.Tuple[0].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3);
        }

        /// <remarks>Lua 5.4 §6.4 string.find — captures are returned after the indices.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringFindReturnsCapturedSubstrings()
        {
            DynValue result = Evaluate("return string.find('hello', 'l(l)o')");

            await Assert.That(result.Tuple.Length).IsEqualTo(3);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(5);
            await Assert.That(result.Tuple[2].String).IsEqualTo("l");
        }

        [global::TUnit.Core.Test]
        public async Task StringRepSupportsOptionalSeparator()
        {
            DynValue result = Evaluate("return string.rep('ab', 3, '-')");
            await Assert.That(result.String).IsEqualTo("ab-ab-ab");
        }

        /// <remarks>Lua 5.4 §6.4 string.rep — non-positive counts return the empty string.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringRepZeroCountReturnsEmptyString()
        {
            DynValue result = Evaluate("return string.rep('text', 0)");
            await Assert.That(result.String).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task StringReverseFlipsByteOrder()
        {
            DynValue result = Evaluate("return string.reverse('Lua')");
            await Assert.That(result.String).IsEqualTo("auL");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesNumericPlaceholders()
        {
            DynValue result = Evaluate("return string.format('%.2f', 3.14159)");
            await Assert.That(result.String).IsEqualTo("3.14");
        }

        /// <remarks>Lua 5.4 §6.4 string.format — %q escapes quotes and control characters.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringFormatPercentQEscapesControlSequences()
        {
            DynValue result = Evaluate(
                "return string.byte(string.format('%q', string.char(76, 117, 97, 10)), 5, 6)"
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(92);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(110);
        }

        /// <remarks>Lua 5.4 §6.4 string.format — width and zero padding for integers.</remarks>
        [global::TUnit.Core.Test]
        public async Task StringFormatZeroPadsIntegers()
        {
            DynValue result = Evaluate("return string.format('%02d:%02d', 7, 5)");
            await Assert.That(result.String).IsEqualTo("07:05");
        }

        private static DynValue Evaluate(string lua)
        {
            Script script = new Script(CoreModulePresets.Complete);
            return script.DoString(lua);
        }
    }
}
