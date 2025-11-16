namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TableTests
    {
        [Test]
        public void LengthCacheInvalidatesWhenEntriesChange()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));

            Assert.That(table.Length, Is.EqualTo(2));

            table.Set(2, DynValue.Nil);
            Assert.That(
                table.Length,
                Is.EqualTo(1),
                "Setting nil should invalidate the cached length."
            );

            table.Append(DynValue.NewNumber(30));
            Assert.Multiple(() =>
            {
                Assert.That(table.Length, Is.EqualTo(2));
                Assert.That(table.RawGet(2).Number, Is.EqualTo(30));
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
    }
}
