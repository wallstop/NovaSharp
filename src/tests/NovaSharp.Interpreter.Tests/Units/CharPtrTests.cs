namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.Interop.LuaStateInterop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CharPtrTests
    {
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
        public void ToStringRespectsExplicitLength()
        {
            CharPtr ptr = "abcdef";
            CharPtr sliced = new(ptr, 2);

            Assert.That(sliced.ToString(2), Is.EqualTo("cd"));
        }
    }
}
