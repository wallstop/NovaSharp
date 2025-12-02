namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Serialization.Json;

    public sealed class JsonNullTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IsNullAlwaysReturnsTrue()
        {
            JsonNull.Create();
            await Assert.That(JsonNull.IsNull()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateReturnsStaticUserData()
        {
            DynValue value = JsonNull.Create();

            await Assert.That(value.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert.That(value.UserData.Object is null).IsTrue().ConfigureAwait(false);
            await Assert
                .That(value.UserData.Descriptor.Type)
                .IsEqualTo(typeof(JsonNull))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsJsonNullDetectsJsonNullValues()
        {
            DynValue jsonNull = JsonNull.Create();
            DynValue ordinaryNil = DynValue.Nil;
            DynValue number = DynValue.NewNumber(1);

            await Assert.That(JsonNull.IsJsonNull(jsonNull)).IsTrue().ConfigureAwait(false);
            await Assert.That(JsonNull.IsJsonNull(ordinaryNil)).IsFalse().ConfigureAwait(false);
            await Assert.That(JsonNull.IsJsonNull(number)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsJsonNullThrowsOnNullDynValue()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                JsonNull.IsJsonNull(null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("v").ConfigureAwait(false);
        }
    }
}
