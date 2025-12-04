namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataStructs;
    using CollectionAssert = NUnit.Framework.CollectionAssert;

    public sealed class SliceTUnitTests
    {
        private static readonly int[] ToArrayForwardExpected = { 6, 7 };
        private static readonly int[] ToArrayReversedExpected = { 7, 6 };
        private static readonly int[] EnumeratorForwardExpected = { 5, 6, 7 };
        private static readonly int[] EnumeratorReversedExpected = { 7, 6, 5 };
        private static readonly int[] CopyToTargetExpected = { 0, 1, 2, 0 };
        private static readonly int[] NonGenericEnumeratorExpected = { 1, 2 };

        [global::TUnit.Core.Test]
        public async Task IndexerReturnsAndSetsUnderlyingItems()
        {
            List<int> source = new() { 1, 2, 3, 4, 5 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: false);

            await Assert.That(slice[0]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(slice[1]).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(slice[2]).IsEqualTo(4).ConfigureAwait(false);

            slice[1] = 99;

            await Assert.That(source[2]).IsEqualTo(99).ConfigureAwait(false);
            await Assert.That(slice[1]).IsEqualTo(99).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexerRespectsReversedView()
        {
            List<string> source = new() { "a", "b", "c", "d" };
            Slice<string> slice = new(source, from: 1, length: 2, reversed: true);

            await Assert.That(slice[0]).IsEqualTo("c").ConfigureAwait(false);
            await Assert.That(slice[1]).IsEqualTo("b").ConfigureAwait(false);

            slice[0] = "X";

            await Assert.That(source[2]).IsEqualTo("X").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CountFromAndReversedReturnExpectedValues()
        {
            List<int> source = Enumerable.Range(0, 10).ToList();
            Slice<int> slice = new(source, from: 3, length: 4, reversed: false);

            await Assert.That(slice.From).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(slice.Count).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(slice.Reversed).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToArrayCopiesSliceRange()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 1, length: 2, reversed: false);

            CollectionAssert.AreEqual(ToArrayForwardExpected, slice.ToArray());
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToListCopiesSliceRange()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 1, length: 2, reversed: true);

            CollectionAssert.AreEqual(ToArrayReversedExpected, slice.ToList().ToArray());
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EnumeratorYieldsItemsInOrder()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            CollectionAssert.AreEqual(EnumeratorForwardExpected, slice.ToArray());
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EnumeratorYieldsItemsInReverseWhenReversed()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: true);

            CollectionAssert.AreEqual(EnumeratorReversedExpected, slice.ToArray());
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void IndexerThrowsOnOutOfRange()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = slice[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = slice[3]);
        }

        [global::TUnit.Core.Test]
        public async Task ToListReturnsIndependentCopy()
        {
            List<int> source = new() { 10, 20, 30 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            List<int> result = slice.ToList();
            result[0] = 99;

            await Assert.That(result[0]).IsEqualTo(99).ConfigureAwait(false);
            await Assert.That(source[0]).IsEqualTo(10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CopyToCopiesIntoTargetArray()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);

            int[] target = { 0, 0, 0, 0 };
            slice.CopyTo(target, 1);

            CollectionAssert.AreEqual(CopyToTargetExpected, target);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void MutatingMethodsThrowInvalidOperation()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            Assert.Throws<InvalidOperationException>(() => slice.Add(4));
            Assert.Throws<InvalidOperationException>(() => slice.Remove(2));
            Assert.Throws<InvalidOperationException>(() => slice.Insert(0, 9));
            Assert.Throws<InvalidOperationException>(() => slice.Clear());
            Assert.Throws<InvalidOperationException>(() => slice.RemoveAt(0));
        }

        [global::TUnit.Core.Test]
        public async Task NonGenericEnumeratorReturnsItems()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);
            IEnumerable enumerable = slice;

            CollectionAssert.AreEqual(
                NonGenericEnumeratorExpected,
                enumerable.Cast<int>().ToArray()
            );
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
