namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class StringModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CharProducesStringFromByteValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65, 66, 67)");

            await Assert.That(result.String).IsEqualTo("ABC").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharThrowsWhenArgumentCannotBeCoerced()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(\"not-a-number\")")
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharReturnsNullByteForZero()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharReturnsMaxByteValue()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(255)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo((char)255).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharReturnsEmptyStringWhenNoArgumentsProvided()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char()");

            await Assert.That(result.String).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharErrorsOnValuesOutsideByteRange()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(-1, 256)")
            );

            await Assert
                .That(exception.Message)
                .Contains("value out of range")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharAcceptsIntegralFloatValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65.0)");

            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        // NOTE: Removed CharTruncatesFloatValues - behavior is version-specific.
        // Lua 5.1/5.2: Float truncation (tested in CharTruncatesFloatValuesLua51And52)
        // Lua 5.3+: Throws error (tested in CharErrorsOnNonIntegerFloatLua53Plus)

        [global::TUnit.Core.Test]
        [Arguments(-1, "negative value")]
        [Arguments(256, "value above 255")]
        [Arguments(300, "value well above 255")]
        [Arguments(-100, "large negative value")]
        public async Task CharErrorsOnOutOfRangeValue(int value, string description)
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return string.char({value})")
            );

            await Assert
                .That(exception.Message)
                .Contains("value out of range")
                .Because($"string.char should error for {description} ({value})")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(0, '\0', "zero produces null byte")]
        [Arguments(255, (char)255, "255 produces max byte")]
        [Arguments(1, (char)1, "one produces SOH")]
        [Arguments(127, (char)127, "127 produces DEL")]
        public async Task CharAcceptsBoundaryValues(int value, char expected, string description)
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return string.char({value})");

            await Assert
                .That(result.String)
                .IsEqualTo(expected.ToString())
                .Because(description)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LenReturnsStringLength()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.len('Nova')");

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LowerReturnsLowercaseString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.lower('NovaSharp')");

            await Assert.That(result.String).IsEqualTo("novasharp").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpperReturnsUppercaseString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.upper('NovaSharp')");

            await Assert.That(result.String).IsEqualTo("NOVASHARP").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsByteCodesForSubstring()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task ByteDefaultsToFirstCharacter()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua')");

            await Assert.That(result.Number).IsEqualTo(76d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ByteSupportsNegativeIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', -1)");

            await Assert.That(result.Number).IsEqualTo(97d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsNilWhenIndexPastEnd()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 4)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsNilWhenStartExceedsEnd()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 3, 2)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsNilForEmptySource()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('', 1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ByteAcceptsIntegralFloatIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 1.0)");

            await Assert.That(result.Number).IsEqualTo(76d).ConfigureAwait(false);
        }

        // NOTE: ByteTruncatesFloatIndices behavior is version-specific.
        // Lua 5.1/5.2: Silently truncates via floor (tested below)
        // Lua 5.3+: Throws "number has no integer representation" (tested below)

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task ByteTruncatesFloatIndicesLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.byte('Lua', 1.5)");

            await Assert
                .That(result.Number)
                .IsEqualTo(76d)
                .Because("Lua 5.1/5.2 should truncate 1.5 to 1 and return 'L' (76)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteErrorsOnNonIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('Lua', 1.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation for indices")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteErrorsOnNaNIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('Lua', 0/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation - NaN has none")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteErrorsOnInfinityIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('Lua', 1/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation - Infinity has none")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task ByteReturnsNilForNaNIndexLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.byte('Lua', 0/0)");

            // NaN floored is still NaN, which when cast to int produces invalid index
            await Assert
                .That(result.IsNil() || result.IsVoid())
                .IsTrue()
                .Because("Lua 5.1/5.2 should return nil for NaN index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task ByteNegativeFractionalIndexUsesFloorLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            // -0.5 floored is -1, which means "last character" in Lua
            DynValue result = script.DoString("return string.byte('Lua', -0.5)");

            // 'a' is the last character, ASCII 97
            await Assert
                .That(result.Number)
                .IsEqualTo(97d)
                .Because("Lua 5.1/5.2 should floor -0.5 to -1 (last char)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteAcceptsLargeIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // Large integer beyond double precision (2^53+1) but stored as integer is valid
            Script script = CreateScriptWithVersion(version);
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

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteAcceptsMathMaxIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // math.maxinteger (2^63-1) is valid when stored as integer
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.byte('a', math.maxinteger)");

            // Index is way beyond string length, should return nil
            await Assert
                .That(result.IsNil() || result.IsVoid())
                .IsTrue()
                .Because("Lua 5.3+ accepts math.maxinteger as index when stored as integer type")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteErrorsOnLargeFloatIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // 1e308 is a valid float but cannot be converted to integer
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.byte('a', 1e308)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ rejects floats that overflow integer range")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteAcceptsWholeNumberFloatIndexLua53Plus(LuaCompatibilityVersion version)
        {
            // 5.0 is a float with exact integer representation
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.byte('hello', 5.0)");

            // 'o' is ASCII 111
            await Assert
                .That(result.Number)
                .IsEqualTo(111d)
                .Because("Lua 5.3+ accepts whole number floats (5.0) as indices")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task ByteDistinguishesIntegerVsFloatForLargeValuesLua53Plus(
            LuaCompatibilityVersion version
        )
        {
            // This tests the critical distinction:
            // - 9007199254740993 as integer (2^53+1) is valid
            // - 9007199254740993 + 0.0 as float loses precision and may fail validation
            // In Lua 5.4, converting to float and back changes the value, so the float
            // version would round to 9007199254740992, which IS representable
            Script script = CreateScriptWithVersion(version);

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

        [global::TUnit.Core.Test]
        public async Task UnicodeReturnsFullUnicodeCodePoints()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task RepSupportsSeparatorsAndZeroCount()
        {
            Script script = CreateScript();
            DynValue repeated = script.DoString("return string.rep('ab', 3, '-')");
            DynValue zeroCount = script.DoString("return string.rep('ab', 0)");

            await Assert.That(repeated.String).IsEqualTo("ab-ab-ab").ConfigureAwait(false);
            await Assert.That(zeroCount.String).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FindReturnsMatchBoundaries()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task MatchReturnsFirstCapture()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return string.match('Version: 1.2.3', '%d+%.%d+%.%d+')"
            );

            await Assert.That(result.String).IsEqualTo("1.2.3").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ReverseReturnsEmptyStringForEmptyInput()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.reverse('')");

            await Assert.That(result.String).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GSubAppliesGlobalReplacement()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task SubHandlesNegativeIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.sub('NovaSharp', -5, -2)");

            await Assert.That(result.String).IsEqualTo("Shar").ConfigureAwait(false);
        }

        // NOTE: string.sub index behavior is version-specific.
        // Lua 5.1/5.2: Silently truncates via floor
        // Lua 5.3+: Throws "number has no integer representation"

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task SubTruncatesFloatIndicesLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.sub('Lua', 1.5, 3)");

            await Assert
                .That(result.String)
                .IsEqualTo("Lua")
                .Because("Lua 5.1/5.2 should truncate 1.5 to 1")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task SubErrorsOnNonIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

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

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task RepTruncatesFloatCountLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.rep('a', 2.5)");

            await Assert
                .That(result.String)
                .IsEqualTo("aa")
                .Because("Lua 5.1/5.2 should truncate 2.5 to 2")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task RepErrorsOnNonIntegerCountLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.rep('a', 2.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because("Lua 5.3+ requires integer representation for count")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatInterpolatesValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('Value: %0.2f', 3.14159)");

            await Assert.That(result.String).IsEqualTo("Value: 3.14").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StartsWithEndsWithContainsTreatNilAsFalse()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task StartsWithEndsWithContainsReturnTrueWhenMatchesPresent()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task DumpPrependsNovaSharpBase64Header()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task GMatchIteratesOverMatches()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GMatchWithInitParameterStartsAtSpecifiedPosition(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.4+ supports optional init parameter for string.gmatch
            Script script = CreateScriptWithVersion(version);
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

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task GMatchIgnoresInitParameterInLuaBelow54(LuaCompatibilityVersion version)
        {
            // Lua 5.1-5.3 ignore the third argument to string.gmatch
            Script script = CreateScriptWithVersion(version);
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

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GMatchWithNegativeInitStartsFromEnd(LuaCompatibilityVersion version)
        {
            // Negative init means offset from end of string (Lua 5.4+)
            Script script = CreateScriptWithVersion(version);
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
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua54);
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

        [global::TUnit.Core.Test]
        public async Task GMatchWithInitBeyondStringLengthReturnsNoMatches()
        {
            // Test init parameter beyond string length in Lua 5.4
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua54);
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

        [global::TUnit.Core.Test]
        public async Task DumpWrapsClrFunctionFailuresWithScriptRuntimeException()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task NovaSharpInitRegistersStringMetatable()
        {
            Script script = new();
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

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CharHandlesNaNAsZeroLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.char(0/0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CharHandlesPositiveInfinityAsZeroLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.char(1/0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CharHandlesNegativeInfinityAsZeroLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.char(-1/0)");

            await Assert.That(result.String.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.String[0]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CharTruncatesFloatValuesLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return string.char(65.5)");

            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CharErrorsOnNaNLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(0/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CharErrorsOnPositiveInfinityLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(1/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CharErrorsOnNegativeInfinityLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(-1/0)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CharErrorsOnNonIntegerFloatLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(65.5)")
            );

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharAcceptsNumericStringArguments()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char('65', '66')");

            await Assert.That(result.String).IsEqualTo("AB").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Octal format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatOctalBasic()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%o', 8)");

            await Assert.That(result.String).IsEqualTo("10").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalWithAlternateFlag()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#o', 8)");

            await Assert.That(result.String).IsEqualTo("010").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalAlternateFlagWithZero()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#o', 0)");

            await Assert.That(result.String).IsEqualTo("0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalWithFieldWidth()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%8o', 8)");

            await Assert.That(result.String).IsEqualTo("      10").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalWithZeroPadding()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%08o', 8)");

            await Assert.That(result.String).IsEqualTo("00000010").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalWithLeftAlign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%-8o', 8)");

            await Assert.That(result.String).IsEqualTo("10      ").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalWithLeftAlignAndAlternate()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%-#8o', 8)");

            await Assert.That(result.String).IsEqualTo("010     ").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalZeroPaddingWithAlternate()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#08o', 8)");

            await Assert.That(result.String).IsEqualTo("00000010").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Unsigned integer format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatUnsignedBasic()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%u', 42)");

            await Assert.That(result.String).IsEqualTo("42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatUnsignedWithFieldWidth()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%8u', 42)");

            await Assert.That(result.String).IsEqualTo("      42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatUnsignedWithZeroPadding()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%08u', 42)");

            await Assert.That(result.String).IsEqualTo("00000042").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Hex format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatHexLowercaseBasic()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%x', 255)");

            await Assert.That(result.String).IsEqualTo("ff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexUppercaseBasic()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%X', 255)");

            await Assert.That(result.String).IsEqualTo("FF").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithAlternateFlagLowercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#x', 255)");

            await Assert.That(result.String).IsEqualTo("0xff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithAlternateFlagUppercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#X', 255)");

            await Assert.That(result.String).IsEqualTo("0XFF").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithFieldWidth()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%8x', 255)");

            await Assert.That(result.String).IsEqualTo("      ff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithZeroPadding()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%08x', 255)");

            await Assert.That(result.String).IsEqualTo("000000ff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithLeftAlign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%-8x', 255)");

            await Assert.That(result.String).IsEqualTo("ff      ").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithPrecision()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%.4x', 255)");

            await Assert.That(result.String).IsEqualTo("00ff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexZeroPaddingWithAlternateLowercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#08x', 255)");

            await Assert.That(result.String).IsEqualTo("0x0000ff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexZeroPaddingWithAlternateUppercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%#08X', 255)");

            await Assert.That(result.String).IsEqualTo("0X0000FF").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexLeftAlignWithAlternate()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%-#8x', 255)");

            await Assert.That(result.String).IsEqualTo("0xff    ").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Integer format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatIntegerWithPositiveSign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%+d', 42)");

            await Assert.That(result.String).IsEqualTo("+42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatIntegerWithPositiveSpace()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('% d', 42)");

            await Assert.That(result.String).IsEqualTo(" 42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatIntegerPositiveSignOverridesSpace()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%+ d', 42)");

            await Assert.That(result.String).IsEqualTo("+42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatNegativeIntegerWithPositiveSign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%+d', -42)");

            await Assert.That(result.String).IsEqualTo("-42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatIntegerWithZeroPaddingAndPositiveSign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%+08d', 42)");

            await Assert.That(result.String).IsEqualTo("+0000042").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatNegativeIntegerWithZeroPadding()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%08d', -42)");

            // Note: Lua counts the minus sign as part of the width
            await Assert.That(result.String).IsEqualTo("-00000042").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatLeftAlignOverridesZeroPadding()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%-08d', 42)");

            await Assert.That(result.String).IsEqualTo("42      ").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Float format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatFloatWithPositiveSign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%+f', 3.14)");

            await Assert.That(result.String).StartsWith("+3.14").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatFloatWithPositiveSpace()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('% f', 3.14)");

            await Assert.That(result.String).StartsWith(" 3.14").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Exponent format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatExponentLowercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%e', 12345.6)");

            await Assert.That(result.String).Contains("e").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatExponentUppercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%E', 12345.6)");

            await Assert.That(result.String).Contains("E").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatGeneralLowercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%g', 0.0001234)");

            await Assert.That(result.String).IsNotEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatGeneralUppercase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%G', 0.0001234)");

            await Assert.That(result.String).IsNotEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatExponentWithPositiveSign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%+e', 12345.6)");

            await Assert.That(result.String).StartsWith("+").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Character format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatCharFromNumber()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%c', 65)");

            await Assert.That(result.String).IsEqualTo("A").ConfigureAwait(false);
        }

        // ========================================
        // string.format - String format specifier tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatStringWithPrecision()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%.3s', 'Hello')");

            await Assert.That(result.String).IsEqualTo("Hel").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatStringWithFieldWidth()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%10s', 'Hello')");

            await Assert.That(result.String).IsEqualTo("     Hello").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatStringWithLeftAlign()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%-10s', 'Hello')");

            await Assert.That(result.String).IsEqualTo("Hello     ").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Escape tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatPercentEscape()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('100%% complete')");

            await Assert.That(result.String).IsEqualTo("100% complete").ConfigureAwait(false);
        }

        // ========================================
        // string.format - Integer precision tests (Lua 5.3+ dual numeric type system)
        // ========================================

        [global::TUnit.Core.Test]
        public async Task FormatDecimalPreservesMathMaxinteger()
        {
            // math.maxinteger = 9223372036854775807 (2^63 - 1)
            // This value cannot be exactly represented as a double (loses precision)
            // With LuaNumber integer subtype, %d should preserve the exact value
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%d', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatDecimalPreservesMathMininteger()
        {
            // math.mininteger = -9223372036854775808 (-2^63)
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%d', math.mininteger)");

            await Assert
                .That(result.String)
                .IsEqualTo("-9223372036854775808")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatIntegerSpecifierPreservesMathMaxinteger()
        {
            // %i is equivalent to %d
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%i', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexPreservesLargeIntegers()
        {
            // math.maxinteger in hex should be 7fffffffffffffff
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%x', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("7fffffffffffffff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexUppercasePreservesLargeIntegers()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%X', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("7FFFFFFFFFFFFFFF").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOctalPreservesLargeIntegers()
        {
            // math.maxinteger in octal
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%o', math.maxinteger)");

            await Assert
                .That(result.String)
                .IsEqualTo("777777777777777777777")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatDecimalWithIntegerLiteral()
        {
            // Large integer literals should preserve precision
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%d', 9223372036854775807)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, "123", false)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, "123", false)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, null, true)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, null, true)]
        public async Task FormatDecimalWithFloatBehaviorByVersion(
            LuaCompatibilityVersion version,
            string expectedResult,
            bool expectsError
        )
        {
            // Lua 5.1/5.2: Float values are converted to integer (truncated)
            // Lua 5.3+: Float values that don't have integer representation throw an error
            Script script = CreateScriptWithVersion(version);

            if (expectsError)
            {
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
            else
            {
                DynValue result = script.DoString("return string.format('%d', 123.456)");
                await Assert
                    .That(result.String)
                    .IsEqualTo(expectedResult)
                    .Because($"Lua {version} should truncate float to integer for %d")
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Tests that string.format with %d accepts exact integer values in Lua 5.3+.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("123", "123")]
        [global::TUnit.Core.Arguments("0", "0")]
        [global::TUnit.Core.Arguments("-456", "-456")]
        [global::TUnit.Core.Arguments("math.maxinteger", "9223372036854775807")]
        [global::TUnit.Core.Arguments("math.mininteger", "-9223372036854775808")]
        [global::TUnit.Core.Arguments("42.0", "42")] // Whole number float should work
        [global::TUnit.Core.Arguments("-1.0", "-1")] // Negative whole number float
        public async Task FormatDecimalAcceptsIntegerValues(string luaExpression, string expected)
        {
            Script script = CreateScript();
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
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("0.5", "fractional")]
        [global::TUnit.Core.Arguments("-0.5", "negative fractional")]
        [global::TUnit.Core.Arguments("1e100", "large float beyond integer range")]
        [global::TUnit.Core.Arguments("-1e100", "large negative float")]
        [global::TUnit.Core.Arguments("0/0", "NaN")]
        [global::TUnit.Core.Arguments("1/0", "positive infinity")]
        [global::TUnit.Core.Arguments("-1/0", "negative infinity")]
        [global::TUnit.Core.Arguments("math.maxinteger + 0.5", "maxinteger plus fractional")]
        public async Task FormatDecimalRejectsNonIntegerValues(
            string luaExpression,
            string description
        )
        {
            Script script = CreateScript();

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
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("%i")]
        [global::TUnit.Core.Arguments("%o")]
        [global::TUnit.Core.Arguments("%u")]
        [global::TUnit.Core.Arguments("%x")]
        [global::TUnit.Core.Arguments("%X")]
        public async Task FormatIntegerSpecifiersRejectFloatValues(string specifier)
        {
            Script script = CreateScript();

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
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("123.456", "123")]
        [global::TUnit.Core.Arguments("0.5", "0")]
        [global::TUnit.Core.Arguments("-0.5", "-0")]
        [global::TUnit.Core.Arguments("42", "42")]
        public async Task FormatFloatAcceptsAnyNumericValue(
            string luaValue,
            string expectedIntegerPart
        )
        {
            ArgumentNullException.ThrowIfNull(luaValue);
            ArgumentNullException.ThrowIfNull(expectedIntegerPart);

            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task FormatHexWithNegativeInteger()
        {
            // -1 as unsigned 64-bit integer is all 1s
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%x', -1)");

            await Assert.That(result.String).IsEqualTo("ffffffffffffffff").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatHexWithMathMininteger()
        {
            // math.mininteger = -2^63 = 0x8000000000000000
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%x', math.mininteger)");

            await Assert.That(result.String).IsEqualTo("8000000000000000").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatDecimalWithBitwiseResult()
        {
            // Bitwise operations produce integer results - verify precision preserved through formatting
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return string.format('%d', math.maxinteger & math.maxinteger)"
            );

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatDecimalWithMathTointeger()
        {
            // math.tointeger returns integer subtype - verify precision preserved
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return string.format('%d', math.tointeger(9223372036854775807))"
            );

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatUnsignedWithMathMaxinteger()
        {
            // %u format specifier with large positive integer
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('%u', math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("9223372036854775807").ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModulePresets.Complete);
        }

        private static Script CreateScriptWithVersion(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
