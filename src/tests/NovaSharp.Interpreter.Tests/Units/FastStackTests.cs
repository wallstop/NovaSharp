namespace NovaSharp.Interpreter.Tests.Units
{
    using NUnit.Framework;
    using NovaSharp.Interpreter.DataStructs;

    [TestFixture]
    public sealed class FastStackTests
    {
        [Test]
        public void PushPeekAndPopRoundTripValues()
        {
            FastStack<int> stack = new(8);

            stack.Push(1);
            stack.Push(2);

            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(2));
                Assert.That(stack.Peek(), Is.EqualTo(2));
                Assert.That(stack.Peek(1), Is.EqualTo(1));
            });

            int popped = stack.Pop();

            Assert.Multiple(() =>
            {
                Assert.That(popped, Is.EqualTo(2));
                Assert.That(stack.Count, Is.EqualTo(1));
                Assert.That(stack.Peek(), Is.EqualTo(1));
            });
        }

        [Test]
        public void RemoveLastClearsMultipleEntries()
        {
            FastStack<int> stack = new(6);

            stack.Push(10);
            stack.Push(20);
            stack.Push(30);
            stack.Push(40);

            stack.RemoveLast(3);

            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(1));
                Assert.That(stack.Peek(), Is.EqualTo(10));
            });

            stack.Push(50);
            stack.Push(60);
            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(3));
                Assert.That(stack.Peek(), Is.EqualTo(60));
                Assert.That(stack.Peek(1), Is.EqualTo(50));
            });
        }

        [Test]
        public void ExpandReservesSlotsForSet()
        {
            FastStack<int> stack = new(5);
            stack.Push(7);
            stack.Expand(2);

            Assert.That(stack.Count, Is.EqualTo(3));

            stack.Set(0, 30);
            stack.Set(1, 20);

            Assert.Multiple(() =>
            {
                Assert.That(stack.Peek(), Is.EqualTo(30));
                Assert.That(stack.Peek(1), Is.EqualTo(20));
                Assert.That(stack.Peek(2), Is.EqualTo(7));
            });
        }

        [Test]
        public void CropAtCountShrinksStack()
        {
            FastStack<int> stack = new(4);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);

            stack.CropAtCount(2);

            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(2));
                Assert.That(stack.Peek(), Is.EqualTo(2));
                Assert.That(stack.Peek(1), Is.EqualTo(1));
            });
        }

        [Test]
        public void ClearUsedResetsHeadWithoutTouchingCapacity()
        {
            FastStack<string> stack = new(3);
            stack.Push("A");
            stack.Push("B");

            stack.ClearUsed();

            Assert.That(stack.Count, Is.EqualTo(0));

            stack.Push("C");
            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(1));
                Assert.That(stack.Peek(), Is.EqualTo("C"));
            });
        }
    }
}
