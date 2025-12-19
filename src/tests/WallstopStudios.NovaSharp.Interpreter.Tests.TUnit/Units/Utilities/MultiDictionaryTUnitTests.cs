namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    public sealed class MultiDictionaryTUnitTests
    {
        private static readonly int[] AlphaValues = { 1, 2 };

        [global::TUnit.Core.Test]
        public async Task AddCreatesListForNewKeyAndAppendsForExistingKey()
        {
            MultiDictionary<string, int> dictionary = new();

            bool firstInsert = dictionary.Add("alpha", 1);
            bool secondInsert = dictionary.Add("alpha", 2);

            await Assert.That(firstInsert).IsTrue().ConfigureAwait(false);
            await Assert.That(secondInsert).IsFalse().ConfigureAwait(false);
            await Assert
                .That(dictionary.Find("alpha").ToArray())
                .IsEquivalentTo(AlphaValues)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FindReturnsEmptySequenceWhenKeyMissing()
        {
            MultiDictionary<string, int> dictionary = new();

            IEnumerable<int> result = dictionary.Find("missing");

            await Assert.That(result).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ContainsKeyReflectsInsertedKeys()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);

            await Assert.That(dictionary.ContainsKey("alpha")).IsTrue().ConfigureAwait(false);
            await Assert.That(dictionary.ContainsKey("beta")).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveValueDeletesSingleValueAndRemovesKeyWhenListEmpty()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);
            dictionary.Add("alpha", 2);

            bool removedLast = dictionary.RemoveValue("alpha", 1);
            bool removedOnly = dictionary.RemoveValue("alpha", 2);

            await Assert.That(removedLast).IsFalse().ConfigureAwait(false);
            await Assert.That(removedOnly).IsTrue().ConfigureAwait(false);
            await Assert.That(dictionary.ContainsKey("alpha")).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveDeletesAllValuesForKey()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);

            dictionary.Remove("alpha");

            await Assert.That(dictionary.Find("alpha")).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClearRemovesAllKeysAndValues()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);
            dictionary.Add("beta", 2);

            dictionary.Clear();

            await Assert.That(dictionary.ContainsKey("alpha")).IsFalse().ConfigureAwait(false);
            await Assert.That(dictionary.Keys).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithComparerHonorsCaseInsensitiveKeys()
        {
            MultiDictionary<string, int> dictionary = new(StringComparer.OrdinalIgnoreCase);
            dictionary.Add("Alpha", 1);

            await Assert.That(dictionary.ContainsKey("alpha")).IsTrue().ConfigureAwait(false);
            await Assert.That(dictionary.Find("alpha").Single()).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(dictionary.Keys.Single()).IsEqualTo("Alpha").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveValueReturnsFalseWhenValueMissing()
        {
            MultiDictionary<string, int> dictionary = new();
            dictionary.Add("alpha", 1);

            bool removed = dictionary.RemoveValue("alpha", 999);

            await Assert.That(removed).IsFalse().ConfigureAwait(false);
            await Assert.That(dictionary.ContainsKey("alpha")).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveValueReturnsFalseWhenKeyMissing()
        {
            MultiDictionary<string, int> dictionary = new();

            bool removed = dictionary.RemoveValue("alpha", 1);

            await Assert.That(removed).IsFalse().ConfigureAwait(false);
        }
    }
}
