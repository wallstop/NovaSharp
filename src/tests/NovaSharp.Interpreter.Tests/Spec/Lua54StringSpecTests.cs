namespace NovaSharp.Interpreter.Tests.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    /// <summary>
    /// Spec-oriented acceptance tests derived from the official Lua 5.4 reference manual,
    /// §6.4 (String Manipulation).
    /// </summary>
    [TestFixture]
    public sealed class Lua54StringSpecTests
    {
        [Test]
        public void StringByteDefaultsToFirstCharacter()
        {
            DynValue result = Evaluate("return string.byte('Lua')");
            Assert.That(result.Number, Is.EqualTo(76));
        }

        [Test]
        public void StringByteSupportsRanges()
        {
            DynValue result = Evaluate("return string.byte('Lua', 2, 3)");

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.That(result.Tuple[0].Number, Is.EqualTo(117));
            Assert.That(result.Tuple[1].Number, Is.EqualTo(97));
        }

        [Test]
        public void StringByteAcceptsNegativeIndices()
        {
            DynValue result = Evaluate("return string.byte('Lua', -1)");
            Assert.That(result.Number, Is.EqualTo(97));
        }

        [Test]
        public void StringByteReturnsNilForOutOfRangeIndex()
        {
            DynValue result = Evaluate("return string.byte('abc', 4)");
            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void StringByteTruncatesFloatIndices()
        {
            DynValue result = Evaluate("return string.byte('Lua', 1.9)");
            Assert.That(result.Number, Is.EqualTo(76));
        }

        // Lua 5.4 Reference Manual §6.4 (string.byte): the manual specifies that empty ranges yield no results.
        // NovaSharp currently returns nil; keep this assertion while we track the allocation-free fix.
        [Test]
        public void StringByteReturnsEmptyTupleWhenRangeIsEmpty()
        {
            DynValue result = Evaluate("return string.byte('Lua', 3, 2)");
            Assert.That(result.IsVoid(), Is.True);

            DynValue count = Evaluate("return select('#', string.byte('Lua', 3, 2))");
            Assert.That(count.Number, Is.EqualTo(0));
        }

        // Lua 5.4 Reference Manual §6.4 (string.byte): indices are clamped following string.sub rules.
        [Test]
        public void StringByteClampsIndicesWithinBounds()
        {
            DynValue result = Evaluate("return string.byte('Lua', -10, 10)");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple.Length, Is.EqualTo(3));
                Assert.That(result.Tuple[0].Number, Is.EqualTo(76));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(117));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(97));
            });
        }

        [Test]
        public void StringCharConcatenatesByteValues()
        {
            DynValue result = Evaluate("return string.char(97, 98, 99)");
            Assert.That(result.String, Is.EqualTo("abc"));
        }

        [Test]
        public void StringCharWrapsValuesModulo256()
        {
            DynValue result = Evaluate("return string.char(-1, 256)");

            Assert.Multiple(() =>
            {
                Assert.That(result.String.Length, Is.EqualTo(2));
                Assert.That(result.String[0], Is.EqualTo((char)255));
                Assert.That(result.String[1], Is.EqualTo('\0'));
            });
        }

        [Test]
        public void StringCharTruncatesFloats()
        {
            DynValue result = Evaluate("return string.char(65.8)");
            Assert.That(result.String, Is.EqualTo("A"));
        }

        // Lua 5.4 Reference Manual §6.4 (string.char): zero arguments yield the empty string.
        [Test]
        public void StringCharWithoutArgumentsReturnsEmptyString()
        {
            DynValue result = Evaluate("return string.char()");
            Assert.That(result.String, Is.EqualTo(string.Empty));
        }

        [Test]
        public void StringLenReturnsLengthInBytes()
        {
            DynValue result = Evaluate("return string.len('Lua')");
            Assert.That(result.Number, Is.EqualTo(3));
        }

        [Test]
        public void StringSubExtractsInclusiveRange()
        {
            DynValue result = Evaluate("return string.sub('abcdefg', 2, 4)");
            Assert.That(result.String, Is.EqualTo("bcd"));
        }

        [Test]
        public void StringSubSupportsNegativeBounds()
        {
            DynValue result = Evaluate("return string.sub('abcdefg', -3)");
            Assert.That(result.String, Is.EqualTo("efg"));
        }

        // Lua 5.4 Reference Manual §6.4 (string.sub): out-of-range lower bounds return empty string.
        [Test]
        public void StringSubReturnsEmptyStringWhenStartExceedsLength()
        {
            DynValue result = Evaluate("return string.sub('abc', 5, 7)");
            Assert.That(result.String, Is.EqualTo(string.Empty));
        }

        // Lua 5.4 Reference Manual §6.4 (string.sub): indices are clamped to the string limits.
        [Test]
        public void StringSubClampsIndicesToStringBounds()
        {
            DynValue result = Evaluate("return string.sub('abcdef', 0, 3)");
            Assert.That(result.String, Is.EqualTo("abc"));
        }

        [Test]
        public void StringFindReturnsStartAndEndPositions()
        {
            DynValue result = Evaluate("return string.find('hello world', 'world')");

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.That(result.Tuple[0].Number, Is.EqualTo(7));
            Assert.That(result.Tuple[1].Number, Is.EqualTo(11));
        }

        // Lua 5.4 Reference Manual §6.4 (string.find): plain flag disables pattern matching semantics.
        [Test]
        public void StringFindPlainSearchTreatsPatternLiterally()
        {
            DynValue result = Evaluate("return string.find('a^b', '^b', 1, true)");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(2));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(3));
            });
        }

        // Lua 5.4 Reference Manual §6.4 (string.find): captures are returned after the indices.
        [Test]
        public void StringFindReturnsCapturedSubstrings()
        {
            DynValue result = Evaluate("return string.find('hello', 'l(l)o')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple.Length, Is.EqualTo(3));
                Assert.That(result.Tuple[0].Number, Is.EqualTo(3));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(5));
                Assert.That(result.Tuple[2].String, Is.EqualTo("l"));
            });
        }

        [Test]
        public void StringRepSupportsOptionalSeparator()
        {
            DynValue result = Evaluate("return string.rep('ab', 3, '-')");
            Assert.That(result.String, Is.EqualTo("ab-ab-ab"));
        }

        // Lua 5.4 Reference Manual §6.4 (string.rep): non-positive counts return the empty string.
        [Test]
        public void StringRepZeroCountReturnsEmptyString()
        {
            DynValue result = Evaluate("return string.rep('text', 0)");
            Assert.That(result.String, Is.EqualTo(string.Empty));
        }

        [Test]
        public void StringReverseFlipsByteOrder()
        {
            DynValue result = Evaluate("return string.reverse('Lua')");
            Assert.That(result.String, Is.EqualTo("auL"));
        }

        [Test]
        public void StringFormatHandlesNumericPlaceholders()
        {
            DynValue result = Evaluate("return string.format('%.2f', 3.14159)");
            Assert.That(result.String, Is.EqualTo("3.14"));
        }

        // Lua 5.4 Reference Manual §6.4 (string.format): %q escapes quotes and control characters.
        [Test]
        public void StringFormatPercentQEscapesControlSequences()
        {
            DynValue result = Evaluate("return string.byte(string.format('%q', string.char(76, 117, 97, 10)), 5, 6)");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(92)); // '\'
                Assert.That(result.Tuple[1].Number, Is.EqualTo(110)); // 'n'
            });
        }

        // Lua 5.4 Reference Manual §6.4 (string.format): width and zero padding for integers.
        [Test]
        public void StringFormatZeroPadsIntegers()
        {
            DynValue result = Evaluate("return string.format('%02d:%02d', 7, 5)");
            Assert.That(result.String, Is.EqualTo("07:05"));
        }

        private static DynValue Evaluate(string lua)
        {
            Script script = new Script(CoreModules.PresetComplete);
            return script.DoString(lua);
        }
    }
}
