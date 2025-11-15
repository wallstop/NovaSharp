namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using NovaSharp.Interpreter.Interop.LuaStateInterop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LuaStateInteropToolsTests
    {
        [TestCase(typeof(sbyte), -1, true, ExpectedResult = false)]
        [TestCase(typeof(short), 0, true, ExpectedResult = true)]
        [TestCase(typeof(int), 0, false, ExpectedResult = false)]
        [TestCase(typeof(uint), 10, false, ExpectedResult = true)]
        [TestCase(typeof(float), 0.5, true, ExpectedResult = true)]
        public bool IsPositiveHandlesPrimitiveTypes(Type type, double rawValue, bool zeroIsPositive)
        {
            object boxed = ConvertNumeric(rawValue, type);
            return Tools.IsPositive(boxed, zeroIsPositive);
        }

        [TestCase(typeof(sbyte), -1, typeof(byte), (ulong)255)]
        [TestCase(typeof(short), -1, typeof(ushort), (ulong)65535)]
        [TestCase(typeof(int), -1, typeof(uint), (ulong)4294967295)]
        [TestCase(typeof(long), -1, typeof(ulong), (ulong)18446744073709551615)]
        [TestCase(typeof(float), 1.5, typeof(uint), (ulong)1)]
        [TestCase(typeof(double), 1.5, typeof(ulong), (ulong)1)]
        public void ToUnsignedConvertsSignedValues(
            Type type,
            double rawValue,
            Type expectedType,
            ulong expectedValue
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToUnsigned(boxed);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf(expectedType));
                Assert.That(
                    Convert.ToUInt64(result, CultureInfo.InvariantCulture),
                    Is.EqualTo(expectedValue)
                );
            });
        }

        [TestCase(typeof(float), 1.7, true, typeof(int), 2)]
        [TestCase(typeof(float), 1.7, false, typeof(int), 1)]
        [TestCase(typeof(double), 1.7, true, typeof(long), 2)]
        [TestCase(typeof(double), 1.7, false, typeof(long), 1)]
        [TestCase(typeof(decimal), 1.2, true, typeof(decimal), 1)]
        public void ToIntegerRoundsWhenRequested(
            Type type,
            double rawValue,
            bool round,
            Type expectedType,
            double expectedValue
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToInteger(boxed, round);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf(expectedType));
                Assert.That(
                    Convert.ToDouble(result, CultureInfo.InvariantCulture),
                    Is.EqualTo(expectedValue)
                );
            });
        }

        [TestCase(typeof(float), 1.7, true, ExpectedResult = 2L)]
        [TestCase(typeof(float), 1.7, false, ExpectedResult = 1L)]
        [TestCase(typeof(double), 2.2, true, ExpectedResult = 2L)]
        [TestCase(typeof(double), 2.2, false, ExpectedResult = 2L)]
        public long UnboxToLongHandlesFloatingPoint(Type type, double rawValue, bool round)
        {
            object boxed = ConvertNumeric(rawValue, type);
            return Tools.UnboxToLong(boxed, round);
        }

        [Test]
        public void ReplaceMetaCharsSubstitutesEscapeSequences()
        {
            string input = @"Line1\nLine2\t\040";
            Assert.That(Tools.ReplaceMetaChars(input), Is.EqualTo("Line1\nLine2\t "));
        }

        [Test]
        public void SprintfSupportsNumericFormatting()
        {
            string result = Tools.Sprintf("Value: %+06d, Hex: %#x, Text: %-5s", -42, 255, "Hi");

            Assert.That(result, Is.EqualTo("Value: -00042, Hex: 0xff, Text: Hi   "));
        }

        [Test]
        public void SprintfSupportsOctalAlternateZeroPadding()
        {
            string result = Tools.Sprintf("%#06o", 9);
            Assert.That(result, Is.EqualTo("000011"));
        }

        [Test]
        public void SprintfFormatsPointerAsLowercaseHex()
        {
            string result = Tools.Sprintf("ptr:%p", new IntPtr(0x2A));
            Assert.That(result, Is.EqualTo("ptr:0x2a"));
        }

        [Test]
        public void SprintfReplacesPercentNWithProcessedLength()
        {
            string result = Tools.Sprintf("abc%nXYZ");
            Assert.That(result, Is.EqualTo("abc3XYZ"));
        }

        [Test]
        public void SprintfHonoursPositiveSpaceFlag()
        {
            string result = Tools.Sprintf("% d", 5);
            Assert.That(result, Is.EqualTo(" 5"));
        }

        [Test]
        public void SprintfSupportsExplicitParameterIndexes()
        {
            string result = Tools.Sprintf("%2$d %1$+05d", 3, 10);
            Assert.That(result, Is.EqualTo("10 +0003"));
        }

        [Test]
        public void SprintfHonoursShortAndLongLengthModifiers()
        {
            string result = Tools.Sprintf("%hd %lu", 40000, (ushort)65535);
            Assert.That(result, Is.EqualTo("-25536 65535"));
        }

        [Test]
        public void SprintfAppliesThousandsGroupingFlag()
        {
            string result = Tools.Sprintf("%'15d", 1234567);
            Assert.That(result, Is.EqualTo("      1,234,567"));
        }

        [Test]
        public void SprintfTruncatesStringsWhenPrecisionSpecified()
        {
            string result = Tools.Sprintf("%.3s", "LuaState");
            Assert.That(result, Is.EqualTo("Lua"));
        }

        [Test]
        public void SprintfUsesFirstCharacterOfStringForCharFormat()
        {
            string result = Tools.Sprintf("%c", "Hello");
            Assert.That(result, Is.EqualTo("H"));
        }

        [Test]
        public void SprintfIgnoresUnsupportedFormatSpecifier()
        {
            string result = Tools.Sprintf("Value:%q:%d", 42, 7);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo("Value:%q:42"));
                Assert.That(result.Contains("%q"), Is.True);
            });
        }

        [Test]
        public void FprintfWritesToDestination()
        {
            using StringWriter writer = new();
            Tools.Fprintf(writer, "Result=%d", 10);
            Assert.That(writer.ToString(), Is.EqualTo("Result=10"));
        }

        [Test]
        public void IsNumericTypeRecognisesNumbers()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Tools.IsNumericType(10), Is.True);
                Assert.That(Tools.IsNumericType(10.5), Is.True);
                Assert.That(Tools.IsNumericType((decimal)1.2), Is.True);
                Assert.That(Tools.IsNumericType("not numeric"), Is.False);
            });
        }

        private static object ConvertNumeric(double rawValue, Type type)
        {
            if (type == typeof(float))
            {
                return (float)rawValue;
            }

            if (type == typeof(double))
            {
                return rawValue;
            }

            if (type == typeof(decimal))
            {
                return (decimal)rawValue;
            }

            return Convert.ChangeType(rawValue, type, CultureInfo.InvariantCulture);
        }
    }
}
