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
    }
}
