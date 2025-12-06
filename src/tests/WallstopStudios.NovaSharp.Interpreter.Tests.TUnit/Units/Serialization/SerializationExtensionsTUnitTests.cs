namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
            table.Set(DynValue.NewString("answer"), DynValue.NewNumber(42));
            table.Set(DynValue.NewString("message"), DynValue.NewString("hello"));
            table.Set(DynValue.NewString("flag"), DynValue.NewBoolean(true));

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
            inner.Set(DynValue.NewString("value"), DynValue.NewNumber(1));

            Table outer = new(owner: null);
            outer.Set(DynValue.NewString("inner"), DynValue.NewTable(inner));

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
            table.Set(DynValue.NewString("with space"), DynValue.NewNumber(3));
            table.Set(DynValue.NewString("local"), DynValue.NewNumber(4));

            string serialized = table.Serialize(prefixReturn: false);

            await Assert.That(serialized).Contains("\t[\"with space\"] = 3,").ConfigureAwait(false);
            await Assert.That(serialized).Contains("\t[\"local\"] = 4,").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeNonStringAndDigitPrefixedKeysUseIndexerNotation()
        {
            Table table = new(owner: null);
            table.Set(DynValue.NewNumber(5), DynValue.NewString("value"));
            table.Set(DynValue.NewString("1start"), DynValue.NewNumber(10));

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
            DynValue str = DynValue.NewString("line\nbreak");
            await Assert
                .That(SerializationExtensions.SerializeValue(str))
                .IsEqualTo("\"line\\nbreak\"");

            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(5), DynValue.NewNumber(6));
            await Assert
                .That(SerializationExtensions.SerializeValue(tuple))
                .IsEqualTo("5")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueReturnsNilForVoidAndNil()
        {
            await Assert
                .That(SerializationExtensions.SerializeValue(DynValue.Nil))
                .IsEqualTo("nil");
            await Assert
                .That(SerializationExtensions.SerializeValue(DynValue.Void))
                .IsEqualTo("nil");
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueSerializesBooleans()
        {
            await Assert
                .That(SerializationExtensions.SerializeValue(DynValue.NewBoolean(true)))
                .IsEqualTo("true");
            await Assert
                .That(SerializationExtensions.SerializeValue(DynValue.NewBoolean(false)))
                .IsEqualTo("false");
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueTupleWithNoValuesReturnsNil()
        {
            DynValue emptyTuple = DynValue.NewTuple(Array.Empty<DynValue>());

            await Assert
                .That(SerializationExtensions.SerializeValue(emptyTuple))
                .IsEqualTo("nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueUsesInvariantCultureForNumbers()
        {
            DynValue number = DynValue.NewNumber(1234.5);

            await Assert
                .That(SerializationExtensions.SerializeValue(number))
                .IsEqualTo("1234.5")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeRoundtripExecutesInLua()
        {
            Table nested = new(owner: null);
            nested.Set(DynValue.NewNumber(1), DynValue.NewString("first"));

            Table table = new(owner: null);
            table.Set(DynValue.NewString("answer"), DynValue.NewNumber(42));
            table.Set(DynValue.NewString("nested"), DynValue.NewTable(nested));

            string serialized = table.Serialize(prefixReturn: true);

            Script script = new(CoreModules.Basic);
            DynValue evaluated = script.DoString(serialized);

            await Assert.That(evaluated.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(evaluated.Table.Get("answer").Number)
                .IsEqualTo(42)
                .ConfigureAwait(false);

            DynValue nestedValue = evaluated.Table.Get("nested");
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
            table.Set(DynValue.NewNumber(1), DynValue.NewNumber(2));

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
            DynValue tableValue = DynValue.NewTable(table);

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
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                SerializationExtensions.SerializeValue(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("dynValue").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeValueThrowsForNonPrimitiveValues()
        {
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil, "nonPrimitive");

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
