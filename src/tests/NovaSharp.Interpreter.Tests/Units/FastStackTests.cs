namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

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

        [Test]
        public void RemoveLastWithNonPositiveCountDoesNothing()
        {
            FastStack<int> stack = new(3);
            stack.Push(1);
            stack.Push(2);

            stack.RemoveLast(0);
            stack.RemoveLast(-2);

            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(2));
                Assert.That(stack.Peek(), Is.EqualTo(2));
                Assert.That(stack.Peek(1), Is.EqualTo(1));
            });
        }

        [Test]
        public void RemoveLastThrowsWhenCountExceeded()
        {
            FastStack<int> stack = new(2);
            stack.Push(5);
            stack.Push(6);

            Assert.That(
                () => stack.RemoveLast(3),
                Throws.TypeOf<System.ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void RemoveLastSingleClearsSlot()
        {
            FastStack<int> stack = new(4);
            stack.Push(8);
            stack.Push(9);

            stack.RemoveLast();

            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(1));
                Assert.That(stack.Peek(), Is.EqualTo(8));
                Assert.That(stack[1], Is.EqualTo(0));
            });
        }

        [Test]
        public void ClearResetsAllStorage()
        {
            FastStack<int> stack = new(3);
            stack.Push(11);
            stack.Push(22);

            stack.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(stack.Count, Is.EqualTo(0));
                Assert.That(stack[0], Is.EqualTo(0));
                Assert.That(stack[1], Is.EqualTo(0));
            });
        }

        [Test]
        public void ICollectionAddAndClearDelegateToStack()
        {
            FastStack<int> stack = new(2);
            ICollection<int> collection = stack;

            collection.Add(5);
            collection.Add(6);

            Assert.That(stack.Count, Is.EqualTo(2));
            Assert.That(stack.Peek(), Is.EqualTo(6));

            Assert.That(collection.Count, Is.EqualTo(2));
            Assert.That(collection.IsReadOnly, Is.False);

            collection.Clear();

            Assert.That(stack.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitInterfaceMembersThrowNotImplemented()
        {
            FastStack<int> stack = new(1);
            IList<int> list = stack;
            ICollection<int> collection = stack;

            Assert.Multiple(() =>
            {
                Assert.That(() => list.IndexOf(1), Throws.TypeOf<NotImplementedException>());
                Assert.That(() => list.Insert(0, 1), Throws.TypeOf<NotImplementedException>());
                Assert.That(() => list.RemoveAt(0), Throws.TypeOf<NotImplementedException>());
                Assert.That(() => collection.Contains(1), Throws.TypeOf<NotImplementedException>());
                Assert.That(
                    () => collection.CopyTo(Array.Empty<int>(), 0),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(() => collection.Remove(1), Throws.TypeOf<NotImplementedException>());
                stack.Push(123);
                Assert.That(() => list[0] = 99, Throws.Nothing);
                Assert.That(list[0], Is.EqualTo(99));
                Assert.That(stack.Peek(), Is.EqualTo(99));
                Assert.That(
                    () => ((IEnumerable<int>)stack).GetEnumerator(),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(
                    () => ((System.Collections.IEnumerable)stack).GetEnumerator(),
                    Throws.TypeOf<NotImplementedException>()
                );
            });

            FastStack<int>.TestHooks.ZeroSlot(stack, 0);
        }
    }
}
