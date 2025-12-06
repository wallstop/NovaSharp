namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataStructs;

    public sealed class FastStackDynamicTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PushPeekAndPopOperateOnTopOfStack()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 2);

            stack.Push(10);
            stack.Push(20);
            stack.Push(30);

            await Assert.That(stack.Peek()).IsEqualTo(30).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(20).ConfigureAwait(false);

            int popped = stack.Pop();
            await Assert.That(popped).IsEqualTo(30).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(20).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetUpdatesItemRelativeToTop()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 2);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            stack.Set(1, 99);

            await Assert.That(stack.Peek()).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsEqualTo(99).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandAddsDefaultSlots()
        {
            FastStackDynamic<string> stack = new(startingCapacity: 1);
            stack.Push("first");

            stack.Expand(2);

            await Assert.That(stack.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(stack.Peek(1)).IsNull().ConfigureAwait(false);
            await Assert.That(stack.Peek(2)).IsEqualTo("first").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ZeroResetsItemInPlace()
        {
            FastStackDynamic<bool> stack = new(startingCapacity: 1);
            stack.Push(true);
            stack.Push(false);

            stack.Zero(0);

            await Assert.That(stack[0]).IsFalse().ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveAndCropPruneTailElements()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 3);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);

            stack.RemoveLast(2);
            await Assert.That(stack.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(2).ConfigureAwait(false);

            stack.Push(5);
            stack.Push(6);
            stack.CropAtCount(2);

            await Assert.That(stack.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryPopAndTryPeekReturnFalseWhenEmpty()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 1);

            bool popResult = stack.TryPop(out int popped);
            bool peekResult = stack.TryPeek(out int peeked);

            await Assert.That(popResult).IsFalse().ConfigureAwait(false);
            await Assert.That(popped).IsEqualTo(default(int)).ConfigureAwait(false);
            await Assert.That(peekResult).IsFalse().ConfigureAwait(false);
            await Assert.That(peeked).IsEqualTo(default(int)).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryPeekSupportsOffsets()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 2);
            stack.Push(7);
            stack.Push(8);
            stack.Push(9);

            bool topResult = stack.TryPeek(0, out int top);
            bool nextResult = stack.TryPeek(1, out int next);
            bool invalidResult = stack.TryPeek(5, out int invalid);

            await Assert.That(topResult).IsTrue().ConfigureAwait(false);
            await Assert.That(top).IsEqualTo(9).ConfigureAwait(false);
            await Assert.That(nextResult).IsTrue().ConfigureAwait(false);
            await Assert.That(next).IsEqualTo(8).ConfigureAwait(false);
            await Assert.That(invalidResult).IsFalse().ConfigureAwait(false);
            await Assert.That(invalid).IsEqualTo(default(int)).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryPopReturnsLastElementAndShrinks()
        {
            FastStackDynamic<string> stack = new(startingCapacity: 2);
            stack.Push("alpha");
            stack.Push("beta");

            bool result = stack.TryPop(out string value);

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(value).IsEqualTo("beta").ConfigureAwait(false);
            await Assert.That(stack.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(stack.Peek()).IsEqualTo("alpha").ConfigureAwait(false);
        }
    }
}
