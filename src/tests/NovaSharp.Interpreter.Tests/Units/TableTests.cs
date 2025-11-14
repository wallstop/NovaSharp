namespace NovaSharp.Interpreter.Tests.Units
{
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
            Assert.That(table.Length, Is.EqualTo(1), "Setting nil should invalidate the cached length.");

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

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () => table.Append(foreignValue)
            );

            Assert.That(exception.Message, Does.Contain("resources owned by different scripts"));
        }
    }
}
