namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class StringModuleTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public async Task CharProducesStringFromByteValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(65, 66, 67)");

            await Assert.That(result.String).IsEqualTo("ABC").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharThrowsWhenArgumentCannotBeCoerced(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(\"not-a-number\")")
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharReturnsNullByteForZero(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharReturnsMaxByteValue(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(255)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo((char)255).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharReturnsEmptyStringWhenNoArgumentsProvided(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char()");

            await Assert.That(result.String).IsEmpty().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharErrorsOnValuesOutsideByteRange(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(-1, 256)")
            );

            await Assert
                .That(exception.Message)
                .Contains("value out of range")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharAcceptsIntegralFloatValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(65.0)");

            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        // NOTE: Removed CharTruncatesFloatValues - behavior is version-specific.
        // Lua 5.1/5.2: Float truncation (tested in CharTruncatesFloatValuesLua51And52)
        // Lua 5.3+: Throws error (tested in CharErrorsOnNonIntegerFloatLua53Plus)

        [Test]
        [LuaTestMatrix(
            new object[] { -1, "negative value" },
            new object[] { 256, "value above 255" },
            new object[] { 300, "value well above 255" },
            new object[] { -100, "large negative value" }
        )]
        public async Task CharErrorsOnOutOfRangeValue(
            LuaCompatibilityVersion version,
            int value,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return string.char({value})")
            );

            await Assert
                .That(exception.Message)
                .Contains("value out of range")
                .Because($"string.char should error for {description} ({value})")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaTestMatrix(
            new object[] { 0, '\0', "zero produces null byte" },
            new object[] { 255, (char)255, "255 produces max byte" },
            new object[] { 1, (char)1, "one produces SOH" },
            new object[] { 127, (char)127, "127 produces DEL" }
        )]
        public async Task CharAcceptsBoundaryValues(
            LuaCompatibilityVersion version,
            int value,
            char expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return string.char({value})");

            await Assert
                .That(result.String)
                .IsEqualTo(expected.ToString())
                .Because(description)
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task LenReturnsStringLength(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.len('Nova')");

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task LowerReturnsLowercaseString(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.lower('NovaSharp')");

            await Assert.That(result.String).IsEqualTo("novasharp").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task UpperReturnsUppercaseString(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.upper('NovaSharp')");

            await Assert.That(result.String).IsEqualTo("NOVASHARP").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteReturnsByteCodesForSubstring(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local codes = {string.byte('Lua', 1, 3)}
                return #codes, codes[1], codes[2], codes[3]
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(76d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(117d).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(97d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteDefaultsToFirstCharacter(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua')");

            await Assert.That(result.Number).IsEqualTo(76d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteSupportsNegativeIndices(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua', -1)");

            await Assert.That(result.Number).IsEqualTo(97d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteReturnsNilWhenIndexPastEnd(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua', 4)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteReturnsNilWhenStartExceedsEnd(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua', 3, 2)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteReturnsNilForEmptySource(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('', 1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ByteAcceptsIntegralFloatIndices(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua', 1.0)");

            await Assert.That(result.Number).IsEqualTo(76d).ConfigureAwait(false);
        }

        // NOTE: ByteTruncatesFloatIndices behavior is version-specific.
        // Lua 5.1/5.2: Silently truncates via floor (tested below)
        // Lua 5.3+: Throws "number has no integer representation" (tested below)

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task ByteTruncatesFloatIndicesLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua', 1.5)");

            await Assert
                .That(result.Number)
                .IsEqualTo(76d)
                .Because("Lua 5.1/5.2 should truncate 1.5 to 1 and return 'L' (76)")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteErrorsOnNonIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('Lua', 1.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation for indices")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteErrorsOnNaNIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('Lua', 0/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation - NaN has none")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteErrorsOnInfinityIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('Lua', 1/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation - Infinity has none")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task ByteReturnsNilForNaNIndexLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua', 0/0)");

            // NaN floored is still NaN, which when cast to int produces invalid index
            await Assert
                .That(result.IsNil() || result.IsVoid())
                .IsTrue()
                .Because("Lua 5.1/5.2 should return nil for NaN index")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task ByteNegativeFractionalIndexUsesFloorLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            // -0.5 floored is -1, which means "last character" in Lua
            DynValue result = script.DoString("return string.byte('Lua', -0.5)");

            // 'a' is the last character, ASCII 97
            await Assert
                .That(result.Number)
                .IsEqualTo(97d)
                .Because("Lua 5.1/5.2 should floor -0.5 to -1 (last char)")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteAcceptsLargeIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // Large integer beyond double precision (2^53+1) but stored as integer is valid
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('a', 9007199254740993)");

            // Index 2^53+1 is way beyond string length, should return nil
            await Assert
                .That(result.IsNil() || result.IsVoid())
                .IsTrue()
                .Because(
                    "Lua 5.3+ accepts large integers (2^53+1) as indices when stored as integer type"
                )
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteAcceptsMathMaxIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // math.maxinteger (2^63-1) is valid when stored as integer
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('a', math.maxinteger)");

            // Index is way beyond string length, should return nil
            await Assert
                .That(result.IsNil() || result.IsVoid())
                .IsTrue()
                .Because("Lua 5.3+ accepts math.maxinteger as index when stored as integer type")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteErrorsOnLargeFloatIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // 1e308 is a valid float but cannot be converted to integer
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('a', 1e308)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ rejects floats that overflow integer range")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteAcceptsWholeNumberFloatIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // 5.0 is a float with exact integer representation
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('hello', 5.0)");

            // 'o' is ASCII 111
            await Assert
                .That(result.Number)
                .IsEqualTo(111d)
                .Because("Lua 5.3+ accepts whole number floats (5.0) as indices")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ByteDistinguishesIntegerVsFloatForLargeValuesLua53Plus(
            LuaCompatibilityVersion version
        )
        {
            // This tests the critical distinction:
            // - 9007199254740993 as integer (2^53+1) is valid
            // - 9007199254740993 + 0.0 as float loses precision and may fail validation
            // In Lua 5.4, converting to float and back changes the value, so the float
            // version would round to 9007199254740992, which IS representable
            Script script = new Script(version, CoreModulePresets.Complete);

            // Integer version should work
            DynValue intResult = script.DoString(
                "local x = 9007199254740993; return string.byte('a', x)"
            );
            await Assert
                .That(intResult.IsNil() || intResult.IsVoid())
                .IsTrue()
                .Because("Large integer index should be accepted (returns nil for out-of-range)")
                .ConfigureAwait(false);

            // Float version also works because the rounded value is still exact
            DynValue floatResult = script.DoString(
                "local x = 9007199254740993.0; return string.byte('a', x)"
            );
            await Assert
                .That(floatResult.IsNil() || floatResult.IsVoid())
                .IsTrue()
                .Because("Float that rounds to representable integer should also be accepted")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task UnicodeReturnsFullUnicodeCodePoints(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local codes = {string.unicode('\u{0100}')}
                return #codes, codes[1]
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(256d).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task RepSupportsSeparatorsLua52Plus(LuaCompatibilityVersion version)
        {
            // The separator parameter was added in Lua 5.2
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue repeated = script.DoString("return string.rep('ab', 3, '-')");

            await Assert.That(repeated.String).IsEqualTo("ab-ab-ab").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public async Task RepIgnoresSeparatorInLua51(LuaCompatibilityVersion version)
        {
            // Lua 5.1 doesn't support the separator parameter - it's ignored
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue repeated = script.DoString("return string.rep('ab', 3, '-')");

            await Assert.That(repeated.String).IsEqualTo("ababab").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task RepSupportsZeroCount(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue zeroCount = script.DoString("return string.rep('ab', 0)");

            await Assert.That(zeroCount.String).IsEmpty().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FindReturnsMatchBoundaries(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local startIndex, endIndex = string.find('NovaSharp', 'Sharp')
                return startIndex, endIndex
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(9d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task MatchReturnsFirstCapture(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "return string.match('Version: 1.2.3', '%d+%.%d+%.%d+')"
            );

            await Assert.That(result.String).IsEqualTo("1.2.3").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task ReverseReturnsEmptyStringForEmptyInput(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.reverse('')");

            await Assert.That(result.String).IsEmpty().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GSubAppliesGlobalReplacement(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local replaced, count = string.gsub('foo bar foo', 'foo', 'baz')
                return replaced, count
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("baz bar baz")
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SubHandlesNegativeIndices(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.sub('NovaSharp', -5, -2)");

            await Assert.That(result.String).IsEqualTo("Shar").ConfigureAwait(false);
        }

        // NOTE: string.sub index behavior is version-specific.
        // Lua 5.1/5.2: Silently truncates via floor
        // Lua 5.3+: Throws "number has no integer representation"

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task SubTruncatesFloatIndicesLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.sub('Lua', 1.5, 3)");

            await Assert
                .That(result.String)
                .IsEqualTo("Lua")
                .Because("Lua 5.1/5.2 should truncate 1.5 to 1")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task SubErrorsOnNonIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.sub('Lua', 1.5, 3)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation for indices")
                .ConfigureAwait(false);
        }

        // NOTE: string.rep count behavior is version-specific.
        // Lua 5.1/5.2: Silently truncates via floor
        // Lua 5.3+: Throws "number has no integer representation"

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task RepTruncatesFloatCountLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.rep('a', 2.5)");

            await Assert
                .That(result.String)
                .IsEqualTo("aa")
                .Because("Lua 5.1/5.2 should truncate 2.5 to 2")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task RepErrorsOnNonIntegerCountLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.rep('a', 2.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation for count")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatInterpolatesValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('Value: %0.2f', 3.14159)");

            await Assert.That(result.String).IsEqualTo("Value: 3.14").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StartsWithEndsWithContainsTreatNilAsFalse(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                return string.startswith(nil, 'prefix'),
                       string.endswith('suffix', nil),
                       string.contains(nil, nil)
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsFalse().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StartsWithEndsWithContainsReturnTrueWhenMatchesPresent(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                return string.startswith('NovaSharp', 'Nova'),
                       string.endswith('NovaSharp', 'Sharp'),
                       string.contains('NovaSharp', 'Shar')
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task DumpPrependsNovaSharpBase64Header(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local function increment(x) return x + 1 end
                return string.dump(increment)
                "
            );

            await Assert
                .That(result.String)
                .StartsWith(StringModule.Base64DumpHeader)
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GMatchIteratesOverMatches(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local iter = string.gmatch('one two', '%w+')
                return iter(), iter()
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].String).IsEqualTo("one").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("two").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task GMatchWithInitParameterStartsAtSpecifiedPosition(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.4+ supports optional init parameter for string.gmatch
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local results = {}
                for m in string.gmatch('abc def ghi', '%w+', 5) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
                "
            );

            // Starting at position 5 should skip 'abc ' and start at 'def'
            await Assert.That(result.String).IsEqualTo("def,ghi").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task GMatchIgnoresInitParameterInLuaBelow54(LuaCompatibilityVersion version)
        {
            // Lua 5.1-5.3 ignore the third argument to string.gmatch
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local results = {}
                for m in string.gmatch('abc def ghi', '%w+', 5) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
                "
            );

            // In Lua 5.1-5.3, the init parameter is ignored - starts from beginning
            await Assert.That(result.String).IsEqualTo("abc,def,ghi").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task GMatchWithNegativeInitStartsFromEnd(LuaCompatibilityVersion version)
        {
            // Negative init means offset from end of string (Lua 5.4+)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local results = {}
                -- 'abc def ghi' has length 11, init=-3 means start at position 9 (the 'g' in 'ghi')
                for m in string.gmatch('abc def ghi', '%w+', -3) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
                "
            );

            // Starting at -3 from end should only match 'ghi'
            await Assert.That(result.String).IsEqualTo("ghi").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GMatchWithInitAtExactWordBoundary()
        {
            // Test init parameter at exact word boundary in Lua 5.4
            Script script = new Script(LuaCompatibilityVersion.Lua54, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local results = {}
                -- 'hello world' - 'world' starts at position 7
                for m in string.gmatch('hello world', '%w+', 7) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
                "
            );

            await Assert.That(result.String).IsEqualTo("world").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task GMatchWithInitBeyondStringLengthReturnsNoMatches(
            LuaCompatibilityVersion version
        )
        {
            // Test init parameter beyond string length in Lua 5.4+
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local count = 0
                for m in string.gmatch('abc', '%w+', 100) do
                    count = count + 1
                end
                return count
                "
            );

            await Assert.That(result.Number).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task DumpWrapsClrFunctionFailuresWithScriptRuntimeException(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.Globals.Set("callback", DynValue.NewCallback((_, _) => DynValue.Nil));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.dump(callback)")
            );

            await Assert
                .That(
                    exception.Message.IndexOf(
                        "function expected",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .IsGreaterThanOrEqualTo(0);
        }

        [Test]
        [AllLuaVersions]
        public async Task NovaSharpInitRegistersStringMetatable(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            Table globals = script.Globals;
            Table stringTable = new(script);
            stringTable.Set("marker", DynValue.NewString("value"));

            StringModule.NovaSharpInit(globals, stringTable);

            Table metatable = script.GetTypeMetatable(DataType.String);
            await Assert.That(metatable).IsNotNull().ConfigureAwait(false);

            DynValue indexTableValue = metatable.Get("__index");
            await Assert.That(indexTableValue.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(indexTableValue.Table.Get("marker").String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AdjustIndexHandlesNilZeroPositiveAndNegativeInputs()
        {
            int? defaultResult = StringModule.TestHooks.AdjustIndex("Nova", DynValue.Nil, 3);
            int? zeroResult = StringModule.TestHooks.AdjustIndex("Nova", DynValue.NewNumber(0), 3);
            int? positiveResult = StringModule.TestHooks.AdjustIndex(
                "Nova",
                DynValue.NewNumber(4),
                3
            );
            int? negativeResult = StringModule.TestHooks.AdjustIndex(
                "Nova",
                DynValue.NewNumber(-2),
                3
            );

            await Assert.That(defaultResult).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(zeroResult).IsNull().ConfigureAwait(false);
            await Assert.That(positiveResult).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(negativeResult).IsEqualTo(6).ConfigureAwait(false);
        }

        // ========================================
        // string.char - Version-specific NaN/Infinity/Float tests
        // Lua 5.1/5.2: NaN, Infinity, and non-integer floats are treated as 0 or truncated
        // Lua 5.3+: These values throw "number has no integer representation"
        // ========================================

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task CharHandlesNaNAsZeroLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(0/0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task CharErrorsOnPositiveInfinityLua51And52(LuaCompatibilityVersion version)
        {
            // In Lua 5.1/5.2, positive infinity throws "invalid value" error
            // (unlike NaN and negative infinity, which are silently treated as 0)
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(1/0)")
            );

            await Assert.That(exception.Message).Contains("invalid value").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task CharHandlesNegativeInfinityAsZeroLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(-1/0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task CharTruncatesFloatValuesLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(65.5)");

            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CharErrorsOnNaNLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(0/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CharErrorsOnPositiveInfinityLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(1/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CharErrorsOnNegativeInfinityLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(-1/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CharErrorsOnNonIntegerFloatLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(65.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task CharAcceptsNumericStringArguments(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char('65', '66')");

            await Assert.That(result.String).IsEqualTo("AB").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Octal format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalBasic(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%o', 8)");

            await Assert.That(result.String).IsEqualTo("10").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalWithAlternateFlag(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#o', 8)");

            await Assert.That(result.String).IsEqualTo("010").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalAlternateFlagWithZero(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#o', 0)");

            await Assert.That(result.String).IsEqualTo("0").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalWithFieldWidth(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%8o', 8)");

            await Assert.That(result.String).IsEqualTo("      10").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalWithZeroPadding(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%08o', 8)");

            await Assert.That(result.String).IsEqualTo("00000010").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalWithLeftAlign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%-8o', 8)");

            await Assert.That(result.String).IsEqualTo("10      ").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalWithLeftAlignAndAlternate(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%-#8o', 8)");

            await Assert.That(result.String).IsEqualTo("010     ").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatOctalZeroPaddingWithAlternate(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#08o', 8)");

            await Assert.That(result.String).IsEqualTo("00000010").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Unsigned integer format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatUnsignedBasic(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%u', 42)");

            await Assert.That(result.String).IsEqualTo("42").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatUnsignedWithFieldWidth(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%8u', 42)");

            await Assert.That(result.String).IsEqualTo("      42").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatUnsignedWithZeroPadding(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%08u', 42)");

            await Assert.That(result.String).IsEqualTo("00000042").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Hex format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatHexLowercaseBasic(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%x', 255)");

            await Assert.That(result.String).IsEqualTo("ff").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexUppercaseBasic(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%X', 255)");

            await Assert.That(result.String).IsEqualTo("FF").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithAlternateFlagLowercase(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#x', 255)");

            await Assert.That(result.String).IsEqualTo("0xff").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithAlternateFlagUppercase(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#X', 255)");

            await Assert.That(result.String).IsEqualTo("0XFF").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithFieldWidth(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%8x', 255)");

            await Assert.That(result.String).IsEqualTo("      ff").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithZeroPadding(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%08x', 255)");

            await Assert.That(result.String).IsEqualTo("000000ff").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithLeftAlign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%-8x', 255)");

            await Assert.That(result.String).IsEqualTo("ff      ").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithPrecision(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%.4x', 255)");

            await Assert.That(result.String).IsEqualTo("00ff").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexZeroPaddingWithAlternateLowercase(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#08x', 255)");

            await Assert.That(result.String).IsEqualTo("0x0000ff").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexZeroPaddingWithAlternateUppercase(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%#08X', 255)");

            await Assert.That(result.String).IsEqualTo("0X0000FF").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexLeftAlignWithAlternate(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%-#8x', 255)");

            await Assert.That(result.String).IsEqualTo("0xff    ").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Integer format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatIntegerWithPositiveSign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%+d', 42)");

            await Assert.That(result.String).IsEqualTo("+42").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatIntegerWithPositiveSpace(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('% d', 42)");

            await Assert.That(result.String).IsEqualTo(" 42").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatIntegerPositiveSignOverridesSpace(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%+ d', 42)");

            await Assert.That(result.String).IsEqualTo("+42").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatNegativeIntegerWithPositiveSign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%+d', -42)");

            await Assert.That(result.String).IsEqualTo("-42").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatIntegerWithZeroPaddingAndPositiveSign(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%+08d', 42)");

            await Assert.That(result.String).IsEqualTo("+0000042").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatNegativeIntegerWithZeroPadding(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%08d', -42)");

            // Note: Lua counts the minus sign as part of the width
            await Assert.That(result.String).IsEqualTo("-00000042").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatLeftAlignOverridesZeroPadding(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%-08d', 42)");

            await Assert.That(result.String).IsEqualTo("42      ").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Float format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatFloatWithPositiveSign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%+f', 3.14)");

            await Assert.That(result.String).StartsWith("+3.14").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatFloatWithPositiveSpace(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('% f', 3.14)");

            await Assert.That(result.String).StartsWith(" 3.14").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Exponent format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatExponentLowercase(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%e', 12345.6)");

            await Assert.That(result.String).Contains("e").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatExponentUppercase(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%E', 12345.6)");

            await Assert.That(result.String).Contains("E").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatGeneralLowercase(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%g', 0.0001234)");

            await Assert.That(result.String).IsNotEmpty().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatGeneralUppercase(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%G', 0.0001234)");

            await Assert.That(result.String).IsNotEmpty().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatExponentWithPositiveSign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%+e', 12345.6)");

            await Assert.That(result.String).StartsWith("+").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Character format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatCharFromNumber(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%c', 65)");

            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        // ========================================
        // string.format - String format specifier tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatStringWithPrecision(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%.3s', 'Hello')");

            await Assert.That(result.String).IsEqualTo("Hel").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatStringWithFieldWidth(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%10s', 'Hello')");

            await Assert.That(result.String).IsEqualTo("     Hello").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatStringWithLeftAlign(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%-10s', 'Hello')");

            await Assert.That(result.String).IsEqualTo("Hello     ").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Escape tests
        // ========================================

        [Test]
        [AllLuaVersions]
        public async Task FormatPercentEscape(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('100%% complete')");

            await Assert.That(result.String).IsEqualTo("100% complete").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Integer precision tests (Lua 5.3+ dual numeric type system)
        // ========================================

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatDecimalPreservesMathMaxinteger(LuaCompatibilityVersion version)
        {
            // math.maxinteger = 9223372036854775807 (2^63 - 1)
            // This value cannot be exactly represented as a double (loses precision)
            // With LuaNumber integer subtype, %d should preserve the exact value
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%d', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatDecimalPreservesMathMininteger(LuaCompatibilityVersion version)
        {
            // math.mininteger = -9223372036854775808 (-2^63)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%d', math.mininteger)");

            await Assert
                .That(result.String)
                .IsEqualTo("-9223372036854775808")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatIntegerSpecifierPreservesMathMaxinteger(
            LuaCompatibilityVersion version
        )
        {
            // %i is equivalent to %d
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%i', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatHexPreservesLargeIntegers(LuaCompatibilityVersion version)
        {
            // math.maxinteger in hex should be 7fffffffffffffff
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%x', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("7fffffffffffffff").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatHexUppercasePreservesLargeIntegers(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%X', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("7FFFFFFFFFFFFFFF").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatOctalPreservesLargeIntegers(LuaCompatibilityVersion version)
        {
            // math.maxinteger in octal
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%o', math.maxinteger)");

            await Assert
                .That(result.String)
                .IsEqualTo("777777777777777777777")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatDecimalWithIntegerLiteral(LuaCompatibilityVersion version)
        {
            // Large integer literals should preserve precision
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%d', 9223372036854775807)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task FormatDecimalTruncatesFloatInLua51And52(LuaCompatibilityVersion version)
        {
            // Lua 5.1/5.2: Float values are converted to integer (truncated)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%d', 123.456)");

            await Assert
                .That(result.String)
                .IsEqualTo("123")
                .Because($"Lua {version} should truncate float to integer for %d")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatDecimalErrorsOnFloatInLua53Plus(LuaCompatibilityVersion version)
        {
            // Lua 5.3+: Float values that don't have integer representation throw an error
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.format('%d', 123.456)")
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because(
                    $"Lua {version} should throw 'number has no integer representation' for %d with 123.456"
                )
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that string.format with %d accepts exact integer values in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaTestMatrix(
            new object[] { "123", "123" },
            new object[] { "0", "0" },
            new object[] { "-456", "-456" },
            new object[] { "math.maxinteger", "9223372036854775807" },
            new object[] { "math.mininteger", "-9223372036854775808" },
            new object[] { "42.0", "42" },
            new object[] { "-1.0", "-1" },
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task FormatDecimalAcceptsIntegerValues(
            LuaCompatibilityVersion version,
            string luaExpression,
            string expected
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return string.format('%d', {luaExpression})");

            await Assert
                .That(result.String)
                .IsEqualTo(expected)
                .Because($"string.format('%d', {luaExpression}) should produce {expected}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that string.format with %d rejects non-integer values in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaTestMatrix(
            new object[] { "0.5", "fractional" },
            new object[] { "-0.5", "negative fractional" },
            new object[] { "1e100", "large float beyond integer range" },
            new object[] { "-1e100", "large negative float" },
            new object[] { "0/0", "NaN" },
            new object[] { "1/0", "positive infinity" },
            new object[] { "-1/0", "negative infinity" },
            new object[] { "math.maxinteger + 0.5", "maxinteger plus fractional" },
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task FormatDecimalRejectsNonIntegerValues(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return string.format('%d', {luaExpression})")
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because($"string.format('%d', {luaExpression}) [{description}] should throw")
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because(
                    $"Error message should indicate no integer representation for {description}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests other integer format specifiers (%i, %o, %u, %x, %X) with non-integer values in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaTestMatrix(
            "%i",
            "%o",
            "%u",
            "%x",
            "%X",
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task FormatIntegerSpecifiersRejectFloatValues(
            LuaCompatibilityVersion version,
            string specifier
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return string.format('{specifier}', 123.456)")
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because($"string.format('{specifier}', 123.456) should throw in Lua 5.3+")
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that %f (float) specifier works with any numeric value (no integer constraint).
        /// </summary>
        [Test]
        [LuaTestMatrix(
            new object[] { "123.456", "123" },
            new object[] { "0.5", "0" },
            new object[] { "-0.5", "-0" },
            new object[] { "42", "42" }
        )]
        public async Task FormatFloatAcceptsAnyNumericValue(
            LuaCompatibilityVersion version,
            string luaValue,
            string expectedIntegerPart
        )
        {
            ArgumentNullException.ThrowIfNull(luaValue);
            ArgumentNullException.ThrowIfNull(expectedIntegerPart);

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return string.format('%f', {luaValue})");

            // %f produces full precision output like "123.456000", just check it starts with expected integer part
            await Assert
                .That(result.String)
                .StartsWith(expectedIntegerPart)
                .Because(
                    $"string.format('%f', {luaValue}) output should start with {expectedIntegerPart}"
                )
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task FormatHexWithNegativeInteger(LuaCompatibilityVersion version)
        {
            // -1 as unsigned 64-bit integer is all 1s
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%x', -1)");

            await Assert.That(result.String).IsEqualTo("ffffffffffffffff").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatHexWithMathMininteger(LuaCompatibilityVersion version)
        {
            // math.mininteger = -2^63 = 0x8000000000000000
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%x', math.mininteger)");

            await Assert.That(result.String).IsEqualTo("8000000000000000").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatDecimalWithBitwiseResult(LuaCompatibilityVersion version)
        {
            // Bitwise operations produce integer results - verify precision preserved through formatting
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "return string.format('%d', math.maxinteger & math.maxinteger)"
            );

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatDecimalWithMathTointeger(LuaCompatibilityVersion version)
        {
            // math.tointeger returns integer subtype - verify precision preserved
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "return string.format('%d', math.tointeger(9223372036854775807))"
            );

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FormatUnsignedWithMathMaxinteger(LuaCompatibilityVersion version)
        {
            // %u format specifier with large positive integer
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%u', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        // ==========================================================================
        // Data-driven tests for string.char infinity/NaN edge cases
        // ==========================================================================

        /// <summary>
        /// Data-driven test for string.char() edge cases that SHOULD throw in Lua 5.3+.
        /// In Lua 5.3+, infinity and NaN have no integer representation and must throw
        /// with a specific error message.
        /// </summary>
        [Test]
        [LuaTestMatrix(
            new object[]
            {
                "string.char(1/0)",
                "positive infinity",
                "number has no integer representation",
            },
            new object[]
            {
                "string.char(-1/0)",
                "negative infinity",
                "number has no integer representation",
            },
            new object[] { "string.char(0/0)", "NaN", "number has no integer representation" },
            new object[]
            {
                "string.char(65, 1/0, 66)",
                "positive infinity as second arg",
                "number has no integer representation",
            },
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task CharRejectsInfinityAndNaNLua53Plus(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description,
            string expectedError
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return {luaExpression}")
            );

            await Assert
                .That(ex.Message)
                .Contains(expectedError)
                .Because(
                    $"Expression '{luaExpression}' ({description}) should throw '{expectedError}' in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for string.char() values that SHOULD succeed in Lua 5.1/5.2.
        /// NaN and negative infinity are silently converted to 0 (null byte).
        /// </summary>
        [Test]
        [LuaTestMatrix(
            new object[] { "string.char(0/0)", "NaN", '\0' },
            new object[] { "string.char(-1/0)", "negative infinity", '\0' },
            MaximumVersion = LuaCompatibilityVersion.Lua52
        )]
        public async Task CharTreatsNaNAndNegativeInfinityAsZeroLua51And52(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description,
            char expectedChar
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return {luaExpression}");

            await Assert
                .That(result.String.Length)
                .IsEqualTo(1)
                .Because(
                    $"Expression '{luaExpression}' ({description}) should produce a single character"
                )
                .ConfigureAwait(false);
            await Assert
                .That(result.String[0])
                .IsEqualTo(expectedChar)
                .Because(
                    $"Expression '{luaExpression}' ({description}) should produce '{expectedChar}' (char code {(int)expectedChar}) in {version}"
                )
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task FormatSAcceptsIntegersInLua51(LuaCompatibilityVersion version)
        {
            // In Lua 5.1, string.format("%s", number) auto-coerces numbers to strings
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(@"return string.format('%s', 123)");

            await Assert
                .That(result.String)
                .IsEqualTo("123")
                .Because("string.format('%s', 123) should return '123' in Lua 5.1")
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task FormatSAcceptsFloatsInLua51(LuaCompatibilityVersion version)
        {
            // In Lua 5.1, string.format("%s", number) auto-coerces numbers to strings
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(@"return string.format('%s', 123.456)");

            await Assert
                .That(result.String)
                .IsEqualTo("123.456")
                .Because("string.format('%s', 123.456) should return '123.456' in Lua 5.1")
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task FormatSAcceptsNegativeNumbersInLua51(LuaCompatibilityVersion version)
        {
            // In Lua 5.1, string.format("%s", number) auto-coerces numbers to strings
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(@"return string.format('%s', -42)");

            await Assert
                .That(result.String)
                .IsEqualTo("-42")
                .Because("string.format('%s', -42) should return '-42' in Lua 5.1")
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task FormatSAcceptsZeroInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(@"return string.format('%s', 0)");

            await Assert
                .That(result.String)
                .IsEqualTo("0")
                .Because("string.format('%s', 0) should return '0' in Lua 5.1")
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task FormatSRejectsTablesInLua51(LuaCompatibilityVersion version)
        {
            // In Lua 5.1, string.format("%s", table) should error (tables are not coercible)
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString(@"return string.format('%s', {})"))
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("string expected")
                .Because("string.format('%s', {}) should error in Lua 5.1 for non-coercible types")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task FormatSAcceptsAnyTypeInLua52Plus(LuaCompatibilityVersion version)
        {
            // In Lua 5.2+, string.format("%s", value) uses tostring() for all types
            Script script = new Script(version, CoreModulePresets.Complete);

            // Number should work
            DynValue numResult = script.DoString(@"return string.format('%s', 123)");
            await Assert.That(numResult.String).IsEqualTo("123").ConfigureAwait(false);

            // Boolean should work (Lua 5.2+)
            DynValue boolResult = script.DoString(@"return string.format('%s', true)");
            await Assert.That(boolResult.String).IsEqualTo("true").ConfigureAwait(false);

            // Nil should work (Lua 5.2+)
            DynValue nilResult = script.DoString(@"return string.format('%s', nil)");
            await Assert.That(nilResult.String).IsEqualTo("nil").ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModulePresets.Complete);
        }
    }
}
