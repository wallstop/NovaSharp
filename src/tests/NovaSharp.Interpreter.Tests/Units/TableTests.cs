namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TableTests
    {
        [Test]
        public void ConstructorWithArrayValuesInitializesSequentialEntries()
        {
            Script script = new();
            DynValue[] values = new[] { DynValue.NewNumber(10), DynValue.NewString("two") };

            Table table = new(script, values);

            Assert.Multiple(() =>
            {
                Assert.That(table.Length, Is.EqualTo(2));
                Assert.That(table.RawGet(1).Number, Is.EqualTo(10));
                Assert.That(table.RawGet(2).String, Is.EqualTo("two"));
            });
        }

        [Test]
        public void ClearRemovesAllEntries()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(1));
            table.Set("name", DynValue.NewString("value"));

            table.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(table.Length, Is.EqualTo(0));
                Assert.That(table.RawGet(1), Is.Null);
                Assert.That(table.RawGet("name"), Is.Null);
            });
        }

        [Test]
        public void LengthCacheInvalidatesWhenEntriesChange()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            table.Set(3, DynValue.NewNumber(30));

            Assert.That(table.Length, Is.EqualTo(3));

            table.Set(2, DynValue.Nil);
            Assert.That(
                table.Length,
                Is.EqualTo(1),
                "Setting nil should invalidate the cached length."
            );

            table.Append(DynValue.NewNumber(40));
            Assert.Multiple(() =>
            {
                Assert.That(table.Length, Is.EqualTo(3));
                Assert.That(table.RawGet(2).Number, Is.EqualTo(40));
                Assert.That(table.RawGet(3).Number, Is.EqualTo(30));
            });
        }

        [Test]
        public void NextKeySkipsNilEntriesAndHandlesTermination()
        {
            Table table = new(new Script());
            table.Set("first", DynValue.Nil);
            table.Set("second", DynValue.NewNumber(2));
            table.Set(3, DynValue.NewNumber(3));

            TablePair? first = table.NextKey(DynValue.Nil);
            Assert.That(first.HasValue, Is.True);
            Assert.That(first.Value.Key.String, Is.EqualTo("second"));

            TablePair? second = table.NextKey(first.Value.Key);
            Assert.That(second.HasValue, Is.True);
            Assert.That(second.Value.Key.Number, Is.EqualTo(3));

            TablePair? tail = table.NextKey(second.Value.Key);
            Assert.That(tail, Is.EqualTo(TablePair.Nil));

            TablePair? missing = table.NextKey(DynValue.NewString("missing"));
            Assert.That(missing, Is.Null);
        }

        [Test]
        public void NextKeyReturnsNullWhenKeyUnknown()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(1));

            TablePair? unknown = table.NextKey(DynValue.NewNumber(999));

            Assert.That(unknown, Is.Null);
        }

        [Test]
        public void NextKeyHandlesNonIntegralNumberKeys()
        {
            Script script = new();
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(2.5);
            table.Set(fractionalKey, DynValue.NewString("head"));
            table.Set("tail", DynValue.NewNumber(7));

            TablePair? next = table.NextKey(fractionalKey);

            Assert.Multiple(() =>
            {
                Assert.That(next.HasValue, Is.True);
                Assert.That(next.Value.Key.String, Is.EqualTo("tail"));
            });
        }

        [Test]
        public void CollectDeadKeysRemovesNilEntries()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            Assert.That(table.Length, Is.EqualTo(2));

            table.Set(2, DynValue.Nil);
            table.CollectDeadKeys();

            Assert.Multiple(() =>
            {
                Assert.That(table.RawGet(2), Is.Null);
                Assert.That(table.Length, Is.EqualTo(1));
            });
        }

        [Test]
        public void AppendThrowsWhenValueOwnedByDifferentScript()
        {
            Script scriptA = new();
            Script scriptB = new();
            Table table = new(scriptA);
            DynValue foreignValue = DynValue.NewTable(scriptB);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Append(foreignValue)
            );

            Assert.That(exception.Message, Does.Contain("resources owned by different scripts"));
        }

        [Test]
        public void SetDynValueNaNThrows()
        {
            Table table = new(new Script());
            DynValue nanKey = DynValue.NewNumber(double.NaN);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(nanKey, DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("table index is NaN"));
        }

        [Test]
        public void SetKeysThrowsWhenPathMissing()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(new object[] { "missing", "child" }, DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("did not point to anything"));
        }

        [Test]
        public void SetKeysThrowsWhenIntermediateIsNotTable()
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(new object[] { "leaf", "child" }, DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("did not point to a table"));
        }

        [Test]
        public void SetObjectArrayThrowsWhenKeysArrayIsNull()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set((object[])null, DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("table index is nil"));
        }

        [Test]
        public void SetObjectArrayThrowsWhenKeysArrayEmpty()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(Array.Empty<object>(), DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("table index is nil"));
        }

        [Test]
        public void SetObjectRoutesStringsNumbersAndValues()
        {
            Script script = new();
            Table table = new(script);

            table.Set((object)"name", DynValue.NewString("nova"));
            table.Set((object)4, DynValue.NewNumber(4));

            double fractionalKey = 4.5;
            table.Set((object)fractionalKey, DynValue.NewNumber(5));

            Assert.Multiple(() =>
            {
                Assert.That(table.RawGet("name").String, Is.EqualTo("nova"));
                Assert.That(table.RawGet(4).Number, Is.EqualTo(4));
                Assert.That(table.RawGet((object)fractionalKey).Number, Is.EqualTo(5));
            });
        }

        [Test]
        public void SetDynValueNumberWithFractionStoresEntry()
        {
            Script script = new();
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(3.25);

            table.Set(fractionalKey, DynValue.NewString("fractional"));

            DynValue result = table.RawGet(fractionalKey);
            Assert.That(result.String, Is.EqualTo("fractional"));
        }

        [Test]
        public void SetObjectArrayCreatesNestedTable()
        {
            Script script = new();
            Table table = new(script);
            table.Set("child", DynValue.NewTable(script));

            table.Set(new object[] { "child", "leaf" }, DynValue.NewString("value"));

            Assert.That(table.RawGet("child", "leaf").String, Is.EqualTo("value"));
        }

        [Test]
        public void GetParamsReturnsNestedValue()
        {
            Script script = new();
            Table table = new(script);
            DynValue child = DynValue.NewTable(script);
            child.Table.Set(1, DynValue.NewString("inner"));
            table.Set("child", child);

            DynValue value = table.Get("child", 1);

            Assert.That(value.String, Is.EqualTo("inner"));
        }

        [Test]
        public void GetObjectReturnsNilWhenKeyMissing()
        {
            Table table = new(new Script());

            DynValue value = table.Get((object)"missing");

            Assert.That(value.IsNil(), Is.True);
        }

        [Test]
        public void GetParamsReturnsNilWhenNoKeysProvided()
        {
            Table table = new(new Script());

            DynValue value = table.Get(Array.Empty<object>());

            Assert.That(value.IsNil(), Is.True);
        }

        [Test]
        public void RawGetParamsReturnsNullWhenKeysMissing()
        {
            Table table = new(new Script());

            DynValue value = table.RawGet(Array.Empty<object>());

            Assert.That(value, Is.Null);
        }

        [Test]
        public void RemoveParamsReturnsFalseWhenNoKeys()
        {
            Table table = new(new Script());
            Assert.That(table.Remove(Array.Empty<object>()), Is.False);
        }

        [Test]
        public void RawGetObjectReturnsNullWhenKeyIsNull()
        {
            Table table = new(new Script());
            Assert.That(table.RawGet((object)null), Is.Null);
        }

        [Test]
        public void RawGetObjectReturnsValueWhenKeyExists()
        {
            Table table = new(new Script());
            table.Set("existing", DynValue.NewNumber(42));

            DynValue value = table.RawGet((object)"existing");

            Assert.That(value.Number, Is.EqualTo(42));
        }

        [Test]
        public void RawGetParamsReturnsNullWhenArrayIsNull()
        {
            Table table = new(new Script());

            DynValue value = table.RawGet((object[])null);

            Assert.That(value, Is.Null);
        }

        [Test]
        public void SetObjectThrowsWhenKeyIsNull()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set((object)null, DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("table index is nil"));
        }

        [Test]
        public void RemoveStringKeyDeletesEntry()
        {
            Table table = new(new Script());
            table.Set("key", DynValue.NewNumber(12));

            bool removed = table.Remove("key");

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(table.RawGet("key"), Is.Null);
            });
        }

        [Test]
        public void RemoveDynValueNumberUsesIntegralPath()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(42));

            bool removed = table.Remove(DynValue.NewNumber(1));

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(table.RawGet(1), Is.Null);
            });
        }

        [Test]
        public void RemoveDynValueStringDeletesEntry()
        {
            Table table = new(new Script());
            table.Set("value", DynValue.NewNumber(100));

            bool removed = table.Remove(DynValue.NewString("value"));

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(table.RawGet("value"), Is.Null);
            });
        }

        [Test]
        public void RemoveObjectTreatsBoxedStringAsStringKey()
        {
            Table table = new(new Script());
            table.Set("boxed", DynValue.NewNumber(5));

            bool removed = table.Remove((object)"boxed");

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(table.RawGet("boxed"), Is.Null);
            });
        }

        [Test]
        public void RemoveObjectTreatsBoxedIntAsArrayKey()
        {
            Table table = new(new Script());
            table.Set(3, DynValue.NewNumber(9));

            bool removed = table.Remove((object)3);

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(table.RawGet(3), Is.Null);
            });
        }

        [Test]
        public void RemoveObjectDynValueKeyRemovesNonIntegralEntry()
        {
            Script script = new();
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(8.5);
            table.Set(fractionalKey, DynValue.NewString("value"));

            bool removed = table.Remove((object)fractionalKey);

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(table.RawGet(fractionalKey), Is.Null);
            });
        }

        [Test]
        public void RemoveObjectArrayDeletesNestedEntry()
        {
            Script script = new();
            Table table = new(script);
            table.Set("branch", DynValue.NewTable(script));
            table.Set(new object[] { "branch", "leaf" }, DynValue.NewNumber(6));

            bool removed = table.Remove(new object[] { "branch", "leaf" });

            DynValue value = table.Get("branch", "leaf") ?? DynValue.Nil;

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(value.IsNil(), Is.True);
            });
        }

        [Test]
        public void RawGetParamsNavigatesNestedTables()
        {
            Script script = new();
            Table table = new(script);
            Table inner = new(script);
            inner.Set("leaf", DynValue.NewNumber(99));
            table.Set("child", DynValue.NewTable(script));
            table.Set(new object[] { "child", "leaf" }, DynValue.NewNumber(7));

            DynValue result = table.RawGet("child", "leaf");

            Assert.That(result.Number, Is.EqualTo(7));
        }

        [Test]
        public void KeysEnumeratesInsertedEntries()
        {
            Table table = new(new Script());
            table.Set("alpha", DynValue.NewNumber(1));
            table.Set(2, DynValue.NewNumber(2));

            object[] keys = table.Keys.Select(k => k.ToObject()).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(keys.Length, Is.EqualTo(2));
                Assert.That(keys, Does.Contain("alpha"));
                Assert.That(keys, Does.Contain((double)2));
            });
        }
    }
}
