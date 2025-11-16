namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TablePairTests
    {
        [Test]
        public void ConstructorStoresKeyAndValue()
        {
            DynValue key = DynValue.NewString("answer");
            DynValue value = DynValue.NewNumber(42);

            TablePair pair = new(key, value);

            Assert.Multiple(() =>
            {
                Assert.That(pair.Key, Is.EqualTo(key));
                Assert.That(pair.Value, Is.EqualTo(value));
            });
        }

        [Test]
        public void NilPairContainsNilKeyAndValue()
        {
            TablePair nil = TablePair.Nil;

            Assert.Multiple(() =>
            {
                Assert.That(nil.Key.Type, Is.EqualTo(DataType.Nil));
                Assert.That(nil.Value.Type, Is.EqualTo(DataType.Nil));
            });
        }

        [Test]
        public void ValueSetterDoesNotChangeStoredValueWhenKeyIsNil()
        {
            TablePair pair = new(DynValue.Nil, DynValue.NewNumber(10));
            pair.Value = DynValue.NewNumber(99);

            Assert.That(pair.Value.Number, Is.EqualTo(10));
        }

        [Test]
        public void EqualityMembersDependOnKeyAndValueIdentity()
        {
            DynValue key = DynValue.NewString("answer");
            DynValue value = DynValue.NewNumber(42);

            TablePair baseline = new(key, value);
            TablePair same = new(key, value);
            TablePair differentValue = new(key, DynValue.NewNumber(99));
            TablePair differentKey = new(DynValue.NewString("question"), value);

            Assert.Multiple(() =>
            {
                Assert.That(baseline.Equals(same), Is.True);
                Assert.That(baseline == same, Is.True);
                Assert.That(baseline.GetHashCode(), Is.EqualTo(same.GetHashCode()));

                Assert.That(baseline.Equals(differentValue), Is.False);
                Assert.That(baseline != differentValue, Is.True);

                Assert.That(baseline.Equals(differentKey), Is.False);
                Assert.That(baseline != differentKey, Is.True);
            });
        }

        [Test]
        public void EqualsObjectHandlesBoxedPairsAndOtherTypes()
        {
            TablePair pair = new(DynValue.NewString("left"), DynValue.NewNumber(7));
            object boxed = pair;

            Assert.Multiple(() =>
            {
                Assert.That(pair.Equals(boxed), Is.True);
                Assert.That(pair.Equals("not a table pair"), Is.False);
            });
        }
    }
}
