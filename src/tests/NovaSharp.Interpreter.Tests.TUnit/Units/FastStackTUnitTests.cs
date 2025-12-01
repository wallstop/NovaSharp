#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataStructs;

    public sealed class FastStackTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PushPeekAndPopRoundTripValues()
        {
            FastStack<int> stack = new(8);

            stack.Push(1);
            stack.Push(2);

            await Assert.That(stack.Count).IsEqualTo(2);
            await Assert.That(stack.Peek()).IsEqualTo(2);
            await Assert.That(stack.Peek(1)).IsEqualTo(1);

            int popped = stack.Pop();

            await Assert.That(popped).IsEqualTo(2);
            await Assert.That(stack.Count).IsEqualTo(1);
            await Assert.That(stack.Peek()).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveLastClearsMultipleEntries()
        {
            FastStack<int> stack = new(6);

            stack.Push(10);
            stack.Push(20);
            stack.Push(30);
            stack.Push(40);

            stack.RemoveLast(3);

            await Assert.That(stack.Count).IsEqualTo(1);
            await Assert.That(stack.Peek()).IsEqualTo(10);

            stack.Push(50);
            stack.Push(60);

            await Assert.That(stack.Count).IsEqualTo(3);
            await Assert.That(stack.Peek()).IsEqualTo(60);
            await Assert.That(stack.Peek(1)).IsEqualTo(50);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandReservesSlotsForSet()
        {
            FastStack<int> stack = new(5);

            stack.Push(7);
            stack.Expand(2);

            await Assert.That(stack.Count).IsEqualTo(3);

            stack.Set(0, 30);
            stack.Set(1, 20);

            await Assert.That(stack.Peek()).IsEqualTo(30);
            await Assert.That(stack.Peek(1)).IsEqualTo(20);
            await Assert.That(stack.Peek(2)).IsEqualTo(7);
        }

        [global::TUnit.Core.Test]
        public async Task CropAtCountShrinksStack()
        {
            FastStack<int> stack = new(4);

            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);

            stack.CropAtCount(2);

            await Assert.That(stack.Count).IsEqualTo(2);
            await Assert.That(stack.Peek()).IsEqualTo(2);
            await Assert.That(stack.Peek(1)).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ClearUsedResetsHeadWithoutTouchingCapacity()
        {
            FastStack<string> stack = new(3);

            stack.Push("A");
            stack.Push("B");

            stack.ClearUsed();

            await Assert.That(stack.Count).IsEqualTo(0);

            stack.Push("C");

            await Assert.That(stack.Count).IsEqualTo(1);
            await Assert.That(stack.Peek()).IsEqualTo("C");
        }

        [global::TUnit.Core.Test]
        public async Task RemoveLastWithNonPositiveCountDoesNothing()
        {
            FastStack<int> stack = new(3);

            stack.Push(1);
            stack.Push(2);

            stack.RemoveLast(0);
            stack.RemoveLast(-2);

            await Assert.That(stack.Count).IsEqualTo(2);
            await Assert.That(stack.Peek()).IsEqualTo(2);
            await Assert.That(stack.Peek(1)).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public void RemoveLastThrowsWhenCountExceeded()
        {
            FastStack<int> stack = new(2);

            stack.Push(5);
            stack.Push(6);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => stack.RemoveLast(3));
        }

        [global::TUnit.Core.Test]
        public async Task RemoveLastSingleClearsSlot()
        {
            FastStack<int> stack = new(4);

            stack.Push(8);
            stack.Push(9);

            stack.RemoveLast();

            await Assert.That(stack.Count).IsEqualTo(1);
            await Assert.That(stack.Peek()).IsEqualTo(8);
            await Assert.That(stack[1]).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task ClearResetsAllStorage()
        {
            FastStack<int> stack = new(3);

            stack.Push(11);
            stack.Push(22);

            stack.Clear();

            await Assert.That(stack.Count).IsEqualTo(0);
            await Assert.That(stack[0]).IsEqualTo(0);
            await Assert.That(stack[1]).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task ICollectionAddAndClearDelegateToStack()
        {
            FastStack<int> stack = new(2);
            ICollection<int> collection = stack;

            collection.Add(5);
            collection.Add(6);

            await Assert.That(stack.Count).IsEqualTo(2);
            await Assert.That(stack.Peek()).IsEqualTo(6);
            await Assert.That(collection.Count).IsEqualTo(2);
            await Assert.That(collection.IsReadOnly).IsFalse();

            collection.Clear();

            await Assert.That(stack.Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task ExplicitInterfaceMembersThrowNotImplemented()
        {
            FastStack<int> stack = new(1);
            IList<int> list = stack;
            ICollection<int> collection = stack;

            Assert.Throws<NotImplementedException>(() => list.IndexOf(1));
            Assert.Throws<NotImplementedException>(() => list.Insert(0, 1));
            Assert.Throws<NotImplementedException>(() => list.RemoveAt(0));
            Assert.Throws<NotImplementedException>(() => collection.Contains(1));
            Assert.Throws<NotImplementedException>(() => collection.CopyTo(Array.Empty<int>(), 0));
            Assert.Throws<NotImplementedException>(() => collection.Remove(1));

            stack.Push(123);
            list[0] = 99;

            await Assert.That(list[0]).IsEqualTo(99);
            await Assert.That(stack.Peek()).IsEqualTo(99);

            Assert.Throws<NotImplementedException>(() => ((IEnumerable<int>)stack).GetEnumerator());
            Assert.Throws<NotImplementedException>(() => ((IEnumerable)stack).GetEnumerator());

            FastStack<int>.TestHooks.ZeroSlot(stack, 0);
        }
    }
}
#pragma warning restore CA2007
