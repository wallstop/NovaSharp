namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LinkedListIndexTests
    {
        [Test]
        public void SetReturnsPreviousValueAndUpdatesExistingNode()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            int previous = index.Set("answer", 42);

            Assert.Multiple(() =>
            {
                Assert.That(previous, Is.EqualTo(default(int)));
                Assert.That(backingList.Count, Is.EqualTo(1));
                Assert.That(index.Find("answer")?.Value, Is.EqualTo(42));
            });

            int updated = index.Set("answer", 88);

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.EqualTo(42));
                Assert.That(
                    backingList.Count,
                    Is.EqualTo(1),
                    "Set should not create a new node when updating"
                );
                Assert.That(index.Find("answer")?.Value, Is.EqualTo(88));
            });
        }

        [Test]
        public void RemoveRemovesNodeFromListAndIndex()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);
            index.Add("first", 1);
            index.Add("second", 2);

            bool removed = index.Remove("first");

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(backingList.Count, Is.EqualTo(1));
                Assert.That(index.Find("first"), Is.Null);
            });

            bool missingRemoval = index.Remove("missing");
            Assert.That(missingRemoval, Is.False);
        }

        [Test]
        public void RemoveReturnsFalseWhenMapNull()
        {
            LinkedListIndex<string, int> index = new(new LinkedList<int>());

            Assert.That(index.Remove("unknown"), Is.False);
        }

        [Test]
        public void TryGetValueHandlesMissingAndExistingKeys()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            Assert.Multiple(() =>
            {
                Assert.That(
                    index.TryGetValue("missing", out LinkedListNode<int> missing),
                    Is.False
                );
                Assert.That(missing, Is.Null);
            });

            index.Add("value", 7);

            Assert.Multiple(() =>
            {
                Assert.That(index.TryGetValue("value", out LinkedListNode<int> node), Is.True);
                Assert.That(node.Value, Is.EqualTo(7));
            });
        }

        [Test]
        public void ContainsKeyReflectsIndexState()
        {
            LinkedListIndex<string, int> index = new(new LinkedList<int>());

            Assert.That(index.ContainsKey("item"), Is.False);

            index.Add("item", 5);
            Assert.That(index.ContainsKey("item"), Is.True);

            index.Remove("item");
            Assert.That(index.ContainsKey("item"), Is.False);
        }

        [Test]
        public void ClearHandlesNullMapAndClearsExistingEntries()
        {
            LinkedList<int> backingList = new();
            LinkedListIndex<string, int> index = new(backingList);

            // Map is null, ensure Clear is a no-op.
            index.Clear();

            index.Add("first", 1);
            index.Add("second", 2);
            Assert.That(index.ContainsKey("first"), Is.True);

            index.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(index.ContainsKey("first"), Is.False);
                Assert.That(
                    backingList.Count,
                    Is.EqualTo(2),
                    "Clearing the index does not mutate the list."
                );
            });
        }
    }
}
