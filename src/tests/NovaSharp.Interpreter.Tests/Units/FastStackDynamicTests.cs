namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

    [TestFixture]
    public sealed class FastStackDynamicTests
    {
        [Test]
        public void PushPeekAndPopOperateOnTopOfStack()
        {
            FastStackDynamic<int> stack = new FastStackDynamic<int>(startingCapacity: 2);

            stack.Push(10);
            stack.Push(20);
            stack.Push(30);

            Assert.That(stack.Peek(), Is.EqualTo(30));
            Assert.That(stack.Peek(1), Is.EqualTo(20));

            int popped = stack.Pop();
            Assert.That(popped, Is.EqualTo(30));
            Assert.That(stack.Peek(), Is.EqualTo(20));
        }

        [Test]
        public void SetUpdatesItemRelativeToTop()
        {
            FastStackDynamic<int> stack = new FastStackDynamic<int>(startingCapacity: 2);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            stack.Set(1, 99);

            Assert.That(stack.Peek(), Is.EqualTo(3));
            Assert.That(stack.Peek(1), Is.EqualTo(99));
        }

        [Test]
        public void ExpandAddsDefaultSlots()
        {
            FastStackDynamic<string> stack = new FastStackDynamic<string>(startingCapacity: 1);
            stack.Push("first");

            stack.Expand(2);

            Assert.That(stack.Count, Is.EqualTo(3));
            Assert.That(stack.Peek(1), Is.Null);
            Assert.That(stack.Peek(2), Is.EqualTo("first"));
        }

        [Test]
        public void ZeroResetsItemInPlace()
        {
            FastStackDynamic<bool> stack = new FastStackDynamic<bool>(startingCapacity: 1);
            stack.Push(true);
            stack.Push(false);

            stack.Zero(0);

            Assert.Multiple(() =>
            {
                Assert.That(stack[0], Is.False);
                Assert.That(stack.Peek(), Is.False);
            });
        }

        [Test]
        public void RemoveAndCropPruneTailElements()
        {
            FastStackDynamic<int> stack = new FastStackDynamic<int>(startingCapacity: 3);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);

            stack.RemoveLast(2);
            Assert.That(stack.Count, Is.EqualTo(2));
            Assert.That(stack.Peek(), Is.EqualTo(2));

            stack.Push(5);
            stack.Push(6);
            stack.CropAtCount(2);

            Assert.That(stack.Count, Is.EqualTo(2));
            Assert.That(stack.Peek(), Is.EqualTo(2));
        }

        [Test]
        public void TryPopAndTryPeekReturnFalseWhenEmpty()
        {
            FastStackDynamic<int> stack = new FastStackDynamic<int>(startingCapacity: 1);

            Assert.Multiple(() =>
            {
                Assert.That(stack.TryPop(out int popped), Is.False);
                Assert.That(popped, Is.EqualTo(default(int)));
                Assert.That(stack.TryPeek(out int peeked), Is.False);
                Assert.That(peeked, Is.EqualTo(default(int)));
            });
        }

        [Test]
        public void TryPeekSupportsOffsets()
        {
            FastStackDynamic<int> stack = new FastStackDynamic<int>(startingCapacity: 2);
            stack.Push(7);
            stack.Push(8);
            stack.Push(9);

            Assert.Multiple(() =>
            {
                Assert.That(stack.TryPeek(0, out int top), Is.True);
                Assert.That(top, Is.EqualTo(9));
                Assert.That(stack.TryPeek(1, out int next), Is.True);
                Assert.That(next, Is.EqualTo(8));
                Assert.That(stack.TryPeek(5, out int invalid), Is.False);
                Assert.That(invalid, Is.EqualTo(default(int)));
            });
        }

        [Test]
        public void TryPopReturnsLastElementAndShrinks()
        {
            FastStackDynamic<string> stack = new FastStackDynamic<string>(startingCapacity: 2);
            stack.Push("alpha");
            stack.Push("beta");

            bool result = stack.TryPop(out string value);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo("beta"));
                Assert.That(stack.Count, Is.EqualTo(1));
                Assert.That(stack.Peek(), Is.EqualTo("alpha"));
            });
        }
    }
}
