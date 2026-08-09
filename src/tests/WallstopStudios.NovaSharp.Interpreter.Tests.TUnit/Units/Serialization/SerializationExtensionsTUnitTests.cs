namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Serialization;

    public sealed class SerializationExtensionsTUnitTests
    {
        private static readonly string[] LineSplitSeparator = { Environment.NewLine };
        private static readonly string[] ExpectedBodyLines =
        {
            "\tanswer = 42,",
            "\tmessage = \"hello\",",
            "\tflag = true,",
        };

        [global::TUnit.Core.Test]
        public async Task SerializePrimeTableFormatsEntries()
        {
            Table table = new(owner: null);
            table.SetValue(LuaValue.NewString("answer"), LuaValue.NewNumber(42));
            table.SetValue(LuaValue.NewString("message"), LuaValue.NewString("hello"));
            table.SetValue(LuaValue.NewString("flag"), LuaValue.NewBoolean(true));

            string serialized = table.Serialize(prefixReturn: true);

            string[] split = serialized.Split(
                LineSplitSeparator,
                StringSplitOptions.RemoveEmptyEntries
            );

            await Assert.That(split.Length).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(split[0]).IsEqualTo("return {").ConfigureAwait(false);

            List<string> bodyLines = new() { split[1], split[2], split[3] };
            await Assert.That(bodyLines.Count).IsEqualTo(3).ConfigureAwait(false);

            foreach (string expected in ExpectedBodyLines)
            {
                await Assert.That(bodyLines.Contains(expected)).IsTrue().ConfigureAwait(false);
            }

            await Assert.That(split[4]).IsEqualTo("}").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeNestedTableRecurses()
        {
            Table inner = new(owner: null);
            inner.SetValue(LuaValue.NewString("value"), LuaValue.NewNumber(1));

            Table outer = new(owner: null);
            outer.SetValue(LuaValue.NewString("inner"), LuaValue.NewTable(inner));

            string serialized = outer.Serialize(prefixReturn: false);

            string expectedSegment =
                string.Join(Environment.NewLine, "\tinner = {", "\t\tvalue = 1,", "\t},")
                + Environment.NewLine;

            await Assert.That(serialized).Contains(expectedSegment).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeInvalidIdentifierUsesBracketNotation()
        {
            Table table = new(owner: null);
            table.SetValue(LuaValue.NewString("with space"), LuaValue.NewNumber(3));
            table.SetValue(LuaValue.NewString("local"), LuaValue.NewNumber(4));

            string serialized = table.Serialize(prefixReturn: false);

            await Assert.That(serialized).Contains("\t[\"with space\"] = 3,").ConfigureAwait(false);
            await Assert.That(serialized).Contains("\t[\"local\"] = 4,").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeNonStringAndDigitPrefixedKeysUseIndexerNotation()
        {
            Table table = new(owner: null);
            table.SetValue(LuaValue.NewNumber(5), LuaValue.NewString("value"));
            table.SetValue(LuaValue.NewString("1start"), LuaValue.NewNumber(10));

            string serialized = table.Serialize(prefixReturn: false);

            await Assert.That(serialized).Contains("\t[5] = \"value\",").ConfigureAwait(false);
            await Assert.That(serialized).Contains("\t[\"1start\"] = 10,").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeEmptyTableHonorsReturnPrefix()
        {
            Table table = new(owner: null);

            string serialized = table.Serialize(prefixReturn: true);

            await Assert
                .That(serialized)
                .IsEqualTo("return {}" + Environment.NewLine)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueEscapesStringAndHandlesTuple()
        {
            LuaValue str = LuaValue.NewString("line\nbreak");
            await Assert
                .That(SerializationExtensions.SerializeValue(str))
                .IsEqualTo("\"line\\nbreak\"");

            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(5), LuaValue.NewNumber(6));
            await Assert
                .That(SerializationExtensions.SerializeValue(tuple))
                .IsEqualTo("5")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueReturnsNilForVoidAndNil()
        {
            await Assert
                .That(SerializationExtensions.SerializeValue(LuaValue.Nil))
                .IsEqualTo("nil");
            await Assert
                .That(SerializationExtensions.SerializeValue(LuaValue.Void))
                .IsEqualTo("nil");
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueSerializesBooleans()
        {
            await Assert
                .That(SerializationExtensions.SerializeValue(LuaValue.NewBoolean(true)))
                .IsEqualTo("true");
            await Assert
                .That(SerializationExtensions.SerializeValue(LuaValue.NewBoolean(false)))
                .IsEqualTo("false");
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueTupleWithNoValuesReturnsNil()
        {
            LuaValue emptyTuple = LuaValue.NewTuple(Array.Empty<LuaValue>());

            await Assert
                .That(SerializationExtensions.SerializeValue(emptyTuple))
                .IsEqualTo("nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueUsesInvariantCultureForNumbers()
        {
            LuaValue number = LuaValue.NewNumber(1234.5);

            await Assert
                .That(SerializationExtensions.SerializeValue(number))
                .IsEqualTo("1234.5")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeRoundtripExecutesInLua()
        {
            Table nested = new(owner: null);
            nested.SetValue(LuaValue.NewNumber(1), LuaValue.NewString("first"));

            Table table = new(owner: null);
            table.SetValue(LuaValue.NewString("answer"), LuaValue.NewNumber(42));
            table.SetValue(LuaValue.NewString("nested"), LuaValue.NewTable(nested));

            string serialized = table.Serialize(prefixReturn: true);

            Script script = new(CoreModules.Basic);
            LuaValue evaluated = script.DoString(serialized);

            await Assert.That(evaluated.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(evaluated.Table.Get("answer").Number)
                .IsEqualTo(42)
                .ConfigureAwait(false);

            LuaValue nestedValue = evaluated.Table.Get("nested");
            await Assert.That(nestedValue.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(nestedValue.Table.Get(1).String)
                .IsEqualTo("first")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeNonPrimeTableThrows()
        {
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            Table table = new(script);
            table.SetValue(LuaValue.NewNumber(1), LuaValue.NewNumber(2));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Serialize()
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueTableOwnedByScriptThrows()
        {
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            Table table = new(script);
            LuaValue tableValue = LuaValue.NewTable(table);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                tableValue.SerializeValue()
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeThrowsWhenTableIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                SerializationExtensions.Serialize((Table)null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("table").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueThrowsWhenValueIsNull()
        {
            string serialized = SerializationExtensions.SerializeValue(default);

            await Assert.That(serialized).IsEqualTo("nil").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueThrowsForNonPrimitiveValues()
        {
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil, "nonPrimitive");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                callback.SerializeValue()
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Value is not a primitive value")
                .ConfigureAwait(false);
        }
    }
}
