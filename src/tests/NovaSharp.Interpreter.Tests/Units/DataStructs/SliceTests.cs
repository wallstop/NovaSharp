namespace NovaSharp.Interpreter.Tests.Units.DataStructs
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SliceTests
    {
        private static readonly string[] ExpectedStringSlice = { "b", "c" };
        private static readonly int[] ReversedSlice = { 40, 30, 20 };
        private static readonly int[] CopyBufferExpectation = { 0, 6, 7, 0 };

        [Test]
        public void CountAndIndexerReflectSliceLength()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Slice<int> slice = new Slice<int>(source, from: 1, length: 3, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(slice.Count, Is.EqualTo(3));
                Assert.That(slice[0], Is.EqualTo(2));
                Assert.That(slice[1], Is.EqualTo(3));
                Assert.That(slice[2], Is.EqualTo(4));
            });
        }

        [Test]
        public void IndexerWritesPropagateToSource()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Slice<int> slice = new Slice<int>(source, from: 1, length: 3, reversed: false);

            slice[1] = 42;

            Assert.Multiple(() =>
            {
                Assert.That(slice[1], Is.EqualTo(42));
                Assert.That(source[2], Is.EqualTo(42));
            });
        }

        [Test]
        public void EnumeratorYieldsItemsInOrder()
        {
            List<string> source = new List<string> { "a", "b", "c", "d" };
            Slice<string> slice = new Slice<string>(source, from: 1, length: 2, reversed: false);

            Assert.That(slice, Is.EqualTo(ExpectedStringSlice));
        }

        [Test]
        public void ReversedSliceEnumeratesBackwards()
        {
            List<int> source = new List<int> { 10, 20, 30, 40, 50 };
            Slice<int> slice = new Slice<int>(source, from: 1, length: 3, reversed: true);

            Assert.That(slice, Is.EqualTo(ReversedSlice));
        }

        [Test]
        public void CalcRealIndexThrowsWhenOutOfRange()
        {
            List<int> source = new List<int> { 1, 2, 3 };
            Slice<int> slice = new Slice<int>(source, from: 0, length: 2, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(() => _ = slice[-1], Throws.InstanceOf<ArgumentOutOfRangeException>());
                Assert.That(() => _ = slice[2], Throws.InstanceOf<ArgumentOutOfRangeException>());
            });
        }

        [Test]
        public void CopyToWritesSequentially()
        {
            List<int> source = new List<int> { 5, 6, 7, 8 };
            Slice<int> slice = new Slice<int>(source, from: 1, length: 2, reversed: false);
            int[] buffer = new int[4];

            slice.CopyTo(buffer, 1);

            Assert.That(buffer, Is.EqualTo(CopyBufferExpectation));
        }

        [Test]
        public void IndexOfHonoursReversedSlices()
        {
            List<string> source = new List<string> { "a", "b", "c", "d" };
            Slice<string> slice = new Slice<string>(source, from: 0, length: 3, reversed: true);

            Assert.Multiple(() =>
            {
                Assert.That(slice.IndexOf("c"), Is.EqualTo(0));
                Assert.That(slice.IndexOf("b"), Is.EqualTo(1));
                Assert.That(slice.IndexOf("z"), Is.EqualTo(-1));
            });
        }

        [Test]
        public void AddAndInsertThrowNotSupported()
        {
            List<int> source = new List<int> { 1, 2, 3 };
            Slice<int> slice = new Slice<int>(source, from: 0, length: 2, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => slice.Add(4),
                    Throws.TypeOf<InvalidOperationException>().With.Message.Contains("readonly")
                );
                Assert.That(
                    () => slice.Insert(0, 4),
                    Throws.TypeOf<InvalidOperationException>().With.Message.Contains("readonly")
                );
            });
        }

        [Test]
        public void RemoveAndClearThrowNotSupported()
        {
            List<int> source = new List<int> { 1, 2, 3 };
            Slice<int> slice = new Slice<int>(source, from: 0, length: 2, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => slice.Remove(1),
                    Throws.TypeOf<InvalidOperationException>().With.Message.Contains("readonly")
                );
                Assert.That(
                    () => slice.Clear(),
                    Throws.TypeOf<InvalidOperationException>().With.Message.Contains("readonly")
                );
            });
        }

        [Test]
        public void ContainsAndIndexOfRespectSliceWindow()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Slice<int> slice = new Slice<int>(source, from: 1, length: 3, reversed: false);

            Assert.Multiple(() =>
            {
                Assert.That(slice.Contains(3), Is.True);
                Assert.That(slice.Contains(5), Is.False);
                Assert.That(slice.IndexOf(4), Is.EqualTo(2));
                Assert.That(slice.IndexOf(5), Is.EqualTo(-1));
            });
        }

        [Test]
        public void CopyToThrowsWhenDestinationTooSmall()
        {
            List<int> source = new List<int> { 1, 2, 3, 4 };
            Slice<int> slice = new Slice<int>(source, from: 1, length: 3, reversed: false);
            int[] buffer = new int[3];

            Assert.That(() => slice.CopyTo(buffer, 1), Throws.TypeOf<IndexOutOfRangeException>());
        }
    }
}
