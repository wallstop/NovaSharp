namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SliceTests
    {
        private static readonly int[] ToArrayForwardExpected = { 6, 7 };
        private static readonly int[] ToArrayReversedExpected = { 7, 6 };
        private static readonly int[] EnumeratorForwardExpected = { 5, 6, 7 };
        private static readonly int[] EnumeratorReversedExpected = { 7, 6, 5 };
        private static readonly int[] CopyToTargetExpected = { 0, 1, 2, 0 };
        private static readonly int[] NonGenericEnumeratorExpected = { 1, 2 };

        [Test]
        public void IndexerReturnsAndSetsUnderlyingItems()
        {
            List<int> source = new() { 1, 2, 3, 4, 5 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(slice[0], Is.EqualTo(2));
                Assert.That(slice[1], Is.EqualTo(3));
                Assert.That(slice[2], Is.EqualTo(4));
            });

            slice[1] = 99;
            Assert.Multiple(() =>
            {
                Assert.That(source[2], Is.EqualTo(99));
                Assert.That(slice[1], Is.EqualTo(99));
            });
        }

        [Test]
        public void IndexerRespectsReversedView()
        {
            List<string> source = new() { "a", "b", "c", "d" };
            Slice<string> slice = new(source, from: 1, length: 2, reversed: true);

            Assert.That(slice[0], Is.EqualTo("c"));
            Assert.That(slice[1], Is.EqualTo("b"));

            slice[0] = "X";
            Assert.That(source[2], Is.EqualTo("X"));
        }

        [Test]
        public void CountFromAndReversedReturnExpectedValues()
        {
            List<int> source = Enumerable.Range(0, 10).ToList();
            Slice<int> slice = new(source, from: 3, length: 4, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(slice.From, Is.EqualTo(3));
                Assert.That(slice.Count, Is.EqualTo(4));
                Assert.That(slice.Reversed, Is.False);
            });
        }

        [Test]
        public void ToArrayCopiesSliceRange()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 1, length: 2, reversed: false);

            Assert.That(slice.ToArray(), Is.EqualTo(ToArrayForwardExpected));
        }

        [Test]
        public void ToListCopiesSliceRange()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 1, length: 2, reversed: true);

            Assert.That(slice.ToList(), Is.EqualTo(ToArrayReversedExpected));
        }

        [Test]
        public void EnumeratorYieldsItemsInOrder()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            Assert.That(slice.ToArray(), Is.EqualTo(EnumeratorForwardExpected));
        }

        [Test]
        public void EnumeratorYieldsItemsInReverseWhenReversed()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: true);

            Assert.That(slice.ToArray(), Is.EqualTo(EnumeratorReversedExpected));
        }

        [Test]
        public void IndexerThrowsOnOutOfRange()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(() => slice[-1], Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => slice[3], Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }

        [Test]
        public void ToListReturnsIndependentCopy()
        {
            List<int> source = new() { 10, 20, 30 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            List<int> result = slice.ToList();
            result[0] = 99;

            Assert.Multiple(() =>
            {
                Assert.That(result[0], Is.EqualTo(99));
                Assert.That(source[0], Is.EqualTo(10));
            });
        }

        [Test]
        public void CopyToCopiesIntoTargetArray()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);

            int[] target = { 0, 0, 0, 0 };
            slice.CopyTo(target, 1);

            Assert.That(target, Is.EqualTo(CopyToTargetExpected));
        }

        [Test]
        public void MutatingMethodsThrowInvalidOperation()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 3, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(() => slice.Add(4), Throws.TypeOf<InvalidOperationException>());
                Assert.That(() => slice.Remove(2), Throws.TypeOf<InvalidOperationException>());
                Assert.That(() => slice.Insert(0, 9), Throws.TypeOf<InvalidOperationException>());
                Assert.That(() => slice.Clear(), Throws.TypeOf<InvalidOperationException>());
                Assert.That(() => slice.RemoveAt(0), Throws.TypeOf<InvalidOperationException>());
            });
        }

        [Test]
        public void NonGenericEnumeratorReturnsItems()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);
            System.Collections.IEnumerable enumerable = slice;

            Assert.That(enumerable.Cast<int>().ToArray(), Is.EqualTo(NonGenericEnumeratorExpected));
        }
    }
}
