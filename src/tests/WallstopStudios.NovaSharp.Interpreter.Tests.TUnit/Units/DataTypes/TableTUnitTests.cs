namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class TableTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ConstructorWithArrayValuesInitializesSequentialEntries(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue[] values = new[] { DynValue.NewNumber(10), DynValue.NewString("two") };

            Table table = new(script, values);

            await Assert.That(table.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(table.RawGet(1).Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(table.RawGet(2).String).IsEqualTo("two").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClearRemovesAllEntries(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(1));
            table.Set("name", DynValue.NewString("value"));

            table.Clear();

            await Assert.That(table.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(table.RawGet(1)).IsNull().ConfigureAwait(false);
            await Assert.That(table.RawGet("name")).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LengthCacheInvalidatesWhenEntriesChange(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            table.Set(3, DynValue.NewNumber(30));

            await Assert.That(table.Length).IsEqualTo(3).ConfigureAwait(false);

            table.Set(2, DynValue.Nil);
            await Assert.That(table.Length).IsEqualTo(1).ConfigureAwait(false);

            table.Append(DynValue.NewNumber(40));
            await Assert.That(table.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(table.RawGet(2).Number).IsEqualTo(40).ConfigureAwait(false);
            await Assert.That(table.RawGet(3).Number).IsEqualTo(30).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NextKeySkipsNilEntriesAndHandlesTermination(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script());
            table.Set("first", DynValue.Nil);
            table.Set("second", DynValue.NewNumber(2));
            table.Set(3, DynValue.NewNumber(3));

            TablePair? first = table.NextKey(DynValue.Nil);
            await Assert.That(first.HasValue).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Value.Key.String).IsEqualTo("second").ConfigureAwait(false);

            TablePair? second = table.NextKey(first.Value.Key);
            await Assert.That(second.HasValue).IsTrue().ConfigureAwait(false);
            await Assert.That(second.Value.Key.Number).IsEqualTo(3).ConfigureAwait(false);

            TablePair? tail = table.NextKey(second.Value.Key);
            await Assert.That(tail).IsEqualTo(TablePair.Nil).ConfigureAwait(false);

            TablePair? missing = table.NextKey(DynValue.NewString("missing"));
            await Assert.That(missing).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NextKeyReturnsNullWhenKeyUnknown(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(1));

            TablePair? unknown = table.NextKey(DynValue.NewNumber(999));

            await Assert.That(unknown).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NextKeyHandlesNonIntegralNumberKeys(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(2.5);
            table.Set(fractionalKey, DynValue.NewString("head"));
            table.Set("tail", DynValue.NewNumber(7));

            TablePair? next = table.NextKey(fractionalKey);

            await Assert.That(next.HasValue).IsTrue().ConfigureAwait(false);
            await Assert.That(next.Value.Key.String).IsEqualTo("tail").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CollectDeadKeysRemovesNilEntries(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            await Assert.That(table.Length).IsEqualTo(2).ConfigureAwait(false);

            table.Set(2, DynValue.Nil);
            table.CollectDeadKeys();

            await Assert.That(table.RawGet(2)).IsNull().ConfigureAwait(false);
            await Assert.That(table.Length).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AppendThrowsWhenValueOwnedByDifferentScript(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new();
            Script scriptB = new();
            Table table = new(scriptA);
            DynValue foreignValue = DynValue.NewTable(scriptB);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Append(foreignValue)
            );

            await Assert
                .That(exception.Message)
                .Contains("resources owned by different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetDynValueNaNThrows(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            DynValue nanKey = DynValue.NewNumber(double.NaN);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(nanKey, DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("table index is NaN")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetKeysThrowsWhenPathMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(new object[] { "missing", "child" }, DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to anything")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetKeysThrowsWhenIntermediateIsNotTable(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(new object[] { "leaf", "child" }, DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to a table")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("missing", "did not point to anything", false)]
        [global::TUnit.Core.Arguments("leaf", "did not point to a table", true)]
        public async Task NestedPathErrorsIncludeOffendingKey(
            string key,
            string expectedMessage,
            bool seedNonTable
        )
        {
            Table table = new(new Script());
            if (seedNonTable)
            {
                table.Set(key, DynValue.NewNumber(5));
            }

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet(key, "child")
            );

            await Assert.That(exception.Message).Contains(expectedMessage).ConfigureAwait(false);
            await Assert.That(exception.Message).Contains(key).ConfigureAwait(false);
            await Assert.That(exception.Message).DoesNotContain("{0}").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetObjectArrayThrowsWhenKeysArrayIsNull(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set((object[])null, DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("table index is nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetObjectArrayThrowsWhenKeysArrayEmpty(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set(Array.Empty<object>(), DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("table index is nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetObjectRoutesStringsNumbersAndValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);

            table.Set((object)"name", DynValue.NewString("nova"));
            table.Set((object)4, DynValue.NewNumber(4));

            double fractionalKey = 4.5;
            table.Set((object)fractionalKey, DynValue.NewNumber(5));

            await Assert.That(table.RawGet("name").String).IsEqualTo("nova").ConfigureAwait(false);
            await Assert.That(table.RawGet(4).Number).IsEqualTo(4).ConfigureAwait(false);
            await Assert
                .That(table.RawGet((object)fractionalKey).Number)
                .IsEqualTo(5)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetDynValueNumberWithFractionStoresEntry(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(3.25);

            table.Set(fractionalKey, DynValue.NewString("fractional"));

            DynValue result = table.RawGet(fractionalKey);
            await Assert.That(result.String).IsEqualTo("fractional").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetObjectArrayCreatesNestedTable(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            table.Set("child", DynValue.NewTable(script));

            table.Set(new object[] { "child", "leaf" }, DynValue.NewString("value"));

            await Assert
                .That(table.RawGet("child", "leaf").String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedSetOverloadsSetTwoAndThreeKeyPaths(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Table table = new(script);
            Table child = new(script);
            Table grandchild = new(script);

            table.Set("child", DynValue.NewTable(child));
            child.Set("grandchild", DynValue.NewTable(grandchild));

            table.Set("child", "leaf", DynValue.NewString("two"));
            table.Set("child", "grandchild", "leaf", DynValue.NewString("three"));

            await Assert.That(child.RawGet("leaf").String).IsEqualTo("two").ConfigureAwait(false);
            await Assert
                .That(grandchild.RawGet("leaf").String)
                .IsEqualTo("three")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedSetRejectsForeignValue(LuaCompatibilityVersion version)
        {
            Script scriptA = new();
            Script scriptB = new();
            Table table = new(scriptA);
            table.Set("child", DynValue.NewTable(scriptA));
            DynValue foreignValue = DynValue.NewTable(scriptB);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set("child", "leaf", foreignValue)
            );

            await Assert
                .That(exception.Message)
                .Contains("resources owned by different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetParamsReturnsNestedValue(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            DynValue child = DynValue.NewTable(script);
            child.Table.Set(1, DynValue.NewString("inner"));
            table.Set("child", child);

            DynValue value = table.Get("child", 1);

            await Assert.That(value.String).IsEqualTo("inner").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedGetAndRawGetOverloadsReturnNestedValues(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Table table = new(script);
            Table child = new(script);
            Table grandchild = new(script);
            table.Set("child", DynValue.NewTable(child));
            child.Set("grandchild", DynValue.NewTable(grandchild));
            child.Set("leaf", DynValue.NewString("two"));
            grandchild.Set("leaf", DynValue.NewString("three"));

            DynValue twoKeyGet = table.Get("child", "leaf");
            DynValue twoKeyRawGet = table.RawGet("child", "leaf");
            DynValue threeKeyGet = table.Get("child", "grandchild", "leaf");
            DynValue threeKeyRawGet = table.RawGet("child", "grandchild", "leaf");

            await Assert.That(twoKeyGet.String).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(twoKeyRawGet.String).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(threeKeyGet.String).IsEqualTo("three").ConfigureAwait(false);
            await Assert.That(threeKeyRawGet.String).IsEqualTo("three").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedIndexersGetAndSetNestedValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            Table child = new(script);
            Table grandchild = new(script);
            table.Set("child", DynValue.NewTable(child));
            child.Set("grandchild", DynValue.NewTable(grandchild));

            table["child", "leaf"] = "two";
            table["child", "grandchild", "leaf"] = "three";

            await Assert.That(table["child", "leaf"]).IsEqualTo("two").ConfigureAwait(false);
            await Assert
                .That(table["child", "grandchild", "leaf"])
                .IsEqualTo("three")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedTerminalNullMatchesArrayPath(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            table.Set("child", DynValue.NewTable(script));

            DynValue fixedGet = table.Get("child", (object)null);
            DynValue fixedRawGet = table.RawGet("child", (object)null);
            bool removed = table.Remove("child", (object)null);
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set("child", (object)null, DynValue.NewNumber(1))
            );

            await Assert.That(fixedGet.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(fixedRawGet).IsNull().ConfigureAwait(false);
            await Assert.That(removed).IsFalse().ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("table index is nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ObjectArrayNestedGetStillUsesArrayPath(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            table.Set("child", DynValue.NewTable(script));
            table.Set(new object[] { "child", "leaf" }, DynValue.NewString("array"));
            object[] keys = new object[] { "child", "leaf" };

            DynValue getValue = table.Get(keys);
            DynValue rawValue = table.RawGet(keys);
            DynValue castGetValue = table.Get((object)keys);
            DynValue castRawValue = table.RawGet((object)keys);

            await Assert.That(getValue.String).IsEqualTo("array").ConfigureAwait(false);
            await Assert.That(rawValue.String).IsEqualTo("array").ConfigureAwait(false);
            await Assert.That(castGetValue.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(castRawValue).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SpanNestedKeyPathsAccessSlicesAndMutateNestedValues(
            LuaCompatibilityVersion version
        )
        {
            (
                string RootGet,
                bool RootRemoved,
                bool RootMissingAfterRemove,
                string TwoKeyGet,
                string TwoKeySliceRawGet,
                string ThreeKeySliceRawGet,
                string UpdatedLeaf,
                bool RemovedTwoKey,
                bool TwoKeyMissingAfterRemove,
                bool RemovedThreeKey,
                bool ThreeKeyMissingAfterRemove
            ) result = ExerciseSpanNestedKeyPaths(version);

            await Assert.That(result.RootGet).IsEqualTo("rooted").ConfigureAwait(false);
            await Assert.That(result.RootRemoved).IsTrue().ConfigureAwait(false);
            await Assert.That(result.RootMissingAfterRemove).IsTrue().ConfigureAwait(false);
            await Assert.That(result.TwoKeyGet).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(result.TwoKeySliceRawGet).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(result.ThreeKeySliceRawGet).IsEqualTo("three").ConfigureAwait(false);
            await Assert.That(result.UpdatedLeaf).IsEqualTo("updated").ConfigureAwait(false);
            await Assert.That(result.RemovedTwoKey).IsTrue().ConfigureAwait(false);
            await Assert.That(result.TwoKeyMissingAfterRemove).IsTrue().ConfigureAwait(false);
            await Assert.That(result.RemovedThreeKey).IsTrue().ConfigureAwait(false);
            await Assert.That(result.ThreeKeyMissingAfterRemove).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SpanNestedKeyPathsTreatEmptyAndDefaultLikeArrayPath(
            LuaCompatibilityVersion version
        )
        {
            (
                bool EmptyGetIsNil,
                bool DefaultGetIsNil,
                bool EmptyRawGetIsNull,
                bool DefaultRawGetIsNull,
                bool EmptyRemoveResult,
                bool DefaultRemoveResult,
                string EmptySetExceptionMessage,
                string DefaultSetExceptionMessage
            ) result = ExerciseEmptyAndDefaultSpanKeyPaths();

            await Assert.That(result.EmptyGetIsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(result.DefaultGetIsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(result.EmptyRawGetIsNull).IsTrue().ConfigureAwait(false);
            await Assert.That(result.DefaultRawGetIsNull).IsTrue().ConfigureAwait(false);
            await Assert.That(result.EmptyRemoveResult).IsFalse().ConfigureAwait(false);
            await Assert.That(result.DefaultRemoveResult).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.EmptySetExceptionMessage)
                .Contains("table index is nil")
                .ConfigureAwait(false);
            await Assert
                .That(result.DefaultSetExceptionMessage)
                .Contains("table index is nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SpanNestedTerminalNullMatchesArrayPath(LuaCompatibilityVersion version)
        {
            (
                bool GetIsNil,
                bool RawGetIsNull,
                bool RemoveResult,
                string SetExceptionMessage
            ) result = ExerciseTerminalNullSpanKeyPath(version);

            await Assert.That(result.GetIsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(result.RawGetIsNull).IsTrue().ConfigureAwait(false);
            await Assert.That(result.RemoveResult).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.SetExceptionMessage)
                .Contains("table index is nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("missing", "did not point to anything", false)]
        [global::TUnit.Core.Arguments("leaf", "did not point to a table", true)]
        public async Task SpanNestedPathErrorsIncludeOffendingKey(
            string key,
            string expectedMessage,
            bool seedNonTable
        )
        {
            (
                string GetMessage,
                string RawGetMessage,
                string SetMessage,
                string RemoveMessage
            ) messages = CaptureSpanPathErrorMessages(key, seedNonTable);

            await Assert.That(messages.GetMessage).Contains(expectedMessage).ConfigureAwait(false);
            await Assert
                .That(messages.RawGetMessage)
                .Contains(expectedMessage)
                .ConfigureAwait(false);
            await Assert.That(messages.SetMessage).Contains(expectedMessage).ConfigureAwait(false);
            await Assert
                .That(messages.RemoveMessage)
                .Contains(expectedMessage)
                .ConfigureAwait(false);
            await Assert.That(messages.GetMessage).Contains(key).ConfigureAwait(false);
            await Assert.That(messages.RawGetMessage).Contains(key).ConfigureAwait(false);
            await Assert.That(messages.SetMessage).Contains(key).ConfigureAwait(false);
            await Assert.That(messages.RemoveMessage).Contains(key).ConfigureAwait(false);
            await Assert.That(messages.GetMessage).DoesNotContain("{0}").ConfigureAwait(false);
            await Assert.That(messages.RawGetMessage).DoesNotContain("{0}").ConfigureAwait(false);
            await Assert.That(messages.SetMessage).DoesNotContain("{0}").ConfigureAwait(false);
            await Assert.That(messages.RemoveMessage).DoesNotContain("{0}").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SpanNestedSetRejectsForeignValue(LuaCompatibilityVersion version)
        {
            string message = CaptureSpanForeignValueExceptionMessage(version);

            await Assert
                .That(message)
                .Contains("resources owned by different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetObjectReturnsNilWhenKeyMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            DynValue value = table.Get((object)"missing");

            await Assert.That(value.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetParamsReturnsNilWhenNoKeysProvided(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            DynValue value = table.Get(Array.Empty<object>());

            await Assert.That(value.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetParamsReturnsNullWhenKeysMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            DynValue value = table.RawGet(Array.Empty<object>());

            await Assert.That(value).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetParamsThrowsWhenPathMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet("missing", "child")
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to anything")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetObjectArrayThrowsWhenPathMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet(new object[] { "missing", "child" })
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to anything")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedGetThrowsWhenPathMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Get("missing", "child")
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to anything")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedSetThrowsWhenPathMissing(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set("missing", "child", DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to anything")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetParamsThrowsWhenIntermediateIsNotTable(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet("leaf", "child")
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to a table")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedRawGetThrowsWhenIntermediateIsNotTable(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.RawGet("leaf", "child")
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to a table")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedRemoveThrowsWhenIntermediateIsNotTable(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script());
            table.Set("leaf", DynValue.NewNumber(5));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Remove("leaf", "child")
            );

            await Assert
                .That(exception.Message)
                .Contains("did not point to a table")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveParamsReturnsFalseWhenNoKeys(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            await Assert.That(table.Remove(Array.Empty<object>())).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetObjectReturnsNullWhenKeyIsNull(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            await Assert.That(table.RawGet((object)null)).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetObjectReturnsValueWhenKeyExists(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set("existing", DynValue.NewNumber(42));

            DynValue value = table.RawGet((object)"existing");

            await Assert.That(value.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetParamsReturnsNullWhenArrayIsNull(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            DynValue value = table.RawGet((object[])null);

            await Assert.That(value).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetObjectThrowsWhenKeyIsNull(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                table.Set((object)null, DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("table index is nil")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveStringKeyDeletesEntry(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set("key", DynValue.NewNumber(12));

            bool removed = table.Remove("key");

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet("key")).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveDynValueNumberUsesIntegralPath(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set(1, DynValue.NewNumber(42));

            bool removed = table.Remove(DynValue.NewNumber(1));

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet(1)).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveDynValueStringDeletesEntry(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set("value", DynValue.NewNumber(100));

            bool removed = table.Remove(DynValue.NewString("value"));

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet("value")).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveObjectTreatsBoxedStringAsStringKey(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set("boxed", DynValue.NewNumber(5));

            bool removed = table.Remove((object)"boxed");

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet("boxed")).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveObjectTreatsBoxedIntAsArrayKey(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set(3, DynValue.NewNumber(9));

            bool removed = table.Remove((object)3);

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet(3)).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveObjectDynValueKeyRemovesNonIntegralEntry(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Table table = new(script);
            DynValue fractionalKey = DynValue.NewNumber(8.5);
            table.Set(fractionalKey, DynValue.NewString("value"));

            bool removed = table.Remove((object)fractionalKey);

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet(fractionalKey)).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveObjectArrayDeletesNestedEntry(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            table.Set("branch", DynValue.NewTable(script));
            table.Set(new object[] { "branch", "leaf" }, DynValue.NewNumber(6));

            bool removed = table.Remove(new object[] { "branch", "leaf" });

            DynValue value = table.Get("branch", "leaf") ?? DynValue.Nil;

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(value.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedNestedRemoveOverloadsDeleteNestedEntries(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Table table = new(script);
            Table child = new(script);
            Table grandchild = new(script);
            table.Set("child", DynValue.NewTable(child));
            child.Set("grandchild", DynValue.NewTable(grandchild));
            child.Set("leaf", DynValue.NewNumber(2));
            grandchild.Set("leaf", DynValue.NewNumber(3));

            bool removedTwoKey = table.Remove("child", "leaf");
            bool removedThreeKey = table.Remove("child", "grandchild", "leaf");

            await Assert.That(removedTwoKey).IsTrue().ConfigureAwait(false);
            await Assert.That(removedThreeKey).IsTrue().ConfigureAwait(false);
            await Assert.That(child.RawGet("leaf")).IsNull().ConfigureAwait(false);
            await Assert.That(grandchild.RawGet("leaf")).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RawGetParamsNavigatesNestedTables(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            Table inner = new(script);
            inner.Set("leaf", DynValue.NewNumber(99));
            table.Set("child", DynValue.NewTable(script));
            table.Set(new object[] { "child", "leaf" }, DynValue.NewNumber(7));

            DynValue result = table.RawGet("child", "leaf");

            await Assert.That(result.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task KeysEnumeratesInsertedEntries(LuaCompatibilityVersion version)
        {
            Table table = new(new Script());
            table.Set("alpha", DynValue.NewNumber(1));
            table.Set(2, DynValue.NewNumber(2));

            object[] keys = table.Keys.Select(key => key.ToObject()).ToArray();

            await Assert.That(keys.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(keys.Contains("alpha")).IsTrue().ConfigureAwait(false);
            // Numeric key may come back as long (integer) or double depending on representation
            await Assert
                .That(keys.Any(k => k is long l && l == 2 || k is double d && d == 2d))
                .IsTrue()
                .ConfigureAwait(false);
        }

        private static (
            string RootGet,
            bool RootRemoved,
            bool RootMissingAfterRemove,
            string TwoKeyGet,
            string TwoKeySliceRawGet,
            string ThreeKeySliceRawGet,
            string UpdatedLeaf,
            bool RemovedTwoKey,
            bool TwoKeyMissingAfterRemove,
            bool RemovedThreeKey,
            bool ThreeKeyMissingAfterRemove
        ) ExerciseSpanNestedKeyPaths(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            Table child = new(script);
            Table grandchild = new(script);
            table.Set("child", DynValue.NewTable(child));
            child.Set("grandchild", DynValue.NewTable(grandchild));
            child.Set("leaf", DynValue.NewString("two"));
            grandchild.Set("leaf", DynValue.NewString("three"));

            object[] rootKey = new object[] { "root" };
            object[] twoKeys = new object[] { "child", "leaf" };
            object[] threeKeys = new object[] { "child", "grandchild", "leaf" };
            object[] paddedTwoKeys = new object[] { "ignored", "child", "leaf", "ignored" };
            object[] paddedThreeKeys = new object[]
            {
                "ignored",
                "child",
                "grandchild",
                "leaf",
                "ignored",
            };

            table.Set(rootKey.AsSpan(), DynValue.NewString("rooted"));
            string rootGet = table.Get(rootKey.AsSpan()).String;
            bool rootRemoved = table.Remove(rootKey.AsSpan());
            bool rootMissingAfterRemove = table.RawGet("root") == null;
            string twoKeyGet = table.Get(twoKeys.AsSpan()).String;
            string twoKeySliceRawGet = table.RawGet(paddedTwoKeys.AsSpan(1, 2)).String;
            string threeKeySliceRawGet = table.RawGet(paddedThreeKeys.AsSpan(1, 3)).String;

            table.Set(paddedTwoKeys.AsSpan(1, 2), DynValue.NewString("updated"));
            string updatedLeaf = child.RawGet("leaf").String;

            bool removedTwoKey = table.Remove(twoKeys.AsSpan());
            bool twoKeyMissingAfterRemove = child.RawGet("leaf") == null;

            table.Set(threeKeys.AsSpan(), DynValue.NewString("reset"));
            bool removedThreeKey = table.Remove(paddedThreeKeys.AsSpan(1, 3));
            bool threeKeyMissingAfterRemove = grandchild.RawGet("leaf") == null;

            return (
                rootGet,
                rootRemoved,
                rootMissingAfterRemove,
                twoKeyGet,
                twoKeySliceRawGet,
                threeKeySliceRawGet,
                updatedLeaf,
                removedTwoKey,
                twoKeyMissingAfterRemove,
                removedThreeKey,
                threeKeyMissingAfterRemove
            );
        }

        private static (
            bool EmptyGetIsNil,
            bool DefaultGetIsNil,
            bool EmptyRawGetIsNull,
            bool DefaultRawGetIsNull,
            bool EmptyRemoveResult,
            bool DefaultRemoveResult,
            string EmptySetExceptionMessage,
            string DefaultSetExceptionMessage
        ) ExerciseEmptyAndDefaultSpanKeyPaths()
        {
            Table table = new(new Script());
            ReadOnlySpan<object> defaultKeys = default;

            return (
                table.Get(ReadOnlySpan<object>.Empty).IsNil(),
                table.Get(defaultKeys).IsNil(),
                table.RawGet(ReadOnlySpan<object>.Empty) == null,
                table.RawGet(defaultKeys) == null,
                table.Remove(ReadOnlySpan<object>.Empty),
                table.Remove(defaultKeys),
                CaptureSpanSetExceptionMessage(table, ReadOnlySpan<object>.Empty),
                CaptureSpanSetExceptionMessage(table, defaultKeys)
            );
        }

        private static (
            bool GetIsNil,
            bool RawGetIsNull,
            bool RemoveResult,
            string SetExceptionMessage
        ) ExerciseTerminalNullSpanKeyPath(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            table.Set("child", DynValue.NewTable(script));
            object[] keys = new object[] { "child", null };

            return (
                table.Get(keys.AsSpan()).IsNil(),
                table.RawGet(keys.AsSpan()) == null,
                table.Remove(keys.AsSpan()),
                CaptureSpanSetExceptionMessage(table, keys.AsSpan())
            );
        }

        private static (
            string GetMessage,
            string RawGetMessage,
            string SetMessage,
            string RemoveMessage
        ) CaptureSpanPathErrorMessages(string key, bool seedNonTable)
        {
            Table table = new(new Script());
            if (seedNonTable)
            {
                table.Set(key, DynValue.NewNumber(5));
            }

            object[] keys = new object[] { key, "child" };

            return (
                CaptureSpanGetExceptionMessage(table, keys),
                CaptureSpanRawGetExceptionMessage(table, keys),
                CaptureSpanSetExceptionMessage(table, keys.AsSpan()),
                CaptureSpanRemoveExceptionMessage(table, keys)
            );
        }

        private static string CaptureSpanForeignValueExceptionMessage(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(version);
            Script scriptB = new(version);
            Table table = new(scriptA);
            table.Set("child", DynValue.NewTable(scriptA));
            DynValue foreignValue = DynValue.NewTable(scriptB);
            object[] keys = new object[] { "child", "leaf" };

            return CaptureSpanSetExceptionMessage(table, keys.AsSpan(), foreignValue);
        }

        private static string CaptureSpanGetExceptionMessage(Table table, object[] keys)
        {
            try
            {
                _ = table.Get(keys.AsSpan());
            }
            catch (ScriptRuntimeException exception)
            {
                return exception.Message;
            }

            return string.Empty;
        }

        private static string CaptureSpanRawGetExceptionMessage(Table table, object[] keys)
        {
            try
            {
                _ = table.RawGet(keys.AsSpan());
            }
            catch (ScriptRuntimeException exception)
            {
                return exception.Message;
            }

            return string.Empty;
        }

        private static string CaptureSpanSetExceptionMessage(
            Table table,
            ReadOnlySpan<object> keys
        ) => CaptureSpanSetExceptionMessage(table, keys, DynValue.NewNumber(1));

        private static string CaptureSpanSetExceptionMessage(
            Table table,
            ReadOnlySpan<object> keys,
            DynValue value
        )
        {
            try
            {
                table.Set(keys, value);
            }
            catch (ScriptRuntimeException exception)
            {
                return exception.Message;
            }

            return string.Empty;
        }

        private static string CaptureSpanRemoveExceptionMessage(Table table, object[] keys)
        {
            try
            {
                _ = table.Remove(keys.AsSpan());
            }
            catch (ScriptRuntimeException exception)
            {
                return exception.Message;
            }

            return string.Empty;
        }
    }
}
