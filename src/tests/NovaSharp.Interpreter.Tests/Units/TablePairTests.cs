namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TablePairTests
    {
        [Test]
        public void NilPropertyExposesSharedSentinel()
        {
            TablePair nilPair = TablePair.Nil;

            Assert.Multiple(() =>
            {
                Assert.That(nilPair.Key, Is.EqualTo(DynValue.Nil));
                Assert.That(nilPair.Value, Is.EqualTo(DynValue.Nil));
            });
        }

        [Test]
        public void EqualityDependsOnKeyAndValue()
        {
            TablePair left = new(DynValue.NewNumber(1), DynValue.NewString("value"));
            TablePair right = new(DynValue.NewNumber(1), DynValue.NewString("value"));
            TablePair differentValue = new(DynValue.NewNumber(1), DynValue.NewString("other"));
            TablePair differentKey = new(DynValue.NewNumber(2), DynValue.NewString("value"));
            object boxedPair = right;

            Assert.Multiple(() =>
            {
                Assert.That(left, Is.EqualTo(right));
                Assert.That(left.Equals(boxedPair), Is.True);
                Assert.That(left != differentValue, Is.True);
                Assert.That(left != differentKey, Is.True);
                Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
            });
        }

        [Test]
        public void EqualsObjectReturnsFalseForDifferentTypeOrNull()
        {
            TablePair pair = new(DynValue.NewNumber(3), DynValue.NewString("payload"));

            Assert.Multiple(() =>
            {
                Assert.That(pair.Equals("not a table pair"), Is.False);
                Assert.That(pair.Equals(null), Is.False);
            });
        }

        [Test]
        public void GetHashCodeHandlesNullKeyAndValue()
        {
            TablePair defaultPair = default;
            TablePair anotherDefault = default;

            Assert.Multiple(() =>
            {
                Assert.That(defaultPair, Is.EqualTo(anotherDefault));
                Assert.That(defaultPair.GetHashCode(), Is.EqualTo(anotherDefault.GetHashCode()));
            });
        }

        [Test]
        public void ConstructorStoresKeyAndValue()
        {
            DynValue key = DynValue.NewNumber(7);
            DynValue value = DynValue.NewString("payload");
            TablePair pair = new(key, value);

            Assert.Multiple(() =>
            {
                Assert.That(pair.Key, Is.EqualTo(key));
                Assert.That(pair.Value, Is.EqualTo(value));
            });
        }
    }
}
