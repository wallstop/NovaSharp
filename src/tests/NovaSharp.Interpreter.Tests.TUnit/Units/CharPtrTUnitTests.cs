namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.LuaPort.LuaStateInterop;

    public sealed class CharPtrTUnitTests
    {
        private static readonly char[] FriendlyCharArray = new[] { 'A', 'B', 'C', '\0' };
        private static readonly byte[] FriendlyByteArray = new byte[] { (byte)'x', (byte)'y', 0 };

        [global::TUnit.Core.Test]
        public async Task ImplicitConversionFromStringCreatesNullTerminatedBuffer()
        {
            CharPtr ptr = "abc";

            await Assert.That(ptr[0]).IsEqualTo('a').ConfigureAwait(false);
            await Assert.That(ptr[1]).IsEqualTo('b').ConfigureAwait(false);
            await Assert.That(ptr[2]).IsEqualTo('c').ConfigureAwait(false);
            await Assert.That(ptr[3]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexerSupportsUnsignedAndLongOffsets()
        {
            CharPtr ptr = "abcd";
            ptr[1u] = 'Z';
            ptr[2L] = 'Y';

            await Assert.That(ptr.ToString()).IsEqualTo("aZYd").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AdditionConcatenatesUntilNullTerminator()
        {
            CharPtr result = "hello" + " world";

            await Assert.That(result.ToString()).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ImplicitConversionFromByteArrayCopiesContents()
        {
            byte[] bytes = new byte[] { (byte)'A', (byte)'B', (byte)'C', 0 };

            CharPtr ptr = bytes;

            await Assert.That(ptr[0]).IsEqualTo('A').ConfigureAwait(false);
            await Assert.That(ptr[1]).IsEqualTo('B').ConfigureAwait(false);
            await Assert.That(ptr[2]).IsEqualTo('C').ConfigureAwait(false);
            await Assert.That(ptr[3]).IsEqualTo('\0').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SubtractionReturnsRelativeOffsetWithinSameBuffer()
        {
            CharPtr root = "abcdef";
            CharPtr start = new(root, 0);
            CharPtr later = new(root, 3);

            await Assert.That(later - start).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RelationalOperatorsCompareIndexWithinSameBuffer()
        {
            CharPtr buffer = "abcdef";
            CharPtr head = new(buffer, 0);
            CharPtr tail = new(buffer, 5);

            await Assert.That(head < tail).IsTrue().ConfigureAwait(false);
            await Assert.That(head <= tail).IsTrue().ConfigureAwait(false);
            await Assert.That(tail > head).IsTrue().ConfigureAwait(false);
            await Assert.That(tail >= head).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualityOperatorsRespectBufferAndIndex()
        {
            CharPtr buffer = "xyz";
            CharPtr first = new(buffer, 0);
            CharPtr second = new(buffer, 0);
            CharPtr advanced = new(buffer, 1);

            await Assert.That(first == second).IsTrue().ConfigureAwait(false);
            await Assert.That(first != advanced).IsTrue().ConfigureAwait(false);
            await Assert.That(first == 'x').IsTrue().ConfigureAwait(false);
            await Assert.That('x' == first).IsTrue().ConfigureAwait(false);
            await Assert.That(first != 'y').IsTrue().ConfigureAwait(false);
            await Assert.That('y' != first).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualsUsesPointerEquality()
        {
            CharPtr buffer = "pointer";
            CharPtr first = new(buffer, 0);
            CharPtr same = new(buffer, 0);
            CharPtr different = "pointer";

            await Assert.That(first.Equals(same)).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Equals(different)).IsFalse().ConfigureAwait(false);
        }

#pragma warning disable CA1508
        [global::TUnit.Core.Test]
        public async Task EqualityHandlesNullPointers()
        {
            CharPtr buffer = "null";
            CharPtr pointer = new(buffer, 0);
            CharPtr nullLeft = (CharPtr)null;
            CharPtr nullRight = (CharPtr)null;

            await Assert.That(nullLeft == nullRight).IsTrue().ConfigureAwait(false);
            await Assert.That(pointer == nullLeft).IsFalse().ConfigureAwait(false);
            await Assert.That(nullRight == pointer).IsFalse().ConfigureAwait(false);
            await Assert.That(pointer.GetHashCode()).IsEqualTo(0).ConfigureAwait(false);
        }
#pragma warning restore CA1508

        [global::TUnit.Core.Test]
        public async Task ToStringRespectsExplicitLength()
        {
            CharPtr sliced = new(CharPtr.FromString("abcdef"), 2);

            await Assert.That(sliced.ToString(2)).IsEqualTo("cd").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PointerArithmeticSupportsIntegerAndUnsignedOffsets()
        {
            CharPtr buffer = "abcdef";

            CharPtr advanced = buffer + 2;
            CharPtr rewind = advanced - 1;
            CharPtr viaUnsigned = buffer + (uint)3;
            CharPtr rewindUnsigned = viaUnsigned - (uint)2;

            await Assert.That(advanced[0]).IsEqualTo('c').ConfigureAwait(false);
            await Assert.That(rewind[0]).IsEqualTo('b').ConfigureAwait(false);
            await Assert.That(viaUnsigned[0]).IsEqualTo('d').ConfigureAwait(false);
            await Assert.That(rewindUnsigned[0]).IsEqualTo('b').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IncrementAndDecrementMutateIndexInPlace()
        {
            CharPtr buffer = "xyz";
            buffer.Inc();
            await Assert.That(buffer[0]).IsEqualTo('y').ConfigureAwait(false);

            buffer.Dec();
            await Assert.That(buffer[0]).IsEqualTo('x').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NavigationHelpersReturnRelativeViews()
        {
            CharPtr buffer = "pointer";
            CharPtr view = new(buffer, 2);

            await Assert.That(view.Next()[0]).IsEqualTo('n').ConfigureAwait(false);
            await Assert.That(view.Prev()[0]).IsEqualTo('o').ConfigureAwait(false);
            await Assert.That(view.Add(2)[0]).IsEqualTo('t').ConfigureAwait(false);
            await Assert.That(view.Sub(2)[0]).IsEqualTo('p').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultAndIntPtrConstructorsInitialiseEmptyBuffers()
        {
            CharPtr defaultPtr = new();
            CharPtr fromIntPtr = new(IntPtr.Zero);

            await Assert.That(defaultPtr.chars).IsNull().ConfigureAwait(false);
            await Assert.That(defaultPtr.index).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(fromIntPtr.chars).IsNotNull().ConfigureAwait(false);
            await Assert.That(fromIntPtr.chars.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(fromIntPtr.index).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CharArrayConstructorPreservesExistingBuffer()
        {
            char[] data = new[] { 'a', 'b', 'c', '\0' };
            CharPtr ptr = data;

            await Assert.That(ptr[0]).IsEqualTo('a').ConfigureAwait(false);
            await Assert.That(ptr[1]).IsEqualTo('b').ConfigureAwait(false);
            await Assert.That(ptr[2]).IsEqualTo('c').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FriendlyAlternatesMirrorOperatorBehaviour()
        {
            CharPtr fromString = CharPtr.FromString("lua");
            CharPtr fromChars = CharPtr.FromCharArray(FriendlyCharArray);
            CharPtr fromBytes = CharPtr.FromByteArray(FriendlyByteArray);
            CharPtr buffer = "abcdef";
            CharPtr advanced = buffer.Add(4);
            CharPtr rewindStatic = CharPtr.Subtract(advanced, 2);
            CharPtr rewindStaticUnsigned = CharPtr.Subtract(advanced, (uint)1);
            CharPtr rewindInstance = advanced.Subtract(2);
            CharPtr start = new(buffer, 0);
            CharPtr middle = new(buffer, 3);

            await Assert.That(fromString.ToString()).IsEqualTo("lua").ConfigureAwait(false);
            await Assert.That(fromChars[1]).IsEqualTo('B').ConfigureAwait(false);
            await Assert.That(fromBytes[1]).IsEqualTo('y').ConfigureAwait(false);
            await Assert.That(rewindStatic[0]).IsEqualTo('c').ConfigureAwait(false);
            await Assert.That(rewindStaticUnsigned[0]).IsEqualTo('d').ConfigureAwait(false);
            await Assert.That(rewindInstance[0]).IsEqualTo('c').ConfigureAwait(false);
            await Assert.That(CharPtr.Subtract(middle, start)).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(CharPtr.Compare(start, middle)).IsLessThan(0).ConfigureAwait(false);
            await Assert.That(CharPtr.Compare(middle, middle)).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(CharPtr.Compare(middle, start))
                .IsGreaterThan(0)
                .ConfigureAwait(false);
        }
    }
}
