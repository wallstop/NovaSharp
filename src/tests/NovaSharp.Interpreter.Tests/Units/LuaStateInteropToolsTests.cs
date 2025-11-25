namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using NovaSharp.Interpreter.LuaPort.LuaStateInterop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LuaStateInteropToolsTests
    {
        [TestCase(typeof(sbyte), -1, true, ExpectedResult = false)]
        [TestCase(typeof(sbyte), 0, false, ExpectedResult = false)]
        [TestCase(typeof(short), 0, true, ExpectedResult = true)]
        [TestCase(typeof(short), 0, false, ExpectedResult = false)]
        [TestCase(typeof(int), 0, false, ExpectedResult = false)]
        [TestCase(typeof(int), 1, true, ExpectedResult = true)]
        [TestCase(typeof(uint), 10, false, ExpectedResult = true)]
        [TestCase(typeof(uint), 0, false, ExpectedResult = false)]
        [TestCase(typeof(uint), 0, true, ExpectedResult = true)]
        [TestCase(typeof(ushort), 0, false, ExpectedResult = false)]
        [TestCase(typeof(ushort), 0, true, ExpectedResult = true)]
        [TestCase(typeof(ulong), 2, false, ExpectedResult = true)]
        [TestCase(typeof(ulong), 0, false, ExpectedResult = false)]
        [TestCase(typeof(double), 0.5, false, ExpectedResult = true)]
        [TestCase(typeof(double), 0, false, ExpectedResult = false)]
        [TestCase(typeof(long), 0, true, ExpectedResult = true)]
        [TestCase(typeof(decimal), 0, false, ExpectedResult = false)]
        [TestCase(typeof(decimal), 0, true, ExpectedResult = true)]
        [TestCase(typeof(float), 0.5, true, ExpectedResult = true)]
        [TestCase(typeof(float), 0, false, ExpectedResult = false)]
        public bool IsPositiveHandlesPrimitiveTypes(Type type, double rawValue, bool zeroIsPositive)
        {
            object boxed = ConvertNumeric(rawValue, type);
            return Tools.IsPositive(boxed, zeroIsPositive);
        }

        [TestCase('\0', false, ExpectedResult = false)]
        [TestCase('A', false, ExpectedResult = true)]
        [TestCase('Z', true, ExpectedResult = true)]
        public bool IsPositiveHandlesCharInputs(char value, bool zeroIsPositive)
        {
            return Tools.IsPositive(value, zeroIsPositive);
        }

        [Test]
        public void IsPositiveHandlesUnsignedAndUnknownTypes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Tools.IsPositive((byte)0, zeroIsPositive: false), Is.False);
                Assert.That(Tools.IsPositive((byte)0, zeroIsPositive: true), Is.True);
                Assert.That(Tools.IsPositive((uint)5, zeroIsPositive: false), Is.True);
                Assert.That(Tools.IsPositive((char)0, zeroIsPositive: false), Is.False);
                Assert.That(Tools.IsPositive(new object(), zeroIsPositive: true), Is.True);
                Assert.That(Tools.IsPositive(new object(), zeroIsPositive: false), Is.False);
            });
        }

        [TestCase(typeof(sbyte), -1, typeof(byte), (ulong)255)]
        [TestCase(typeof(short), -1, typeof(ushort), (ulong)65535)]
        [TestCase(typeof(int), -1, typeof(uint), (ulong)4294967295)]
        [TestCase(typeof(long), -1, typeof(ulong), (ulong)18446744073709551615)]
        [TestCase(typeof(float), 1.5, typeof(uint), (ulong)1)]
        [TestCase(typeof(double), 1.5, typeof(ulong), (ulong)1)]
        [TestCase(typeof(decimal), 5, typeof(ulong), (ulong)5)]
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

        [Test]
        public void ToIntegerReturnsDecimalWithoutRounding()
        {
            object result = Tools.ToInteger((decimal)1.75m, round: false);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<decimal>());
                Assert.That(result, Is.EqualTo(1.75m));
            });
        }

        [TestCase(typeof(sbyte), -12)]
        [TestCase(typeof(short), 1200)]
        [TestCase(typeof(int), 42)]
        [TestCase(typeof(long), 123456789)]
        [TestCase(typeof(byte), 200)]
        [TestCase(typeof(ushort), 60000)]
        [TestCase(typeof(uint), 123456)]
        [TestCase(typeof(ulong), 123456)]
        public void ToIntegerReturnsIntegralValues(Type type, double rawValue)
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToInteger(boxed, round: false);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf(type));
                Assert.That(result, Is.EqualTo(boxed));
            });
        }

        [Test]
        public void UnboxToLongHandlesDecimalValues()
        {
            long rounded = Tools.UnboxToLong((decimal)1.6m, round: true);
            long truncated = Tools.UnboxToLong((decimal)1.6m, round: false);

            Assert.Multiple(() =>
            {
                Assert.That(rounded, Is.EqualTo(2));
                Assert.That(truncated, Is.EqualTo(1));
            });
        }

        [Test]
        public void ToIntegerReturnsNullForUnsupportedTypes()
        {
            Assert.That(Tools.ToInteger(TimeSpan.Zero, round: false), Is.Null);
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

        [TestCase(typeof(sbyte), -8, false, ExpectedResult = -8)]
        [TestCase(typeof(short), 32000, false, ExpectedResult = 32000)]
        [TestCase(typeof(int), 123456, false, ExpectedResult = 123456)]
        [TestCase(typeof(long), -123456789, false, ExpectedResult = -123456789)]
        [TestCase(typeof(byte), 200, false, ExpectedResult = 200)]
        [TestCase(typeof(ushort), 65530, false, ExpectedResult = 65530)]
        [TestCase(typeof(uint), 1234, false, ExpectedResult = 1234)]
        [TestCase(typeof(ulong), 9876, false, ExpectedResult = 9876)]
        [TestCase(typeof(decimal), 42, false, ExpectedResult = 42)]
        public long UnboxToLongHandlesIntegralValues(Type type, double rawValue, bool round)
        {
            object boxed = ConvertNumeric(rawValue, type);
            return Tools.UnboxToLong(boxed, round);
        }

        [Test]
        public void UnboxToLongHandlesUnsignedValues()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Tools.UnboxToLong((byte)200, round: false), Is.EqualTo(200));
                Assert.That(Tools.UnboxToLong((ushort)60000, round: false), Is.EqualTo(60000));
                Assert.That(Tools.UnboxToLong((uint)123456, round: false), Is.EqualTo(123456));
                Assert.That(
                    Tools.UnboxToLong((ulong)9876543210L, round: false),
                    Is.EqualTo(9876543210L)
                );
            });
        }

        [Test]
        public void ReplaceMetaCharsSubstitutesEscapeSequences()
        {
            string input = @"Line1\nLine2\t\040";
            Assert.That(Tools.ReplaceMetaChars(input), Is.EqualTo("Line1\nLine2\t "));
        }

        [Test]
        public void ReplaceMetaCharsHandlesAdditionalEscapes()
        {
            string input = @"\000\a\b\f\v\r";
            string expected = "\0\a\b\f\v\r";

            Assert.That(Tools.ReplaceMetaChars(input), Is.EqualTo(expected));
        }

        [TestCase(@"\000", "\0")]
        [TestCase(@"\a", "\a")]
        [TestCase(@"\b", "\b")]
        [TestCase(@"\f", "\f")]
        [TestCase(@"\v", "\v")]
        [TestCase(@"\r", "\r")]
        [TestCase(@"\n", "\n")]
        [TestCase(@"\t", "\t")]
        [TestCase(@"\060", "0")]
        [TestCase(@"\x", "x")]
        public void ReplaceMetaCharsExpandsIndividualEscapes(string input, string expected)
        {
            Assert.That(Tools.ReplaceMetaChars(input), Is.EqualTo(expected));
        }

        [Test]
        public void ReplaceMetaCharsLeavesUnknownSequencesIntact()
        {
            Assert.That(Tools.ReplaceMetaChars(@"\q"), Is.EqualTo("q"));
        }

        [Test]
        public void StringFormatSupportsNumericFormatting()
        {
            string result = Tools.StringFormat(
                "Value: %+06d, Hex: %#x, Text: %-5s",
                -42,
                255,
                "Hi"
            );

            Assert.That(result, Is.EqualTo("Value: -00042, Hex: 0xff, Text: Hi   "));
        }

        [Test]
        public void StringFormatSupportsOctalAlternateZeroPadding()
        {
            string result = Tools.StringFormat("%#06o", 9);
            Assert.That(result, Is.EqualTo("000011"));
        }

        [Test]
        public void StringFormatFormatsPointerAsLowercaseHex()
        {
            string result = Tools.StringFormat("ptr:%p", new IntPtr(0x2A));
            Assert.That(result, Is.EqualTo("ptr:0x2a"));
        }

        [Test]
        public void StringFormatReplacesPercentNWithProcessedLength()
        {
            string result = Tools.StringFormat("abc%nXYZ");
            Assert.That(result, Is.EqualTo("abc3XYZ"));
        }

        [Test]
        public void StringFormatHonoursPositiveSpaceFlag()
        {
            string result = Tools.StringFormat("% d", 5);
            Assert.That(result, Is.EqualTo(" 5"));
        }

        [Test]
        public void StringFormatSpaceFlagIsSuppressedByPlusFlag()
        {
            string result = Tools.StringFormat("%+ d", 7);
            Assert.That(result, Is.EqualTo("+7"));
        }

        [Test]
        public void StringFormatFormatsUppercaseHexWithAlternateZeroPadding()
        {
            string result = Tools.StringFormat("%#08X", 0x2A);
            Assert.That(result, Is.EqualTo("0X00002A"));
        }

        [Test]
        public void StringFormatDoesNotDuplicateOctalPrefixForZero()
        {
            string result = Tools.StringFormat("%#o", 0);
            Assert.That(result, Is.EqualTo("0"));
        }

        [Test]
        public void StringFormatAlternateOctalWithSpacePadding()
        {
            string result = Tools.StringFormat("%#6o", 15);
            Assert.That(result, Is.EqualTo("   017"));
        }

        [Test]
        public void StringFormatAppliesPositiveSignWhenZeroPadding()
        {
            string result = Tools.StringFormat("%+06d", 5);
            Assert.That(result, Is.EqualTo("+00005"));
        }

        [Test]
        public void StringFormatSupportsLiteralPercent()
        {
            string result = Tools.StringFormat("Progress: 50%% done");
            Assert.That(result, Is.EqualTo("Progress: 50% done"));
        }

        [Test]
        public void StringFormatLeftAlignmentDisablesZeroPadding()
        {
            string result = Tools.StringFormat("%-05d", 12);
            Assert.That(result, Is.EqualTo("12   "));
        }

        [Test]
        public void StringFormatSupportsExplicitParameterIndexes()
        {
            string result = Tools.StringFormat("%2$d %1$+05d", 3, 10);
            Assert.That(result, Is.EqualTo("10 +0003"));
        }

        [Test]
        public void StringFormatHonoursShortAndLongLengthModifiers()
        {
            string result = Tools.StringFormat("%hd %lu", 40000, (ushort)65535);
            Assert.That(result, Is.EqualTo("-25536 65535"));
        }

        [Test]
        public void StringFormatShortIndicatorCastsWideInputs()
        {
            string result = Tools.StringFormat("%hd %hu", 70000L, 131071UL);
            Assert.That(result, Is.EqualTo("4464 65535"));
        }

        [Test]
        public void StringFormatShortIndicatorCastsUnsignedIntegers()
        {
            string result = Tools.StringFormat("%hu", 131071u);
            Assert.That(result, Is.EqualTo("65535"));
        }

        [Test]
        public void StringFormatLongIndicatorExtendsNarrowInputs()
        {
            string result = Tools.StringFormat("%ld %lu", (short)-3, uint.MaxValue);
            Assert.That(result, Is.EqualTo("-3 4294967295"));
        }

        [Test]
        public void StringFormatAlternateHexLeftAlignedPadsWithSpaces()
        {
            string result = Tools.StringFormat("%-#6x", 26);
            Assert.That(result, Is.EqualTo("0x1a  "));
        }

        [Test]
        public void StringFormatAlternateOctalLeftAlignedPadsWithSpaces()
        {
            string result = Tools.StringFormat("%-#6o", 8);
            Assert.That(result, Is.EqualTo("010   "));
        }

        [Test]
        public void StringFormatUnsignedIgnoresNonNumericValues()
        {
            string result = Tools.StringFormat("%u", "text");
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void StringFormatFloatIgnoresNonNumericValues()
        {
            string result = Tools.StringFormat("%f", "text");
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void StringFormatAppliesThousandsGroupingFlag()
        {
            string result = Tools.StringFormat("%'15d", 1234567);
            Assert.That(result, Is.EqualTo("      1,234,567"));
        }

        [Test]
        public void StringFormatTruncatesStringsWhenPrecisionSpecified()
        {
            string result = Tools.StringFormat("%.3s", "LuaState");
            Assert.That(result, Is.EqualTo("Lua"));
        }

        [Test]
        public void StringFormatStringRightAlignmentUsesPadding()
        {
            string result = Tools.StringFormat("%5s", "Lua");
            Assert.That(result, Is.EqualTo("  Lua"));
        }

        [Test]
        public void StringFormatUsesFirstCharacterOfStringForCharFormat()
        {
            string result = Tools.StringFormat("%c", "Hello");
            Assert.That(result, Is.EqualTo("H"));
        }

        [Test]
        public void StringFormatCharSpecifierAcceptsNumericAndCharInputs()
        {
            string result = Tools.StringFormat("%c %c", 65, 'Z');
            Assert.That(result, Is.EqualTo("A Z"));
        }

        [Test]
        public void StringFormatFormatsFloatingPointSpecifiers()
        {
            string result = Tools.StringFormat(
                "%.2f|%.2e|%.2E|%.2g|%.2G",
                1.2345,
                1.2345,
                1.2345,
                12345.0,
                12345.0
            );
            Assert.That(result, Is.EqualTo("1.23|1.23e+000|1.23E+000|1.2e+04|1.2E+04"));
        }

        [Test]
        public void StringFormatIgnoresUnsupportedFormatSpecifier()
        {
            string result = Tools.StringFormat("Value:%q:%d", 42, 7);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo("Value:%q:42"));
                Assert.That(ContainsOrdinal(result, "%q"), Is.True);
            });
        }

        [Test]
        public void StringFormatTreatsSpecifierIAsSignedInteger()
        {
            string result = Tools.StringFormat("%i %d", 42, 7);
            Assert.That(result, Is.EqualTo("42 7"));
        }

        [TestCase(typeof(byte), 200)]
        [TestCase(typeof(ushort), 65530)]
        [TestCase(typeof(uint), 12345)]
        [TestCase(typeof(ulong), 12345)]
        public void ToUnsignedReturnsUnsignedValues(Type type, double rawValue)
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToUnsigned(boxed);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf(type));
                Assert.That(result, Is.EqualTo(boxed));
            });
        }

        [Test]
        public void StringFormatDropsNonNumericValuesForNumericFormats()
        {
            string result = Tools.StringFormat("Value:%d", "text");
            Assert.That(result, Is.EqualTo("Value:"));
        }

        [Test]
        public void ToUnsignedReturnsNullForUnsupportedTypes()
        {
            Assert.That(Tools.ToUnsigned(TimeSpan.Zero), Is.Null);
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

        private static bool ContainsOrdinal(string text, string value)
        {
            return text != null && text.Contains(value, StringComparison.Ordinal);
        }
    }
}
