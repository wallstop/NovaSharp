namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    public sealed class TablePairTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task NilPropertyExposesSharedSentinel()
        {
            TablePair nilPair = TablePair.Nil;

            await Assert.That(nilPair.Key).IsEqualTo(LuaValue.Nil).ConfigureAwait(false);
            await Assert.That(nilPair.Value).IsEqualTo(LuaValue.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualityDependsOnKeyAndValue()
        {
            TablePair left = new(LuaValue.NewNumber(1), LuaValue.NewString("value"));
            TablePair right = new(LuaValue.NewNumber(1), LuaValue.NewString("value"));
            TablePair differentValue = new(LuaValue.NewNumber(1), LuaValue.NewString("other"));
            TablePair differentKey = new(LuaValue.NewNumber(2), LuaValue.NewString("value"));
            object boxedPair = right;

            await Assert.That(left).IsEqualTo(right).ConfigureAwait(false);
            await Assert.That(left.Equals(boxedPair)).IsTrue().ConfigureAwait(false);
            await Assert.That(left.Equals(differentValue)).IsFalse().ConfigureAwait(false);
            await Assert.That(left.Equals(differentKey)).IsFalse().ConfigureAwait(false);
            await Assert
                .That(left.GetHashCode())
                .IsEqualTo(right.GetHashCode())
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualsObjectReturnsFalseForDifferentTypeOrNull()
        {
            TablePair pair = new(LuaValue.NewNumber(3), LuaValue.NewString("payload"));

            await Assert.That(pair.Equals("not a table pair")).IsFalse().ConfigureAwait(false);
            await Assert.That(pair.Equals(null)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeHandlesNullKeyAndValue()
        {
            TablePair defaultPair = default;
            TablePair anotherDefault = default;

            await Assert.That(defaultPair).IsEqualTo(anotherDefault).ConfigureAwait(false);
            await Assert
                .That(defaultPair.GetHashCode())
                .IsEqualTo(anotherDefault.GetHashCode())
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorStoresKeyAndValue()
        {
            LuaValue key = LuaValue.NewNumber(7);
            LuaValue value = LuaValue.NewString("payload");
            TablePair pair = new(key, value);

            await Assert.That(pair.Key).IsEqualTo(key).ConfigureAwait(false);
            await Assert.That(pair.Value).IsEqualTo(value).ConfigureAwait(false);
        }
    }
}
