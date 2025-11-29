#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class TablePairTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task NilPropertyExposesSharedSentinel()
        {
            TablePair nilPair = TablePair.Nil;

            await Assert.That(nilPair.Key).IsEqualTo(DynValue.Nil);
            await Assert.That(nilPair.Value).IsEqualTo(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task EqualityDependsOnKeyAndValue()
        {
            TablePair left = new(DynValue.NewNumber(1), DynValue.NewString("value"));
            TablePair right = new(DynValue.NewNumber(1), DynValue.NewString("value"));
            TablePair differentValue = new(DynValue.NewNumber(1), DynValue.NewString("other"));
            TablePair differentKey = new(DynValue.NewNumber(2), DynValue.NewString("value"));
            object boxedPair = right;

            await Assert.That(left).IsEqualTo(right);
            await Assert.That(left.Equals(boxedPair)).IsTrue();
            await Assert.That(left.Equals(differentValue)).IsFalse();
            await Assert.That(left.Equals(differentKey)).IsFalse();
            await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        }

        [global::TUnit.Core.Test]
        public async Task EqualsObjectReturnsFalseForDifferentTypeOrNull()
        {
            TablePair pair = new(DynValue.NewNumber(3), DynValue.NewString("payload"));

            await Assert.That(pair.Equals("not a table pair")).IsFalse();
            await Assert.That(pair.Equals(null)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeHandlesNullKeyAndValue()
        {
            TablePair defaultPair = default;
            TablePair anotherDefault = default;

            await Assert.That(defaultPair).IsEqualTo(anotherDefault);
            await Assert.That(defaultPair.GetHashCode()).IsEqualTo(anotherDefault.GetHashCode());
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorStoresKeyAndValue()
        {
            DynValue key = DynValue.NewNumber(7);
            DynValue value = DynValue.NewString("payload");
            TablePair pair = new(key, value);

            await Assert.That(pair.Key).IsEqualTo(key);
            await Assert.That(pair.Value).IsEqualTo(value);
        }
    }
}
#pragma warning restore CA2007
