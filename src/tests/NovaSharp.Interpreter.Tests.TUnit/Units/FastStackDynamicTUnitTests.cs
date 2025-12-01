#pragma warning disable CA2007
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

            await Assert.That(stack.Peek()).IsEqualTo(30);
            await Assert.That(stack.Peek(1)).IsEqualTo(20);

            int popped = stack.Pop();
            await Assert.That(popped).IsEqualTo(30);
            await Assert.That(stack.Peek()).IsEqualTo(20);
        }

        [global::TUnit.Core.Test]
        public async Task SetUpdatesItemRelativeToTop()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 2);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            stack.Set(1, 99);

            await Assert.That(stack.Peek()).IsEqualTo(3);
            await Assert.That(stack.Peek(1)).IsEqualTo(99);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandAddsDefaultSlots()
        {
            FastStackDynamic<string> stack = new(startingCapacity: 1);
            stack.Push("first");

            stack.Expand(2);

            await Assert.That(stack.Count).IsEqualTo(3);
            await Assert.That(stack.Peek(1)).IsNull();
            await Assert.That(stack.Peek(2)).IsEqualTo("first");
        }

        [global::TUnit.Core.Test]
        public async Task ZeroResetsItemInPlace()
        {
            FastStackDynamic<bool> stack = new(startingCapacity: 1);
            stack.Push(true);
            stack.Push(false);

            stack.Zero(0);

            await Assert.That(stack[0]).IsFalse();
            await Assert.That(stack.Peek()).IsFalse();
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
            await Assert.That(stack.Count).IsEqualTo(2);
            await Assert.That(stack.Peek()).IsEqualTo(2);

            stack.Push(5);
            stack.Push(6);
            stack.CropAtCount(2);

            await Assert.That(stack.Count).IsEqualTo(2);
            await Assert.That(stack.Peek()).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task TryPopAndTryPeekReturnFalseWhenEmpty()
        {
            FastStackDynamic<int> stack = new(startingCapacity: 1);

            bool popResult = stack.TryPop(out int popped);
            bool peekResult = stack.TryPeek(out int peeked);

            await Assert.That(popResult).IsFalse();
            await Assert.That(popped).IsEqualTo(default(int));
            await Assert.That(peekResult).IsFalse();
            await Assert.That(peeked).IsEqualTo(default(int));
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

            await Assert.That(topResult).IsTrue();
            await Assert.That(top).IsEqualTo(9);
            await Assert.That(nextResult).IsTrue();
            await Assert.That(next).IsEqualTo(8);
            await Assert.That(invalidResult).IsFalse();
            await Assert.That(invalid).IsEqualTo(default(int));
        }

        [global::TUnit.Core.Test]
        public async Task TryPopReturnsLastElementAndShrinks()
        {
            FastStackDynamic<string> stack = new(startingCapacity: 2);
            stack.Push("alpha");
            stack.Push("beta");

            bool result = stack.TryPop(out string value);

            await Assert.That(result).IsTrue();
            await Assert.That(value).IsEqualTo("beta");
            await Assert.That(stack.Count).IsEqualTo(1);
            await Assert.That(stack.Peek()).IsEqualTo("alpha");
        }
    }
}
#pragma warning restore CA2007
