namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Serialization
{
    using System;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Serialization.Json;

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
            LuaValue value = JsonNull.Create();

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
            LuaValue jsonNull = JsonNull.Create();
            LuaValue ordinaryNil = LuaValue.Nil;
            LuaValue number = LuaValue.NewNumber(1);

            await Assert.That(JsonNull.IsJsonNull(jsonNull)).IsTrue().ConfigureAwait(false);
            await Assert.That(JsonNull.IsJsonNull(ordinaryNil)).IsFalse().ConfigureAwait(false);
            await Assert.That(JsonNull.IsJsonNull(number)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsJsonNullThrowsOnNullDynValue()
        {
            await Assert.That(JsonNull.IsJsonNull(default)).IsFalse().ConfigureAwait(false);
        }
    }
}
