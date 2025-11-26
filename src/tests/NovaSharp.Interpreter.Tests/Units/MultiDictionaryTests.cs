namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.DataStructs;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MultiDictionaryTests
    {
        private static readonly int[] AlphaValues = { 1, 2 };

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
                Assert.That(dictionary.Find("alpha").ToArray(), Is.EqualTo(AlphaValues));
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

        [Test]
        public void ConstructorWithComparerHonorsCaseInsensitiveKeys()
        {
            MultiDictionary<string, int> dictionary = new(StringComparer.OrdinalIgnoreCase);
            dictionary.Add("Alpha", 1);

            Assert.Multiple(() =>
            {
                Assert.That(dictionary.ContainsKey("alpha"), Is.True);
                Assert.That(dictionary.Find("alpha").Single(), Is.EqualTo(1));
                Assert.That(dictionary.Keys.Single(), Is.EqualTo("Alpha"));
            });
        }

        [Test]
        public void RemoveValueReturnsFalseWhenValueMissing()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);

            bool removed = dictionary.RemoveValue("alpha", 999);

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.False);
                Assert.That(dictionary.ContainsKey("alpha"), Is.True);
            });
        }

        [Test]
        public void RemoveValueReturnsFalseWhenKeyMissing()
        {
            MultiDictionary<string, int> dictionary = new();

            bool removed = dictionary.RemoveValue("alpha", 1);

            Assert.That(removed, Is.False);
        }
    }
}
