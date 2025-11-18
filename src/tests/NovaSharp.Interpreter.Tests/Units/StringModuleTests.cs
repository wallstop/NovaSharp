namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StringModuleTests
    {
        [Test]
        public void CharProducesStringFromByteValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65, 66, 67)");

            Assert.That(result.String, Is.EqualTo("ABC"));
        }

        [Test]
        public void CharThrowsWhenArgumentCannotBeCoerced()
        {
            Script script = CreateScript();

            Assert.That(
                () => script.DoString("return string.char(\"not-a-number\")"),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void CharReturnsNullByteForZero()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(0)");

            Assert.Multiple(() =>
            {
                Assert.That(result.String.Length, Is.EqualTo(1));
                Assert.That(result.String[0], Is.EqualTo('\0'));
            });
        }

        [Test]
        public void CharReturnsMaxByteValue()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(255)");

            Assert.Multiple(() =>
            {
                Assert.That(result.String.Length, Is.EqualTo(1));
                Assert.That(result.String[0], Is.EqualTo((char)255));
            });
        }

        [Test]
        public void CharReturnsEmptyStringWhenNoArgumentsProvided()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char()");

            Assert.That(result.String, Is.Empty);
        }

        [Test]
        public void CharWrapsValuesOutsideByteRange()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(-1, 256)");

            Assert.Multiple(() =>
            {
                Assert.That(result.String.Length, Is.EqualTo(2));
                Assert.That(result.String[0], Is.EqualTo((char)255));
                Assert.That(result.String[1], Is.EqualTo('\0'));
            });
        }

        [Test]
        public void CharAcceptsIntegralFloatValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65.0)");

            Assert.That(result.String, Is.EqualTo("A"));
        }

        [Test]
        public void CharTruncatesFloatValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.char(65.5)");

            Assert.That(result.String, Is.EqualTo("A"));
        }

        [Test]
        public void LenReturnsStringLength()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.len('Nova')");

            Assert.That(result.Number, Is.EqualTo(4));
        }

        [Test]
        public void LowerReturnsLowercaseString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.lower('NovaSharp')");

            Assert.That(result.String, Is.EqualTo("novasharp"));
        }

        [Test]
        public void UpperReturnsUppercaseString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.upper('NovaSharp')");

            Assert.That(result.String, Is.EqualTo("NOVASHARP"));
        }

        [Test]
        public void ByteReturnsByteCodesForSubstring()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local codes = {string.byte('Lua', 1, 3)}
                return #codes, codes[1], codes[2], codes[3]
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(3));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(76));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(117));
                Assert.That(result.Tuple[3].Number, Is.EqualTo(97));
            });
        }

        [Test]
        public void ByteDefaultsToFirstCharacter()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua')");

            Assert.That(result.Number, Is.EqualTo(76));
        }

        [Test]
        public void ByteSupportsNegativeIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', -1)");

            Assert.That(result.Number, Is.EqualTo(97));
        }

        [Test]
        public void ByteReturnsNilWhenIndexPastEnd()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 4)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void ByteReturnsNilWhenStartExceedsEnd()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 3, 2)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void ByteReturnsNilForEmptySource()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('', 1)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void ByteAcceptsIntegralFloatIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 1.0)");

            Assert.That(result.Number, Is.EqualTo(76));
        }

        [Test]
        public void ByteTruncatesFloatIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.byte('Lua', 1.5)");

            Assert.That(result.Number, Is.EqualTo(76));
        }

        [Test]
        public void UnicodeReturnsFullUnicodeCodePoints()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local codes = {string.unicode('\u{0100}')}
                return #codes, codes[1]
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(1));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(256));
            });
        }

        [Test]
        public void RepSupportsSeparatorsAndZeroCount()
        {
            Script script = CreateScript();
            DynValue repeated = script.DoString("return string.rep('ab', 3, '-')");
            DynValue zeroCount = script.DoString("return string.rep('ab', 0)");

            Assert.Multiple(() =>
            {
                Assert.That(repeated.String, Is.EqualTo("ab-ab-ab"));
                Assert.That(zeroCount.String, Is.Empty);
            });
        }

        [Test]
        public void FindReturnsMatchBoundaries()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local startIndex, endIndex = string.find('NovaSharp', 'Sharp')
                return startIndex, endIndex
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(5));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(9));
            });
        }

        [Test]
        public void MatchReturnsFirstCapture()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return string.match('Version: 1.2.3', '%d+%.%d+%.%d+')"
            );

            Assert.That(result.String, Is.EqualTo("1.2.3"));
        }

        [Test]
        public void ReverseReturnsEmptyStringForEmptyInput()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.reverse('')");

            Assert.That(result.String, Is.Empty);
        }

        [Test]
        public void GsubAppliesGlobalReplacement()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local replaced, count = string.gsub('foo bar foo', 'foo', 'baz')
                return replaced, count
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].String, Is.EqualTo("baz bar baz"));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(2));
            });
        }

        [Test]
        public void SubHandlesNegativeIndices()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.sub('NovaSharp', -5, -2)");

            Assert.That(result.String, Is.EqualTo("Shar"));
        }

        [Test]
        public void FormatInterpolatesValues()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return string.format('Value: %0.2f', 3.14159)");

            Assert.That(result.String, Is.EqualTo("Value: 3.14"));
        }

        [Test]
        public void StartsWithEndsWithContainsTreatNilAsFalse()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                return string.startswith(nil, 'prefix'),
                       string.endswith('suffix', nil),
                       string.contains(nil, nil)
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(result.Tuple[1].Boolean, Is.False);
                Assert.That(result.Tuple[2].Boolean, Is.False);
            });
        }

        [Test]
        public void StartsWithEndsWithContainsReturnTrueWhenMatchesPresent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                return string.startswith('NovaSharp', 'Nova'),
                       string.endswith('NovaSharp', 'Sharp'),
                       string.contains('NovaSharp', 'Shar')
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].Boolean, Is.True);
                Assert.That(result.Tuple[2].Boolean, Is.True);
            });
        }

        [Test]
        public void DumpPrependsNovaSharpBase64Header()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local function increment(x) return x + 1 end
                return string.dump(increment)
                "
            );

            Assert.That(result.String, Does.StartWith(StringModule.Base64DumpHeader));
        }

        [Test]
        public void GmatchIteratesOverMatches()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local iter = string.gmatch('one two', '%w+')
                return iter(), iter()
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].String, Is.EqualTo("one"));
                Assert.That(result.Tuple[1].String, Is.EqualTo("two"));
            });
        }

        [Test]
        public void DumpWrapsClrFunctionFailuresWithScriptRuntimeException()
        {
            Script script = CreateScript();
            script.Globals.Set("callback", DynValue.NewCallback((_, _) => DynValue.Nil));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.dump(callback)")
            );

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain("function expected").IgnoreCase);
        }

        [Test]
        public void NovaSharpInitRegistersStringMetatable()
        {
            Script script = new Script();
            Table globals = script.Globals;
            Table stringTable = new(script);
            stringTable.Set("marker", DynValue.NewString("value"));

            StringModule.NovaSharpInit(globals, stringTable);

            Table metatable = script.GetTypeMetatable(DataType.String);
            Assert.That(metatable, Is.Not.Null);

            DynValue indexTableValue = metatable.Get("__index");
            Assert.That(indexTableValue.Type, Is.EqualTo(DataType.Table));
            Assert.That(indexTableValue.Table.Get("marker").String, Is.EqualTo("value"));
        }

        [Test]
        public void AdjustIndexHandlesNilZeroPositiveAndNegativeInputs()
        {
            MethodInfo adjustIndex = typeof(StringModule).GetMethod(
                "AdjustIndex",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.That(adjustIndex, Is.Not.Null);

            object defaultResult = adjustIndex!.Invoke(
                null,
                new object[] { "Nova", DynValue.Nil, 3 }
            );
            object zeroResult = adjustIndex.Invoke(
                null,
                new object[] { "Nova", DynValue.NewNumber(0), 3 }
            );
            object positiveResult = adjustIndex.Invoke(
                null,
                new object[] { "Nova", DynValue.NewNumber(4), 3 }
            );
            object negativeResult = adjustIndex.Invoke(
                null,
                new object[] { "Nova", DynValue.NewNumber(-2), 3 }
            );

            Assert.Multiple(() =>
            {
                Assert.That(defaultResult, Is.EqualTo(3));
                Assert.That(zeroResult, Is.Null);
                Assert.That(positiveResult, Is.EqualTo(3));
                Assert.That(negativeResult, Is.EqualTo(6));
            });
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
