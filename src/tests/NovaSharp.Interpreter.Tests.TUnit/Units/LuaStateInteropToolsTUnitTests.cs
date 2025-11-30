namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.LuaPort.LuaStateInterop;

    public sealed class LuaStateInteropToolsTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(sbyte), -1d, true, false)]
        [global::TUnit.Core.Arguments(typeof(sbyte), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(short), 0d, true, true)]
        [global::TUnit.Core.Arguments(typeof(short), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(int), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(int), 1d, true, true)]
        [global::TUnit.Core.Arguments(typeof(uint), 10d, false, true)]
        [global::TUnit.Core.Arguments(typeof(uint), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(uint), 0d, true, true)]
        [global::TUnit.Core.Arguments(typeof(ushort), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(ushort), 0d, true, true)]
        [global::TUnit.Core.Arguments(typeof(ulong), 2d, false, true)]
        [global::TUnit.Core.Arguments(typeof(ulong), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(double), 0.5d, false, true)]
        [global::TUnit.Core.Arguments(typeof(double), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(long), 0d, true, true)]
        [global::TUnit.Core.Arguments(typeof(decimal), 0d, false, false)]
        [global::TUnit.Core.Arguments(typeof(decimal), 0d, true, true)]
        [global::TUnit.Core.Arguments(typeof(float), 0.5d, true, true)]
        [global::TUnit.Core.Arguments(typeof(float), 0d, false, false)]
        public async Task IsPositiveHandlesPrimitiveTypes(
            Type type,
            double rawValue,
            bool zeroIsPositive,
            bool expected
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            bool result = Tools.IsPositive(boxed, zeroIsPositive);
            await Assert.That(result).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments('\0', false, false)]
        [global::TUnit.Core.Arguments('A', false, true)]
        [global::TUnit.Core.Arguments('Z', true, true)]
        public async Task IsPositiveHandlesCharInputs(
            char value,
            bool zeroIsPositive,
            bool expected
        )
        {
            bool result = Tools.IsPositive(value, zeroIsPositive);
            await Assert.That(result).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task IsPositiveHandlesUnsignedAndUnknownTypes()
        {
            await Assert.That(Tools.IsPositive((byte)0, zeroIsPositive: false)).IsFalse();
            await Assert.That(Tools.IsPositive((byte)0, zeroIsPositive: true)).IsTrue();
            await Assert.That(Tools.IsPositive((uint)5, zeroIsPositive: false)).IsTrue();
            await Assert.That(Tools.IsPositive((char)0, zeroIsPositive: false)).IsFalse();
            await Assert.That(Tools.IsPositive(new object(), zeroIsPositive: true)).IsTrue();
            await Assert.That(Tools.IsPositive(new object(), zeroIsPositive: false)).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(sbyte), -1d, typeof(byte), (ulong)255)]
        [global::TUnit.Core.Arguments(typeof(short), -1d, typeof(ushort), (ulong)65535)]
        [global::TUnit.Core.Arguments(typeof(int), -1d, typeof(uint), (ulong)4294967295)]
        [global::TUnit.Core.Arguments(
            typeof(long),
            -1d,
            typeof(ulong),
            (ulong)18446744073709551615
        )]
        [global::TUnit.Core.Arguments(typeof(float), 1.5d, typeof(uint), (ulong)1)]
        [global::TUnit.Core.Arguments(typeof(double), 1.5d, typeof(ulong), (ulong)1)]
        [global::TUnit.Core.Arguments(typeof(decimal), 5d, typeof(ulong), (ulong)5)]
        public async Task ToUnsignedConvertsSignedValues(
            Type type,
            double rawValue,
            Type expectedType,
            ulong expectedValue
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToUnsigned(boxed);
            await Assert.That(result?.GetType()).IsEqualTo(expectedType);
            await Assert
                .That(Convert.ToUInt64(result, CultureInfo.InvariantCulture))
                .IsEqualTo(expectedValue);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(float), 1.7d, true, typeof(int), 2d)]
        [global::TUnit.Core.Arguments(typeof(float), 1.7d, false, typeof(int), 1d)]
        [global::TUnit.Core.Arguments(typeof(double), 1.7d, true, typeof(long), 2d)]
        [global::TUnit.Core.Arguments(typeof(double), 1.7d, false, typeof(long), 1d)]
        [global::TUnit.Core.Arguments(typeof(decimal), 1.2d, true, typeof(decimal), 1d)]
        public async Task ToIntegerRoundsWhenRequested(
            Type type,
            double rawValue,
            bool round,
            Type expectedType,
            double expectedValue
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToInteger(boxed, round);
            await Assert.That(result?.GetType()).IsEqualTo(expectedType);
            await Assert
                .That(Convert.ToDouble(result, CultureInfo.InvariantCulture))
                .IsEqualTo(expectedValue);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsDecimalWithoutRounding()
        {
            object result = Tools.ToInteger((decimal)1.75m, round: false);
            await Assert.That(result).IsTypeOf<decimal>();
            await Assert.That(result).IsEqualTo(1.75m);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(sbyte), -12d)]
        [global::TUnit.Core.Arguments(typeof(short), 1200d)]
        [global::TUnit.Core.Arguments(typeof(int), 42d)]
        [global::TUnit.Core.Arguments(typeof(long), 123456789d)]
        [global::TUnit.Core.Arguments(typeof(byte), 200d)]
        [global::TUnit.Core.Arguments(typeof(ushort), 60000d)]
        [global::TUnit.Core.Arguments(typeof(uint), 123456d)]
        [global::TUnit.Core.Arguments(typeof(ulong), 123456d)]
        public async Task ToIntegerReturnsIntegralValues(Type type, double rawValue)
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToInteger(boxed, round: false);

            await Assert.That(result?.GetType()).IsEqualTo(type);
            await Assert.That(result).IsEqualTo(boxed);
        }

        [global::TUnit.Core.Test]
        public async Task UnboxToLongHandlesDecimalValues()
        {
            long rounded = Tools.UnboxToLong((decimal)1.6m, round: true);
            long truncated = Tools.UnboxToLong((decimal)1.6m, round: false);

            await Assert.That(rounded).IsEqualTo(2L);
            await Assert.That(truncated).IsEqualTo(1L);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsNullForUnsupportedTypes()
        {
            await Assert.That(Tools.ToInteger(TimeSpan.Zero, round: false)).IsNull();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(float), 1.7d, true, 2L)]
        [global::TUnit.Core.Arguments(typeof(float), 1.7d, false, 1L)]
        [global::TUnit.Core.Arguments(typeof(double), 2.2d, true, 2L)]
        [global::TUnit.Core.Arguments(typeof(double), 2.2d, false, 2L)]
        public async Task UnboxToLongHandlesFloatingPoint(
            Type type,
            double rawValue,
            bool round,
            long expected
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            long result = Tools.UnboxToLong(boxed, round);
            await Assert.That(result).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(sbyte), -8d, false, -8L)]
        [global::TUnit.Core.Arguments(typeof(short), 32000d, false, 32000L)]
        [global::TUnit.Core.Arguments(typeof(int), 123456d, false, 123456L)]
        [global::TUnit.Core.Arguments(typeof(long), -123456789d, false, -123456789L)]
        [global::TUnit.Core.Arguments(typeof(byte), 200d, false, 200L)]
        [global::TUnit.Core.Arguments(typeof(ushort), 65530d, false, 65530L)]
        [global::TUnit.Core.Arguments(typeof(uint), 1234d, false, 1234L)]
        [global::TUnit.Core.Arguments(typeof(ulong), 9876d, false, 9876L)]
        [global::TUnit.Core.Arguments(typeof(decimal), 42d, false, 42L)]
        public async Task UnboxToLongHandlesIntegralValues(
            Type type,
            double rawValue,
            bool round,
            long expected
        )
        {
            object boxed = ConvertNumeric(rawValue, type);
            long result = Tools.UnboxToLong(boxed, round);
            await Assert.That(result).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task UnboxToLongHandlesUnsignedValues()
        {
            await Assert.That(Tools.UnboxToLong((byte)200, round: false)).IsEqualTo(200L);
            await Assert.That(Tools.UnboxToLong((ushort)60000, round: false)).IsEqualTo(60000L);
            await Assert.That(Tools.UnboxToLong((uint)123456, round: false)).IsEqualTo(123456L);
            await Assert
                .That(Tools.UnboxToLong((ulong)9876543210L, round: false))
                .IsEqualTo(9876543210L);
        }

        [global::TUnit.Core.Test]
        public async Task ReplaceMetaCharsSubstitutesEscapeSequences()
        {
            string input = @"Line1\nLine2\t\040";
            await Assert.That(Tools.ReplaceMetaChars(input)).IsEqualTo("Line1\nLine2\t ");
        }

        [global::TUnit.Core.Test]
        public async Task ReplaceMetaCharsHandlesAdditionalEscapes()
        {
            string input = @"\000\a\b\f\v\r";
            string expected = "\0\a\b\f\v\r";

            await Assert.That(Tools.ReplaceMetaChars(input)).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(@"\000", "\0")]
        [global::TUnit.Core.Arguments(@"\a", "\a")]
        [global::TUnit.Core.Arguments(@"\b", "\b")]
        [global::TUnit.Core.Arguments(@"\f", "\f")]
        [global::TUnit.Core.Arguments(@"\v", "\v")]
        [global::TUnit.Core.Arguments(@"\r", "\r")]
        [global::TUnit.Core.Arguments(@"\n", "\n")]
        [global::TUnit.Core.Arguments(@"\t", "\t")]
        [global::TUnit.Core.Arguments(@"\060", "0")]
        [global::TUnit.Core.Arguments(@"\x", "x")]
        public async Task ReplaceMetaCharsExpandsIndividualEscapes(string input, string expected)
        {
            await Assert.That(Tools.ReplaceMetaChars(input)).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task ReplaceMetaCharsLeavesUnknownSequencesIntact()
        {
            await Assert.That(Tools.ReplaceMetaChars(@"\q")).IsEqualTo("q");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatSupportsNumericFormatting()
        {
            string result = Tools.StringFormat(
                "Value: %+06d, Hex: %#x, Text: %-5s",
                -42,
                255,
                "Hi"
            );

            await Assert.That(result).IsEqualTo("Value: -00042, Hex: 0xff, Text: Hi   ");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatSupportsOctalAlternateZeroPadding()
        {
            string result = Tools.StringFormat("%#06o", 9);
            await Assert.That(result).IsEqualTo("000011");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatFormatsPointerAsLowercaseHex()
        {
            string result = Tools.StringFormat("ptr:%p", new IntPtr(0x2A));
            await Assert.That(result).IsEqualTo("ptr:0x2a");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatReplacesPercentNWithProcessedLength()
        {
            string result = Tools.StringFormat("abc%nXYZ");
            await Assert.That(result).IsEqualTo("abc3XYZ");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHonoursPositiveSpaceFlag()
        {
            string result = Tools.StringFormat("% d", 5);
            await Assert.That(result).IsEqualTo(" 5");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatSpaceFlagIsSuppressedByPlusFlag()
        {
            string result = Tools.StringFormat("%+ d", 7);
            await Assert.That(result).IsEqualTo("+7");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatFormatsUppercaseHexWithAlternateZeroPadding()
        {
            string result = Tools.StringFormat("%#08X", 0x2A);
            await Assert.That(result).IsEqualTo("0X00002A");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatDoesNotDuplicateOctalPrefixForZero()
        {
            string result = Tools.StringFormat("%#o", 0);
            await Assert.That(result).IsEqualTo("0");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatAlternateOctalWithSpacePadding()
        {
            string result = Tools.StringFormat("%#6o", 15);
            await Assert.That(result).IsEqualTo("   017");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatAppliesPositiveSignWhenZeroPadding()
        {
            string result = Tools.StringFormat("%+06d", 5);
            await Assert.That(result).IsEqualTo("+00005");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatSupportsLiteralPercent()
        {
            string result = Tools.StringFormat("Progress: 50%% done");
            await Assert.That(result).IsEqualTo("Progress: 50% done");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatLeftAlignmentDisablesZeroPadding()
        {
            string result = Tools.StringFormat("%-05d", 12);
            await Assert.That(result).IsEqualTo("12   ");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatSupportsExplicitParameterIndexes()
        {
            string result = Tools.StringFormat("%2$d %1$+05d", 3, 10);
            await Assert.That(result).IsEqualTo("10 +0003");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHonoursShortAndLongLengthModifiers()
        {
            string result = Tools.StringFormat("%hd %lu", 40000, (ushort)65535);
            await Assert.That(result).IsEqualTo("-25536 65535");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatShortIndicatorCastsWideInputs()
        {
            string result = Tools.StringFormat("%hd %hu", 70000L, 131071UL);
            await Assert.That(result).IsEqualTo("4464 65535");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatShortIndicatorCastsUnsignedIntegers()
        {
            string result = Tools.StringFormat("%hu", 131071u);
            await Assert.That(result).IsEqualTo("65535");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatLongIndicatorExtendsNarrowInputs()
        {
            string result = Tools.StringFormat("%ld %lu", (short)-3, uint.MaxValue);
            await Assert.That(result).IsEqualTo("-3 4294967295");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatAlternateHexLeftAlignedPadsWithSpaces()
        {
            string result = Tools.StringFormat("%-#6x", 26);
            await Assert.That(result).IsEqualTo("0x1a  ");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatAlternateOctalLeftAlignedPadsWithSpaces()
        {
            string result = Tools.StringFormat("%-#6o", 8);
            await Assert.That(result).IsEqualTo("010   ");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatUnsignedIgnoresNonNumericValues()
        {
            string result = Tools.StringFormat("%u", "text");
            await Assert.That(result).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatFloatIgnoresNonNumericValues()
        {
            string result = Tools.StringFormat("%f", "text");
            await Assert.That(result).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatAppliesThousandsGroupingFlag()
        {
            string result = Tools.StringFormat("%'15d", 1234567);
            await Assert.That(result).IsEqualTo("      1,234,567");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatTruncatesStringsWhenPrecisionSpecified()
        {
            string result = Tools.StringFormat("%.3s", "LuaState");
            await Assert.That(result).IsEqualTo("Lua");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatStringRightAlignmentUsesPadding()
        {
            string result = Tools.StringFormat("%5s", "Lua");
            await Assert.That(result).IsEqualTo("  Lua");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatUsesFirstCharacterOfStringForCharFormat()
        {
            string result = Tools.StringFormat("%c", "Hello");
            await Assert.That(result).IsEqualTo("H");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatCharSpecifierAcceptsNumericAndCharInputs()
        {
            string result = Tools.StringFormat("%c %c", 65, 'Z');
            await Assert.That(result).IsEqualTo("A Z");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatFormatsFloatingPointSpecifiers()
        {
            string result = Tools.StringFormat(
                "%.2f|%.2e|%.2E|%.2g|%.2G",
                1.2345,
                1.2345,
                1.2345,
                12345.0,
                12345.0
            );
            await Assert.That(result).IsEqualTo("1.23|1.23e+000|1.23E+000|1.2e+04|1.2E+04");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatIgnoresUnsupportedFormatSpecifier()
        {
            string result = Tools.StringFormat("Value:%q:%d", 42, 7);

            await Assert.That(result).IsEqualTo("Value:%q:42");
            await Assert.That(ContainsOrdinal(result, "%q")).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatTreatsSpecifierIAsSignedInteger()
        {
            string result = Tools.StringFormat("%i %d", 42, 7);
            await Assert.That(result).IsEqualTo("42 7");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(byte), 200d)]
        [global::TUnit.Core.Arguments(typeof(ushort), 65530d)]
        [global::TUnit.Core.Arguments(typeof(uint), 12345d)]
        [global::TUnit.Core.Arguments(typeof(ulong), 12345d)]
        public async Task ToUnsignedReturnsUnsignedValues(Type type, double rawValue)
        {
            object boxed = ConvertNumeric(rawValue, type);
            object result = Tools.ToUnsigned(boxed);

            await Assert.That(result?.GetType()).IsEqualTo(type);
            await Assert.That(result).IsEqualTo(boxed);
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatDropsNonNumericValuesForNumericFormats()
        {
            string result = Tools.StringFormat("Value:%d", "text");
            await Assert.That(result).IsEqualTo("Value:");
        }

        [global::TUnit.Core.Test]
        public async Task ToUnsignedReturnsNullForUnsupportedTypes()
        {
            await Assert.That(Tools.ToUnsigned(TimeSpan.Zero)).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task FprintfWritesToDestination()
        {
            using StringWriter writer = new();
            Tools.Fprintf(writer, "Result=%d", 10);
            await Assert.That(writer.ToString()).IsEqualTo("Result=10");
        }

        [global::TUnit.Core.Test]
        public async Task IsNumericTypeRecognisesNumbers()
        {
            await Assert.That(Tools.IsNumericType(10)).IsTrue();
            await Assert.That(Tools.IsNumericType(10.5)).IsTrue();
            await Assert.That(Tools.IsNumericType((decimal)1.2)).IsTrue();
            await Assert.That(Tools.IsNumericType("not numeric")).IsFalse();
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
