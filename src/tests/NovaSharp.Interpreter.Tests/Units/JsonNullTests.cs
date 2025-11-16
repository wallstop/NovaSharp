namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Serialization.Json;
    using NUnit.Framework;

    [TestFixture]
    public sealed class JsonNullTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure the JsonNull userdata is registered before each test.
            JsonNull.Create();
        }

        [Test]
        public void IsNullAlwaysReturnsTrue()
        {
            Assert.That(JsonNull.IsNull(), Is.True);
        }

        [Test]
        public void CreateReturnsStaticUserData()
        {
            DynValue value = JsonNull.Create();

            Assert.Multiple(() =>
            {
                Assert.That(value.Type, Is.EqualTo(DataType.UserData));
                Assert.That(value.UserData.Object, Is.Null);
                Assert.That(value.UserData.Descriptor.Type, Is.EqualTo(typeof(JsonNull)));
            });
        }

        [Test]
        public void IsJsonNullDetectsJsonNullValues()
        {
            DynValue jsonNull = JsonNull.Create();
            DynValue ordinaryNil = DynValue.Nil;
            DynValue number = DynValue.NewNumber(1);

            Assert.Multiple(() =>
            {
                Assert.That(JsonNull.IsJsonNull(jsonNull), Is.True);
                Assert.That(JsonNull.IsJsonNull(ordinaryNil), Is.False);
                Assert.That(JsonNull.IsJsonNull(number), Is.False);
            });
        }
    }
}
