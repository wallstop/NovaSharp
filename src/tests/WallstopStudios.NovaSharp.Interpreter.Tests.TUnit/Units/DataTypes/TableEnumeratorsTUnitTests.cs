namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Tests for <see cref="TablePairsEnumerator"/>, <see cref="TableKeysEnumerator"/>,
    /// <see cref="TableValuesEnumerator"/>, and <see cref="TableNonNilPairsEnumerator"/>.
    /// </summary>
    public sealed class TableEnumeratorsTUnitTests
    {
        #region TablePairsEnumerator Tests

        [Test]
        public async Task TablePairsEnumeratorIteratesAllPairs()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", DynValue.NewNumber(1));
            table.Set("b", DynValue.NewNumber(2));
            table.Set("c", DynValue.NewNumber(3));

            List<TablePair> pairs = new();
            foreach (TablePair pair in table.GetPairsEnumerator())
            {
                pairs.Add(pair);
            }

            await Assert.That(pairs.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task TablePairsEnumeratorHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            int count = 0;
            foreach (TablePair _ in table.GetPairsEnumerator())
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TablePairsEnumeratorResetWorks()
        {
            Script script = new();
            Table table = new(script);
            table.Set("x", DynValue.NewNumber(10));
            table.Set("y", DynValue.NewNumber(20));

            TablePairsEnumerator enumerator = table.GetPairsEnumerator();

            // First pass
            int count1 = 0;
            while (enumerator.MoveNext())
            {
                count1++;
            }

            // Reset and second pass
            enumerator.Reset();
            int count2 = 0;
            while (enumerator.MoveNext())
            {
                count2++;
            }

            await Assert.That(count1).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(count2).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task TablePairsEnumeratorCurrentReturnsCorrectPair()
        {
            Script script = new();
            Table table = new(script);
            table.Set("key", DynValue.NewNumber(42));

            TablePairsEnumerator enumerator = table.GetPairsEnumerator();
            enumerator.MoveNext();

            await Assert.That(enumerator.Current.Key.String).IsEqualTo("key").ConfigureAwait(false);
            await Assert.That(enumerator.Current.Value.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task TablePairsEnumeratorGetEnumeratorReturnsSelf()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", DynValue.NewNumber(1));

            TablePairsEnumerator enumerator = table.GetPairsEnumerator();
            TablePairsEnumerator fromMethod = enumerator.GetEnumerator();

            // Should be the same struct (allows foreach pattern)
            enumerator.MoveNext();
            fromMethod.MoveNext();
            await Assert
                .That(enumerator.Current.Key.String)
                .IsEqualTo(fromMethod.Current.Key.String)
                .ConfigureAwait(false);
        }

        #endregion

        #region TableKeysEnumerator Tests

        [Test]
        public async Task TableKeysEnumeratorIteratesAllKeys()
        {
            Script script = new();
            Table table = new(script);
            table.Set("one", DynValue.NewNumber(1));
            table.Set("two", DynValue.NewNumber(2));

            List<string> keys = new();
            foreach (DynValue key in table.GetKeysEnumerator())
            {
                keys.Add(key.String);
            }

            await Assert.That(keys.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(keys).Contains("one").ConfigureAwait(false);
            await Assert.That(keys).Contains("two").ConfigureAwait(false);
        }

        [Test]
        public async Task TableKeysEnumeratorHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            int count = 0;
            foreach (DynValue _ in table.GetKeysEnumerator())
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TableKeysEnumeratorResetWorks()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", DynValue.NewNumber(1));

            TableKeysEnumerator enumerator = table.GetKeysEnumerator();
            enumerator.MoveNext();
            enumerator.Reset();
            enumerator.MoveNext();

            await Assert.That(enumerator.Current.String).IsEqualTo("a").ConfigureAwait(false);
        }

        #endregion

        #region TableValuesEnumerator Tests

        [Test]
        public async Task TableValuesEnumeratorIteratesAllValues()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", DynValue.NewNumber(10));
            table.Set("b", DynValue.NewNumber(20));
            table.Set("c", DynValue.NewNumber(30));

            List<double> values = new();
            foreach (DynValue value in table.GetValuesEnumerator())
            {
                values.Add(value.Number);
            }

            await Assert.That(values.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(values).Contains(10.0).ConfigureAwait(false);
            await Assert.That(values).Contains(20.0).ConfigureAwait(false);
            await Assert.That(values).Contains(30.0).ConfigureAwait(false);
        }

        [Test]
        public async Task TableValuesEnumeratorHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            int count = 0;
            foreach (DynValue _ in table.GetValuesEnumerator())
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TableValuesEnumeratorResetWorks()
        {
            Script script = new();
            Table table = new(script);
            table.Set("x", DynValue.NewNumber(99));

            TableValuesEnumerator enumerator = table.GetValuesEnumerator();
            enumerator.MoveNext();
            enumerator.Reset();
            enumerator.MoveNext();

            await Assert.That(enumerator.Current.Number).IsEqualTo(99).ConfigureAwait(false);
        }

        #endregion

        #region TableNonNilPairsEnumerator Tests

        [Test]
        public async Task TableNonNilPairsEnumeratorSkipsNilValues()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", DynValue.NewNumber(1));
            table.Set("b", DynValue.Nil);
            table.Set("c", DynValue.NewNumber(3));

            List<TablePair> pairs = new();
            foreach (TablePair pair in table.GetNonNilPairsEnumerator())
            {
                pairs.Add(pair);
            }

            await Assert.That(pairs.Count).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task TableNonNilPairsEnumeratorHandlesAllNilTable()
        {
            Script script = new();
            Table table = new(script);
            table.Set("a", DynValue.Nil);
            table.Set("b", DynValue.Nil);

            int count = 0;
            foreach (TablePair _ in table.GetNonNilPairsEnumerator())
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TableNonNilPairsEnumeratorHandlesEmptyTable()
        {
            Script script = new();
            Table table = new(script);

            int count = 0;
            foreach (TablePair _ in table.GetNonNilPairsEnumerator())
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TableNonNilPairsEnumeratorResetWorks()
        {
            Script script = new();
            Table table = new(script);
            table.Set("x", DynValue.NewNumber(5));

            TableNonNilPairsEnumerator enumerator = table.GetNonNilPairsEnumerator();

            int count1 = 0;
            while (enumerator.MoveNext())
            {
                count1++;
            }

            enumerator.Reset();

            int count2 = 0;
            while (enumerator.MoveNext())
            {
                count2++;
            }

            await Assert.That(count1).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(count2).IsEqualTo(1).ConfigureAwait(false);
        }

        #endregion
    }
}
