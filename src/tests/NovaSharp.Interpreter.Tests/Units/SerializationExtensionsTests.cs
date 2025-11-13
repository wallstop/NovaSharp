namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Serialization;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SerializationExtensionsTests
    {
        [Test]
        public void SerializePrimeTableFormatsEntries()
        {
            Table table = new Table(owner: null);
            table.Set(DynValue.NewString("answer"), DynValue.NewNumber(42));
            table.Set(DynValue.NewString("message"), DynValue.NewString("hello"));
            table.Set(DynValue.NewString("flag"), DynValue.NewBoolean(true));

            string serialized = table.Serialize(prefixReturn: true);

            string[] split = serialized.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries
            );

            Assert.That(split.Length, Is.EqualTo(5));
            Assert.That(split[0], Is.EqualTo("return {"));

            List<string> bodyLines = new List<string> { split[1], split[2], split[3] };

            Assert.That(
                bodyLines,
                Is.EquivalentTo(
                    new[] { "\tanswer = 42,", "\tmessage = \"hello\",", "\tflag = true," }
                )
            );

            Assert.That(split[4], Is.EqualTo("}"));
        }

        [Test]
        public void SerializeNestedTableRecurses()
        {
            Table inner = new Table(owner: null);
            inner.Set(DynValue.NewString("value"), DynValue.NewNumber(1));

            Table outer = new Table(owner: null);
            outer.Set(DynValue.NewString("inner"), DynValue.NewTable(inner));

            string serialized = outer.Serialize(prefixReturn: false);

            string expectedSegment =
                string.Join(Environment.NewLine, "\tinner = {", "\t\tvalue = 1,", "\t},")
                + Environment.NewLine;

            Assert.That(serialized, Does.Contain(expectedSegment));
        }

        [Test]
        public void SerializeInvalidIdentifierUsesBracketNotation()
        {
            Table table = new Table(owner: null);
            table.Set(DynValue.NewString("with space"), DynValue.NewNumber(3));
            table.Set(DynValue.NewString("local"), DynValue.NewNumber(4));

            string serialized = table.Serialize(prefixReturn: false);

            Assert.That(serialized, Does.Contain("\t[\"with space\"] = 3,"));
            Assert.That(serialized, Does.Contain("\t[\"local\"] = 4,"));
        }

        [Test]
        public void SerializeEmptyTableHonorsReturnPrefix()
        {
            Table table = new Table(owner: null);

            string serialized = table.Serialize(prefixReturn: true);

            Assert.That(serialized, Is.EqualTo("return {}" + Environment.NewLine));
        }

        [Test]
        public void SerializeValueEscapesStringAndHandlesTuple()
        {
            DynValue str = DynValue.NewString("line\nbreak");
            Assert.That(
                SerializationExtensions.SerializeValue(str),
                Is.EqualTo("\"line\\nbreak\"")
            );

            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(5), DynValue.NewNumber(6));
            Assert.That(SerializationExtensions.SerializeValue(tuple), Is.EqualTo("5"));
        }

        [Test]
        public void SerializeRoundtripExecutesInLua()
        {
            Table nested = new Table(owner: null);
            nested.Set(DynValue.NewNumber(1), DynValue.NewString("first"));

            Table table = new Table(owner: null);
            table.Set(DynValue.NewString("answer"), DynValue.NewNumber(42));
            table.Set(DynValue.NewString("nested"), DynValue.NewTable(nested));

            string serialized = table.Serialize(prefixReturn: true);

            Script script = new Script(CoreModules.Basic);
            DynValue evaluated = script.DoString(serialized);

            Assert.That(evaluated.Type, Is.EqualTo(DataType.Table));
            Assert.That(evaluated.Table.Get("answer").Number, Is.EqualTo(42));

            DynValue nestedValue = evaluated.Table.Get("nested");
            Assert.That(nestedValue.Type, Is.EqualTo(DataType.Table));
            Assert.That(nestedValue.Table.Get(1).String, Is.EqualTo("first"));
        }

        [Test]
        public void SerializeNonPrimeTableThrows()
        {
            Script script = new Script(default);
            Table table = new Table(script);
            table.Set(DynValue.NewNumber(1), DynValue.NewNumber(2));

            Assert.That(() => table.Serialize(), Throws.TypeOf<ScriptRuntimeException>());
        }

        [Test]
        public void SerializeValueTableOwnedByScriptThrows()
        {
            Script script = new Script(default);
            Table table = new Table(script);
            DynValue tableValue = DynValue.NewTable(table);

            Assert.That(() => tableValue.SerializeValue(), Throws.TypeOf<ScriptRuntimeException>());
        }
    }
}
