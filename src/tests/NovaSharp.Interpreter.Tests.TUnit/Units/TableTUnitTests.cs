#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    public sealed class TableTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorWithArrayValuesInitializesSequentialEntries()
        {
            Script script = new();
            DynValue[] values = new[] { DynValue.NewNumber(10), DynValue.NewString("two") };

            Table table = new(script, values);

            await Assert.That(table.Length).IsEqualTo(2);
            await Assert.That(table.RawGet(1).Number).IsEqualTo(10);
            await Assert.That(table.RawGet(2).String).IsEqualTo("two");
        }

        [global::TUnit.Core.Test]
        public async Task ClearRemovesAllEntries()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(1));
            table.Set("name", DynValue.NewString("value"));

            table.Clear();

            await Assert.That(table.Length).IsEqualTo(0);
            await Assert.That(table.RawGet(1)).IsNull();
            await Assert.That(table.RawGet("name")).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task LengthCacheInvalidatesWhenEntriesChange()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            table.Set(3, DynValue.NewNumber(30));

            await Assert.That(table.Length).IsEqualTo(3);

            table.Set(2, DynValue.Nil);
            await Assert.That(table.Length).IsEqualTo(1);

            table.Append(DynValue.NewNumber(40));
            await Assert.That(table.Length).IsEqualTo(3);
            await Assert.That(table.RawGet(2).Number).IsEqualTo(40);
            await Assert.That(table.RawGet(3).Number).IsEqualTo(30);
        }

        [global::TUnit.Core.Test]
        public async Task NextKeySkipsNilEntriesAndHandlesTermination()
        {
            Table table = new(new Script());
            table.Set("first", DynValue.Nil);
            table.Set("second", DynValue.NewNumber(2));
            table.Set(3, DynValue.NewNumber(3));

            TablePair? first = table.NextKey(DynValue.Nil);
            await Assert.That(first.HasValue).IsTrue();
            await Assert.That(first.Value.Key.String).IsEqualTo("second");

            TablePair? second = table.NextKey(first.Value.Key);
            await Assert.That(second.HasValue).IsTrue();
            await Assert.That(second.Value.Key.Number).IsEqualTo(3);

            TablePair? tail = table.NextKey(second.Value.Key);
            await Assert.That(tail).IsEqualTo(TablePair.Nil);

            TablePair? missing = table.NextKey(DynValue.NewString("missing"));
            await Assert.That(missing).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task NextKeyReturnsNullWhenKeyUnknown()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(1));

            TablePair? unknown = table.NextKey(DynValue.NewNumber(999));

            await Assert.That(unknown).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task NextKeyHandlesNonIntegralNumberKeys()
        {
            Script script = new();
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(2.5);
            table.Set(fractionalKey, DynValue.NewString("head"));
            table.Set("tail", DynValue.NewNumber(7));

            TablePair? next = table.NextKey(fractionalKey);

            await Assert.That(next.HasValue).IsTrue();
            await Assert.That(next.Value.Key.String).IsEqualTo("tail");
        }

        [global::TUnit.Core.Test]
        public async Task CollectDeadKeysRemovesNilEntries()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            await Assert.That(table.Length).IsEqualTo(2);

            table.Set(2, DynValue.Nil);
            table.CollectDeadKeys();

            await Assert.That(table.RawGet(2)).IsNull();
            await Assert.That(table.Length).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task AppendThrowsWhenValueOwnedByDifferentScript()
        {
            Script scriptA = new();
            Script scriptB = new();
            Table table = new(scriptA);
            DynValue foreignValue = DynValue.NewTable(scriptB);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Append(foreignValue)
            );

            await Assert.That(exception.Message).Contains("resources owned by different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task SetDynValueNaNThrows()
        {
            Table table = new(new Script());
            DynValue nanKey = DynValue.NewNumber(double.NaN);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(nanKey, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("table index is NaN");
        }

        [global::TUnit.Core.Test]
        public async Task SetKeysThrowsWhenPathMissing()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(new object[] { "missing", "child" }, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("did not point to anything");
        }

        [global::TUnit.Core.Test]
        public async Task SetKeysThrowsWhenIntermediateIsNotTable()
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(new object[] { "leaf", "child" }, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("did not point to a table");
        }

        [global::TUnit.Core.Test]
        public async Task SetObjectArrayThrowsWhenKeysArrayIsNull()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set((object[])null, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("table index is nil");
        }

        [global::TUnit.Core.Test]
        public async Task SetObjectArrayThrowsWhenKeysArrayEmpty()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(Array.Empty<object>(), DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("table index is nil");
        }

        [global::TUnit.Core.Test]
        public async Task SetObjectRoutesStringsNumbersAndValues()
        {
            Script script = new();
            Table table = new(script);

            table.Set((object)"name", DynValue.NewString("nova"));
            table.Set((object)4, DynValue.NewNumber(4));

            double fractionalKey = 4.5;
            table.Set((object)fractionalKey, DynValue.NewNumber(5));

            await Assert.That(table.RawGet("name").String).IsEqualTo("nova");
            await Assert.That(table.RawGet(4).Number).IsEqualTo(4);
            await Assert.That(table.RawGet((object)fractionalKey).Number).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        public async Task SetDynValueNumberWithFractionStoresEntry()
        {
            Script script = new();
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(3.25);

            table.Set(fractionalKey, DynValue.NewString("fractional"));

            DynValue result = table.RawGet(fractionalKey);
            await Assert.That(result.String).IsEqualTo("fractional");
        }

        [global::TUnit.Core.Test]
        public async Task SetObjectArrayCreatesNestedTable()
        {
            Script script = new();
            Table table = new(script);
            table.Set("child", DynValue.NewTable(script));

            table.Set(new object[] { "child", "leaf" }, DynValue.NewString("value"));

            await Assert.That(table.RawGet("child", "leaf").String).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task GetParamsReturnsNestedValue()
        {
            Script script = new();
            Table table = new(script);
            DynValue child = DynValue.NewTable(script);
            child.Table.Set(1, DynValue.NewString("inner"));
            table.Set("child", child);

            DynValue value = table.Get("child", 1);

            await Assert.That(value.String).IsEqualTo("inner");
        }

        [global::TUnit.Core.Test]
        public async Task GetObjectReturnsNilWhenKeyMissing()
        {
            Table table = new(new Script());

            DynValue value = table.Get((object)"missing");

            await Assert.That(value.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetParamsReturnsNilWhenNoKeysProvided()
        {
            Table table = new(new Script());

            DynValue value = table.Get(Array.Empty<object>());

            await Assert.That(value.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RawGetParamsReturnsNullWhenKeysMissing()
        {
            Table table = new(new Script());

            DynValue value = table.RawGet(Array.Empty<object>());

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RawGetParamsThrowsWhenPathMissing()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet("missing", "child")
            );

            await Assert.That(exception.Message).Contains("did not point to anything");
        }

        [global::TUnit.Core.Test]
        public async Task RawGetParamsThrowsWhenIntermediateIsNotTable()
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet("leaf", "child")
            );

            await Assert.That(exception.Message).Contains("did not point to a table");
        }

        [global::TUnit.Core.Test]
        public async Task RemoveParamsReturnsFalseWhenNoKeys()
        {
            Table table = new(new Script());

            await Assert.That(table.Remove(Array.Empty<object>())).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task RawGetObjectReturnsNullWhenKeyIsNull()
        {
            Table table = new(new Script());

            await Assert.That(table.RawGet((object)null)).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RawGetObjectReturnsValueWhenKeyExists()
        {
            Table table = new(new Script());
            table.Set("existing", DynValue.NewNumber(42));

            DynValue value = table.RawGet((object)"existing");

            await Assert.That(value.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task RawGetParamsReturnsNullWhenArrayIsNull()
        {
            Table table = new(new Script());

            DynValue value = table.RawGet((object[])null);

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task SetObjectThrowsWhenKeyIsNull()
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set((object)null, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("table index is nil");
        }

        [global::TUnit.Core.Test]
        public async Task RemoveStringKeyDeletesEntry()
        {
            Table table = new(new Script());
            table.Set("key", DynValue.NewNumber(12));

            bool removed = table.Remove("key");

            await Assert.That(removed).IsTrue();
            await Assert.That(table.RawGet("key")).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveDynValueNumberUsesIntegralPath()
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(42));

            bool removed = table.Remove(DynValue.NewNumber(1));

            await Assert.That(removed).IsTrue();
            await Assert.That(table.RawGet(1)).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveDynValueStringDeletesEntry()
        {
            Table table = new(new Script());
            table.Set("value", DynValue.NewNumber(100));

            bool removed = table.Remove(DynValue.NewString("value"));

            await Assert.That(removed).IsTrue();
            await Assert.That(table.RawGet("value")).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveObjectTreatsBoxedStringAsStringKey()
        {
            Table table = new(new Script());
            table.Set("boxed", DynValue.NewNumber(5));

            bool removed = table.Remove((object)"boxed");

            await Assert.That(removed).IsTrue();
            await Assert.That(table.RawGet("boxed")).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveObjectTreatsBoxedIntAsArrayKey()
        {
            Table table = new(new Script());
            table.Set(3, DynValue.NewNumber(9));

            bool removed = table.Remove((object)3);

            await Assert.That(removed).IsTrue();
            await Assert.That(table.RawGet(3)).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveObjectDynValueKeyRemovesNonIntegralEntry()
        {
            Script script = new();
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(8.5);
            table.Set(fractionalKey, DynValue.NewString("value"));

            bool removed = table.Remove((object)fractionalKey);

            await Assert.That(removed).IsTrue();
            await Assert.That(table.RawGet(fractionalKey)).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveObjectArrayDeletesNestedEntry()
        {
            Script script = new();
            Table table = new(script);
            table.Set("branch", DynValue.NewTable(script));
            table.Set(new object[] { "branch", "leaf" }, DynValue.NewNumber(6));

            bool removed = table.Remove(new object[] { "branch", "leaf" });

            DynValue value = table.Get("branch", "leaf") ?? DynValue.Nil;

            await Assert.That(removed).IsTrue();
            await Assert.That(value.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RawGetParamsNavigatesNestedTables()
        {
            Script script = new();
            Table table = new(script);
            Table inner = new(script);
            inner.Set("leaf", DynValue.NewNumber(99));
            table.Set("child", DynValue.NewTable(script));
            table.Set(new object[] { "child", "leaf" }, DynValue.NewNumber(7));

            DynValue result = table.RawGet("child", "leaf");

            await Assert.That(result.Number).IsEqualTo(7);
        }

        [global::TUnit.Core.Test]
        public async Task KeysEnumeratesInsertedEntries()
        {
            Table table = new(new Script());
            table.Set("alpha", DynValue.NewNumber(1));
            table.Set(2, DynValue.NewNumber(2));

            object[] keys = table.Keys.Select(key => key.ToObject()).ToArray();

            await Assert.That(keys.Length).IsEqualTo(2);
            await Assert.That(keys.Contains("alpha")).IsTrue();
            await Assert.That(keys.Contains(2d)).IsTrue();
        }
    }
}
#pragma warning restore CA2007
