namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Tests for <see cref="Table"/> buffer-populating methods (FillPairs, FillKeys, FillValues).
    /// </summary>
    public sealed class TableBufferMethodsTUnitTests
    {
        #region FillPairs(Span<TablePair>) Tests

        [Test]
        public async Task FillPairsSpanFillsCorrectly()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));
            table.Set("c", LuaValue.NewNumber(3));

            TablePair[] buffer = new TablePair[10];
            int count = table.FillPairs(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsSpanHandlesSmallBuffer()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));
            table.Set("c", LuaValue.NewNumber(3));

            TablePair[] buffer = new TablePair[2];
            int count = table.FillPairs(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsSpanHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            TablePair[] buffer = new TablePair[5];
            int count = table.FillPairs(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsSpanHandlesEmptyBuffer()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));

            int count = table.FillPairs(Span<TablePair>.Empty);

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        #endregion

        #region FillKeys(Span<LuaValue>) Tests

        [Test]
        public async Task FillKeysSpanFillsCorrectly()
        {
            Script script = new();
            Table table = new(script);
            table.Set("x", LuaValue.NewNumber(10));
            table.Set("y", LuaValue.NewNumber(20));

            LuaValue[] buffer = new LuaValue[5];
            int count = table.FillKeys(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(buffer[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(buffer[1].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
        }

        [Test]
        public async Task FillKeysSpanHandlesSmallBuffer()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));
            table.Set("c", LuaValue.NewNumber(3));

            LuaValue[] buffer = new LuaValue[1];
            int count = table.FillKeys(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task FillKeysSpanHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            LuaValue[] buffer = new LuaValue[3];
            int count = table.FillKeys(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        #endregion

        #region FillValues(Span<LuaValue>) Tests

        [Test]
        public async Task FillValuesSpanFillsCorrectly()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(100));
            table.Set("b", LuaValue.NewNumber(200));

            LuaValue[] buffer = new LuaValue[5];
            int count = table.FillValues(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(buffer[0].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(buffer[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
        }

        [Test]
        public async Task FillValuesSpanHandlesSmallBuffer()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));

            LuaValue[] buffer = new LuaValue[1];
            int count = table.FillValues(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task FillValuesSpanHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            LuaValue[] buffer = new LuaValue[3];
            int count = table.FillValues(buffer.AsSpan());

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        #endregion

        #region FillPairs(ICollection<TablePair>) Tests

        [Test]
        public async Task FillPairsCollectionFillsCorrectly()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));

            List<TablePair> list = new() { default }; // Pre-populate to verify Clear
            List<TablePair> result = table.FillPairs(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(list.Count).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsCollectionClearsFirst()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));

            List<TablePair> list = new()
            {
                new TablePair(LuaValue.NewString("old"), LuaValue.NewNumber(999)),
            };

            List<TablePair> result = table.FillPairs(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(list.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(list[0].Key.String).IsEqualTo("a").ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsCollectionThrowsOnNull()
        {
            Script script = new();
            Table table = new(script);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                table.FillPairs<List<TablePair>>(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("destination").ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsCollectionHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            List<TablePair> list = new();
            List<TablePair> result = table.FillPairs(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(list.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task FillPairsCollectionSupportsFluentChaining()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));

            // Fluent API allows direct iteration
            int count = 0;
            foreach (TablePair pair in table.FillPairs(new List<TablePair>()))
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(2).ConfigureAwait(false);
        }

        #endregion

        #region FillKeys(ICollection<LuaValue>) Tests

        [Test]
        public async Task FillKeysCollectionFillsCorrectly()
        {
            Script script = new();
            Table table = new(script);
            table.Set("key1", LuaValue.NewNumber(1));
            table.Set("key2", LuaValue.NewNumber(2));

            List<LuaValue> list = new();
            List<LuaValue> result = table.FillKeys(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task FillKeysCollectionClearsFirst()
        {
            Script script = new();
            Table table = new(script);
            table.Set("new", LuaValue.NewNumber(1));

            List<LuaValue> list = new() { LuaValue.NewString("old") };
            List<LuaValue> result = table.FillKeys(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(list.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(list[0].String).IsEqualTo("new").ConfigureAwait(false);
        }

        [Test]
        public async Task FillKeysCollectionThrowsOnNull()
        {
            Script script = new();
            Table table = new(script);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                table.FillKeys<List<LuaValue>>(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("destination").ConfigureAwait(false);
        }

        [Test]
        public async Task FillKeysCollectionSupportsFluentChaining()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));

            // Fluent API allows direct iteration
            int count = 0;
            foreach (LuaValue key in table.FillKeys(new List<LuaValue>()))
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(2).ConfigureAwait(false);
        }

        #endregion

        #region FillValues(ICollection<LuaValue>) Tests

        [Test]
        public async Task FillValuesCollectionFillsCorrectly()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(50));
            table.Set("b", LuaValue.NewNumber(60));

            List<LuaValue> list = new();
            List<LuaValue> result = table.FillValues(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task FillValuesCollectionClearsFirst()
        {
            Script script = new();
            Table table = new(script);
            table.Set("x", LuaValue.NewNumber(42));

            List<LuaValue> list = new() { LuaValue.NewNumber(999) };
            List<LuaValue> result = table.FillValues(list);

            await Assert.That(result).IsSameReferenceAs(list).ConfigureAwait(false);
            await Assert.That(list.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(list[0].Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task FillValuesCollectionThrowsOnNull()
        {
            Script script = new();
            Table table = new(script);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                table.FillValues<List<LuaValue>>(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("destination").ConfigureAwait(false);
        }

        [Test]
        public async Task FillValuesCollectionSupportsFluentChaining()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(50));
            table.Set("b", LuaValue.NewNumber(60));

            // Fluent API allows direct iteration
            int count = 0;
            foreach (LuaValue value in table.FillValues(new List<LuaValue>()))
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(2).ConfigureAwait(false);
        }

        #endregion

        #region Count Property Tests

        [Test]
        public async Task CountReturnsCorrectValue()
        {
            Script script = new();
            Table table = new(script);

            await Assert.That(table.Count).IsEqualTo(0).ConfigureAwait(false);

            table.Set("a", LuaValue.NewNumber(1));
            await Assert.That(table.Count).IsEqualTo(1).ConfigureAwait(false);

            table.Set("b", LuaValue.NewNumber(2));
            await Assert.That(table.Count).IsEqualTo(2).ConfigureAwait(false);

            table.Set("c", LuaValue.NewNumber(3));
            await Assert.That(table.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CountIncludesNilEntries()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", LuaValue.NewNumber(1));
            table.Set("b", LuaValue.NewNumber(2));
            table.Set("c", LuaValue.NewNumber(3));

            await Assert.That(table.Count).IsEqualTo(3).ConfigureAwait(false);

            // Setting to nil keeps the entry in Count (use GetNonNilPairsEnumerator to skip nils)
            table.Set("b", LuaValue.Nil);

            // Count still includes the nil entry in the internal structure
            await Assert.That(table.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        #endregion
    }
}
