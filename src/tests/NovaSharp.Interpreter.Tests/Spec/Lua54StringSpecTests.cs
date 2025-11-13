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

        [Test]
        public void StringFindReturnsStartAndEndPositions()
        {
            DynValue result = Evaluate("return string.find('hello world', 'world')");

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.That(result.Tuple[0].Number, Is.EqualTo(7));
            Assert.That(result.Tuple[1].Number, Is.EqualTo(11));
        }

        [Test]
        public void StringRepSupportsOptionalSeparator()
        {
            DynValue result = Evaluate("return string.rep('ab', 3, '-')");
            Assert.That(result.String, Is.EqualTo("ab-ab-ab"));
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

        private static DynValue Evaluate(string lua)
        {
            Script script = new Script(CoreModules.PresetComplete);
            return script.DoString(lua);
        }
    }
}
