namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MultiDictionaryTests
    {
        [Test]
        public void AddCreatesListForNewKeyAndAppendsForExistingKey()
        {
            MultiDictionary<string, int> dictionary = new();

            bool firstInsert = dictionary.Add("alpha", 1);
            bool secondInsert = dictionary.Add("alpha", 2);

            Assert.Multiple(() =>
            {
                Assert.That(firstInsert, Is.True);
                Assert.That(secondInsert, Is.False);
                Assert.That(dictionary.Find("alpha").ToArray(), Is.EqualTo(new[] { 1, 2 }));
            });
        }

        [Test]
        public void FindReturnsEmptySequenceWhenKeyMissing()
        {
            MultiDictionary<string, int> dictionary = new();

            IEnumerable<int> result = dictionary.Find("missing");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ContainsKeyReflectsInsertedKeys()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);

            Assert.Multiple(() =>
            {
                Assert.That(dictionary.ContainsKey("alpha"), Is.True);
                Assert.That(dictionary.ContainsKey("beta"), Is.False);
            });
        }

        [Test]
        public void RemoveValueDeletesSingleValueAndRemovesKeyWhenListEmpty()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);
            dictionary.Add("alpha", 2);

            bool removedLast = dictionary.RemoveValue("alpha", 1);
            bool removedOnly = dictionary.RemoveValue("alpha", 2);

            Assert.Multiple(() =>
            {
                Assert.That(removedLast, Is.False);
                Assert.That(removedOnly, Is.True);
                Assert.That(dictionary.ContainsKey("alpha"), Is.False);
            });
        }

        [Test]
        public void RemoveDeletesAllValuesForKey()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);

            dictionary.Remove("alpha");

            Assert.That(dictionary.Find("alpha"), Is.Empty);
        }

        [Test]
        public void ClearRemovesAllKeysAndValues()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);
            dictionary.Add("beta", 2);

            dictionary.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(dictionary.ContainsKey("alpha"), Is.False);
                Assert.That(dictionary.Keys, Is.Empty);
            });
        }
    }
}
