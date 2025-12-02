namespace NovaSharp.Interpreter.Tests.TUnit.Units.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataStructs;
    using CollectionAssert = NUnit.Framework.CollectionAssert;

    public sealed class SliceDataStructsTUnitTests
    {
        private static readonly string[] ExpectedStringSlice = { "b", "c" };
        private static readonly int[] ReversedSlice = { 40, 30, 20 };
        private static readonly int[] CopyBufferExpectation = { 0, 6, 7, 0 };

        [global::TUnit.Core.Test]
        public async Task CountAndIndexerReflectSliceLength()
        {
            List<int> source = new() { 1, 2, 3, 4, 5 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: false);

            await Assert.That(slice.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(slice[0]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(slice[1]).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(slice[2]).IsEqualTo(4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexerWritesPropagateToSource()
        {
            List<int> source = new() { 1, 2, 3, 4, 5 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: false);

            slice[1] = 42;

            await Assert.That(slice[1]).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(source[2]).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EnumeratorYieldsItemsInOrder()
        {
            List<string> source = new() { "a", "b", "c", "d" };
            Slice<string> slice = new(source, from: 1, length: 2, reversed: false);

            CollectionAssert.AreEqual(ExpectedStringSlice, slice.ToArray());
        }

        [global::TUnit.Core.Test]
        public async Task ReversedSliceEnumeratesBackwards()
        {
            List<int> source = new() { 10, 20, 30, 40, 50 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: true);

            CollectionAssert.AreEqual(ReversedSlice, slice.ToArray());
        }

        [global::TUnit.Core.Test]
        public void CalcRealIndexThrowsWhenOutOfRange()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = slice[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = slice[2]);
        }

        [global::TUnit.Core.Test]
        public async Task CopyToWritesSequentially()
        {
            List<int> source = new() { 5, 6, 7, 8 };
            Slice<int> slice = new(source, from: 1, length: 2, reversed: false);
            int[] buffer = new int[4];

            slice.CopyTo(buffer, 1);

            CollectionAssert.AreEqual(CopyBufferExpectation, buffer);
        }

        [global::TUnit.Core.Test]
        public async Task IndexOfHonoursReversedSlices()
        {
            List<string> source = new() { "a", "b", "c", "d" };
            Slice<string> slice = new(source, from: 0, length: 3, reversed: true);

            await Assert.That(slice.IndexOf("c")).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(slice.IndexOf("b")).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(slice.IndexOf("z")).IsEqualTo(-1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AddAndInsertThrowNotSupported()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);

            InvalidOperationException addException = Assert.Throws<InvalidOperationException>(() =>
                slice.Add(4)
            )!;
            InvalidOperationException insertException = Assert.Throws<InvalidOperationException>(
                () =>
                    slice.Insert(0, 4)
            )!;

            await Assert.That(addException.Message).Contains("readonly").ConfigureAwait(false);
            await Assert.That(insertException.Message).Contains("readonly").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveAndClearThrowNotSupported()
        {
            List<int> source = new() { 1, 2, 3 };
            Slice<int> slice = new(source, from: 0, length: 2, reversed: false);

            InvalidOperationException removeException = Assert.Throws<InvalidOperationException>(
                () =>
                    slice.Remove(1)
            )!;
            InvalidOperationException clearException = Assert.Throws<InvalidOperationException>(
                () =>
                    slice.Clear()
            )!;

            await Assert.That(removeException.Message).Contains("readonly").ConfigureAwait(false);
            await Assert.That(clearException.Message).Contains("readonly").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ContainsAndIndexOfRespectSliceWindow()
        {
            List<int> source = new() { 1, 2, 3, 4, 5 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: false);

            await Assert.That(slice.Contains(3)).IsTrue().ConfigureAwait(false);
            await Assert.That(slice.Contains(5)).IsFalse().ConfigureAwait(false);
            await Assert.That(slice.IndexOf(4)).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(slice.IndexOf(5)).IsEqualTo(-1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void CopyToThrowsWhenDestinationTooSmall()
        {
            List<int> source = new() { 1, 2, 3, 4 };
            Slice<int> slice = new(source, from: 1, length: 3, reversed: false);
            int[] buffer = new int[3];

            Assert.Throws<IndexOutOfRangeException>(() => slice.CopyTo(buffer, 1));
        }
    }
}
