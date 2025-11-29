#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class StringModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CharProducesStringFromByteValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65, 66, 67)");

            await Assert.That(result.String).IsEqualTo("ABC");
        }

        [global::TUnit.Core.Test]
        public async Task CharThrowsWhenArgumentCannotBeCoerced()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.char(\"not-a-number\")")
            );

            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task CharReturnsNullByteForZero()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(0)");

            await Assert.That(result.String.Length).IsEqualTo(1);
            await Assert.That(result.String[0]).IsEqualTo('\0');
        }

        [global::TUnit.Core.Test]
        public async Task CharReturnsMaxByteValue()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(255)");

            await Assert.That(result.String.Length).IsEqualTo(1);
            await Assert.That(result.String[0]).IsEqualTo((char)255);
        }

        [global::TUnit.Core.Test]
        public async Task CharReturnsEmptyStringWhenNoArgumentsProvided()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char()");

            await Assert.That(result.String).IsEmpty();
        }

        [global::TUnit.Core.Test]
        public async Task CharWrapsValuesOutsideByteRange()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(-1, 256)");

            await Assert.That(result.String.Length).IsEqualTo(2);
            await Assert.That(result.String[0]).IsEqualTo((char)255);
            await Assert.That(result.String[1]).IsEqualTo('\0');
        }

        [global::TUnit.Core.Test]
        public async Task CharAcceptsIntegralFloatValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65.0)");

            await Assert.That(result.String).IsEqualTo("A");
        }

        [global::TUnit.Core.Test]
        public async Task CharTruncatesFloatValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65.5)");

            await Assert.That(result.String).IsEqualTo("A");
        }

        [global::TUnit.Core.Test]
        public async Task LenReturnsStringLength()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.len('Nova')");

            await Assert.That(result.Number).IsEqualTo(4d);
        }

        [global::TUnit.Core.Test]
        public async Task LowerReturnsLowercaseString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.lower('NovaSharp')");

            await Assert.That(result.String).IsEqualTo("novasharp");
        }

        [global::TUnit.Core.Test]
        public async Task UpperReturnsUppercaseString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.upper('NovaSharp')");

            await Assert.That(result.String).IsEqualTo("NOVASHARP");
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

            await Assert.That(result.Tuple.Length).IsEqualTo(4);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3d);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(76d);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(117d);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(97d);
        }

        [global::TUnit.Core.Test]
        public async Task ByteDefaultsToFirstCharacter()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua')");

            await Assert.That(result.Number).IsEqualTo(76d);
        }

        [global::TUnit.Core.Test]
        public async Task ByteSupportsNegativeIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', -1)");

            await Assert.That(result.Number).IsEqualTo(97d);
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsNilWhenIndexPastEnd()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 4)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsNilWhenStartExceedsEnd()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 3, 2)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ByteReturnsNilForEmptySource()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('', 1)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ByteAcceptsIntegralFloatIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 1.0)");

            await Assert.That(result.Number).IsEqualTo(76d);
        }

        [global::TUnit.Core.Test]
        public async Task ByteTruncatesFloatIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 1.5)");

            await Assert.That(result.Number).IsEqualTo(76d);
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

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1d);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(256d);
        }

        [global::TUnit.Core.Test]
        public async Task RepSupportsSeparatorsAndZeroCount()
        {
            Script script = CreateScript();
            DynValue repeated = script.DoString("return string.rep('ab', 3, '-')");
            DynValue zeroCount = script.DoString("return string.rep('ab', 0)");

            await Assert.That(repeated.String).IsEqualTo("ab-ab-ab");
            await Assert.That(zeroCount.String).IsEmpty();
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

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(5d);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(9d);
        }

        [global::TUnit.Core.Test]
        public async Task MatchReturnsFirstCapture()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return string.match('Version: 1.2.3', '%d+%.%d+%.%d+')"
            );

            await Assert.That(result.String).IsEqualTo("1.2.3");
        }

        [global::TUnit.Core.Test]
        public async Task ReverseReturnsEmptyStringForEmptyInput()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.reverse('')");

            await Assert.That(result.String).IsEmpty();
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

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].String).IsEqualTo("baz bar baz");
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2d);
        }

        [global::TUnit.Core.Test]
        public async Task SubHandlesNegativeIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.sub('NovaSharp', -5, -2)");

            await Assert.That(result.String).IsEqualTo("Shar");
        }

        [global::TUnit.Core.Test]
        public async Task FormatInterpolatesValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('Value: %0.2f', 3.14159)");

            await Assert.That(result.String).IsEqualTo("Value: 3.14");
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

            await Assert.That(result.Tuple.Length).IsEqualTo(3);
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].Boolean).IsFalse();
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
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

            await Assert.That(result.Tuple.Length).IsEqualTo(3);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
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

            await Assert.That(result.String).StartsWith(StringModule.Base64DumpHeader);
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

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].String).IsEqualTo("one");
            await Assert.That(result.Tuple[1].String).IsEqualTo("two");
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
            await Assert.That(metatable).IsNotNull();

            DynValue indexTableValue = metatable.Get("__index");
            await Assert.That(indexTableValue.Type).IsEqualTo(DataType.Table);
            await Assert.That(indexTableValue.Table.Get("marker").String).IsEqualTo("value");
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

            await Assert.That(defaultResult).IsEqualTo(3);
            await Assert.That(zeroResult).IsNull();
            await Assert.That(positiveResult).IsEqualTo(3);
            await Assert.That(negativeResult).IsEqualTo(6);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
#pragma warning restore CA2007
