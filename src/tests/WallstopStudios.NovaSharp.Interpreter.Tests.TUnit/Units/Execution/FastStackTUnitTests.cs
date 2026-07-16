namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class FastStackTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultMaxCapacityIsUnbounded()
        {
            FastStack<int> stack = new(4);

            await Assert.That(stack.MaxCapacity).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GrowthWithinMaxCapacitySucceeds()
        {
            FastStack<int> stack = new(2, maxCapacity: 8);

            for (int i = 0; i < 8; i++)
            {
                stack.Push(i);
            }

            await Assert.That(stack.Count).IsEqualTo(8).ConfigureAwait(false);
            await Assert.That(stack.MaxCapacity).IsEqualTo(8).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PushBeyondMaxCapacityThrowsStackOverflow()
        {
            FastStack<int> stack = new(2, maxCapacity: 4);

            for (int i = 0; i < 4; i++)
            {
                stack.Push(i);
            }

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                stack.Push(99)
            );

            await Assert.That(exception.Message).Contains("stack overflow").ConfigureAwait(false);
            // The failed push must not have corrupted the stack.
            await Assert.That(stack.Count).IsEqualTo(4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandBeyondMaxCapacityThrowsStackOverflow()
        {
            FastStack<int> stack = new(2, maxCapacity: 4);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                stack.Expand(5)
            );

            await Assert.That(exception.Message).Contains("stack overflow").ConfigureAwait(false);
            await Assert.That(stack.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GrowthClampsBackingArrayToMaxCapacity()
        {
            // Geometric doubling from 3 would overshoot to 12; the ceiling must clamp it to 10.
            FastStack<int> stack = new(3, maxCapacity: 10);

            stack.Expand(10);

            await Assert.That(stack.Count).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(stack.Capacity).IsEqualTo(10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GrowthJumpBeyondDoublingAllocatesExactRequired()
        {
            // A single expand far past double the capacity must grow to exactly the required size in one
            // step (this is the same fallback branch that guards the doubling against int overflow) rather
            // than looping. Unbounded ceiling, so no clamp applies.
            FastStack<int> stack = new(2);

            stack.Expand(1000);

            await Assert.That(stack.Count).IsEqualTo(1000).ConfigureAwait(false);
            await Assert.That(stack.Capacity).IsEqualTo(1000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PushPeekAndPopRoundTripValues()
        {
            FastStack<int> stack = new(8);

            stack.Push(1);
            stack.Push(2);

            await Assert.That(stack.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(1).ConfigureAwait(false);

            int popped = stack.Pop();

            await Assert.That(popped).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PushGrowsPastInitialCapacity()
        {
            FastStack<int> stack = new(1);

            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            await Assert.That(stack.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek(2)).IsEqualTo(1).ConfigureAwait(false);
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

            await Assert.That(stack.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(10).ConfigureAwait(false);

            stack.Push(50);
            stack.Push(60);

            await Assert.That(stack.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(60).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(50).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandReservesSlotsForSet()
        {
            FastStack<int> stack = new(5);

            stack.Push(7);
            stack.Expand(2);

            await Assert.That(stack.Count).IsEqualTo(3).ConfigureAwait(false);

            stack.Set(0, 30);
            stack.Set(1, 20);

            await Assert.That(stack.Peek()).IsEqualTo(30).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(stack.Peek(2)).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandGrowsPastInitialCapacity()
        {
            FastStack<int> stack = new(1);

            stack.Push(7);
            stack.Expand(3);

            await Assert.That(stack.Count).IsEqualTo(4).ConfigureAwait(false);

            stack.Set(0, 40);
            stack.Set(1, 30);
            stack.Set(2, 20);

            await Assert.That(stack.Peek()).IsEqualTo(40).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(30).ConfigureAwait(false);
            await Assert.That(stack.Peek(2)).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(stack.Peek(3)).IsEqualTo(7).ConfigureAwait(false);
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

            await Assert.That(stack.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClearUsedResetsHeadWithoutTouchingCapacity()
        {
            FastStack<string> stack = new(3);

            stack.Push("A");
            stack.Push("B");

            stack.ClearUsed();

            await Assert.That(stack.Count).IsEqualTo(0).ConfigureAwait(false);

            stack.Push("C");

            await Assert.That(stack.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo("C").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveLastWithNonPositiveCountDoesNothing()
        {
            FastStack<int> stack = new(3);

            stack.Push(1);
            stack.Push(2);

            stack.RemoveLast(0);
            stack.RemoveLast(-2);

            await Assert.That(stack.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(1).ConfigureAwait(false);
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

            await Assert.That(stack.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(8).ConfigureAwait(false);
            await Assert.That(stack[1]).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClearResetsAllStorage()
        {
            FastStack<int> stack = new(3);

            stack.Push(11);
            stack.Push(22);

            stack.Clear();

            await Assert.That(stack.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(stack[0]).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(stack[1]).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ICollectionAddAndClearDelegateToStack()
        {
            FastStack<int> stack = new(2);
            ICollection<int> collection = stack;

            collection.Add(5);
            collection.Add(6);

            await Assert.That(stack.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(6).ConfigureAwait(false);
            await Assert.That(collection.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(collection.IsReadOnly).IsFalse().ConfigureAwait(false);

            collection.Clear();

            await Assert.That(stack.Count).IsEqualTo(0).ConfigureAwait(false);
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

            await Assert.That(list[0]).IsEqualTo(99).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(99).ConfigureAwait(false);

            Assert.Throws<NotImplementedException>(() => ((IEnumerable<int>)stack).GetEnumerator());
            Assert.Throws<NotImplementedException>(() => ((IEnumerable)stack).GetEnumerator());

            FastStack<int>.TestHooks.ZeroSlot(stack, 0);
        }
    }
}
