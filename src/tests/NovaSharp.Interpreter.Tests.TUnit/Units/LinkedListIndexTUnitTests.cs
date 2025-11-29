#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataStructs;

    public sealed class LinkedListIndexTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task SetReturnsPreviousValueAndUpdatesExistingNode()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            int previous = index.Set("answer", 42);

            await Assert.That(previous).IsEqualTo(default(int));
            await Assert.That(backingList.Count).IsEqualTo(1);
            await Assert.That(index.Find("answer")?.Value).IsEqualTo(42);

            int updated = index.Set("answer", 88);

            await Assert.That(updated).IsEqualTo(42);
            await Assert.That(backingList.Count).IsEqualTo(1);
            await Assert.That(index.Find("answer")?.Value).IsEqualTo(88);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveRemovesNodeFromListAndIndex()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);
            index.Add("first", 1);
            index.Add("second", 2);

            bool removed = index.Remove("first");

            await Assert.That(removed).IsTrue();
            await Assert.That(backingList.Count).IsEqualTo(1);
            await Assert.That(index.Find("first")).IsNull();

            bool missingRemoval = index.Remove("missing");
            await Assert.That(missingRemoval).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveReturnsFalseWhenMapNull()
        {
            LinkedListIndex<string, int> index = new(new LinkedList<int>());

            await Assert.That(index.Remove("unknown")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task TryGetValueHandlesMissingAndExistingKeys()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            bool missingResult = index.TryGetValue("missing", out LinkedListNode<int> missing);
            await Assert.That(missingResult).IsFalse();
            await Assert.That(missing).IsNull();

            index.Add("value", 7);

            bool hitResult = index.TryGetValue("value", out LinkedListNode<int> node);
            await Assert.That(hitResult).IsTrue();
            await Assert.That(node.Value).IsEqualTo(7);
        }

        [global::TUnit.Core.Test]
        public async Task ContainsKeyReflectsIndexState()
        {
            LinkedListIndex<string, int> index = new(new LinkedList<int>());

            await Assert.That(index.ContainsKey("item")).IsFalse();

            index.Add("item", 5);
            await Assert.That(index.ContainsKey("item")).IsTrue();

            index.Remove("item");
            await Assert.That(index.ContainsKey("item")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ClearHandlesNullMapAndClearsExistingEntries()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            index.Clear();

            index.Add("first", 1);
            index.Add("second", 2);
            await Assert.That(index.ContainsKey("first")).IsTrue();

            index.Clear();

            await Assert.That(index.ContainsKey("first")).IsFalse();
            await Assert.That(backingList.Count).IsEqualTo(2);
        }
    }
}
#pragma warning restore CA2007
