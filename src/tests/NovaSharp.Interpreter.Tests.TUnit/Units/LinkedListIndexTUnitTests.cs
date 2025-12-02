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

            await Assert.That(previous).IsEqualTo(default(int)).ConfigureAwait(false);
            await Assert.That(backingList.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(index.Find("answer")?.Value).IsEqualTo(42).ConfigureAwait(false);

            int updated = index.Set("answer", 88);

            await Assert.That(updated).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(backingList.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(index.Find("answer")?.Value).IsEqualTo(88).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveRemovesNodeFromListAndIndex()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);
            index.Add("first", 1);
            index.Add("second", 2);

            bool removed = index.Remove("first");

            await Assert.That(removed).IsTrue().ConfigureAwait(false);
            await Assert.That(backingList.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(index.Find("first")).IsNull().ConfigureAwait(false);

            bool missingRemoval = index.Remove("missing");
            await Assert.That(missingRemoval).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveReturnsFalseWhenMapNull()
        {
            LinkedListIndex<string, int> index = new(new LinkedList<int>());

            await Assert.That(index.Remove("unknown")).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryGetValueHandlesMissingAndExistingKeys()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            bool missingResult = index.TryGetValue("missing", out LinkedListNode<int> missing);
            await Assert.That(missingResult).IsFalse().ConfigureAwait(false);
            await Assert.That(missing).IsNull().ConfigureAwait(false);

            index.Add("value", 7);

            bool hitResult = index.TryGetValue("value", out LinkedListNode<int> node);
            await Assert.That(hitResult).IsTrue().ConfigureAwait(false);
            await Assert.That(node.Value).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ContainsKeyReflectsIndexState()
        {
            LinkedListIndex<string, int> index = new(new LinkedList<int>());

            await Assert.That(index.ContainsKey("item")).IsFalse().ConfigureAwait(false);

            index.Add("item", 5);
            await Assert.That(index.ContainsKey("item")).IsTrue().ConfigureAwait(false);

            index.Remove("item");
            await Assert.That(index.ContainsKey("item")).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClearHandlesNullMapAndClearsExistingEntries()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            index.Clear();

            index.Add("first", 1);
            index.Add("second", 2);
            await Assert.That(index.ContainsKey("first")).IsTrue().ConfigureAwait(false);

            index.Clear();

            await Assert.That(index.ContainsKey("first")).IsFalse().ConfigureAwait(false);
            await Assert.That(backingList.Count).IsEqualTo(2).ConfigureAwait(false);
        }
    }
}
