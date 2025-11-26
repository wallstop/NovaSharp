namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.LuaPort.LuaStateInterop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CharPtrTests
    {
        private static readonly char[] FriendlyCharArray = new[] { 'A', 'B', 'C', '\0' };
        private static readonly byte[] FriendlyByteArray = new byte[] { (byte)'x', (byte)'y', 0 };

        [Test]
        public void ImplicitConversionFromStringCreatesNullTerminatedBuffer()
        {
            CharPtr ptr = "abc";

            Assert.Multiple(() =>
            {
                Assert.That(ptr[0], Is.EqualTo('a'));
                Assert.That(ptr[1], Is.EqualTo('b'));
                Assert.That(ptr[2], Is.EqualTo('c'));
                Assert.That(ptr[3], Is.EqualTo('\0'));
            });
        }

        [Test]
        public void IndexerSupportsUnsignedAndLongOffsets()
        {
            CharPtr ptr = "abcd";
            ptr[1u] = 'Z';
            ptr[2L] = 'Y';

            Assert.That(ptr.ToString(), Is.EqualTo("aZYd"));
        }

        [Test]
        public void AdditionConcatenatesUntilNullTerminator()
        {
            CharPtr ptr1 = "hello";
            CharPtr ptr2 = " world";

            CharPtr result = ptr1 + ptr2;

            Assert.That(result.ToString(), Is.EqualTo("hello world"));
        }

        [Test]
        public void ImplicitConversionFromByteArrayCopiesContents()
        {
            byte[] bytes = new byte[] { (byte)'A', (byte)'B', (byte)'C', 0 };

            CharPtr ptr = bytes;

            Assert.Multiple(() =>
            {
                Assert.That(ptr[0], Is.EqualTo('A'));
                Assert.That(ptr[1], Is.EqualTo('B'));
                Assert.That(ptr[2], Is.EqualTo('C'));
                Assert.That(ptr[3], Is.EqualTo('\0'));
            });
        }

        [Test]
        public void SubtractionReturnsRelativeOffsetWithinSameBuffer()
        {
            CharPtr root = "abcdef";
            CharPtr start = new(root, 0);
            CharPtr later = new(root, 3);

            Assert.That(later - start, Is.EqualTo(3));
        }

        [Test]
        public void RelationalOperatorsCompareIndexWithinSameBuffer()
        {
            CharPtr buffer = "abcdef";
            CharPtr head = new(buffer, 0);
            CharPtr tail = new(buffer, 5);

            Assert.Multiple(() =>
            {
                Assert.That(head < tail, Is.True);
                Assert.That(head <= tail, Is.True);
                Assert.That(tail > head, Is.True);
                Assert.That(tail >= head, Is.True);
            });
        }

        [Test]
        public void EqualityOperatorsRespectBufferAndIndex()
        {
            CharPtr buffer = "xyz";
            CharPtr first = new(buffer, 0);
            CharPtr second = new(buffer, 0);
            CharPtr advanced = new(buffer, 1);

            Assert.Multiple(() =>
            {
                Assert.That(first == second, Is.True);
                Assert.That(first != advanced, Is.True);
                Assert.That(first == 'x', Is.True);
                Assert.That('x' == first, Is.True);
                Assert.That(first != 'y', Is.True);
                Assert.That('y' != first, Is.True);
            });
        }

        [Test]
        public void EqualsUsesPointerEquality()
        {
            CharPtr buffer = "pointer";
            CharPtr first = new(buffer, 0);
            CharPtr same = new(buffer, 0);
            CharPtr different = "pointer";

            Assert.Multiple(() =>
            {
                Assert.That(first.Equals(same), Is.True);
                Assert.That(first.Equals(different), Is.False);
            });
        }

        [Test]
        public void EqualityHandlesNullPointers()
        {
            CharPtr buffer = "null";
            CharPtr pointer = new(buffer, 0);

            Assert.Multiple(() =>
            {
                Assert.That((CharPtr)null == (CharPtr)null, Is.True);
                Assert.That(pointer == (CharPtr)null, Is.False);
                Assert.That((CharPtr)null == pointer, Is.False);
                Assert.That(pointer.GetHashCode(), Is.EqualTo(0));
            });
        }

        [Test]
        public void ToStringRespectsExplicitLength()
        {
            CharPtr ptr = "abcdef";
            CharPtr sliced = new(ptr, 2);

            Assert.That(sliced.ToString(2), Is.EqualTo("cd"));
        }

        [Test]
        public void PointerArithmeticSupportsIntegerAndUnsignedOffsets()
        {
            CharPtr buffer = "abcdef";

            CharPtr advanced = buffer + 2;
            CharPtr rewind = advanced - 1;
            CharPtr viaUnsigned = buffer + (uint)3;
            CharPtr rewindUnsigned = viaUnsigned - (uint)2;

            Assert.Multiple(() =>
            {
                Assert.That(advanced[0], Is.EqualTo('c'));
                Assert.That(rewind[0], Is.EqualTo('b'));
                Assert.That(viaUnsigned[0], Is.EqualTo('d'));
                Assert.That(rewindUnsigned[0], Is.EqualTo('b'));
            });
        }

        [Test]
        public void IncrementAndDecrementMutateIndexInPlace()
        {
            CharPtr buffer = "xyz";
            buffer.Inc();
            Assert.That(buffer[0], Is.EqualTo('y'));

            buffer.Dec();
            Assert.That(buffer[0], Is.EqualTo('x'));
        }

        [Test]
        public void NavigationHelpersReturnRelativeViews()
        {
            CharPtr buffer = "pointer";
            CharPtr view = new(buffer, 2);

            Assert.Multiple(() =>
            {
                Assert.That(view.Next()[0], Is.EqualTo('n'));
                Assert.That(view.Prev()[0], Is.EqualTo('o'));
                Assert.That(view.Add(2)[0], Is.EqualTo('t'));
                Assert.That(view.Sub(2)[0], Is.EqualTo('p'));
            });
        }

        [Test]
        public void DefaultAndIntPtrConstructorsInitialiseEmptyBuffers()
        {
            CharPtr defaultPtr = new();
            CharPtr fromIntPtr = new(IntPtr.Zero);

            Assert.Multiple(() =>
            {
                Assert.That(defaultPtr.chars, Is.Null);
                Assert.That(defaultPtr.index, Is.EqualTo(0));
                Assert.That(fromIntPtr.chars, Is.Not.Null);
                Assert.That(fromIntPtr.chars.Length, Is.EqualTo(0));
                Assert.That(fromIntPtr.index, Is.EqualTo(0));
            });
        }

        [Test]
        public void CharArrayConstructorPreservesExistingBuffer()
        {
            char[] data = new[] { 'a', 'b', 'c', '\0' };
            CharPtr ptr = data;

            Assert.Multiple(() =>
            {
                Assert.That(ptr[0], Is.EqualTo('a'));
                Assert.That(ptr[1], Is.EqualTo('b'));
                Assert.That(ptr[2], Is.EqualTo('c'));
            });
        }

        [Test]
        public void FriendlyAlternatesMirrorOperatorBehaviour()
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

            Assert.Multiple(() =>
            {
                Assert.That(fromString.ToString(), Is.EqualTo("lua"));
                Assert.That(fromChars[1], Is.EqualTo('B'));
                Assert.That(fromBytes[1], Is.EqualTo('y'));
                Assert.That(rewindStatic[0], Is.EqualTo('c'));
                Assert.That(rewindStaticUnsigned[0], Is.EqualTo('d'));
                Assert.That(rewindInstance[0], Is.EqualTo('c'));
                Assert.That(CharPtr.Subtract(middle, start), Is.EqualTo(3));
                Assert.That(CharPtr.Compare(start, middle), Is.LessThan(0));
                Assert.That(CharPtr.Compare(middle, middle), Is.EqualTo(0));
                Assert.That(CharPtr.Compare(middle, start), Is.GreaterThan(0));
            });
        }
    }
}
