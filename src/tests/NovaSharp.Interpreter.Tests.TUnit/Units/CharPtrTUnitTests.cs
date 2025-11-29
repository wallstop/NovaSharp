#pragma warning disable CA2007
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

            await Assert.That(ptr[0]).IsEqualTo('a');
            await Assert.That(ptr[1]).IsEqualTo('b');
            await Assert.That(ptr[2]).IsEqualTo('c');
            await Assert.That(ptr[3]).IsEqualTo('\0');
        }

        [global::TUnit.Core.Test]
        public async Task IndexerSupportsUnsignedAndLongOffsets()
        {
            CharPtr ptr = "abcd";
            ptr[1u] = 'Z';
            ptr[2L] = 'Y';

            await Assert.That(ptr.ToString()).IsEqualTo("aZYd");
        }

        [global::TUnit.Core.Test]
        public async Task AdditionConcatenatesUntilNullTerminator()
        {
            CharPtr result = "hello" + " world";

            await Assert.That(result.ToString()).IsEqualTo("hello world");
        }

        [global::TUnit.Core.Test]
        public async Task ImplicitConversionFromByteArrayCopiesContents()
        {
            byte[] bytes = new byte[] { (byte)'A', (byte)'B', (byte)'C', 0 };

            CharPtr ptr = bytes;

            await Assert.That(ptr[0]).IsEqualTo('A');
            await Assert.That(ptr[1]).IsEqualTo('B');
            await Assert.That(ptr[2]).IsEqualTo('C');
            await Assert.That(ptr[3]).IsEqualTo('\0');
        }

        [global::TUnit.Core.Test]
        public async Task SubtractionReturnsRelativeOffsetWithinSameBuffer()
        {
            CharPtr root = "abcdef";
            CharPtr start = new(root, 0);
            CharPtr later = new(root, 3);

            await Assert.That(later - start).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task RelationalOperatorsCompareIndexWithinSameBuffer()
        {
            CharPtr buffer = "abcdef";
            CharPtr head = new(buffer, 0);
            CharPtr tail = new(buffer, 5);

            await Assert.That(head < tail).IsTrue();
            await Assert.That(head <= tail).IsTrue();
            await Assert.That(tail > head).IsTrue();
            await Assert.That(tail >= head).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EqualityOperatorsRespectBufferAndIndex()
        {
            CharPtr buffer = "xyz";
            CharPtr first = new(buffer, 0);
            CharPtr second = new(buffer, 0);
            CharPtr advanced = new(buffer, 1);

            await Assert.That(first == second).IsTrue();
            await Assert.That(first != advanced).IsTrue();
            await Assert.That(first == 'x').IsTrue();
            await Assert.That('x' == first).IsTrue();
            await Assert.That(first != 'y').IsTrue();
            await Assert.That('y' != first).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EqualsUsesPointerEquality()
        {
            CharPtr buffer = "pointer";
            CharPtr first = new(buffer, 0);
            CharPtr same = new(buffer, 0);
            CharPtr different = "pointer";

            await Assert.That(first.Equals(same)).IsTrue();
            await Assert.That(first.Equals(different)).IsFalse();
        }

#pragma warning disable CA1508
        [global::TUnit.Core.Test]
        public async Task EqualityHandlesNullPointers()
        {
            CharPtr buffer = "null";
            CharPtr pointer = new(buffer, 0);
            CharPtr nullLeft = (CharPtr)null;
            CharPtr nullRight = (CharPtr)null;

            await Assert.That(nullLeft == nullRight).IsTrue();
            await Assert.That(pointer == nullLeft).IsFalse();
            await Assert.That(nullRight == pointer).IsFalse();
            await Assert.That(pointer.GetHashCode()).IsEqualTo(0);
        }
#pragma warning restore CA1508

        [global::TUnit.Core.Test]
        public async Task ToStringRespectsExplicitLength()
        {
            CharPtr sliced = new(CharPtr.FromString("abcdef"), 2);

            await Assert.That(sliced.ToString(2)).IsEqualTo("cd");
        }

        [global::TUnit.Core.Test]
        public async Task PointerArithmeticSupportsIntegerAndUnsignedOffsets()
        {
            CharPtr buffer = "abcdef";

            CharPtr advanced = buffer + 2;
            CharPtr rewind = advanced - 1;
            CharPtr viaUnsigned = buffer + (uint)3;
            CharPtr rewindUnsigned = viaUnsigned - (uint)2;

            await Assert.That(advanced[0]).IsEqualTo('c');
            await Assert.That(rewind[0]).IsEqualTo('b');
            await Assert.That(viaUnsigned[0]).IsEqualTo('d');
            await Assert.That(rewindUnsigned[0]).IsEqualTo('b');
        }

        [global::TUnit.Core.Test]
        public async Task IncrementAndDecrementMutateIndexInPlace()
        {
            CharPtr buffer = "xyz";
            buffer.Inc();
            await Assert.That(buffer[0]).IsEqualTo('y');

            buffer.Dec();
            await Assert.That(buffer[0]).IsEqualTo('x');
        }

        [global::TUnit.Core.Test]
        public async Task NavigationHelpersReturnRelativeViews()
        {
            CharPtr buffer = "pointer";
            CharPtr view = new(buffer, 2);

            await Assert.That(view.Next()[0]).IsEqualTo('n');
            await Assert.That(view.Prev()[0]).IsEqualTo('o');
            await Assert.That(view.Add(2)[0]).IsEqualTo('t');
            await Assert.That(view.Sub(2)[0]).IsEqualTo('p');
        }

        [global::TUnit.Core.Test]
        public async Task DefaultAndIntPtrConstructorsInitialiseEmptyBuffers()
        {
            CharPtr defaultPtr = new();
            CharPtr fromIntPtr = new(IntPtr.Zero);

            await Assert.That(defaultPtr.chars).IsNull();
            await Assert.That(defaultPtr.index).IsEqualTo(0);
            await Assert.That(fromIntPtr.chars).IsNotNull();
            await Assert.That(fromIntPtr.chars.Length).IsEqualTo(0);
            await Assert.That(fromIntPtr.index).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task CharArrayConstructorPreservesExistingBuffer()
        {
            char[] data = new[] { 'a', 'b', 'c', '\0' };
            CharPtr ptr = data;

            await Assert.That(ptr[0]).IsEqualTo('a');
            await Assert.That(ptr[1]).IsEqualTo('b');
            await Assert.That(ptr[2]).IsEqualTo('c');
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

            await Assert.That(fromString.ToString()).IsEqualTo("lua");
            await Assert.That(fromChars[1]).IsEqualTo('B');
            await Assert.That(fromBytes[1]).IsEqualTo('y');
            await Assert.That(rewindStatic[0]).IsEqualTo('c');
            await Assert.That(rewindStaticUnsigned[0]).IsEqualTo('d');
            await Assert.That(rewindInstance[0]).IsEqualTo('c');
            await Assert.That(CharPtr.Subtract(middle, start)).IsEqualTo(3);
            await Assert.That(CharPtr.Compare(start, middle)).IsLessThan(0);
            await Assert.That(CharPtr.Compare(middle, middle)).IsEqualTo(0);
            await Assert.That(CharPtr.Compare(middle, start)).IsGreaterThan(0);
        }
    }
}
#pragma warning restore CA2007
